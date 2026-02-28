# Wand of Construction System Analysis

## Overview
The Wand of Construction is a sophisticated structure saving and placement tool in ImproveGame that allows players to capture, preview, and reconstruct complex tile arrangements. It features multiple operational modes, real-time preview rendering, and intelligent material consumption. However, it contains a significant bug where multi-tile objects (paintings, furniture, candles, sinks, light sources) are incorrectly displayed in the preview overlay.

## Core Architecture

### Main Components

#### 1. ConstructWand Item Class
- **Location**: `Content/Items/ConstructWand.cs`
- **Purpose**: Main item implementation inheriting from `SelectorItem`
- **Key Features**:
  - Three operational modes: Save, Place, ExplodeAndPlace
  - Rectangular selection for saving structures
  - File-based structure storage and loading
  - Integration with UI for mode switching

#### 2. QoLStructure Class
- **Location**: `Content/Functions/Construction/QoLStructure.cs`
- **Purpose**: Data structure for storing and parsing tile arrangements
- **Key Features**:
  - TagCompound-based serialization
  - Support for vanilla and mod tiles/walls
  - Origin offset handling for placement positioning
  - Sign text preservation

#### 3. GenerateCore Class
- **Location**: `Content/Functions/Construction/GenerateCore.cs`
- **Purpose**: Handles structure placement logic
- **Key Features**:
  - Coroutine-based placement for performance
  - Separate handling for single tiles, multi-tiles, walls, and wiring
  - Material consumption and inventory management

#### 4. PreviewRenderer Class
- **Location**: `Content/Functions/Construction/PreviewRenderer.cs`
- **Purpose**: Real-time preview overlay system
- **Key Features**:
  - RenderTarget2D-based preview rendering
  - Individual tile highlighting with proper colors and effects
  - **BUG**: Incorrect rendering of multi-tile objects

#### 5. FileOperator Class
- **Location**: `Content/Functions/Construction/FileOperator.cs`
- **Purpose**: File I/O operations for structure saving/loading
- **Features**: JSON-based structure files, caching system

## Operational Modes

### WandSystem.Construct Enum
```csharp
public enum Construct : byte { Save, Place, ExplodeAndPlace }
```

#### Save Mode
- Allows rectangular selection of tile areas
- Captures all tile data, wall data, wiring, actuators, and sign text
- Saves to compressed file format
- Threaded saving operation with progress feedback

#### Place Mode
- Loads saved structure files
- Real-time preview overlay at mouse position
- Coroutine-based placement with material consumption
- Origin offset support for precise positioning

#### ExplodeAndPlace Mode
- Combines destruction and placement
- Removes existing tiles before placing new structure
- Useful for complex replacements

## Selection and Saving Process

### Rectangular Selection
The wand inherits from `SelectorItem` which provides:

```csharp
protected Point SelectRange = new Point(200, 200);
private void DoSelector(Player player)
{
    end = ModifySize(start, Main.MouseWorld.ToTileCoordinates(), SelectRange.X, SelectRange.Y);
    Color color = ModifyColor(!unCancelled);
    GameRectangle.Create(this, IsNeedKill, start, end, color * 0.35f, color, TextDisplayType.All);
}
```

### Structure Capture
When selection is confirmed:

```csharp
var rectangle = new Rectangle(minI, minJ, maxI - minI, maxJ - minJ);
var saveTileThread = new Thread(() =>
{
    FileOperator.SaveAsFile(rectangle);
    SoundEngine.PlaySound(SoundID.ResearchComplete);
});
saveTileThread.Start();
```

### Data Collection
The `QoLStructure` constructor captures:

```csharp
for (int x = rectInWorld.X; x <= rectInWorld.X + rectInWorld.Width; x++)
{
    for (int y = rectInWorld.Y; y <= rectInWorld.Y + rectInWorld.Height; y++)
    {
        Tile tile = Framing.GetTileSafely(x, y);
        // Capture tile type, wall type, frame data, colors, wiring, actuators
        data.Add(new TileDefinition(tileIndex, wallIndex, tile, extraDatas, extraDatas2));
    }
}
```

## Placement Process

### Coroutine-Based Generation
```csharp
public static void GenerateFromTag(TagCompound tag, Point position)
{
    CoroutineSystem.GenerateRunner.Run(Generate(tag, position));
}

public static IEnumerator Generate(TagCompound tag, Point position)
{
    var structure = new QoLStructure(tag);
    yield return KillTiles(structure, position);
    yield return GenerateWalls(structure, position);
    yield return GenerateSingleTiles(structure, position);
    yield return GenerateMultiTiles(structure, position);
    yield return GenerateOutSet(structure, position);
    yield return SquareTiles(structure, position);
    yield return TextSigns(structure, position);
}
```

