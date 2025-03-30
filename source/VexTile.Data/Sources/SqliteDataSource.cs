using NLog;
using SQLite;
using VexTile.Common.Data;
using VexTile.Common.Sources;
using VexTile.Data.Tables;

namespace VexTile.Data.Sources;

/// <summary>
/// This class encapsulated the getting of tile and metadata from the MbTiles file
/// via a Sqlite connection
/// </summary>
public sealed class SqliteDataSource : ITileDataSource
{
    private readonly Logger log = LogManager.GetCurrentClassLogger();
    private readonly SQLiteConnection sharedConnection;

    public SqliteDataSource(string path)
    {
        var connectionString = new SQLiteConnectionString(path, SQLiteOpenFlags.ReadOnly, false);
        sharedConnection = new SQLiteConnection(connectionString);
    }

    /// <summary>
    /// This class encapsulated the getting of tile and metadata from the MbTiles file
    /// via a Sqlite connection
    /// </summary>
    public SqliteDataSource(SQLiteConnectionString connectionString)
    {
        try
        {
            sharedConnection = new SQLiteConnection(connectionString);
        }
        catch (Exception e)
        {
            log.Error(e);
            throw;
        }
    }

    /// <summary>
    /// Gets the entire metadata as an enumerable
    /// </summary>
    /// <returns>the metadata in the connected database</returns>
    public IEnumerable<IMetaData> GetMetaData()
    {
        foreach (MetaData? item in sharedConnection.Table<MetaData>())
        {
            yield return item;
        }
    }

    /// <summary>
    /// Gets a specific tile
    /// </summary>
    /// <param name="x">the X</param>
    /// <param name="y">the Y</param>
    /// <param name="zoom">the Zoom level</param>
    /// <returns>A tile where one was found, otherwise null</returns>
    public ITile? GetTile(int x, int y, int zoom) =>
        sharedConnection
            .Table<Tile>()
            .FirstOrDefault(t => t.X == x && t.Y == y && t.Zoom == zoom);

    public void Dispose()
    {
        sharedConnection.Close();
        sharedConnection.Dispose();
    }
}