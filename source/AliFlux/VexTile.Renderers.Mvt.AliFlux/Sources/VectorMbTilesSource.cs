#nullable enable

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using VexTile.Common.Data;
using VexTile.Common.Sources;
using VexTile.Renderer.Mvt.AliFlux.Drawing;
using VexTile.Renderer.Mvt.AliFlux.GlobalMercator;

namespace VexTile.Renderer.Mvt.AliFlux.Sources;

// MbTiles loading code in GIST by geobabbler
// https://gist.github.com/geobabbler/9213392
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class VectorTilesSource : IVectorTileSource
{
    private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
    public GeoExtent? Bounds { get; private set; }
    public CoordinatePair? Center { get; private set; }
    public int MinZoom { get; private set; }
    public int MaxZoom { get; private set; }
    public string? Name { get; private set; }
    public string? Description { get; private set; }
    public string? MbTilesVersion { get; private set; }

    private readonly ConcurrentDictionary<string, VectorTile> tileCache = new();

    private readonly GlobalMercatorImplementation gmt = new();

    private readonly ITileDataSource sharedDataSource;

    // converted to use Sqlite-Net
    public VectorTilesSource(ITileDataSource dataSource)
    {
        sharedDataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));

        LoadMetadata();
    }

    // converted to use Sqlite-Net
    private void LoadMetadata()
    {
        try
        {
            foreach (IMetaData item in sharedDataSource.GetMetaData())
            {
                string name = item.Name;
                switch (name.ToLower())
                {
                    case "bounds":
                        string value = item.Value;
                        string[] values = value.Split(',');
                        Bounds = new GeoExtent
                        {
                            West = Convert.ToDouble(values[0]),
                            South = Convert.ToDouble(values[1]),
                            East = Convert.ToDouble(values[2]),
                            North = Convert.ToDouble(values[3])
                        };
                        break;
                    case "center":
                        value = item.Value;
                        values = value.Split(',');
                        Center = new CoordinatePair
                        {
                            X = Convert.ToDouble(values[0]),
                            Y = Convert.ToDouble(values[1])
                        };
                        break;
                    case "minzoom":
                        MinZoom = Convert.ToInt32(item.Value);
                        break;
                    case "maxzoom":
                        MaxZoom = Convert.ToInt32(item.Value);
                        break;
                    case "name":
                        Name = item.Value;
                        break;
                    case "description":
                        Description = item.Value;
                        break;
                    case "version":
                        MbTilesVersion = item.Value;
                        break;
                }
            }
        }
        catch (Exception)
        {
            throw new MemberAccessException("Could not load Mbtiles source file");
        }
    }

    private byte[]? GetRawTile(int x, int y, int zoom)
    {
        try
        {
            var found = sharedDataSource.GetTile(x, y, zoom);

            if (found is not null)
            {
                return found.TileData;
            }
        }
        catch
        {
            throw new MemberAccessException("Could not load tile from Mbtiles");
        }

        return null;
    }

    public async Task<VectorTile?> GetVectorTileAsync(int x, int y, int zoom)
    {
        Rect extent = new(0, 0, 1, 1);
        var overZoomed = false;

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
            if (await GetCachedVectorTileAsync(x, y, zoom) is { } cachedTile)
            {
                cachedTile.IsOverZoomed = overZoomed;
                return cachedTile.ApplyExtent(extent);
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
        }

        return null;
    }

    private readonly SemaphoreSlim _tileCacheLock = new(1, 1);

    private async Task<VectorTile?> GetCachedVectorTileAsync(int x, int y, int zoom)
    {
        await _tileCacheLock.WaitAsync();
        
        try
        {
            var key = x + "," + y + "," + zoom;
            if (tileCache.TryGetValue(key, out var existingTile))
            {
                return existingTile;
            }

            if (GetRawTile(x, y, zoom) is { } rawTileStream)
            {
                var pbfTileProvider = new PbfTileSource(rawTileStream);
                var tile = await pbfTileProvider.GetTileAsync();
                tileCache[key] = tile;

                return tile;
            }
        }
        finally
        {
            _tileCacheLock.Release();
        }

        return null;
    }

    public Task<byte[]> GetTileAsync(int x, int y, int zoom)
        => GetRawTile(x, y, zoom) is { } rawTile
           ? Task.FromResult(rawTile)
           : Task.FromResult(Array.Empty<byte>());
}
