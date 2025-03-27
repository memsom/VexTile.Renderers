using System.IO.Compression;
using NetTopologySuite.IO.VectorTiles;
using NetTopologySuite.IO.VectorTiles.Mapbox;
using SQLite;

namespace VexTile.Renderers.Mvt.Nts;

/// <summary>
/// This is an experimental class.
///
/// We can take the NTS Mapbox tile reading code and generate a vector tile object based on it.
///
/// This is in now way complete and should be assumed to be a POC at best.
/// </summary>
public class MvtSource
{
    private readonly SQLiteConnection db;
    private readonly MapboxTileReader reader;

    public MvtSource(string filename)
    {
        SQLiteConnectionString connectionString = new(filename, SQLiteOpenFlags.ReadOnly, false);
        db = new SQLiteConnection(connectionString);
        //Create a MapboxTileReader.
        reader = new MapboxTileReader();
    }

    public IEnumerable<VectorTile> GetVectorTiles(int z)
    {
        var tiles = db.Table<Tiles>().Where(t => t.Zoom == z);
        foreach (var tile in tiles)
        {
            var tileDefinition = new NetTopologySuite.IO.VectorTiles.Tiles.Tile(tile.X, tile.Y, tile.Zoom).InvertY();

            yield return reader.Read(GetVectorTileStream(tile), tileDefinition);
        }
    }

    public VectorTile? GetVectorTile(int x, int y, int z)
    {
        //Define which tile you want to read. You may be able to extract the x/y/zoom info from the file path of the tile.
        var tileDefinition = new NetTopologySuite.IO.VectorTiles.Tiles.Tile(x, y, z).InvertY();

        //Open a vector tile file as a stream.
        if (GetVectorTileStream(x, y, z) is { } tileStream)
        {
            //Read the vector tile.
            return reader.Read(tileStream, tileDefinition);
        }

        return null;
    }

    private Stream GetVectorTileStream(Tiles tile)
    {
        if (IsGZipped(tile.TileData))
        {
            using var stream = new MemoryStream(tile.TileData);
            using var zipStream = new GZipStream(stream, CompressionMode.Decompress);

            var resultStream = new MemoryStream();
            zipStream.CopyTo(resultStream);
            resultStream.Seek(0, SeekOrigin.Begin);
            return resultStream;
        }

        return new MemoryStream(tile.TileData);
    }

    private Stream? GetVectorTileStream(int x, int y, int z)
    {
        var tile = db.Table<Tiles>().FirstOrDefault(t => t.X == x && t.Y == y && t.Zoom == z);

        if (tile is null)
        {
            return null;
        }

        return GetVectorTileStream(tile);
    }

    private static bool IsGZipped(byte[] data) { return IsZipped(data, 3, "1F-8B-08"); }

    private static bool IsZipped(byte[] data, int signatureSize = 4, string expectedSignature = "50-4B-03-04")
    {
        if (data.Length < signatureSize)
        {
            return false;
        }

        byte[] signature = new byte[signatureSize];
        Buffer.BlockCopy(data, 0, signature, 0, signatureSize);
        string actualSignature = BitConverter.ToString(signature);
        return (actualSignature == expectedSignature);
    }
}