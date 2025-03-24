using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NLog;
using SkiaSharp;
using VexTile.Common;
using VexTile.Common.Enums;
using VexTile.Common.Sources;
using VexTile.Renderer.Mvt.AliFlux.Drawing;
using VexTile.Renderer.Mvt.AliFlux.Enums;
using VexTile.Renderer.Mvt.AliFlux.Sources;

namespace VexTile.Renderer.Mvt.AliFlux;

public class VectorStyle : IVectorStyle
{
    readonly Logger log = LogManager.GetCurrentClassLogger();
    public string Hash { get; }
    public List<Layer> Layers { get; } = new();
    public Dictionary<string, Source> Sources { get; } = new();
    public Dictionary<string, object> Metadata { get; } = new();

    protected readonly ConcurrentDictionary<string, Brush[]> BrushesCache = new();

    public string CustomStyle { get; set; }

    public VectorStyle(VectorStyleKind style, double scale = 1, string customStyle = null)
    {
        CustomStyle = customStyle;

        string json;

        if (style == VectorStyleKind.Custom)
        {
            if (!string.IsNullOrWhiteSpace(CustomStyle))
            {
                json = CustomStyle;
            }
            else
            {
#if DEBUG
                json = VectorStyleReader.GetStyle(VectorStyleKind.Default); // fallback use who know what will happen...
                log.Debug("The custom style was not set, so we have fallen back to Basic.");
#else
                throw new VectorStyleException("FATAL ERROR: Style could not be loaded!");
#endif
            }
        }
        else
        {
            json = VectorStyleReader.GetStyle(style);
        }


        // this should all be simplified to a generic template.
        var jObject = JObject.Parse(json);

        if (jObject["metadata"] != null)
        {
            Metadata = jObject["metadata"].ToObject<Dictionary<string, object>>();
        }

        //List<string> fontNames = new List<string>();


        if (jObject["sources"] is { } sources)
        {
            foreach (var jToken in sources)
            {
                var jSource = (JProperty)jToken;
                var source = new Source();

                if (jSource.Value is not IDictionary<string, JToken> sourceDict)
                {
                    continue;
                }

                source.Name = jSource.Name;

                if (sourceDict.TryGetValue("url", out var urlValue))
                {
                    source.URL = SimplifyJson(urlValue) as string;
                }

                if (sourceDict.TryGetValue("type", out var typeValue))
                {
                    source.Type = SimplifyJson(typeValue) as string;
                }

                if (sourceDict.TryGetValue("minzoom", out var minzoomValue))
                {
                    source.MinZoom = Convert.ToDouble(SimplifyJson(minzoomValue));
                }

                if (sourceDict.TryGetValue("maxzoom", out var maxzoomValue))
                {
                    source.MaxZoom = Convert.ToDouble(SimplifyJson(maxzoomValue));
                }

                Sources[jSource.Name] = source;
            }
        }

        JToken layersObject = jObject["layers"];
        int i = 0;

        if (layersObject is JArray layers)
        {
            foreach (JObject jLayer in layers.OfType<JObject>())
            {
                var layer = new Layer
                {
                    Index = i
                };

                IDictionary<string, JToken> layerDict = jLayer;

                if (layerDict.TryGetValue("minzoom", out var minzoomValue))
                {
                    layer.MinZoom = Convert.ToDouble(SimplifyJson(minzoomValue));
                }

                if (layerDict.TryGetValue("maxzoom", out var maxzoomValue))
                {
                    layer.MaxZoom = Convert.ToDouble(SimplifyJson(maxzoomValue));
                }

                if (layerDict.TryGetValue("id", out var idValue))
                {
                    layer.ID = SimplifyJson(idValue) as string;
                }

                if (layerDict.TryGetValue("type", out var typeValue))
                {
                    layer.Type = SimplifyJson(typeValue) as string;
                }

                if (layerDict.TryGetValue("source", out var sourceValue))
                {
                    layer.SourceName = SimplifyJson(sourceValue) as string;
                    layer.Source = Sources[layer.SourceName];
                }

                if (layerDict.TryGetValue("source-layer", out var sourceLayerValue))
                {
                    layer.SourceLayer = SimplifyJson(sourceLayerValue) as string;
                }

                if (layerDict.TryGetValue("paint", out var paintValue))
                {
                    layer.Paint = SimplifyJson(paintValue) as Dictionary<string, object>;
                }

                if (layerDict.TryGetValue("layout", out var layoutValue))
                {
                    layer.Layout = SimplifyJson(layoutValue) as Dictionary<string, object>;
                }

                if (layerDict.TryGetValue("filter", out var filterValue))
                {
                    var filterArray = filterValue as JArray;
                    layer.Filter = SimplifyJson(filterArray) as object[];
                }

                Layers.Add(layer);

                i++;
            }
        }

        Hash = Utils.Sha256(json);
    }


