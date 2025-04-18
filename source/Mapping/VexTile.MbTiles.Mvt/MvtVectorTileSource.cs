﻿using System.Globalization;
using BruTile;
using BruTile.MbTiles;
using BruTile.Predefined;
using SQLite;
using VexTile.Common.Enums;
using VexTile.Data.Sources;
using VexTile.Renderer.Mvt.AliFlux;
using VexTile.Renderer.Mvt.AliFlux.Sources;
using TileInfo = BruTile.TileInfo;

namespace VexTile.TileSource.Mvt;

/// <summary>
/// An <see cref="ITileSource"/> implementation for MapBox Vector Tiles files
/// </summary>
/// <seealso href="https://www.mapbox.com/developers/mbtiles/"/>
public class MvtVectorTileSource : ITileSource
{
    public MbTilesType Type { get; }
    public string Version { get; }
    public string Description { get; }
    public string Json { get; }
    public string Compression { get; }

    private readonly List<string>? _whitelist;
    private readonly Dictionary<int, TileRange>? _tileRange;

    private readonly VectorStyle _style;

    ///  <summary>
    /// 
    ///  </summary>
    ///  <param name="connectionString">The connection string to the mbtiles file</param>
    ///  <param name="schema">The TileSchema of the mbtiles file. If this parameter is set the schema information
    ///  within the mbtiles file will be ignored. </param>
    ///  <param name="type">BaseLayer or Overlay</param>
    ///  <param name="determineZoomLevelsFromTilesTable">When 'determineZoomLevelsFromTilesTable' is true the zoom levels
    ///  will be determined from the available tiles in the 'tiles' table. This operation can take long if there are many tiles in
    ///  the 'tiles' table. When 'determineZoomLevelsFromTilesTable' is false the zoom levels will be read from the metadata table
    /// (by reading 'zoomMin' and 'zoomMax'). If there are no zoom levels specified in the metadata table the GlobalSphericalMercator
    /// default levels are assumed. This parameter will have no effect if the schema is passed in as argument. The default is false.</param>
    ///  <param name="determineTileRangeFromTilesTable">In some cases not all tiles specified by the schema are present in each
    ///  level. When 'determineTileRangeFromTilesTable' is 'true' the range of tiles available for each level is determined
    ///  by the tiles present for each level in the 'tiles' table. The advantage is that requests can be faster because they do not have to
    ///  go to the database if they are outside the TileRange. The downside is that for large files it can take long to read the TileRange
    ///  from the tiles table. The default is false.</param>
    ///  <param name="styleKind">The style to use for the rendering</param>
    ///  <param name="styleProviderName">the name of the style's provider name</param>
    ///  <param name="whitelist">white list of layers to render</param>
    ///  <param name="determineExtent">when false, the extent is ignored and set to the world</param>
    public MvtVectorTileSource(
        SQLiteConnectionString connectionString,
        ITileSchema? schema = null,
        MbTilesType type = MbTilesType.None,
        bool determineZoomLevelsFromTilesTable = false,
        bool determineTileRangeFromTilesTable = false,
        VectorStyleKind styleKind = VectorStyleKind.Default,
        string styleProviderName = "openmaptiles",
        List<string>? whitelist = null,
        bool determineExtent = true)
    {
        if (!File.Exists(connectionString.DatabasePath))
            throw new FileNotFoundException($"The mbtiles file does not exist: '{connectionString.DatabasePath}'", connectionString.DatabasePath);

        _whitelist = whitelist;

        using var connection = new SQLiteConnection(connectionString);
        Schema = schema ?? ReadSchemaFromDatabase(connection, determineZoomLevelsFromTilesTable, determineExtent);
        Type = type == MbTilesType.None ? ReadType(connection) : type;
        Version = ReadString(connection, "version");
        Attribution = new Attribution(ReadString(connection, "attribution"));
        Description = ReadString(connection, "description");
        Name = ReadString(connection, "name");
        Json = ReadString(connection, "json");
        Compression = ReadString(connection, "compression");

        if (determineTileRangeFromTilesTable)
        {
            // The tile range should be based on the tiles actually present.
            var zoomLevelsFromDatabase = Schema.Resolutions.Select(r => r.Key);
            _tileRange = ReadTileRangeForEachLevelFromTilesTable(connection, zoomLevelsFromDatabase);
        }

        _style = new VectorStyle(styleKind);
        var dataSource = new SqliteDataSource(connectionString);
        var provider = new VectorTilesSource(dataSource);
        _style.SetSourceProvider(styleProviderName, provider);
    }

