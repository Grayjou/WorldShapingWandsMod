using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry.Shapes;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Geometry;

public static class ShapeRegistry
{
    private static readonly Dictionary<ShapeType, IShapeProvider> _providers = new();
    private static bool _initialized = false;

    public static void Initialize()
    {
        if (_initialized) return;

        Register(new RectangleShape());
        Register(new EllipseShape());
        Register(new DiamondShape());
        Register(new TriangleShape());
        Register(new ElbowShape());
        Register(new CardinalLineShape());
        Register(new StraightLineShape());
        Register(new MoldShape());

        // (S10 2026-04-29; StencilMagicWandSelectionPlan.md §7) Magic Wand
        // Read provider.
        Register(new MagicWandReadShape());

        _initialized = true;
    }

    public static void Unload()
    {
        _providers.Clear();
        _initialized = false;
    }

        /// <summary>
        /// Registers a shape provider.
        /// </summary>
        public static void Register(IShapeProvider provider)
        {
            _providers[provider.ShapeType] = provider;
        }

        /// <summary>
        /// Gets the provider for a specific shape type.
        /// </summary>
        public static IShapeProvider GetProvider(ShapeType shapeType)
        {
            Initialize();

#pragma warning disable CS0618
            if (shapeType == ShapeType.MagicWandApply)
                shapeType = ShapeType.MagicWandRead;
#pragma warning restore CS0618
            
            if (_providers.TryGetValue(shapeType, out var provider))
                return provider;
                
            throw new ArgumentException($"No provider registered for shape type: {shapeType}");
        }

        /// <summary>
        /// Main entry point: calculates tiles for any shape type.
        /// Slicing is handled natively by each shape provider via the context's
        /// <see cref="ShapeContext.Slice"/> and <see cref="ShapeContext.ConnectDiameter"/> properties.
        /// </summary>
        public static ShapeTileSet GetShapeTiles(ShapeType shapeType, ShapeContext context)
        {
            bool quickHalfHorizontal = context.Slice == SliceMode.QuickHalfHorizontal;
            bool quickHalfVertical = context.Slice == SliceMode.QuickHalfVertical;
            bool needsQuickFlipAnchorFix = context.InvertHalfOrientation && (quickHalfHorizontal || quickHalfVertical);
            int shiftX = quickHalfVertical ? context.Start.X - context.End.X : 0;
            int shiftY = quickHalfHorizontal ? context.Start.Y - context.End.Y : 0;

            // (S12 2026-04-29; HalfShapeQuickSlice.md §4) Single point of
            // expansion for SliceMode.QuickHalfHorizontal/QuickHalfVertical.
            // No-op for SliceMode.Full and the non-quick half modes.
            // Providers see only plain HalfH/HalfV after this call, so the
            // legacy SliceHelper.SliceFilledTiles pipeline runs unchanged.
            context = SliceHelper.PreExpandForQuickSlice(context);
            var provider = GetProvider(shapeType);
            var tileSet = provider.GetTiles(context);

            if (!needsQuickFlipAnchorFix || (shiftX == 0 && shiftY == 0))
                return tileSet;

            return new ShapeTileSet(
                tileSet.Tiles.Select(p => new Point(p.X + shiftX, p.Y + shiftY)),
                tileSet.BoundaryTiles.Select(p => new Point(p.X + shiftX, p.Y + shiftY))
            );
        }

        /// <summary>
        /// Returns shape-aware display dimensions for the given shape and context.
        /// Shapes like CardinalLine return accurate dimensions (e.g. Wx1 for horizontal lines)
        /// instead of the raw bounding box.
        /// </summary>
        public static (int Width, int Height) GetDisplayDimensions(ShapeType shapeType, ShapeContext context)
        {
            var provider = GetProvider(shapeType);
            return provider.GetDisplayDimensions(context);
        }

    /// <summary>
    /// Returns the number of input points required to define the given shape.
    /// All current shapes require 2 points (bounding box corners).
    /// Future multi-point shapes (Arc, Polygon, etc.) will return higher values.
    /// Returns -1 for variable-point shapes (e.g. Polygon).
    /// </summary>
    public static int GetRequiredPoints(ShapeType shape) => shape switch
    {
        ShapeType.Rectangle => 2,
        ShapeType.Ellipse => 2,
        ShapeType.Diamond => 2,
        ShapeType.Triangle => 2,
        ShapeType.Elbow => 2,
        ShapeType.CardinalLine => 2,
        ShapeType.StraightLine => 2,
        // Magic Wand shapes are single-point stamps like Mold:
        // Start == End == cursor. The shape-cycle UI treats them as
        // 2-point inputs to keep the existing pipeline; semantically
        // only the Start position matters.
        ShapeType.MagicWandRead => 2,
    #pragma warning disable CS0618
        ShapeType.MagicWandApply => 2,
    #pragma warning restore CS0618
        // Future multi-point shapes will be added here:
        // ShapeType.Arc => 3,
        // ShapeType.ArcDonut => 4,
        // ShapeType.Polygon => -1, // Variable
        _ => 2,
    };

    /// <summary>
    /// Returns true if the given shape requires more than 2 input points.
    /// </summary>
    public static bool IsMultiPointShape(ShapeType shape) =>
        GetRequiredPoints(shape) is not 2;

    /// <summary>
    /// Convenience overload for simple rectangle-based selections.
    /// </summary>
    public static ShapeTileSet GetShapeTiles(
        ShapeType shapeType,
        Microsoft.Xna.Framework.Point start,
        Microsoft.Xna.Framework.Point end,
        ShapeMode mode = ShapeMode.Filled,
        bool verticalFirst = false)
    {
        var context = new ShapeContext(start, end)
        {
            Mode = mode,
            VerticalFirst = verticalFirst
        };
        
        return GetShapeTiles(shapeType, context);
    }
}