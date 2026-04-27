# Wand of Replacement — User Guide

> **Wand Family**: Replacement
> **Variants**: Instant · Select · Confirm · Stamp
> **Cycle Key**: Right-click the wand in inventory to cycle between variants

---

## Overview

The Wand of Replacement is a "find and replace" for terrain. It swaps one type of tile or
wall for another within a shaped area while preserving everything else: surrounding tiles,
slope where possible, paint (if you ask it to), and the build's overall geometry. It's the
right tool when you want to *change* a structure rather than rebuild it.

Common workflows:

- Convert a stone hideout into a wood cabin without losing the layout.
- Swap dirt walls for stone bricks across an entire room.
- Re-grass a corrupted area in one stroke.
- Replace platforms in bulk without picking each one off.

---

## Quick Start

1. **Equip** any Wand of Replacement variant.
2. Open the **settings panel** and pick the **Source object type** (what you're replacing)
   and **Target object type** (what it becomes). Or click **Same Type** to do a like-for-
   like swap (Tile → Tile, Wall → Wall, etc.).
3. Optionally **chose** a specific source and/or target item via right-click in the slot
   buttons (the *InventoryView* picker — see below). Otherwise the wand uses the first
   matching item on your hotbar / inventory.
4. **Click** in the world — every matching source tile inside the shape is replaced.

In **Select**, **Confirm**, and **Stamp** modes, **right-click** during selection to
cancel and clear the preview.

---

## Variants

| Variant | Clicks | How It Works |
|---------|--------|--------------|
| **Instant** | 1 | Shape centred on cursor, applied immediately |
| **Select** | 2 | Click start corner → click end corner → area replaced |
| **Confirm** | 3 | Click start → click end → preview → click to confirm or right-click to cancel |
| **Stamp** | 4 | Click start → click end → confirm → stamp repeatedly at new positions |

---

## Object Types

The replacement wand operates on a **Source → Target** pair. Both sides choose from the
same enum:

| Object Type | Description |
|-------------|-------------|
| **Tile** | Standard solid blocks (dirt, stone, wood, brick, etc.) |
| **Platform** | Platform tiles (wood platform, stone platform, etc.) |
| **Rope** | Rope variants (rope, vine rope, silk rope, web rope) |
| **PlanterBox** | Planter boxes for growing herbs |
| **Air** | Empty space — used as a **target** to erase matching source tiles |
| **Wall** | Background walls |

> **Seeds** and **Rails** were intentionally removed from the Replacement wand UI because
> their substrate / variant interactions don't fit a clean replace pipeline. Use the
> Wand of Building for those.

### Same Type Mode

Clicking **Same Type** in the settings panel locks the target to the source: Tile→Tile,
Wall→Wall, etc. This is the most common workflow ("just swap this material for another
of the same kind") and is set explicitly — it is not inferred from accidental equality.

### Per-object-type chosen items (InventoryView)

Each object type has its own independent **chosen-item** slot for both Source and Target.
That means choosing *Stone* as the Source while in **Tile** mode does NOT carry over when
you switch to **Wall** mode — your Wall-mode source choice is remembered separately.

This isolation was added in the 2026-04-26 InventoryView v1 framework after a bug where
a chosen Solid item would persist into Platform mode and prevent placement. Each sub-mode
now has its own clean slot.

When no chosen item is set, the wand falls back to scanning your hotbar / inventory for
the first matching item — exactly like vanilla bulk-replace mods.

---

## Paint Behaviour

The Wand of Replacement provides two independent paint controls:

### PreservePaint (default ON)

When ON, every replaced tile / wall keeps the **paint colour** it originally had. So if
you replace painted stone with painted wood, the wood inherits the stone's paint.

### Paint Sprayer (tri-state)

A separate **Paint Sprayer** selector chooses where new paint comes from for tiles that
didn't have any paint before:

| State | Behaviour |
|-------|-----------|
| **Off** (default) | No auto-painting. |
| **Inventory** | Pulls and consumes paint from your inventory (vanilla Paint Sprayer parity). |
| **CoatingSettings** | Pulls the paint colour from your **Wand of Coating** settings. **Does not** consume inventory paint. |

### Interaction

`PreservePaint` always wins on **previously painted** tiles. `PaintSprayer` only applies
to tiles that **had no paint** before replacement. This way you can re-paint accents
across a build (PaintSprayer = CoatingSettings) without overwriting deliberately painted
features.

---

## Replacement Algorithm

The wand uses a two-pass approach to maximise compatibility:

1. **`ReplaceTile` (preferred)** — Terraria's native tile-conversion call. Fast,
   preserves paint and coatings automatically, and produces no item drops. The wand
   tries this path first whenever the source and target are compatible.
2. **`KillTile + PlaceTile` (fallback)** — When `ReplaceTile` isn't possible (e.g.,
   incompatible families or special-case tiles), the wand destroys the old tile and
   places the new one. This path may produce an item drop unless `SuppressDrops` is on.

You don't choose between paths; the wand picks automatically per tile.

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
| **SuppressDrops** | Sandbox | OFF | Replaced tiles drop nothing |
| **VacuumItems** | Sandbox | ON | Pulls item drops to player (SP only) |
| **BypassPickaxePower** | Sandbox | OFF | Ignores pickaxe-power gating during fallback Kill+Place |
| **MaxOperationSize** | Limits | 10000 | Cap on tiles per operation |

---

## Client Config Settings

| Setting | Config | Default | Effect |
|---------|--------|---------|--------|
| **EnableWandSounds** | Preferences | ON | Plays the `Item29` completion sound |
| **EnableProgressiveProcessing** | Performance | ON | Visualises large operations in batches (SP only) |

---

## Multiplayer Behaviour

| Aspect | Singleplayer | Multiplayer |
|--------|--------------|-------------|
| Per-tile replacement effects | ✅ | ❌ Silent (engine behaviour) |
| Completion sound + chat message | ✅ | ✅ (via `OperationResult`) |
| Progressive batching | ✅ | ❌ Server processes the whole batch at once |
| Vacuum items | ✅ | ❌ Items scatter |
| Undo (`/undo`) | ✅ | ✅ |

---

## Interactions With Other Systems

### Delimitation Area

When a Wand of Delimitation area is active, the Wand of Replacement only replaces tiles
**inside** the delimitation area. Tiles outside are silently skipped. If your shape lands
entirely outside it, you'll see:

> *"No action executed. Delimitation Area is active and does not overlap with the selection."*

See **[Wand of Delimitation](WandOfDelimitation.md)** for the full picture.

### Undo / Safekeeping

Replacement records the original tile data before each swap, so undo restores the exact
prior state — paint, slope, liquid, and wiring all intact. Works in SP and MP.

### Wand of Coating

When Paint Sprayer is set to **CoatingSettings**, the colour comes from the player's
Wand of Coating's `PaintColor` field. Configure once on the Coating wand, then run
replacements anywhere with consistent paint without burning through inventory paint.

---

## Tips & Tricks

- **Refresh a build's material**: Same Type Mode + Tile, replace stone with marble across
  the whole castle in one stroke.
- **Erase one material only**: Set Source = Tile (any kind), Target = Air, with a chosen
  source item — only that material disappears, leaving everything else.
- **Re-grass corrupted ground**: Source = Tile (corrupt grass), Target = Tile (regular
  grass), Same Type OFF, target chosen as a grass seed analogue. (For mass-seeding workflows
  the Wand of Building's `GrassSeed` mode is often more direct.)
- **Re-paint without re-painting**: PreservePaint ON + Paint Sprayer = CoatingSettings —
  any tile that had no paint receives the Coating wand's colour, while painted features
  stay untouched.
- **Stamp + Mold for templated swaps**: Sculpt a window-frame mold once, then Stamp-
  replace stone with glass within that mold anywhere in the world.

---

## See Also

- **[Wand of Building](WandOfBuilding.md)** — Place new tiles instead of swapping existing ones.
- **[Wand of Coating](WandOfCoating.md)** — Configures the colour used by Paint Sprayer's *CoatingSettings* mode.
- **[Wand of Dismantling](WandOfDismantling.md)** — Destroys tiles instead of swapping them.
- **[Wand of Delimitation](WandOfDelimitation.md)** — Constrain replacement to a precise area.
- **[Wand of Safekeeping](WandOfSafekeeping.md)** — Protect tiles or walls so Replacement can't touch them.
- **[Wand of Molding](WandOfMolding.md)** — Provides the custom Mold shape.
