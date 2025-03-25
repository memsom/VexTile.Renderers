using System.Text.RegularExpressions;
using VexTile.Common.Enums;
using VexTile.Common.Styles;

namespace VexTile.Common.Drawing;

public class Layer
{
    public int Index { get; set; } = -1;
    public string ID { get; set; } = "";
    public string Type { get; set; } = "";
    public string SourceName { get; set; } = "";
    public Source Source { get; set; }
    public string SourceLayer { get; set; } = "";
    public Dictionary<string, object> Paint { get; set; } = new();
    public Dictionary<string, object> Layout { get; set; } = new();
    public object[] Filter { get; set; } = [];
    public double? MinZoom { get; set; }
    public double? MaxZoom { get; set; }

    Dictionary<double, Brush>  brushes = new();

    public Brush GetBrush(double scale, Dictionary<string, object> attributes)
    {
        // only recalculate this if we have no choice
        if (!brushes.TryGetValue(scale, out var brush))
        {
            var paintData = Paint;
            var layoutData = Layout;
            var index = Index;

            brush = new Brush
            {
                ZIndex = index,
                Layer = this,
                //GlyphsDirectory = this.FontDirectory
            };

            var paint = new Paint();
            brush.Paint = paint;

            if (ID == "country_label")
            {
                Console.WriteLine("country_label -> nothing");
            }

            if (paintData.TryGetValue("fill-color", out var fillColorValue) && ColorFactory.ParseColor(GetValue(fillColorValue, attributes)) is var fillColor)
            {
                paint.FillColor = fillColor;
            }

            if (paintData.TryGetValue("background-color", out var backgroundColorValue) && ColorFactory.ParseColor(GetValue(backgroundColorValue, attributes)) is var backgroundColor)
            {
                paint.BackgroundColor = backgroundColor;
            }

            if (paintData.TryGetValue("text-color", out var textColorValue) && ColorFactory.ParseColor(GetValue(textColorValue, attributes)) is var textColor)
            {
                paint.TextColor = textColor;
            }

            if (paintData.TryGetValue("line-color", out var lineColorValue) && ColorFactory.ParseColor(GetValue(lineColorValue, attributes)) is var lineColor)
            {
                paint.LineColor = lineColor;
            }

            // --

            if (paintData.TryGetValue("line-pattern", out var linePatternValue) && GetValue(linePatternValue, attributes) is string linePattern)
            {
                paint.LinePattern = linePattern;
            }

            if (paintData.TryGetValue("background-pattern", out var backgroundPatternValue) && GetValue(backgroundPatternValue, attributes) is string backgroundPattern)
            {
                paint.BackgroundPattern = backgroundPattern;
            }

            if (paintData.TryGetValue("fill-pattern", out var fillPatternValue) && GetValue(fillPatternValue, attributes) is string fillPattern)
            {
                paint.FillPattern = fillPattern;
            }

            // --

            if (paintData.TryGetValue("text-opacity", out var textOpacityValue) && GetValue(textOpacityValue, attributes) is string textOpacity)
            {
                paint.TextOpacity = Convert.ToDouble(textOpacity);
            }

            if (paintData.TryGetValue("icon-opacity", out var iconOpacityValue))
            {
                paint.IconOpacity = Convert.ToDouble(GetValue(iconOpacityValue, attributes));
            }

            if (paintData.TryGetValue("line-opacity", out var lineOpacityValue))
            {
                paint.LineOpacity = Convert.ToDouble(GetValue(lineOpacityValue, attributes));
            }

            if (paintData.TryGetValue("fill-opacity", out var fillOpacityValue))
            {
                paint.FillOpacity = Convert.ToDouble(GetValue(fillOpacityValue, attributes));
            }

            if (paintData.TryGetValue("background-opacity", out var backgroundOpacityValue))
            {
                paint.BackgroundOpacity = Convert.ToDouble(GetValue(backgroundOpacityValue, attributes));
            }

            // --

            if (paintData.TryGetValue("line-width", out var lineWidthValue))
            {
                paint.LineWidth = Convert.ToDouble(GetValue(lineWidthValue, attributes)) * scale; // * screenScale;
            }

            if (paintData.TryGetValue("line-offset", out var lineOffsetValue))
            {
                paint.LineOffset = Convert.ToDouble(GetValue(lineOffsetValue, attributes)) * scale; // * screenScale;
            }

            if (paintData.TryGetValue("line-dasharray", out var lineDashArrayValue) && GetValue(lineDashArrayValue, attributes) is object[] lineDashArray)
            {
                paint.LineDashArray = lineDashArray.Select(item => Convert.ToDouble(item) * scale).ToArray();
            }

            // --

            if (paintData.TryGetValue("text-halo-color", out var textHaloColorValue))
            {
                paint.TextStrokeColor = ColorFactory.ParseColor(GetValue(textHaloColorValue, attributes));
            }

            if (paintData.TryGetValue("text-halo-width", out var textHaloWidthValue))
            {
                paint.TextStrokeWidth = Convert.ToDouble(GetValue(textHaloWidthValue, attributes)) * scale;
            }

            if (paintData.TryGetValue("text-halo-blur", out var textHaloBlurValue) && GetValue(textHaloBlurValue, attributes) is string textHaloBlur)
            {
                paint.TextStrokeBlur = Convert.ToDouble(textHaloBlur) * scale;
            }


            if (layoutData.TryGetValue("line-cap", out var lineCapValue) && GetValue(lineCapValue, attributes) is string lineCap)
            {
                if (lineCap == "butt")
                {
                    paint.LineCap = PenLineCap.Flat;
                }
                else if (lineCap == "round")
                {
                    paint.LineCap = PenLineCap.Round;
                }
                else if (lineCap == "square")
                {
                    paint.LineCap = PenLineCap.Square;
                }
            }

            if (layoutData.TryGetValue("visibility", out var visibilityValue) && GetValue(visibilityValue, attributes) is string visibility)
            {
                paint.Visibility = visibility == "visible";
            }

            if (layoutData.TryGetValue("text-field", out var textFieldValue) && GetValue(textFieldValue, attributes) is string textField)
            {
                brush.TextField = textField;

                // check performance implications of Regex.Replace
                brush.Text = Regex.Replace(
                    input: brush.TextField,
                    pattern: @"\{([A-Za-z0-9\-\:_]+)\}",
                    evaluator: m =>
                    {
                        var key = StripBraces(m.Value);
                        if (attributes.TryGetValue(key, out var attribute) && attribute is string attributeValue)
                        {
                            return attributeValue;
                        }

                        return "";
                    }).Trim();
            }

            if (layoutData.TryGetValue("text-font", out var textFontValue) && GetValue(textFontValue, attributes) is object[] textFont)
            {
                paint.TextFont = (textFont).Select(item => (string)item).ToArray();
            }

            if (layoutData.TryGetValue("text-size", out var textSizeValue))
            {
                paint.TextSize = Convert.ToDouble(GetValue(textSizeValue, attributes)) * scale;
            }

            if (layoutData.TryGetValue("text-max-width", out var testMaxWidthValue) && GetValue(testMaxWidthValue, attributes) is string maxWidth)
            {
                paint.TextMaxWidth = Convert.ToDouble(maxWidth) * scale; // * screenScale;
            }

            if (layoutData.TryGetValue("text-offset", out var textOffsetValue) && GetValue(textOffsetValue, attributes) is object[] textOffsetValues)
            {
                paint.TextOffset = new Point(Convert.ToDouble(textOffsetValues[0]) * scale, Convert.ToDouble(textOffsetValues[1]) * scale);
            }

            if (layoutData.TryGetValue("text-optional", out var textOptionalValue) && GetValue(textOptionalValue, attributes) is bool optional)
            {
                paint.TextOptional = optional;
            }

            if (layoutData.TryGetValue("text-transform", out var textTransformValue) && GetValue(textTransformValue, attributes) is string transform)
            {
                if (transform == "none")
                {
                    paint.TextTransform = TextTransform.None;
                }
                else if (transform == "uppercase")
                {
                    paint.TextTransform = TextTransform.Uppercase;
                }
                else if (transform == "lowercase")
                {
                    paint.TextTransform = TextTransform.Lowercase;
                }
            }

            if (layoutData.TryGetValue("icon-size", out var iconSizeValue) && GetValue(iconSizeValue, attributes) is string iconSize)
            {
                paint.IconScale = Convert.ToDouble(iconSize) * scale;
            }

            if (layoutData.TryGetValue("icon-image", out var iconImageValue) && GetValue(iconImageValue, attributes) is string iconImge)
            {
                paint.IconImage = iconImge;
            }

            brushes.Add(scale, brush); //cache the brush
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

    private object GetValue(object token, Dictionary<string, object>? attributes = null)
    {
        if (token is string value && attributes is not null)
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

        if (token.GetType().IsArray && token is object[] array)
        {
            //List<object> result = new List<object>();

            //foreach (object item in array)
            //{
            //    var obj = getValue(item, attributes);
            //    result.Add(obj);
            //}

            //return result.ToArray();

            return array.Select(item => GetValue(item, attributes)).ToArray();
        }

        if (token is Dictionary<string, object> dict && dict.TryGetValue("stops", out object? stopsValue) && stopsValue is object[] stops)
        {
            // if it has stops, it's interpolation domain now :P
            //var pointStops = stops.Select(item => new Tuple<double, JToken>((item as JArray)[0].Value<double>(), (item as JArray)[1])).ToList();
            var pointStops = stops.Select(
                item =>
                    new Tuple<double, object>(
                        Convert.ToDouble((item as object[])[0]), (item as object[])[1])).ToList();

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


            if (dict.TryGetValue("base", out object? baseValue))
            {
                power = Convert.ToDouble(GetValue(baseValue, attributes));
            }

            //var referenceElement = (stops[0] as object[])[1];

            return InterpolateValues(pointStops[zoomAIndex].Item2, pointStops[zoomBIndex].Item2, zoomA, zoomB, zoom, power, false);
        }

        return token;
    }

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

    private bool IsNumber(object value) => value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

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
