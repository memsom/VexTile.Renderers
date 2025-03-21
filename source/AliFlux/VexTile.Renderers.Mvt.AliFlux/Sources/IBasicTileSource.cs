using System.Threading.Tasks;

namespace VexTile.Renderer.Mvt.AliFlux.Sources;

public interface IBasicTileSource
{
    Task<byte[]> GetTileAsync(int x, int y, int zoom);
}