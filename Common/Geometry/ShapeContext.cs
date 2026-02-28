using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using System;

namespace WorldShapingWandsMod.Common.Geometry;

/// <summary>
/// Context containing all parameters needed for shape generation.
/// </summary>
public struct ShapeContext
{
    public Point Start { get; set; }
    public Point End { get; set; }
    public ShapeMode Mode { get; set; }
    public int Thickness { get; set; }
    public HorizontalBias HBias { get; set; }
    public VerticalBias VBias { get; set; }
    public bool VerticalFirst { get; set; }

    public ShapeContext(Point start, Point end)
    {
        Start = start;
        End = end;
        Mode = ShapeMode.Filled;
        Thickness = 1;
        HBias = HorizontalBias.None;
        VBias = VerticalBias.None;
        VerticalFirst = false;
    }

    public ShapeContext(Point start, Point end, ShapeMode mode, int thickness, 
        HorizontalBias hBias, VerticalBias vBias, bool verticalFirst)
    {
        Start = start;
        End = end;
        Mode = mode;
        Thickness = thickness;
        HBias = hBias;
        VBias = vBias;
        VerticalFirst = verticalFirst;
    }

    public Rectangle GetBounds()
    {
        int minX = Math.Min(Start.X, End.X);
        int minY = Math.Min(Start.Y, End.Y);
        int maxX = Math.Max(Start.X, End.X);
        int maxY = Math.Max(Start.Y, End.Y);
        
        return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    public Vector2 GetCenter()
    {
        var bounds = GetBounds();
        return new Vector2(
            bounds.X + bounds.Width / 2f,
            bounds.Y + bounds.Height / 2f
        );
    }

    public ShapeContext With(
        ShapeMode? mode = null,
        int? thickness = null,
        HorizontalBias? hBias = null,
        VerticalBias? vBias = null,
        bool? verticalFirst = null)
    {
        return new ShapeContext(
            Start, End,
            mode ?? Mode,
            thickness ?? Thickness,
            hBias ?? HBias,
            vBias ?? VBias,
            verticalFirst ?? VerticalFirst
        );
    }
}

