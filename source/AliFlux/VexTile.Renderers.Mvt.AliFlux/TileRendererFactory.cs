using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VexTile.Renderer.Mvt.AliFlux.Drawing;
using VexTile.Renderer.Mvt.AliFlux.Enums;
using VexTile.Renderer.Mvt.AliFlux.Sources;

namespace VexTile.Renderer.Mvt.AliFlux;

public static class TileRendererFactory
{
    static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

    public static async Task<byte[]> RenderAsync(VectorStyle style, ICanvas canvas, int x, int y, double zoom, double sizeX = 512, double sizeY = 512, double scale = 1, List<string> whiteListLayers = null)
    {
        Dictionary<Source, byte[]> rasterTileCache = new Dictionary<Source, byte[]>();
        Dictionary<Source, VectorTile> vectorTileCache = new Dictionary<Source, VectorTile>();
        Dictionary<string, List<VectorTileLayer>> categorizedVectorLayers = new Dictionary<string, List<VectorTileLayer>>();

        double actualZoom = zoom;

        if (sizeX < 1024)
        {
            double ratio = 1024 / sizeX;
            double zoomDelta = Math.Log(ratio, 2);

            actualZoom = zoom - zoomDelta;
        }

        sizeX *= scale;
        sizeY *= scale;

        canvas.StartDrawing(sizeX, sizeY);

        var visualLayers = new List<VisualLayer>();

        // refactor this messy block
        foreach (var layer in style.Layers)
        {
            if (whiteListLayers != null && layer.Type != "background" && layer.SourceLayer != "" && !whiteListLayers.Contains(layer.SourceLayer))
            {
                continue;
            }

            if (layer.Source != null)
            {
                if (layer.Source == null || layer.Source.Type == "vector")
                {
                    if (!vectorTileCache.ContainsKey(layer.Source) && layer.Source.Provider is IVectorTileSource source)
                    {
                        VectorTile tile = await source.GetVectorTileAsync(x, y, (int)zoom);

                        if (tile == null)
                        {
                            return null;
                        }

                        // magic sauce! :p
                        if (tile.IsOverZoomed)
                        {
                            canvas.ClipOverflow = true;
                        }

                        vectorTileCache[layer.Source] = tile;

                        // normalize the points from 0 to size
                        foreach (var vectorLayer in tile.Layers)
                        {
                            foreach (var feature in vectorLayer.Features)
                            {
                                foreach (var geometry in feature.Geometry)
                                {
                                    for (int i = 0; i < geometry.Count; i++)
                                    {
                                        var point = geometry[i];
                                        geometry[i] = new Point(point.X / feature.Extent * sizeX, point.Y / feature.Extent * sizeY);
                                    }
                                }
                            }
                        }

                        foreach (var tileLayer in tile.Layers)
                        {
                            if (!categorizedVectorLayers.ContainsKey(tileLayer.Name))
                            {
                                categorizedVectorLayers[tileLayer.Name] = new List<VectorTileLayer>();
                            }

                            categorizedVectorLayers[tileLayer.Name].Add(tileLayer);
                        }
                    }
                }
                else if (layer.Source.Type == "raster")
                {
                    if (!rasterTileCache.ContainsKey(layer.Source) && layer.Source.Provider is not null)
                    {
                        byte[] tile = await layer.Source.Provider.GetTileAsync(x, y, (int)zoom);

                        if (tile == null)
                        {
                            continue;
                        }

                        rasterTileCache[layer.Source] = tile;
                    }

                    if (rasterTileCache.ContainsKey(layer.Source) && style.ValidateLayer(layer, (int)zoom, null))
                    {
                        var brush = style.ParseStyle(layer, scale, new Dictionary<string, object>());

                        if (!brush.Paint.Visibility)
                        {
                            continue;
                        }

                        visualLayers.Add(new VisualLayer()
                        {
                            Type = VisualLayerType.Raster,
                            RasterData = rasterTileCache[layer.Source],
                            Brush = brush,
                        });
                    }
                }

                if (categorizedVectorLayers.TryGetValue(layer.SourceLayer, out var tileLayers))
                {
                    foreach (var tileLayer in tileLayers)
                    {
                        foreach (var feature in tileLayer.Features)
                        {
                            Dictionary<string, object> attributes = new(feature.Attributes)
                            {
                                ["$type"] = feature.GeometryType,
                                ["$id"] = layer.ID,
                                ["$zoom"] = actualZoom
                            };

                            if (style.ValidateLayer(layer, actualZoom, attributes))
                            {
                                var brush = style.ParseStyle(layer, scale, attributes);

                                if (!brush.Paint.Visibility)
                                {
                                    continue;
                                }

                                visualLayers.Add(new VisualLayer()
                                {
                                    Type = VisualLayerType.Vector,
                                    VectorTileFeature = feature,
                                    Geometry = feature.Geometry,
                                    Brush = brush,
                                    Id = $"{layer.ID} :: {layer.SourceName} :: {layer.SourceLayer}"
                                });
                            }
                        }
                    }
                }
            }
            else if (layer.Type == "background")
            {
                var brushes = style.GetStyleByType("background", actualZoom, scale);
                foreach (var brush in brushes)
                {
                    canvas.DrawBackground(brush);
                }
            }
        }

        // defered rendering to preserve text drawing order
        foreach (var layer in visualLayers.OrderBy(item => item.Brush.ZIndex))
        {
            if (layer.Type == VisualLayerType.Vector)
            {
                var feature = layer.VectorTileFeature;
                var geometry = layer.Geometry;
                var brush = layer.Brush;

                if (!brush.Paint.Visibility)
                {
                    continue;
                }

                try
                {
                    if (feature.GeometryType == "Point")
                    {
                        foreach (var point in geometry)
                        {
                            canvas.DrawPoint(point.First(), brush);
                        }
                    }
                    else if (feature.GeometryType == "LineString")
                    {
                        foreach (var line in geometry)
                        {
                            canvas.DrawLineString(line, brush);
                        }
                    }
                    else if (feature.GeometryType == "Polygon")
                    {
                        foreach (var polygon in geometry)
                        {
                            canvas.DrawPolygon(polygon, brush);
                        }
                    }
                    else if (feature.GeometryType == "Unknown")
                    {
                        canvas.DrawUnknown(geometry, brush);
                    }
                    else
                    {
                        log.Debug($"unknown Geometry type {feature.GeometryType}");
                    }
                }
                catch (Exception e)
                {
                    log.Error(e);
                }
            }
            else if (layer.Type == VisualLayerType.Raster)
            {
                canvas.DrawImage(layer.RasterData, layer.Brush);
            }
        }

        foreach (var layer in visualLayers.OrderBy(item => item.Brush.ZIndex).Reverse())
        {
            if (layer.Type == VisualLayerType.Vector)
            {
                var feature = layer.VectorTileFeature;
                var geometry = layer.Geometry;
                var brush = layer.Brush;

                if (!brush.Paint.Visibility)
                {
                    continue;
                }

                if (feature.GeometryType == "Point")
                {
                    foreach (var point in geometry)
                    {
                        if (brush.Text != null)
                        {
                            canvas.DrawText(point.First(), brush);
                        }
                    }
                }
                else if (feature.GeometryType == "LineString")
                {
                    foreach (var line in geometry)
                    {
                        if (brush.Text != null)
                        {
                            canvas.DrawTextOnPath(line, brush);
                        }
                    }
                }
            }
        }

        return canvas.FinishDrawing();
    }
}