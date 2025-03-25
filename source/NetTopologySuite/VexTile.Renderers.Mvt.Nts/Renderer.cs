using NetTopologySuite.Features;
using NetTopologySuite.IO.VectorTiles;
using VexTile.Common.Styles;

namespace VexTile.Renderers.Mvt.Nts;

public class Renderer
{
    public static void Render(VectorTile vt, MapboxVectorStyle style)
    {
        //Loop through each layer.
        foreach (var l in vt.Layers)
        {
            Console.WriteLine(l.Name);
            //Access the features of the layer and do something with them.
            var features = l.Features;

            foreach (IFeature feature in features)
            {
                Console.WriteLine($"--- {feature.Geometry.GeometryType}");
                foreach (var attributeName in feature.Attributes.GetNames())
                {
                    Console.WriteLine($"--- --- {attributeName} :: {feature.Attributes[attributeName]}");
                }
            }
        }
    }
}
