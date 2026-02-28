# Space Wand System Analysis

## Overview
The Space Wand is a sophisticated tile manipulation tool in ImproveGame that allows players to select, preview, and place tiles in various shapes and configurations. It features a radial menu system for configuration, real-time visual feedback through overlay systems, and intelligent handling of tile replacement and indestructible objects. The system is designed for large-scale building operations while maintaining safety and performance.

## Core Architecture

### Main Components

#### 1. SpaceWand Item Class
- **Location**: `Content/Items/SpaceWand.cs`
- **Purpose**: Main item implementation handling user interaction, state management, and placement logic
- **Key Features**:
  - Implements `IMarqueeItem` for overlay rendering
  - Implements `IConditionItem` for availability checking
  - Manages selection state and coordinates
  - Handles item consumption and multiplayer synchronization

#### 2. Shape Selection System
- **Location**: `Content/Items/SpaceWand.Shapes.cs`
- **Purpose**: Calculates tile coordinates for different selection shapes
- **Supported Shapes**: Line, Corner, Square (Empty/Filled), Circle (Empty/Filled)

#### 3. Operation System
- **Location**: `Content/Items/SpaceWand.Operations.cs`
- **Purpose**: Handles tile placement, replacement, and destruction logic
- **Key Features**: Safe tile replacement, indestructible tile detection, slope application

#### 4. UI System
- **Location**: `UI/SpaceWand/`
- **Components**: `SpaceWandGUI.cs`, `SelectionButton.cs`, `RoundButton.cs`
- **Purpose**: Radial menu interface for configuring wand settings

#### 5. Overlay Systems
- **MarqueeSystem**: `Common/ModSystems/MarqueeSystem/` - Individual tile highlighting
- **GameRectangleSystem**: `Common/GameRectangle.cs` - Rectangular selection overlays

## Selection Process

### Initialization
The selection process begins when the player successfully calls `CanUseItem()`:

```csharp
if (count > 0)
{
    StartingPoint = Main.MouseWorld.ToTileCoordinates().ToVector2() * 16f;
    return true;
}
```

### Real-time Selection Updates
During `UseItem_PreUpdate()`, the system continuously updates the selection:

```csharp
_shouldDrawing = true;
_borderColor = CanPlaceTiles ? new Color(135, 0, 180) : new Color(250, 40, 80);
_backgroundColor = _borderColor * 0.35f;
```

### Shape-Based Tile Calculation
The `GetSelectedTiles()` method determines affected tiles based on `ShapeType`:

#### Line Selection
```csharp
public static IEnumerable<Point> GetLineTiles(Vector2 startPoint, Vector2 mousePosition)
{
    var direction = GetDirection(startPoint, mousePosition);
    int distanceToMouse = Math.Max(distanceToMouseX, distanceToMouseY);
    var position = startPoint.ToTileCoordinates();

    var increment = direction switch
    {
        Direction.Right => new Point(1, 0),
        Direction.Down => new Point(0, 1),
        // ... other directions
    };

    for (int i = 0; i <= distanceToMouse; i++)
    {
        yield return position;
        position += increment;
    }
}
```

#### Corner Selection
Creates L-shaped paths with horizontal and vertical components, order determined by control key state.

#### Square Selection
```csharp
public static IEnumerable<Point> GetSquareTiles(Vector2 startPoint, Vector2 mousePosition, bool filled)
{
    var startingPoint = startPoint.ToTileCoordinates();
    var nowPoint = mousePosition.ToTileCoordinates();
    var position = PointExtensions.Min(startingPoint, nowPoint);
    var size = (startingPoint - nowPoint).Abs();

    for (int i = 0; i <= size.X; i++)
    {
        for (int j = 0; j <= size.Y; j++)
        {
            if (filled || i == 0 || i == size.X || j == 0 || j == size.Y)
                yield return position + new Point(i, j);
        }
    }
}
```

