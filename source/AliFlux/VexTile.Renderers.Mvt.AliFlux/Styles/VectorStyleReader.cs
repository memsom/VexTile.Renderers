using System;
using System.IO;
using System.Linq;
using System.Reflection;
using VexTile.Common.Enums;

namespace VexTile.Renderer.Mvt.AliFlux.Styles;

public static class VectorStyleReader
{
    static readonly NLog.Logger log  = NLog.LogManager.GetCurrentClassLogger();

    private static string[] names;

    public static string GetStyle(VectorStyleKind styleKind)
    {
        string name = styleKind.ToString().ToLower();
        var assembly = Assembly.GetExecutingAssembly();
        string nsname = assembly.GetName().Name;
        string resourceName = $"{nsname}.Styles.{name}-style.json";
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null) throw new Common.Styles.VectorStyleException($"Could not find '{nsname}.Styles.{name}-style.json'");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static bool TryGetFont(string name, out Stream stream)
    {
        try
        {
            name = name.Replace(' ', '-'); // spaces to dashes
            var assembly = Assembly.GetExecutingAssembly();
            string nsname = assembly.GetName().Name;
            string resourceName = $"{nsname}.Styles.fonts.{name}";

            // init names
            if (names == null)
            {
                names = assembly.GetManifestResourceNames();
            }

            // get the name from the names list
            string realName = names?.FirstOrDefault(x => x.StartsWith(resourceName));

            if (!string.IsNullOrWhiteSpace(realName))
            {
                using Stream manifestStream = assembly.GetManifestResourceStream(realName);

                if (manifestStream is not null)
                {
                    stream = new MemoryStream();
                    manifestStream.CopyTo(stream);
                    stream.Seek(0, SeekOrigin.Begin); // make sure it is at stream start
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex);
        }

        stream = null;
        return false;
    }
}
