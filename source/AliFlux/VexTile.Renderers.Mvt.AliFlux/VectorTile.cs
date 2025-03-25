using System.Collections.Generic;
using VexTile.Common.Drawing;
using VexTile.Common.Extensions;

namespace VexTile.Renderer.Mvt.AliFlux;

public class VectorTile
{
    public bool IsOverZoomed { get; set; }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "<Pending>")]
    public List<VectorTileLayer> Layers = new();

    public VectorTile ApplyExtent(Rect extent)
    {
        var newTile = new VectorTile
        {
            IsOverZoomed = IsOverZoomed
        };

        foreach (var layer in Layers)
        {
            var vectorLayer = new VectorTileLayer
            {
                Name = layer.Name
            };

            foreach (var feature in layer.Features)
            {
                var vectorFeature = new VectorTileFeature
                {
                    Attributes = new Dictionary<string, object>(feature.Attributes),
                    Extent = feature.Extent,
                    GeometryType = feature.GeometryType
                };

                var vectorGeometry = new List<List<Point>>();
                foreach (var geometry in feature.Geometry)
                {
                    var vectorPoints = new List<Point>();

                    foreach (var point in geometry)
                    {
                        double newX = Utils.ConvertRange(point.X, extent.Left, extent.Right, 0, vectorFeature.Extent);
                        double newY = Utils.ConvertRange(point.Y, extent.Top, extent.Bottom, 0, vectorFeature.Extent);

                        vectorPoints.Add(new Point(newX, newY));
                    }

                    vectorGeometry.Add(vectorPoints);
                }

                vectorFeature.Geometry = vectorGeometry;
                vectorLayer.Features.Add(vectorFeature);
            }

            newTile.Layers.Add(vectorLayer);
        }

        return newTile;
    }
}
