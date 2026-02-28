# WorldShapingWandsMod — Roadmap

## Current State (as of this writing)

The core infrastructure is complete and stable. All four wand families compile and run, shapes
generate correctly, and undo captures full tile + wall state.

| System | Status | Notes |
|---|---|---|
| Shape geometry (5 shapes, 3 modes, variable thickness) | ✅ Solid | Ellipse uses O(W×H) raster — outperforms binary-search and pathfinding approaches in benchmarks up to 1000×1000 |
| Outline system (`OutlineHelper`) | ✅ Solid | Centralised; shapes only provide filled tiles |
| Selection state (immutable, lockable) | ✅ Solid | |
| Per-player wand state (`WandPlayer`) | ✅ Solid | |
| Wand of Building | ✅ Working | Inventory stock check, infinite-resource config, undo |
| Wand of Destruction | ✅ Working | Pick-power check, tile+wall, undo |
| Wand of Replacement | ✅ Working | Pick-power check, inventory consumption, undo |
| Wand of Wiring | ✅ Working | Wire/actuator placement and removal |
| Three selection modes (Instant / Select / Confirm) | ✅ Working | |
| Mode cycling via right-click inventory | ✅ Working | |
| Screen-culled selection overlay | ✅ Working | |
| Per-wand settings panels (draggable UI) | ✅ Working | |
| Config (infinite-resource threshold) | ✅ Working | |
| Undo stack (20-deep, tile + wall state) | ✅ Working | Fixed: was dropping newest action instead of oldest |
| Localization keys | ✅ Partial | En-US only; workshop description placeholder |

---

## Phase 1 — Stability & Completeness (current focus)

**Goal**: fill gaps in existing wands; no new wand families yet.

### 1.1 Settings UI completion
- Building wand: functional Object selector (Solid / Platform / Rope / Rail / GrassSeed / PlantPot)
- Building wand: Slope selector (flat / top-left / top-right / bottom-left / bottom-right / half)
- Destruction wand: expose `SuppressDrops` toggle in UI (already in settings struct, not shown)

### 1.2 Building wand: block-exhaustion behaviour
- Config option: **Next block** (automatically switch to the next matching block in inventory), **Interrupt** (stop mid-operation), **Cancel** (abort if not enough blocks before starting)

### 1.3 Building wand: replace mode
- When enabled, overwrite existing tiles instead of skipping occupied positions
- Validate pick power and `CanKillTile` on source tile before replacing

### 1.4 HUD element — active wand info
- Small HUD indicator showing current wand name, SelectionMode, and active shape type

### 1.5 Screen-edge indicators
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
| Overlay colour constants | Hardcoded in `SelectionOverlay` | Move to `WandConfig` or a `OverlayTheme` static class |

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
