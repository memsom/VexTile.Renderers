//#define USE_DEBUG_COLLISION

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SkiaSharp;
using VexTile.ClipperLib;
using VexTile.Renderer.Mvt.AliFlux.Drawing;
using VexTile.Renderer.Mvt.AliFlux.Enums;
using Point = VexTile.Renderer.Mvt.AliFlux.Drawing.Point;

namespace VexTile.Renderer.Mvt.AliFlux;

public class SkiaCanvas : ICanvas
{
    readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

    private int _width;
    private int _height;

    private SKSurface _surface;
    private SKCanvas _canvas;

    public bool ClipOverflow { get; set; } = false;
    private Rect _clipRectangle;
    private List<IntPoint> _clipRectanglePath;
    private readonly ConcurrentDictionary<string, SKTypeface> _fontPairs = new();
    private static readonly object s_fontLock = new();
    private readonly List<Rect> _textRectangles = new();

    public void StartDrawing(double width, double height)
    {
        _width = (int)width;
        _height = (int)height;

        var info = new SKImageInfo(_width, _height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

        _surface = SKSurface.Create(info);
        _canvas = _surface.Canvas;

        double padding = -5;
        _clipRectangle = new Rect(padding, padding, _width - padding * 2, _height - padding * 2);

        _clipRectanglePath = new List<IntPoint>
        {
            new((int)_clipRectangle.Top, (int)_clipRectangle.Left),
            new((int)_clipRectangle.Top, (int)_clipRectangle.Right),
            new((int)_clipRectangle.Bottom, (int)_clipRectangle.Right),
            new((int)_clipRectangle.Bottom, (int)_clipRectangle.Left)
        };
    }

    SKColor _backgroundColor = SKColors.White;

    public void DrawBackground(Brush style)
    {
        _backgroundColor = style.Paint.BackgroundColor; // we cache this
        _canvas.Clear(SKColorFactory.MakeColor(style.Paint.BackgroundColor.Red, style.Paint.BackgroundColor.Green, style.Paint.BackgroundColor.Blue, style.Paint.BackgroundColor.Alpha));
    }

    private SKStrokeCap ConvertCap(PenLineCap cap)
    {
        if (cap == PenLineCap.Flat)
        {
            return SKStrokeCap.Butt;
        }

        if (cap == PenLineCap.Round)
        {
            return SKStrokeCap.Round;
        }

        return SKStrokeCap.Square;
    }

    private double Clamp(double number, double min = 0, double max = 1) { return Math.Max(min, Math.Min(max, number)); }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1168:Empty arrays and collections should be returned instead of null", Justification = "<Pending>")]
    private List<List<Point>> ClipPolygon(List<Point> geometry) // may break polygons into multiple ones
    {
        var c = new Clipper();

        var polygon = new List<IntPoint>();

        foreach (var point in geometry)
        {
            polygon.Add(new IntPoint((int)point.X, (int)point.Y));
        }

        c.AddPolygon(polygon, PolyType.ptSubject);

        c.AddPolygon(_clipRectanglePath, PolyType.ptClip);

        var solution = new List<List<IntPoint>>();

        bool success = c.Execute(ClipType.ctIntersection, solution, PolyFillType.pftNonZero, PolyFillType.pftEvenOdd);

        if (success && solution.Count > 0)
        {
            var result = solution.Select(s => s.Select(item => new Point(item.X, item.Y)).ToList()).ToList();
            return result;
        }

        return null;
    }

    private SKPath GetPathFromGeometry(List<Point> geometry)
    {
        SKPath path = new()
        {
            FillType = SKPathFillType.EvenOdd,
        };

        var firstPoint = geometry[0];

        path.MoveTo((float)firstPoint.X, (float)firstPoint.Y);
        foreach (var point in geometry.Skip(1))
        {
            //var lastPoint = path.LastPoint;
            path.LineTo((float)point.X, (float)point.Y);
        }

        return path;
    }

    private static bool IsLeftToRight(List<Point> geometry)
    {
        var firstPoint = geometry[0];
        var lastPoint = geometry[geometry.Count - 1];

        return  (firstPoint.X <= lastPoint.X);
    }

    private static bool IsTopToBottom(List<Point> geometry)
    {
        var firstPoint = geometry[0];
        var lastPoint = geometry[geometry.Count - 1];

        return  (firstPoint.Y > lastPoint.Y);
    }

    public void DrawLineString(List<Point> geometry, Brush style)
    {
        if (ClipOverflow)
        {
            geometry = LineClipper.ClipPolyline(geometry, _clipRectangle);
            if (geometry == null)
            {
                return;
            }
        }

        var path = GetPathFromGeometry(geometry);
        if (path == null)
        {
            return;
        }

        var color = style.Paint.LineColor;

        SKPaint fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeCap = ConvertCap(style.Paint.LineCap),
            StrokeWidth = (float)style.Paint.LineWidth,
            Color = SKColorFactory.MakeColor(color.Red, color.Green, color.Blue, (byte)Clamp(color.Alpha * style.Paint.LineOpacity, 0, 255)),
            IsAntialias = true,
        };

        if (style.Paint.LineDashArray.Any())
        {
            var effect = SKPathEffect.CreateDash(style.Paint.LineDashArray.Select(n => (float)n).ToArray(), 0);
            fillPaint.PathEffect = effect;
        }

        _canvas.DrawPath(path, fillPaint);
    }

