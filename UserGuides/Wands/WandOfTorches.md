# Wand of Torches — User Guide

> **Wand Family**: Torches
> **Variants**: Instant · Select · Confirm · Stamp
> **Cycle Key**: Right-click the wand in inventory to cycle between variants

---

## Overview

The Wand of Torches lays down torches across an entire shape using one of two **tiling
patterns** — Manhattan (diamond lattice) or Grid (rectangular) — at any spacing you set.
It can also **convert**, **replace**, and **remove** torches in bulk, automatically pick
the right **biome torch**, and apply or strip the **Echo coating** in a single pass.

If you've ever wanted to light a giant arena, a multi-floor base, or a sky-island farm
**evenly** without spending fifteen minutes click-spamming torches, this is the wand. The
preview overlay shows you the exact lattice before you commit, the **align-to-existing**
snap keeps new placements in lockstep with the torches you've already placed, and the
**reference-mode** picker lets you choose where the lattice's seed cell sits.

For long, **wall-following torch runs** (caves, tunnels, mountainside trails) see the
sister tool: **[TorchWheelWand](TorchWheelWand.md)**.

---

## Quick Start

1. **Equip** the Wand of Torches and have torches in your inventory or hotbar.
2. Open the **settings panel** (right-click while not selecting, or press `.`):
   - Pick a **Tiling Style** (Manhattan / Grid).
   - Set **Spacing X** and **Spacing Y** (default 5 × 5).
   - Pick a **Mode** (Place / Replace / Remove / Convert).
3. **Click** to apply.
4. The preview overlay highlights every cell that will be torched, with the **seed cell**
   shown in magenta.

In **Select**, **Confirm**, and **Stamp** modes, **right-click** during selection to
cancel and clear the preview.

---

## Variants

| Variant | Clicks | How It Works |
|---------|--------|--------------|
| **Instant** | 1 | Shape centred on cursor, applied immediately on release |
| **Select** | 2 | Click start → click end → applied |
| **Confirm** | 3 | Click start → click end → preview → click to confirm or right-click to cancel |
| **Stamp** | 4 | Click start → click end → confirm → stamp the same lattice repeatedly |

---

## Tiling

Torch positions inside the shape are chosen by a **lattice** seeded at a reference point
and stepped across the selection.

### Tiling Style

| Style | Pattern | Use For |
|-------|---------|---------|
| **Manhattan** (default) | Diamond lattice — every other row offset by half-step | Even visual coverage, organic feel |
| **Grid** | Rectangular lattice — straight rows and columns | Architectural builds where torches need to align with grid features |

### Spacing

| Setting | Default | Range | Effect |
|---------|---------|-------|--------|
| **Spacing X** | 5 | ≥1 | Horizontal distance between lattice cells |
| **Spacing Y** | 5 | ≥1 | Vertical distance between lattice cells |

Smaller spacing = brighter, denser coverage. Larger spacing = scenic / minimum-light
coverage.

### Flip Tiling

| Toggle | Default | Effect |
|--------|---------|--------|
| **Flip Tiling** | OFF | When ON, the Manhattan pattern flips diagonally (offset rows go *up* instead of *down*). Has no effect on Grid (which is symmetric). |

### Reference Mode (seed cell)

The lattice has to start *somewhere*. The Reference Mode picker chooses where:

| Reference Mode | Where the lattice's seed cell sits |
|----------------|------------------------------------|
| **First Valid Tile** (default) | Scan the selection and use the first tile that's a legal torch position |
| **Bbox Top-Left** | The top-left corner of the selection's bounding box |
| **Bbox Top-Right** | The top-right corner |
| **Bbox Bottom-Left** | The bottom-left corner |
| **Bbox Bottom-Right** | The bottom-right corner |
| **First Bbox Click** | The very first click point of the selection |
| **Mouse Position** | The cursor position at the moment of execution |

The seed cell is shown in **magenta** in the preview overlay.

### Align To Existing Torches

| Toggle | Default | Effect |
|--------|---------|--------|
| **Align to Existing Torches** | ON | After the seed is computed, scan the selection for any existing torch and snap the seed onto it — so newly-placed torches stay in lockstep with what's already there |

This makes incremental "extend the lighting" passes painless: the second click lines up
perfectly with the first.

---

## Modes

| Mode | What It Does |
|------|--------------|
| **Place** (default) | Place new torches at every lattice cell that's empty and legal |
| **Replace** | Swap the torch type at every lattice cell that already has one |
| **Remove** | Delete the torch at every lattice cell that has one (drops as item) |
| **Convert** | Change biome variant of every existing torch in the lattice without breaking it |

### Overwrite Torches

| Toggle | Default | Effect |
|--------|---------|--------|
| **Overwrite Torches** | OFF | When ON in **Place** mode, existing torches at lattice cells are replaced. When OFF, existing torches are left alone. |

---

## Biome Torch

| Toggle | Default | Effect |
|--------|---------|--------|
| **Biome Torch** | OFF | Place torches as the **biome-appropriate variant** for each cell (Crimson Torch in Crimson, Coral Torch underwater, Ice Torch in Snow, etc.). Mirrors vanilla **Torch God's Favour** behaviour. |

When **Biome Torch** is ON and the player has the Torch God's Favour, the wand auto-picks
the biome variant per-cell. Without the Favour, the wand falls back to the regular torch
unless the server config relaxes that gate.

---

## Echo Coat (tri-state)

The **Echo Coat** tri-state controls whether placed/replaced torches are Echo-coated:

