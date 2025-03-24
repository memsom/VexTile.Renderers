using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;
using VexTile.Common;
using VexTile.Common.Enums;
using VexTile.Renderer.Mvt.AliFlux.Sources;

namespace VexTile.Renderer.Mvt.AliFlux;

public class TileRenderer : ITileRenderer
{
    private readonly VectorStyle style;
    private readonly SQLiteConnection connection;

    public TileRenderer(string path, VectorStyleKind styleKind, string customStyle = null, string styleProviderString = "openmaptiles")
        : this(new SQLiteConnection(new SQLiteConnectionString(path, (SQLiteOpenFlags)1, false)), styleKind, customStyle, styleProviderString) { }

    public TileRenderer(SQLiteConnection connection, VectorStyleKind styleKind, string customStyle = null, string styleProviderString = "openmaptiles")
    {
        style = new VectorStyle(styleKind)
        {
            CustomStyle = customStyle,
        };

        this.connection = connection;

        var provider = new VectorTilesSource(connection);
        style.SetSourceProvider(styleProviderString, provider);
    }

    public Task<byte[]> RenderTileAsync(
        ICanvas canvas,
        int x, int y, double zoom,
        double sizeX = 512, double sizeY = 512,
        double scale = 1,
        List<string> whiteListLayers = null) =>
        TileRendererFactory.RenderAsync(style, canvas, x, y, zoom, sizeX, sizeY, scale, whiteListLayers);

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            connection.Close();
            connection.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}