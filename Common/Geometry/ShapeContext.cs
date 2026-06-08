using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using System;

#nullable enable

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

    /// <summary>
    /// When true, forces equal width and height — rectangles become squares,
    /// ellipses become circles, etc. The larger dimension is used, centered
    /// on the original bounding box.
    /// </summary>
    public bool EqualDimensions { get; set; }

    /// <summary>
    /// How the shape is sliced to produce a half-shape.
    /// Shapes compute partial geometry natively based on this value.
    /// The specific half is determined by Start/End drag direction.
    /// </summary>
    public SliceMode Slice { get; set; }

    /// <summary>
    /// When true and slicing is active on a hollow shape, the diameter edge
    /// (flat side) is drawn. When false, the diameter edge is omitted,
    /// leaving an open-sided shape (e.g. 3-sided rectangle, open half-circle).
    /// Only meaningful when <see cref="Slice"/> != <see cref="SliceMode.Full"/>
    /// and <see cref="Mode"/> == <see cref="ShapeMode.Hollow"/>.
    /// </summary>
    public bool ConnectDiameter { get; set; }

    /// <summary>
    /// (S2 2026-04-30 — DesignDoc_HalfShapeOrientationFlipToggle.md #IOP)
    /// When true, inverts which half of a sliced shape is kept relative to the
    /// drag direction. Normally <see cref="SliceHelper.IsStartAbove" /> /
    /// <see cref="SliceHelper.IsStartLeft" /> select the kept half from
    /// Start/End; with this flag set the opposite half is kept instead.
    /// Only meaningful when <see cref="Slice" /> != <see cref="SliceMode.Full" />.
    /// Applied symmetrically inside <see cref="SliceHelper.SliceFilledTiles" />
    /// AND <see cref="SliceHelper.RemoveDiameterEdge" /> so the diameter band
    /// stays on the discarded side.
    /// </summary>
    public bool InvertHalfOrientation { get; set; }

    /// <summary>
    /// (S2 2026-05-24 Session 2 — FromCenterShapeOption.md)
    /// Controls how <see cref="GetBounds" /> computes the shape bounding box.
    /// Off = default drag bbox (Start..End corners).
    /// Odd  = Start is the center tile; bbox is symmetric with odd dimensions.
    /// Even = Start is the corner of the center 2×2; bbox has even dimensions.
    /// Shapes that do not support this (Triangle, Elbow, etc.) receive Off
    /// from <see cref="Settings.ShapeInfo.ToShapeContext" /> unconditionally.
    /// </summary>
    public DrawFromCenterMode DrawFromCenter { get; set; }

    /// <summary>
    /// Additional input points beyond Start and End for multi-point shapes.
    /// Null for all current 2-point shapes (zero allocation overhead).
    /// Future shapes (Arc=1 extra, ArcDonut=2 extra, Polygon=N-2 extra) populate this.
    /// </summary>
    public Point[]? ExtraPoints { get; set; }

    /// <summary>
    /// Returns the total number of defining points for this shape context.
    /// Always at least 2 (Start + End). Multi-point shapes add ExtraPoints.
    /// </summary>
    public int TotalPoints => 2 + (ExtraPoints?.Length ?? 0);

    public ShapeContext(Point start, Point end)
    {
        Start = start;
        End = end;
        Mode = ShapeMode.Filled;
        Thickness = 1;
        HBias = HorizontalBias.None;
        VBias = VerticalBias.None;
        VerticalFirst = false;
        EqualDimensions = false;
        Slice = SliceMode.Full;
        ConnectDiameter = true;
        InvertHalfOrientation = false;
        ExtraPoints = null;
        DrawFromCenter = DrawFromCenterMode.Off;
    }

    public ShapeContext(Point start, Point end, ShapeMode mode, int thickness, 
        HorizontalBias hBias, VerticalBias vBias, bool verticalFirst, bool equalDimensions = false,
        SliceMode slice = SliceMode.Full, bool connectDiameter = true,
        bool invertHalfOrientation = false,
        DrawFromCenterMode drawFromCenter = DrawFromCenterMode.Off)
    {
        Start = start;
        End = end;
        Mode = mode;
        Thickness = thickness;
        HBias = hBias;
        VBias = vBias;
        VerticalFirst = verticalFirst;
        EqualDimensions = equalDimensions;
        Slice = slice;
        ConnectDiameter = connectDiameter;
        InvertHalfOrientation = invertHalfOrientation;
        ExtraPoints = null;
        DrawFromCenter = drawFromCenter;
    }

    /// <summary>
    /// Returns the bounding rectangle for this shape context.
    /// <para>
    /// When <see cref="DrawFromCenter" /> is <see cref="DrawFromCenterMode.Odd" />:
    /// Start is the center tile. rx = |End.X−Start.X|, ry = |End.Y−Start.Y|.
    /// bbox = (cx−rx, cy−ry) to (cx+rx, cy+ry) — always odd dimensions.
    /// </para>
    /// <para>
    /// When <see cref="DrawFromCenter" /> is <see cref="DrawFromCenterMode.Even" />:
    /// Start is the corner of the central 2×2 block. rx = |dx|+1, ry = |dy|+1.
    /// bbox extends 2rx×2ry in the drag direction — always even dimensions.
    /// </para>
    /// <para>
    /// When <see cref="EqualDimensions" /> is true, the radius is clamped to
    /// max(rx, ry) before computing the bbox, maintaining center symmetry.
    /// </para>
    /// <para>
    /// When <see cref="DrawFromCenter" /> is <see cref="DrawFromCenterMode.Off" />:
    /// legacy behavior — the rectangle is expanded to a square using the
    /// larger dimension, anchored at <see cref="Start" /> when EqualDimensions
    /// is active.
    /// </para>
    /// </summary>
    public Rectangle GetBounds()
    {
        // ---- Draw-from-center path ----
        if (DrawFromCenter == DrawFromCenterMode.Odd)
        {
            int rx = Math.Abs(End.X - Start.X);
            int ry = Math.Abs(End.Y - Start.Y);
            if (EqualDimensions)
            {
                int r = Math.Max(rx, ry);
                rx = r; ry = r;
            }
            int minX = Start.X - rx;
            int minY = Start.Y - ry;
            int width  = 2 * rx + 1;
            int height = 2 * ry + 1;
            return new Rectangle(minX, minY, width, height);
        }

        if (DrawFromCenter == DrawFromCenterMode.Even)
        {
            // rx/ry are the half-side lengths (minimum 1 so the bbox is always at least 2×2)
            int rx = Math.Abs(End.X - Start.X) + 1;
            int ry = Math.Abs(End.Y - Start.Y) + 1;
            if (EqualDimensions)
            {
                int r = Math.Max(rx, ry);
                rx = r; ry = r;
            }
            // Anchor at Start corner; extend in drag direction so the center stays
            // between Start and (Start + 1) in each axis.
            int minX, minY;
            if (End.X >= Start.X) minX = Start.X;
            else                  minX = Start.X - 2 * rx + 1;
            if (End.Y >= Start.Y) minY = Start.Y;
            else                  minY = Start.Y - 2 * ry + 1;
            return new Rectangle(minX, minY, 2 * rx, 2 * ry);
        }

        // ---- Default drag-bbox path ----
        int dminX = Math.Min(Start.X, End.X);
        int dminY = Math.Min(Start.Y, End.Y);
        int dmaxX = Math.Max(Start.X, End.X);
        int dmaxY = Math.Max(Start.Y, End.Y);

        int dwidth  = dmaxX - dminX + 1;
        int dheight = dmaxY - dminY + 1;

        if (EqualDimensions)
        {
            int size = Math.Max(dwidth, dheight);

            // Anchor the square at Start — extend in the direction of End.
            // This keeps the Start corner fixed, so the origin never shifts
            // due to integer truncation as the selection grows by 1 tile.
            if (End.X >= Start.X)
                dminX = Start.X;
            else
                dminX = Start.X - size + 1;

            if (End.Y >= Start.Y)
                dminY = Start.Y;
            else
                dminY = Start.Y - size + 1;

            dwidth  = size;
            dheight = size;
        }

        return new Rectangle(dminX, dminY, dwidth, dheight);
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
        bool? verticalFirst = null,
        bool? equalDimensions = null,
        SliceMode? slice = null,
        bool? connectDiameter = null,
        bool? invertHalfOrientation = null,
        Point[]? extraPoints = null,
        DrawFromCenterMode? drawFromCenter = null)
    {
        return new ShapeContext(
            Start, End,
            mode ?? Mode,
            thickness ?? Thickness,
            hBias ?? HBias,
            vBias ?? VBias,
            verticalFirst ?? VerticalFirst,
            equalDimensions ?? EqualDimensions,
            slice ?? Slice,
            connectDiameter ?? ConnectDiameter,
            invertHalfOrientation ?? InvertHalfOrientation,
            drawFromCenter ?? DrawFromCenter
        )
        {
            ExtraPoints = extraPoints ?? ExtraPoints
        };
    }
}

