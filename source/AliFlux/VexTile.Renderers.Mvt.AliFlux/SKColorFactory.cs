using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace VexTile.Renderer.Mvt.AliFlux;

// ReSharper disable once InconsistentNaming
public static class SKColorFactory
{
    // Use packed ARGB (0xAARRGGBB) as the key to avoid string allocations
    private static readonly ConcurrentDictionary<uint, SKColor> Colours = new();

    // try to centralise this as tracking down where colours ar made is hard
    public static SKColor MakeColor(byte red, byte green, byte blue, byte alpha = 255, [CallerMemberName] string callerName = "<unknown>")
    {
        uint key = MakeKey(red, green, blue, alpha);

        var color = Colours.GetOrAdd(key, _ => new SKColor(red, green, blue, alpha));

#if DEBUG_COLORS
        var hex = MakeKeyHex(red, green, blue, alpha); // RRGGBBAA for readability
        log.Debug($"{callerName} -> GetOrAdd {hex} :: SKColorFactory.MakeColor({red}, {green}, {blue}, {alpha})");
#endif

        return color;
    }

    private static uint MakeKey(byte red, byte green, byte blue, byte alpha)
    {
        // ARGB packed to match SKColor's internal layout
        return ((uint)alpha << 24) | ((uint)red << 16) | ((uint)green << 8) | blue;
    }

#if DEBUG_COLORS
    // Keep previous readable logging format (RRGGBBAA)
    private static string MakeKeyHex(byte red, byte green, byte blue, byte alpha)
    {
        return $"{red:x2}{green:x2}{blue:x2}{alpha:x2}";
    }
#endif

    // Make logging colors simpler
    public static SKColor LogColor(SKColor color, [CallerMemberName] string callerName = "<unknown>")
    {
        // Use the packed ARGB from the color directly
        uint key = (uint)color;

        Colours[key] = color;

#if DEBUG_COLORS
        var hex = MakeKeyHex(color.Red, color.Green, color.Blue, color.Alpha);
        log.Debug($"{callerName} -> Log {hex} :: SKColorFactory.LogColor");
#endif

        return color;
    }
}
