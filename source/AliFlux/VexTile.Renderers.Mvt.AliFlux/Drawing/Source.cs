using VexTile.Common.Sources;
using VexTile.Renderer.Mvt.AliFlux.Sources;

namespace VexTile.Renderer.Mvt.AliFlux.Drawing;

public class Source
{
    public string URL { get; set; } = "";
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public IBasicTileSource Provider { get; set; } = null;
    public double? MinZoom { get; set; } = null;
    public double? MaxZoom { get; set; } = null;
}