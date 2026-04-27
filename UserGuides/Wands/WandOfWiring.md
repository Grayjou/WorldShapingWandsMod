# Wand of Wiring — User Guide

> **Wand Family**: Wiring
> **Variants**: Instant · Select · Confirm · Stamp
> **Cycle Key**: Right-click the wand in inventory to cycle between variants

---

## Overview

The Wand of Wiring lays down (or tears up) coloured wire and actuators across an entire
shape in one click. It replaces the bucket-of-clicks workflow of the vanilla **Grand
Design** for any wiring task that's larger than a few tiles — runs along a roof, bus lines
between rooms, full-room actuator nets, or sweeping a whole floor clean of stray wire.

The default shape is the **Elbow** (right-angle joint), which mirrors how the vanilla
Grand Design draws — so even with no settings changes, the wand feels familiar to anyone
who's used the vanilla wrench.

Use it whenever you need to:

- Run **multi-colour bus lines** (e.g. red + blue together) in one pass.
- Build **actuator floors and walls** with a single shape — no more 1-click-per-tile.
- **Tear out** a tangle of someone else's wiring without smashing the tiles underneath.
- Lay an **L-shaped junction** between two rooms in a single click via the default Elbow.

---

## Quick Start

1. **Equip** the Wand of Wiring (Instant is fastest).
2. Open the **settings panel** (right-click while not selecting, or press `.`):
   - Pick which **wires** to act on: Red · Green · Blue · Yellow · Actuator (defaults to Red only).
   - Pick the **Mode**: Place or Remove.
   - Pick a **Shape** (default is **Elbow** for vanilla Grand Design parity).
3. **Click** to apply.

In **Select**, **Confirm**, and **Stamp** modes, **right-click** during selection to
cancel and clear the preview.

---

## Variants

| Variant | Clicks | How It Works |
|---------|--------|--------------|
| **Instant** | 1 | Shape centred on cursor, applied immediately on release |
| **Select** | 2 | Click start → click end → applied |
| **Confirm** | 3 | Click start → click end → preview → click to confirm or right-click to cancel |
| **Stamp** | 4 | Click start → click end → confirm → stamp the same shape repeatedly |

---

## Wire & Actuator Selection

The settings panel exposes **five independent toggles**, one per wire colour plus actuator:

| Toggle | Default | Effect |
|--------|---------|--------|
| **Wire Red** | ON | Operate on red wire layer |
| **Wire Green** | OFF | Operate on green wire layer |
| **Wire Blue** | OFF | Operate on blue wire layer |
| **Wire Yellow** | OFF | Operate on yellow wire layer |
| **Actuator** | OFF | Operate on the actuator layer |

You can combine any number of these — the wand processes all selected layers in a single
shape pass. The settings struct packs them into a single `byte` flag (`PackWireFlags()`)
for efficient multiplayer sync.

> **All-off guard**: If every toggle is off, the wand reports "no wire colour selected"
> and skips the operation. The `HasAnySelection` property mirrors this guard.

---

## Modes

The wand has two operation modes, set in the panel:

### Place (default)

Lays down wire / actuator on every tile in the shape that doesn't already have it. Wire
costs are deducted from inventory; if you run out, the operation halts gracefully and
reports the partial result.

### Remove

Strips wire / actuator off every tile in the shape that does have it. **Removed wires drop
back as items** (vanilla behaviour) so you reclaim the materials.

> **Tip**: Combining `Remove` + all five toggles ON makes the wand a one-pass cleaner —
> sweep a shape and you've stripped every wire layer and actuator within it.

---

## Default Shape: Elbow

Unlike the other building wands (which default to **Rectangle**), the Wand of Wiring
defaults to the **Elbow** shape with thickness 1. This matches how the vanilla Grand
Design draws an L-shaped wire run between two clicks — so the wand "just works" without
opening the settings panel.

You can switch to any other shape (Rectangle, Cardinal Line, Straight Line, Ellipse,
Diamond, Triangle) for different layout patterns. The Mold shape from a Wand of Molding
template is also available — wire any custom mold you've sculpted.

---

## Shape & Selection

All standard shapes are available:

`Rectangle · Ellipse · Diamond · Triangle · Elbow (default) · Cardinal Line · Straight Line · Mold`

with the usual options:

