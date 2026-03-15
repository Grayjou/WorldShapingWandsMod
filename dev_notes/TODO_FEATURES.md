# TODO Features

> **Legend:** ✅ Implemented &nbsp;|&nbsp; 🔄 In Progress / Partial &nbsp;|&nbsp; ❌ Not Started
> 
> Updated after DevNotes #18 (Phase C bug fixes)

---

## SimpleWands (Apply Actions Immediately)

### Mode cycling & selection

- ✅ Four variations per wand — right-clicking in inventory cycles through modes
- ✅ Modes: **Instant** (click-and-drag), **Select** (click start → click end), **Confirm** (click start → click end → click confirm), **Stamp** (define template → repeat)
- ✅ Right-click while selecting cancels the selection with fade animation
- ✅ Right-click while *not* selecting opens the wand settings UI
- ✅ Shape overlays shown during selection (screen-culled)
- ✅ Dual-sprite system: inventory sprites vs swing sprites
- ✅ Lore tooltips (Shift-gated) with divine common lore and wand-specific lore
- ✅ Shimmer decraft: all wands decraft into Amethyst (follows vanilla Shellphone convention)
- ✅ Cursor highlight: pulsing tile highlight at cursor when holding wand, circle brush preview for thick lines
- ✅ Dual-selection storage: large-shape slot + small-shape slot prevents cross-category leaks (#114)
- ✅ Selection compatibility: visual suppression of incompatible inherited selections (#114)
- ✅ Instant wand overlay: shows own selection during drag, hides inherited selections (#117)
- ✅ Instant wand slot preservation: `ClearActiveSelection()` only clears own shape-category slot (#117)
- ✅ Instant wand UseItem guard: `EnsureSelectionCompatibility` skipped for OneClick wands (#109)
- ✅ Right-click inventory cycle: `!mouseInterface` guard prevents cycling in NPC shops (#111)
- ❌ Screen-edge indicators for off-screen tiles — three candidate approaches:
  - **A) Arrow indicators**: Draw directional arrows at screen edges pointing toward off-screen selection corners
  - **B) Minimap dot**: Render selection bounds as a coloured region on the minimap
  - **C) Distance label**: Show "Selection: 45 tiles NW" text when selection is off-screen
  - **Colour parameterization**: Selection overlay colour shifts based on distance — nearer = brighter, farther = dimmer/shifted hue
- ✅ Instant wand stamp state preservation: `ClearActiveSelection()` no longer clears stamp state (#118)
- ✅ Instant wand selection isolation: instant wands use `_instantSelection` field, completely bypassing the dual-slot system (#119)
- ❌ Configurable persistent start-mode across hotbar changes

### Wand of Building ✅

- ✅ Places first matching item from inventory (hotbar priority, then main inventory, then ammo slots)
- ✅ Respects `PlaceType` (Solid, Platform, Rope, Rail, GrassSeed, PlantPot, Wall)
- ✅ Wall placement mode (separate `ExecuteWallBuilding` path)
- ✅ Inventory stock check before placement
- ✅ Infinite-resource mode (configurable threshold)
- ✅ Undo support
- ✅ Behaviour when block runs out mid-placement (config: NextBlock / Interrupt / Cancel)
- ✅ Block-replacement mode (replace existing tiles with selected block, respecting pick power)
- ✅ Settings UI: Object selector, Slope selector (6 slopes), Shape selector
- ✅ Sound effect on successful placement (Terraria API native sounds)
- ✅ Same-type tile slope overwrite: applies slope in-place without consuming items when tile type matches
- ✅ Recipe (Instant only): 10 Wood, 10 Gray Brick, 10 Red Brick, 20 Rope, 1 Mana Crystal @Anvil

### Wand of Dismantling ✅

- ✅ Destroys tiles and/or walls in a shape
- ✅ Pick-power validation (`HasEnoughPickPowerToHurtTile`)
- ✅ `CanKillTile` check for indestructible tiles
- ✅ Drop suppression toggle (`SuppressDrops`)
- ✅ Progressive placement mode (batched waves with natural effects)
- ✅ Settings UI: toggle tile destruction, toggle wall destruction, shape selector
- ✅ Undo support (tile + wall state both captured)
- ✅ Sound effect on successful dismantling (SoundID.Tink, conditional: SuppressDrops && EnableWandSounds)
- ✅ Recipe (Instant only): 10 Wood, 20 Rope, 5 Gemstone, 50 Dynamite, 1 Mana Crystal @Anvil
- ✅ Multiplayer fix: skip WorldGen.gen in MP for proper per-tile network sync
- ✅ Container destruction toggle (chests, dressers) with auto-unlock and item dropping (#118)
- ✅ Demon altar destruction protection (`AllowDemonAltarDestruction` config, #124)
- ✅ Container destruction tooltip explaining "Destroy Tiles" requirement (#149)
- ✅ Tree/chest block order fix: top-to-bottom sort + deferred `CanKillTile` at execution time

### Wand of Replacement ✅

- ✅ Replaces every instance of source-type tiles with target-type tiles in selection
- ✅ Pick-power validation
- ✅ `CanKillTile` check
- ✅ Inventory consumption of replacement tiles
- ✅ Undo support
- ✅ Settings UI: Source / Target ObjectType selectors (Tile, Platform, Rope, PlanterBox, Wall — seeds and rails removed in #148)
- ✅ Air as target type — erases matching tiles
- ✅ Wall-to-wall replacement mode (#37) — uses WallType/KillWall/PlaceWall/WallFrame
- ✅ Substrate-variant awareness (dirt ↔ grass, mud ↔ jungle grass, etc.)
- ✅ Progressive replacement mode (batched waves)
- ✅ Sound effect on successful replacement (SoundID.Item29 @ 0.25 vol, conditional)
- ✅ Recipe (Instant only): 1 Any Wand of Building + 1 Any Wand of Dismantling + 1 Mana Crystal @Anvil
- ✅ Multiplayer fix: skip WorldGen.gen in MP for proper per-tile network sync
- ✅ Wall/torch support detection: `WouldTileLoseSupport()` protects wall-anchored objects (#145)
- ✅ Target type deselection toggle (`AllowDeselect`, #139)


### Wand of Wiring ✅

- ✅ Place or remove red/green/blue/yellow wire and actuators
- ✅ Shape-based area wiring
- ✅ Four selection modes
- ✅ Settings UI with wire type toggles and place/remove mode   
- ✅ Sound effect on successful wiring (SoundID.Item64, conditional: EnableWandSounds)
- ✅ Recipe (Instant only): 1 Wire Kite, 50 Wire, 10 Actuator @Tinkerer's Workbench
- ✅ **Proper multiplayer wiring packets** — `WandPacketHandler` with server-side execution + broadcast (#112)

#### Missing from MagicWiring integration

- ❌ **Per-wire colour overlay** — wiring-specific colour coding
- ❌ **Distance clamping with visual feedback**
- ✅ **Wire/actuator inventory consumption** — implemented in #113

### Wand of Safekeeping ✅

- ✅ Protect/unprotect tiles and walls in a shape
- ✅ Persistent protection (saved with world via TagCompound)
- ✅ Session protection (cleared on world exit)
- ✅ Protection overlay rendering
- ✅ All other wands check `SafekeepingSystem.IsProtected()` before modifying tiles
- ✅ Settings UI: Mode toggle, tile/wall protection toggles, shape selector
- ✅ Sound effect on protection (SoundID.MaxMana @ 0.5 vol) and unprotection (SoundID.Unlock), conditional: EnableWandSounds (#118)
- ✅ Recipe (Instant only): 5 Any Gold Bar, 10 Any Silver Bar, 5 Gemstone, 20 Obsidian, 10 Any Evil Stone, 1 Mana Crystal @Anvil

### Wand of Coating ✅ (Added DevNotes #15–#16)

- ✅ Paint tiles with selected colour, illuminant coating, echo coating
- ✅ Paint walls with selected colour, illuminant coating, echo coating
- ✅ Scrape moss from tiles
- ✅ Harvest moss (drops moss items)
- ✅ Settings UI: mode selector (PaintTile/PaintWall/ScrapeMoss/HarvestMoss), paint colour picker, illuminant/echo toggles, shape selector
- ✅ Sound effects (Item109 for paint, Item131 for scrape/harvest)
- ✅ Recipe (Instant only): crafted at Tinkerer's Workbench
- ✅ Localization in all 3 language files

---

## Item Drop Handling ✅ (Added DevNotes #18b)

- ✅ Wiring wand drop consolidation: `WorldGen.gen = true` suppression + `GiveItemToPlayer()` stacking
- ✅ Replacement wand wall drops: `WorldGen.gen = suppressDrops` per-tile instead of unconditional
- ✅ Vacuum item collection: `VacuumItemsInArea()` + `TryAbsorbItem()` — localized area scan
- ✅ Vacuum fills existing inventory stacks → empty slots → teleports overflow to player position
- ✅ Vacuum integrated into 10 execution paths (6 instant + 4 progressive per-batch)
- ✅ VacuumItems config option (default: true, Sandbox Options section)
- ✅ MP-aware: `MessageID.SyncItem` sent for absorbed/teleported ground items

---

## Progressive Wall Batching ✅ (Added DevNotes #18b)

- ✅ `WallBuildingInfo` struct (Position, IsReplacement, SuppressDrops)
- ✅ `EnqueueWallBuilding()` method in `ProgressiveTileProcessor`
- ✅ `ProcessWallBuildingBatch()` with proper gen toggling, `WallFrame` per batch
- ✅ `OperationType.WallBuilding` enum value and `FinalizeOperation` case
- ✅ Building wand branches to progressive wall batching when count > batch size

---

## Display & Feedback

### Dimension Display ✅

- ✅ W × H dimension label rendered near cursor (UI-scaled, screen-clamped)
- ✅ Shape-specific GetDisplayDimensions (CardinalLine shows actual line length, Elbow shows segment lengths)
- ✅ Debounced area tile count display next to dimensions
- ✅ Configurable offset position with full screen-bounds clamping (Math.Clamp)

### Sound Effects ✅

- ✅ Building: Terraria API native placement sounds (no mod override needed)
- ✅ Dismantling: SoundID.Tink (conditional: SuppressDrops && EnableWandSounds)
- ✅ Replacement: SoundID.Item29 @ Volume 0.25f (conditional: SuppressDrops && EnableWandSounds)
- ✅ Wiring: SoundID.Item64 / GrandDesignSound (conditional: EnableWandSounds)
- ✅ Safekeeping: Protect = SoundID.MaxMana @ 0.5 vol, Unprotect = SoundID.Unlock (conditional: EnableWandSounds) (#118)
- ✅ EnableWandSounds config toggle (default: true)

### Selection Overlay ✅

- ✅ Screen-culled tile rendering
- ✅ Cancelled selection fade animation with rising "Cancelled" text
- ✅ Tile set caching (invalidated on change)
- ✅ Instant wand overlay during click-drag (#117)
- ✅ Instant wand uses isolated `_instantSelection` — never touches dual-slot (#119)
- ✅ Three render modes: Auto / AlwaysFullShape / AlwaysBoundingBox (#141)
- ✅ Auto mode debounced fade-in for shapes >200 tiles
- ✅ AlwaysFullShape flicker fix (#146) — guarded `_debounceFadeIn` reset

---

## Recipe Groups ✅

- ✅ Any Gem (Amethyst through Amber) — labeled "Gemstone"
- ✅ Any Gold Bar (Gold / Platinum)
- ✅ Any Silver Bar (Silver / Tungsten)
- ✅ Any Evil Stone (Ebonstone / Crimstone)
- ✅ Any Wand of Building (all 4 modes)
- ✅ Any Wand of Dismantling (all 4 modes)
- ✅ **Thorium gem support**: Add Opal and Aquamarine to AnyGem when Thorium Mod is loaded

---

## Lore System ✅

- ✅ Common divine lore: "The Gods of Space let you enact your thoughts into reality."
- ✅ Per-wand specific lore (override `WandLore`)
- ✅ `ShowDivineLore` virtual property — Wiring wand overrides to false (non-divine lore)
- ✅ Shift-gated tooltip display

---

## Shape & Outline System ✅

- ✅ 8 shape types: Rectangle, Ellipse, Diamond, Triangle, Elbow, CardinalLine, HalfEllipseH, HalfEllipseV
- ✅ StraightLine shape: free-angle Bresenham line with variable-thickness circular brush
- ✅ 2 fill modes: Filled, Hollow
- ✅ Variable outline thickness (0–configurable max via `MaxOutlineThickness`, default 10, range 1–100)
- ✅ Equal Dimensions toggle (square/circle constraint)
- ✅ CardinalLine circular brush thickness (Euclidean distance, cached offsets)
- ✅ Centralised OutlineHelper
- ✅ Even thickness: diameter-based instead of radius-based for even values (#115)
- ✅ Brush rasterization ≥4: uses `EllipseShape.GetCircleBrushOffsets()` IncrementalFast O(W+H) algorithm (#116)
- ✅ Shape slicing: Full / HalfHorizontal / HalfVertical with `SliceHelper` (#138)
- ✅ Connect diameter toggle for sliced hollow shapes
- ✅ `ToShapeContext()` centralised shape parameter conversion used by all wands


---

## Configuration ✅

- ✅ `WandConfig` — infinite resource threshold, suppress drops, bypass pickaxe power
- ✅ VacuumItems config toggle — auto-collect dropped items into inventory (default: true)
- ✅ Per-type infinite resource overrides with 3-state `InfiniteOverride` enum: Default/ForceOn/ForceOff (#147)
- ✅ Per-type infinite thresholds (tiles, walls, grass seeds, wires, actuators)
- ✅ Configurable max outline thickness
- ✅ Progressive mode with configurable batch size and interval
- ✅ Selection distance caps (Small/Big/Hollow)
- ✅ Show lore tooltips toggle
- ✅ Show dimensions toggle
- ✅ EnableWandSounds toggle (default: true)
- ✅ SafekeepingClearThreshold (configurable confirmation threshold)
- ✅ AllowDemonAltarDestruction toggle (#124)
- ✅ OverlayRenderMode (Auto/AlwaysFullShape/AlwaysBoundingBox) (#141)

---

## Undo System ✅

- ✅ Per-player undo stack (20-deep)
- ✅ Tile state captured (type, frame, slope, half-block, wall type)
- ✅ `/undo` chat command (count, all, status) for dev testing
- ❌ Redo
- ❌ Multi-level undo for preview-mode commits

---

## Future — Preview / Designer Workflow

- ❌ Staged change buffer with commit/discard
- ❌ Overlay colour-coding (green=add, red=remove, yellow=replace)
- ❌ Ghost tile rendering at reduced opacity
- ❌ Resource delta display before commit
- ❌ Moving selection (offset staged changes)

---

## Future — Multiplayer & Polish

- ✅ Basic multiplayer fix: Dismantling/Replacement skip WorldGen.gen in MP for per-tile network sync
- ✅ Wiring packets: full client→server→broadcast pipeline (#112)
- ❌ **M2b (HIGH PRIORITY)**: Packet-based sync for Building, Dismantling, Replacement, Coating — see `MultiplayerFixSchedule.md` (7-day plan)
- ❌ M2c (deferred): Safekeeping MP sync — must be done FIRST (server needs protection data for all other wand validations)
- ❌ Server-side validation for all wand operations
- ❌ Multiplayer protection permission system (per-player areas, admin override)
- ❌ `TileLoader.CanPlace()` validation for modded tiles
- ❌ Workshop release polish
- ❌ Attack speed modifier research (Fargo's Souls, Thorium use-time interference)

---

## Localization ✅

- ✅ English (en-US) localization file with all UI, Config, Items, Conditions keys
- ✅ Chat messages extracted to `Messages.*` localization keys (60+ strings)
- ✅ `Msg` utility helper for accessing localized messages
- 🔄 French (fr-FR) localization — file structure created, awaiting translation
- ❌ Spanish (es-ES) localization — lore entries and tooltips missing
- ❌ Additional language support

---

## MagicWiring Merge (see `MagicWiringMergeSchedule.md`)

- ✅ M1: Shape helper & distance clamping port
- ✅ M2a: Packet-based MP sync for wiring family (#112)
- ✅ M3: Wire/actuator inventory consumption (#113)
- ❌ **M2b: Packet-based MP sync for Building, Dismantling, Replacement, Coating** (7-day schedule, see `MultiplayerFixSchedule.md`)
- ❌ M2c: Safekeeping MP sync (Day 1 of MP fix schedule — prerequisite for all others)
- ❌ M4: Per-wire colour overlay (1 session, independent)
- ❌ M5: Deprecate standalone MagicWiring

---

## Safekeeping Multiplayer (deferred)

- ❌ Per-player protected areas (each player has independent protection set)
- ❌ Admin/host override capability
- ❌ Configurable: per-player mode disabled by default
- ❌ Protection persistence per-player in world save
- 📝 Reference: MagicStorage GUID-based ownership model studied — see `ClonedRepoAnalysis.md` §1.2–1.3
- 📝 Reference: ImproveGame NetPasswordSystem studied (too simple) — see `ClonedRepoAnalysis.md` §2.6

---

## Future — Cross-Mod Compatibility


- ❌ **Wand of Erosion**: Erosion algorithms for deepening holes, shaping terrain

---

## Concerns & Open Questions

- **Undo complexity**: reversing operations may leave multi-tile objects (doors, furniture) in broken states
- **Flood fill**: potential future feature — fill contiguous regions of matching tiles
- ~~**Wiring consumption**: currently free — should match vanilla wire consumption eventually~~ ✅ Fixed: wires/actuators consumed with per-type infinite toggles
- **Max canvas limit**: large operations can cause frame spikes — progressive mode mitigates but doesn't cap
