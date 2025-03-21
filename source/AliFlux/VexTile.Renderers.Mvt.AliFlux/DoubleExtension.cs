using System;

namespace VexTile.Renderer.Mvt.AliFlux;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "<Pending>")]
internal static class DoubleExtension
{
    //private const double DefaultPrecision = 0.0001;

    internal static bool BasicallyEqualTo(this double a, double b)
    {
        return a.BasicallyEqualTo(b, 0.0001);
    }

    private static bool BasicallyEqualTo(this double a, double b, double precision)
    {
        return Math.Abs(a - b) <= precision;
    }
}
