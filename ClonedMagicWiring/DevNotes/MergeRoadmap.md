# Merge Roadmap - Integration with BulkShapeOps Mod

## Overview
Plan to merge MagicWiring with a separate bulk operations mod that provides block placing and removing with shapes. This integration would create a comprehensive construction toolkit for Terraria.

## Shared Functionality

### Common Shapes
Both mods share these shapes:
- **Rectangles** (filled & hollow) ✅
- **Triangles** (filled & hollow) ✅

### Shape System Architecture
Current `ShapeHelper` class is well-designed for extension:
- Static methods for shape generation
- Consistent `List<Point>` return type
- Already supports multiple shape types via enum
- Distance clamping system reusable

## BulkShapeOps Unique Features

### Additional Shapes
- **Ellipses/Circles** (filled & hollow)
  - Implementation note: Use Bresenham's ellipse algorithm or discrete distance formula
  - Similar to current diamond approach but Euclidean distance instead of Manhattan
- **Custom/Free-form** polygons
  - Multi-click point selection
  - Convex hull or simple polygon filling

### Noise/Randomization System
Add parameter to introduce variation to standard shapes:
- **Perlin/Simplex noise** overlay on shape boundaries
- **Random tile skipping** (e.g., 80% fill density)
- **Edge roughness** parameter
- **Organic deformation** of geometric shapes

**Implementation considerations:**
```csharp
public enum NoiseType { None, Skip, EdgeRoughness, Perlin }

public static List<Point> GetShapeTiles(
    Point start, 
    Point end, 
    WiringShape shape, 
    bool verticalFirst = false,
    NoiseType noise = NoiseType.None,
    float noiseStrength = 0.2f,
    int? seed = null)
{
    var baseTiles = GetBaseShape(start, end, shape, verticalFirst);
    return ApplyNoise(baseTiles, noise, noiseStrength, seed);
}
```

## Integration Strategy

### Option A: Separate Items, Shared Backend
- Keep Wand of Wiring for wiring operations
- Create separate tool for block operations
- Share `ShapeHelper` and `OverlaySystem` via common library
- Each tool has its own UI panel

**Pros:**
- Clear separation of concerns
- Users can choose which features they want
- Easier to maintain/test
- Can release separately or together

**Cons:**
- Two items in inventory
- Potential UI clutter
- Duplicated code if not carefully architected

### Option B: Unified Multi-Tool
- Single "Magic Constructor" tool
- Tabbed UI with Wiring, Blocks, and future categories
- Shared shape/noise system
- Mode switcher (wire/block/both)

**Pros:**
- Single inventory slot
- Unified UX
- Easier to discover features
- Natural extension point for future tools

**Cons:**
- More complex UI
- Harder to learn for new users
- Risk of feature bloat

### Option C: Modular System
- Base "Shape Operations Framework" mod
- Individual feature mods depend on framework
- MagicWiring becomes a plugin
- BulkShapeOps becomes another plugin

**Pros:**
- Maximum flexibility
- Other mod creators can add features
- Clean architecture
- Users install only what they want

**Cons:**
- Complex for average user
- Dependency management
- More mods to maintain

## Recommended Approach: Option B (Unified Multi-Tool)

### Reasoning
1. Better user experience (one tool to learn)
2. Shared UI reduces development time
3. Natural synergy (often wire and build together)
4. Future-proof for additional features
5. Cleaner inventory management

### Implementation Phases

#### Phase 1: Architecture Preparation
- [ ] Extract `ShapeHelper` to be operation-agnostic
- [ ] Refactor overlay system to support multiple visualizations
- [ ] Design extensible operation system (IOperation interface)
- [ ] Create plugin architecture in UI system

#### Phase 2: Core Integration
- [ ] Implement ellipse shape generation
- [ ] Add noise system to ShapeHelper
- [ ] Create unified settings storage
- [ ] Design tabbed UI system

#### Phase 3: Block Operations
- [ ] Implement block placement operation
- [ ] Add material requirements calculation
- [ ] Integrate with inventory system
- [ ] Add block removal with drop handling

#### Phase 4: Polish & Balance
- [ ] Adjust recipe to reflect increased utility
- [ ] Balance crafting costs for combined tool
- [ ] Performance optimization for large operations
- [ ] Add comprehensive config options

## Technical Considerations

### Shape Ellipse Implementation
```csharp
private static List<Point> GetFilledEllipse(int minX, int minY, int maxX, int maxY)
{
    var tiles = new List<Point>();
    double cx = (minX + maxX) / 2.0;
    double cy = (minY + maxY) / 2.0;
    double rx = (maxX - minX) / 2.0;
    double ry = (maxY - minY) / 2.0;
    
    for (int x = minX; x <= maxX; x++)
    {
        for (int y = minY; y <= maxY; y++)
        {
            // Ellipse equation: (x-cx)²/rx² + (y-cy)²/ry² <= 1
            double dx = x - cx;
            double dy = y - cy;
            double dist = (dx * dx) / (rx * rx) + (dy * dy) / (ry * ry);
            
            if (dist <= 1.0 + 0.001)
                tiles.Add(new Point(x, y));
        }
    }
    return tiles;
}
```

### Noise System
```csharp
private static List<Point> ApplyNoise(List<Point> tiles, NoiseType type, 
    float strength, int? seed)
{
    if (type == NoiseType.None || strength <= 0)
        return tiles;
        
    var random = seed.HasValue ? new Random(seed.Value) : new Random();
    
    switch (type)
    {
        case NoiseType.Skip:
            // Randomly skip tiles
            return tiles.Where(t => random.NextDouble() > strength).ToList();
            
        case NoiseType.EdgeRoughness:
            // Add/remove edge tiles randomly
            var result = new List<Point>(tiles);
            var tileSet = new HashSet<Point>(tiles);
            foreach (var tile in tiles)
            {
                if (IsEdgeTile(tile, tileSet))
                {
                    if (random.NextDouble() < strength)
                    {
                        // Add neighboring tiles or remove this one
                        if (random.NextDouble() < 0.5)
                            result.Remove(tile);
                        else
                            AddRandomNeighbor(result, tile, random);
                    }
                }
            }
            return result;
            
        // Perlin noise implementation...
    }
}
```

## Performance Targets
- Shape generation: <5ms for 2000 tiles
- Noise application: <10ms for 1000 tiles
- UI response: <16ms (60fps)
- Network packet: <1KB for typical operation

## Compatibility Notes
- Ensure backward compatibility with existing MagicWiring save data
- Migration path for users upgrading from standalone version
- Config migration system
- Deprecation warnings if needed

## Testing Strategy
- Unit tests for all shape algorithms
- Performance benchmarks for large selections
- Multiplayer sync testing
- UI scaling tests (different resolutions)
- Edge case testing (world boundaries, invalid tiles, etc.)

## Release Strategy
1. Release MagicWiring standalone (current)
2. Gather user feedback
3. Develop BulkShapeOps prototype
4. Alpha test unified version
5. Offer upgrade path or fresh install
6. Maintain both versions temporarily
7. Eventual deprecation of standalone after adoption

## Open Questions
- [ ] Should wiring and blocks share the same shape in one operation?
- [ ] How to handle insufficient materials?
- [ ] Undo system scope (per-operation vs global)?
- [ ] Should noise patterns be saveable/shareable?
- [ ] Integration with other construction mods?