### Single Tile Placement
```csharp
public static IEnumerator GenerateSingleTiles(QoLStructure structure, Point position)
{
    for (int x = 0; x <= width; x++)
    {
        for (int y = height; y >= 0; y--)
        {
            TileDefinition tileData = structure.StructureDatas[index];
            int tileType = structure.ParseTileType(tileData);

            if (tileType != -1 && !Main.tile[placePosition].HasTile)
            {
                int tileItemType = GetTileItem(tileItemFindType, tileData.TileFrameX, tileData.TileFrameY);
                if (tileItemType != -1)
                {
                    PickItemFromArray(Main.LocalPlayer, inventory, item =>
                        item.type == tileItemType &&
                        TryPlaceTile(placePosition.X, placePosition.Y, item, Main.LocalPlayer, forced: true),
                        true);
                }
            }
        }
    }
}
```

### Multi-Tile Placement
```csharp
public static IEnumerator GenerateMultiTiles(QoLStructure structure, Point position)
{
    for (int x = 0; x <= width; x++)
    {
        for (int y = height; y >= 0; y--)
        {
            TileDefinition tileData = structure.StructureDatas[index];
            int tileType = structure.ParseTileType(tileData);

            var tileObjectData = GetTileData(tileType, tileData.TileFrameX, tileData.TileFrameY);
            if (tileObjectData != null && (tileObjectData.CoordinateFullWidth > 18 ||
                                           tileObjectData.CoordinateFullHeight > 18))
            {
                // Calculate top-left frame coordinates
                int subX = (tileData.TileFrameX / tileObjectData.CoordinateFullWidth) *
                           tileObjectData.CoordinateFullWidth;
                int subY = (tileData.TileFrameY / tileObjectData.CoordinateFullHeight) *
                           tileObjectData.CoordinateFullHeight;

                int tileItemType = GetTileItem(tileType, subX, subY);

                // Only place at origin tile
                Point16 frame = new(subX / 18, subY / 18);
                var origin = tileObjectData.Origin.ToPoint();
                if (frame.X != origin.X || frame.Y != origin.Y)
                    continue;

                // Place multi-tile object
                PickItemFromArray(Main.LocalPlayer, inventory, item =>
                    item.type == tileItemType && _TryPlace(item), true);
            }
        }
    }
}
```

## Preview Overlay System

### RenderTarget2D Architecture
```csharp
internal static RenderTarget2D PreviewTarget;
private void DrawTarget(On_Main.orig_CheckMonoliths orig)
{
    if (PreviewTarget == null)
    {
        var tag = FileOperator.GetTagFromFile(WandSystem.ConstructFilePath);
        if (tag != null)
        {
            int width = tag.GetShort("Width");
            int height = tag.GetShort("Height");
            PreviewTarget = new RenderTarget2D(Main.graphics.GraphicsDevice,
                width * 16 + 20, height * 16 + 20, false, default, default, default,
                RenderTargetUsage.PreserveContents);
        }
    }
    DrawPreviewToRender(PreviewTarget, WandSystem.ConstructFilePath);
}
```

### Drawing Process
```csharp
private static void DrawPreviewToRender(RenderTarget2D renderTarget, string filePath)
{
    var structure = new QoLStructure(filePath);
    DrawBorder(position, (structure.Width + 1) * 16f, (structure.Height + 1) * 16f,
        color * 0.35f, color);
    DrawPreviewFromTag(Main.spriteBatch, structure, position, 1f);
}
```

### Tile Rendering Logic
```csharp
public static bool DrawPreviewFromTag(SpriteBatch sb, QoLStructure structure, Vector2 origin, float scale = 1f)
{
    for (int x = 0; x <= width; x++)
    {
        for (int y = 0; y <= height; y++)
        {
            TileDefinition tileData = data[index];
            int tileType = structure.ParseTileType(tileData);

            if (tileType != -1)
            {
                int tileItemType = GetTileItem(tileType, tileData.TileFrameX, tileData.TileFrameY);
                TileObjectData tileObjectData = null;
                if (tileItemType != -1)
                {
                    tileObjectData = TileObjectData.GetTileData(tileType,
                        MaterialCore.ItemToPlaceStyle[tileItemType]);
                }
                bool multiTile = tileObjectData != null &&
                    (tileObjectData.CoordinateFullWidth > 18 || tileObjectData.CoordinateFullHeight > 18);

                // **BUG**: Multi-tile objects are rendered as single 16x16 tiles
                if (tileData.BlockType != BlockType.HalfBlock || BlockType.Solid && !multiTile)
                {
                    sb.Draw(texture, position * scale, normalTileRect, color, 0f,
                        new Vector2(0f, 8f), scale, spriteEffects, 0f);
                }
                // ... slope and multi-tile rendering continues but incorrectly
            }
        }
    }
}
```

## The Multi-Tile Rendering Bug

