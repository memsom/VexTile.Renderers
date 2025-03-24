using System.IO;
using System.Threading.Tasks;
using VexTile.Common.Sources;

namespace VexTile.Renderer.Mvt.AliFlux.Sources;

public class RasterTileSource : IBasicTileSource
{
    public string Path { get; private set; }

    public RasterTileSource(string path)
    {
        this.Path = path;
    }

    public async Task<byte[]> GetTileAsync(int x, int y, int zoom)
    {
        return await Task.Run(() =>
        {
            var qualifiedPath = Path
                .Replace("{x}", x.ToString())
                .Replace("{y}", y.ToString())
                .Replace("{z}", zoom.ToString());

            return File.ReadAllBytes(qualifiedPath);
        });
    }
}