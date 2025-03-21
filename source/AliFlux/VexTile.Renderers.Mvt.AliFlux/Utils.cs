using System;
using System.Security.Cryptography;
using System.Text;

namespace VexTile.Renderer.Mvt.AliFlux;

internal static class Utils
{
    public static double ConvertRange(double oldValue, double oldMin, double oldMax, double newMin, double newMax, bool clamp = false)
    {
        double NewRange;
        double NewValue;
        double OldRange = (oldMax - oldMin);
        if (OldRange == 0)
        {
            NewValue = newMin;
        }
        else
        {
            NewRange = (newMax - newMin);
            NewValue = (((oldValue - oldMin) * NewRange) / OldRange) + newMin;
        }

        if (clamp)
        {
            NewValue = Math.Min(Math.Max(NewValue, newMin), newMax);
        }

        return NewValue;
    }

    public static string Sha256(string randomString)
    {
        var crypt = SHA256.Create();
        var hash = new StringBuilder();
        byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));
        foreach (byte theByte in crypto)
        {
            hash.Append(theByte.ToString("x2"));
        }
        return hash.ToString();
    }
}