    public void SetSourceProvider(int index, IBasicTileSource provider)
    {
        int i = 0;
        foreach (var pair in Sources)
        {
            if (index == i)
            {
                pair.Value.Provider = provider;
                return;
            }

            i++;
        }
    }

    public void SetSourceProvider(string name, IBasicTileSource provider)
    {
        Sources[name].Provider = provider;
    }

    private static object SimplifyJson(JToken token)
    {
        if (token.Type == JTokenType.Object && token is IDictionary<string, JToken> dict)
        {
            return dict.Select(pair =>
                    new KeyValuePair<string, object>(
                        pair.Key,
                        SimplifyJson(pair.Value)))
                .ToDictionary(key => key.Key, value => value.Value);
        }

        if (token.Type == JTokenType.Array && token is JArray array)
        {
            return array
                .Select(SimplifyJson)
                .ToArray();
        }

        return token.ToObject<object>();
    }

    public Brush[] GetStyleByType(string type, double zoom, double scale = 1)
    {
        List<Brush> results = new();

        int i = 0;
        foreach (var layer in Layers)
        {
            if (layer.Type == type)
            {
                var attributes = new Dictionary<string, object>
                {
                    ["$type"] = "",
                    ["$id"] = "",
                    ["$zoom"] = zoom
                };

                results.Add(ParseStyle(layer, scale, attributes));
            }

            i++;
        }

        return results.ToArray();
    }