| State | Result |
|-------|--------|
| **Ignore** (default) | New torches have no coating; replaced torches keep their existing Echo state |
| **Apply** | All affected torches end up Echo-coated (invisible, still emits light) |
| **Remove** | All affected torches end up un-coated (visible torch) |

---

## Source Item Selection

| Setting | Effect |
|---------|--------|
| **Chosen Torch Item Type** | When set via the inventory-view picker, the wand uses *this* torch type as the source. When unset (default), the wand falls back to scanning the hotbar for the first torch stack. |

This lets you carry several torch types and pick which one to lay down without
rearranging your hotbar.

---

## Shape & Selection

All standard shapes are available:

`Rectangle · Ellipse · Diamond · Triangle · Elbow · Cardinal Line · Straight Line · Mold`

with the usual options:

- **Filled / Hollow** + **Thickness**
- **Equal Dimensions**, **Slice Mode**, **Connect Diameter**, **Invert Selection**

The **Mold** shape from a Wand of Molding template is supported — torch any custom mold.

---

## Server Config Settings

These live on `TorchWheelConfig` because they govern torch logic shared with the Torch
Wheel — both wands honour them.

| Setting | Default | Effect |
|---------|---------|--------|
| **EvilTorchRequiresHardmode** | ON | Block evil-biome torches (Ichor / Cursed) before Hardmode |
| **SubstituteCoralTorchPreHardmode** | OFF | If an evil torch is gated by Hardmode, substitute Coral Torch instead |
| **OceanWaterproofTorch** | CoralTorch | Which waterproof torch to use in Ocean biome cells |
| **NonOceanWaterproofTorch** | EvilTorch | Which waterproof torch to use elsewhere when underwater |
| **SmartBiomeTorchLookup** | ON | If you carry the *actual* biome torch in inventory, consume it directly instead of converting a regular torch |

| Setting | Config | Default | Effect |
|---------|--------|---------|--------|
| **MaxOperationSize** | Limits | 10000 | Maximum tiles per operation |

---

## Client Config Settings

| Setting | Config | Default | Effect |
|---------|--------|---------|--------|
| **EnableWandSounds** | Preferences | ON | Plays the completion sound on each operation |
| **EnableProgressiveProcessing** | Performance | ON | Visualises large operations in batches (singleplayer only) |
| **Overlay colours** | Overlay / CanvasOverlay | — | Family colour for selection + magenta seed cell highlight |

---

## Multiplayer Behaviour

| Aspect | Singleplayer | Multiplayer |
|--------|--------------|-------------|
| Torch placement / removal | ✅ Instant | ✅ Server-authoritative |
| Biome torch resolution | ✅ Client-side hint | ✅ Server validates against player's Torch God's Favour |
| Echo coat tri-state | ✅ | ✅ Synced in packet |
| Completion sound + chat message | ✅ | ✅ |
| Progressive batching | ✅ | ❌ Server processes all at once |
| Undo (`/undo`) | ✅ | ✅ |

---

## Interactions With Other Systems

### Delimitation Area

If a **Wand of Delimitation** area is active, lattice cells outside the delimitation area
are silently skipped. The lattice itself is still computed across the full shape, so
spacing stays consistent — only the application is masked.

### Safekeeping

Tiles protected by a **Wand of Safekeeping** are never torched, replaced, or removed by
this wand. The post-operation chat summary reports how many cells were skipped because of
protection.

### Wand of Coating — Echo

The Echo Coat tri-state on this wand and the Echo tri-state on the Wand of Coating use
the same underlying coating system. You can place torches with Echo via this wand, then
later strip Echo with the Wand of Coating (or vice versa).

### Undo

Every torch operation is recorded as a **TileSnapshot** in the per-player undo history
(default 20 steps). `Backspace` or `/undo` reverts; `/undoinfo` previews the cost.

---

## Tips & Tricks

- **Even hall lighting**: Manhattan, 5×5, Place — drop a Rectangle that covers your hall.
  You get an evenly-lit corridor in one click.
- **Architectural lighting**: Switch to Grid tiling when your build has a clear grid (for
  example, every 8 tiles for a tiled floor) — torches will land on column intersections.
- **Extend without misalignment**: Leave **Align to Existing Torches** ON. The next pass
  will snap to your existing lattice automatically.
- **Convert biome on the fly**: Mode = **Convert** + **Biome Torch** ON across an
  existing torch field changes them all to the local biome variant without removing them.
- **Strip Echo from a hidden room**: Mode = **Replace**, **Echo Coat = Remove**, run the
  wand over the area — every torch stays in place but becomes visible again.
- **Sparse minimum-light coverage**: Spacing X = 8, Y = 8, Manhattan, Place — covers the
  largest area per torch while still suppressing spawns.
- **Pair with TorchWheel**: Use this wand for *areas* (rooms, arenas), and the
  TorchWheelWand for *paths* (tunnels, ridges). They share the same torch-conversion
  configs, so behaviour stays consistent.

---

## See Also

- **[TorchWheelWand](TorchWheelWand.md)** — Wall-following torch trail for paths and tunnels.
- **[Wand of Coating](WandOfCoating.md)** — Apply / remove Echo coating without touching the torches themselves.
- **[Wand of Safekeeping](WandOfSafekeeping.md)** — Protect a torched area from accidental removal.
- **[Wand of Delimitation](WandOfDelimitation.md)** — Scope torch operations to a precise area.
- **[Wand of Molding](WandOfMolding.md)** — Provides the custom Mold shape.
