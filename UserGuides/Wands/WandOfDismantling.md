# Wand of Dismantling — User Guide

> **Wand Family**: Destruction
> **Variants**: Instant · Select · Confirm · Stamp
> **Cycle Key**: Right-click the wand in inventory to cycle between variants

---

## Overview

The Wand of Dismantling destroys tiles, walls, and containers in shaped areas. It is the
primary tool for clearing terrain in bulk. Think of it as an area-of-effect pickaxe that
works in geometric shapes.

---

## Variants

| Variant | Clicks | How It Works |
|---|---|---|
| **Instant** | 1 click | Click once → shape centered on cursor is destroyed immediately |
| **Select** | 2 clicks | Click start corner → click end corner → area destroyed |
| **Confirm** | 3 clicks | Click start → click end → see preview → click to confirm or right-click to cancel |
| **Stamp** | 4 clicks | Click start → click end → see preview → confirm → stamp repeatedly at new positions |

### Right-Click Cancel

In **Select**, **Confirm**, and **Stamp** modes, right-click at any time during selection
to cancel the operation and clear the preview.

---

## Settings (Cycled In-Game)

All settings are cycled using the assigned hotkeys (default keys shown below).

### Target Toggles

| Setting | Default | Description |
|---|---|---|
| **Destroy Tiles** | ✅ ON | Destroys foreground tiles (blocks, platforms, etc.) |
| **Destroy Walls** | ❌ OFF | Destroys background walls |
| **Destroy Containers** | ❌ OFF | Destroys chests, dressers, and other containers |

When **Destroy Containers** is enabled:
- Container contents are dropped as items before the tile is destroyed
- **Locked chests** require the appropriate key in the player's inventory
  (Golden Key, Shadow Key, Temple Key, etc.)
- If the server config `IgnoreLockedKeyRequirements` is ON, locked chests are
  destroyed without needing keys
- If the server config `AutoOpenChestsOnDestruction` is ON, normal (unlocked)
  chests are automatically opened and destroyed; locked chests still require keys
  unless `IgnoreLockedKeyRequirements` is also ON

### Shape Settings

| Setting | Options | Description |
|---|---|---|
| **Shape** | Rectangle · Ellipse · Diamond · Triangle · Elbow · CardinalLine · StraightLine | Geometric shape of the operation area |
| **Fill Mode** | Filled · Hollow | Whether to fill the entire shape or just the outline |
| **Thickness** | 1–50 | Line/outline thickness (for hollow shapes and lines) |
| **Equal Dimensions** | ON/OFF | Forces the shape to have equal width and height (square, circle, etc.) |
| **Slice** | Full · HalfHorizontal · HalfVertical | Cuts the shape in half along an axis |

---

## Server Config Settings

These settings are in **Mod Configuration → World Shaping Wands (Server)** and affect
all players on the server.

| Setting | Default | Effect |
|---|---|---|
| **SuppressDrops** | OFF | When ON, destroyed tiles don't drop items |
| **VacuumItems** | ON | Teleports dropped items to the player (SP only — MP fix pending) |
| **BypassPickaxePower** | OFF | When ON, wand ignores pickaxe power requirements |
| **ProtectDemonAltars** | ON | Prevents wand from destroying demon/crimson altars |
| **ProtectDelicateTiles** | ON | Prevents wand from destroying life crystals, shadow orbs, etc. |
| **AutoOpenChestsOnDestruction** | ON | Auto-empties normal chests before destroying them |
| **IgnoreLockedKeyRequirements** | OFF | Allows destroying locked chests without keys |
| **MaxOperationSize** | 10000 | Maximum tiles per operation (prevents accidentally massive operations) |

---

## Client Config Settings

These settings are in **Mod Configuration → World Shaping Wands (Client)** and
affect only your local experience.

| Setting | Default | Effect |
|---|---|---|
| **EnableWandSounds** | ON | Plays completion sounds after operations |
| **EnableProgressiveProcessing** | ON | Processes large operations in visible batches (SP only) |

---

## Multiplayer Behaviour

| Aspect | Singleplayer | Multiplayer |
|---|---|---|
| Per-tile destruction effects | ✅ Sounds, dust, gore | ❌ Silent (by design — see note) |
| Completion sound | ✅ Tink sound | ✅ Tink sound (via OperationResult) |
| Completion message | ✅ Chat message | ✅ Chat message (via OperationResult) |
| Progressive batching | ✅ Visual batches | ❌ Instant (server processes all at once) |
| Locked chest validation | ✅ Key required | ✅ Key required (fixed Session 3) |
| Vacuum items | ✅ Works | ❌ Not yet supported (items scatter) |
| Undo | ✅ Works | ❌ Not yet supported |

> **Note on MP silence**: Terraria's server never plays tile destruction sounds or
> spawns visual effects. This is engine behaviour, not a bug. The server processes
> tiles as pure data operations and sends the final state to clients. All mods that
> do bulk tile operations (including ImproveGame) work the same way.

---

## Tips

1. **Start small**: Use **Confirm** mode when learning — you can preview the area
   before committing. Switch to **Select** or **Instant** once comfortable.

2. **Walls only**: Turn off Destroy Tiles and turn on Destroy Walls to clear only
   background walls without touching foreground blocks.

3. **Lines for mining**: Use the **StraightLine** shape for precise tunnel-cutting.
   **CardinalLine** snaps to horizontal/vertical; **StraightLine** allows diagonals.

4. **Undo is your friend**: Press the undo key to reverse the last operation.
   Multiple levels of undo are supported (SP only currently).

5. **Stamp mode for repetition**: If you're clearing the same shape repeatedly
   (e.g., digging evenly-spaced rooms), use Stamp mode — define the shape once,
   then stamp it wherever you click.

6. **Protect important tiles**: Enable `ProtectDelicateTiles` in server config to
   prevent accidental destruction of life crystals, shadow orbs, and similar
   world-progression items.
