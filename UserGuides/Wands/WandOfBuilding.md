# Wand of Building — User Guide

> **Wand Family**: Building (Construction)
> **Variants**: Instant · Select · Confirm · Stamp
> **Cycle Key**: Right-click the wand in inventory to cycle between variants

---

## Overview

The Wand of Building is the primary construction tool — it places tiles, walls, platforms,
ropes, rails, planter boxes, and grass seeds in shaped areas. It is the constructive
counterpart to the Wand of Dismantling and the natural partner to the Wand of Replacement.

It places real items from your inventory (consuming them by default), respects vanilla
placement rules where it can, and adds three power-tools that vanilla doesn't have:

- **Slope** control — every placed tile can be auto-sloped to a chosen corner.
- **Actuation** tri-state — placed tiles can be pre-actuated, de-actuated, or left alone.
- **Paint Sprayer** tri-state — automatic painting from inventory or from your Coating
  wand's settings, no extra clicks.

---

## Quick Start

1. **Equip** any Wand of Building variant.
2. Open the **settings panel** and pick an **Object Type** (Solid / Platform / Rope / Rail /
   GrassSeed / PlantPot / Wall / Torch).
3. Optionally **chose** a specific source item via right-click in the slot button (the
   *InventoryView* picker — see below). Otherwise the wand uses the first matching item on
   your hotbar / inventory.
4. **Click** to apply the shape — the chosen item is placed inside.

In **Select**, **Confirm**, and **Stamp** modes, **right-click** during selection to
cancel and clear the preview.

---

## Variants

| Variant | Clicks | How It Works |
|---------|--------|--------------|
| **Instant** | 1 | Shape centred on cursor, applied immediately |
| **Select** | 2 | Click start corner → click end corner → area placed |
| **Confirm** | 3 | Click start → click end → preview → click to confirm or right-click to cancel |
| **Stamp** | 4 | Click start → click end → confirm → stamp repeatedly at new positions |

---

## Object Types

| Object Type | Description |
|-------------|-------------|
| **Solid** | Standard solid blocks (dirt, stone, wood, brick, etc.). Default. |
| **Platform** | Platform tiles (wood platform, stone platform, etc.) |
| **Rope** | Rope variants (rope, vine rope, silk rope, web rope) |
| **Rail** | Minecart track tiles |
| **GrassSeed** | Grass seeds; requires a compatible substrate (dirt, mud, etc.) |
| **PlantPot** | Planter boxes (substrate for herbs) |
| **Wall** | Background walls |
| **Torch** | Torch placement — biome-aware via the Wand of Torches (use that wand for full torch features) |

### Per-object-type chosen items (InventoryView)

Each object type has its own independent **chosen-item** slot. Choosing *Stone* in **Solid**
mode does NOT carry over when you switch to **Platform** mode — the platform mode keeps
its own choice (or none). Walls have their own dedicated slot too.

This isolation was added in the 2026-04-26 InventoryView v1 framework after a bug where
a chosen Solid item would persist into Platform mode and prevent placement entirely.

When no chosen item is set, the wand falls back to scanning your hotbar / inventory for
the first item that matches the active object type.

---

## Slope Control

The **Slope** setting determines what shape each placed tile takes. Slopes are applied
at placement time using Terraria's own slope flags, so they're identical to picking up a
hammer afterwards — but in bulk and aligned with your shape.

