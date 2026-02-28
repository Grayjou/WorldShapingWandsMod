# UI Overlay Improvements - Outlining Individual Boundary Tiles

## Current Implementation
- Semi-transparent solid squares (MagicPixel texture)
- Different opacity for edge vs interior tiles
- Edge detection: checks 4-connected neighbors
- Colors: green (place), blue (remove), orange (clamped/pulsing)

**Current Code Location**: `Drawing/WiringOverlaySystem.cs` lines 104-110

```csharp
bool isEdge = !tileSet.Contains(new Point(tile.X - 1, tile.Y)) ||
             !tileSet.Contains(new Point(tile.X + 1, tile.Y)) ||
             !tileSet.Contains(new Point(tile.X, tile.Y - 1)) ||
             !tileSet.Contains(new Point(tile.X, tile.Y + 1));

Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, destRect,
    isEdge ? borderColor : fillColor);
```

## Problem
- Looks "blobby" and unclear at large scales
- Hard to distinguish individual tile boundaries within selection
- Filled interior can obscure tile details
- Not visually distinct enough from terrain
- Individual tiles in the boundary aren't clearly outlined

## User Request: Outline Every Boundary Square Individually

The desired improvement is to outline each individual boundary tile (not just the outer perimeter), making each tile's boundary visible. This helps identify exact tile positions in the selection.

### Implementation Approach

**Option A: Draw 4 edges per boundary tile (with deduplication)**

For each boundary tile, draw its 4 edges, but only draw an edge if the adjacent tile is NOT in the selection. This prevents double-drawing shared edges.

```csharp
foreach (var tile in tiles)
{
    bool isEdge = !tileSet.Contains(new Point(tile.X - 1, tile.Y)) ||
                 !tileSet.Contains(new Point(tile.X + 1, tile.Y)) ||
                 !tileSet.Contains(new Point(tile.X, tile.Y - 1)) ||
                 !tileSet.Contains(new Point(tile.X, tile.Y + 1));
    
    if (!isEdge) continue; // Only process boundary tiles
    
    var destRect = new Rectangle((int)screenPos.X, (int)screenPos.Y, TileSize, TileSize);
    
    // Draw border segments only where tile is on boundary
    if (!tileSet.Contains(new Point(tile.X - 1, tile.Y)))
        DrawLeftEdge(destRect, borderColor); // Left edge
    if (!tileSet.Contains(new Point(tile.X + 1, tile.Y)))
        DrawRightEdge(destRect, borderColor); // Right edge
    if (!tileSet.Contains(new Point(tile.X, tile.Y - 1)))
        DrawTopEdge(destRect, borderColor); // Top edge
    if (!tileSet.Contains(new Point(tile.X, tile.Y + 1)))
        DrawBottomEdge(destRect, borderColor); // Bottom edge
}
```

**Performance Impact**: ⚠️ **LOW-MODERATE**
- Only boundary tiles are processed (not all tiles)
- Each boundary tile draws 1-4 edges (average ~2-3 for most shapes)
- Edge deduplication is automatic (only draw if neighbor not in set)
- Estimated: 10-20% slower than current, still well under 1ms for typical selections

**Option B: Draw all tile outlines, then fill interior**

Draw outline for every tile in selection, then fill interior tiles.

```csharp
// Pass 1: Fill interior tiles
foreach (var tile in tiles)
{
    bool isEdge = IsEdgeTile(tile, tileSet);
    if (!isEdge)
        DrawFill(destRect, fillColor);
}

// Pass 2: Draw outline for all tiles
foreach (var tile in tiles)
{
    DrawTileOutline(destRect, borderColor, 1); // 1px outline
}
```

**Performance Impact**: ⚠️ **MODERATE**
- Requires 2 passes over all tiles
- 4 draw calls per tile (one per edge)
- Estimated: 30-50% slower, may approach 1ms for large selections

**Recommendation**: Option A (boundary edges only with deduplication)
- Cleaner visual result (only outer perimeter is outlined)
- Better performance (only boundary tiles processed)
- No overdraw of shared edges
- Matches user's request for outlining boundary squares

## Alternative: Outline All Tiles Individually

