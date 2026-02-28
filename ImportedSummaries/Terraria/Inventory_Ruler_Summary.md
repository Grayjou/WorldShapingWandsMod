# Inventory Ruler Mod Summary

## Overview
The Inventory Ruler is a hypothetical Terraria mod that extends the measurement functionality of the standard Ruler and LaserRuler tools into the inventory interface. It provides visual overlays and dimension displays for inventory management, allowing players to measure item stack sizes, inventory slot dimensions, and organize items with precision.

## Core Functionality

### How It Works
The Inventory Ruler mod integrates ruler measurement tools directly into the player's inventory UI, providing several key features:

1. **Inventory Grid Overlay**: Displays measurement lines over the inventory slots
2. **Item Dimension Display**: Shows the physical dimensions of items in tiles
3. **Stack Size Visualization**: Visual indicators for item stack counts and capacities
4. **Slot Measurement**: Displays coordinates and dimensions of inventory slots

### Activation and Controls
- Activated via a toggle key (similar to the standard Ruler tools)
- Works in conjunction with existing Ruler/LaserRuler items
- Can be enabled/disabled independently of world measurement tools

## Overlay Creation

### Technical Implementation
The mod hooks into Terraria's `DrawInventory()` method in `Main.cs`, adding overlay rendering after the base inventory drawing is complete.

#### Code Structure
```csharp
// Hook into inventory drawing
public override void PostDrawInterface(SpriteBatch spriteBatch)
{
    if (InventoryRulerEnabled && Main.playerInventory)
    {
        DrawInventoryRulerOverlay(spriteBatch);
    }
}

private void DrawInventoryRulerOverlay(SpriteBatch spriteBatch)
{
    // Draw grid lines between inventory slots
    DrawInventoryGridOverlay(spriteBatch);
    
    // Display dimension text for each slot
    for (int i = 0; i < 10; i++)
    {
        for (int j = 0; j < 5; j++)
        {
            DrawSlotDimensionText(spriteBatch, i, j);
        }
    }
}

private void DrawInventoryGridOverlay(SpriteBatch spriteBatch)
{
    Texture2D rulerTexture = TextureAssets.Extra[68].Value; // Same texture as LaserRuler
    float alpha = 0.4f * GetFadeAlpha(); // Fade based on movement
    
    // Draw L-shaped tiles for each inventory slot
    for (int slotX = 0; slotX < 10; slotX++)
    {
        for (int slotY = 0; slotY < 5; slotY++)
        {
            Vector2 slotPos = GetInventorySlotPosition(slotX, slotY);
            DrawSlotRulerTile(spriteBatch, rulerTexture, slotPos, alpha);
        }
    }
}

private void DrawSlotRulerTile(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float alpha)
{
    Microsoft.Xna.Framework.Rectangle sourceRect;
    Microsoft.Xna.Framework.Color color = new Microsoft.Xna.Framework.Color(0.24f, 0.8f, 0.9f, alpha);
    
    // Draw the L-shaped ruler tile (similar to LaserRuler grid)
    // Top-left corner piece
    sourceRect = new Microsoft.Xna.Framework.Rectangle(0, 0, 16, 16);
    spriteBatch.Draw(texture, position, sourceRect, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
    
    // Additional pieces would be drawn to complete the L-shape
    // This creates the grid pattern around each slot
}

private void DrawSlotDimensionText(SpriteBatch spriteBatch, int slotX, int slotY)
{
    Vector2 slotPosition = GetInventorySlotPosition(slotX, slotY);
    Item item = Main.player[Main.myPlayer].inventory[slotX + slotY * 10];
    
    string dimensionText = GetItemDimensionText(item);
    Vector2 textPosition = slotPosition + new Vector2(2f, 2f); // Offset from top-left
    
    // Draw text using Terraria's font system
    DynamicSpriteFontExtensionMethods.DrawString(
        spriteBatch,
        FontAssets.MouseText.Value,
        dimensionText,
        textPosition,
        Microsoft.Xna.Framework.Color.White * 0.8f,
        0f,
        Vector2.Zero,
        0.7f, // Scale
        SpriteEffects.None,
        0f
    );
}

private string GetItemDimensionText(Item item)
{
    if (item.IsAir) return "";
    
    // Example: show stack size and max stack
    return $"{item.stack}/{item.maxStack}";
}
```

### Visual Components

#### Grid Overlay
- Draws horizontal and vertical lines between inventory slots
- Uses similar color scheme to LaserRuler (cyan/teal)
- Lines fade based on player movement (like standard rulers)

##### Ruler Tiling System
The grid overlay uses an L-shaped tiling pattern similar to the LaserRuler, where each inventory slot is surrounded by ruler lines forming an "L" shape. This creates a precise grid that helps players align items and measure slot dimensions.

**How the L-Shape Tiling Works:**
1. **Tile-Based Drawing**: Each inventory slot gets a ruler tile drawn around it
2. **L-Shape Pattern**: Uses texture pieces that form L-shaped corners and edges
3. **Grid Formation**: When drawn for all slots, creates a complete grid network
4. **Texture Pieces**: Uses `TextureAssets.Extra[68]` with different source rectangles for different parts of the L-shape
5. **Positioning**: Each tile is positioned relative to the inventory slot coordinates

