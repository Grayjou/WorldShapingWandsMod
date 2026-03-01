# TODO Features

> **Legend:** ✅ Implemented &nbsp;|&nbsp; 🔄 In Progress / Partial &nbsp;|&nbsp; ❌ Not Started

---

## SimpleWands (Apply Actions Immediately)

### Mode cycling & selection

- ✅ Three variations per wand — right-clicking in inventory cycles through modes (like the Shellphone)
- ✅ Modes: **OneClick** (click-and-drag, apply on mouse release), **TwoClick** (click start → click end to apply), **ThreeClick** (click start → click end → click confirm)
- ✅ Right-click while selecting cancels the selection
- ❌ Right-click while *not* selecting opens the wand settings UI *(currently opens settings via `CanUseItem`, but the wand-select-mode start-mode persistence across hotbar changes is missing)*
- ❌ Configurable persistent start-mode across hotbar changes
- ✅ Shape overlays shown during selection (screen-culled)
- ❌ Screen-edge indicators for off-screen tiles (currently simply not drawn)

### Wand of Building ✅

- ✅ Places first matching item from inventory (hotbar priority, then main inventory, then ammo slots)
- ✅ Respects `PlaceType` (Solid, Platform, Rope, Rail, GrassSeed, PlantPot)
- ✅ Inventory stock check before placement
- ✅ Infinite-resource mode (configurable threshold)
- ✅ Undo support
- ✅ Behaviour when block runs out mid-placement (config: NextBlock / Interrupt / Cancel via `BlockExhaustionMode`)
- ✅ Block-replacement mode (replace existing tiles with selected block, respecting pick power)
- ✅ Settings UI: Object selector UI
- ✅ Settings UI: Slope selector (6 slopes: Full Block, Half Block, ◣ ◢ ◤ ◥) — icon buttons with hover labels
- ✅ Settings UI: Shape selector

### Wand of Destruction ✅

- ✅ Destroys tiles and/or walls in a shape
- ✅ Pick-power validation (`HasEnoughPickPowerToHurtTile`)
- ✅ `CanKillTile` check for indestructible tiles
- ✅ Drop suppression toggle (`SuppressDrops`)
- ✅ Settings UI: toggle tile destruction
- ✅ Settings UI: toggle wall destruction
- ✅ Settings UI: Shape selector
- ✅ Undo support (tile + wall state both captured since fix)

### Wand of Replacement

- ✅ Replaces every instance of source-type tiles with target-type tiles in selection
- ✅ Pick-power validation
- ✅ `CanKillTile` check
- ✅ Inventory consumption of replacement tiles
- ✅ Undo support
- ✅ Settings UI: Source / Target ObjectType selectors (Tile, Platform, Rope, PlanterBox, Rail, Seeds, Air)
- ✅ Air as a source/target type — Source=Air fills empty gaps, Target=Air erases matching tiles
- ✅ Configurable source/target types via UI (replaces old positional-inventory logic)

### Wand of Wiring ✅

- ✅ Place or remove red/green/blue/yellow wire and actuators
- ✅ Shape-based area wiring
- ✅ Three selection modes
- ✅ Edge shape defaults to vanilla wire-kite behaviour

#### Missing from MagicWiring integration

The original MagicWiring mod has several features not yet ported:

- ❌ **Per-wire colour overlay** — MagicWiring uses green (place), blue (remove), orange (clamped/pulsing) overlay colours. Our current overlay uses the shared `WandColors` palette without wiring-specific colour coding.
- ❌ **Distance clamping with visual feedback** — MagicWiring enforces `MaxWiringDistance` (default 200 tiles) with `ShapeHelper.ClampDistance`. Clamped selections pulse orange. Our wand has no distance limit.
- ❌ **Max wiring distance config** — `MagicWiringConfig.MaxWiringDistance` with server-side enforcement in the packet handler.
- ❌ **Wire/actuator inventory consumption** — MagicWiring checks `HasItem(player, ItemID.Wire)` per tile and consumes one wire item per placement. Our wiring wand places for free (no wire consumption).
- ❌ **Proper multiplayer wiring packets** — MagicWiring has `WiringPacketHandler` with `SendWiringOperation` / `HandleWiringOperation`, server-side distance clamping, and broadcast to all clients. Our wiring uses `NetMessage.SendTileSquare` only.
- ❌ **Hold & Drag interaction mode** — MagicWiring distinguishes Hold (hold-click-drag-release) from Toggle (click-start → click-confirm). Our three selection modes (OneClick/TwoClick/ThreeClick) cover similar ground but the mapping isn't 1:1.
- ✅ **Cancellation fade feedback** — cancelled selection overlay changes colour per wand type and fades out over ~45 ticks, with rising "Cancelled" text.

### Simple Wand Display

- ❌ HUD display of current wand settings (SelectionMode, block type)
- ❌ Real-time tooltip showing pending resource consumption

---

## Preview / Designer Workflow

### Preview Mode

