using System.Threading.Tasks;
using VexTile.Common.Sources;

namespace VexTile.Renderer.Mvt.AliFlux.Sources;

public interface IVectorTileSource: IBaseTileSource
{
    Task<VectorTile> GetVectorTileAsync(int x, int y, int zoom);
    Task<byte[]> GetTileAsync(int x, int y, int zoom);
}

public interface IPbfTileSource: IBaseTileSource
{
    Task<VectorTile> GetTileAsync();
}
