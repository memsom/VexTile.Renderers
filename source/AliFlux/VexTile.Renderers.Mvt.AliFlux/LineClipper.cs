﻿using System;
using System.Collections.Generic;
using System.Linq;
using VexTile.Renderer.Mvt.AliFlux.Drawing;
using VexTile.Renderer.Mvt.AliFlux.Enums;

namespace VexTile.Renderer.Mvt.AliFlux;

internal static class LineClipper
{
    private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

    private static OutCode ComputeOutCode(double x, double y, Rect r)
    {
        var code = OutCode.Inside;

        if (x < r.Left)
        {
            code |= OutCode.Left;
        }

        if (x > r.Right)
        {
            code |= OutCode.Right;
        }

        if (y < r.Top)
        {
            code |= OutCode.Top;
        }

        if (y > r.Bottom)
        {
            code |= OutCode.Bottom;
        }

        return code;
    }

    private static OutCode ComputeOutCode(Point p, Rect r) => ComputeOutCode(p.X, p.Y, r);

    private static Point CalculateIntersection(Rect r, Point p1, Point p2, OutCode clipTo)
    {
        double dx = (p2.X - p1.X);
        double dy = (p2.Y - p1.Y);

        double slopeY = dx / dy; // slope to use for possibly-vertical lines
        double slopeX = dy / dx; // slope to use for possibly-horizontal lines

        if (clipTo.HasFlag(OutCode.Top))
        {
            return new Point(
                p1.X + slopeY * (r.Top - p1.Y),
                r.Top
            );
        }

        if (clipTo.HasFlag(OutCode.Bottom))
        {
            return new Point(
                p1.X + slopeY * (r.Bottom - p1.Y),
                r.Bottom
            );
        }

        if (clipTo.HasFlag(OutCode.Right))
        {
            return new Point(
                r.Right,
                p1.Y + slopeX * (r.Right - p1.X)
            );
        }

        if (clipTo.HasFlag(OutCode.Left))
        {
            return new Point(
                r.Left,
                p1.Y + slopeX * (r.Left - p1.X)
            );
        }

        throw new ArgumentOutOfRangeException("clipTo = " + clipTo);
    }

    private static Tuple<Point, Point> ClipSegment(Rect r, Point p1, Point p2)
    {
        // classify the endpoints of the line
        var outCodeP1 = ComputeOutCode(p1, r);
        var outCodeP2 = ComputeOutCode(p2, r);
        bool accept = false;

        while (true)
        {
            // should only iterate twice, at most
            // Case 1:
            // both endpoints are within the clipping region
            if ((outCodeP1 | outCodeP2) == OutCode.Inside)
            {
                accept = true;
                break;
            }

            // Case 2:
            // both endpoints share an excluded region, impossible for a line between them to be within the clipping region
            if ((outCodeP1 & outCodeP2) != 0)
            {
                break;
            }

            // Case 3:
            // The endpoints are in different regions, and the segment is partially within the clipping rectangle

            // Select one of the endpoints outside the clipping rectangle
            var outCode = outCodeP1 != OutCode.Inside ? outCodeP1 : outCodeP2;

            // calculate the intersection of the line with the clipping rectangle
            var p = CalculateIntersection(r, p1, p2, outCode);

            // update the point after clipping and recalculate outcode
            if (outCode == outCodeP1)
            {
                p1 = p;
                outCodeP1 = ComputeOutCode(p1, r);
            }
            else
            {
                p2 = p;
                outCodeP2 = ComputeOutCode(p2, r);
            }
        }

        // if clipping area contained a portion of the line
        if (accept)
        {
            return new(p1, p2);
        }

        // the line did not intersect the clipping area
        return null;
    }

    private static Rect GetLineRect(List<Point> polyLine)
    {
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (var point in polyLine)
        {
            if (point.X < minX)
            {
                minX = point.X;
            }

            if (point.Y < minY)
            {
                minY = point.Y;
            }

            if (point.X > maxX)
            {
                maxX = point.X;
            }

            if (point.Y > maxY)
            {
                maxY = point.Y;
            }
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1168:Empty arrays and collections should be returned instead of null", Justification = "<Pending>")]
    public static List<Point> ClipPolyline(List<Point> polyLine, Rect bounds)
    {
        var lineRect = GetLineRect(polyLine);

        if (!bounds.IntersectsWith(lineRect))
        {
            return null;
        }

        List<Point> newLine = null;

        for (int i = 1; i < polyLine.Count; i++)
        {
            var p1 = polyLine[i - 1];
            var p2 = polyLine[i];

            var newSegment = ClipSegment(bounds, p1, p2);

            if (newSegment != null)
            {
                if (newLine == null)
                {
                    newLine =
                    [
                        newSegment.Item1,
                        newSegment.Item2
                    ];
                }
                else
                {
                    if (newLine.Last() == newSegment.Item1)
                    {
                        newLine.Add(newSegment.Item2);
                    }
                    else
                    {
                        newLine.Add(newSegment.Item1);
                        newLine.Add(newSegment.Item2);
                    }
                }
            }
            else
            {
                Log.Debug("Segment is null");
            }
        }

        return newLine;
    }
}