### Problem Description
The preview system incorrectly renders multi-tile objects (paintings, furniture, candles, sinks, light sources) as individual 16×16 tiles instead of their proper multi-tile sprites. This creates visual confusion where:

1. **Paintings** (3×2, 2×3) appear as scattered single tiles
2. **Furniture** (chairs, tables, beds) show as disconnected pieces
3. **Light sources** (candles, candelabras, chandeliers) appear malformed
4. **Decorative objects** (sinks, toilets, pianos) look broken

### Root Cause Analysis

#### 1. Incorrect Multi-Tile Detection
```csharp
bool multiTile = tileObjectData != null &&
    (tileObjectData.CoordinateFullWidth > 18 || tileObjectData.CoordinateFullHeight > 18);
```
This correctly identifies multi-tile objects, but the rendering logic doesn't handle them properly.

#### 2. Single-Tile Rendering for All
The preview renders every tile position individually:
```csharp
for (int x = 0; x <= width; x++)
{
    for (int y = 0; y <= height; y++)
    {
        // Draw tile at (x,y) regardless of whether it's part of a multi-tile object
        sb.Draw(texture, position * scale, normalTileRect, color, ...);
    }
}
```

#### 3. Missing Multi-Tile Sprite Logic
Unlike the placement system which only places at origin tiles, the preview draws every constituent tile of multi-tile objects, causing visual artifacts.

#### 4. Frame Coordinate Confusion
The system uses `tileData.TileFrameX/Y` directly for rendering, but multi-tile objects should only render their sprite when at the top-left position, and use the full multi-tile texture.

### Impact
- **Visual Confusion**: Players cannot accurately preview how structures will look
- **Placement Mismatch**: Preview doesn't match actual placement result
- **Usability Issues**: Difficult to position multi-tile objects precisely

## Material Detection Bug: Tile Wand vs Basic Material Selection

### Problem Description
When saving structures containing tiles that can be created by both basic materials and tile wands, the material requirements calculation incorrectly selects tile wands over basic materials. For example:

- A structure with 100 Living Wood blocks shows material requirements as "100 Living Wood Wands" instead of "100 Living Wood"
- This occurs because both Living Wood (item) and Living Wood Wand have `createTile = TileID.LivingWood`

### Root Cause Analysis

#### 1. TileToItem Mapping Construction
In `MaterialCore.PostSetupContent()`, the system builds mappings by iterating through all items:

```csharp
for (int i = 0; i < ItemLoader.ItemCount; i++)
{
    var item = new Item(i);
    if (item.createTile != -1)
    {
        int targetTile = item.createTile;
        ItemToTile.Add(i, targetTile);
        ItemToPlaceStyle.Add(i, item.placeStyle);
        if (TileToItem.ContainsKey(targetTile))
        {
            TileToItem[targetTile].Add(i);  // Both Living Wood and Living Wood Wand get added
        }
        else
        {
            TileToItem.Add(targetTile, new() { i });
        }
    }
}
```

#### 2. Item Selection Logic
The `GetTileItem()` method selects from the list using:

```csharp
return itemTypes.FirstOrDefault(i => 
    (MaterialCore.ItemToPlaceStyle[i] == TileFrameToPlaceStyle(tileType, tileFrameX, tileFrameY) || 
     ItemLoader.IsModItem(i)), -1);
```

For Living Wood tiles, both items have the same `placeStyle` (typically 0), so the selection depends on list order.

#### 3. List Order Dependency
The list order is determined by item ID iteration order. If Living Wood Wand has a lower item ID than Living Wood, it gets selected first.

### Technical Details
- **Living Wood Item**: `ItemID.LivingWood` (creates `TileID.LivingWood`)
- **Living Wood Wand**: Has `tileWand >= 0` and `createTile = TileID.LivingWood` (but consumes Living Wood as ammo)
- **Selection Issue**: Both items have identical `createTile` and `placeStyle` values

### Impact
- **Incorrect Material Requirements**: Players see wrong items in material lists
- **Confusing UI**: Wand requirements instead of basic materials
- **Resource Planning**: Players prepare wrong materials for construction

## Material Consumption System

### Inventory Scanning
```csharp
var inventory = GetAllInventoryItemsList(Main.LocalPlayer, "portable").ToArray();
PickItemFromArray(Main.LocalPlayer, inventory, item =>
    item.type == tileItemType && TryPlaceTile(...), true);
```

### Item Matching Logic
```csharp
public static int GetTileItem(int tileType, int tileFrameX, int tileFrameY)
{
    int getItemTileType = tileType;
    if (MaterialCore.FinishSetup && MaterialCore.TileToItem.TryGetValue(getItemTileType, out List<int> itemTypes))
    {
        return itemTypes.FirstOrDefault(i =>
            (MaterialCore.ItemToPlaceStyle[i] == TileFrameToPlaceStyle(tileType, tileFrameX, tileFrameY) ||
             ItemLoader.IsModItem(i)), -1);
    }
    return -1;
}
```

