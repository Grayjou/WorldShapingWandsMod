# WorldShapingWandsMod — Roadmap

## Current State (as of this writing)

The core infrastructure is complete and stable. All four wand families compile and run, shapes
generate correctly, and undo captures full tile + wall state.

| System | Status | Notes |
|---|---|---|
| Shape geometry (8 shapes, 2 modes, variable thickness) | ✅ Solid | Ellipse uses direct `Math.Sqrt` rasterisation; Half-ellipses clip a doubled ellipse to one half; Diamond uses ×2 integer arithmetic; StraightLine uses 8-sector direction detection |
| Outline system (`OutlineHelper`) | ✅ Solid | Centralised; shapes only provide filled tiles; only Filled and Hollow modes (Outline removed) |
| Selection state (immutable, lockable) | ✅ Solid | |
| Per-player wand state (`WandPlayer`) | ✅ Solid | |
| Wand of Building | ✅ Working | Inventory stock check, infinite-resource config, undo, block-replacement, exhaustion modes |
| Wand of Destruction | ✅ Working | Pick-power check, tile+wall, undo |
| Wand of Replacement | ✅ Working | Pick-power check, inventory consumption, undo |
| Wand of Wiring | ✅ Working | Wire/actuator placement and removal — missing: wire consumption, per-wire colour overlay, distance clamping, proper MP packets (see MagicWiring gap analysis) |
| Three selection modes (Instant / Select / Confirm) | ✅ Working | `SelectionOwnerItemType` prevents cross-wand execution on switch |
| Mode cycling via right-click inventory | ✅ Working | |
| Screen-culled selection overlay | ✅ Working | |
| Per-wand settings panels (draggable UI) | ✅ Working | |
| Config (infinite-resource threshold) | ✅ Working | |
| Undo stack (20-deep, tile + wall state) | ✅ Working | Fixed: was dropping newest action instead of oldest |
| Localization keys | ✅ Working | En-US; single correctly-named hjson file; icon-button tooltips use `Language.GetTextValue` |

---

## Phase 1 — Stability & Completeness (current focus)

**Goal**: fill gaps in existing wands; no new wand families yet.

### 1.1 Settings UI completion
- ✅ Building wand: functional Object selector (Solid / Platform / Rope / Rail / GrassSeed / PlantPot)
- ✅ All panels: shape icon buttons (14 buttons across 8 shape types × filled/hollow + edge + straight)
- ✅ All panels: object-type icon buttons where applicable (Building: 6, Replacement: 6 source + 7 target)
- Building wand: Slope selector (flat / top-left / top-right / bottom-left / bottom-right / half)
- Destruction wand: expose `SuppressDrops` toggle in UI (already in settings struct, not shown)

### 1.2 Building wand: block-exhaustion behaviour ✅
- ✅ Config option: **NextBlock** (automatically switch to the next matching block in inventory), **Interrupt** (stop mid-operation), **Cancel** (abort if not enough blocks before starting)
- Implemented via `BlockExhaustionMode` enum and per-tile item lookup in `ExecuteBuilding`

### 1.3 Building wand: replace mode ✅
- ✅ When enabled, overwrite existing tiles instead of skipping occupied positions
- ✅ Validate pick power and `CanKillTile` on source tile before replacing

### 1.4 Wand of Replacement: Air as object type
- Air is simply another entry in the object type settings (not a separate wand or mode)
- Source = Air → "fill empty gaps" (place into empty tiles within selection)
- Target = Air → "erase matching tiles" (remove matching tiles, like targeted destruction)
- Should be straightforward: add `PlaceType.Air` variant, condition = `!tile.HasTile` or `tile.HasTile` depending on direction

### 1.5 UI redesign: custom asset buttons with hover labels

**Problem**: as more features are added (shapes, modes, object types, exhaustion, slopes…), text-based
`UIToggleButton` panels are becoming crowded and tall.

**Proposed solution**: replace text buttons with small **custom-asset icon buttons** (Terraria style,
like the vanilla wiring UI). Each button is a 32×32 or 22×22 sprite that shows a **hover label**
on mouseover using `UICommon.TooltipMouseText(label)`.

