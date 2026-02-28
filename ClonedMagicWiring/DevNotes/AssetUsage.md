# Asset Usage Documentation

## Required Assets vs Actual Usage

### Listed in OriginalReadme.md (Required for Build)

The original README listed these assets as required:

```
- Content/Items/WiringWandItem.png (32x32)
- Content/Projectiles/WiringWandProjectile.png (16x16)
- UI/WireRed.png (16x16)
- UI/WireGreen.png (16x16)
- UI/WireBlue.png (16x16)
- UI/WireYellow.png (16x16)
- UI/Actuator.png (16x16)
- UI/Place.png (16x16)
- UI/Remove.png (16x16)
- UI/FilledRect.png (16x16)
- UI/HollowRect.png (16x16)
- UI/FilledDiamond.png (16x16)
- UI/HollowDiamond.png (16x16)
```

### Actual Implementation

**Current Status**: The UI does NOT use PNG assets for wire colors or shape icons.

#### What IS Used:
1. **Content/Items/WiringWandItem.png** (32x32)
   - Used as the item sprite when held in inventory
   - Displayed in hotbar and inventory slots
   - Location: `Content/Items/WiringWandItem.cs`

2. **Content/Projectiles/WiringWandProjectile.png** (16x16)
   - Technically registered but projectile is invisible (`hide = true`)
   - Not actually visible in-game
   - Location: `Content/Projectiles/WiringWandProjectile.cs`

#### What is NOT Used (replaced with text buttons):

The UI system (`UI/WiringWandState.cs`) creates **text-based toggle buttons** instead of using image assets:

```csharp
// Wire buttons use text with tint colors, NOT PNG assets
_redWireButton = MakeToggle("Red Wire", WiringSettings.WireRed, Col1X, currentY);
_redWireButton.TintColor = new Color(200, 50, 50); // Red tint

_greenWireButton = MakeToggle("Green Wire", WiringSettings.WireGreen, Col2X, currentY);
_greenWireButton.TintColor = new Color(50, 200, 50); // Green tint

_blueWireButton = MakeToggle("Blue Wire", WiringSettings.WireBlue, Col1X, currentY);
_blueWireButton.TintColor = new Color(50, 100, 220); // Blue tint

_yellowWireButton = MakeToggle("Yellow Wire", WiringSettings.WireYellow, Col2X, currentY);
_yellowWireButton.TintColor = new Color(220, 200, 50); // Yellow tint

_actuatorButton = MakeToggle("Actuator", WiringSettings.Actuator, Col1X, currentY);
_actuatorButton.TintColor = new Color(200, 200, 200); // Gray tint

// Mode buttons
_placeModeButton = MakeToggle("Place", ...);
_removeModeButton = MakeToggle("Remove", ...);

// Shape buttons
_wireKiteButton = MakeToggle("Wire Kite", ...);
_filledRectButton = MakeToggle("Filled Rect", ...);
_hollowRectButton = MakeToggle("Hollow Rect", ...);
// etc...
```

### Why Text Buttons Instead of Images?

**Advantages:**
1. **No asset dependencies** - Mod works without creating PNG files
2. **Easier localization** - Text can be translated
3. **Simpler maintenance** - No need to create/update multiple icons
4. **Flexible styling** - TintColor allows dynamic color changes
5. **Smaller file size** - No image files to distribute

**Disadvantages:**
1. **Less polished appearance** - Icons could look more professional
2. **Longer button text** - Takes more horizontal space
3. **No visual icons** - Players must read text instead of recognizing icons

### Optional: Adding Icon Support

If you want to add icon support in the future:

1. Create the PNG assets in the UI folder
2. Modify `UIToggleButton.cs` to support optional texture parameter
3. Update `WiringWandState.cs` to load and pass textures:

```csharp
// Example with texture support
private Texture2D _redWireTexture;

public override void OnInitialize()
{
    // Load texture
    if (ModContent.RequestIfExists<Texture2D>("MagicWiring/UI/WireRed", out var asset))
        _redWireTexture = asset.Value;
    
    // Create button with texture
    _redWireButton = MakeToggle(_redWireTexture, WiringSettings.WireRed, ...);
}
```

### Overlay Rendering

The selection overlay uses **TextureAssets.MagicPixel** (built-in Terraria 1px white texture) with color tinting:

```csharp
// Location: Drawing/WiringOverlaySystem.cs
Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, destRect,
    isEdge ? borderColor : fillColor);
```

No custom overlay textures are needed or used.

### Summary

**Required Assets (minimum):**
- Content/Items/WiringWandItem.png (32x32) - Item sprite

**Optional Assets (not currently used):**
- Content/Projectiles/WiringWandProjectile.png (16x16) - Projectile hidden
- UI/*.png - All UI icons replaced with text buttons

**Built-in Textures Used:**
- TextureAssets.MagicPixel - For overlay rendering and UI backgrounds
