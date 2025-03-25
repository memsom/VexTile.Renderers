using System.Collections.Generic;
using VexTile.Common.Drawing;
using VexTile.Renderer.Mvt.AliFlux.Drawing;

namespace VexTile.Renderer.Mvt.AliFlux;

public class VectorTileFeature
{
    public double Extent { get; set; }
    public string GeometryType { get; set; }

    public Dictionary<string, object> Attributes { get; set; } = new();

    public List<List<Point>> Geometry { get; set; } = new();
}
