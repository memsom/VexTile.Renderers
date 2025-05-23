﻿using System.Collections.Generic;
using VexTile.Renderer.Mvt.AliFlux.Enums;

namespace VexTile.Renderer.Mvt.AliFlux.Drawing;

public class VisualLayer
{
    public VisualLayerType Type { get; set; }

    public byte[] RasterData { get; set; }

    public VectorTileFeature VectorTileFeature { get; set; }

    public List<List<Point>> Geometry { get; set; }

    public Brush Brush { get; set; }

    public string Id => $"{LayerId} :: {SourceName} :: {SourceLayer}";
    public string LayerId { get; set; }
    public string SourceName { get; set; }
    public string SourceLayer { get; set; }
}
