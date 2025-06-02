using System.Collections.Generic;
using SkiaSharp;
using VexTile.Renderer.Mvt.AliFlux.Drawing;

namespace VexTile.Renderer.Mvt.AliFlux;

public interface ICanvas
{
    bool ClipOverflow { get; set; }

    void StartDrawing(double width, double height);

    SKColor BackgroundColor { get; }

    void DrawBackground(SKColor color);

    void DrawLineString(List<Point> geometry, Brush style);

    void DrawPolygon(List<Point> geometry, Brush style, SKColor? background);

    void DrawPoint(Point geometry, Brush style);

    void DrawText(Point geometry, Brush style);

    void DrawTextOnPath(List<Point> geometry, Brush style);

    void DrawImage(byte[] imageData, Brush style);

    void DrawUnknown(List<List<Point>> geometry, Brush style);

    void DrawDebugBox(TileInfo tileData, SKColor color);

    byte[] ToPngByteArray(int quality = 80);
}