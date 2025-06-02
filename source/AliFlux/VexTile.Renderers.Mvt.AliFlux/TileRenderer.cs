using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VexTile.Common.Enums;
using VexTile.Common.Sources;
using VexTile.Renderer.Mvt.AliFlux.Sources;

namespace VexTile.Renderer.Mvt.AliFlux;

public class TileRenderer : ITileRenderer
{
    private readonly VectorStyle style;
    private readonly ITileDataSource connection;

    public TileRenderer(
        ITileDataSource connection,
        VectorStyleKind styleKind,
        string customStyle = null,
        string styleProviderString = "openmaptiles")
    {
        style = new VectorStyle(styleKind)
        {
            CustomStyle = customStyle,
        };

        this.connection = connection;

        var provider = new VectorTilesSource(connection);
        style.SetSourceProvider(styleProviderString, provider);
    }

    public async Task<byte[]> RenderTileAsync(
        ICanvas canvas,
        int x, int y, double zoom,
        double sizeX = 512, double sizeY = 512,
        double scale = 1,
        List<string> whiteListLayers = null)
    {
        await TileRendererFactory.RenderAsync(style, canvas, new TileInfo(x, y, zoom, sizeX, sizeY, scale, whiteListLayers));
        return canvas.ToPngByteArray();
    }

    public async Task<byte[]> RenderTileAsync(
        ICanvas canvas,
        TileInfo tileData)
    {
        await TileRendererFactory.RenderAsync(style, canvas, tileData);
        return canvas.ToPngByteArray();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            connection.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}