    public Brush ParseStyle(Layer layer, double scale, Dictionary<string, object> attributes)
    {
        var paintData = layer.Paint;
        var layoutData = layer.Layout;
        int index = layer.Index;

        var brush = new Brush
        {
            ZIndex = index,
            Layer = layer,
            //GlyphsDirectory = this.FontDirectory
        };

        var paint = new Paint();
        brush.Paint = paint;

        if (layer.ID == "country_label")
        {
            log.Debug("country_label -> nothing");
        }

        if (paintData != null)
        {
            if (paintData.TryGetValue("fill-color", out object fillColorValue))
            {
                paint.FillColor = ParseColor(GetValue(fillColorValue, attributes));
            }

            if (paintData.TryGetValue("background-color", out object backgroundColorValue))
            {
                paint.BackgroundColor = ParseColor(GetValue(backgroundColorValue, attributes));
            }

            if (paintData.TryGetValue("text-color", out object textColorValue))
            {
                paint.TextColor = ParseColor(GetValue(textColorValue, attributes));
            }

            if (paintData.TryGetValue("line-color", out object lineColorValue))
            {
                paint.LineColor = ParseColor(GetValue(lineColorValue, attributes));
            }

            // --

            if (paintData.TryGetValue("line-pattern", out object linePatternValue))
            {
                paint.LinePattern = (string)GetValue(linePatternValue, attributes);
            }

            if (paintData.TryGetValue("background-pattern", out object backgroundPatternValue))
            {
                paint.BackgroundPattern = (string)GetValue(backgroundPatternValue, attributes);
            }

            if (paintData.TryGetValue("fill-pattern", out object fillPatternValue))
            {
                paint.FillPattern = (string)GetValue(fillPatternValue, attributes);
            }

            // --

            if (paintData.TryGetValue("text-opacity", out object textOpacityValue))
            {
                paint.TextOpacity = Convert.ToDouble(GetValue(textOpacityValue, attributes));
            }

            if (paintData.TryGetValue("icon-opacity", out object iconOpacityValue))
            {
                paint.IconOpacity = Convert.ToDouble(GetValue(iconOpacityValue, attributes));
            }

            if (paintData.TryGetValue("line-opacity", out object lineOpacityValue))
            {
                paint.LineOpacity = Convert.ToDouble(GetValue(lineOpacityValue, attributes));
            }

            if (paintData.TryGetValue("fill-opacity", out object fillOpacityValue))
            {
                paint.FillOpacity = Convert.ToDouble(GetValue(fillOpacityValue, attributes));
            }

            if (paintData.TryGetValue("background-opacity", out object backgroundOpacityValue))
            {
                paint.BackgroundOpacity = Convert.ToDouble(GetValue(backgroundOpacityValue, attributes));
            }

            // --

            if (paintData.TryGetValue("line-width", out object lineWidthValue))
            {
                paint.LineWidth = Convert.ToDouble(GetValue(lineWidthValue, attributes)) * scale; // * screenScale;
            }

            if (paintData.TryGetValue("line-offset", out object lineOffsetValue))
            {
                paint.LineOffset = Convert.ToDouble(GetValue(lineOffsetValue, attributes)) * scale; // * screenScale;
            }

            if (paintData.TryGetValue("line-dasharray", out object lineDashArrayValue))
            {
                object[] array = (GetValue(lineDashArrayValue, attributes) as object[]);
                paint.LineDashArray = array.Select(item => Convert.ToDouble(item) * scale).ToArray();
            }

            // --

            if (paintData.TryGetValue("text-halo-color", out object textHaloColorValue))
            {
                paint.TextStrokeColor = ParseColor(GetValue(textHaloColorValue, attributes));
            }

            if (paintData.TryGetValue("text-halo-width", out object textHaloWidthValue))
            {
                paint.TextStrokeWidth = Convert.ToDouble(GetValue(textHaloWidthValue, attributes)) * scale;
            }

            if (paintData.TryGetValue("text-halo-blur", out object textHaloBlurValue))
            {
                paint.TextStrokeBlur = Convert.ToDouble(GetValue(textHaloBlurValue, attributes)) * scale;
            }
        }

        if (layoutData != null)
        {
            if (layoutData.TryGetValue("line-cap", out object lineCapValue))
            {
                string value = (string)GetValue(lineCapValue, attributes);
                if (value == "butt")
                {
                    paint.LineCap = PenLineCap.Flat;
                }
                else if (value == "round")
                {
                    paint.LineCap = PenLineCap.Round;
                }
                else if (value == "square")
                {
                    paint.LineCap = PenLineCap.Square;
                }
            }

            if (layoutData.TryGetValue("visibility", out object visibilityValue))
            {
                paint.Visibility = ((string)GetValue(visibilityValue, attributes)) == "visible";
            }

            if (layoutData.TryGetValue("text-field", out object value11))
            {
                brush.TextField = (string)GetValue(value11, attributes);

                // check performance implications of Regex.Replace
                brush.Text = Regex.Replace(
                    input: brush.TextField,
                    pattern: @"\{([A-Za-z0-9\-\:_]+)\}",
                    evaluator: m =>
                    {
                        string key = StripBraces(m.Value);
                        if (attributes.TryGetValue(key, out object attribute))
                        {
                            return attribute.ToString();
                        }

                        return "";
                    }).Trim();
            }

            if (layoutData.TryGetValue("text-font", out object textFornValue))
            {
                paint.TextFont = ((object[])GetValue(textFornValue, attributes)).Select(item => (string)item).ToArray();
            }

            if (layoutData.TryGetValue("text-size", out object textSizeValue))
            {
                paint.TextSize = Convert.ToDouble(GetValue(textSizeValue, attributes)) * scale;
            }

            if (layoutData.TryGetValue("text-max-width", out object testMaxWidthValue))
            {
                paint.TextMaxWidth = Convert.ToDouble(GetValue(testMaxWidthValue, attributes)) * scale; // * screenScale;
            }

            if (layoutData.TryGetValue("text-offset", out object textOffsetValue))
            {
                object[] value = (object[])GetValue(textOffsetValue, attributes);
                paint.TextOffset = new Point(Convert.ToDouble(value[0]) * scale, Convert.ToDouble(value[1]) * scale);
            }

            if (layoutData.TryGetValue("text-optional", out object textOptionalValue))
            {
                paint.TextOptional = (bool)(GetValue(textOptionalValue, attributes));
            }

            if (layoutData.TryGetValue("text-transform", out object textTransformValue))
            {
                string value = (string)GetValue(textTransformValue, attributes);
                if (value == "none")
                {
                    paint.TextTransform = TextTransform.None;
                }
                else if (value == "uppercase")
                {
                    paint.TextTransform = TextTransform.Uppercase;
                }
                else if (value == "lowercase")
                {
                    paint.TextTransform = TextTransform.Lowercase;
                }
            }

            if (layoutData.TryGetValue("icon-size", out object iconSizeValue))
            {
                paint.IconScale = Convert.ToDouble(GetValue(iconSizeValue, attributes)) * scale;
            }

            if (layoutData.TryGetValue("icon-image", out object iconImageValue))
            {
                paint.IconImage = (string)GetValue(iconImageValue, attributes);
            }
        }

        return brush;
    }

