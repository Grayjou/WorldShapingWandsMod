# CalamityMod Dynamic Item Tooltips and Stat Meter Analysis

## Overview
CalamityMod implements a sophisticated dynamic tooltip system, particularly through its **Stat Meter** accessory, which provides real-time player statistics in item tooltips. This system allows players to view comprehensive stat information without needing external UI elements, enhancing gameplay transparency and decision-making.

## The Stat Meter Accessory
The Stat Meter (`CalamityModPublic/Items/Accessories/StatMeter.cs`) is a key item that transforms standard item tooltips into dynamic stat displays. When equipped, it modifies tooltips to show detailed player statistics based on the currently held item and player state.

### Dynamic Tooltip Generation
The Stat Meter uses tModLoader's `ModifyTooltips` hook to dynamically generate tooltip content. The method analyzes:

1. **Held Item Analysis**: Examines the player's currently held item to determine damage type, stats, and applicable modifiers
2. **Player Stat Calculation**: Retrieves real-time player statistics including damage boosts, defense, movement speed, etc.
3. **Conditional Content**: Displays different information based on game mode (Revenge+), location (Abyss), and item type

### Tooltip Structure
The Stat Meter uses a placeholder-based system in its base tooltip text:
- `[REV]`: Revenge mode specific stats (rage/adrenaline damage boosts, durations)
- `[ITEMS]`: Item-specific stats based on held item's damage class
- `[GENERIC]`: General player stats (defense, endurance, life regen, etc.)
- `[ABYSS]`: Abyss-specific debuffs when in the Abyss biome

### Key Features

#### Damage Class Detection
The system intelligently detects the held item's damage class and displays relevant statistics:
- **Melee**: Attack speed, armor penetration
- **Ranged**: Ammo consumption reduction
- **Magic**: Mana cost reduction, mana regen
- **Summon**: Minion/sentry slots, whip range
- **Rogue**: Stealth regen, velocity boosts
- **Tools**: Mining speed, tool range

#### Real-time Stat Calculation
Uses Terraria's stat calculation methods:
- `player.GetTotalDamage(dc)` for damage multipliers
- `player.GetTotalCritChance(dc)` for critical hit chances
- `player.GetTotalAttackSpeed(dc)` for attack speeds
- Custom CalamityPlayer properties for mod-specific stats

## tModLoader Framework Integration

### ModifyTooltips Hook
tModLoader provides the `ModifyTooltips` method in both `ModItem` and `GlobalItem` classes:

```csharp
public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
{
    // Modify the list of tooltip lines
}
```

This hook receives:
- `Item item`: The item whose tooltip is being displayed
- `List<TooltipLine> tooltips`: Collection of tooltip lines that can be added to, removed from, or modified

### TooltipLine Structure
Each tooltip line is a `TooltipLine` object with properties:
- `string Text`: The display text
- `string Name`: Unique identifier for the line
- `string Mod`: Mod that owns the line
- `Color? OverrideColor`: Optional color override

### Global Tooltip Modifications
CalamityMod's `CalamityGlobalItem` (`CalamityModPublic/Items/Calal*.cs`) provides global tooltip modifications for all items:

- **Rarity Color Application**: Custom colors for special items
- **Enchantment Tooltips**: Displays equipped enchantments
- **Hold Shift Tooltips**: Extended information when holding Shift
- **Charge Indicators**: For items with charge mechanics

## Varying Tooltip Colors

### OverrideColor Property
tModLoader allows setting custom colors via the `OverrideColor` property on `TooltipLine` objects:

```csharp
TooltipLine line = new TooltipLine(Mod, "CustomLine", "Colored Text");
line.OverrideColor = Color.Red;
tooltips.Add(line);
```

### CalamityMod Color Implementations

#### Rarity-Based Coloring
Special items receive unique name colors:
- **Developer Items**: Various custom colors (purple, blue, rainbow, etc.)
- **Animated Colors**: Items like The Community use `Main.DiscoR/G/B` for rainbow effects
- **Pulsing Colors**: `CalamityUtils.ColorSwap()` creates alternating colors
- **Dynamic Colors**: Some items use time-based color calculations

#### Functional Coloring
Different tooltip elements use colors for clarity:
- **Gray Text**: `new Color(170, 170, 170)` for secondary information
- **Hold Shift Indicators**: Custom colors defined by `IHoldShiftTooltipItem` interface
- **Flavor Text**: Special colors for lore or descriptive text

#### Color Utility Functions
CalamityMod provides utility functions for dynamic colors:
- `CalamityUtils.ColorSwap(Color a, Color b, float time)`: Alternates between two colors
- `CalamityUtils.MulticolorLerp(float time, Color[] colors)`: Interpolates through multiple colors
- Time-based calculations using `Main.GlobalTimeWrappedHourly`

## Technical Implementation Details

### Stat Calculation Precision
The Stat Meter displays stats with appropriate decimal places:
- Damage percentages: 2 decimal places (`n2`)
- General percentages: 1 decimal place (`n1`)
- Flat values: 1 decimal place for readability

### Performance Considerations
- Calculations are performed only when tooltips are displayed
- Uses cached player references (`Main.LocalPlayer`)
- Avoids expensive operations in tooltip generation

### Localization Support
All dynamic text uses localization keys, allowing for full translation support:
- `this.GetLocalization("DamageStats").Format(...)`
- Supports multiple languages through tModLoader's localization system

## Conclusion
CalamityMod's dynamic tooltip system demonstrates advanced use of tModLoader's modding capabilities, providing players with comprehensive stat information through an elegant UI integration. The combination of real-time stat calculation, conditional content display, and rich color customization creates an immersive and informative player experience.</content>
<parameter name="filePath">c:\Users\RYZEN 9\Documents\Cloned\SummariesAndAnalysis\CalamityMod\Analysis\CalamityMod_Dynamic_Item_Tooltips_Analysis.md