    private SKTextAlign ConvertAlignment(TextAlignment alignment)
    {
        if (alignment == TextAlignment.Center)
        {
            return SKTextAlign.Center;
        }

        if (alignment == TextAlignment.Left)
        {
            return SKTextAlign.Left;
        }

        if (alignment == TextAlignment.Right)
        {
            return SKTextAlign.Right;
        }

        return SKTextAlign.Center;
    }

    private SKPaint GetTextStrokePaint(Brush style)
    {
        var color = style.Paint.TextStrokeColor;

        var paint = new SKPaint()
        {
            IsStroke = true,
            StrokeWidth = (float)style.Paint.TextStrokeWidth,
            Color = SKColorFactory.MakeColor(color.Red, color.Green, color.Blue, (byte)Clamp(color.Alpha * style.Paint.TextOpacity, 0, 255)),
            TextSize = (float)style.Paint.TextSize,
            IsAntialias = true,
            TextEncoding = SKTextEncoding.Utf32,
            TextAlign = ConvertAlignment(style.Paint.TextJustify),
            Typeface = GetFont(style.Paint.TextFont, style),
        };

        return paint;
    }

    private SKPaint GetTextPaint(Brush style)
    {
        var color = style.Paint.TextColor;

        var paint = new SKPaint()
        {
            Color = SKColorFactory.MakeColor(color.Red, color.Green, color.Blue, (byte)Clamp(color.Alpha * style.Paint.TextOpacity, 0, 255)),
            TextSize = (float)style.Paint.TextSize,
            IsAntialias = true,
            TextEncoding = SKTextEncoding.Utf32,
            TextAlign = ConvertAlignment(style.Paint.TextJustify),
            Typeface = GetFont(style.Paint.TextFont, style),
            HintingLevel = SKPaintHinting.Normal,
        };

        return paint;
    }