- **Filled / Hollow** + **Thickness** (outline width 0–50)
- **Equal Dimensions** (force square / circular bounds)
- **Slice Mode** (top / bottom / left / right halves)
- **Connect Diameter** (close gaps in sliced Hollow shapes)
- **Invert Selection** (operate on the bounding box's negative space)

---

## Server Config Settings

| Setting | Config | Default | Effect |
|---------|--------|---------|--------|
| **MaxOperationSize** | Limits | 10000 | Maximum tiles per operation |
| **InfiniteWires** | Resources | OFF | When ON, Place mode never consumes wire from inventory |
| **InfiniteActuators** | Resources | OFF | When ON, Place mode never consumes actuators from inventory |

> Wiring doesn't break tiles, so **SuppressDrops**, **BypassPickaxePower**, and the
> demon-altar / delicate-tile guards do not apply.

### Per-type infinite override

The 3-state per-type infinite override (`Default` / `ForceOn` / `ForceOff`) on the
`PreferencesConfig` lets you flip the wire/actuator infinite flag without leaving the game
— useful for swapping in and out of "creative" mode mid-build.

---

## Client Config Settings

| Setting | Config | Default | Effect |
|---------|--------|---------|--------|
| **EnableWandSounds** | Preferences | ON | Plays the completion sound after each operation |
| **EnableProgressiveProcessing** | Performance | ON | Visualises large operations in batches (singleplayer only) |
| **Overlay colours** | Overlay / CanvasOverlay | — | Wiring uses its own family colour for the selection overlay |

---

## Multiplayer Behaviour

| Aspect | Singleplayer | Multiplayer |
|--------|--------------|-------------|
| Wire placement / removal | ✅ Instant | ✅ Server-authoritative via `WandPacketHandler` |
| Wire flags packed to 1 byte | ✅ | ✅ Sent in the wiring packet |
| Wire item drops on Remove | ✅ | ✅ (server spawns drops) |
| Completion sound + chat message | ✅ | ✅ |
| Progressive batching | ✅ | ❌ Server processes all at once |
| Undo (`/undo`) | ✅ | ✅ |

The wiring packet uses the standard cooldown rate-limit + max-distance validation shared
with every other wand family — no special multiplayer setup needed.

---

## Interactions With Other Systems

### Delimitation Area

If a **Wand of Delimitation** area is active, the Wand of Wiring only operates on tiles
inside the delimitation selection. Tiles outside the area are silently skipped. If your
shape lands entirely outside the delimitation area, you'll see:

> *"No action executed. Delimitation Area is active and does not overlap with the selection."*

### Safekeeping

Tiles or walls protected by a **Wand of Safekeeping** are **never wired or unwired** by
this wand. Protection wins. The protected positions are reported in the post-operation
chat summary so you can see exactly what was skipped.

### Undo

Every wiring operation is recorded as a **TileSnapshot** in the per-player undo history
(20 steps deep by default). Press `Backspace` (or `/undo`) to revert the last operation
— wires return to their pre-operation state and any drops created by Remove are reclaimed.

### Vanilla actuation

Tiles with the **actuator** flag respect vanilla mechanics — they can be triggered by
in-game wiring as soon as the wand lays them down. There is no separate "trigger" wand;
use vanilla switches, levers, and pressure plates as normal.

---

## Tips & Tricks

- **Vanilla parity**: With the default **Elbow** shape and only **Wire Red** ON, the wand
  is functionally a faster Grand Design — minus the per-tile clicking.
- **Multi-colour bus**: Toggle Red + Green + Blue together to lay a 3-colour bus along a
  Cardinal Line in a single shape pass.
- **Actuator floor**: Switch to Rectangle, toggle only Actuator, fill an entire floor —
  trap-doors, secret passages, and bait floors with one click.
- **Tangle cleanup**: Remove mode + all 5 toggles + a generous Rectangle = a one-pass
  wire-and-actuator wipe that returns every wire to your inventory.
- **Hollow rooms**: Hollow Rectangle + Wire Red lays a clean wire perimeter for
  room-edge sensors without filling the interior.
- **Mold reuse**: If you've sculpted a complex chassis with the Wand of Molding, use it
  as a Wiring shape — your stamped circuit follows the exact mold outline.

---

## See Also

- **[Wand of Building](WandOfBuilding.md)** — Build the chassis you're about to wire.
- **[Wand of Dismantling](WandOfDismantling.md)** — Tear up a wired chassis without losing wires (use Wiring **Remove** first).
- **[Wand of Safekeeping](WandOfSafekeeping.md)** — Protect existing wired areas from accidental edits.
- **[Wand of Delimitation](WandOfDelimitation.md)** — Constrain wiring to a precise area.
- **[Wand of Molding](WandOfMolding.md)** — Provides the custom Mold shape.