    private static ITileSchema ReadSchemaFromDatabase(SQLiteConnection connection, bool determineZoomLevelsFromTilesTable, bool determineExtent)
    {
        // ReadZoomLevels can return null. This is no problem. GlobalSphericalMercator will initialize with default values
        var zoomLevels = ReadZoomLevels(connection);

        var format = ReadFormat(connection); // we really only want this to be pbf, and if it is not, that is an issue

        var extent = ReadExtent(connection, determineExtent);

        if (determineZoomLevelsFromTilesTable)
            zoomLevels = ReadZoomLevelsFromTilesTable(connection);

        return new GlobalSphericalMercator(format.ToString(), YAxis.TMS, zoomLevels, extent: extent);
    }

    private static int[]? ReadZoomLevels(SQLiteConnection connection)
    {
        var zoomMin = ReadInt(connection, "minzoom");
        if (zoomMin == null)
            return null;
        var zoomMax = ReadInt(connection, "maxzoom");
        if (zoomMax == null)
            return null;

        var length = zoomMax.Value - zoomMin.Value + 1;
        var levels = new int[length];
        for (var i = 0; i < length; i++)
            levels[i] = i + zoomMin.Value;

        return levels;
    }

    private static string ReadString(SQLiteConnection connection, string name)
    {
        const string sql = "SELECT \"value\" FROM metadata WHERE \"name\"=?;";
        try
        {
            return connection.ExecuteScalar<string>(sql, name);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static int? ReadInt(SQLiteConnection connection, string name)
    {
        const string sql = "SELECT \"value\" FROM metadata WHERE \"name\"=?;";
        try
        {
            return connection.ExecuteScalar<int?>(sql, name);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static Extent ReadExtent(SQLiteConnection connection, bool determineExtent)
    {
        if (!determineExtent)
        {
            return new Extent(-20037508.342789, -20037508.342789, 20037508.342789, 20037508.342789);

        }

        const string sql = "SELECT \"value\" FROM metadata WHERE \"name\"=?;";
        try
        {

            var extentString = connection.ExecuteScalar<string>(sql, "bounds");
            var components = extentString.Split(',');
            var extent = new Extent(
                double.Parse(components[0], NumberFormatInfo.InvariantInfo),
                double.Parse(components[1], NumberFormatInfo.InvariantInfo),
                double.Parse(components[2], NumberFormatInfo.InvariantInfo),
                double.Parse(components[3], NumberFormatInfo.InvariantInfo)
                );

            return ToMercator(extent);

        }
        catch (Exception)
        {
            return new Extent(-20037508.342789, -20037508.342789, 20037508.342789, 20037508.342789);
        }
    }

    private static Extent ToMercator(Extent extent)
    {
        var minX = extent.MinX;
        var minY = extent.MinY;
        ToMercator(ref minX, ref minY);
        var maxX = extent.MaxX;
        var maxY = extent.MaxY;
        ToMercator(ref maxX, ref maxY);

        return new Extent(minX, minY, maxX, maxY);
    }

    private static void ToMercator(ref double mercatorXLon, ref double mercatorYLat)
    {
        if ((Math.Abs(mercatorXLon) > 180 || Math.Abs(mercatorYLat) > 90))
            return;

        var num = mercatorXLon * 0.017453292519943295;
        var x = 6378137.0 * num;
        var a = mercatorYLat * 0.017453292519943295;

        mercatorXLon = x;
        mercatorYLat = 3189068.5 * Math.Log((1.0 + Math.Sin(a)) / (1.0 - Math.Sin(a)));
    }

    public async Task<byte[]?> GetTileAsync(TileInfo tileInfo)
    {
        var index = tileInfo.Index;

        if (IsTileIndexValid(index))
        {
            byte[] result;
            
            var canvas = new SkiaCanvas();

            try
            {
                result = await TileRendererFactory.RenderAsync(
                    _style,
                    canvas,
                    tileInfo.Index.Col, tileInfo.Index.Row, Convert.ToInt32(tileInfo.Index.Level),
                    256, 256, 1,
                    whiteListLayers: _whitelist);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return null;
            }


            return result == null || result.Length == 0
                ? null
                : result;

        }
        return null;
    }

    private static int[] ReadZoomLevelsFromTilesTable(SQLiteConnection connection)
    {
        // Note: This can be slow
        var sql = "select distinct zoom_level as level from tiles";
        var zoomLevelsObjects = connection.Query<ZoomLevel>(sql);
        var zoomLevels = zoomLevelsObjects.Select(z => z.Level).ToArray();
        return zoomLevels;
    }

    [Table("tiles")]
    private class ZoomLevel // I would rather just user 'int' instead of this class in Query, but can't get it to work
    {
        [Column("level")]
        public int Level { get; set; }
    }

    private static Dictionary<int, TileRange> ReadTileRangeForEachLevelFromTilesTable(SQLiteConnection connection, IEnumerable<int> zoomLevels)
    {
        var tableName = "tiles";
        var tileRange = new Dictionary<int, TileRange>();
        foreach (var zoomLevel in zoomLevels)
        {
            var sql = $"select min(tile_column) AS tc_min, max(tile_column) AS tc_max, min(tile_row) AS tr_min, max(tile_row) AS tr_max from {tableName} where zoom_level = {zoomLevel};";
            var rangeForLevel = connection.Query<ZoomLevelMinMax>(sql).First();
            tileRange.Add(zoomLevel, rangeForLevel.ToTileRange());
        }
        return tileRange;
    }

    private static MbTilesFormat ReadFormat(SQLiteConnection connection)
    {
        var sql = "SELECT \"value\" FROM metadata WHERE \"name\"=\"format\";";
        var formatString = connection.ExecuteScalar<string>(sql);
        if (Enum.TryParse<MbTilesFormat>(formatString, true, out var format))
            return format;
        return MbTilesFormat.Png;
    }

    private static MbTilesType ReadType(SQLiteConnection connection)
    {
        var sql = "SELECT \"value\" FROM metadata WHERE \"name\"=\"type\";";
        var typeString = connection.ExecuteScalar<string>(sql);
        if (Enum.TryParse<MbTilesType>(typeString, true, out var type))
            return type;
        return MbTilesType.BaseLayer;
    }

    private bool IsTileIndexValid(TileIndex index)
    {
        if (_tileRange == null)
            return true;

        // This is an optimization that makes use of an additional 'map' table which is not part of the spec
        if (_tileRange.TryGetValue(index.Level, out var tileRange))
            return
                tileRange.FirstCol <= index.Col &&
                index.Col <= tileRange.LastCol &&
                tileRange.FirstRow <= index.Row &&
                index.Row <= tileRange.LastRow;
        return false;
    }
    public ITileSchema Schema { get; }
    public string Name { get; }
    public Attribution Attribution { get; set; }

    [Table("tiles")]
    private class ZoomLevelMinMax
    {
        [Column("tr_min")]
        public int TileRowMin { get; set; }
        [Column("tr_max")]
        public int TileRowMax { get; set; }
        [Column("tc_min")]
        public int TileColMin { get; set; }
        [Column("tc_max")]
        public int TileColMax { get; set; }
        // ReSharper restore UnusedAutoPropertyAccessor.Local

        public TileRange ToTileRange()
        {
            return new TileRange(TileColMin, TileRowMin, TileColMax - TileColMin + 1, TileRowMax - TileRowMin + 1);
        }
    }
}