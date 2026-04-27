# Wand of Dismantling — User Guide

> **Wand Family**: Dismantling (Destruction)
> **Variants**: Instant · Select · Confirm · Stamp
> **Cycle Key**: Right-click the wand in inventory to cycle between variants

---

## Overview

The Wand of Dismantling destroys tiles, walls, and containers in shaped areas. It is
the primary tool for clearing terrain in bulk — think of it as an area-of-effect pickaxe
that respects geometric shapes rather than a single tile per swing.

It honours pickaxe-power requirements, drops items as a normal pickaxe would, and
cooperates with Terraria's protection rules unless server settings explicitly relax them.
For destructive workflows where you don't want any drops or progress side effects, the
mod's **Carefree Mode** unlocks a special **Void Everything** path that erases tile data
entirely with no kill effects.

---

## Quick Start

1. **Equip** any Wand of Dismantling variant (Instant is fastest; Confirm is safest while
   learning).
2. **Open the settings panel** and choose what to destroy: Tiles, Walls, Containers — or
   any combination.
3. Pick a **Shape** and **Fill Mode** (Filled or Hollow).
4. **Click** in the world — the shape centred on your cursor is dismantled.
5. To undo: press the undo key or run `/undo` in chat.

In **Select**, **Confirm**, and **Stamp** modes, **right-click** during selection to cancel
and clear the preview.

---

## Variants

| Variant | Clicks | How It Works |
|---------|--------|--------------|
| **Instant** | 1 | Shape centred on cursor, applied immediately |
| **Select** | 2 | Click start corner → click end corner → area destroyed |
| **Confirm** | 3 | Click start → click end → preview → click to confirm or right-click to cancel |
| **Stamp** | 4 | Click start → click end → confirm → stamp repeatedly at new positions |

---

## Target Toggles

The settings panel exposes four boolean targets — toggle on whatever you want the wand
to destroy this stroke. They are independent: any combination is legal.

| Toggle | Default | Description |
|--------|---------|-------------|
| **Destroy Tiles** | ✅ ON | Foreground tiles (blocks, platforms, ropes, etc.) |
| **Destroy Walls** | ❌ OFF | Background walls |
| **Destroy Containers** | ❌ OFF | Chests, dressers, weapon racks, item frames, etc. — see below |
| **Void Everything** | ❌ OFF | **Carefree-only** — clears all tile data without kill effects (no drops, no dust, no sound). The fastest erasure path. |

### Container destruction

When **Destroy Containers** is enabled:

- Container contents are **dropped as items** before the tile is destroyed.
- **Locked chests** require the appropriate key in the player's inventory (Golden Key,
  Shadow Key, Temple Key, etc.) — same as smashing them with a pickaxe.
- The server config **`AutoOpenChestsOnDestruction`** (default ON) makes normal chests
  auto-empty before destruction.
- The server config **`IgnoreLockedKeyRequirements`** (default OFF) bypasses the key
  requirement on locked chests entirely.

### Void Everything (Carefree Mode)

`VoidEverything` only does anything when the server's **Carefree Mode** is enabled. It
bypasses the normal kill pipeline and writes empty tile data directly — no item drops,
no dust effects, no kill sounds, no pickaxe-power checks, no liquid spillage. It is the
intended tool for blueprint-style world editing where the cost of cleanup outweighs the
realism of a normal demolition. When Carefree Mode is off, the toggle has no effect; the
wand falls back to the standard destruction path.

---

## Shape & Selection

All standard shapes are available:

`Rectangle · Ellipse · Diamond · Triangle · Elbow · Cardinal Line · Straight Line`