| Slope | Description |
|-------|-------------|
| **Default** | Full square block (no slope). |
| **VerticalHalf** | Half-height tile (a "half-block"). |
| **BottomRight** | Bottom-right corner raised (slope `/`). |
| **BottomLeft** | Bottom-left corner raised (slope `\`). |
| **TopRight** | Top-right corner raised. |
| **TopLeft** | Top-left corner raised. |

### OverwriteSlope toggle

| State | Behaviour |
|-------|-----------|
| **OverwriteSlope ON** (default) | The chosen Slope is **always applied** — placed tiles AND tiles that get re-placed receive the slope. |
| **OverwriteSlope OFF** | Existing tiles **keep their current slope**; only freshly-placed tiles get the default slope. |

OverwriteSlope OFF is what you want when you're filling in gaps in a hand-shaped area
without flattening the artist's slopework.

---

## Actuation Tri-State

Every placed tile can be pre-configured for actuation:

| State | Result on each placed tile |
|-------|----------------------------|
| **Ignore** (default) | Actuation state left unchanged for re-placed tiles; new tiles get vanilla default (not actuated). |
| **Actuate ON** | The placed tile is set to actuated (pass-through). |
| **Actuate OFF** | The placed tile is forced to solid / de-actuated. |

This is the fastest way to wire a hidden door, secret floor, or trap — place the entire
mechanism in one shape, set Actuation to ON, and the tiles spawn already prepared for a
toggling lever.

---

## Paint Sprayer (Tri-State)

The Wand of Building can auto-paint surfaces it places. The **Paint Sprayer** selector
chooses where the paint comes from:

| State | Behaviour |
|-------|-----------|
| **Off** (default) | No auto-painting. |
| **Inventory** | Pulls and consumes paint from your inventory (vanilla Paint Sprayer parity). |
| **CoatingSettings** | Pulls the paint colour from your **Wand of Coating** settings. **Does not** consume inventory paint. |

`CoatingSettings` mode is the recommended workflow once you've configured a Wand of
Coating: set your colour once and every Building stroke uses it consistently without
burning through paint stacks.

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
| **VacuumItems** | Sandbox | ON | Pulls item drops to player when fallback paths produce drops (rare for Building) |

> Building consumes inventory items by default — there is no SuppressDrops counterpart for
> the constructive direction. Carefree Mode does not add a "free placement" toggle in v1.0.0;
> if you want infinite-material editing, use a creative mod like Cheat Sheet alongside.

---

## Client Config Settings

| Setting | Config | Default | Effect |
|---------|--------|---------|--------|
| **EnableWandSounds** | Preferences | ON | Plays the placement completion sound |
| **EnableProgressiveProcessing** | Performance | ON | Visualises large operations in batches (SP only) |

---

## Multiplayer Behaviour

| Aspect | Singleplayer | Multiplayer |
|--------|--------------|-------------|
| Tile placement | ✅ | ✅ Server-side |
| Wall placement | ✅ | ✅ Server-side |
| Slope flag application | ✅ | ✅ |
| Actuation flag application | ✅ | ✅ |
| Paint Sprayer (Inventory) | ✅ | ✅ |
| Paint Sprayer (CoatingSettings) | ✅ | ✅ |
| Completion sound + chat message | ✅ | ✅ (via `OperationResult`) |
| Progressive batching | ✅ | ❌ Server processes the batch at once |
| Undo (`/undo`) | ✅ | ✅ |

> **Server authority.** Building packets are processed server-side: the server validates
> inventory, applies slopes, actuation, and paint, and broadcasts the result. Building
> from a client without the right items in inventory simply produces nothing — the client
> never desyncs.

---

## Interactions With Other Systems

### Delimitation Area

When a Wand of Delimitation selection is active, the Wand of Building only places tiles
inside it. Tiles outside are silently skipped. If your shape lands entirely outside the
delimitation area, you'll see the standard *"No action executed"* warning.

### Wand of Coating (Paint Sprayer source)

When Paint Sprayer is set to **CoatingSettings**, the colour comes from the player's
Wand of Coating's `PaintColor` field. This is the cleanest workflow for keeping a project
colour-consistent without managing paint inventory.

### Wand of Replacement

Building places new material on empty space; Replacement swaps existing material. If
you find yourself trying to overwrite an existing tile with Building, the Replacement
wand is almost always the right tool instead.

### Wand of Molding

The Wand of Molding produces the **Mold** custom shape. With Building in Stamp mode and
the Mold shape selected, you can stamp a hand-sculpted shape (a custom house silhouette,
a logo, an arch) anywhere in the world.

### Undo / Safekeeping

Every Building operation records the prior tile state. Undo removes the placed tiles and
restores whatever was there before — nothing is permanently lost.

---

## Tips & Tricks

- **Slope a roofline in one click**: Pick a Triangle shape, set Slope = `BottomRight` (or
  `BottomLeft`), and click — the wand auto-slopes the diagonal edge cleanly.
- **Hidden doors with one stroke**: Set Object = Solid, Actuation = Actuate ON, and place
  a 1-wide column. Wire the bottom tile to a lever — instant retracting door.
- **Project-wide colour consistency**: Configure your Wand of Coating once with your build's
  accent colour. Then set Building's Paint Sprayer = CoatingSettings. Every block you place
  inherits the colour without touching paint inventory.
- **Switch sub-modes confidently**: Each object type has its own chosen-item slot, so
  alternating Solid → Wall → Platform doesn't lose your choices. Choose once per material,
  reuse forever.
- **Confirm before Instant**: Building consumes items. While learning shapes and sizes,
  Confirm mode lets you preview the entire shape before any inventory is spent.
- **Stamp for tilesets**: Stamp mode + Mold shape lets you stamp the same custom geometry
  (a window frame, a column capital, a pre-made arch) anywhere with a single click each.

---

## See Also

- **[Wand of Coating](WandOfCoating.md)** — Configures the colour used by Paint Sprayer's *CoatingSettings* mode.
- **[Wand of Dismantling](WandOfDismantling.md)** — Removes tiles instead of placing them.
- **[Wand of Replacement](WandOfReplacement.md)** — Swaps existing tiles for new ones.
- **[Wand of Delimitation](WandOfDelimitation.md)** — Constrain Building to a precise area.
- **[Wand of Wiring](WandOfWiring.md)** — Wire the chassis after building it.
- **[Wand of Torches](WandOfTorches.md)** — Light the build with a single shape pass.
- **[Wand of Safekeeping](WandOfSafekeeping.md)** — Lock down a finished build.
- **[Wand of Molding](WandOfMolding.md)** — Provides the custom Mold shape for Stamp mode.