If the goal is to outline EVERY tile (not just boundary), showing grid lines:

```csharp
foreach (var tile in tiles)
{
    // Draw semi-transparent fill
    DrawFill(destRect, fillColor);
    
    // Draw 1px grid lines around this tile
    DrawRectangleOutline(destRect, gridLineColor, thickness: 1);
}
```

This creates a visible grid showing each tile's boundaries. Performance is similar to Option B above.

## Proposed Solutions (original options retained below)

### Option 1: Outline-Only Rendering
Draw only 1-pixel borders around each tile in the selection.

**Pros:**
- Cleaner, more professional look
- Doesn't obscure tile details
- Clear tile boundaries
- Minimal screen coverage

**Cons:**
- Harder to see at a glance what's selected
- Thin lines may be hard to see on busy backgrounds
- More complex rendering logic

**Performance Impact**: ⚠️ **MODERATE**
- 4x draw calls per tile (one per edge)
- Need edge deduplication to avoid overdraw
- Could batch into line primitives
- Estimated: 2-3x slower for large selections (>500 tiles)

**Implementation Complexity**: Medium
- Need to track which edges to draw (no duplicates)
- Could use line rendering or rotated 1-pixel quads
- Consider using GraphicsDevice.DrawUserPrimitives for batching

### Option 2: Outlined Squares (Border + Fill)
Draw filled squares with distinct visible borders.

**Pros:**
- Best of both worlds: visibility + clarity
- Can use different border/fill colors
- Still shows selection area clearly
- Professional appearance

**Cons:**
- Slightly more complex than current
- Two draw calls per tile

**Performance Impact**: ✅ **MINIMAL**
- Already doing edge detection
- Just need to draw border as separate rectangle
- Can batch both passes
- Estimated: <10% slower than current

**Implementation Complexity**: Low
- Draw fill pass (all tiles)
- Draw border pass (edge tiles only or all tiles with border offset)
- Could use DrawRectangle helper or 2-pass Draw calls

### Option 3: Mixed Approach
Hollow shapes show outline only, filled shapes show current style.

**Pros:**
- Shape type visually reinforced
- Best performance (selectively expensive)
- User gets visual feedback about shape type

**Cons:**
- Inconsistent appearance
- May be confusing

**Performance Impact**: ⚠️ **VARIABLE**
- Depends on shape selection ratio
- Hollow = expensive, filled = current

### Option 4: Textured Border Overlay
Create actual border textures (like UI nineslice).

**Pros:**
- Highest quality appearance
- Can include decorative elements
- GPU-friendly (texture streaming)

**Cons:**
- Requires new assets
- More VRAM usage
- Tiling complexity

**Performance Impact**: ✅ **GOOD**
- Single textured quad per tile
- GPU handles texturing efficiently

## Recommendation: Option 2 (Outlined Squares)

**Rationale:**
1. Minimal performance impact (~5-10% based on similar rendering code)
2. Significantly improved visual clarity
3. Low implementation complexity
4. No new assets required
5. Maintains current color-coding system

**Implementation Plan:**
```csharp
// Pseudo-code
foreach (var tile in tiles)
{
    // Draw fill
    Draw(MagicPixel, destRect, fillColor * 0.15f);
    
    // Draw border (inset by 1-2 pixels)
    var borderRect = new Rectangle(destRect.X + 1, destRect.Y + 1, 
                                   destRect.Width - 2, destRect.Height - 2);
    DrawRectangleOutline(borderRect, borderColor, thickness: 1);
}
```

## Performance Testing Notes
- Current average: ~0.1ms for 200 tiles
- With Option 2: estimate ~0.11ms for 200 tiles
- Acceptable threshold: <0.5ms for smooth 60fps
- Large selection (1000 tiles) should stay under 1ms

## Future Consideration
If performance becomes an issue:
- Implement spatial culling (already partially done with screenBounds)
- Use RenderTarget2D for caching static previews
- Consider LOD system (simplified at zoom-out levels)
- Instanced rendering for very large selections

## User Preference
Could add config option for overlay style:
- Classic (current)
- Outlined (Option 2)
- Minimal (outline only)
