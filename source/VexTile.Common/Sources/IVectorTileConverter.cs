using NetTopologySuite.IO.VectorTiles;
using NetTopologySuite.IO.VectorTiles.Tiles;

namespace VexTile.Common.Sources;

public interface IVectorTileConverter
{
    Task<VectorTile?> ConvertToVectorTile(Tile tile, byte[]? data = null);
}