- ❌ **All modes**: show tiles-to-be-destroyed and tiles-to-be-added in real-time  
- ❌ **Normal mode exclusive**: block consumption tracking, required-items display, indestructible-block warnings  
- ❌ **Commit**: apply changes and deduct inventory  
- ❌ **Discard**: cancel staged changes  
- ❌ **Proper intersection solve** — when multiple staged shapes overlap, compute net additions/removals correctly (e.g., two overlapping filled circles → net block counts)

### Regular Mode Improvements

- ✅ Max selection dimension caps (SmallSelectionCap for Edge/StraightLine, BigSelectionCap for everything else — configurable in WandConfig)
- ❌ Drag-and-select points mode
- ❌ Noise unavailable in regular mode (enforce)

---

## Designer Wands (Selection + Commit)

> Wands marked **(?)** represent features that could be merged into earlier wands.

### Selection Wand

- ❌ Shape-based region selection
- ❌ Boolean operations: Add / Remove / Intersect / XOR across multiple selections

### Drawing Wand

- ❌ Paint future tile placements inside a selection without actually placing blocks
- ❌ Draw a shape (supported types) inside the selection with a chosen block

### Replacement Wand (?)

- ❌ Same as Drawing Wand but stages tile-type replacements

### Eraser Wand (?)

- ❌ Same as Drawing Wand but stages air (removal)

### Commit Wand (?)

- ❌ Apply all staged changes; deduct required items from inventory; undo-able as one action

### Moving Selection

- ❌ Dynamically offset the staged selection to preview placement position before committing

### Resource Tracking

- ❌ Aggregate resource requirements for all staged changes across the commit buffer

---

## Undo System

- ✅ Per-player undo stack (up to 20 actions)
- ✅ Tile state captured (type, frame, slope, half-block, wall type)
- ❌ Undo for regular-mode actions — complex when tiles don't always drop themselves (honey, multi-tile objects)
- ❌ Multi-level undo for preview-mode commits (restore entire pre-commit state)
- ❌ Redo

---

## Boundary Noise System

- ❌ Procedural noise jitter on shape boundaries
- ❌ Regenerate noise on demand
- ❌ Bias types: Positive / Negative / Both
- ❌ Configurable max amplitude

---

## Shape & Outline Enhancements

- ✅ Thickness controls: +/- keybinds (`ThicknessControls`)
- ✅ Outline mode: slim (0), standard (1), Chebyshev-eroded thick (2+)
- 🔄 **StraightLine perpendicular thickness** — the StraightLine shape currently ignores outline thickness. It should expand perpendicular to the line direction using odd widths only (even → width-1). On standby pending design review — unclear if it makes sense for all use cases, though it would make the thickness UI consistent across shapes.
- ❌ Inward hollowing (industry standard — current Hollow uses 4-neighbour boundary)
- ❌ Flood fill within selection
- ❌ Additional shape modes (e.g., concentric rings, spiral)

---

## Dimension Display

- ✅ W x H dimension label rendered near cursor (top-left offset, UI-scaled, screen-clamped)
- ❌ Tile count display
- ❌ Resource count display in overlay

---

## Memory / Performance

- ✅ Screen-culling of overlay tiles (tiles outside viewport are skipped)
- ❌ Max canvas area limit for preview mode
- ❌ Background processing for very large operations

---

## Wall Operations

> **Industry standard**: most building mods use a **separate item for wall placement** (e.g. "Wall Wand" or "Wall of Building"), while **tile destruction wands handle wall removal** via toggles (Destroy Tiles / Destroy Walls). This avoids overcrowding the building wand UI with wall controls while keeping destruction ergonomic. Our Wand of Destruction already follows this pattern with its tile/wall toggles.

- ✅ Destruction wand can toggle wall removal
- ❌ Building wand wall placement
- ❌ Replacement wand wall replacement
- ❌ UI integration without further cluttering existing panels

---

## Mod Integration & Multiplayer

- ❌ `TileLoader.CanPlace()` validation for modded tiles
- ❌ Packet-based sync for placement (currently client-only; sends `NetMessage.SendTileSquare`)
- ❌ Server validation pass

---

## Concerns & Open Questions

- **Undo complexity**: reversing building operations is non-trivial when tiles don't drop themselves (honey, multi-tile objects like doors or furniture). The current undo restores tile data directly, which may leave dangling multi-tile objects in a broken state.
- **Wall operations UI saturation**: adding full wall controls may overcrowd existing panels — needs a design decision.
- **Wand of Destruction vs Wand of Replacement with air**: the destruction wand is **not** redundant. It removes *all* tiles and/or walls in the selected area regardless of type, while the replacement wand replaces a *specific* tile type with another. Destruction is area-wide mass removal; replacement is type-selective swap. Even with "air" as a replacement target, the user would have to specify which source tile type to erase — destruction skips that step entirely. Keep both.
- **Flood fill**: to add or not? Could dramatically extend scope.
- **Tile overlay for modded tiles**: ModTile sprites may need special rendering care.
- **Wiring visualisation**: displaying existing wire layouts inline with the preview is complex.
