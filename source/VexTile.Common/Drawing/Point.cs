using System.Globalization;

namespace VexTile.Common.Drawing;

public struct Point(double x, double y)
{
    public double X { get; set; } = x;
    public double Y { get; set; } = y;

    public override bool Equals(object? obj)
    {
        if (obj is Point p)
        {
            return this == p;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() ^ (Y.GetHashCode() * 397);
    }

    public override string ToString() =>
        $"{{X={X.ToString(CultureInfo.InvariantCulture)} Y={Y.ToString(CultureInfo.InvariantCulture)}}}";

    public static bool operator ==(Point p1, Point p2) => (p1.X == p2.X) && (p1.Y == p2.Y);

    public static bool operator !=(Point p1, Point p2) => (p1.X != p2.X) || (p1.Y != p2.Y);

    public double Distance(Point other)
    {
        return Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));
    }

    public Point Offset(double dx, double dy)
    {
        Point p = this;
        p.X += dx;
        p.Y += dy;
        return p;
    }

    public Point Round()
    {
        return new Point(Math.Round(X), Math.Round(Y));
    }

    public bool IsEmpty => (X == 0) && (Y == 0);

    public static explicit operator Size(Point pt)
    {
        return new Size(pt.X, pt.Y);
    }
}