    private unsafe string StripBraces(string s)
    {
        int len = s.Length;
        char* newChars = stackalloc char[len];
        char* currentChar = newChars;

        for (int i = 0; i < len; ++i)
        {
            char c = s[i];
            switch (c)
            {
                case '{':
                case '}':
                    continue;
                default:
                    *currentChar++ = c;
                    break;
            }
        }

        return new string(newChars, 0, (int)(currentChar - newChars));
    }

    private static SKColor HslaToColor(double ta, double th, double ts, double tl)
    {
        double h = th / 365;
        double colorComponent = 0;
        double num = 0;
        double colorComponent1 = 0;
        double s = ts / 100;
        double l = tl / 100;
        if (!l.BasicallyEqualTo(0))
        {
            if (!s.BasicallyEqualTo(0))
            {
                double num1 = (l < 0.5 ? l * (1 + s) : l + s - l * s);
                double num2 = 2 * l - num1;
                colorComponent = GetColorComponent(num2, num1, h + 0.333333333333333);
                num = GetColorComponent(num2, num1, h);
                colorComponent1 = GetColorComponent(num2, num1, h - 0.333333333333333);
            }
            else
            {
                double num3 = l;
                colorComponent1 = num3;
                num = num3;
                colorComponent = num3;
            }
        }

        byte r = (255 * colorComponent) > 255 ? (byte)255 : (byte)(255 * colorComponent);
        byte g = (255 * num) > 255 ? (byte)255 : (byte)(255 * num);
        byte b = (255 * colorComponent1) > 255 ? (byte)255 : (byte)(255 * colorComponent1);
        byte a = (byte)ta;

        return SKColorFactory.MakeColor(r, g, b, a);
    }

    private static double GetColorComponent(double temp1, double temp2, double temp3)
    {
        temp3 = MoveIntoRange(temp3);
        if (temp3 < 0.166666666666667)
        {
            return temp1 + (temp2 - temp1) * 6 * temp3;
        }

        if (temp3 < 0.5)
        {
            return temp2;
        }

        if (temp3 >= 0.666666666666667)
        {
            return temp1;
        }

        return temp1 + (temp2 - temp1) * (0.666666666666667 - temp3) * 6;
    }

    private static double MoveIntoRange(double temp3)
    {
        if (temp3 < 0)
        {
            return temp3 + 1;
        }

        if (temp3 <= 1)
        {
            return temp3;
        }

        return temp3 - 1;
    }


