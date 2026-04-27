# Wand of Safekeeping — User Guide

> **Wand Family**: Safekeeping
> **Variants**: Instant · Select · Confirm · Stamp
> **Cycle Key**: Right-click the wand in inventory to cycle between variants

---

## Overview

The Wand of Safekeeping marks tiles and walls as **protected**. Once protected, no wand in
the suite — Building, Dismantling, Replacement, Wiring, Coating, Fluids, Torches — will
modify those positions. Protection survives world saves (persisted via `TagCompound`) and
is enforced server-side in multiplayer.

This is the wand you reach for when:

- You've built something irreplaceable and don't want a careless drag of the **Wand of
  Dismantling** to erase it.
- You're on a multiplayer server and want to protect a build from accidental edits by
  teammates (without disabling their wands).
- You want to **replace** the contents of a room while keeping its walls intact — protect
  the walls, then run the Wand of Replacement with abandon.
- You need a **visual annotation layer** on the world: the colour-coded overlay shows you
  at a glance what's locked.

---

## Quick Start

1. **Equip** the Wand of Safekeeping.
2. Open the **settings panel** (right-click while not selecting, or press `.`):
   - Pick a **Mode**: Protect or Unprotect.
   - Toggle **Protect Tiles** and/or **Protect Walls** (both ON by default).
3. **Click** to apply the shape — every position inside is now (un)protected.
4. **Hold the wand** at any time to see the colour-coded protection overlay across the
   whole world.

In **Select**, **Confirm**, and **Stamp** modes, **right-click** during selection to
cancel and clear the preview.

---

## Variants

| Variant | Clicks | How It Works |
|---------|--------|--------------|
| **Instant** | 1 | Shape centred on cursor, applied immediately on release |
| **Select** | 2 | Click start → click end → applied |
| **Confirm** | 3 | Click start → click end → preview → click to confirm or right-click to cancel |
| **Stamp** | 4 | Click start → click end → confirm → stamp protection across multiple positions |

---

## Modes

| Mode | Effect |
|------|--------|
| **Protect** (default) | Marks each position in the shape as protected. Other wands skip these positions. |
| **Unprotect** | Clears protection from each position in the shape. Other wands can edit them again. |

---

## Layer Toggles

Protection has **two independent layers** — tile and wall — each with its own toggle:

| Toggle | Default | Effect |
|--------|---------|--------|
| **Protect Tiles** | ON | Foreground tile (block, platform, rope, plant…) at the position |
| **Protect Walls** | ON | Background wall at the position |

Combinations:

| Tiles | Walls | Result |
|-------|-------|--------|
| ON | ON | Full protection — neither layer can be touched |
| ON | OFF | Only tiles protected — walls remain editable |
| OFF | ON | Only walls protected — tiles remain editable |
| OFF | OFF | The wand reports **"Nothing"** and skips the operation |

This separation is what makes "**replace tiles inside a room without losing the walls**"
possible: protect just the walls, then go wild with Replacement / Dismantling.

---

## Visual Overlay

While the Wand of Safekeeping is **held**, a colour-coded overlay renders across all
visible chunks:

| Colour | Meaning |
|--------|---------|
| **Cyan** | Tile-only protection |
| **Magenta** | Wall-only protection |
| **Gold** | Both tile and wall protected |

The overlay is purely client-side and culled to the on-screen view, so it stays cheap even
on huge maps. It hides automatically when you put the wand away.

---

## Shape & Selection

All standard shapes are available:

`Rectangle · Ellipse · Diamond · Triangle · Elbow · Cardinal Line · Straight Line · Mold`

with the usual options:

- **Filled / Hollow** + **Thickness** (outline width 0–50)
- **Equal Dimensions** (force square / circular bounds)
- **Slice Mode** (top / bottom / left / right halves)
- **Connect Diameter**
- **Invert Selection**

The **Mold** shape from a Wand of Molding template is supported — protect any custom mold
you've sculpted.

---

## Server Config Settings

| Setting | Config | Default | Effect |
|---------|--------|---------|--------|
| **MaxOperationSize** | Limits | 10000 | Maximum tiles per (un)protect operation |

