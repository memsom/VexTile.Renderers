using System.Collections.Generic;
using VexTile.Common.Drawing;
using VexTile.Renderer.Mvt.AliFlux.Drawing;

namespace VexTile.Renderer.Mvt.AliFlux;

public interface ICanvas
{
    bool ClipOverflow { get; set; }

    void StartDrawing(double width, double height);

    void DrawBackground(Brush style);

    void DrawLineString(List<Point> geometry, Brush style);

    void DrawPolygon(List<Point> geometry, Brush style);

    void DrawPoint(Point geometry, Brush style);

    void DrawText(Point geometry, Brush style);

    void DrawTextOnPath(List<Point> geometry, Brush style);

    void DrawImage(byte[] imageData, Brush style);

    void DrawUnknown(List<List<Point>> geometry, Brush style);

    byte[] FinishDrawing();
}