    private SKColor ParseColor(object iColor)
    {
        var culture = new CultureInfo("en-US", true);

        if (iColor is System.Drawing.Color color)
        {
            return SKColorFactory.MakeColor(color.R, color.G, color.B, color.A);
        }

        if (iColor is SKColor skColor)
        {
            return SKColorFactory.LogColor(skColor);
        }

        string colorString = (string)iColor;

        if (colorString[0] == '#')
        {
            return SKColorFactory.LogColor(SKColor.Parse(colorString));
        }

        if (colorString.StartsWith("hsl("))
        {
            string[] segments = colorString.Replace('%', '\0').Split(',', '(', ')');

            double h = double.Parse(segments[1], culture);
            double s = double.Parse(segments[2], culture);
            double l = double.Parse(segments[3], culture);

            return HslaToColor(255, h, s, l);
        }

        if (colorString.StartsWith("hsla("))
        {
            string[] segments = colorString.Replace('%', '\0').Split(',', '(', ')');

            double h = double.Parse(segments[1], culture);
            double s = double.Parse(segments[2], culture);
            double l = double.Parse(segments[3], culture);
            double a = double.Parse(segments[4], culture) * 255;

            return HslaToColor(a, h, s, l);
        }

        if (colorString.StartsWith("rgba("))
        {
            string[] segments = colorString.Replace('%', '\0').Split(',', '(', ')');

            double r = double.Parse(segments[1], culture);
            double g = double.Parse(segments[2], culture);
            double b = double.Parse(segments[3], culture);
            double a = double.Parse(segments[4], culture) * 255;

            return SKColorFactory.MakeColor((byte)r, (byte)g, (byte)b, (byte)a);
        }

        if (colorString.StartsWith("rgb("))
        {
            string[] segments = colorString.Replace('%', '\0').Split(',', '(', ')');
            double r = double.Parse(segments[1], culture);
            double g = double.Parse(segments[2], culture);
            double b = double.Parse(segments[3], culture);

            return SKColorFactory.MakeColor((byte)r, (byte)g, (byte)b, 255);
        }

        try
        {
            return SKColorFactory.LogColor(ConvertFromString(colorString));
        }
        catch (Exception e)
        {
            log.Error(e);
            throw new VectorStyleException("Not implemented color format: " + colorString);
        }
    }

    private static SKColor ConvertFromString(string value)
    {
        if (null == value)
        {
            return SKColors.Transparent;
        }

        return KnownColors.ParseColor(value);
    }