> Safekeeping doesn't destroy tiles or consume materials, so the drop-suppression /
> pick-power / infinite-resource configs do not apply.

### Persistence

Safekeeping data is stored on the server's `SafekeepingSystem` and serialised to the world
file via tModLoader's `TagCompound` save hook. Load order: world load → safekeeping
restored → wands respect protection on first frame after load. **No setup needed.**

---

## Client Config Settings

| Setting | Config | Default | Effect |
|---------|--------|---------|--------|
| **EnableWandSounds** | Preferences | ON | Plays the completion sound on each operation |
| **MaxUndoStackSize** | Preferences | 20 | Per-player undo history depth (Safekeeping ops are tracked too) |
| **Overlay colours** | Overlay / CanvasOverlay | Cyan / Magenta / Gold | The 3-state protection overlay colour |

---

## Multiplayer Behaviour

| Aspect | Singleplayer | Multiplayer |
|--------|--------------|-------------|
| Protection apply / clear | ✅ Instant | ✅ Server-authoritative |
| Other wands honour protection | ✅ | ✅ Enforced server-side |
| Persistence across world saves | ✅ via `TagCompound` | ✅ via `TagCompound` (world host) |
| Overlay visible to wand holder | ✅ | ✅ (each client renders own holders' overlay) |
| Undo (`/undo`) | ✅ | ✅ |

> **Server-side enforcement** is the key guarantee: even a player who edits their client
> config to disable protection cannot bypass it. The server checks every wand operation
> against `SafekeepingSystem` before applying.

---

## Interactions With Other Systems

### Every Other Wand

Every wand family in WSW (Building, Dismantling, Replacement, Wiring, Coating, Fluids,
Torches) checks each candidate position against `SafekeepingSystem` and skips protected
positions silently. The post-operation chat summary reports how many positions were
skipped due to protection, so you always know whether the wand respected your wards.

### Delimitation Area

If a **Wand of Delimitation** area is active, the Safekeeping operation itself is also
clipped to the delimitation area. This lets you "protect everything inside this room"
without re-typing the room's bounds — define the room with Delimitation once, then run
Safekeeping with a generous Rectangle.

### Undo

Every Safekeeping operation is recorded in the per-player undo history (default 20 steps).
Press `Backspace` or use `/undo` to revert — the protection state returns to exactly what
it was before the operation. `/undoinfo` previews the undo cost.

### Wand of Molding

The Mold shape is honoured — you can protect the exact silhouette of any sculpted mold.

---

## Tips & Tricks

- **Lock your masterpiece**: Hold Safekeeping, draw a generous Rectangle around your
  finished build, click. Now no wand will touch it — even your own.
- **Walls-only protection**: Toggle Tiles OFF, Walls ON, protect a whole house. Now you
  can rearrange the **furniture** inside (with Building / Replacement / Dismantling)
  without ever scratching the wallpaper.
- **Tile-only protection**: Toggle Walls OFF, Tiles ON, protect a road or platform. The
  walls behind can be redecorated independently.
- **MP teamwork**: On a server, every player runs Safekeeping over their own build. No
  one's wand will touch anyone else's work — perfect for collaborative cities.
- **Visual auditing**: Just *holding* the wand surfaces every protected position on
  screen — a free site-survey.
- **Pair with Delimitation**: Use Delimitation to scope a room, then Safekeeping with an
  oversized Rectangle to protect "everything in this room" without measuring twice.
- **Unprotect before demolition**: When it's time to tear something down, switch to
  Unprotect mode and sweep the same shape — instantly editable again.

---

## See Also

- **[Wand of Delimitation](WandOfDelimitation.md)** — Scope Safekeeping to a precise area, or scope other wands so they only touch unprotected zones.
- **[Wand of Dismantling](WandOfDismantling.md)** — The wand most likely to make you wish you'd protected something earlier.
- **[Wand of Building](WandOfBuilding.md)** — Build, then protect.
- **[Wand of Molding](WandOfMolding.md)** — Provides the custom Mold shape.
