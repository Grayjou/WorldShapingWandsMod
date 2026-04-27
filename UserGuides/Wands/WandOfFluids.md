# Wand of Fluids — User Guide

> **Wand Family**: Fluids
> **Variants**: Instant · Select · Confirm · Stamp
> **Cycle Key**: Right-click the wand in inventory to cycle between variants

---

## Overview

The Wand of Fluids fills, drains, and mixes the four vanilla liquids (Water, Lava, Honey,
Shimmer) inside any shaped area, with three different fill algorithms tuned for very
different use cases. It also places **Bubble Blocks** (the trapping tile that holds liquid
in unusual shapes) and supports a **Coat-in-Bubble** containment shell so you can build
levitating pools and lava lakes without leaks.

Use it whenever you need to:

- Fill a swimming pool, fountain, or lava moat without bucket micro-management.
- Drain a flooded basement (with optional per-liquid selectivity).
- Build a sky-high water tank using bubble containment.
- Convert lava + water into obsidian, or honey + lava into Crispy Honey, with the **Mix**
  toggle.
- Rain-fill cavernous regions naturally so liquid follows actual terrain pockets.

---

## Quick Start

1. **Equip** the Wand of Fluids (Instant is fastest).
2. Open the **settings panel**:
   - Pick a **Liquid Type** (Water / Lava / Honey / Shimmer) — or toggle **Place Bubble**
     to place Bubble Blocks instead of liquid.
   - Pick an **Operation** (Fill or Drain).
   - Pick a **Fill Mode** (FullLiquid / RainFill / PocketFill).
3. **Click** to apply the shape.

In **Select**, **Confirm**, and **Stamp** modes, **right-click** during selection to
cancel and clear the preview.

---

## Variants

| Variant | Clicks | How It Works |
|---------|--------|--------------|
| **Instant** | 1 | Shape centred on cursor, applied immediately |
| **Select** | 2 | Click start corner → click end corner → area applied |
| **Confirm** | 3 | Click start → click end → preview → click to confirm or right-click to cancel |
| **Stamp** | 4 | Click start → click end → confirm → stamp repeatedly at new positions |

---

## Liquid Types

| Liquid | Vanilla ID | Notes |
|--------|------------|-------|
| **Water** | 0 | Default; clear blue. |
| **Lava** | 1 | Damages players & enemies; destroys most drops. |
| **Honey** | 2 | Slows entities; provides Honey buff when standing in it. |
| **Shimmer** | 3 | Aether liquid; transmutes items per vanilla rules. |

> Each liquid has its own **overlay colour** — selection previews look like the liquid you
> are about to place, so you always know what's coming. The colour palette is configurable
> in the Overlay config; defaults match Terraria's in-world liquid colours.

---

## Fill Modes

The wand has three fill algorithms. Pick whichever one matches the shape you're working in.

### FullLiquid

Fills **every non-solid tile** within the selection shape. Simple, predictable, and the
fastest. Good for pools, channels, and any closed cavity you've already shaped.

This is the only mode that supports **Place Bubble**, **Mix Liquids**, and **Overwrite
Liquids** — see below.

### RainFill

Seeds rain at the **top of each column** in the selection and lets it fall like real rain.
The water/lava/honey/shimmer raycasts downward and settles into terrain pockets it can
actually reach from above. Good for natural-looking flooding of irregular cavities.

When **`RainFillSummonsClouds`** (Preferences config, default ON) is enabled, RainFill
spawns visible rain-cloud projectiles for the duration of the fill. When
**`RainFillSpawnDusts`** is enabled (default ON), per-liquid dust particles drip to ground
during the fill — they're cosmetic only and free to disable for performance.

### PocketFill

Fills only **sealed enclosed pockets** within the selection — the cavities you would have
to reach by drilling, not the ones already exposed from above. Ideal for spotting and
filling underground voids without spilling into existing chambers.

> RainFill and PocketFill ignore the **Mix Liquids**, **Overwrite Liquids**, and **Place
> Bubble** options — those only apply to FullLiquid. Setting them while another fill mode
> is active silently has no effect.

---

## Operations

### Fill

Adds the chosen liquid to every eligible tile in the shape, according to the active fill
mode. Existing liquid is normally left untouched unless **Overwrite** or **Mix** is on
(see below).

### Drain

Removes liquid from the shape. Two sub-modes:

- **Selective Drain OFF** (default) — drains **all** liquids in the shape regardless of
  type.
- **Selective Drain ON** — drains only the liquid type currently selected in **Liquid
  Type**. Lets you remove water from a mixed-liquid pool without disturbing lava nearby.

> SelectiveDrain shares its icon with the regular drain operation in the current beta;
> per-liquid drain art is on the polish backlog. The behaviour is fully wired.

---

## Special Toggles (FullLiquid only)

### Mix Liquids

When ON, tiles that already contain a *different* liquid are converted to the vanilla
mixing-result tile instead of being overwritten:

| Existing | Pouring | Result |
|----------|---------|--------|
| Water | Lava | **Obsidian** |
| Honey | Water | **Honey Block** |
| Honey | Lava | **Crispy Honey Block** |
| Any | Shimmer | **Aetherium Block** (per vanilla shimmer-mix rules) |

