using System.Collections.Generic;

namespace VexTile.Renderer.Mvt.AliFlux;

public class VectorTileLayer
{
    public string Name { get; set; }

    public List<VectorTileFeature> Features { get; } = new();
}