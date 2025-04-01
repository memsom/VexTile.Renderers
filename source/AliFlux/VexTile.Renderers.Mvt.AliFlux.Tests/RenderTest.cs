using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using VexTile.Common.Enums;
using VexTile.Data.Sources;
using VexTile.Renderer.Mvt.AliFlux;
using VexTile.Renderer.Mvt.AliFlux.Sources;
using Xunit;

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
        Assert.True(File.Exists(path));

        SQLiteConnectionString val = new(path, SQLiteOpenFlags.ReadOnly, false);
        var dataSource = new SqliteDataSource(val);
        var provider = new VectorTilesSource(dataSource);
        style.SetSourceProvider("openmaptiles", provider);
        var tile = await TileRendererFactory.RenderAsync(style, canvas, new TileInfo(0, 0, 0));

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
        Assert.True(File.Exists(path));

        var bytes = await File.ReadAllBytesAsync(path);

        var provider = new PbfTileSource(bytes);
        style.SetSourceProvider("openmaptiles", provider);
        var tile = await TileRendererFactory.RenderAsync(style, canvas, new TileInfo(0));

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
        Assert.True(File.Exists(path));

        var dataSource = new SqliteDataSource(path);
        var renderer = new TileRenderer(dataSource, VectorStyleKind.Default);

        var tile = await renderer.RenderTileAsync(canvas, new (0, 0, 0));

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


    [Fact]
    public async Task AAABasicFactoryRenderTest()
    {
        var canvas = new SkiaCanvas();
        var style = new VectorStyle(VectorStyleKind.Default);

        string path = "zurich.mbtiles";
        Assert.True(File.Exists(path));

        SQLiteConnectionString val = new(path, SQLiteOpenFlags.ReadOnly, false);
        var dataSource = new SqliteDataSource(val);
        var provider = new VectorTilesSource(dataSource);
        style.SetSourceProvider("openmaptiles", provider);

        var info = new TileInfo(3, 1, 2, layerWhiteList:["water"]); // australia
        var tile = await TileRendererFactory.RenderAsync(style, canvas, info);

        Assert.NotNull(tile);
        Assert.True(tile.Length > 0);

        // tile should be a PNG image

        Assert.True(IsPng(tile));

        if (File.Exists("aaa.png"))
        {
            File.Delete("aaa.png");
        }

        await File.WriteAllBytesAsync("aaa.png", tile);
    }
}