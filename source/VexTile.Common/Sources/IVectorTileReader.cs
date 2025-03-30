using NetTopologySuite.IO.VectorTiles;
using NetTopologySuite.IO.VectorTiles.Tiles;

namespace VexTile.Common.Sources;

public interface IVectorTileReader
{
    Task<VectorTile?> ReadVectorTile(Tile tile, byte[]? data = null);
}
