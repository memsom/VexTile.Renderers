using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace VexTile.Renderer.Mvt.AliFlux;

// ReSharper disable once InconsistentNaming
public static class SKColorFactory
{
    private static readonly Dictionary<string, SKColor> Colours = new();

    // try to centralise this as tracking down where colours ar made is hard
    public static SKColor MakeColor(byte red, byte green, byte blue, byte alpha = 255, [CallerMemberName] string callerName = "<unknown>")
    {
        string key = MakeKey(red, green, blue, alpha);

        if (!Colours.TryGetValue(key, out SKColor color))
        {
            color = new SKColor(red, green, blue, alpha);
            Colours[key] = color;

#if DEBUG_COLORS
            log.Debug($"{callerName} -> Created {key} :: SKColorFactory.MakeColor({red}, {green}, {blue}, {alpha})");
#endif
        }
        else
        {
#if DEBUG_COLORS
            log.Debug($"{callerName} -> Got {key} :: SKColorFactory.MakeColor");
#endif
        }

        return color;
    }

    private static string MakeKey(byte red, byte green, byte blue, byte alpha)
    {
        string key = $"{red:x2}{green:x2}{blue:x2}{alpha:x2}";
        return key;
    }

    // make logging colors simpler
    public static SKColor LogColor(SKColor color, [CallerMemberName] string callerName = "<unknown>")
    {
        string key = MakeKey(color.Red, color.Green, color.Blue, color.Alpha);

        if (!Colours.TryGetValue(key, out _))
        {
            Colours.Add(key, color);
        }
        return color;
    }
}
