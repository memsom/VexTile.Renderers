using System.Collections.Generic;

namespace VexTile.Renderer.Mvt.AliFlux.Drawing;

public class Layer
{
    public int Index { get; set; } = -1;
    public string ID { get; set; } = "";
    public string Type { get; set; } = "";
    public string SourceName { get; set; } = "";
    public Source Source { get; set; }
    public string SourceLayer { get; set; } = "";
    public Dictionary<string, object> Paint { get; set; } = new();
    public Dictionary<string, object> Layout { get; set; } = new();
    public object[] Filter { get; set; } = [];
    public double? MinZoom { get; set; }
    public double? MaxZoom { get; set; }
}
