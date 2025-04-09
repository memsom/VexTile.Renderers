// defining this will add a box around the tile boundary and also
// burn in the XYZ value of the tile. This is very handy for debugging

#define USE_DEBUG_BOX

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using SkiaSharp;
using VexTile.Renderer.Mvt.AliFlux.Drawing;
using VexTile.Renderer.Mvt.AliFlux.Enums;
using VexTile.Renderer.Mvt.AliFlux.Sources;

namespace VexTile.Renderer.Mvt.AliFlux;

public static class TileRendererFactory
{
    private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Renders a tile at {x,y} with the given zoom level from the attached Provider using the style
    ///
    /// This is now a legacy compatibility helper as we can use the TileInfo instead
    /// </summary>
    /// <param name="style">the style to use</param>
    /// <param name="canvas">the canvas to draw to</param>
    /// <param name="x">the X</param>
    /// <param name="y">the Y</param>
    /// <param name="zoom">the zoom level</param>
    /// <param name="sizeX">optional width size for the tile, defaults to 512</param>
    /// <param name="sizeY">optional height size for the tile, defaults to 512</param>
    /// <param name="scale">optional scale, defaults to 1</param>
    /// <param name="whiteListLayers">optional whitelist to reduce layers to render</param>
    /// <param name="overrideBackground">override the default background color</param>
    /// <returns>a png</returns>
    public static async Task<byte[]> RenderAsync(VectorStyle style, ICanvas canvas, int x, int y, double zoom, double sizeX = 512, double sizeY = 512, double scale = 1, List<string> whiteListLayers = null, Color? overrideBackground = null) =>
        await RenderAsync(style, canvas,
            new TileInfo(x, y, zoom, sizeX, sizeY, scale, whiteListLayers), overrideBackground);

    /// <summary>
    /// This is basically to avoid a lot of boilerplate
    /// </summary>
    /// <param name="style">the style to apply</param>
    /// <param name="canvas">the canvas to draw on</param>
    /// <param name="tileData">contains all the tile information</param>
    /// <param name="overrideBackground">override the default background color</param>
    /// <returns>a png</returns>
    public static async Task<byte[]> RenderAsync(VectorStyle style, ICanvas canvas, TileInfo tileData, Color? overrideBackground = null)
    {
        Dictionary<Source, VectorTile> vectorTileCache = new();
        Dictionary<string, List<VectorTileLayer>> categorizedVectorLayers = new();

        double actualZoom = tileData.Zoom;

        if (tileData.SizeX < 1024)
        {
            double ratio = 1024 / tileData.SizeX;
            double zoomDelta = Math.Log(ratio, 2);

            actualZoom = tileData.Zoom - zoomDelta;
        }

        var sizeX = tileData.ScaledSizeX;
        var sizeY = tileData.ScaledSizeY;

        canvas.StartDrawing(sizeX, sizeY);

        var visualLayers = new List<VisualLayer>();

        // refactor this messy block
        foreach (var layer in style.Layers)
        {
            if (tileData.LayerWhiteList != null &&
                layer.Type != "background" &&
                layer.SourceLayer != "" &&
                !tileData.LayerWhiteList.Contains(layer.SourceLayer))
            {
                continue;
            }

            if (layer.Source != null)
            {
                if (layer.Source.Type == "raster")
                {
                    // we are no longer supporting raster as a source because the
                    // raster code seems to have been reading from a file. We can
                    // add a proper tile reader to access the mbtiles file if
                    // needed, but for now we were not doing anything useful with
                    // this source anyway
                    continue;
                }

                if (!vectorTileCache.ContainsKey(layer.Source) && layer.Source.Provider is { } source)
                {
                    VectorTile tile = null;

                    // we should be able to
                    if (source is IVectorTileSource vectorSource)
                        tile = await vectorSource.GetVectorTileAsync(tileData.X, tileData.Y, (int)tileData.Zoom);
                    else if (source is IPbfTileSource pbfSource) tile = await pbfSource.GetTileAsync();

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
                                    geometry[i] = new(point.X / feature.Extent * sizeX, point.Y / feature.Extent * sizeY);
                                }
                            }
                        }
                    }

                    foreach (var tileLayer in tile.Layers)
                    {
                        if (!categorizedVectorLayers.ContainsKey(tileLayer.Name))
                        {
                            categorizedVectorLayers[tileLayer.Name] = new();
                        }

                        categorizedVectorLayers[tileLayer.Name].Add(tileLayer);
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
                                var brush = style.ParseStyle(layer, tileData.Scale, attributes);

                                if (!brush.Paint.Visibility)
                                {
                                    continue;
                                }

                                visualLayers.Add(new()
                                {
                                    Type = VisualLayerType.Vector,
                                    VectorTileFeature = feature,
                                    Geometry = feature.Geometry,
                                    Brush = brush,
                                    LayerId = layer.ID,
                                    SourceName = layer.SourceName,
                                    SourceLayer = layer.SourceLayer,
                                });
                            }
                        }
                    }
                }
            }
            else if (layer.Type == "background")
            {
                var brushes = style.GetStyleByType("background", actualZoom, tileData.Scale);
                foreach (var brush in brushes)
                {
                    if (overrideBackground is { } c)
                    {
                        brush.Paint.BackgroundColor = new SKColor(c.R, c.G, c.B, c.A);
                    }

                    canvas.DrawBackground(brush.Paint.BackgroundColor);
                }
            }
        }

#if USE_DEBUG_BOX
        return RenderVisualLayers(canvas, visualLayers, tileData);
#else
        return RenderVisualLayers(canvas, visualLayers);
#endif
    }

#if USE_DEBUG_BOX
    private static byte[] RenderVisualLayers(ICanvas canvas, List<VisualLayer> visualLayers, TileInfo tileData)
#else
    private static byte[] RenderVisualLayers(ICanvas canvas, List<VisualLayer> visualLayers)
#endif
    {
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
                            //we know water is broken, so for now we are special casing it
                            if (layer.SourceLayer == "water")
                            {
                                canvas.DrawPolygon(polygon, brush, canvas.BackgroundColor);
                            }
                            else
                            {
                                canvas.DrawPolygon(polygon, brush, null);
                            }
                        }
                    }
                    else if (feature.GeometryType == "Unknown")
                    {
                        canvas.DrawUnknown(geometry, brush);
                    }
                    else
                    {
                        Log.Debug($"unknown Geometry type {feature.GeometryType}");
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
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

#if USE_DEBUG_BOX
        canvas.DrawDebugBox(tileData, SKColors.Black);
#endif

        return canvas.FinishDrawing();
    }
}