#### Circle Selection
Uses squared distance calculations for performance:
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
                // Yield tile based on filled/empty logic
            }
        }
    }
}
```

## Block Detection and Item Selection

### PlaceType System
The wand supports six material categories defined by `PlaceType` enum:

```csharp
public enum PlaceType : byte
{
    Platform, Solid, Rope, Rail, GrassSeed, PlantPot
}
```

### Item Matching Conditions
Each `PlaceType` has specific matching criteria:

```csharp
public static Func<Item, bool> GetConditions(PlaceType placeType)
{
    return placeType switch
    {
        PlaceType.Platform => item => item.createTile > -1 && TileID.Sets.Platforms[item.createTile],
        PlaceType.Solid => item =>
            item.createTile > -1 && !Main.tileSolidTop[item.createTile] &&
            Main.tileSolid[item.createTile] && !GrassSeeds.Contains(item.createTile) &&
            TileID.Sets.ClosedDoor != item.createTile,
        PlaceType.Rope => item => item.createTile > -1 && Main.tileRope[item.createTile],
        PlaceType.Rail => item => item.createTile == TileID.MinecartTrack,
        PlaceType.GrassSeed => item => GrassSeeds.Contains(item.createTile),
        PlaceType.PlantPot => item => item.createTile == TileID.PlanterBox,
        _ => _ => false,
    };
}
```

### Inventory Scanning
The `EnoughItem()` method finds suitable items:

```csharp
public static int EnoughItem(Player player, Func<Item, bool> condition, int amount = 1)
{
    int oneIndex = -1;
    int num = 0;
    for (int i = 0; i < 50; i++)
    {
        Item item = player.inventory[i];
        if (item.type != ItemID.None && item.stack > 0 && condition(item))
        {
            if (oneIndex == -1)
                oneIndex = i;
            if (!item.consumable)
                return oneIndex;
            num += item.stack;
        }
    }
    return num >= amount ? oneIndex : -1;
}
```

### Tile Wand Support
For items with `tileWand >= 0`, the system finds the corresponding ammo:
```csharp
if (item.tileWand >= 0)
{
    int actualItemIndex = EnoughItem(player, i => i.type == item.tileWand);
    if (actualItemIndex <= -1)
        return;
    usingTileWand = true;
    indexOfItemBeingConsumed = actualItemIndex;
}
```

## Application and Placement Logic

### Tile Processing Order
Tiles are processed bottom-to-top for physics stability:
```csharp
var sortedPoints = tiles.OrderByDescending(p => p.Y);
ForeachTile(sortedPoints, (x, y) =>
{
    OperateTile(player, x, y, tilesHashSet, PlaceType, BlockType, ref playSound, itemsConsumed);
});
```

### Per-Tile Operation Flow
The `OperateTile()` method handles each tile position:

#### 1. Item Validation
```csharp
int oneIndex = EnoughItem(player, GetConditions(placeType));
if (oneIndex <= -1)
    return;
```

#### 2. Placement Eligibility
```csharp
if (!TileLoader.CanPlace(x, y, item.createTile))
    return;
