using System.Globalization;
using SkiaSharp;
using VexTile.Common.Extensions;
using VexTile.Common.Styles;

namespace VexTile.Common.Drawing;

public static class ColorFactory
{
    public static SKColor ParseColor(object iColor)
    {
        var culture = new CultureInfo("en-US", true);

        if (iColor is System.Drawing.Color color)
        {
            return MakeColor(color.R, color.G, color.B, color.A);
        }

        if (iColor is SKColor skColor)
        {
            return LogColor(skColor);
        }

        string colorString = (string)iColor;

        if (colorString[0] == '#')
        {
            return LogColor(SKColor.Parse(colorString));
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

            return MakeColor((byte)r, (byte)g, (byte)b, (byte)a);
        }

        if (colorString.StartsWith("rgb("))
        {
            string[] segments = colorString.Replace('%', '\0').Split(',', '(', ')');
            double r = double.Parse(segments[1], culture);
            double g = double.Parse(segments[2], culture);
            double b = double.Parse(segments[3], culture);

            return MakeColor((byte)r, (byte)g, (byte)b, 255);
        }

        try
        {
            return LogColor(ConvertFromString(colorString));
        }
        catch (Exception e)
        {
            throw new VectorStyleException("Not implemented color format: " + colorString);
        }
    }

    private static SKColor ConvertFromString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return SKColors.Transparent;
        }

        return KnownColors.ParseColor(value);
    }
    
    private static readonly Dictionary<string, SKColor> _colours = new();

    // try to centralise this as tracking down where colours ar made is hard
    public static SKColor MakeColor(byte red, byte green, byte blue, byte alpha = 255)
    {
        string key = MakeKey(red, green, blue, alpha);

        if (!_colours.TryGetValue(key, out SKColor color))
        {
            color = new SKColor(red, green, blue, alpha);
            _colours.Add(key, color);
        }

        return color;
    }

    private static string MakeKey(byte red, byte green, byte blue, byte alpha)
    {
        string key = $"{red:x2}{green:x2}{blue:x2}{alpha:x2}";
        return key;
    }

    // make logging colors simpler
    public static SKColor LogColor(SKColor color)
    {
        string key = MakeKey(color.Red, color.Green, color.Blue, color.Alpha);

        if (!_colours.TryGetValue(key, out _))
        {
            _colours.Add(key, color);
        }

        return color;
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

        return MakeColor(r, g, b, a);
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
}