**Implementation pattern** (from tModLoader ExampleMod):
- Subclass `UIImageButton` → override `DrawSelf` → call `base.DrawSelf` + `UICommon.TooltipMouseText` on hover
- Load assets via `ModContent.Request<Texture2D>("MyMod/Assets/UI/ButtonName")`
- Icons go in `Assets/UI/` folder — Terraria-style pixel art at 2× scale

**Design decisions pending**:
- **Object types**: good candidate for icons — block sprite thumbnails are immediately recognisable
- **Modes (Filled/Hollow)**: good candidate — small shape-outline icons are clear
- **Shapes**: **uncertain** — text labels ("Rect", "Ellipse", "Edge", "Straight") may be more descriptive
  than tiny icons. Could keep text for shapes while using icons for everything else.
- **Exhaustion mode, slope selector, wire types**: all could benefit from compact icon layout
- Need to test in-game before committing — panel height is the main concern

### 1.6 HUD element — active wand info
- Small HUD indicator showing current wand name, SelectionMode, and active shape type

### 1.7 Screen-edge indicators
- Draw directional arrow or count indicator at screen edges for off-screen tiles in the selection

---

## Phase 2 — Boundary Noise System (medium priority)

Adds procedural jitter to shape boundaries for organic-looking terrain placement.

- `NoiseSettings` struct: amplitude, bias (Positive / Negative / Both), seed
- Per-wand noise toggle
- Keyboard shortcut or UI button to regenerate noise with a new seed
- Noise unavailable in Instant mode (too disorienting for click-drag)
- Performance ceiling: noise only evaluated on boundary tiles (already identified by `OutlineHelper`)

**Possible deduplication**: noise generation is shape-agnostic; a single `NoiseHelper` used by all
wand types avoids duplication.

---

## Phase 3 — Preview / Commit / Discard Workflow (high value, high effort)

Transforms the mod from "immediate apply" to a staged-changes model for precision building.

### Core additions
- `StagedChange` struct: position + intended new tile + wall type
- `ChangeBuffer` per player: ordered list of staged changes with net-delta calculation
- `CommitAction` and `DiscardAction` commands

### UI additions
- Overlay colour-coding: **green** = tiles to add, **red** = tiles to remove, **yellow** = replacements
- Info panel: required blocks, currently held, shortfall highlighted in red
- Indestructible block warnings (tiles that fail `CanKillTile` or pick-power check)

### Overlay Design Notes (for designer workflow)

The current `SelectionOverlay` draws a flat colour per tile. Phase 3 needs richer overlays:

1. **Colour semantics** — assign fixed colours from `WandColors` for each staged change type:
   - Build (place new tile): semi-transparent green
   - Destroy (remove existing tile): semi-transparent red
   - Replace (swap tile type): semi-transparent yellow/amber
   - Wall-only changes: same scheme but dimmer alpha
   - Conflicting / impossible tiles: pulsing red outline

2. **Layered rendering** — staged changes overlay on top of the existing selection outline.
   The outline still shows the selection boundary; the interior fill uses change-type colours.
   This requires `SelectionOverlay.Draw` to accept a `ChangeBuffer` alongside the `SelectionState`.

3. **Resource info overlay** — a small floating panel near the selection showing:
   - Block type icon + count needed vs count in inventory
   - Red text for shortfalls
   - Drawn via `UIWorldInfoPanel` (new element), positioned at selection corner

4. **Ghost tile rendering** — for preview mode, draw the *intended* tile sprite at reduced opacity
   instead of a flat colour fill. This gives the player a true preview of the final result.
   Implementation: use `Main.instance.TilePaintSystem` data + `Main.spriteBatch.Draw` with the
   tile's source rectangle from `Main.tileFrame` at ~40% alpha.

5. **Performance** — overlay rendering must remain screen-culled. Only compute overlay data for
   tiles within `Main.screenPosition` ± margin. The `ChangeBuffer` should support spatial queries
   (e.g., a `HashSet<Point>` for O(1) lookup of whether a tile has a staged change).

6. **Overlay toggle** — a keybind or UI toggle to show/hide the preview overlay without discarding
   the staged changes. Useful when the overlay obscures the build context.

### Intersection resolution
When multiple staged shapes overlap, compute the net result:
- Two filled areas with different block types → second operation overrides first
- An erase followed by a fill → net fill