```

#### 3. Grass Seed Handling
Special case for grass seeds (bypasses replacement logic):
```csharp
if (GrassSeeds.Contains(item.createTile))
{
    if (WorldGen.PlaceTile(x, y, item.createTile, true, false, player.whoAmI, item.placeStyle))
    {
        playSound = true;
        HandleItemConsumption(player, indexOfItemBeingConsumed, itemsConsumed, usingTileWand);
    }
}
```

#### 4. Existing Tile Processing
For positions with existing tiles:

##### Slope Reset
```csharp
if (originalTile.HasTile)
{
    WorldGen.SlopeTile(x, y, noEffects: true);
    if (originalTile.TileType == item.createTile)
    {
        SetSlopeFor(placeType, blockType, x, y, tilesHashSet);
    }
}
```

##### Tile Replacement (when enabled)
```csharp
if (player.TileReplacementEnabled)
{
    if (!ValidTileForReplacement(item, x, y))
    {
        SetSlopeFor(placeType, blockType, x, y, tilesHashSet);
        return;
    }

    if (!player.HasEnoughPickPowerToHurtTile(x, y))
        return;

    if (WorldGen.ReplaceTile(x, y, (ushort)item.createTile, item.placeStyle))
    {
        playSound = true;
        HandleItemConsumption(player, indexOfItemBeingConsumed, itemsConsumed, usingTileWand);
        SetSlopeFor(placeType, blockType, x, y, tilesHashSet);
    }
    else
    {
        TryKillTile(x, y, player);
        if (!originalTile.HasTile && WorldGen.PlaceTile(x, y, item.createTile, true, true, player.whoAmI, item.placeStyle))
        {
            playSound = true;
            HandleItemConsumption(player, indexOfItemBeingConsumed, itemsConsumed, usingTileWand);
            SetSlopeFor(placeType, blockType, x, y, tilesHashSet);
        }
    }
}
```

##### Empty Tile Placement
```csharp
else if (WorldGen.PlaceTile(x, y, item.createTile, true, true, player.whoAmI, item.placeStyle))
{
    playSound = true;
    HandleItemConsumption(player, indexOfItemBeingConsumed, itemsConsumed, usingTileWand);
    SetSlopeFor(placeType, blockType, x, y, tilesHashSet);
}
```

### Slope Application
The `SetSlopeFor()` method applies block shaping:

```csharp
private static void SetSlopeFor(PlaceType placeType, BlockType blockType, int x, int y, HashSet<Point> positions)
{
    var tile = Main.tile[x, y];
    if (WorldGen.CanPoundTile(x, y) && placeType is PlaceType.Solid or PlaceType.Platform)
    {
        tile.BlockType = blockType;
        WorldGen.SquareTileFrame(x, y, false);
    }

    if (placeType is PlaceType.Solid && blockType is BlockType.Solid && !positions.Contains(new Point(x, y + 1)))
        WorldGen.SlopeTile(x, y + 1);
}
```

## Indestructible Tile Handling

### Pick Power Validation
The system uses multiple layers of protection for indestructible tiles:

#### Primary Check
```csharp
if (!player.HasEnoughPickPowerToHurtTile(x, y))
    return;
```

This automatically excludes tiles requiring extremely high pickaxe power:
- **Crimson Hearts** (`TileID.Heart`) - 200 pickaxe power required
- **Shadow Orbs** (`TileID.ShadowOrbs`) - 200 pickaxe power required
- **Other world-critical tiles** with elevated requirements

#### Hammer-Only Tiles
```csharp
if (tile.HasTile && !Main.tileHammer[Main.tile[x, y].TileType])
{
    if (player.HasEnoughPickPowerToHurtTile(x, y))
    {
        // Handle destruction
    }
}
```

#### WorldGen.CanKillTile Validation
```csharp
return player.HasEnoughPickPowerToHurtTile(x, y) && WorldGen.CanKillTile(x, y);
```

### TryKillTile Implementation
The destruction method provides additional safety:

```csharp
public static bool TryKillTile(int x, int y, Player player)
{
    Tile tile = Main.tile[x, y];
    if (tile.HasTile && !Main.tileHammer[tile.TileType])
    {
        if (player.HasEnoughPickPowerToHurtTile(x, y))
        {
            if (TileID.Sets.Grass[tile.TileType] || TileID.Sets.GrassSpecial[tile.TileType] ||
                Main.tileMoss[tile.TileType] || TileID.Sets.tileMossBrick[tile.TileType])
            {
                player.PickTile(x, y, 10000);
            }
            player.PickTile(x, y, 10000);
        }
    }
    return !Main.tile[x, y].HasTile;
}
```

### Graceful Failure Handling
When indestructible tiles are encountered:
1. The operation returns early for that specific tile
2. No error is thrown or displayed
3. Processing continues for remaining tiles in the selection
4. The overall operation completes successfully for placeable tiles

## UI and Configuration System

### Radial Menu Structure
The SpaceWand features a three-page radial menu:

#### Material Page (PlaceType)
- Platform, Solid, Rope, Rail, GrassSeed, PlantPot
- Uses `TextureAssets.Item[]` for icons

#### Slope Page (BlockType)
- SlopeDownRight, SlopeDownLeft, HalfBlock, SlopeUpLeft, SlopeUpRight, Solid
- Uses custom textures from `UI/SpaceWand/{blockType}`

#### Shape Page (ShapeType)
- Line, Corner, CircleFilled, CircleEmpty, SquareEmpty, SquareFilled
- Uses custom textures from `UI/SpaceWand/{shapeType}`

### Page Switching Logic
```csharp
public enum PageType : int
{
    Material, Slope, Shape
}
```

### UI State Management
The `SpaceWandGUI` class handles:
- Visibility and animation states
- Page transitions
- Button interactions
- Tooltip display

## Multiplayer and Networking

### Synchronization Strategy
- **Client Authority**: Placement calculations happen on the client
- **Server Validation**: Server processes placement requests
- **Packet-Based Communication**: Uses `SpaceWandOperation` packets

### Operation Packet Structure
```csharp
public class SpaceWandOperation : NetModule
{
    private Vector2 _startPoint;
    private Vector2 _mousePosition;
    private ShapeType _shapeType;
    private BlockType _blockType;
    private PlaceType _placeType;
    private bool _controlLeft;
    // ... implementation
}
```

### Item Consumption Tracking
```csharp
private static void HandleItemConsumption(Player player, int index, Dictionary<int, int> itemsConsumed, bool ignoreConsumable)
{
    if (index == -1)
        return;

    ref Item item = ref player.inventory[index];
    int type = item.type;
    if (!TryConsumeItem(ref item, player, ignoreConsumable))
        return;

    if (!itemsConsumed.TryAdd(type, 1))
        itemsConsumed[type]++;
}
```

## Performance Optimizations

### Screen Culling
Both overlay systems only render visible tiles:
```csharp
if (x >= screenMinX && x <= screenMaxX && y >= screenMinY && y <= screenMaxY)
{
    // Render tile highlight
}
```

### Efficient Algorithms
- **Squared Distance**: Circle selection uses `DistanceSquared` to avoid `Math.Sqrt()`
- **Bounds Checking**: Square selection uses simple coordinate comparisons
- **Yield Return**: Shape algorithms use lazy evaluation to avoid unnecessary calculations

### Memory Management
- **HashSet for Positions**: `tiles.ToHashSet()` enables O(1) lookups for slope logic
- **Coroutine-Based Sync**: 8-frame interval synchronization reduces network traffic
- **Limited Selection Sizes**: Maximum dimensions prevent excessive calculations

## Configuration Persistence

### Data Serialization
```csharp
public override void LoadData(TagCompound tag)
{
    PlaceType = (PlaceType)tag.GetByte("PlaceType");
    ShapeType = (ShapeType)tag.GetByte("ShapeType");
}

