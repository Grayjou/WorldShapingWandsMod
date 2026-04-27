# Wand of Coating — User Guide

> **Wand Family**: Coating (Paint & Coating)
> **Variants**: Instant · Select · Confirm · Stamp
> **Cycle Key**: Right-click the wand in inventory to cycle between variants

---

## Overview

The Wand of Coating is the bulk paint, coating, and moss-management tool. It replaces
Terraria's single-tile paintbrush, paint scraper, and moss harvester with a shaped,
area-of-effect tool. You select a shape, choose what to paint (or scrape), and the wand
applies the change to every tile or wall inside the shape in one stroke.

Use it whenever you want to:

- Re-paint a section of your build in a different colour without going block-by-block.
- Apply Illuminant or Echo coatings across an entire room or facade.
- Strip paint and coatings from large areas you've previously decorated.
- Trim hanging long-moss for decorative drops, or reset stone moss to plain stone.

---

## Quick Start

1. **Equip** the Wand of Coating (Instant is the easiest to start with).
2. Open the **settings panel** (default: hold the wand and press the settings hotkey).
3. Pick a **Mode** (PaintTile / PaintWall / ScrapeMoss / HarvestMoss) and a **Paint Colour**
   (or leave Paint Colour at *Ignore* if you only want to manage Illuminant / Echo).
4. Set the **Illuminant** and **Echo** tri-state toggles to *Apply*, *Remove*, or *Ignore*.
5. **Click** in the world — the shape centred on your cursor is coated immediately.

In **Select**, **Confirm**, and **Stamp** modes, **right-click** during selection to cancel
and clear the preview.

---

## Variants

| Variant | Clicks | How It Works |
|---------|--------|--------------|
| **Instant** | 1 | Shape centred on cursor, applied immediately |
| **Select** | 2 | Click start corner → click end corner → area applied |
| **Confirm** | 3 | Click start → click end → preview → click to confirm or right-click to cancel |
| **Stamp** | 4 | Click start → click end → confirm → stamp the same shape repeatedly |

> Stamp mode for Coating uses a **dedicated repeat cadence** (default `1` tick between
> repeats) so painting feels responsive while held — separate from the slower default
> stamp cadence used by destructive wands.

---

## Coating Modes

The wand has four primary modes selected from the settings panel:

### PaintTile (default)

Applies the configured Paint Colour, Illuminant, and Echo state to **foreground tiles**
(blocks, platforms, ropes, etc.) within the shape.

### PaintWall

Same operation as PaintTile, but targets **background walls** instead.

### ScrapeMoss

Removes **moss variants** from stone tiles by converting them back to plain stone, strips
any paint from those tiles, and drops the moss as an item — matching vanilla Paint Scraper
behaviour. Tiles that don't have a moss variant are skipped.

### HarvestMoss

Trims **long hanging moss** (`TileID.LongMoss`) down to short moss. Drops moss items at the
vanilla 25% per-tile chance. **Does not** affect short moss already on stone tiles — only
the hanging variants.

---

## Paint Colour & Coating Tri-States

The Wand of Coating is built around three independent properties that can each be left
alone, applied, or removed.

| Property | Values | Notes |
|----------|--------|-------|
| **Paint Colour** | `Ignore` (255), `None` (0), or any of the **30 vanilla paints** (1–30) | `Ignore` leaves the existing paint untouched. `None` strips paint to bare. 1–30 apply that colour. |
| **Illuminant** | `Ignore` / `Apply` / `Remove` | Tri-state toggle. |
| **Echo**       | `Ignore` / `Apply` / `Remove` | Tri-state toggle. |

### Tri-state semantics

| State | Result on each tile |
|-------|---------------------|
| **Ignore** | Coating is left **unchanged** — neither added nor removed. |
| **Apply** | Coating is **added** if it isn't already there. |
| **Remove** | Coating is **removed** if it's currently there. |

This independence is the wand's most important feature: you can repaint without touching
existing Illuminant or Echo, or strip a coating without touching paint, etc.

### Common combinations

| Goal | Mode | Paint Colour | Illuminant | Echo |
|------|------|--------------|------------|------|
| Re-paint walls to red, keep coatings | `PaintWall` | Red | Ignore | Ignore |
| Glow-up an entire room | `PaintTile` | Ignore | Apply | Ignore |
| Hide a vault under Echo | `PaintWall` | Ignore | Ignore | Apply |
| Strip everything (paint + coatings) | `PaintTile` | None (0) | Remove | Remove |
| Add Illuminant + Echo, no colour change | `PaintTile` | Ignore | Apply | Apply |

### Repaint toggle

A separate **Repaint** boolean in the settings panel controls how the wand treats tiles
that are *already* painted a different colour:

