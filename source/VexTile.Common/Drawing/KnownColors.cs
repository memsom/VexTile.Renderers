using SkiaSharp;

namespace VexTile.Common.Drawing;

internal static class KnownColors
{
    static KnownColors()
    {
        var type = typeof(SKColors);
        var fia = type.GetFields()
            .Where(x => x.FieldType == typeof(SKColor));

        foreach (var item in fia)
        {
            var colorValue = (SKColor)item.GetValue(null);
            uint coloruint = (uint)(colorValue.Alpha << 24 | colorValue.Red << 16 | colorValue.Green << 8 | colorValue.Blue);
            string aRgbString = $"#{coloruint,8:X8}";
            s_knownArgbColors[aRgbString] = colorValue;
        }
    }

    private static string MatchColor(string colorString, out bool isKnownColor, out bool isNumericColor, out bool isContextColor, out bool isScRgbColor)
    {
        string trimmedString = colorString.Trim();

        if (trimmedString.Length is 4 or 5 or 7 or 9 &&
            trimmedString[0] == '#')
        {
            isNumericColor = true;
            isScRgbColor = false;
            isKnownColor = false;
            isContextColor = false;
            return trimmedString;
        }

        isNumericColor = false;

        if (trimmedString.StartsWith("sc#", StringComparison.Ordinal))
        {
            isNumericColor = false;
            isScRgbColor = true;
            isKnownColor = false;
            isContextColor = false;
        }
        else
        {
            isScRgbColor = false;
        }

        if (trimmedString.StartsWith(SContextColor, StringComparison.OrdinalIgnoreCase))
        {
            isContextColor = true;
            isScRgbColor = false;
            isKnownColor = false;
            return trimmedString;
        }

        isContextColor = false;
        isKnownColor = true;

        return trimmedString;
    }

    internal static SKColor ParseColor(string color)
    {
        string trimmedColor = MatchColor(color, out bool isPossibleKnowColor, out bool isNumericColor, out bool isContextColor, out bool isScRgbColor);

        if (!isPossibleKnowColor &&
            !isNumericColor &&
            !isScRgbColor &&
            !isContextColor)
        {
            throw new FormatException("Bad colour format");
        }

        //Is it a number?
        if (isNumericColor)
        {
            return ParseHexColor(trimmedColor);
        }

        return ColorStringToKnownColor(trimmedColor);
    }

    private const int SZeroChar = '0';
    private const int SALower = 'a';
    private const int SAUpper = 'A';

    private static int ParseHexChar(char c)
    {
        int intChar = c;

        if (intChar is >= SZeroChar and <= SZeroChar + 9)
        {
            return intChar - SZeroChar;
        }

        if (intChar is >= SALower and <= SALower + 5)
        {
            return intChar - SALower + 10;
        }

        if (intChar is >= SAUpper and <= SAUpper + 5)
        {
            return intChar - SAUpper + 10;
        }
        throw new FormatException("Bad format");
    }

    private static SKColor ParseHexColor(string trimmedColor)
    {
        int a, r, g, b;
        a = 255;

        if (trimmedColor.Length > 7)
        {
            a = ParseHexChar(trimmedColor[1]) * 16 + ParseHexChar(trimmedColor[2]);
            r = ParseHexChar(trimmedColor[3]) * 16 + ParseHexChar(trimmedColor[4]);
            g = ParseHexChar(trimmedColor[5]) * 16 + ParseHexChar(trimmedColor[6]);
            b = ParseHexChar(trimmedColor[7]) * 16 + ParseHexChar(trimmedColor[8]);
        }
        else if (trimmedColor.Length > 5)
        {
            r = ParseHexChar(trimmedColor[1]) * 16 + ParseHexChar(trimmedColor[2]);
            g = ParseHexChar(trimmedColor[3]) * 16 + ParseHexChar(trimmedColor[4]);
            b = ParseHexChar(trimmedColor[5]) * 16 + ParseHexChar(trimmedColor[6]);
        }
        else if (trimmedColor.Length > 4)
        {
            a = ParseHexChar(trimmedColor[1]);
            a += a * 16;
            r = ParseHexChar(trimmedColor[2]);
            r += r * 16;
            g = ParseHexChar(trimmedColor[3]);
            g += g * 16;
            b = ParseHexChar(trimmedColor[4]);
            b += b * 16;
        }
        else
        {
            r = ParseHexChar(trimmedColor[1]);
            r += r * 16;
            g = ParseHexChar(trimmedColor[2]);
            g += g * 16;
            b = ParseHexChar(trimmedColor[3]);
            b += b * 16;
        }

        return ColorFactory.MakeColor((byte)r, (byte)g, (byte)b, (byte)a);
    }

