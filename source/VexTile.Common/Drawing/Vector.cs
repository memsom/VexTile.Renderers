namespace VexTile.Common.Drawing;

/// <summary>
/// based on Xamarin Forms
/// </summary>
public struct Vector(double x, double y)
{
    
    public Vector(Point p)
        : this(p.X, p.Y)
    {
    }

    public Vector(double angle)
        : this(Math.Cos(Math.PI * angle / 180), Math.Sin(Math.PI * angle / 180))
    {
    }

    public double X { private set; get; } = x;
    public double Y { private set; get; } = y;

    public double LengthSquared
    {
        get { return X * X + Y * Y; }
    }

    public double Length
    {
        get { return Math.Sqrt(LengthSquared); }
    }

    public Vector Normalized
    {
        get
        {
            double length = Length;

            if (length != 0)
            {
                return new Vector(X / length, Y / length);
            }
            return new Vector();
        }
    }

    public static double AngleBetween(Vector v1, Vector v2)
    {
        return 180 * (Math.Atan2(v2.Y, v2.X) - Math.Atan2(v1.Y, v1.X)) / Math.PI;
    }

    public static explicit operator Point(Vector v)
    {
        return new Point(v.X, v.Y);
    }

    public override string ToString()
    {
        return $"{X},{Y}";
    }
}