**Visual Result:**
```
┌───┬───┬───┐
│   │   │   │  <- L-shaped lines around each slot
├───┼───┼───┤
│   │   │   │
└───┴───┴───┘
```

This tiling system provides precise visual guides for inventory management, similar to how the LaserRuler helps with world building.

#### Dimension Display
- Shows slot coordinates (e.g., "Slot 3,2")
- Displays item dimensions in tiles (e.g., "4x3 tiles")
- Stack size indicators (e.g., "64/99")

## Dimension Display System

### Item Dimension Calculation
The mod calculates and displays item dimensions based on:

1. **Physical Size**: Width and height in tiles that the item would occupy if placed
2. **Stack Visualization**: Shows current stack vs. max stack with progress bars
3. **Slot Utilization**: Percentage of slot space used by current items

### Display Formats
- **Text Overlay**: Small text labels on slots showing dimensions
- **Color Coding**: Different colors for different item types/sizes
- **Progress Bars**: Visual stack fullness indicators

## Integration with Existing Systems

### Ruler Item Compatibility
- Works with standard Ruler and LaserRuler items
- Can sync measurement units between world and inventory
- Shares toggle states with world rulers

### UI Integration
- Seamlessly integrates with Terraria's inventory UI
- Respects inventory scale settings
- Adapts to different screen resolutions

## Benefits for Players

### Inventory Management
- Precise item organization
- Quick identification of item sizes
- Stack management visualization

### Building Aid
- Direct correlation between inventory items and world placement
- Easier planning of builds using inventory contents

### Accessibility
- Visual aids for players who benefit from measurement tools
- Enhanced inventory navigation

## Technical Considerations

### Performance Impact
- Minimal performance overhead (similar to existing ruler overlays)
- Only renders when inventory is visible and ruler is active

### Mod Compatibility
- Designed to work with inventory-modifying mods
- Hooks into standard drawing methods for broad compatibility

### Customization Options
- Configurable colors and opacity
- Toggleable display elements
- Customizable measurement units

## Future Enhancements

### Potential Features
- Integration with chest inventories
- Recipe measurement previews
- Auto-organization based on dimensions
- Export inventory layouts

## Text Drawing in Terraria

### General Text Rendering System
Terraria uses XNA/MonoGame's SpriteBatch system for all text rendering, with a custom font management system. Text is drawn using the `DrawString` method on the SpriteBatch, but Terraria provides enhanced functionality through `DynamicSpriteFontExtensionMethods.DrawString`.

#### Key Components
1. **Font Assets**: Terraria loads fonts via `FontAssets` (e.g., `FontAssets.MouseText.Value`)
2. **SpriteBatch**: The main rendering batch used for all 2D graphics
3. **Color and Effects**: Support for color tinting, scaling, rotation, and sprite effects
4. **Localization**: Text strings are pulled from `Lang` arrays for multi-language support

#### Basic Text Drawing Syntax
```csharp
DynamicSpriteFontExtensionMethods.DrawString(
    SpriteBatch spriteBatch,      // The rendering batch
    SpriteFont font,              // Font to use (from FontAssets)
    string text,                  // The text to draw
    Vector2 position,             // Screen position
    Color color,                  // Text color (with alpha)
    float rotation,               // Rotation in radians
    Vector2 origin,               // Origin point for rotation/scaling
    float scale,                  // Size multiplier
    SpriteEffects effects,        // Flip effects
    float layerDepth              // Z-depth for layering
);
```

#### Common Parameters
- **Position**: Screen coordinates (top-left origin)
- **Color**: RGBA color with alpha for transparency
- **Scale**: Multiplier for text size (1.0f = normal)
- **Origin**: Pivot point (Vector2.Zero = top-left)
- **LayerDepth**: Controls draw order (0.0f = front)

#### Font Types Available
- `FontAssets.MouseText`: Standard UI text
- `FontAssets.ItemStack`: Small text for stack counts
- `FontAssets.DeathText`: Large dramatic text
- `FontAssets.CombatText`: Combat damage numbers

#### Text Rendering Process
1. **String Preparation**: Get localized text from Lang arrays
2. **Position Calculation**: Determine screen coordinates
3. **Color Determination**: Apply UI colors or custom tints
4. **Draw Call**: Execute DrawString with all parameters
5. **Layering**: Use appropriate layerDepth for proper ordering

#### Performance Considerations
- Text rendering is relatively expensive compared to sprites
- Batch similar text draws together when possible
- Use appropriate font sizes to avoid unnecessary scaling
- Cache frequently used text measurements

This text drawing system provides the foundation for all UI text in Terraria, including inventory labels, tooltips, and interface elements.

This summary provides a comprehensive overview of how an Inventory Ruler mod would function within Terraria's ecosystem, building upon the existing ruler measurement systems while extending them to inventory management.</content>
<parameter name="filePath">c:\Users\RYZEN 9\Documents\TerrariaCoding\AnalysisAndSummaries\Summaries\Inventory_Ruler_Summary.md