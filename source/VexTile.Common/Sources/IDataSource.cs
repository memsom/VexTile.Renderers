using NetTopologySuite.IO.VectorTiles.Tiles;

namespace VexTile.Common.Sources;

public interface IDataSource
{
    Task<byte[]?> GetTileAsync(Tile tile);
}
