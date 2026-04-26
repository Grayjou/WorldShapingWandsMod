-# Wand of Delimitation — User Guide

> **Wand Family**: Delimitation
> **Variants**: Instant · Select · Confirm · Stamp
> **Cycle Key**: Right-click the wand in inventory to cycle between variants

---

## Overview

The Wand of Delimitation defines a **constraint area** that filters the operations of all
other wands. When a delimitation area is active, wands like Building, Dismantling, Coating,
etc. will only affect tiles that fall within the delimitation selection — everything outside
is ignored.

Think of it as a stencil or mask: you paint the area you want to work in, and all your
other tools automatically respect that boundary.

---

## Quick Start

1. **Equip** any Wand of Delimitation variant (Instant is the easiest to start with)
2. **Click** to place a shape — this creates a **canvas** (the working region boundary)
3. By default, the first shape also becomes your **selection** (the active constraint)
4. **Equip any other wand** — its operations now only affect tiles inside your delimitation area
5. To clear: open the settings panel → click **Clear All**

---

## Core Concepts

### Canvas

The **canvas** is the outer boundary of your delimitation workspace. It defines the maximum
region where selections can exist. Think of it as the frame of a painting — selections
cannot extend beyond the canvas.

- A white/translucent overlay shows canvas tiles
- If **Auto-Create Canvas** is enabled (default), the first shape you draw becomes the canvas
- In **Canvas Edit Mode**, shapes add to or remove from the canvas itself

### Selection

The **selection** is the active constraint area within the canvas. When another wand operates,
only tiles inside the selection are affected.

- An olive/highlighted overlay shows selected tiles (distinct from canvas tiles)
- Selections are always clipped to the canvas — if the canvas shrinks, selections outside
  the new canvas are automatically removed

### How Other Wands Are Affected

When a delimitation selection is active, **every other wand family** passes its tiles through
`FilterBySelection` before executing. If a wand's operation targets tiles that don't overlap
with the delimitation area, you'll see a warning:

> *"No action executed. Delimitation Area is active and does not overlap with the selection."*

If this happens during continuous channeling (e.g., Stamp mode), the warning appears once
after ~1 second of sustained empty results, then re-arms.

---

## Modes

### Selection Mode (Default)

Shapes you draw are applied to the **tile selection** within the canvas:

| Operation | Effect |
|-----------|--------|
| **Add (Union)** | Adds shape tiles to the selection |
| **Remove (Subtract)** | Removes shape tiles from the selection |
| **Intersect** | Keeps only tiles in both the shape and existing selection |
| **XOR (Toggle)** | Toggles tiles: selected → deselected, deselected → selected |

### Canvas Edit Mode

Toggle this in the settings panel. Shapes now modify the **canvas boundary** instead
of the selection:

| Operation | Effect |
|-----------|--------|
| **Add** | Expands the canvas |
| **Remove** | Shrinks the canvas (selection is clipped to new bounds) |
| **Intersect** | Canvas becomes the intersection of old canvas and shape |
| **XOR** | Toggles canvas tiles |

After any canvas modification, the selection is automatically clipped to stay within
the new canvas bounds.

---

## Variants

| Variant | Clicks | How It Works |
|---------|--------|--------------|
| **Instant** | 1 click | Shape centered on cursor, applied immediately |
| **Select** | 2 clicks | Click start corner → click end corner → area applied |
| **Confirm** | 3 clicks | Click start → click end → see preview → click to confirm or right-click to cancel |
| **Stamp** | 4 clicks | Click start → click end → confirm → stamp repeatedly at new positions |

---

## Settings Panel

### Mode
- **Selection Mode**: Shapes modify the tile selection within the canvas
- **Canvas Edit Mode**: Shapes modify the canvas boundary itself

### Operation
- **Add (Union)**: Combine shape with existing area
- **Remove (Subtract)**: Cut shape away from existing area
- **Intersect**: Keep only the overlap
- **XOR (Toggle)**: Flip the membership of each tile

### Shape
All standard shapes are available:
Rectangle · Ellipse · Diamond · Triangle · Elbow · Cardinal Line · Straight Line

With options:
- **Filled / Hollow**: Solid shape or ring/border only
- **Equal Dimensions**: Force square/circle proportions
- **Slice Mode**: Half-shapes (top/bottom/left/right halves)
- **Connect Diameter**: Connect opposite points for Ellipse/Diamond
- **Thickness**: Border thickness for Hollow shapes

### Actions
- **Clear Selection**: Remove all selected tiles (keeps canvas)
- **Invert Selection**: Toggle all canvas tiles — selected ↔ unselected
- **Clear Canvas**: Remove the canvas entirely (also clears selection)
- **Clear All**: Remove both canvas and selection
- **Teleport to Player**: Move the entire canvas+selection to the player's position

### Auto-Create Canvas
When enabled (default), the first shape drawn in Selection Mode automatically creates
a canvas from that shape, then selects all of it. This saves you from needing to
manually switch to Canvas Edit Mode first.

---

## Configuration

Overlay appearance is controlled via the **Canvas Overlay Config**:

| Setting | Default | Description |
|---------|---------|-------------|
| Outside Dimming Alpha | 0.2 | Dimming of tiles outside the canvas |
| Canvas Fill Alpha | 0.3 | Opacity of the canvas fill |
| Tile Selection Alpha | 0.4 | Opacity of the selection highlight |
| Outside Dimming Color | Black | Color of the dimming overlay |
| Canvas Fill Color | White | Color of the canvas area fill |
| Tile Selection Color | Olive | Color of the selection highlight |

---

## Tips & Tricks

- **Use Delimitation for precise edits**: Paint only one wall in a room by delimiting that wall first
- **Intersect is powerful**: Draw a large delimitation, then use Intersect with a smaller shape to narrow down the area
- **XOR for cleanup**: If you accidentally selected too much, XOR with the excess to toggle it off
- **Watch for the warning**: If other wands stop working, check if you have a delimitation area active
- **Clear All when done**: Delimitation stays active across wand switches — clear it when you're finished
- **Canvas Edit for precision**: Switch to Canvas Edit Mode when you need to expand or shrink the working region

---

## See Also

- **Wand of Molding** — Uses the same canvas/selection system for custom shape creation
- **Common Concepts** — Selection modes, shapes, overlays (coming soon)
