using System.Globalization;

namespace VexTile.Common.Drawing;

public struct Size
{
    private double width;
    private double height;

    public static readonly Size Zero;

    public Size(double width, double height)
    {
        if (double.IsNaN(width))
        {
            throw new ArgumentException("NaN is not a valid value for width");
        }

        if (double.IsNaN(height))
        {
            throw new ArgumentException("NaN is not a valid value for height");
        }

        this.width = width;
        this.height = height;
    }

    public bool IsZero => (width == 0) && (height == 0);

    public double Width
    {
        get => width;
        set
        {
            if (double.IsNaN(value))
            {
                throw new ArgumentException("NaN is not a valid value for Width");
            }

            width = value;
        }
    }

    public double Height
    {
        get => height;
        set
        {
            if (double.IsNaN(value))
            {
                throw new ArgumentException("NaN is not a valid value for Height");
            }

            height = value;
        }
    }

    public static Size operator +(Size s1, Size s2) =>
        new Size(s1.width + s2.width, s1.height + s2.height);


    public static Size operator -(Size s1, Size s2) =>
        new Size(s1.width - s2.width, s1.height - s2.height);


    public static Size operator *(Size s1, double value) =>
        new Size(s1.width * value, s1.height * value);


    public static bool operator ==(Size s1, Size s2) =>
        (s1.width == s2.width) && (s1.height == s2.height);


    public static bool operator !=(Size s1, Size s2) =>
        (s1.width != s2.width) || (s1.height != s2.height);


    public static explicit operator Point(Size size) =>
        new Point(size.Width, size.Height);


    public bool Equals(Size other) => width.Equals(other.width) && height.Equals(other.height);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        return obj is Size size && Equals(size);
    }
    
    public override int GetHashCode()
    {
        unchecked
        {
            return (width.GetHashCode() * 397) ^ height.GetHashCode();
        }
    }

    public override string ToString()
    {
        return $"{{Width={width.ToString(CultureInfo.InvariantCulture)} Height={height.ToString(CultureInfo.InvariantCulture)}}}";
    }

    public void Deconstruct(out double width, out double height)
    {
        width = Width;
        height = Height;
    }
}