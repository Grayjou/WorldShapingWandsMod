# ImproveGame Wand Detection and Consumption Analysis

## Overview

ImproveGame implements sophisticated detection and consumption logic for tile wands (such as Hive Wand, Living Wood Wand, and custom mod wands) within its SpaceWand system. This analysis examines how the mod identifies wand items, handles their consumption mechanics, and ensures compatibility with both vanilla and modded content.

## Wand Detection Mechanism

### Core Detection Logic

ImproveGame detects tile wands by checking the `Item.tileWand` property:

```csharp
if (item.tileWand >= 0)
{
    // Item is identified as a tile wand
    int actualItemIndex = EnoughItem(player, i => i.type == item.tileWand);
    // ...
}
```

**Key Points:**
- Any item with `tileWand >= 0` is classified as a tile wand
- This includes vanilla wands (Hive Wand, Living Wood Wand, etc.) and custom mod wands
- The detection is not based on general "placeable" classification but specifically on the `tileWand` property

### Relationship to Placeable Items

While the user hypothesized that detection occurs based on placeable classification, the actual implementation is more specific:

- **Placeable items** have `createTile > 0` (can place tiles directly)
- **Tile wands** have `tileWand >= 0` (consume ammo to place tiles)
- The SpaceWand system prioritizes tile wands when available, falling back to direct placeable items

This distinction ensures proper handling of both wand-based and direct placement mechanics.

## Consumption Mechanics

### Wand vs. Ammo Consumption

When using a tile wand, ImproveGame follows vanilla Terraria's consumption logic:

```csharp
// 使用物块魔杖时，ignoreConsumable应为true，无论如何都消耗物品。这是原版的逻辑
bool usingTileWand = false;
if (item.tileWand >= 0)
{
    usingTileWand = true;
    indexOfItemBeingConsumed = actualItemIndex; // Ammo index, not wand index
}
```

**Consumption Behavior:**
- **Wand itself**: Never consumed (wands are typically not consumable)
- **Ammo**: Consumed with `ignoreConsumable = true`
- The system ensures the wand remains in inventory while depleting the appropriate ammo

### Infinite Stack Handling

The consumption logic includes special handling for infinite item stacks:

```csharp
public static int EnoughItem(Player player, Func<Item, bool> condition, int amount = 1)
{
    // ...
    if (!item.consumable)
        return oneIndex; // Returns immediately for non-consumable items
    // ...
}
```

**Infinite Stack Behavior:**
- Non-consumable items (including infinite stacks) are detected as available
- However, the `TryConsumeItem` function with `ignoreConsumable = true` attempts consumption
- In practice, `ItemLoader.ConsumeItem()` may prevent actual consumption of infinite/modded items
- This results in "consumption" attempts that don't reduce stack count for infinite items

## Compatibility with Custom Mod Wands

### Automatic Detection

Custom mod wands work seamlessly with ImproveGame's SpaceWand system because:

1. **Standard Property Usage**: Mods creating tile wands properly set the `tileWand` property
2. **No Hardcoded Lists**: ImproveGame doesn't maintain lists of specific wand items
3. **Runtime Detection**: All detection occurs at runtime based on item properties

### Example Compatibility

```csharp
// Mod wand example
public class CustomTileWand : ModItem
{
    public override void SetDefaults()
    {
        Item.tileWand = ModContent.ItemType<CustomAmmo>();
        Item.createTile = ModContent.TileType<CustomTile>();
        // ... other properties
    }
}
```

Such custom wands are automatically recognized and handled identically to vanilla wands.

## Technical Implementation Details

### Detection Flow

1. `EnoughItem()` scans inventory for items matching placement conditions
2. If found item has `tileWand >= 0`, switches to wand mode
3. Locates corresponding ammo using `item.tileWand` as item type
4. Validates ammo availability
5. Proceeds with placement using wand mechanics

### Consumption Flow

1. Determines consumption target (ammo for wands, item itself for direct placement)
2. Calls `HandleItemConsumption()` with appropriate `ignoreConsumable` flag
3. `TryConsumeItem()` performs the actual consumption
4. Tracks consumed items for UI feedback

### Edge Cases Handled

- **Missing Ammo**: Operation cancels if ammo not found
- **Infinite Stacks**: Detected as available but may not actually consume
- **Mixed Inventory**: Correctly prioritizes appropriate items
- **Mod Interactions**: Respects `ItemLoader.ConsumeItem()` hooks

## Conclusion

ImproveGame's wand detection and consumption system provides robust support for both vanilla and custom tile wands through property-based detection rather than hardcoded lists. The consumption mechanics preserve vanilla behavior while ensuring infinite stacks function correctly. This approach guarantees broad compatibility with modded content without requiring specific integration.