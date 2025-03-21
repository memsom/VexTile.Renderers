using System.Threading.Tasks;

namespace VexTile.Renderer.Mvt.AliFlux.Sources;

public interface IVectorTileSource: IBasicTileSource
{
    Task<VectorTile> GetVectorTileAsync(int x, int y, int zoom);
}