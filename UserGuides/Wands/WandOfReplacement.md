# Wand of Replacement — User Guide

> **Wand Family**: Replacement
> **Variants**: Instant · Select · Confirm · Stamp
> **Cycle Key**: Right-click the wand in inventory to cycle between variants

---

## Overview

The Wand of Replacement swaps one type of tile or wall for another within a shaped area.
It replaces the existing material with a new one of your choice, preserving the terrain
shape while changing its composition. Think of it as "find and replace" for terrain.

---

## Variants

| Variant | Clicks | How It Works |
|---|---|---|
| **Instant** | 1 click | Click once → shape centered on cursor is replaced immediately |
| **Select** | 2 clicks | Click start corner → click end corner → area replaced |
| **Confirm** | 3 clicks | Click start → click end → see preview → click to confirm or right-click to cancel |
| **Stamp** | 4 clicks | Click start → click end → see preview → confirm → stamp repeatedly at new positions |

### Right-Click Cancel

In **Select**, **Confirm**, and **Stamp** modes, right-click at any time during selection
to cancel the operation and clear the preview.

---

## Settings (Cycled In-Game)

### Object Types

The replacement wand operates on a source → target pair. The selected object type
determines what is replaced and what it becomes.

| Object Type | Description |
|---|---|
| **Tile** | Standard blocks (dirt, stone, wood, etc.) |
| **Platform** | Platform tiles (wood platform, stone platform, etc.) |
| **Rope** | Rope variants (rope, vine rope, silk rope, etc.) |
| **PlanterBox** | Planter boxes for growing herbs |
| **Rail** | Minecart track tiles |
| **Seeds** | Grass seeds, corrupt seeds, etc. (applied to compatible blocks) |
| **Air** | Empty space — erases matching tiles without replacement |
| **Wall** | Background walls (stone wall, wood wall, etc.) |

### How Object Type Works

- **OldObject** (source): The type of object to look for in the selection
- **NewObject** (target): The type of object to replace it with

Example: Setting OldObject=Tile, NewObject=Tile, then selecting stone and having
dirt in your inventory → all stone tiles in the area become dirt tiles.

### Shape Settings

| Setting | Options | Description |
|---|---|---|
| **Shape** | Rectangle · Ellipse · Diamond · Triangle · Elbow · CardinalLine · StraightLine | Geometric shape of the operation area |
| **Fill Mode** | Filled · Hollow | Whether to fill the entire shape or just the outline |
| **Thickness** | 1–50 | Line/outline thickness (for hollow shapes and lines) |
| **Equal Dimensions** | ON/OFF | Forces equal width and height |
| **Slice** | Full · HalfHorizontal · HalfVertical | Cuts the shape in half |

---

## Server Config Settings

| Setting | Default | Effect |
|---|---|---|
| **SuppressDrops** | OFF | When ON, replaced tiles don't drop items |
| **VacuumItems** | ON | Teleports dropped items to the player |
| **BypassPickaxePower** | OFF | Ignores pickaxe power requirements for breaking tiles |
| **MaxOperationSize** | 10000 | Maximum tiles per operation |

---

## Multiplayer Behaviour

| Aspect | Singleplayer | Multiplayer |
|---|---|---|
| Per-tile replacement effects | ✅ Visual feedback | ❌ Silent |
| Completion sound | ✅ Item29 sound | ✅ Item29 sound (via OperationResult) |
| Completion message | ✅ Chat message | ✅ Chat message (via OperationResult) |
| Progressive batching | ✅ Visual batches | ❌ Instant |
| Vacuum items | ✅ Works | ❌ Not yet supported |
| Undo | ✅ Works | ❌ Not yet supported |

---

## Replacement Logic

The wand uses a two-pass approach for maximum compatibility:

1. **ReplaceTile (preferred)**: Uses Terraria's native `WorldGen.ReplaceTile` method,
   which handles tile type conversion without breaking/placing. Faster, preserves
   paint and coatings, and doesn't create item drops.

2. **KillTile + PlaceTile (fallback)**: When `ReplaceTile` isn't possible (e.g.,
   incompatible tile types), the wand falls back to destroying the old tile and
   placing the new one. This path may create item drops.

---

## Tips

1. **Wall replacement**: Set both OldObject and NewObject to **Wall** to replace
   background walls. This is the only way to change walls in bulk.

2. **Selective replacement**: The wand only replaces tiles that match the target
   material. Empty spaces are skipped — no accidental fills.

3. **Material source**: The replacement material is determined by your currently
   selected inventory item. Hold the block you want to place.

4. **Confirm mode for safety**: When replacing near valuable structures, use
   Confirm mode to preview the area before committing.

5. **Seeds on grass**: Use the Seeds object type to spread grass, corruption,
   crimson, or hallow seeds across a large area of compatible blocks.

6. **Air for partial clearing**: Set NewObject to Air to selectively erase tiles
   that match a specific type, leaving others untouched.