Mix Liquids only acts on the **mixing case** — same-type tiles still receive normal fill
behaviour. Mix Liquids is **mutually exclusive** with Overwrite Liquids; if both are set
the wand uses Mix.

### Overwrite Liquids

When ON, FullLiquid **destructively replaces** an existing different-typed liquid in
target tiles. Pouring water onto lava clears the lava and fills with water — no obsidian.
Useful for cleaning up an area without an explicit drain pass first. Off by default since
the original behaviour (preserve existing liquid) is the safer one.

### Place Bubble

When ON, the wand places **Bubble Blocks** (`TileID.Bubble`) instead of pouring liquid.
Bubble Blocks act as one-way containers — liquid can pass into them but not out — which
makes elevated water tanks and sky-pools possible. Place Bubble works only with FullLiquid;
other modes silently fall back.

### Coat in Bubble

A separate toggle that places a **shell** of Bubble Blocks one tile out from the shape's
exterior before filling it with liquid. Combine with FullLiquid + Fill to build a sealed
levitating pool in one stroke.

---

## Shape & Selection

All standard shapes are available:

`Rectangle · Ellipse · Diamond · Triangle · Elbow · Cardinal Line · Straight Line`

plus the **Mold** shape from any Wand of Molding template.

Shape options: `Filled / Hollow · Equal Dimensions · Slice Mode · Connect Diameter · Thickness`.

---

## Server Config Settings

| Setting | Config | Default | Effect |
|---------|--------|---------|--------|
| **MaxOperationSize** | Limits | 10000 | Cap on tiles per single operation |
| **MaxLiquidsPerOperation** | Limits | (configurable) | Per-operation cap on liquid placements (RainFill/PocketFill share this budget) |

---

## Client Config Settings

| Setting | Config | Default | Effect |
|---------|--------|---------|--------|
| **EnableWandSounds** | Preferences | ON | Plays the splash/pour completion sound |
| **EnableProgressiveProcessing** | Performance | ON | Visualises large operations in batches (SP only) |
| **RainFillSummonsClouds** | Preferences | ON | RainFill spawns rain-cloud projectiles (cosmetic) |
| **RainFillSpawnDusts** | Preferences | ON | RainFill spawns per-liquid dust particles (cosmetic) |
| **Overlay liquid colours** | Overlay | per-liquid | Each liquid type has its own selection overlay colour |

---

## Multiplayer Behaviour

| Aspect | Singleplayer | Multiplayer |
|--------|--------------|-------------|
| Liquid placement | ✅ | ✅ Server-side |
| Bubble placement | ✅ | ✅ Server-side |
| Completion sound + chat message | ✅ | ✅ (via `OperationResult`) |
| Rain cloud / dust visuals | ✅ | ✅ Synced as projectiles / dust packets |
| Progressive batching | ✅ | ❌ Server processes the batch at once |
| Undo (`/undo`) | ✅ | ✅ |

---

## Interactions With Other Systems

### Delimitation Area

When a Wand of Delimitation selection is active, the Wand of Fluids only fills/drains
tiles inside it. Outside tiles are silently skipped. If your shape lands entirely outside
the delimitation area, you'll see the standard *"No action executed"* warning.

### Undo / Safekeeping

Every Fluids operation snapshots prior liquid state per tile. Undo restores the original
liquids exactly, including type and amount. The Bubble placement and Coat-in-Bubble
shells are also part of the snapshot.

### Wand of Coating

The Wand of Coating doesn't paint liquids, but you *can* use it to paint the surrounding
tiles of a pool — coatings stay intact when liquid is added or drained.

---

## Tips & Tricks

- **Sky pools**: FullLiquid + **Coat in Bubble** + a Hollow Ellipse shape = floating
  bubble-walled water feature in one click.
- **Quick obsidian farms**: FullLiquid + **Mix Liquids** ON. Pour water over lava (or
  vice versa) inside a tight rectangle and harvest the obsidian afterwards.
- **Selective cleanups**: Drain + **Selective Drain** ON to remove just water from a
  multi-liquid hazard without disturbing the lava.
- **Naturalistic floods**: RainFill across a wide rectangle over rough terrain — water
  settles into actual pockets exactly where it would in real Terraria rain.
- **Pocket auditing**: PocketFill is great for spotting hidden cavities. If a region
  fills, you've discovered an unexposed pocket — the same pass also seals it.
- **Stamp for fountains**: Stamp mode + a small Mold-shaped pool template = repeatable
  garden fountains across a city build.
- **Conserve performance**: If RainFill feels heavy, turn off `RainFillSummonsClouds` and
  `RainFillSpawnDusts` in Preferences — the placement itself is unchanged.

---

## See Also

- **[Wand of Delimitation](WandOfDelimitation.md)** — Constrain liquid operations to a precise area.
- **[Wand of Building](WandOfBuilding.md)** — Build the basin before you fill it.
- **[Wand of Dismantling](WandOfDismantling.md)** — Carve out a pocket to fill.
- **[Wand of Safekeeping](WandOfSafekeeping.md)** — Protect a finished basin from accidental drains.
- **[Wand of Molding](WandOfMolding.md)** — Provides the custom Mold shape used by Stamp mode.