### Undo within preview mode
- Each staged change is its own undo entry inside the buffer
- Committing the buffer creates one undo entry restoring the pre-commit world state

**Possible optimisation**: `ChangeBuffer` only stores positions where the final state differs
from the current world — avoids redundant snapshotting.

---

## Phase 4 — Designer Wands (medium priority)

Builds on the preview system. Depends on Phase 3 being complete.

### Selection Wand
- Boolean region operations: Add / Remove / Intersect / XOR
- Visual indicator for each boolean mode

### Drawing Wand
- Paint inside a selection region without placing tiles (stages changes)
- Full shape support with selected block type
- No inventory consumption until commit

### Eraser Wand (may merge with Drawing Wand)
- Stage air/removal operations

### Replacement Wand (may merge with Drawing Wand)
- Stage tile-type swaps

### Commit Wand (may merge into Selection Wand right-click)
- Trigger commit from any selection state
- Display final resource delta before confirming

### Moving Selection
- Offset all staged changes dynamically to preview position before commit
- Essential for "build a room template, move it around to find the right spot"

---

## Phase 5 — QoL & Advanced Features (lower priority)

- **Multi-level undo**: undo individual staged operations inside a committed buffer
- **Wall-aware building**: wand of building can optionally place walls
- **Max canvas area limit**: configurable cap on total tiles in a single operation (prevents lag)
- **Save / load structures**: `TagCompound`-serialised tile snapshots (inspiration: WandOfConstruction)
- **Flood fill mode**: fill a contiguous region of matching tiles

---

## Phase 6 — Multiplayer & Polish (future)

- Packet-based sync for all placement/destruction operations
- Server-side validation (currently client-authority with `NetMessage.SendTileSquare` notifications)
- Multiplayer: lock selection region to prevent concurrent modification conflicts
- Workshop release (description, icon, crafting recipes)

---

## Architecture Decisions & Deduplication Opportunities

| Concern | Current approach | Opportunity |
|---|---|---|
| Per-wand settings | Four separate `WandOf*Settings` structs | Extract shared fields (`SelectionMode`, `ShapeInfo`) into a `BaseWandSettings` base class |
| `Execute*` methods | Duplicated structure in `WandOfBuildingBase`, `WandOfDestructionBase`, etc. | A shared `WandOperation` base that takes a `TileAction` delegate would remove the per-type switch |
| `IsHoldingWandItem` | Duplicated in `WandPlayer` and `SelectionOverlay` | Centralise in `WandPlayer` as a property |
| `CancelSelection` | Virtually identical in all four wand base classes | Already `virtual` — consolidate the `Main.NewText` call into the base |
| Overlay colour constants | Previously hardcoded in `SelectionOverlay` | ✅ Centralised in `WandColors.cs` |
| `ShapeMode.Hollow` vs `ShapeMode.Outline` | Were identical (`BuildHollow` delegated to `BuildOutline`) | ✅ Resolved: removed `ShapeMode.Outline`; only Filled and Hollow remain |
| Overlay colour constants | Hardcoded in `SelectionOverlay` | ✅ Centralised in `WandColors.cs` |
| UI crowdedness | Text buttons scale poorly with feature count | Proposed: custom-asset icon buttons with hover labels (tModLoader `UIImageButton` + `TooltipMouseText` pattern). Icons for object types and modes; text may stay for shapes (more descriptive). See Phase 1.5 |

---

## Risk Register

| Risk | Likelihood | Mitigation |
|---|---|---|
| Undo breaking multi-tile objects (doors, furniture) | Medium | Document as known limitation; add tile-object check before snapshot |
| Large operations causing frame spikes | Medium | Max canvas limit + background processing (coroutine) |
| Preview mode state becoming desynced on world reload | Medium | Clear staged changes on `OnEnterWorld` |
| Modded tile incompatibility | Low (vanilla only currently) | Add `TileLoader.CanPlace()` validation in Phase 6 |
| Multiplayer desyncs | High without fix | Addressed in Phase 6 packet work |

---

*See [`TODO_FEATURES.md`](TODO_FEATURES.md) for a granular, status-annotated checklist.*
