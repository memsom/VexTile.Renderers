using System;

namespace VexTile.Renderer.Mvt.AliFlux.GlobalMercator;

public class GlobalMercatorImplementation
{
    private readonly int tileSize;
    private readonly double initialResolution;
    private readonly double originShift;


    public GlobalMercatorImplementation()
    {
        tileSize = 256;
        initialResolution = 2 * Math.PI * 6378137 / tileSize;
        originShift = 2 * Math.PI * 6378137 / 2.0;
    }

    public CoordinatePair LatLonToMeters(double lat, double lon)
    {
        var retval = new CoordinatePair
        {
            X = lon * originShift / 180.0,
            Y = Math.Log(Math.Tan((90 + lat) * Math.PI / 360.0)) / (Math.PI / 180.0)
        };

        retval.Y *= originShift / 180.0;
        return retval;
    }

    public CoordinatePair MetersToLatLon(double mx, double my)
    {
        var retval = new CoordinatePair
        {
            X = (mx / originShift) * 180.0,
            Y = (my / originShift) * 180.0
        };

        retval.Y = 180 / Math.PI * (2 * Math.Atan(Math.Exp(retval.Y * Math.PI / 180.0)) - Math.PI / 2.0);
        return retval;
    }

    public CoordinatePair PixelsToMeters(double px, double py, int zoom)
    {
        var res = Resolution(zoom);

        return new CoordinatePair
        {
            X = px * res - originShift,
            Y = py * res - originShift
        };
    }

    public CoordinatePair MetersToPixels(double mx, double my, int zoom)
    {
        var res = Resolution(zoom);

        return new CoordinatePair
        {
            X = (mx + originShift) / res,
            Y = (my + originShift) / res
        };
    }

    public TileAddress PixelsToTile(double px, double py)
    {
        return new TileAddress
        {
            X = (int)(Math.Ceiling(Convert.ToDouble(px / tileSize)) - 1),
            Y = (int)(Math.Ceiling(Convert.ToDouble(py / tileSize)) - 1)
        };
    }

    public TileAddress MetersToTile(double mx, double my, int zoom)
    {
        var p = MetersToPixels(mx, my, zoom);
        return PixelsToTile(p.X, p.Y);
    }

    public TileAddress LatLonToTile(double lat, double lon, int zoom)
    {
        var m = LatLonToMeters(lat, lon);
        return MetersToTile(m.X, m.Y, zoom);
    }

    public TileAddress LatLonToTileXYZ(double lat, double lon, int zoom)
    {
        var m = LatLonToMeters(lat, lon);
        var retval = MetersToTile(m.X, m.Y, zoom);
        retval.Y = (int)Math.Pow(2, zoom) - retval.Y - 1;
        return retval;
    }

    public GeoExtent TileBounds(int tx, int ty, int zoom)
    {
        var min = PixelsToMeters(tx * tileSize, ty * tileSize, zoom);
        var max = PixelsToMeters((tx + 1) * tileSize, (ty + 1) * tileSize, zoom);
        return new GeoExtent() { North = max.Y, South = min.Y, East = max.X, West = min.X };
    }

    public GeoExtent TileLatLonBounds(int tx, int ty, int zoom)
    {
        var bounds = TileBounds(tx, ty, zoom);
        var min = MetersToLatLon(bounds.West, bounds.South);
        var max = MetersToLatLon(bounds.East, bounds.North);
        return new GeoExtent() { North = max.Y, South = min.Y, East = max.X, West = min.X };
    }

    public TileAddress GoogleTile(int tx, int ty, int zoom)
    {
        var retval = new TileAddress
        {
            X = tx,
            Y = Convert.ToInt32((Math.Pow(2, zoom) - 1) - ty)
        };
        return retval;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1643:Strings should not be concatenated using '+' in a loop", Justification = "<Pending>")]
    public string QuadTree(int tx, int ty, int zoom)
    {
        var retval = string.Empty;
        ty = ((1 << zoom) - 1) - ty;
        for (var i = zoom; i >= 1; i--)
        {
            var digit = 0;

            var mask = 1 << (i - 1);

            if ((tx & mask) != 0)
            {
                digit += 1;
            }

            if ((ty & mask) != 0)
            {
                digit += 2;
            }

            retval += digit;
        }

        return retval;
    }

    public TileAddress QuadTreeToTile(string quadtree, int zoom)
    {
        var tx = 0;
        var ty = 0;

        for (var i = zoom; i >= 1; i--)
        {
            var ch = quadtree[zoom - i];
            var mask = 1 << (i - 1);

            var digit = ch - '0';

            if (Convert.ToBoolean(digit & 1)) tx += mask;

            if (Convert.ToBoolean(digit & 2)) ty += mask;
        }

        ty = ((1 << zoom) - 1) - ty;

        return new TileAddress
        {
            X = tx,
            Y = ty
        };
    }

    public string LatLonToQuadTree(double lat, double lon, int zoom)
    {
        var m = LatLonToMeters(lat, lon);
        var t = MetersToTile(m.X, m.Y, zoom);

        return QuadTree(Convert.ToInt32(t.X), Convert.ToInt32(t.Y), zoom);
    }

    private double Resolution(int zoom) { return initialResolution / (1 << zoom); }
}