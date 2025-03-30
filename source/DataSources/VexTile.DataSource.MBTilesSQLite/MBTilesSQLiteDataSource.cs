using BruTile;
using BruTile.Predefined;
using NetTopologySuite.IO.VectorTiles.Tiles;
using SQLite;
using System.Globalization;
using VexTile.Common.Sources;
using VexTile.DataSource.MBTilesSQLite.Tables;

namespace VexTile.DataSource.MBTilesSQLite;

/// <summary>
/// An <see cref="IDataSource"/> implementation for MapBox database files
/// </summary>
public class MBTilesSQLiteDataSource : IDataSource
{
    private readonly SQLiteConnectionString _connectionString;
    private readonly Dictionary<int, NetTopologySuite.IO.VectorTiles.Tiles.TileRange>? _tileRange;

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
    public MBTilesSQLiteDataSource(SQLiteConnectionString connectionString,
        ITileSchema? schema = null,
        MbTilesType type = MbTilesType.None,
        bool determineZoomLevelsFromTilesTable = false,
        bool determineTileRangeFromTilesTable = false)
    {
        if (!File.Exists(connectionString.DatabasePath))
            throw new FileNotFoundException($"The mbtiles file does not exist: '{connectionString.DatabasePath}'", connectionString.DatabasePath);

        _connectionString = connectionString;

        using var connection = new SQLiteConnection(connectionString);

        Schema = schema ?? ReadSchemaFromDatabase(connection, determineZoomLevelsFromTilesTable);
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
    }

    public MBTilesSQLiteDataSource(string path,
        ITileSchema? schema = null,
        MbTilesType type = MbTilesType.None,
        bool determineZoomLevelsFromTilesTable = false,
        bool determineTileRangeFromTilesTable = false) 
        : this(new SQLiteConnectionString(path, (SQLiteOpenFlags)1, false), schema, type, determineZoomLevelsFromTilesTable, determineTileRangeFromTilesTable)
    {
    }

    /// <summary>
    /// Type of MBTiles file
    /// </summary>
    public MbTilesType Type { get; }

    /// <summary>
    /// Version of MBTiles files content
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Description for MBTiles file content
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// JSON string containing extra information about MBTiles file content
    /// </summary>
    public string Json { get; }

    /// <summary>
    /// Compression type of MBTiles file
    /// </summary>
    public string Compression { get; }

    /// <summary>
    /// TileSchema of MBTiles file content
    /// </summary>
    public ITileSchema Schema { get; }

    /// <summary>
    /// Name of MBTiles file content
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Attribution for MBTiles file content
    /// </summary>
    public Attribution Attribution { get; set; }

    /// <summary>
    /// Get binary data for tile from database
    /// </summary>
    /// <param name="tileInfo">Tile to get binary data for</param>
    /// <returns>Task which returns binary data</returns>
    public async Task<byte[]?> GetTileAsync(Tile tile)
    {
        if (!IsTileIndexValid(tile))
            return null;

        byte[] result;

        try
        {
            var cn = new SQLiteAsyncConnection(_connectionString);
            {
                var sql = "SELECT tile_data FROM \"tiles\" WHERE zoom_level=? AND tile_row=? AND tile_column=?;";
                result = await cn.ExecuteScalarAsync<byte[]>(sql, tile.Zoom, tile.Y, tile.X).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
            return null;
        }

        return result == null || result.Length == 0 ? null : result;
    }

    private static ITileSchema ReadSchemaFromDatabase(SQLiteConnection connection, bool determineZoomLevelsFromTilesTable)
    {
        // ReadZoomLevels can return null. This is no problem. GlobalSphericalMercator will initialize with default values
        var zoomLevels = ReadZoomLevels(connection);
        var format = ReadFormat(connection);
        var extent = ReadExtent(connection);

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

    private static Extent ReadExtent(SQLiteConnection connection)
    {
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

    private static int[] ReadZoomLevelsFromTilesTable(SQLiteConnection connection)
    {
        // Note: This can be slow
        var sql = "SELECT DISTINCT zoom_level AS level FROM tiles";
        var zoomLevelsObjects = connection.Query<ZoomLevel>(sql);
        var zoomLevels = zoomLevelsObjects.Select(z => z.Level).ToArray();

        return zoomLevels;
    }

    private static Dictionary<int, NetTopologySuite.IO.VectorTiles.Tiles.TileRange> ReadTileRangeForEachLevelFromTilesTable(SQLiteConnection connection, IEnumerable<int> zoomLevels)
    {
        var tableName = "tiles";
        var tileRange = new Dictionary<int, NetTopologySuite.IO.VectorTiles.Tiles.TileRange>();

        foreach (var zoomLevel in zoomLevels)
        {
            var sql = $"SELECT MIN(tile_column) AS tc_min, max(tile_column) AS tc_max, min(tile_row) AS tr_min, max(tile_row) AS tr_max FROM {tableName} WHERE zoom_level = {zoomLevel};";
            var rangeForLevel = connection.Query<ZoomLevelMinMax>(sql).First();
            tileRange.Add(zoomLevel, rangeForLevel.ToTileRange(zoomLevel));
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
        if (Math.Abs(mercatorXLon) > 180 || Math.Abs(mercatorYLat) > 90)
            return;

        var num = mercatorXLon * 0.017453292519943295;
        var x = 6378137.0 * num;
        var a = mercatorYLat * 0.017453292519943295;

        mercatorXLon = x;
        mercatorYLat = 3189068.5 * Math.Log((1.0 + Math.Sin(a)) / (1.0 - Math.Sin(a)));
    }

    private bool IsTileIndexValid(Tile tile)
    {
        if (_tileRange == null)
            return true;

        // This is an optimization that makes use of an additional 'map' table which is not part of the spec
        if (_tileRange.TryGetValue(tile.Zoom, out var tileRange))
            return tileRange.XMin <= tile.X && tile.X <= tileRange.XMax &&
                tileRange.YMin <= tile.Y && tile.Y <= tileRange.YMax;

        return false;
    }
}