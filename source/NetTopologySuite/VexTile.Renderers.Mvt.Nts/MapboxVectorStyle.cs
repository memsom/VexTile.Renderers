using VexTile.Common.Drawing;

namespace VexTile.Renderers.Mvt.Nts;

public class MapboxVectorStyle
{
    public List<Layer> Layers { get; } = new();

    public Dictionary<string, object> Metadata { get; set; } = new();

    public Dictionary<string, Source> Sources { get; } = new();

    public string Hash { get; set; }
}
