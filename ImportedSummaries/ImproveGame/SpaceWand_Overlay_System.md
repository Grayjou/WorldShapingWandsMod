# Space Wand Overlay System Analysis

## Overview
The Space Wand implements a sophisticated overlay system to preview placement areas before applying changes. This system provides real-time visual feedback showing exactly which tiles will be affected, using different rendering approaches for different wand types.

## Two Overlay Systems

### 1. MarqueeSystem (Space Wand - Shape-based Selections)

#### Architecture
- **Location**: `Common/ModSystems/MarqueeSystem/`
- **Purpose**: Handles rectangular and individual tile highlighting
- **Interface**: `IMarqueeItem` for items that need selection overlays

#### Implementation Details

##### IMarqueeItem Interface
```csharp
public interface IMarqueeItem
{
    bool ShouldDraw { get; set; }
    Rectangle Marquee { get; }
    Color BorderColor { get; }
    Color BackgroundColor { get; }
    
    void PreDrawMarquee(ref bool shouldDraw, Rectangle marquee, Color backgroundColor, Color borderColor);
    void PostDrawMarquee(Rectangle marquee, Color backgroundColor, Color borderColor);
}
```

##### SpaceWand Implementation
- Implements `IMarqueeItem` with properties for drawing state and colors
- `PreDrawMarquee()` calls `MarqueeSystem.DrawSelectedTiles()` instead of drawing a rectangle
- This bypasses the default rectangle drawing for individual tile highlights

#### Drawing Process

1. **Selection Calculation**:
   ```csharp
   // In UseItem_PreUpdate()
   _shouldDrawing = true;
   _borderColor = CanPlaceTiles ? new Color(135, 0, 180) : new Color(250, 40, 80);
   _backgroundColor = _borderColor * 0.35f;
   ```

2. **Tile Selection**:
   - `GetSelectedTiles()` determines which tiles are in the selection based on shape
   - Shapes include: Line, Corner, Square (Empty/Filled), Circle (Empty/Filled)

3. **Individual Tile Highlighting**:
   ```csharp
   // In PreDrawMarquee()
   MarqueeSystem.DrawSelectedTiles(GetSelectedTiles(...), borderColor, backgroundColor);
   shouldDraw = false; // Prevent default rectangle drawing
   ```

4. **MarqueeSystem.DrawSelectedTiles()**:
   - Iterates through selected tile positions
   - Draws colored borders around each individual tile
   - Only renders tiles within screen bounds for performance
   - Uses `SDFRectangle.HasBorder()` for smooth borders

#### Shape Algorithms

##### Circle Selection
```csharp
public static IEnumerable<Point> GetCircleTiles(Vector2 startPoint, Vector2 mousePosition, bool filled, bool lastControlLeft)
{
    var center = startPoint.ToTileCoordinates();
    float radius = (int)(startPoint.Distance(mousePosition) / 16f) + 0.5f;
    float radiusSquared = radius * radius;
    
    for (int i = -(int)radius; i <= radius; i++)
    {
        for (int j = -(int)radius; j <= radius; j++)
        {
            int x = center.X + i, y = center.Y + j;
            if (Vector2.DistanceSquared(new Vector2(x, y), center.ToVector2()) < radiusSquared)
            {
                // Yield tile if in circle
            }
        }
    }
}
```

##### Square Selection
```csharp
public static IEnumerable<Point> GetSquareTiles(Vector2 startPoint, Vector2 mousePosition, bool filled)
{
    var min = Vector2.Min(startPoint, mousePosition).ToTileCoordinates();
    var max = Vector2.Max(startPoint, mousePosition).ToTileCoordinates();
    
    for (int x = min.X; x <= max.X; x++)
    {
        for (int y = min.Y; y <= max.Y; y++)
        {
            if (!filled && (x != min.X && x != max.X && y != min.Y && y != max.Y))
                continue; // Skip interior for empty squares
            yield return new Point(x, y);
        }
    }
}
```

### 2. GameRectangle System (Other Wands - Rectangular Selections)

#### Architecture
- **Location**: `Common/GameRectangle.cs` and `Common/ModSystems/GameRectangleSystem.cs`
- **Purpose**: Manages rectangular selection overlays
- **Used by**: `SelectorItem`-based wands (LiquidWand, StarburstWand, etc.)

#### Implementation Details

##### GameRectangle Class
```csharp
public class GameRectangle
{
    public Rectangle Rectangle;
    public Color BorderColor, BackgroundColor;
    public Func<bool> NeedKill; // Condition to remove the overlay
    public ModItem Parent;
    
    public static int Create(ModItem parent, Func<bool> needKill, 
        Rectangle rectangle, Color backgroundColor, Color borderColor, 
        TextDisplayType textDisplayMode = TextDisplayType.None)
```

##### SelectorItem Integration
- `SelectorItem` manages `start` and `end` tile coordinates
- `DoSelector()` calculates the selection rectangle and creates GameRectangle
- Colors change based on cancellation state

#### Drawing Process

1. **Rectangle Calculation**:
   ```csharp
   // In SelectorItem.DoSelector()
   end = ModifySize(start, Main.MouseWorld.ToTileCoordinates(), SelectRange.X, SelectRange.Y);
   Color color = ModifyColor(!unCancelled);
   GameRectangle.Create(this, IsNeedKill, start, end, color * 0.35f, color, TextDisplayType.All);
   ```

2. **GameRectangleSystem Rendering**:
   - Maintains a list of active rectangles
   - Draws semi-transparent filled rectangles with borders
   - Optionally displays width/height text
   - Removes rectangles when `NeedKill()` returns true

## Performance Optimizations

### Screen Culling
- Both systems only render overlays for tiles/rectangles visible on screen
- Prevents unnecessary drawing for off-screen selections

### Efficient Algorithms
- Circle selection uses squared distance to avoid `Math.Sqrt()`
- Square selection uses simple bounds checking
- Individual tile highlighting vs. full rectangle drawing based on selection type

## Visual Feedback

### Color Coding
- **Purple**: Valid placement area
- **Red**: Invalid placement area
- **Green**: Cancelled selection

### Dynamic Updates
- Overlays update in real-time as mouse moves
- Size and shape change dynamically with cursor position
- Colors reflect current validity state

## Integration with Placement Logic

### Synchronization
- Selection calculations feed directly into placement operations
- `SpaceWandOperation.Proceed()` uses the same tile selection logic
- Ensures preview matches actual placement

### State Management
- Overlays automatically clear when:
  - Item is no longer held
  - Selection is cancelled
  - Placement is executed

## Extensibility

### Adding New Shapes
To add a new shape (e.g., Diamond):
1. Add to `ShapeType` enum
2. Implement `GetDiamondTiles()` method
3. Add case in `GetSelectedTiles()`
4. Create corresponding UI texture

### Custom Overlays
Items can implement `IMarqueeItem` for custom overlay behavior, or use `GameRectangle.Create()` for simple rectangles.

This dual-system approach allows different wand types to use the most appropriate overlay method for their selection patterns, providing clear visual feedback while maintaining good performance.</content>
<parameter name="filePath">c:\Users\RYZEN 9\Documents\Cloned\SummariesAndAnalysis\Analysis\ImproveGame\SpaceWand_Overlay_System.md