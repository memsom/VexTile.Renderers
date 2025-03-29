using System.Threading.Tasks;
using VexTile.Common.Sources;

namespace VexTile.Renderer.Mvt.AliFlux.Sources;

public interface IVectorTileSource: IBasicTileSource
{
    Task<VectorTile> GetVectorTileAsync(int x, int y, int zoom);
    Task<byte[]> GetTileAsync(int x, int y, int zoom);
}

public interface IPbfTileSource: IBasicTileSource
{
    Task<VectorTile> GetTileAsync();
}