    private const string SContextColor = "ContextColor ";
    internal const string SContextColorNoSpace = "ContextColor";


    private static SKColor ColorStringToKnownColor(string colorString)
    {
        var color = InternalColorStringToKnownColor(colorString);
        return ColorFactory.LogColor(color);
    }

    /// Return the VTKnownColor from a color string.  If there's no match, VTKnownColor.UnknownColor
    private static SKColor InternalColorStringToKnownColor(string? colorString)
    {
        if (null != colorString)
        {
            // We use invariant culture because we don't globalize our color names
            string colorUpper = colorString.ToUpper(System.Globalization.CultureInfo.InvariantCulture);

            // Use String.Equals because it does explicit equality
            // StartsWith/EndsWith are culture sensitive and are 4-7 times slower than Equals

            switch (colorUpper.Length)
            {
                case 3:
                    if (colorUpper.Equals("RED")) return SKColors.Red;
                    if (colorUpper.Equals("TAN")) return SKColors.Tan;
                    break;
                case 4:
                    switch (colorUpper[0])
                    {
                        case 'A':
                            if (colorUpper.Equals("AQUA")) return SKColors.Aqua;
                            break;
                        case 'B':
                            if (colorUpper.Equals("BLUE")) return SKColors.Blue;
                            break;
                        case 'C':
                            if (colorUpper.Equals("CYAN")) return SKColors.Cyan;
                            break;
                        case 'G':
                            if (colorUpper.Equals("GOLD")) return SKColors.Gold;
                            if (colorUpper.Equals("GRAY")) return SKColors.Gray;
                            break;
                        case 'L':
                            if (colorUpper.Equals("LIME")) return SKColors.Lime;
                            break;
                        case 'N':
                            if (colorUpper.Equals("NAVY")) return SKColors.Navy;
                            break;
                        case 'P':
                            if (colorUpper.Equals("PERU")) return SKColors.Peru;
                            if (colorUpper.Equals("PINK")) return SKColors.Pink;
                            if (colorUpper.Equals("PLUM")) return SKColors.Plum;
                            break;
                        case 'S':
                            if (colorUpper.Equals("SNOW")) return SKColors.Snow;
                            break;
                        case 'T':
                            if (colorUpper.Equals("TEAL")) return SKColors.Teal;
                            break;
                    }
                    break;
                case 5:
                    switch (colorUpper[0])
                    {
                        case 'A':
                            if (colorUpper.Equals("AZURE")) return SKColors.Azure;
                            break;
                        case 'B':
                            if (colorUpper.Equals("BEIGE")) return SKColors.Beige;
                            if (colorUpper.Equals("BLACK")) return SKColors.Black;
                            if (colorUpper.Equals("BROWN")) return SKColors.Brown;
                            break;
                        case 'C':
                            if (colorUpper.Equals("CORAL")) return SKColors.Coral;
                            break;
                        case 'G':
                            if (colorUpper.Equals("GREEN")) return SKColors.Green;
                            break;
                        case 'I':
                            if (colorUpper.Equals("IVORY")) return SKColors.Ivory;
                            break;
                        case 'K':
                            if (colorUpper.Equals("KHAKI")) return SKColors.Khaki;
                            break;
                        case 'L':
                            if (colorUpper.Equals("LINEN")) return SKColors.Linen;
                            break;
                        case 'O':
                            if (colorUpper.Equals("OLIVE")) return SKColors.Olive;
                            break;
                        case 'W':
                            if (colorUpper.Equals("WHEAT")) return SKColors.Wheat;
                            if (colorUpper.Equals("WHITE")) return SKColors.White;
                            break;
                    }
                    break;
                case 6:
                    switch (colorUpper[0])
                    {
                        case 'B':
                            if (colorUpper.Equals("BISQUE")) return SKColors.Bisque;
                            break;
                        case 'I':
                            if (colorUpper.Equals("INDIGO")) return SKColors.Indigo;
                            break;
                        case 'M':
                            if (colorUpper.Equals("MAROON")) return SKColors.Maroon;
                            break;
                        case 'O':
                            if (colorUpper.Equals("ORANGE")) return SKColors.Orange;
                            if (colorUpper.Equals("ORCHID")) return SKColors.Orchid;
                            break;
                        case 'P':
                            if (colorUpper.Equals("PURPLE")) return SKColors.Purple;
                            break;
                        case 'S':
                            if (colorUpper.Equals("SALMON")) return SKColors.Salmon;
                            if (colorUpper.Equals("SIENNA")) return SKColors.Sienna;
                            if (colorUpper.Equals("SILVER")) return SKColors.Silver;
                            break;
                        case 'T':
                            if (colorUpper.Equals("TOMATO")) return SKColors.Tomato;
                            break;
                        case 'V':
                            if (colorUpper.Equals("VIOLET")) return SKColors.Violet;
                            break;
                        case 'Y':
                            if (colorUpper.Equals("YELLOW")) return SKColors.Yellow;
                            break;
                    }
                    break;
                case 7:
                    switch (colorUpper[0])
                    {
                        case 'C':
                            if (colorUpper.Equals("CRIMSON")) return SKColors.Crimson;
                            break;
                        case 'D':
                            if (colorUpper.Equals("DARKRED")) return SKColors.DarkRed;
                            if (colorUpper.Equals("DIMGRAY")) return SKColors.DimGray;
                            break;
                        case 'F':
                            if (colorUpper.Equals("FUCHSIA")) return SKColors.Fuchsia;
                            break;
                        case 'H':
                            if (colorUpper.Equals("HOTPINK")) return SKColors.HotPink;
                            break;
                        case 'M':
                            if (colorUpper.Equals("MAGENTA")) return SKColors.Magenta;
                            break;
                        case 'O':
                            if (colorUpper.Equals("OLDLACE")) return SKColors.OldLace;
                            break;
                        case 'S':
                            if (colorUpper.Equals("SKYBLUE")) return SKColors.SkyBlue;
                            break;
                        case 'T':
                            if (colorUpper.Equals("THISTLE")) return SKColors.Thistle;
                            break;
                    }
                    break;
                case 8:
                    switch (colorUpper[0])
                    {
                        case 'C':
                            if (colorUpper.Equals("CORNSILK")) return SKColors.Cornsilk;
                            break;
                        case 'D':
                            if (colorUpper.Equals("DARKBLUE")) return SKColors.DarkBlue;
                            if (colorUpper.Equals("DARKCYAN")) return SKColors.DarkCyan;
                            if (colorUpper.Equals("DARKGRAY")) return SKColors.DarkGray;
                            if (colorUpper.Equals("DEEPPINK")) return SKColors.DeepPink;
                            break;
                        case 'H':
                            if (colorUpper.Equals("HONEYDEW")) return SKColors.Honeydew;
                            break;
                        case 'L':
                            if (colorUpper.Equals("LAVENDER")) return SKColors.Lavender;
                            break;
                        case 'M':
                            if (colorUpper.Equals("MOCCASIN")) return SKColors.Moccasin;
                            break;
                        case 'S':
                            if (colorUpper.Equals("SEAGREEN")) return SKColors.SeaGreen;
                            if (colorUpper.Equals("SEASHELL")) return SKColors.SeaShell;
                            break;
                    }
                    break;
                case 9:
                    switch (colorUpper[0])
                    {
                        case 'A':
                            if (colorUpper.Equals("ALICEBLUE")) return SKColors.AliceBlue;
                            break;
                        case 'B':
                            if (colorUpper.Equals("BURLYWOOD")) return SKColors.BurlyWood;
                            break;
                        case 'C':
                            if (colorUpper.Equals("CADETBLUE")) return SKColors.CadetBlue;
                            if (colorUpper.Equals("CHOCOLATE")) return SKColors.Chocolate;
                            break;
                        case 'D':
                            if (colorUpper.Equals("DARKGREEN")) return SKColors.DarkGreen;
                            if (colorUpper.Equals("DARKKHAKI")) return SKColors.DarkKhaki;
                            break;
                        case 'F':
                            if (colorUpper.Equals("FIREBRICK")) return SKColors.Firebrick;
                            break;
                        case 'G':
                            if (colorUpper.Equals("GAINSBORO")) return SKColors.Gainsboro;
                            if (colorUpper.Equals("GOLDENROD")) return SKColors.Goldenrod;
                            break;
                        case 'I':
                            if (colorUpper.Equals("INDIANRED")) return SKColors.IndianRed;
                            break;
                        case 'L':
                            if (colorUpper.Equals("LAWNGREEN")) return SKColors.LawnGreen;
                            if (colorUpper.Equals("LIGHTBLUE")) return SKColors.LightBlue;
                            if (colorUpper.Equals("LIGHTCYAN")) return SKColors.LightCyan;
                            if (colorUpper.Equals("LIGHTGRAY")) return SKColors.LightGray;
                            if (colorUpper.Equals("LIGHTPINK")) return SKColors.LightPink;
                            if (colorUpper.Equals("LIMEGREEN")) return SKColors.LimeGreen;
                            break;
                        case 'M':
                            if (colorUpper.Equals("MINTCREAM")) return SKColors.MintCream;
                            if (colorUpper.Equals("MISTYROSE")) return SKColors.MistyRose;
                            break;
                        case 'O':
                            if (colorUpper.Equals("OLIVEDRAB")) return SKColors.OliveDrab;
                            if (colorUpper.Equals("ORANGERED")) return SKColors.OrangeRed;
                            break;
                        case 'P':
                            if (colorUpper.Equals("PALEGREEN")) return SKColors.PaleGreen;
                            if (colorUpper.Equals("PEACHPUFF")) return SKColors.PeachPuff;
                            break;
                        case 'R':
                            if (colorUpper.Equals("ROSYBROWN")) return SKColors.RosyBrown;
                            if (colorUpper.Equals("ROYALBLUE")) return SKColors.RoyalBlue;
                            break;
                        case 'S':
                            if (colorUpper.Equals("SLATEBLUE")) return SKColors.SlateBlue;
                            if (colorUpper.Equals("SLATEGRAY")) return SKColors.SlateGray;
                            if (colorUpper.Equals("STEELBLUE")) return SKColors.SteelBlue;
                            break;
                        case 'T':
                            if (colorUpper.Equals("TURQUOISE")) return SKColors.Turquoise;
                            break;
                    }
                    break;
                case 10:
                    switch (colorUpper[0])
                    {
                        case 'A':
                            if (colorUpper.Equals("AQUAMARINE")) return SKColors.Aquamarine;
                            break;
                        case 'B':
                            if (colorUpper.Equals("BLUEVIOLET")) return SKColors.BlueViolet;
                            break;
                        case 'C':
                            if (colorUpper.Equals("CHARTREUSE")) return SKColors.Chartreuse;
                            break;
                        case 'D':
                            if (colorUpper.Equals("DARKORANGE")) return SKColors.DarkOrange;
                            if (colorUpper.Equals("DARKORCHID")) return SKColors.DarkOrchid;
                            if (colorUpper.Equals("DARKSALMON")) return SKColors.DarkSalmon;
                            if (colorUpper.Equals("DARKVIOLET")) return SKColors.DarkViolet;
                            if (colorUpper.Equals("DODGERBLUE")) return SKColors.DodgerBlue;
                            break;
                        case 'G':
                            if (colorUpper.Equals("GHOSTWHITE")) return SKColors.GhostWhite;
                            break;
                        case 'L':
                            if (colorUpper.Equals("LIGHTCORAL")) return SKColors.LightCoral;
                            if (colorUpper.Equals("LIGHTGREEN")) return SKColors.LightGreen;
                            break;
                        case 'M':
                            if (colorUpper.Equals("MEDIUMBLUE")) return SKColors.MediumBlue;
                            break;
                        case 'P':
                            if (colorUpper.Equals("PAPAYAWHIP")) return SKColors.PapayaWhip;
                            if (colorUpper.Equals("POWDERBLUE")) return SKColors.PowderBlue;
                            break;
                        case 'S':
                            if (colorUpper.Equals("SANDYBROWN")) return SKColors.SandyBrown;
                            break;
                        case 'W':
                            if (colorUpper.Equals("WHITESMOKE")) return SKColors.WhiteSmoke;
                            break;
                    }
                    break;
                case 11:
                    switch (colorUpper[0])
                    {
                        case 'D':
                            if (colorUpper.Equals("DARKMAGENTA")) return SKColors.DarkMagenta;
                            if (colorUpper.Equals("DEEPSKYBLUE")) return SKColors.DeepSkyBlue;
                            break;
                        case 'F':
                            if (colorUpper.Equals("FLORALWHITE")) return SKColors.FloralWhite;
                            if (colorUpper.Equals("FORESTGREEN")) return SKColors.ForestGreen;
                            break;
                        case 'G':
                            if (colorUpper.Equals("GREENYELLOW")) return SKColors.GreenYellow;
                            break;
                        case 'L':
                            if (colorUpper.Equals("LIGHTSALMON")) return SKColors.LightSalmon;
                            if (colorUpper.Equals("LIGHTYELLOW")) return SKColors.LightYellow;
                            break;
                        case 'N':
                            if (colorUpper.Equals("NAVAJOWHITE")) return SKColors.NavajoWhite;
                            break;
                        case 'S':
                            if (colorUpper.Equals("SADDLEBROWN")) return SKColors.SaddleBrown;
                            if (colorUpper.Equals("SPRINGGREEN")) return SKColors.SpringGreen;
                            break;
                        case 'T':
                            if (colorUpper.Equals("TRANSPARENT")) return SKColors.Transparent;
                            break;
                        case 'Y':
                            if (colorUpper.Equals("YELLOWGREEN")) return SKColors.YellowGreen;
                            break;
                    }
                    break;
                case 12:
                    switch (colorUpper[0])
                    {
                        case 'A':
                            if (colorUpper.Equals("ANTIQUEWHITE")) return SKColors.AntiqueWhite;
                            break;
                        case 'D':
                            if (colorUpper.Equals("DARKSEAGREEN")) return SKColors.DarkSeaGreen;
                            break;
                        case 'L':
                            if (colorUpper.Equals("LIGHTSKYBLUE")) return SKColors.LightSkyBlue;
                            if (colorUpper.Equals("LEMONCHIFFON")) return SKColors.LemonChiffon;
                            break;
                        case 'M':
                            if (colorUpper.Equals("MEDIUMORCHID")) return SKColors.MediumOrchid;
                            if (colorUpper.Equals("MEDIUMPURPLE")) return SKColors.MediumPurple;
                            if (colorUpper.Equals("MIDNIGHTBLUE")) return SKColors.MidnightBlue;
                            break;
                    }
                    break;
                case 13:
                    switch (colorUpper[0])
                    {
                        case 'D':
                            if (colorUpper.Equals("DARKSLATEBLUE")) return SKColors.DarkSlateBlue;
                            if (colorUpper.Equals("DARKSLATEGRAY")) return SKColors.DarkSlateGray;
                            if (colorUpper.Equals("DARKGOLDENROD")) return SKColors.DarkGoldenrod;
                            if (colorUpper.Equals("DARKTURQUOISE")) return SKColors.DarkTurquoise;
                            break;
                        case 'L':
                            if (colorUpper.Equals("LIGHTSEAGREEN")) return SKColors.LightSeaGreen;
                            if (colorUpper.Equals("LAVENDERBLUSH")) return SKColors.LavenderBlush;
                            break;
                        case 'P':
                            if (colorUpper.Equals("PALEGOLDENROD")) return SKColors.PaleGoldenrod;
                            if (colorUpper.Equals("PALETURQUOISE")) return SKColors.PaleTurquoise;
                            if (colorUpper.Equals("PALEVIOLETRED")) return SKColors.PaleVioletRed;
                            break;
                    }
                    break;
                case 14:
                    switch (colorUpper[0])
                    {
                        case 'B':
                            if (colorUpper.Equals("BLANCHEDALMOND")) return SKColors.BlanchedAlmond;
                            break;
                        case 'C':
                            if (colorUpper.Equals("CORNFLOWERBLUE")) return SKColors.CornflowerBlue;
                            break;
                        case 'D':
                            if (colorUpper.Equals("DARKOLIVEGREEN")) return SKColors.DarkOliveGreen;
                            break;
                        case 'L':
                            if (colorUpper.Equals("LIGHTSLATEGRAY")) return SKColors.LightSlateGray;
                            if (colorUpper.Equals("LIGHTSTEELBLUE")) return SKColors.LightSteelBlue;
                            break;
                        case 'M':
                            if (colorUpper.Equals("MEDIUMSEAGREEN")) return SKColors.MediumSeaGreen;
                            break;
                    }
                    break;
                case 15:
                    if (colorUpper.Equals("MEDIUMSLATEBLUE")) return SKColors.MediumSlateBlue;
                    if (colorUpper.Equals("MEDIUMTURQUOISE")) return SKColors.MediumTurquoise;
                    if (colorUpper.Equals("MEDIUMVIOLETRED")) return SKColors.MediumVioletRed;
                    break;
                case 16:
                    if (colorUpper.Equals("MEDIUMAQUAMARINE")) return SKColors.MediumAquamarine;
                    break;
                case 17:
                    if (colorUpper.Equals("MEDIUMSPRINGGREEN")) return SKColors.MediumSpringGreen;
                    break;
                case 20:
                    if (colorUpper.Equals("LIGHTGOLDENRODYELLOW")) return SKColors.LightGoldenrodYellow;
                    break;
            }
        }
        // colorString was null or not found
        return SKColors.Transparent;
    }

    internal static SKColor ArgbStringToKnownColor(string argbString)
    {
        string argbUpper = argbString.Trim().ToUpper(System.Globalization.CultureInfo.InvariantCulture);

        if (s_knownArgbColors.TryGetValue(argbUpper, out SKColor color))
        {
            return color;
        }

        return SKColors.Transparent;
    }

    //private static Dictionary<uint, SolidColorBrush> s_solidColorBrushCache = new Dictionary<uint, SolidColorBrush>();
    private static readonly Dictionary<string, SKColor> s_knownArgbColors = new();
}
