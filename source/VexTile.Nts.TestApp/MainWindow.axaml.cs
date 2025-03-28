using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using BruTile.MbTiles;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.VectorTiles;
using SQLite;
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
        var mbTilesTileSource = new MbTilesTileSource(new SQLiteConnectionString("base.mbtiles", true));
        var mbTilesLayer = new TileLayer(mbTilesTileSource) {Name = "base"};

        MapControl.Map.Layers.Add(mbTilesLayer);

        var res = MapControl.Map.Navigator.Resolutions[2];
        MapControl.Map.Navigator.ZoomTo(res);

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

        var mvtSource = new MvtSource(@"zurich.mbtiles");

        for (int zoom = 0; zoom < 18; zoom++)
        {
            foreach (var tile in mvtSource.GetVectorTiles(zoom))
            {
                ProcessTileData(tile, zoom);
            }
        }
    }

    double GetMin(int zoom)
    {

        if (zoom <= 0) return MapControl.Map.Navigator.Resolutions.First();
        if (zoom >= MapControl.Map.Navigator.Resolutions.Count) return MapControl.Map.Navigator.Resolutions.Last();
        return MapControl.Map.Navigator.Resolutions[zoom];
    }
    double GetMax(int zoom)
    {
        var actual = zoom + 1;
        if (actual <= 0) return MapControl.Map.Navigator.Resolutions.First();
        if (actual >= MapControl.Map.Navigator.Resolutions.Count) return MapControl.Map.Navigator.Resolutions.Last();
        return MapControl.Map.Navigator.Resolutions[actual];
    }

    private void ProcessTileData(VectorTile? tile, int? zoom = null)
    {
        if (tile == null) return;

        foreach (var layer in tile.Layers)
        {
            Console.WriteLine(layer.Name);
            if (layer.Name == "water")
            {
                foreach (var feature in layer.Features)
                {
                    var item = feature.Geometry.CompensatePoints().ToFeature();
                    var vstyle = new VectorStyle
                    {
                        Fill = new Brush(Color.Blue),
                        Outline = new Pen(Color.Red)
                    };

                    if (zoom is {} valid)
                    {
                        vstyle.MaxVisible = GetMax(valid);
                    }

                    item.Styles.Add(vstyle);
                    _features.Add(item);
                }
            }


            if (layer.Name == "landcover")
            {
                foreach (var feature in layer.Features)
                {
                    var item = feature.Geometry.CompensatePoints().ToFeature();
                    var vstyle = new VectorStyle
                    {
                        Fill = new Brush(Color.Yellow),
                        Outline = new Pen(Color.Orange)
                    };

                    if (zoom is {} valid)
                    {
                        vstyle.MaxVisible = GetMax(valid);
                    }

                    item.Styles.Add(vstyle);
                    _features.Add(item);
                }
            }

            continue; // temp

            if (layer.Name == "boundary")
            {
                foreach (var feature in layer.Features)
                {
                    var item = feature.Geometry.CompensatePoints().ToFeature();
                    var vstyle = new VectorStyle
                    {
                        Fill = new Brush(Color.Black),
                        Outline = new Pen(Color.Black)
                    };

                    if (zoom is {} valid)
                    {
                        vstyle.MaxVisible = GetMax(valid);
                    }

                    item.Styles.Add(vstyle);
                    _features.Add(item);
                }
            }

            if (layer.Name == "water_name")
            {
                foreach (var feature in layer.Features)
                {
                    var item = feature.Geometry.CompensatePoint().ToFeature();
                    var vstyle = new VectorStyle
                    {
                        Fill = new Brush(Color.Transparent),
                        Outline = new Pen(Color.Transparent)
                    };

                    if (zoom is {} valid)
                    {
                        vstyle.MinVisible = GetMin(valid);
                    }

                    item.Styles.Add(vstyle);

                    if (feature.Attributes.GetOptionalValue("name") is { } name)
                    {
                        var labelStyle = new LabelStyle()
                        {
                            Text = name.ToString(),
                        };

                        if (zoom is {} validlabel)
                        {
                            labelStyle.MinVisible = GetMin(validlabel);
                        }

                        item.Styles.Add(labelStyle);
                    }

                    _features.Add(item);
                }
            }

            if (layer.Name == "place")
            {
                foreach (var feature in layer.Features)
                {
                    feature.Geometry.Normalize();
                    var item = feature.Geometry.CompensatePoint().ToFeature();
                    var vstyle = new VectorStyle
                    {
                        Fill = new Brush(Color.Transparent),
                        Outline = new Pen(Color.Transparent)
                    };
                    if (zoom is {} valid)
                    {
                        vstyle.MinVisible = GetMin(valid);
                    }
                    item.Styles.Add(vstyle);

                    if (feature.Attributes.GetOptionalValue("name:en") is { } name)
                    {
                        var labelStyle = new LabelStyle()
                        {
                            Text = name.ToString(),
                        };

                        if (zoom is {} validlabel)
                        {
                            labelStyle.MaxVisible = GetMin(validlabel+1);
                        }
                        item.Styles.Add(labelStyle);
                    }

                    _features.Add(item);
                }
            }
        }
    }
}

public static class GeometryExtensions
{
    public static Geometry CompensatePoint(this Geometry geometry)
    {
        var position = geometry.Centroid;
        var (xx, yy) = SphericalMercator.FromLonLat(position.X, position.Y);

        foreach (var coordinate in geometry.Coordinates)
        {
            coordinate.X = xx;
            coordinate.Y = yy;
        }

        geometry.Normalize();

        geometry.GeometryChanged();

        return geometry;
    }

    public static Geometry CompensatePoints(this Geometry geometry)
    {
        foreach (var coordinate in geometry.Coordinates)
        {
            var (xx, yy) = SphericalMercator.FromLonLat(coordinate.X, coordinate.Y);
            coordinate.X = xx;
            coordinate.Y = yy;
        }

        geometry.GeometryChanged();

        return geometry;
    }
}