    public bool ValidateLayer(Layer layer, double zoom, Dictionary<string, object> attributes)
    {
        if (layer.MinZoom != null && zoom < layer.MinZoom.Value)
        {
            return false;
        }

        if (layer.MaxZoom != null && zoom > layer.MaxZoom.Value)
        {
            return false;
        }

        if (attributes != null && layer.Filter.Any() && !ValidateUsingFilter(layer.Filter, attributes))
        {
            // make this more performant
            return false;
        }

        return true;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "<Pending>")]
    protected Layer[] FindLayers(double zoom, string layerName, Dictionary<string, object> attributes)
    {
        List<Layer> result = new List<Layer>();

        foreach (var layer in Layers)
        {
            //if (attributes.ContainsKey("class"))
            //{
            //    if (id == "highway-trunk" && (string)attributes["class"] == "primary")
            //    {

            //    }
            //}

            if (layer.SourceLayer == layerName)
            {
                bool valid = !(layer.Filter.Any() && !ValidateUsingFilter(layer.Filter, attributes));

                if (layer.MinZoom != null && zoom < layer.MinZoom.Value)
                {
                    valid = false;
                }

                if (layer.MaxZoom != null && zoom > layer.MaxZoom.Value)
                {
                    valid = false;
                }

                if (valid)
                {
                    result.Add(layer);
                }
            }
        }

        return result.ToArray();
    }

    private bool ValidateUsingFilter(object[] filterArray, Dictionary<string, object> attributes)
    {
        if (filterArray.Length == 0)
        {
            Console.WriteLine("nothing");
        }

        string operation = filterArray[0] as string;

        if (operation == "all")
        {
            foreach (object[] subFilter in filterArray.Skip(1))
            {
                if (!ValidateUsingFilter(subFilter, attributes))
                {
                    return false;
                }
            }

            return true;
        }

        if (operation == "any")
        {
            foreach (object[] subFilter in filterArray.Skip(1))
            {
                if (ValidateUsingFilter(subFilter, attributes))
                {
                    return true;
                }
            }

            return false;
        }

        if (operation == "none")
        {
            bool result = false;
            foreach (object[] subFilter in filterArray.Skip(1))
            {
                if (ValidateUsingFilter(subFilter, attributes))
                {
                    result = true;
                }
            }

            return !result;
        }

        switch (operation)
        {
            case "==":
            case "!=":
            case ">":
            case ">=":
            case "<":
            case "<=":

                string key = (string)filterArray[1];

                if (operation == "==")
                {
                    if (!attributes.ContainsKey(key))
                    {
                        return false;
                    }
                }
                else
                {
                    // special case, comparing inequality with non existent attribute
                    if (!attributes.ContainsKey(key))
                    {
                        return true;
                    }
                }

                if (!(attributes[key] is IComparable))
                {
                    throw new VectorStyleException("Comparing colors probably failed");
                }

                var valueA = (IComparable)attributes[key];
                object valueB = GetValue(filterArray[2], attributes);

                if (IsNumber(valueA) && IsNumber(valueB))
                {
                    valueA = Convert.ToDouble(valueA);
                    valueB = Convert.ToDouble(valueB);
                }

                if (key is "capital")
                {
                    log.Debug("capital");
                }

                if (valueA.GetType() != valueB.GetType())
                {
                    return false;
                }

                int comparison = valueA.CompareTo(valueB);

                if (operation == "==")
                {
                    return comparison == 0;
                }

                if (operation == "!=")
                {
                    return comparison != 0;
                }

                if (operation == ">")
                {
                    return comparison > 0;
                }

                if (operation == "<")
                {
                    return comparison < 0;
                }

                if (operation == ">=")
                {
                    return comparison >= 0;
                }

                if (operation == "<=")
                {
                    return comparison <= 0;
                }

                break;
        }

        if (operation == "has")
        {
            return attributes.ContainsKey((string)filterArray[1]);
        }

        if (operation == "!has")
        {
            return !attributes.ContainsKey((string)filterArray[1]);
        }


        if (operation == "in")
        {
            string key = (string)filterArray[1];
            if (!attributes.TryGetValue(key, out object value))
            {
                return false;
            }

#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions
            foreach (object item in filterArray.Skip(2))
            {
                if (GetValue(item, attributes).Equals(value))
                {
                    return true;
                }
            }
#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
            return false;
        }

        if (operation == "!in")
        {
            string key = (string)filterArray[1];
            if (!attributes.TryGetValue(key, out object value))
            {
                return true;
            }

#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions
            foreach (object item in filterArray.Skip(2))
            {
                if (GetValue(item, attributes).Equals(value))
                {
                    return false;
                }
            }
#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
            return true;
        }

        return false;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "<Pending>")]
    private object GetValue(object token, Dictionary<string, object> attributes = null)
    {
        if (token is string value && attributes != null)
        {
            if (value.Length == 0)
            {
                return "";
            }

            if (value[0] == '$')
            {
                return GetValue(attributes[value]);
            }
        }

        if (token.GetType().IsArray)
        {
            object[] array = token as object[];
            //List<object> result = new List<object>();

            //foreach (object item in array)
            //{
            //    var obj = getValue(item, attributes);
            //    result.Add(obj);
            //}

            //return result.ToArray();

            return array.Select(item => GetValue(item, attributes)).ToArray();
        }

        if (token is Dictionary<string, object> dict && dict.TryGetValue("stops", out object stopsValue) && stopsValue is object[] stops)
        {
            // if it has stops, it's interpolation domain now :P
            //var pointStops = stops.Select(item => new Tuple<double, JToken>((item as JArray)[0].Value<double>(), (item as JArray)[1])).ToList();
            var pointStops = stops.Select(item => new Tuple<double, object>(Convert.ToDouble((item as object[])[0]), (item as object[])[1])).ToList();

            double zoom = (double)attributes["$zoom"];
            double minZoom = pointStops.First().Item1;
            double maxZoom = pointStops.Last().Item1;
            double power = 1;

            double zoomA = minZoom;
            double zoomB = maxZoom;
            int zoomAIndex = 0;
            int zoomBIndex = pointStops.Count - 1;

            // get min max zoom bounds from array
            if (zoom <= minZoom)
            {
                //zoomA = minZoom;
                //zoomB = pointStops[1].Item1;
                return pointStops.First().Item2;
            }

            if (zoom >= maxZoom)
            {
                //zoomA = pointStops[pointStops.Count - 2].Item1;
                //zoomB = maxZoom;
                return pointStops.Last().Item2;
            }

            // checking for consecutive values
            for (int i = 1; i < pointStops.Count; i++)
            {
                double previousZoom = pointStops[i - 1].Item1;
                double thisZoom = pointStops[i].Item1;

                if (zoom >= previousZoom && zoom <= thisZoom)
                {
                    zoomA = previousZoom;
                    zoomB = thisZoom;

                    zoomAIndex = i - 1;
                    zoomBIndex = i;
                    break;
                }
            }


            if (dict.TryGetValue("base", out object value1))
            {
                power = Convert.ToDouble(GetValue(value1, attributes));
            }

            //var referenceElement = (stops[0] as object[])[1];

            return InterpolateValues(pointStops[zoomAIndex].Item2, pointStops[zoomBIndex].Item2, zoomA, zoomB, zoom, power, false);
        }

        return token;
    }

    private bool IsNumber(object value) => value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private object InterpolateValues(object startValue, object endValue, double zoomA, double zoomB, double zoom, double power, bool clamp = false)
    {
        if (startValue is string s)
        {
            // implement color mappings
            //var minValue = parseColor(startValue.Value<string>());
            //var maxValue = parseColor(endValue.Value<string>());


            //var newR = convertRange(zoom, zoomA, zoomB, minValue.ScR, maxValue.ScR, power, false);
            //var newG = convertRange(zoom, zoomA, zoomB, minValue.ScG, maxValue.ScG, power, false);
            //var newB = convertRange(zoom, zoomA, zoomB, minValue.ScB, maxValue.ScB, power, false);
            //var newA = convertRange(zoom, zoomA, zoomB, minValue.ScA, maxValue.ScA, power, false);

            //return Color.FromScRgb((float)newA, (float)newR, (float)newG, (float)newB);

            string minValue = s;
            string maxValue = endValue as string;

            if (Math.Abs(zoomA - zoom) <= Math.Abs(zoomB - zoom))
            {
                return minValue;
            }

            return maxValue;
        }

        if (startValue.GetType().IsArray)
        {
            List<object> result = new List<object>();
            object[] startArray = startValue as object[];
            object[] endArray = endValue as object[];

            for (int i = 0; i < startArray.Count(); i++)
            {
                object minValue = startArray[i];
                object maxValue = endArray[i];

                object value = InterpolateValues(minValue, maxValue, zoomA, zoomB, zoom, power, clamp);

                result.Add(value);
            }

            return result.ToArray();
        }

        if (IsNumber(startValue))
        {
            double minValue = Convert.ToDouble(startValue);
            double maxValue = Convert.ToDouble(endValue);

            return InterpolateRange(zoom, zoomA, zoomB, minValue, maxValue, power, clamp);
        }

        throw new VectorStyleException("Unimplemented interpolation");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1172:Unused method parameters should be removed", Justification = "<Pending>")]
    private double InterpolateRange(double oldValue, double oldMin, double oldMax, double newMin, double newMax, double power, bool clamp = false)
    {
        double difference = oldMax - oldMin;
        double progress = oldValue - oldMin;

        double normalized = 0;

        if (difference == 0)
        {
            normalized = 0;
        }
        else if (power == 1)
        {
            normalized = progress / difference;
        }
        else
        {
            normalized = (Math.Pow(power, progress) - 1f) / (Math.Pow(power, difference) - 1f);
        }

        double result = (normalized * (newMax - newMin)) + newMin;


        return result;
    }
}