- **Repaint ON** (default) — already-painted tiles are over-painted with the new colour.
- **Repaint OFF** — already-painted tiles are skipped; only **unpainted** surfaces are
  coloured. Useful for filling in gaps without disturbing existing decoration.

---

## Shape & Selection

All standard shapes are available:

`Rectangle · Ellipse · Diamond · Triangle · Elbow · Cardinal Line · Straight Line`

with the usual options:

- **Filled / Hollow**
- **Equal Dimensions** (force squares / circles)
- **Slice Mode** (top/bottom/left/right halves)
- **Connect Diameter** (connect opposite points for Hollow Ellipse / Diamond)
- **Thickness** (outline / line width)

The **Mold** shape from a Wand of Molding template is also available — paint, scrape, or
coat any custom mold you've sculpted.

---

## Server Config Settings

| Setting | Config | Default | Effect |
|---------|--------|---------|--------|
| **MaxOperationSize** | Limits | 10000 | Maximum tiles per operation |
| **EvilTorchRequiresHardmode** | (n/a) | — | Coating doesn't gate on hardmode; this only affects torches. |

> Coating operations don't destroy tiles, so **SuppressDrops**, **BypassPickaxePower**, and
> the protection toggles do not apply. Paint inventory consumption is governed by Terraria's
> standard rules — paint is consumed per tile unless you've enabled an infinite-paint mode
> in your loadout.

---

## Client Config Settings

| Setting | Config | Default | Effect |
|---------|--------|---------|--------|
| **EnableWandSounds** | Preferences | ON | Plays the `Item109` completion sound after each operation |
| **EnableProgressiveProcessing** | Performance | ON | Visualises large operations in batches (singleplayer only) |
| **Overlay colours** | Overlay / CanvasOverlay | — | Coating uses the family-coloured selection overlay (see **Wand Colours**) |

---

## Multiplayer Behaviour

| Aspect | Singleplayer | Multiplayer |
|--------|--------------|-------------|
| Coating application | ✅ Instant | ✅ Instant (server-side) |
| Completion sound + chat message | ✅ | ✅ (via `OperationResult`) |
| Progressive batching | ✅ | ❌ Server processes all at once |
| Undo (`/undo`) | ✅ | ✅ |

---

## Interactions With Other Systems

### Delimitation Area

If a **Wand of Delimitation** area is active, the Wand of Coating only paints tiles inside
the delimitation selection — exactly like every other wand family. Tiles outside the
delimitation area are silently skipped. If your shape lands entirely outside the
delimitation area, you'll see:

> *"No action executed. Delimitation Area is active and does not overlap with the selection."*

See the **[Wand of Delimitation](WandOfDelimitation.md)** guide for full details.

### Undo / Safekeeping

Every Coating operation is recorded as a **TileSnapshot** in the undo history. Use the
undo hotkey (or `/undo`) to revert the last operation; `/undoinfo` previews the cost.
This works in both singleplayer and multiplayer.

### Building & Replacement Paint Sprayer

The Wand of Building and Wand of Replacement both expose a **Paint Sprayer** tri-state with
a `CoatingSettings` option. When set, those wands pull their paint colour directly from
*this* wand's settings, so configuring the Wand of Coating once paints everything you build
or replace afterwards in the same colour without consuming inventory paint.

---

## Tips & Tricks

- **Strip absolutely everything**: `PaintTile` mode, Paint Colour `None`, Illuminant
  `Remove`, Echo `Remove`. Now you have a one-click "reset to bare" wand.
- **Hidden bases**: `PaintWall` + Echo `Apply` makes background walls effectively
  invisible — perfect for adventure-map secret rooms.
- **Bulk Illuminant lighting**: Apply Illuminant across a ceiling for ambient glow with
  no torches and no spawn-blocking light tiles.
- **Decorate without disturbing**: `Repaint OFF` lets you fill in unpainted tiles only,
  preserving the work you already did.
- **Pair with Delimitation**: Use a Delimitation area to paint exactly one wall of a
  multi-room build without leaking colour into adjacent rooms.
- **Mold for signage**: Sculpt a logo with the Wand of Molding, switch to Wand of
  Coating in Stamp mode, pick the Mold shape — and stamp the logo painted in any colour
  anywhere on the map.

---

## See Also

- **[Wand of Delimitation](WandOfDelimitation.md)** — Constrain coating to a precise area.
- **[Wand of Molding](WandOfMolding.md)** — Provides the custom Mold shape.
- **[Wand of Building](WandOfBuilding.md)** — Auto-paint placed tiles using Coating's settings.
- **[Wand of Replacement](WandOfReplacement.md)** — Auto-paint replaced tiles using Coating's settings.
- **[Wand of Torches](WandOfTorches.md)** — Echo-coat torches as they're placed (shared coating system).
- **[Wand of Safekeeping](WandOfSafekeeping.md)** — Protect coated tiles from accidental edits.
