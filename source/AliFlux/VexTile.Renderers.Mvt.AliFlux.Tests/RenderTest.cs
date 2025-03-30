using SQLite;
using VexTile.Common;
using VexTile.Common.Enums;
using VexTile.Renderer.Mvt.AliFlux;
using VexTile.Renderer.Mvt.AliFlux.Sources;

namespace VexTile.Renderers.Mvt.AliFlux.Tests;

public class RenderTest
{
    // PNG header = 137 80 78 71 13 10 26 10
    private static bool IsPng(byte[] bytes) => bytes is [137, 80, 78, 71, 13, 10, 26, 10, ..];

    [Fact]
    public async Task BasicFactoryRenderTest()
    {
        var canvas = new SkiaCanvas();
        var style = new VectorStyle(VectorStyleKind.Default);

        string path = "zurich.mbtiles";

        var val = new SQLiteConnectionString(path, (SQLiteOpenFlags)1, false);
        var provider = new VectorTilesSource(new SQLiteConnection(val));
        style.SetSourceProvider("openmaptiles", provider);
        var tile = await TileRendererFactory.RenderAsync(style, canvas, 0, 0, 0);

        Assert.NotNull(tile);
        Assert.True(tile.Length > 0);

        // tile should be a PNG image

        Assert.True(IsPng(tile));

        if (File.Exists("test1.png"))
        {
            File.Delete("test1.png");
        }

        await File.WriteAllBytesAsync("test1.png", tile);
    }

    [Fact]
    public async Task BasicFactoryRenderTestPbf()
    {
        var canvas = new SkiaCanvas();
        var style = new VectorStyle(VectorStyleKind.Default);

        string path = "newyork-mapbox.pbf";

        var bytes = await File.ReadAllBytesAsync(path);

        var provider = new PbfTileSource(bytes);
        style.SetSourceProvider("openmaptiles", provider);
        var tile = await TileRendererFactory.RenderAsync(style, canvas, 0, 0, 0);

        Assert.NotNull(tile);
        Assert.True(tile.Length > 0);

        // tile should be a PNG image

        Assert.True(IsPng(tile));

        if (File.Exists("test1pbf.png"))
        {
            File.Delete("test1pbf.png");
        }

        await File.WriteAllBytesAsync("test1pbf.png", tile);
    }

    [Fact]
    public async Task BasicTileRendererTest()
    {
        var canvas = new SkiaCanvas();

        string path = "zurich.mbtiles";

        var renderer = new TileRenderer(path, VectorStyleKind.Default);

        var tile = await renderer.RenderTileAsync(canvas, 0, 0, 0);

        Assert.NotNull(tile);
        Assert.True(tile.Length > 0);

        // tile should be a PNG image

        Assert.True(IsPng(tile));

        if (File.Exists("test2.png"))
        {
            File.Delete("test2.png");
        }

        await File.WriteAllBytesAsync("test2.png", tile);
    }
}