### PlaceStyle Calculation
The `TileFrameToPlaceStyle` method in `PlaceStyleHelper.cs` handles the complex mapping from frame coordinates to item placeStyle values for different tile types.

## Performance Optimizations

### Coroutine-Based Processing
- Large structures are placed over multiple frames
- Progress tracking with `_taskProcessed` counters
- Yield returns prevent UI freezing

### Caching System
- `FileOperator.CachedStructureDatas` prevents re-parsing
- `RenderTarget2D` reuse for preview rendering
- Texture caching in `GetTileDrawTexture`

### Selective Processing
- Only processes tiles that need placement
- Skips empty tiles and already-placed positions
- Multi-frame distribution for large operations

## Networking and Multiplayer

### Client-Server Architecture
- Structure saving happens client-side
- Placement operations are synchronized
- File I/O is local to each client

### Packet Handling
- Uses existing tile synchronization systems
- NetMessage.SendTileSquare for bulk updates
- Sign text synchronization for placed signs

## Configuration and UI

### Mode Switching
Right-click opens the StructureGUI for mode selection and file management.

### File Management
- Automatic file discovery in save directory
- File validation and error handling
- Progress feedback during operations

## Extensibility

### Mod Support
- Full ModTile and ModWall compatibility
- Dynamic tile type mapping through `QoLStructure.entries`
- Mod-specific placeStyle handling

### Custom Structure Formats
The TagCompound system allows for future extensions with additional metadata.

## Bug Fix Recommendations

### 1. Fix Multi-Tile Preview Rendering
Implement proper multi-tile sprite rendering in `DrawPreviewFromTag`:

```csharp
// Only render at origin tiles for multi-tile objects
if (multiTile)
{
    Point16 frame = new(tileData.TileFrameX / 18, tileData.TileFrameY / 18);
    if (frame.X != tileObjectData.Origin.X || frame.Y != tileObjectData.Origin.Y)
        continue;
    
    // Render full multi-tile sprite
    Rectangle fullRect = new(0, 0, tileObjectData.CoordinateFullWidth, tileObjectData.CoordinateFullHeight);
    sb.Draw(texture, position * scale, fullRect, color, 0f, 
        new Vector2(tileObjectData.Origin.X * 16, tileObjectData.Origin.Y * 16), scale, spriteEffects, 0f);
}
else
{
    // Render single tile as before
    sb.Draw(texture, position * scale, normalTileRect, color, ...);
}
```

### 2. Fix Material Detection: Prefer Basic Materials Over Tile Wands
Modify `GetTileItem()` in `TileHelper.cs` to prioritize basic materials:

```csharp
public static int GetTileItem(int tileType, int tileFrameX, int tileFrameY)
{
    int getItemTileType = tileType;
    if (getItemTileType is TileID.OpenDoor)
    {
        getItemTileType = TileID.ClosedDoor;
    }
    if (MaterialCore.FinishSetup && MaterialCore.TileToItem.TryGetValue(getItemTileType, out List<int> itemTypes))
    {
        int targetPlaceStyle = TileFrameToPlaceStyle(tileType, tileFrameX, tileFrameY);
        
        // First, try to find basic materials (tileWand == -1) that match the placeStyle
        int basicMaterial = itemTypes.FirstOrDefault(i => 
            MaterialCore.ItemToPlaceStyle[i] == targetPlaceStyle && 
            new Item(i).tileWand == -1, -1);
        
        if (basicMaterial != -1)
            return basicMaterial;
        
        // Fallback: find any item that matches the placeStyle
        int anyMatch = itemTypes.FirstOrDefault(i => 
            MaterialCore.ItemToPlaceStyle[i] == targetPlaceStyle, -1);
        
        if (anyMatch != -1)
            return anyMatch;
        
        // Final fallback: first mod item
        return itemTypes.FirstOrDefault(i => ItemLoader.IsModItem(i), -1);
    }
    return -1;
}
```

This fix ensures that for Living Wood tiles, the system will select Living Wood (basic material) over Living Wood Wand (tile wand), providing correct material requirements.

### 2. Improve Frame Coordinate Handling
Ensure consistent frame coordinate calculation between saving, preview, and placement.

### 3. Add Multi-Tile Validation
Add checks to prevent saving incomplete multi-tile objects.

This comprehensive system provides powerful structure manipulation capabilities but requires the multi-tile preview bug to be addressed for optimal usability.</content>
<parameter name="filePath">c:\Users\RYZEN 9\Documents\Cloned\SummariesAndAnalysis\ImproveGame\Analysis\WandOfConstruction_System_Analysis.md