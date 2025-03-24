#nullable enable

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using VexTile.Common.Tables;
using VexTile.Renderer.Mvt.AliFlux.Drawing;
using VexTile.Renderer.Mvt.AliFlux.GlobalMercator;

namespace VexTile.Renderer.Mvt.AliFlux.Sources;

// MbTiles loading code in GIST by geobabbler
// https://gist.github.com/geobabbler/9213392
[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "<Pending>")]
public class VectorTilesSource : IVectorTileSource
{
    static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
    public GeoExtent? Bounds { get; private set; }
    public CoordinatePair? Center { get; private set; }
    public int MinZoom { get; private set; } = 0;
    public int MaxZoom { get; private set; } = 0;
    public string? Name { get; private set; }
    public string? Description { get; private set; }
    public string? MBTilesVersion { get; private set; }
    public string? Path { get; private set; }

    private readonly ConcurrentDictionary<string, VectorTile> tileCache = new();

    private readonly GlobalMercatorImplementation gmt = new();

    private readonly SQLiteConnection sharedConnection;

    // converted to use Sqlite-Net
    public VectorTilesSource(SQLiteConnection connection)
    {
        sharedConnection = connection ?? throw new ArgumentNullException(nameof(connection));

        LoadMetadata();
    }

    // converted to use Sqlite-Net
    private void LoadMetadata()
    {
        try
        {
            foreach (var item in sharedConnection.Table<MetaData>())
            {
                string name = item.Name;
                switch (name.ToLower())
                {
                    case "bounds":
                        string val = item.Value;
                        string[] vals = val.Split(',');
                        this.Bounds = new GeoExtent
                        {
                            West = Convert.ToDouble(vals[0]),
                            South = Convert.ToDouble(vals[1]),
                            East = Convert.ToDouble(vals[2]),
                            North = Convert.ToDouble(vals[3])
                        };
                        break;
                    case "center":
                        val = item.Value;
                        vals = val.Split(',');
                        this.Center = new CoordinatePair
                        {
                            X = Convert.ToDouble(vals[0]),
                            Y = Convert.ToDouble(vals[1])
                        };
                        break;
                    case "minzoom":
                        this.MinZoom = Convert.ToInt32(item.Value);
                        break;
                    case "maxzoom":
                        this.MaxZoom = Convert.ToInt32(item.Value);
                        break;
                    case "name":
                        this.Name = item.Value;
                        break;
                    case "description":
                        this.Description = item.Value;
                        break;
                    case "version":
                        this.MBTilesVersion = item.Value;
                        break;

                }
            }

        }
        catch (Exception)
        {
            throw new MemberAccessException("Could not load Mbtiles source file");
        }
    }

    // converted to use Sqlite-Net
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1168:Empty arrays and collections should be returned instead of null", Justification = "<Pending>")]
    public byte[]? GetRawTile(int x, int y, int zoom)
    {
        try
        {
            var found = sharedConnection.Table<Tiles>().FirstOrDefault(t => t.X == x && t.Y == y && t.Zoom == zoom);

            if (found is { } tiles)
            {
                return tiles.TileData;
            }
        }
        catch
        {
            throw new MemberAccessException("Could not load tile from Mbtiles");
        }

        return null;
    }

    public void ExtractTile(int x, int y, int zoom, string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        using (var fileStream = File.Create(path))
        using (var bfw = new BinaryWriter(fileStream))
        {
            if (GetRawTile(x, y, zoom) is byte[] bytes)
            {
                bfw.Write(bytes);
            }
            bfw.Close();
        }
    }

    public async Task<VectorTile?> GetVectorTileAsync(int x, int y, int zoom)
    {
        var extent = new Rect(0, 0, 1, 1);
        bool overZoomed = false;

        if (zoom > MaxZoom)
        {
            var bounds = gmt.TileLatLonBounds(x, y, zoom);

            var northEast = new CoordinatePair
            {
                X = bounds.East,
                Y = bounds.North
            };

            var northWest = new CoordinatePair
            {
                X = bounds.West,
                Y = bounds.North
            };

            var southEast = new CoordinatePair
            {
                X = bounds.East,
                Y = bounds.South
            };

            var southWest = new CoordinatePair
            {
                X = bounds.West,
                Y = bounds.South
            };

            var center = new CoordinatePair
            {
                X = (northEast.X + southWest.X) / 2,
                Y = (northEast.Y + southWest.Y) / 2
            };

            var biggerTile = gmt.LatLonToTile(center.Y, center.X, MaxZoom);

            var biggerBounds = gmt.TileLatLonBounds(biggerTile.X, biggerTile.Y, MaxZoom);

            var newL = Utils.ConvertRange(northWest.X, biggerBounds.West, biggerBounds.East, 0, 1);
            var newT = Utils.ConvertRange(northWest.Y, biggerBounds.North, biggerBounds.South, 0, 1);

            var newR = Utils.ConvertRange(southEast.X, biggerBounds.West, biggerBounds.East, 0, 1);
            var newB = Utils.ConvertRange(southEast.Y, biggerBounds.North, biggerBounds.South, 0, 1);

            extent = new Rect(new Point(newL, newT), new Point(newR, newB));
            //thisZoom = MaxZoom;

            x = biggerTile.X;
            y = biggerTile.Y;
            zoom = MaxZoom;

            overZoomed = true;
        }

        try
        {
            var actualTile = await GetCachedVectorTileAsync(x, y, zoom);

            if (actualTile != null)
            {
                actualTile.IsOverZoomed = overZoomed;
                actualTile = actualTile.ApplyExtent(extent);
            }

            return actualTile;

        }
        catch (Exception e)
        {
            log.Error(e);
            return null;
        }
    }

    private readonly object keyLocker = new();

    private async Task<VectorTile?> GetCachedVectorTileAsync(int x, int y, int zoom)
    {
        return await Task.Run(() =>
        {
            var key = x.ToString() + "," + y.ToString() + "," + zoom.ToString();

            lock (keyLocker)
            {
                if (tileCache.ContainsKey(key))
                {
                    return tileCache[key];
                }

                if (GetRawTile(x, y, zoom) is byte[] rawTileStream)
                {

                    var pbfTileProvider = new PbfTileSource(rawTileStream);
                    var tile = pbfTileProvider.GetVectorTileAsync(x, y, zoom).Result;
                    tileCache[key] = tile;

                    return tile;
                }

                return default;
            }
        });
    }

    public async Task<byte[]> GetTileAsync(int x, int y, int zoom) =>
        await Task.Run(() =>
        {
            if (GetRawTile(x, y, zoom) is byte[] rawTile)
            {
                return rawTile;
            }

            return new byte[0];
        });

}