public override void SaveData(TagCompound tag)
{
    tag.Add("PlaceType", (byte)PlaceType);
    tag.Add("ShapeType", (byte)ShapeType);
}
```

## Safety and Validation Systems

### World Bounds Checking
```csharp
if (!WorldGen.InWorld(position.X, position.Y, 10))
    break;
```

### Placement Validation
- `TileLoader.CanPlace()` - Mod compatibility
- `WorldGen.CanPoundTile()` - Slope applicability
- `ValidTileForReplacement()` - Replacement eligibility

### Error Prevention
- **Infinite Loop Protection**: Shape algorithms include iteration limits
- **Null Reference Guards**: Comprehensive null checking
- **Type Validation**: Enum bounds checking

## Integration Points

### Mod Compatibility
- **TileLoader**: Respects mod tile placement rules
- **ModSystems**: Integrates with ImproveGame's system framework
- **UIFramework**: Uses custom UI components

### Terraria Integration
- **WorldGen**: Uses vanilla tile manipulation methods
- **Player**: Leverages existing player properties and methods
- **Item**: Compatible with vanilla and mod items

## Extensibility

### Adding New Shapes
1. Add to `ShapeType` enum
2. Implement `GetNewShapeTiles()` method
3. Add case in `GetSelectedTiles()`
4. Create corresponding UI texture

### Adding New PlaceTypes
1. Extend `PlaceType` enum
2. Add condition in `GetConditions()`
3. Add UI button and texture
4. Update `SetupMaterialPage()`

### Custom Overlay Behavior
Items can implement `IMarqueeItem` for custom preview rendering or use `GameRectangle.Create()` for rectangular overlays.

This comprehensive system provides powerful tile manipulation capabilities while maintaining game balance, performance, and safety through intelligent indestructible tile detection and graceful failure handling.</content>
<parameter name="filePath">c:\Users\RYZEN 9\Documents\Cloned\SummariesAndAnalysis\ImproveGame\Analysis\SpaceWand_System_Analysis.md