plus the **Mold** shape (any template you've sculpted with the Wand of Molding).

Shape options:

- **Filled / Hollow**
- **Equal Dimensions** (force squares / circles)
- **Slice Mode** (top/bottom/left/right halves)
- **Connect Diameter** (connect opposite points for Hollow Ellipse / Diamond)
- **Thickness** (outline / line width)

---

## Server Config Settings

| Setting | Config | Default | Effect |
|---------|--------|---------|--------|
| **SuppressDrops** | Sandbox | OFF | Destroyed tiles drop nothing |
| **VacuumItems** | Sandbox | ON | Pulls item drops to the player (singleplayer only) |
| **BypassPickaxePower** | Sandbox | OFF | Wand ignores pickaxe-power requirements |
| **ProtectDemonAltars** | Sandbox | ON | Demon/Crimson altars are skipped |
| **ProtectDelicateTiles** | Sandbox | ON | Life crystals, shadow orbs, pots, etc. are skipped |
| **AutoOpenChestsOnDestruction** | Sandbox | ON | Auto-empties normal chests before destruction |
| **IgnoreLockedKeyRequirements** | Sandbox | OFF | Locked chests destroyed without keys |
| **MaxOperationSize** | Limits | 10000 | Cap on tiles per single operation |
| **EnableCarefreeMode** | Carefree | OFF | Master toggle for Carefree-only paths (incl. Void Everything) |
| **CarefreeSuppressDrops** | Carefree | ON | Carefree-mode override for SuppressDrops |
| **CarefreeBypassPickaxePower** | Carefree | ON | Carefree-mode override for BypassPickaxePower |

> Carefree-mode overrides only apply when **EnableCarefreeMode** is ON. They let you keep
> normal-mode rules conservative while a "godmode" pass-through is enabled for sandbox work.

---

## Client Config Settings

| Setting | Config | Default | Effect |
|---------|--------|---------|--------|
| **EnableWandSounds** | Preferences | ON | Plays completion sounds |
| **EnableProgressiveProcessing** | Performance | ON | Visualises large operations in batches (singleplayer only) |

---

## Multiplayer Behaviour

| Aspect | Singleplayer | Multiplayer |
|--------|--------------|-------------|
| Per-tile destruction effects (sound/dust/gore) | ✅ | ❌ Silent — engine behaviour, not a bug |
| Completion sound + chat message | ✅ | ✅ (via `OperationResult`) |
| Progressive batching | ✅ | ❌ Server processes the whole batch at once |
| Locked chest validation | ✅ | ✅ |
| Vacuum items | ✅ | ❌ Items scatter on the ground |
| Undo (`/undo`) | ✅ | ✅ |

> **Why the silence in MP?** Terraria's server never plays tile destruction sounds or
> spawns visual effects — it processes tiles as pure data and broadcasts the resulting
> state. Every mod that does bulk tile operations works the same way.

---

## Interactions With Other Systems

### Delimitation Area

When a **Wand of Delimitation** selection is active, the Wand of Dismantling only
destroys tiles inside the delimitation area. Tiles outside it are silently skipped.
If your shape lands entirely outside the delimitation area, you'll see:

> *"No action executed. Delimitation Area is active and does not overlap with the selection."*

See **[Wand of Delimitation](WandOfDelimitation.md)** for full details.

### Undo / Safekeeping

Every dismantling operation is recorded as a `TileSnapshot` in the undo history. The
snapshot includes destroyed tile types, walls, paint, slope, liquid, and wiring — so
undo restores the area exactly, including drops removed from the world. Both `Undo` and
`Redo` work in singleplayer and multiplayer.

### Container compatibility

The wand's container handling is aware of:

- **Locked chests** (any vanilla locked variant)
- **Dressers**, weapon racks, item frames, food plates, hat racks, mannequin/womannequin
- **Lihzahrd altar** (Temple Key)
- **Shadow chests** (Shadow Key)

Modded containers are supported when they implement the standard `ChestUI` and tile-frame
patterns; report any container that drops contents incorrectly so support can be added.

---

## Tips & Tricks

- **Confirm before you Instant**: While learning, use Confirm mode — you preview every
  shape before committing. Switch to Instant once your aim and settings are second nature.
- **Walls only, no tiles**: Turn off Destroy Tiles and on Destroy Walls to clear back-
  ground without touching foreground geometry.
- **Lines for tunnels**: Cardinal Line snaps to horizontal/vertical; Straight Line allows
  diagonals. Combine with Thickness to dig 1-, 2-, or 3-tall corridors.
- **Stamp for repetition**: Digging evenly-spaced rooms? Stamp mode applies the same shape
  on every click after the initial preview.
- **Carefree blueprints**: Enable Carefree + Void Everything to flatten an area without
  generating any drops or noise — perfect for prepping a megabuild.
- **Protect what matters**: Keep `ProtectDelicateTiles` and `ProtectDemonAltars` on by
  default; flip them off only when you're intentionally clearing a special tile.

---

## See Also

- **[Wand of Delimitation](WandOfDelimitation.md)** — Constrain destruction to a precise area.
- **[Wand of Molding](WandOfMolding.md)** — Provides the custom Mold shape.
- **[Wand of Replacement](WandOfReplacement.md)** — Swap tiles instead of destroying them.
- **[Wand of Building](WandOfBuilding.md)** — The constructive counterpart.
- **[Wand of Wiring](WandOfWiring.md)** — Run **Remove** mode first to recover wires before tearing tiles down.
- **[Wand of Safekeeping](WandOfSafekeeping.md)** — The wand most likely to make you wish you'd protected something earlier.