    private string TransformText(string text, Brush style)
    {
        if (text.Length == 0)
        {
            return string.Empty;
        }

        if (style.Paint.TextTransform == TextTransform.Uppercase)
        {
            text = text.ToUpper();
        }
        else if (style.Paint.TextTransform == TextTransform.Lowercase)
        {
            text = text.ToLower();
        }

        var paint = GetTextPaint(style);
        text = BreakText(text, paint, style);

        return text;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1643:Strings should not be concatenated using '+' in a loop", Justification = "<Pending>")]
    private string BreakText(string input, SKPaint paint, Brush style)
    {
        string restOfText = input;
        string brokenText = string.Empty;
        do
        {
            long lineLength = paint.BreakText(restOfText, (float)(style.Paint.TextMaxWidth * style.Paint.TextSize));

            if (lineLength == restOfText.Length)
            {
                // its the end
                brokenText += restOfText.Trim();
                break;
            }

            int lastSpaceIndex = restOfText.LastIndexOf(' ', (int)(lineLength - 1));
            if (lastSpaceIndex is -1 or 0)
            {
                // no more spaces, probably ;)
                brokenText += restOfText.Trim();
                break;
            }

            brokenText += restOfText.Substring(0, lastSpaceIndex).Trim() + "\n";

            restOfText = restOfText.Substring(lastSpaceIndex, restOfText.Length - lastSpaceIndex);
        } while (restOfText.Length > 0);

        return brokenText.Trim();
    }

    private bool TextCollides(Rect rectangle)
    {
        foreach (var rect in _textRectangles)
        {
            if (rect.IntersectsWith(rectangle))
            {
                return true;
            }
        }

        return false;
    }

    private SKTypeface GetFont(string[] familyNames, Brush style)
    {
        lock (s_fontLock)
        {
            foreach (string name in familyNames)
            {
                if (_fontPairs.TryGetValue(name, value: out var font))
                {
                    return font;
                }

                // check file system for embedded fonts
                if (VectorStyleReader.TryGetFont(name, out var stream))
                {
                    var newType = SKTypeface.FromStream(stream);
                    if (newType != null)
                    {
                        _fontPairs[name] = newType;
                        return newType;
                    }
                }

                var typeface = SKTypeface.FromFamilyName(name);
                if (typeface.FamilyName == name)
                {
                    // gotcha!
                    _fontPairs[name] = typeface;
                    return typeface;
                }
            }

            // all options exhausted...
            // get the first one
            var fallback = SKTypeface.FromFamilyName(familyNames.First());
            _fontPairs[familyNames.First()] = fallback;
            return fallback;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1481:Unused local variables should be removed", Justification = "<Pending>")]
    protected SKTypeface QualifyTypeface(string text, SKTypeface typeface)
    {
        ushort[] glyphs = new ushort[typeface.CountGlyphs(text)];
        if (glyphs.Length < text.Length)
        {
            var fm = SKFontManager.Default;
            int charIdx = (glyphs.Length > 0) ? glyphs.Length : 0;
            return fm.MatchCharacter(text[glyphs.Length]);
        }

        return typeface;
    }

    private void QualifyTypeface(Brush style, SKPaint paint)
    {
        ushort[] glyphs = new ushort[paint.Typeface.CountGlyphs(style.Text)];
        if (glyphs.Length < style.Text.Length)
        {
            var fm = SKFontManager.Default;
            int charIdx = (glyphs.Length > 0) ? glyphs.Length : 0;
            var newTypeface = fm.MatchCharacter(style.Text[glyphs.Length]);

            if (newTypeface == null)
            {
                return;
            }

            paint.Typeface = newTypeface;

            glyphs = new ushort[newTypeface.CountGlyphs(style.Text)];
            if (glyphs.Length < style.Text.Length)
            {
                // still causing issues
                // so we cut the rest
                charIdx = (glyphs.Length > 0) ? glyphs.Length : 0;

                style.Text = style.Text.Substring(0, charIdx);
            }
        }
    }

    public void DrawText(Point geometry, Brush style)
    {
        if (style.Paint.TextOptional)
        {
            //  check symbol collision
            //return;
        }

        var paint = GetTextPaint(style);
        QualifyTypeface(style, paint);

        var strokePaint = GetTextStrokePaint(style);
        string text = TransformText(style.Text, style);
        string[] allLines = text.Split('\n');

        // detect collisions
        if (allLines.Length > 0)
        {
            string biggestLine = allLines.OrderBy(line => line.Length).Last();
            byte[] bytes = Encoding.UTF32.GetBytes(biggestLine);

            int twidth = (int)(paint.MeasureText(bytes));
            int left = (int)(geometry.X - twidth / 2);
            int top = (int)(geometry.Y - style.Paint.TextSize / 2 * allLines.Length);
            int theight = (int)(style.Paint.TextSize * allLines.Length);

            var rectangle = new Rect(left, top, twidth, theight);
            rectangle.Inflate(5, 5);

            if (ClipOverflow && !_clipRectangle.Contains(rectangle))
            {
                return;
            }

#if !USE_DEBUG_COLLISION
            if (TextCollides(rectangle))
            {
                // collision detected
                return;
            }

            _textRectangles.Add(rectangle);
#else
            _textRectangles.Add(rectangle);

            var list = new List<Point>()
            {
                new(rectangle.Top, rectangle.Left),
                new(rectangle.Top, rectangle.Right),
                new(rectangle.Bottom, rectangle.Right),
                new(rectangle.Bottom, rectangle.Left),
            };

            var brush = new Brush();
            brush.Paint = new Paint();
            brush.Paint.FillColor = new SKColor(0, 0, 255, 150);

            DrawPolygon(list, brush);
#endif
        }

        int i = 0;
        foreach (string line in allLines)
        {
            float lineOffset = (float)(i * style.Paint.TextSize) - (allLines.Length * (float)style.Paint.TextSize) / 2 + (float)style.Paint.TextSize;
            var position = new SKPoint((float)geometry.X + (float)(style.Paint.TextOffset.X * style.Paint.TextSize), (float)geometry.Y + (float)(style.Paint.TextOffset.Y * style.Paint.TextSize) + lineOffset);

            if (style.Paint.TextStrokeWidth != 0)
            {
                _canvas.DrawText(line, position, strokePaint);
            }

            _canvas.DrawText(line, position, paint);
            i++;
        }
    }

    private double GetPathLength(List<Point> path)
    {
        double distance = 0;
        for (int i = 0; i < path.Count - 2; i++)
        {
            var v = Subtract(path[i], path[i + 1]);
            double length = v.Length;
            distance += length;
        }

        return distance;
    }

    private Vector Subtract(Point point1, Point point2) { return new Vector(point1.X - point2.X, point1.Y - point2.Y); }

    private double GetAbsoluteDiff2Angles(double x, double y, double c = Math.PI) => c - Math.Abs((Math.Abs(x - y) % 2 * c) - c);

    private bool CheckPathSqueezing(List<Point> path, double textHeight)
    {
        double previousAngle = 0;
        for (int i = 0; i < path.Count - 2; i++)
        {
            var vector = Subtract(path[i], path[i + 1]);

            double angle = Math.Atan2(vector.Y, vector.X);
            double angleDiff = Math.Abs(GetAbsoluteDiff2Angles(angle, previousAngle));

            if (angleDiff > Math.PI / 3)
            {
                return true;
            }

            previousAngle = angle;
        }

        return false;
    }

    private void DebugRectangle(Rect rectangle, SKColor color)
    {
#if USE_DEBUG_COLLISION
        var list = new List<Point>()
        {
            new(rectangle.Top, rectangle.Left),
            new(rectangle.Top, rectangle.Right),
            new(rectangle.Bottom, rectangle.Right),
            new(rectangle.Bottom, rectangle.Left),
        };

        var brush = new Brush
        {
            Paint = new Paint
            {
                FillColor = color
            }
        };

        DrawPolygon(list, brush);
#endif
    }

    public void DrawTextOnPath(List<Point> geometry, Brush style)
    {
        geometry = LineClipper.ClipPolyline(geometry, _clipRectangle);
        if (geometry == null)
        {
            return;
        }

        // is the path | ----> | or | <---- | ?

        var ltr = IsLeftToRight(geometry);
        var ttb = IsTopToBottom(geometry);

        SKPath path;
        if (ltr)
        {
            path = GetPathFromGeometry(geometry);
        }
        else
        {
            var pathPoints = new List<Point>(geometry);
            pathPoints.Reverse();
            path = GetPathFromGeometry(pathPoints);
        }

        string text = TransformText(style.Text, style);

        if (CheckPathSqueezing(geometry, style.Paint.TextSize))
        {
            return; // path was squeezed
        }

        var bounds = path.Bounds;

        double hedge = 2;

        double left = bounds.Left - style.Paint.TextSize - hedge;
        double top = bounds.Top - style.Paint.TextSize - hedge;
        double right = bounds.Right + style.Paint.TextSize + hedge;
        double bottom = bounds.Bottom + style.Paint.TextSize + hedge;

        var rectangle = new Rect(left, top, right - left, bottom - top);

        // if (rectangle.Left <= 0 || rectangle.Right >= _width || rectangle.Top <= 0 || rectangle.Bottom >= _height)
        // {
        //     DebugRectangle(rectangle, new SKColor(255, 100, 100, 128));
        //     // bounding box (much bigger) collides with edges
        //     return;
        // }

        if (TextCollides(rectangle))
        {
            DebugRectangle(rectangle, SKColorFactory.MakeColor(100, 255, 100, 128));
            // collides with other
            return;
        }

        _textRectangles.Add(rectangle);

        if (style.Text.Length * style.Paint.TextSize * 0.2 >= GetPathLength(geometry))
        {
            DebugRectangle(rectangle, SKColorFactory.MakeColor(100, 100, 255, 128));
            // exceeds estimated path length
            return;
        }

        DebugRectangle(rectangle, SKColorFactory.MakeColor(255, 0, 0, 150));

        var offset = new SKPoint((float)style.Paint.TextOffset.X, (float)style.Paint.TextOffset.Y);
        if (style.Paint.TextStrokeWidth != 0)
        {
            //  implement this func custom way...
            _canvas.DrawTextOnPath(text, path, offset, true, GetTextStrokePaint(style));
        }

        _canvas.DrawTextOnPath(text, path, offset, true, GetTextPaint(style));


    }

    public void DrawPoint(Point geometry, Brush style)
    {
        if (style.Paint.IconImage != null)
        {
            // draw icon here
        }
    }

    public void DrawPolygon(List<Point> geometry, Brush style)
    {
        List<List<Point>> allGeometries;
        if (ClipOverflow)
        {
            allGeometries = ClipPolygon(geometry);
        }
        else
        {
            allGeometries = new List<List<Point>>()
            {
                geometry
            };
        }

        if (allGeometries == null)
        {
            return;
        }

        foreach (var geometryPart in allGeometries)
        {
            var path = GetPathFromGeometry(geometryPart);
            if (path == null)
            {
                return;
            }



            var color = !IsClockwise(geometry)
                ? SKColorFactory.MakeColor(
                    style.Paint.FillColor.Red,
                    style.Paint.FillColor.Green,
                    style.Paint.FillColor.Blue,
                    (byte)Clamp(style.Paint.FillColor.Alpha * style.Paint.FillOpacity, 0, 255))
                : _backgroundColor;

            SKPaint fillPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                StrokeCap = ConvertCap(style.Paint.LineCap),
                Color = color,
                IsAntialias = true,
            };

            _canvas.DrawPath(path, fillPaint);
        }
    }

    private static bool IsClockwise(List<Point> polygon)
    {
        double sum = 0.0;
        for (int i = 0; i < polygon.Count; i++)
        {
            Point v1 = polygon[i];
            Point v2 = polygon[(i + 1) % polygon.Count];
            sum += (v2.X - v1.X) * (v2.Y + v1.Y);
        }

        return sum > 0.0;
    }

    public void DrawImage(byte[] imageData, Brush style)
    {
        try
        {
            var image = SKBitmap.Decode(imageData);
            _canvas.DrawBitmap(image, new SKPoint(0, 0));
        }
        catch (Exception)
        {
            // something went wrong with the image format
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S1186:Methods should not be empty", Justification = "Third party")]
    public void DrawUnknown(List<List<Point>> geometry, Brush style) { }

    public void DrawDebugBox(TileInfo tileData, SKColor color)
    {
        _surface.Canvas.DrawRect(new SKRect(0, 0, _width, _height), new SKPaint
        {
            Color = color,
            Style = SKPaintStyle.Stroke,
        });

        _surface.Canvas.DrawText($"({tileData.X}, {tileData.Y}, {(int)tileData.Zoom})", new SKPoint(20, 20), new SKPaint
        {
            FakeBoldText = true,
            TextSize = 14,
            Color = color,
            Style = SKPaintStyle.Stroke,
        });
    }

    public byte[] FinishDrawing()
    {
        using var image = _surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 80);
        using var result = new MemoryStream();
        data.SaveTo(result);
        return result.ToArray();
    }
}