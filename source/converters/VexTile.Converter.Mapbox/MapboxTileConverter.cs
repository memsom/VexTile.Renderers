using NetTopologySuite.IO.VectorTiles;
using System.IO.Compression;
using VexTile.Common.Sources;

namespace VexTile.Converter.Mapbox;

public class MapboxTileConverter : IVectorTileConverter
{
    private NetTopologySuite.IO.VectorTiles.Mapbox.MapboxTileReader _tileConverter;
    private IDataSource _dataSource;

    public MapboxTileConverter(IDataSource dataSource)
    {
        _dataSource = dataSource;
        _tileConverter = new NetTopologySuite.IO.VectorTiles.Mapbox.MapboxTileReader();
    }

    public async Task<VectorTile?> ConvertToVectorTile(NetTopologySuite.IO.VectorTiles.Tiles.Tile tile, byte[]? data = null)
    {
        if (_dataSource == null)
            return new VectorTile();

        // If no data is provided, get it from data source
        if (data == null)
            data = await _dataSource.GetTileAsync(tile);

        // Is there any data to use
        if (data == null)
            return null;

        Stream stream = new MemoryStream(data);

        if (IsGZipped(data))
            stream = new GZipStream(stream, CompressionMode.Decompress);

        return _tileConverter.Read(stream, tile);
    }

    private static bool IsGZipped(byte[] data)
    {
        return IsZipped(data, 3, "1F-8B-08");
    }

    private static bool IsZipped(byte[] data, int signatureSize = 4, string expectedSignature = "50-4B-03-04")
    {
        if (data.Length < signatureSize)
            return false;
        byte[] signature = new byte[signatureSize];
        Buffer.BlockCopy(data, 0, signature, 0, signatureSize);
        string actualSignature = BitConverter.ToString(signature);
        return actualSignature == expectedSignature;
    }
}
