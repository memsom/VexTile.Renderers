using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VexTile.Renderer.Mvt.AliFlux;

public interface ITileRenderer : IDisposable
{
    Task<byte[]> RenderTileAsync(
        ICanvas canvas,
        int x, int y, double zoom,
        double sizeX = 512, double sizeY = 512,
        double scale = 1,
        List<string> whiteListLayers = null);
}