using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts.Extensions;
using Mapsui.Styles;
using VexTile.Renderers.Mvt.Nts;

namespace VexTile.Nts.TestApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        InitMap();
    }

    readonly ObservableCollection<IFeature> _features = new();

    private void InitMap()
    {
        var mvtSource = new MvtSource(@"zurich.mbtiles");

        var tile = mvtSource.GetVectorTile(0, 0, 0);

        foreach (var layer in tile?.Layers ?? [])
        {
            Console.WriteLine(layer.Name);
            if (layer.Name == "water")
            {
                foreach (var feature in layer.Features)
                {
                    var item = feature.Geometry.ToFeature();
                    var vstyle = new VectorStyle
                    {
                        Fill = new Brush(Color.Blue),
                        Outline = new Pen(Color.Red)
                    };
                    item.Styles.Add(vstyle);
                    _features.Add(item);
                }
            }
            if (layer.Name == "landcover")
            {
                foreach (var feature in layer.Features)
                {
                    var item = feature.Geometry.ToFeature();
                    var vstyle = new VectorStyle
                    {
                        Fill = new Brush(Color.Yellow),
                        Outline = new Pen(Color.Orange)
                    };
                    item.Styles.Add(vstyle);
                    _features.Add(item);
                }
            }
            if (layer.Name == "boundary")
            {
                foreach (var feature in layer.Features)
                {
                    var item = feature.Geometry.ToFeature();
                    var vstyle = new VectorStyle
                    {
                        Fill = new Brush(Color.Black),
                        Outline = new Pen(Color.Black)
                    };
                    item.Styles.Add(vstyle);
                    _features.Add(item);
                }
            }
            if (layer.Name == "water_name")
            {
                foreach (var feature in layer.Features)
                {
                    var item = feature.Geometry.ToFeature();
                    var vstyle = new VectorStyle
                    {
                        Fill = new Brush(Color.Transparent),
                        Outline = new Pen(Color.Transparent)
                    };
                    item.Styles.Add(vstyle);

                    if (feature.Attributes.GetOptionalValue("name") is { } name)
                    {
                        var labelStyle = new LabelStyle()
                        {
                            Text = name.ToString(),
                        };
                        item.Styles.Add(labelStyle);
                    }

                    _features.Add(item);
                }
            }

            if (layer.Name == "place")
            {
                foreach (var feature in layer.Features)
                {
                    var item = feature.Geometry.ToFeature();
                    var vstyle = new VectorStyle
                    {
                        Fill = new Brush(Color.Transparent),
                        Outline = new Pen(Color.Transparent)
                    };
                    item.Styles.Add(vstyle);

                    if (feature.Attributes.GetOptionalValue("name") is { } name)
                    {
                        var labelStyle = new LabelStyle()
                        {
                            Text = name.ToString(),
                        };
                        item.Styles.Add(labelStyle);
                    }
                    _features.Add(item);
                }
            }
        }

        var mapLayer = new MemoryLayer("tile")
        {
            IsMapInfoLayer = true,
            Features = _features,
            Style = new VectorStyle
            {
                Fill = new Brush(Color.Transparent),
                Outline = new Pen(Color.Transparent)
            }
        };

        MapControl.Map.Layers.Add(mapLayer);
    }
}