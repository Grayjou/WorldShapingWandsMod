# WorldShapingWandsMod — Roadmap

## Current State (post-DevNotes #18 — Phase C Bug Fixes)

The core infrastructure is complete and stable. All **six** wand families compile and run, shapes
generate correctly, undo captures full tile + wall state, and crafting recipes are in place.
Wiring has full multiplayer packet support (#112). Selection system uses dual-slot storage
with visual compatibility (#114). Instant wands now use a **completely isolated selection state**
(`_instantSelection`) that never touches the dual-slot system (#119). This means instant wand
click-drags can never overwrite stored Select/Confirm/Stamp selections.
Even thickness and brush rasterization use proper diameter-based algorithms (#115, #116).
Safekeeping sound updated: protect uses SoundID.MaxMana @ 0.5 vol (#118).
Comprehensive analysis of 4 cloned MIT repos completed (`ClonedRepoAnalysis.md`).
MagicWiring merge schedule rewritten with actionable MP packet designs (`MagicWiringMergeSchedule.md`).
Animation schedule expanded with thorough spritesheet cell management research.
Multiplayer fix schedule rewritten with 7-day plan and verified Terraria source findings (`MultiplayerFixSchedule.md`).
Beta readiness evaluation completed (`BetaReadinessEvaluation.md`).
Lorebook with thematic lore entries created (`Lorebook.md`).

**Phase B additions (DevNotes #18a):**
- Ammo counting bug fix across 4 wand families (building, replacement, wiring, coating)
- CardinalLine debounce fix for overlay flicker
- StraightLine shape — free-angle Bresenham line with variable thickness brush (15 files)

**Phase C additions (DevNotes #18b — Bug Fixes):**
- Wiring wand 200-item drop cap fix — suppress drops via `WorldGen.gen`, consolidate to inventory
- Replacement wand wall drops — respect `SuppressDrops` config per-tile
- Building wand wall progressive batching — full `WallBuildingInfo` infrastructure
- Dismantling tree/chest block order fix — top-to-bottom sort + deferred `CanKillTile`
- **Vacuum item collection system** — localized `VacuumItemsInArea()` collects drops to inventory
- **VacuumItems config option** (default: true) — configurable in Sandbox Options section
- All 3 localization files updated (en-US, fr-FR, es-ES)

**DevNotes #16 additions:**
- Building wand same-type slope overwrite — applies slope in-place without consuming items
- Container destruction for dismantling wand — unlocks locked chests, drops contents, destroys tile
- Configurable max outline thickness (default 10, range 1–100)
- Per-type infinite resource toggles (tiles, walls, grass seeds, wires, actuators)
- WiringHelper split into per-wire/per-actuator infinite checks
- **Wand of Coating** — 6th wand family: apply/remove Illuminant, Echo, and Actuated coatings
- Coating wand crafting recipe, sound effects, UI panel with coating type selector

**DevNotes #17 additions:**
- Wall/torch `WouldTileLoseSupport` fix — replacement skips tiles that would lose structural support (#145)
- Selection overlay 3-state render modes: `AlwaysFullShape`, `OnlyOutline`, `OutlineAndPartialFill` (#141)
- `AlwaysFullShape` flicker fix — fade-in animation no longer triggers on every cursor movement (#146)
- Infinite resource 3-state redesign: `InfiniteOverride` enum (Default/ForceOn/ForceOff) per-type (#147)
- Seeds and Rails removed from Wand of Replacement UI (7→5 buttons, shoddy behaviour) (#148)
- Container destruction tooltip on dismantling UI toggle button (#149)
- Auto overlay tooltip expanded (#150)

| System | Status | Notes |
|---|---|---|
| Shape geometry (9 shapes, 2 modes, variable thickness) | ✅ Solid | Ellipse, Diamond, CardinalLine, StraightLine, Half-ellipses all correct |
| Outline system (`OutlineHelper`) | ✅ Solid | Centralised; shapes only provide filled tiles; Filled and Hollow modes |
| Selection state (immutable, lockable) | ✅ Solid | 4 modes: Instant/Select/Confirm/Stamp |
| Dual-selection storage (large + small slots) | ✅ Solid | Prevents cross-shape-category leaks (#114) |
| Selection compatibility (visual suppression) | ✅ Solid | Incompatible inherited selections hidden, not destroyed (#114) |
| Per-player wand state (`WandPlayer`) | ✅ Solid | |
| Wand of Building | ✅ Working | Tile+wall placement, stock check, infinite-resource, undo, exhaustion modes, slope overwrite, recipe |
| Wand of Dismantling | ✅ Working | Pick-power check, tile+wall, undo, progressive mode, container destruction, recipe |
| Wand of Replacement | ✅ Working | Tile+wall replacement (#37), pick-power, inventory consumption, undo, recipe |
| Wand of Wiring | ✅ Working | Wire/actuator placement and removal, recipe, **full MP packets** (#112) |
| Wand of Safekeeping | ✅ Working | Protect/unprotect tiles+walls, persistent+session, recipe |
| Wand of Coating | ✅ Working | Apply/remove Illuminant, Echo, Actuated coatings, recipe |
| Four selection modes | ✅ Working | SelectionOwnerItemType prevents cross-wand execution |
| Mode cycling via right-click inventory | ✅ Working | mouseInterface guard (#111) |
| Screen-culled selection overlay | ✅ Working | Dimension labels with area count, instant wand overlay (#117) |
| Per-wand settings panels (draggable UI) | ✅ Working | Icon-based buttons |
| Config (infinite-resource, progressive, per-type toggles, max thickness) | ✅ Working | SafekeepingClearThreshold, MaxOutlineThickness, per-type infinite toggles |
| Undo stack (20-deep, tile + wall state) | ✅ Working | |
| Recipe system with recipe groups | ✅ Working | AnyGem, AnyGoldBar, AnySilverBar, AnyEvilStone, AnyWandOfBuilding, AnyWandOfDismantling |
| Sound effects (per-wand, conditional) | ✅ Working | Dismantling: Tink, Replacement: Item29, Wiring: Item64, Safekeeping: Protect=MaxMana@0.5vol, Unprotect=Unlock (#118) |
| Instant wand selection isolation | ✅ Working | Isolated `_instantSelection` field, never touches dual-slot system (#119) |
| Lore system (divine + per-wand) | ✅ Working | ShowDivineLore override for Wiring |
| Localization keys | ✅ Working | En-US; includes Wall type |
| Dual-sprite system | ✅ Working | Inventory vs swing textures |
| Shimmer decraft | ✅ Working | All wands decraft to Amethyst |
| Cursor highlight | ✅ Working | Pulsing highlight at cursor when holding wand |
| UI close button | ✅ Working | Properly clears UserInterface state |
| Even thickness algorithm | ✅ Working | Diameter-based for even values (#115) |
| Brush rasterization | ✅ Working | EllipseShape.GetCircleBrushOffsets IncrementalFast O(W+H) (#116) |
| Wiring MP packets | ✅ Working | WandPacketHandler: client→server→broadcast (#112) |

---

## Phase 1 — Alpha Release (current)

**Goal**: Complete, polished, single-player-ready alpha. All 6 wand families functional with
recipes, sound effects, correct dimension display, and proper documentation.

### Completed in DevNotes #4
- ✅ build.txt metadata (author, homepage)
- ✅ description.txt rewritten (5 families, 4 modes, features, limitations)
- ✅ Sound effects: Building (Item9), Dismantling (Item14), Replacement (Item29)
- ✅ ElbowShape GetDisplayDimensions (shows segment lengths, not bounding box)
- ✅ Dimension display: Math.Clamp positioning, configurable offset
- ✅ Area tile count display (debounced, appended to dimension label)
- ✅ Replacement wall mode (#37): ObjType.Wall, ExecuteWallReplacement, UI buttons
- ✅ Air source type clarified: Air is target-only (enum doc + UI already correct)
- ✅ Lore finalized: Replacement updated, Wiring divine header removed
- ✅ Recipes: Building, Dismantling, Replacement added; Safekeeping updated
- ✅ WandRecipeSystem with 4 recipe groups
- ✅ TODO_FEATURES.md and Roadmap.md fully updated

### Completed in DevNotes #5
- ✅ Cardinal line circular brush algorithm (replaces checker-pattern perpendicular expansion)
- ✅ Cursor highlight / starting area preview when holding wand
- ✅ Only Instant mode has crafting recipe (other modes via inventory right-click)
- ✅ "Gemstone" label for AnyGem recipe group
- ✅ Replacement wand recipe uses AnyWandOfBuilding / AnyWandOfDismantling groups
- ✅ Sound effects: correct IDs, conditional playback, config toggle (EnableWandSounds)
- ✅ Multiplayer fix: Dismantling and Replacement skip WorldGen.gen in MP

### Completed in DevNotes #6
- ✅ Shimmer decraft to Amethyst instead of shimmer cycling (follows vanilla Shellphone convention)
- ✅ UI close button fix: all 5 panels now call WandUISystem.CloseAllUI() properly
- ✅ Shimmer tooltip hint added

### Completed in DevNotes #7
- ✅ Removed shimmer tooltip (added in #6, deemed useless — cluttered tooltip for no value)
- ✅ Localization: moved 60+ hardcoded `Main.NewText()` strings to hjson keys (`Messages.*`)
- ✅ Created `Msg` utility helper for localized chat messages
- ✅ Safekeeping ClearAll: configurable threshold confirmation (default: 50 protected tiles)
- ✅ New config: `SafekeepingClearThreshold` with header, label, and tooltip
- ✅ `/undo` chat command for dev testing (supports count, all, status)
- ✅ MagicWiring merge schedule documented (Option B — Unified, 5 phases, 6–9 sessions)
- ✅ Showcase script for GIF/image demonstrations (10 scenes)
- ✅ Credits: ImproveGame and CheatSheet acknowledged in description.txt
- ✅ MOT API check: WandRecipeConditions already scrubs MoreObtainingTooltips lines
- ✅ French localization file structure prepared (placeholder — user will translate)

### Completed in DevNotes #8–#12
- ✅ Wiring multiplayer packets: full `WandPacketHandler` with client→server→broadcast (#112)
- ✅ Server-side distance validation for wiring operations
- ✅ SafekeepingSystem protection checks in server-side wiring
- ✅ InfiniteResource config respected in MP wiring
- ✅ Dual-selection storage: large-shape and small-shape slots (#114)
- ✅ SelectionOwnerItemType per-slot tracking
- ✅ Selection visual compatibility: `IsSelectionVisuallyActive()` suppresses inherited selections
- ✅ `EnsureSelectionCompatibility()` deferred-clear on UseItem click

### Completed in DevNotes #13
- ✅ Instant wand UseItem guard: skip `EnsureSelectionCompatibility` for OneClick (#109v2)
- ✅ Right-click inventory cycle: `!mouseInterface` guard prevents NPC shop cycling (#111v2)
- ✅ Even thickness: diameter-based brush instead of radius-based for even values (#115)
- ✅ Brush rasterization ≥4: `EllipseShape.GetCircleBrushOffsets()` uses IncrementalFast O(W+H) (#116)

### Completed in DevNotes #14
- ✅ Instant wand overlay: `IsSelectionVisuallyActive()` now allows own selections during drag (#117)
- ✅ Instant wand slot preservation: `ClearActiveSelection()` only clears active shape-category slot (#117)

### Completed in DevNotes #15
- ✅ Safekeeping sound fix: Protect plays SoundID.MaxMana at 0.5 volume; Unprotect plays SoundID.Unlock (#118)
- ✅ Stamp state preservation: `ClearActiveSelection()` no longer resets IsStampLocked/StampDelta/StampAnchorOffset/SelectionClickStep (#118)
- ✅ Comprehensive cloned repo analysis: MagicStorage security, ImproveGame networking, SilkyUI Framework, SerousCommonLib (`ClonedRepoAnalysis.md`)
- ✅ Instant wand selection isolation: `_instantSelection` + `StartInstantSelection/UpdateInstantSelection/ClearInstantSelection` + `GetVisualSelection()` — all 5 instant wands now bypass dual-slot system entirely (#119)
- ✅ MagicWiring merge schedule rewritten: detailed MP packet formats (Building 28B, Dismantling 25B, Replacement 26B), server execution flows, inventory sync via SyncEquipment, undo strategy, progressive mode MP policy
- ✅ Animation schedule expanded: thorough spritesheet cell management research (vanilla DrawAnimation, mount frame system, Calamity patterns, cell size conventions)

### Completed in DevNotes #16
- ✅ Building wand same-type slope overwrite: applies slope in-place without consuming items
- ✅ Container destruction for dismantling wand: unlocks locked chests, drops contents, destroys tile
- ✅ Configurable MaxOutlineThickness (default 10, range 1–100)
- ✅ Per-type infinite resource toggles: tiles, walls, grass seeds, wires, actuators
- ✅ WiringHelper split into per-wire/per-actuator infinite checks
- ✅ **Wand of Coating**: 6th wand family — apply/remove Illuminant, Echo, Actuated coatings; recipe, sound, UI panel
- ✅ Demon altar destruction protection (`AllowDemonAltarDestruction` config, default: false)

### Completed in DevNotes #17
- ✅ Wall/torch `WouldTileLoseSupport` fix: replacement skips tiles that would lose structural support (#145)
- ✅ Selection overlay 3-state render modes: `AlwaysFullShape`, `OnlyOutline`, `OutlineAndPartialFill` (#141)
- ✅ `AlwaysFullShape` flicker fix: fade-in animation no longer triggers on cursor movement (#146)
- ✅ Infinite resource 3-state redesign: `InfiniteOverride` enum (Default/ForceOn/ForceOff) per-type (#147)
- ✅ Seeds & Rails removed from Wand of Replacement UI (7→5 buttons) (#148)
- ✅ Container destruction tooltip on dismantling UI toggle (#149)
- ✅ Auto overlay tooltip expanded (#150)
- ✅ ExhaustionMode clarification: per-wand property, not config (#151)

### Remaining Alpha Polish
- ✅ Cardinal line thickness verified (#21 — circular brush)
- ✅ Instant wand interaction bugs fixed (#109, #111, #117)
- ✅ Even thickness and brush rasterization (#115, #116)
- ✅ Workshop description written (Steam BBCode format)
- ❌ Multiplayer testing notes (wiring packets done, other 5 families need packets — see `MultiplayerFixSchedule.md`)
- ❌ Advanced editor mode scaffolding (roadmap only)

### Completed in DevNotes #18a — Phase B
- ✅ Ammo counting fix: all 4 applicable wands (building, replacement, wiring, coating) now correctly count remaining ammo
- ✅ CardinalLine debounce fix: overlay no longer flickers during selection
- ✅ **StraightLine shape**: free-angle Bresenham line with variable-thickness circular brush (15 files modified)

### Completed in DevNotes #18b — Phase C Bug Fixes
- ✅ Wiring wand 200-item drop cap: suppress drops via `WorldGen.gen = true`, consolidate wire/actuator items to inventory via `GiveItemToPlayer()`
- ✅ Replacement wand wall drops: `WorldGen.gen = suppressDrops` instead of unconditional `gen = true`
- ✅ Building wand wall progressive batching: `WallBuildingInfo` struct, `EnqueueWallBuilding()`, `ProcessWallBuildingBatch()`, `OperationType.WallBuilding`
- ✅ Dismantling tree/chest order fix: tiles sorted top-to-bottom (ascending Y), `CanKillTile` deferred to execution time
- ✅ **Vacuum item collection**: `VacuumItemsInArea()` + `TryAbsorbItem()` in `BulkTileOperations.cs` — localized area scan, fills inventory stacks, teleports overflow to player
- ✅ Vacuum integrated into all 10 execution paths (6 instant + 4 progressive per-batch)
- ✅ **VacuumItems config option** (default: true) — Sandbox Options section, all 3 localization files

---

## Phase 2 — Boundary Noise System (deferred — very late stage)

Adds procedural jitter to shape boundaries for organic-looking terrain placement.
**Deferred**: Out of active TODO list. Very late stage feature, low priority.

- `NoiseSettings` struct: amplitude, bias (Positive / Negative / Both), seed
- Per-wand noise toggle
- Keyboard shortcut or UI button to regenerate noise with a new seed
- Noise unavailable in Instant mode (too disorienting for click-drag)
- Performance ceiling: noise only evaluated on boundary tiles

---

## Phase 3 — Preview / Commit / Discard Workflow (high value, high effort)

Transforms the mod from "immediate apply" to a staged-changes model for precision building.

- `StagedChange` struct and `ChangeBuffer` per player
- Overlay colour-coding: green (add), red (remove), yellow (replace)
- Info panel: required blocks, inventory status, shortfall warnings
- Ghost tile rendering at reduced opacity
- Intersection resolution for overlapping staged shapes
- Moving selection (offset staged changes before commit)

---

## Phase 4 — Designer Wands (depends on Phase 3)

- Selection Wand: boolean region operations (Add / Remove / Intersect / XOR)
- Drawing Wand: paint tile placements without committing
- Commit Wand: trigger commit from any selection state
- Resource tracking across commit buffer

---

## Phase 5 — QoL & Advanced Features

- Multi-level undo / redo
- Max canvas area limit (configurable cap)
- Save / load structures (`TagCompound` tile snapshots)
- Flood fill mode
- HUD element showing active wand info
- Screen-edge indicators for off-screen tiles — three candidate approaches:
  - A) Arrow indicators at screen edges pointing toward selection
  - B) Minimap dot/region overlay
  - C) Distance label ("Selection: 45 tiles NW")
  - Distance-based colour parameterization (nearer = brighter, farther = dimmer)
- **Cross-mod gem support**: When Thorium Mod is loaded, add Opal and Aquamarine to AnyGem recipe group via `ModLoader.TryGetMod("ThoriumMod")` in `AddRecipeGroups()`
- Wand of Erosion: erosion algorithms for deepening holes, shaping terrain

---

## Phase 6 — MagicWiring Merge (see `MagicWiringMergeSchedule.md`)

Absorbs MagicWiring features into the Wand of Wiring using Option B (Unified Multi-Tool):

- M1: Port shape helper and distance clamping ✅
- M2a: **Wiring packets done** (#112) ✅
- M3: Wire/actuator inventory consumption ✅ (#113)
- **M2b: Building/Dismantling/Replacement/Coating MP packets** — **highest priority**, 7-day schedule in `MultiplayerFixSchedule.md`
- **M2c: Safekeeping MP sync** — Day 1 prerequisite (SafekeepingSync packet), enables server-side protection checks
- M4: Per-wire colour overlay in selection (1 session, independent)
- M5: Deprecate standalone MagicWiring mod

Estimated: 7-day MP fix schedule (17–24 hours), then 1–2 sessions for M4+M5.
See [`MultiplayerFixSchedule.md`](MultiplayerFixSchedule.md) for the comprehensive day-by-day plan
and [`BetaReadinessEvaluation.md`](BetaReadinessEvaluation.md) for the full beta readiness assessment.

---

## Phase 7 — Multiplayer & Polish

- Server-side validation for all operations
- Multiplayer protection permissions (per-player protected areas, admin override)
- `TileLoader.CanPlace()` validation for modded tiles
- Workshop release (description, icon, tags)
- Attack speed modifier research (Fargo's Souls, Thorium — ensure use time is unaffected)

---

## Phase 8 — Editor Mode (high effort, experimental)

A double-layered indexed pixel art editor for Walls and Tiles.

**Architecture Sketch**:
- Two `RenderTarget2D` layers: Tile layer (foreground) and Wall layer (background)
- Each layer maps 1 pixel = 1 tile, using palette indices (not actual colours)
- Palette = array of tile/wall types available in player inventory
- Editor UI: colour picker (tile type selector), brush tools, fill, line, select
- "Commit" flushes the canvas to the world via existing `ExecuteBuilding` / `ExecuteWallBuilding`
- Undo integrates with existing `UndoManager` stack

This is essentially a constrained paint program where the "colours" are tile types
and the "canvas" is the game world. The dual-layer model (walls behind tiles) maps
naturally to Terraria's rendering order.

**Deferred**: Requires Phase 3 (Preview/Commit) as foundation.

---

## Animation & Use-Time Research (deferred)

- **Instant wands**: Should have looping use animation while click-dragging
- **Non-instant wands**: Single-use animation on each click, duration = `Item.useTime`
- **Attack speed modifiers**: Mods like Fargo's Souls and Thorium may modify `Item.useTime` /
  `Item.useAnimation` globally. Investigate `Item.attackSpeedOnlyAffectsWeaponAnimation` or
  override use time in `CanUseItem()` / `UseSpeedMultiplier()` to prevent wand timing drift.

---

## Architecture Notes

| Concern | Status |
|---|---|
| Per-wand settings | Separate classes per family — works well for different needs |
| `Execute*` methods | Each family has its own — deduplication possible but low priority |
| Overlay colours | Centralised in `WandColors.cs` |
| Shape modes | Filled + Hollow only (Outline removed as redundant) |
| Recipe groups | Centralised in `WandRecipeSystem.cs` |

---

*See [`TODO_FEATURES.md`](TODO_FEATURES.md) for granular status checklist.*

---

## Future Plans (Post-Beta)

These features are planned for future development after the beta evaluation period.
They are documented here for reference but are **not scheduled** for any specific release.
See also: [`Lorebook.md`](Lorebook.md) for thematic lore entries for each wand.

### Full Multiplayer Fix
- Complete multiplayer synchronization for all wand operations
- Network packets for building, dismantling, replacement, and safekeeping wands
- Server-side validation of operations
- See `MultiplayerFixSchedule.md` for the 7-day plan

### Wand of Torches (Torch God Lore)
- Dedicated wand for mass torch placement and management
- Lore tie-in: blessing from the Torch God after the event
- Auto-biome-matching torch placement (uses Torch God's Favor logic)
- Could support bulk torch replacement and torch line drawing
- Crafting concept: Torch God's Favor + multiple torch types + Mana Crystal

### Lore Entry Ideas
- **Wand of Coating**: Tied to the Painter NPC, bestowed after certain conditions (wand already implemented — lore pending)
- Each wand family gets a thematic origin story connecting to Terraria's world
- See `Lorebook.md` for full lore entries per wand family

### Amalgamated Wands
Combined wands that merge functionality from multiple wand families.
Wands product of the convergence of divinity and engineering — the point where
mortal craftsmanship matches the gods' power.
Higher crafting tier, requires components from the merged wand families.

- **Amalgamated Wand of Replacement** (Provisional name):
  A wand that replaces the first inventory row items with the second row items,
  enabling multiple simultaneous replacements in one go.

- **Amalgamated Wand of Coatings** (Provisional name):
  A wand that coats specific blocks based on inventory position — blocks or walls
  from the first row get coated with paints from the second row.
  Also capable of replacing paint colours from row 1 to row 2.

- **Amalgamated Wand of Destruction**:
  Destroys only a specified set of tiles. Features a TileSet that can scan, remove,
  and clear from targeted tiles.
  UI modes: dismantle, target, remove target.
  **Whitelist mode**: Destroy only whitelisted tiles.
  **Blacklist mode**: Destroy all tiles except blacklisted ones.
  Challenging implementation — may require new UI elements for mode switching.

### Simple Wand of Coating (Provisional name)
- Wand that paints and scrapes but based on positional inventory — follows vanilla
  painting conventions with paint consumption depending on config
- A stepping stone before the full Wand of Coating

### WandOfSelection — Custom Selections
- A meta-wand that lets the player define and store arbitrary selections
- Alters Custom Selection (already somewhere in the current known plans)
- Custom Selections don't get consumed when used
- Confirm wands can apply them on their initial position
- Stamp wands can anchor them and then apply them as stamping works
- Could support freeform, polygon, or flood-fill selection modes

### Beta Evaluation Notes
- Current state is near-beta quality for single-player
- All **6** wand families are functional with undo, progressive mode, and config
- **9 shapes** including StraightLine (free-angle Bresenham)
- **Primary beta blocker**: Multiplayer synchronization for 5 non-wiring families (7-day fix schedule)
- Item drop handling improved: vacuum system, wiring consolidation, configurable toggle
- Performance validated: shapes past 200 tiles no longer cause lag (overlay debounce working)
- Infinite resource config redesigned with 3-state per-type overrides (Default/ForceOn/ForceOff)
- Seeds and Rails removed from Wand of Replacement (shoddy behavior, not useful for replacement)
- Container destruction tooltip added to dismantling UI (#149)
- Selection overlay supports 3 render modes (#141)
- **See [`BetaReadinessEvaluation.md`](BetaReadinessEvaluation.md) for the comprehensive assessment**
