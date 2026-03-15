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
            var provider = GetProvider(shapeType);
            return provider.GetTiles(context);
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