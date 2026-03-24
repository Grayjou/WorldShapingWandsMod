# Wand of Coating — User Guide

> **Wand Family**: Coating / Painting
> **Variants**: Instant · Select · Confirm · Stamp
> **Cycle Key**: Right-click the wand in inventory to cycle between variants

---

## Overview

The Wand of Coating applies paint, coatings (Illuminant/Echo), and moss management
to tiles and walls in shaped areas. It replaces Terraria's single-tile paintbrush and
scraper with a bulk area tool.

---

## Variants

| Variant | Clicks | How It Works |
|---|---|---|
| **Instant** | 1 click | Click once → shape centered on cursor is coated immediately |
| **Select** | 2 clicks | Click start corner → click end corner → area coated |
| **Confirm** | 3 clicks | Click start → click end → see preview → click to confirm or right-click to cancel |
| **Stamp** | 4 clicks | Click start → click end → see preview → confirm → stamp repeatedly at new positions |

### Right-Click Cancel

In **Select**, **Confirm**, and **Stamp** modes, right-click at any time during selection
to cancel the operation and clear the preview.

---

## Coating Modes

The wand has four operating modes:

### PaintTile (Default)

Applies the selected paint colour to **foreground tiles** (blocks).

- **Paint Colour** (1–30): Any of Terraria’s 30 paint colours, or 0 for “None” (strips paint)
- **Illuminant** tri-state toggle: Ignore / Apply / Remove
- **Echo** tri-state toggle: Ignore / Apply / Remove
- Use **Ignore** to leave existing coatings unchanged
- Use **Apply** to add the coating
- Use **Remove** to strip the coating

### PaintWall

Same as PaintTile but targets **background walls** instead of foreground tiles.

### ScrapeMoss

Removes moss from stone tiles by converting moss variants (green moss stone,
red moss stone, etc.) back to their base stone type. Also strips any paint
from the affected tiles.

- Only affects tiles that have moss variants
- Drops moss items (matches vanilla scraper behaviour)

### HarvestMoss

Trims **long hanging moss** (LongMoss tile type) down to short moss. Drops moss
items with a 25% chance per tile, matching vanilla scraper behaviour.

- Only targets `TileID.LongMoss` — does NOT affect short moss on stone tiles
- Useful for harvesting decorative hanging moss without removing the base

---

## Settings Summary

| Setting | Used In | Description |
|---|---|---|
| **Mode** | All | PaintTile · PaintWall · ScrapeMoss · HarvestMoss |
| **Paint Colour** | PaintTile, PaintWall | Colour index 0–30 (0 = strip paint) |
| **Illuminant** | PaintTile, PaintWall | Tri-state: Ignore / Apply / Remove |
| **Echo** | PaintTile, PaintWall | Tri-state: Ignore / Apply / Remove |
| **Shape** | All | Rectangle · Ellipse · Diamond · Triangle · Elbow · Lines |
| **Fill Mode** | All | Filled · Hollow |
| **Thickness** | All | Outline/line thickness |

---

## Coating Interaction Table

The Illuminant and Echo tri-state toggles work as follows:

| State | Result |
|---|---|
| **Apply** | Coating is **applied** to all tiles in the selection |
| **Remove** | Coating is **removed** from all tiles in the selection |
| **Ignore** | Coating is **left unchanged** (neither added nor removed) |

This means you can:
- **Add paint + Illuminant without touching Echo**: Set Paint=colour, Illuminant=Apply, Echo=Ignore
- **Strip all coatings**: Set Paint=0, Illuminant=Remove, Echo=Remove
- **Only add Echo**: Set Illuminant=Ignore, Echo=Apply

---

## Server Config Settings

| Setting | Default | Effect |
|---|---|---|
| **MaxOperationSize** | 10000 | Maximum tiles per operation |

> Note: Coating operations don't destroy tiles, so SuppressDrops, BypassPickaxePower,
> and protection settings do not apply.

---

## Multiplayer Behaviour

| Aspect | Singleplayer | Multiplayer |
|---|---|---|
| Coating application | ✅ Instant | ✅ Instant (server-side) |
| Completion sound | ✅ Item109 sound | ✅ Item109 sound (via OperationResult) |
| Completion message | ✅ Chat message | ✅ Chat message (via OperationResult) |
| Progressive batching | ✅ Visual batches | ❌ Instant |
| Undo | ✅ Works | ❌ Not yet supported |

---

## Tips

1. **Strip paint in bulk**: Set Mode to PaintTile, Paint Colour to 0 (None),
   and both Ignore toggles OFF → removes all paint and coatings from tiles.

2. **Hidden bases**: Use Echo coating to make walls and tiles invisible.
   Great for hidden rooms in adventure maps.

3. **Illuminant lighting**: Apply Illuminant coating instead of placing torches
   or light sources. Tiles glow at 100% brightness without any visible light source.

4. **Moss farming**: Use HarvestMoss mode to trim long hanging moss for decorative
   moss items without removing the base moss from stone tiles.

5. **Selective coating**: Use the Ignore toggles to change only paint or only
   coatings without affecting the other. This prevents accidentally stripping
   existing Illuminant/Echo coatings when repainting.

6. **Paint requirement**: You need the appropriate paint items in your inventory
   (just like using a regular paintbrush). The wand consumes paint per tile
   unless the server config makes paint infinite.
