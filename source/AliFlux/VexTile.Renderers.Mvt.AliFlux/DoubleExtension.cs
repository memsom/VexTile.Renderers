using System;

namespace VexTile.Renderer.Mvt.AliFlux;

internal static class DoubleExtension
{
    //private const double DefaultPrecision = 0.0001;

    internal static bool BasicallyEqualTo(this double a, double b) => a.BasicallyEqualTo(b, 0.0001);

    private static bool BasicallyEqualTo(this double a, double b, double precision) => Math.Abs(a - b) <= precision;
}
