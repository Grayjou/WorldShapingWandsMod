# Cycling Items Implementation in tModLoader

## Overview
Cycling items are Terraria mod items that change their behavior or appearance when right-clicked in the inventory. This allows players to switch between different modes or variants of the same item without consuming additional inventory space. Examples include momentum eaters that cycle through different absorption strengths.

## Implementation Pattern

### Base Class Structure
Create an abstract base class that inherits from `ModItem` and defines the cycling behavior:

```csharp
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace YourMod
{
    public abstract class BaseCyclingItem : ModItem
    {
        // Abstract methods for mode-specific properties
        public abstract float GetModePercentage();
        public abstract string GetModeName();
        public abstract Color GetModeColor();
        public abstract int GetNextModeItemType();

        public override void SetDefaults()
        {
            // Common item properties
            Item.width = 32;
            Item.height = 32;
            Item.scale = 0.75f;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.value = Item.sellPrice(gold: 2);
            Item.rare = ItemRarityID.LightPurple;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.channel = true;
        }

        public override bool CanRightClick() => true;

        public override void RightClick(Player player)
        {
            // Cycle to next mode by changing item type
            int nextType = GetNextModeItemType();
            int stack = Item.stack;
            bool wasFavorited = Item.favorited;
            
            Item.SetDefaults(nextType);
            Item.stack = stack + 1; // +1 because RightClick consumes one
            Item.favorited = wasFavorited; // Preserve favorite status
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Update item name color
            var nameLine = tooltips.Find(x => x.Name == "ItemName" && x.Mod == "Terraria");
            if (nameLine != null)
            {
                nameLine.OverrideColor = GetModeColor();
            }

            // Add mode information tooltip
            Color modeColor = GetModeColor();
            string hexColor = $"{modeColor.R:X2}{modeColor.G:X2}{modeColor.B:X2}";
            tooltips.Add(new TooltipLine(Mod, "ModeInfo", $"[c/{hexColor}:Mode: {GetModeName()}]"));
        }
    }
}
```

### Concrete Implementation
Create specific item classes that inherit from the base class:

```csharp
namespace YourMod
{
    public class CyclingItem25 : BaseCyclingItem
    {
        public override string Texture => "YourMod/CyclingItem25";

        public override float GetModePercentage() => 0.25f;
        public override string GetModeName() => "25%";
        public override Color GetModeColor() => Color.LightBlue;
        public override int GetNextModeItemType() => ModContent.ItemType<CyclingItem50>();
    }

    public class CyclingItem50 : BaseCyclingItem
    {
        public override string Texture => "YourMod/CyclingItem50";

        public override float GetModePercentage() => 0.50f;
        public override string GetModeName() => "50%";
        public override Color GetModeColor() => Color.Yellow;
        public override int GetNextModeItemType() => ModContent.ItemType<CyclingItem75>();
    }

    // Continue for other modes...
}
```

## Key Components

1. **CanRightClick()**: Must return `true` to enable right-click functionality in inventory.

2. **RightClick(Player player)**: Handles the cycling logic by:
   - Storing current stack and favorite status
   - Setting the item to the next mode using `SetDefaults(nextType)`
   - Restoring stack (adding 1 since right-click consumes one)
   - Preserving favorite status

3. **ModifyTooltips()**: Updates tooltips to reflect the current mode with appropriate colors.

4. **Abstract Methods**: Each mode defines its own properties and the next item type to cycle to.

## Recipes and Registration
Ensure each cycling item has appropriate recipes and is registered in your mod's item loading system.

## Notes
- This pattern follows the "Shellphone" cycling mechanism used in vanilla Terraria.
- Items cycle in a loop (e.g., 25% → 50% → 75% → 100% → 25%).
- The implementation preserves item stack and favorite status across cycles.
- Visual feedback is provided through colored tooltips and item names.

## Example from AeroDynamics Mod
This implementation is based on the MomentumEater items in the AeroDynamics mod, which cycle through different horizontal/total momentum absorption percentages.</content>
<parameter name="filePath">c:\Users\RYZEN 9\Documents\My Games\Terraria\tModLoader\ModSources\AeroDynamics\CyclincItems.md