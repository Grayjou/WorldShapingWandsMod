# Wand of Molding — User Guide

> **Wand Family**: Molding
> **Variants**: Instant · Select · Confirm · Stamp
> **Cycle Key**: Right-click the wand in inventory to cycle between variants

---

## Overview

The Wand of Molding lets you design **custom shapes** by painting a mold (a set of tile
offsets), which can then be used as a Stamp template by any other wand family. Instead of
being limited to the built-in shapes (Rectangle, Ellipse, Diamond, etc.), you can sculpt
any arbitrary shape and reuse it.

The mold system uses the same canvas + selection architecture as the Wand of Delimitation,
but with a different purpose: here the "selection" is your mold shape, not a constraint area.

---

## Quick Start

1. **Equip** the Wand of Molding (Instant is the fastest to start)
2. **Click** to draw your first shape — this creates a **canvas** and **mold selection** automatically
3. Use **Add** shapes to build up your custom mold, or **Remove** to carve away parts
4. Once you're satisfied, the mold is **automatically promoted to a Custom Shape**
5. **Equip any other wand in Stamp mode** — it now stamps using your molded shape!

---

## Core Concepts

### Canvas (Working Region)

The **canvas** defines the maximum area where mold cells can exist. A teal/cyan overlay
shows the canvas area, with dimming outside it to make the working region stand out.

- If **Auto-Create Canvas** is enabled (default), the first operation creates the canvas
- Switch to **Canvas Edit Mode** to expand, shrink, or reshape the canvas

### Mold Selection (The Shape)

The **mold selection** is the set of tile offsets that will become your custom shape.
Highlighted tiles within the canvas show what tiles are part of the mold.

- Add shapes to grow the mold
- Remove shapes to carve holes or trim edges
- The mold is always clipped to the canvas — shrinking the canvas removes mold cells outside it

### Custom Shape Promotion

After each operation, the mold selection is **automatically promoted** to a Custom Shape
(the `Mold` entry in the shape dropdown of other wand families). No explicit "Export" or
"Promote" button is needed — the shape is always live and up to date.

When you switch to another wand and select the **Mold** shape type, it uses your current
mold as the stamp template.

---

## Modes

### Selection Mode (Default)

Shapes you draw are applied to the **mold selection** within the canvas:

| Operation | Effect |
|-----------|--------|
| **Add (Union)** | Adds tiles to the mold |
| **Remove (Subtract)** | Removes tiles from the mold |
| **Intersect** | Keeps only tiles in both the new shape and existing mold |
| **XOR (Toggle)** | Toggles tile membership in the mold |

### Canvas Edit Mode

Toggle this in the settings panel. Shapes now modify the **canvas boundary**:

| Operation | Effect |
|-----------|--------|
| **Add** | Expands the canvas |
| **Remove** | Shrinks the canvas (mold is clipped to new bounds) |
| **Intersect** | Canvas becomes the intersection of old canvas and shape |
| **XOR** | Toggles canvas tiles |

---

## Variants

| Variant | Clicks | How It Works |
|---------|--------|--------------|
| **Instant** | 1 click | Shape centered on cursor, applied immediately |
| **Select** | 2 clicks | Click start corner → click end corner → area applied |
| **Confirm** | 3 clicks | Click start → click end → preview → confirm or cancel |
| **Stamp** | 4 clicks | Click start → click end → confirm → stamp repeatedly |

---

## Settings Panel

### Mode
- **Selection Mode**: Shapes modify the mold selection
- **Canvas Edit Mode**: Shapes modify the canvas boundary

### Operation
- **Add (Union)**: Build up the mold / canvas
- **Remove (Subtract)**: Carve away from the mold / canvas
- **Intersect**: Keep only overlapping tiles
- **XOR (Toggle)**: Flip tile membership

### Shape
All standard shapes available for sculpting the mold:
Rectangle · Ellipse · Diamond · Triangle · Elbow · Cardinal Line · Straight Line

With full options: Filled/Hollow, Equal Dimensions, Slice Mode, Thickness, etc.

### Actions
- **Clear Selection**: Remove all mold tiles (keeps canvas)
- **Invert Selection**: Toggle all canvas tiles — molded ↔ unmolded
- **Clear Canvas**: Remove the canvas entirely (also clears mold)
- **Clear All**: Remove both canvas and mold
- **Teleport to Player**: Move the entire canvas + mold to the player's position

---

## Configuration

Overlay appearance is controlled via the **Molding Canvas Overlay** section in Canvas Overlay Config:

| Setting | Default | Description |
|---------|---------|-------------|
| Outside Dimming Alpha | 0.2 | Dimming of tiles outside the canvas |
| Canvas Fill Alpha | 0.4 | Opacity of the canvas area fill |
| Tile Selection Alpha | 0.4 | Opacity of the mold highlight |
| Outside Dimming Color | Black | Color of the dimming overlay |
| Canvas Fill Color | Pale Cyan (200, 255, 255) | Teal theme distinguishes from Delimitation |
| Tile Selection Color | Teal (0, 180, 180) | Mold highlight color |

---

## Contextual Hints

The Wand of Molding provides guidance when an operation produces zero changes:

| Situation | Hint Message |
|-----------|-------------|
| Player is far from canvas (> 30 tiles) | *"No cells added — canvas is far away. Use 'Teleport to Player' to bring it closer."* |
| Near canvas, Selection Mode active | *"No cells added — shape is outside the canvas. Switch to Canvas Edit mode to expand the canvas first."* |
| Tiles already match | *"Mold: no change (tiles already match the current selection)."* |

Hints are rate-limited (one every 3 seconds) to avoid message spam during channeling.

---

## Workflow Example: Creating a Custom House Template

1. **Equip** Wand of Molding (Select mode) in **Canvas Edit Mode**
2. **Draw** a large Rectangle — this creates the canvas workspace
3. Switch to **Selection Mode**
4. **Add** a Rectangle for the walls (the outer shell of the house)
5. **Remove** a smaller Rectangle inside (the interior — hollow it out)
6. **Add** small Rectangles for doorframe, windows
7. Your mold is now a house-shaped template!
8. **Switch** to Wand of Building (Stamp mode), select **Mold** shape
9. **Stamp** the house anywhere in the world — the mold shape is applied

---

## Tips & Tricks

- **Start with Canvas Edit**: Create a generous canvas first, then sculpt the mold inside it
- **Use Teleport**: If you wander far from your mold, use "Teleport to Player" to bring it to you
- **Invert for complex shapes**: Sometimes it's easier to define what you DON'T want, then Invert
- **Multiple operations**: Build up complex shapes by combining Add + Remove + Intersect
- **The mold persists**: Your mold stays active until you clear it — switch between wands freely
- **Canvas = safety net**: The canvas prevents accidental mold expansion. Resize it intentionally in Canvas Edit Mode.

---

## See Also

- **Wand of Delimitation** — Uses the same canvas/selection architecture for tile filtering
- **Common Concepts** — Selection modes, shapes, overlays (coming soon)
