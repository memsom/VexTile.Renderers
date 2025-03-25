using System.Drawing;

namespace VexTile.Common.Drawing;

public struct Rect(double x, double y, double width, double height)
{
    public Rect(Point tl, Point br) : this(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y) { }

    public Rect(Point loc, Size sz) : this(loc.X, loc.Y, sz.Width, sz.Height) { }

    public double X { get; set; } = x;

    public double Y { get; set; } = y;

    public double Width { get; set; } = width;

    public double Height { get; set; } = height;

#pragma warning disable S2223 // Non-constant static fields should not be visible
    public static Rect Zero { get; } = new();
#pragma warning restore S2223 // Non-constant static fields should not be visible

    public double Top => Y;

    public double Bottom => Y + Height;

    public double Right => X + Width;

    public double Left => X;

    public bool IsEmpty => (Width <= 0) || (Height <= 0);

    public System.Drawing.Point Center => new((int)(X + Width / 2), (int)(Y + Height / 2));

    public System.Drawing.Size Size
    {
        get => new((int)Width, (int)Height);
        set
        {
            Width = value.Width;
            Height = value.Height;
        }
    }

    public System.Drawing.Point Location
    {
        get => new((int)X, (int)Y);
        set
        {
            X = value.X;
            Y = value.Y;
        }
    }

    public static Rect FromLTRB(double left, double top, double right, double bottom) => new(left, top, right - left, bottom - top);

    public bool Equals(Rect other) => X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);

    public override bool Equals(object obj)
    {
        if (obj is null) return false;

        return obj is Rect rect && Equals(rect) || obj is Rectangle rectangle && Equals(rectangle);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = X.GetHashCode();
            hashCode = (hashCode * 397) ^ Y.GetHashCode();
            hashCode = (hashCode * 397) ^ Width.GetHashCode();
            hashCode = (hashCode * 397) ^ Height.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(Rect r1, Rect r2) => (r1.Location == r2.Location) && (r1.Size == r2.Size);

    public static bool operator !=(Rect r1, Rect r2) => !(r1 == r2);

    // Hit Testing / Intersection / Union
    public bool Contains(Rect rect) => X <= rect.X && Right >= rect.Right && Y <= rect.Y && Bottom >= rect.Bottom;

    public bool Contains(System.Drawing.Point pt) => Contains(pt.X, pt.Y);

    public bool Contains(double x, double y) => (x >= Left) && (x < Right) && (y >= Top) && (y < Bottom);

    public bool IntersectsWith(Rect r) => !((Left >= r.Right) || (Right <= r.Left) || (Top >= r.Bottom) || (Bottom <= r.Top));

    public Rect Union(Rect r) => Union(this, r);

    public static Rect Union(Rect r1, Rect r2) => FromLTRB(Math.Min(r1.Left, r2.Left), Math.Min(r1.Top, r2.Top), Math.Max(r1.Right, r2.Right), Math.Max(r1.Bottom, r2.Bottom));

    public Rect Intersect(Rect r) => Intersect(this, r);

    public static Rect Intersect(Rect r1, Rect r2)
    {
        double x = Math.Max(r1.X, r2.X);
        double y = Math.Max(r1.Y, r2.Y);
        double width = Math.Min(r1.Right, r2.Right) - x;
        double height = Math.Min(r1.Bottom, r2.Bottom) - y;

        if (width < 0 || height < 0) return Zero;

        return new Rect(x, y, width, height);
    }

    // Inflate and Offset
    public Rect Inflate(System.Drawing.Size sz) => Inflate(sz.Width, sz.Height);

    public Rect Inflate(double width, double height)
    {
        Rect r = this;
        r.X -= width;
        r.Y -= height;
        r.Width += width * 2;
        r.Height += height * 2;
        return r;
    }

    public Rect Offset(double dx, double dy)
    {
        Rect r = this;
        r.X += dx;
        r.Y += dy;
        return r;
    }

    public Rect Offset(System.Drawing.Point dr) => Offset(dr.X, dr.Y);

    public Rect Round() => new(Math.Round(X), Math.Round(Y), Math.Round(Width), Math.Round(Height));

    public void Deconstruct(out double x, out double y, out double width, out double height)
    {
        x = X;
        y = Y;
        width = Width;
        height = Height;
    }
}