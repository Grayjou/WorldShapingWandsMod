# WorldShapingWands Mod — Issues & Fixes Tracker

## Status Legend

- ✅ **Fixed** — implemented and compiling
- 🔜 **Deferred** — valid issue, needs more design/testing
- ❌ **Rejected** — wrong approach or not needed

---

## Summary Table

| # | Issue | Status | Complexity |
|---|-------|--------|-----------|
| 1 | Sloped platform dual behavior | ✅ Fixed | Low |
| 2 | UI click interference | ✅ Fixed (all 5 instant wands) | Low |
| 3 | Performance: tile sounds | ✅ Fixed (subsumed by #18) | Medium |
| 4 | Tile/wall merging | ✅ Partly fixed via #1 | Low |
| 5 | Staff holding animation | ✅ Fixed (corrected location) | Low |
| 6 | Organic diamond shapes | 🔜 New shape variant | Medium |
| 7 | Sparse straight lines | 🔜 New shape variant | Medium |
| 8 | Rename StraightLine | ✅ Fixed | Low |
| 9 | Diagonal wiring | ❌ Rejected | N/A |
| 10 | Item icons in messages | ✅ Fixed | Low |
| 11 | Dimension display accuracy | ✅ Fixed (Elbow override) | Low |
| 12 | Regular shapes (circle etc.) | ✅ Fixed (merged with #20) | High |
| 13 | Area calculation display | ✅ Fixed (debounced tile count) | Medium |
| 14 | Dimension positioning | ✅ Fixed (Math.Clamp + offset) | Low |
| 15 | Rename to Dismantling | ✅ Fixed | Low |
| 16 | Multi-tile objects | ✅ Fixed (skip, not reject) | Medium |
| 17 | Gravity block placement | ✅ Fixed (bottom-to-top sort) | Low |
| 18 | Bulk operations performance | ✅ Fixed (batch + progressive mode) | High |
| 19 | Wall merging issues | 🔜 External mod fix | Low |
| 20 | Equal dimension snapping | ✅ Fixed (merged with #12) | Medium |
| 21 | Cardinal line thickness | ✅ Fixed (#108 supersedes) | Low |
| 22 | Omnidirectional lines | 🔜 New shape feature | High |
| 23 | Enhanced sound effects | ✅ Fixed | Low |
| 24 | Global sandbox config | ✅ Fixed | Medium |
| 25 | Replace tiles under chests | ✅ Fixed (ReplaceTile + fallback) | Medium |
| 26 | Grass/dirt variant detection | ✅ Fixed (substrate mapping) | Medium |
| 27 | Dual sprites (inventory vs use) | ✅ Fixed (base class pattern) | Low |
| 28 | Cross-wand selection persistence lag | ✅ Fixed (superseded by #98, #107) | Medium |
| 29 | Overlay performance (tile set caching) | ✅ Fixed (cached tile computation) | Medium |
| 30 | Wiring UI click unresponsive | ✅ Fixed (UserInterface.Draw) | Low |
| 31 | Undo-step keybind for multi-click modes | ✅ Fixed (Backspace keybind) | Medium |
| 32 | Dual sprite not loading on real items | ✅ Fixed (static dictionary) | Low |
| 33 | Wall mode for building wand | ✅ Fixed (PlaceType.Wall) | Medium |
| 34 | Cross-mod container support | 🔜 JSON-driven patching | High |
| 35 | Torch tiling system | 🔜 Separate wand needed | High |
| 36 | Advanced multi-pair replacement | 🔜 Separate wand needed | High |
| 37 | Replacement wall mode | ✅ Fixed | Medium |
| 50 | build.txt metadata (author, homepage) | ✅ Fixed | Low |
| 51 | description.txt rewrite | ✅ Fixed | Low |
| 52 | Replacement lore update | ✅ Fixed | Low |
| 53 | Wiring lore: suppress divine header | ✅ Fixed | Low |
| 54 | Crafting recipes (Building, Dismantling, Replacement) | ✅ Fixed | Medium |
| 55 | Recipe groups (AnyGem, AnyGoldBar, AnySilverBar, AnyEvilStone) | ✅ Fixed | Medium |
| 56 | Safekeeping recipe update (recipe groups) | ✅ Fixed | Low |
| 57 | Cardinal line circular brush algorithm | ✅ Fixed | Medium |
| 58 | Cursor highlight / starting area preview | ✅ Fixed | Medium |
| 59 | Recipe: only Instant mode has crafting recipe | ✅ Fixed | Low |
| 60 | Recipe: gemstone label fix | ✅ Fixed | Low |
| 61 | Recipe: replacement wand uses wand groups | ✅ Fixed | Low |
| 62 | Shimmer decraft (replaces cycling) | ✅ Fixed | Low |
| 63 | Sound effects: correct IDs, conditional, config | ✅ Fixed | Low |
| 64 | MP: dismantling and replacement not working | ✅ Fixed | High |
| 65 | Shimmer decraft instead of shimmer cycle | ✅ Fixed | Low |
| 66 | Wand UI close button / Escape not working | ✅ Fixed (all 5 panels) | Medium |
| 67 | Thorium gem support (Opal, Aquamarine) | 🔜 Future roadmap | Low |
| 68 | Remove shimmer tooltip (cosmetic) | ✅ Fixed | Low |
| 69 | MOT API tooltip scrubbing verified | ✅ Fixed (no change needed) | Low |
| 70 | Localization: hardcoded strings to hjson | ✅ Fixed (major refactor) | High |
| 71 | French localization placeholder | ✅ Fixed | Medium |
| 72 | Safekeeping ClearAll confirmation | ✅ Fixed | Low |
| 73 | /undo chat command (dev testing) | ✅ Fixed | Medium |
| 74 | MagicWiring merge schedule | 🔜 Documentation/Planning | Low |
| 75 | Showcase script | 🔜 Documentation | Low |
| 76 | Credits & acknowledgments | 🔜 Documentation | Low |
| 77 | Editor mode architecture sketch | 🔜 Roadmap | High |
| 78 | Animation cycles & use-time research | 🔜 Roadmap | Medium |
| 79 | Safekeeping multiplayer organization | 🔜 Roadmap | High |
| 80 | Steam Workshop page | 🔜 Deferred | Low |
| 81 | Issue archive structure | 🔜 Deferred | Low |
| 82 | Cross-mod recipe groups (Thorium etc.) | ✅ Fixed | Medium |
| 83 | Localization newlines for long labels | ✅ Fixed | Low |
| 84 | Wiring UI close button click-through fix | ✅ Fixed | Low |
| 85 | Ellipse rasterization Y-axis symmetry | ✅ Fixed | Medium |
| 86 | Triangle jitter on drag | ✅ Fixed | Low |
| 87 | MP replacement & dismantling (WorldGen.gen) | ✅ Fixed | Medium |
| 88 | EqualDimensions jitter fix | ✅ Fixed (Start-anchored) | Medium |
| 89 | French localization HJSON crash | ✅ Fixed | Low |
| 90 | Spanish localization `\n` literal | ✅ Fixed | Low |
| 91 | Start/End position highlight overlay | ✅ Fixed | Medium |
| 92 | MP dual-sync elimination | ✅ Fixed | Medium |
| 93 | Safekeeping MP persistence | 🔜 Future | High |
| 94 | Building wand MP drops | 🔜 Future | Medium |
| 95 | End marker EqualDimensions position | ✅ Fixed | Low |
| 96 | Wiring settings panel close button | ✅ Fixed (PanelHeight) | Low |
| 97 | Replacement Wall target removal + auto-infer | ✅ Fixed | Medium |
| 98 | Selection persistence (dual shape-category) | ✅ Fixed | High |
| 99 | Safekeeping client-side-only note | ✅ Fixed | Low |
| 100 | Building wand progressive batching | ✅ Fixed | High |
| 101 | Cardinal line thickness during casting | ✅ Fixed (#108 supersedes) | Medium |
| 102 | Wiring colorblind wire color icons | 🔜 Future | Medium |
| 103 | Even thickness for cardinal lines | ✅ Fixed (#108 supersedes) | Low |
| 104 | Ellipse odd width off-by-2 | ✅ Fixed (halfW rounding) | Medium |
| 105 | Wiring panel close button clipping | ✅ Fixed (PanelHeight 490→510) | Low |
| 106 | Cardinal line end marker position | ✅ Fixed (8-dir snap) | Medium |
| 107 | Selection click-step compatibility | ✅ Fixed (step tracking) | High |
| 108 | Cardinal line thickness in preview + even | ✅ Fixed (ToShapeContext + even) | Medium |
| 109 | Instant wands broken (EnsureSelectionCompatibility) | ✅ Fixed (skip OneClick in UseItem) | Critical |
| 110 | Overlay shows stale selection after compatibility clear | ✅ Fixed (visual suppression, not deletion) | Medium |
| 111 | Right-click inventory cycle cancels selection | ✅ Fixed (mouseInterface guard on HoldItem) | Low |
| 112 | Wiring MP packet handler (MagicWiring merge) | ✅ Fixed (WandPacketHandler) | High |
| 113 | Wiring InfiniteResource config support | ✅ Fixed (IsInfiniteResourceMode) | Medium |
| 114 | Selection compatibility redesign (preserve across wands) | ✅ Fixed (IsSelectionVisuallyActive) | High |
| 115 | Even thickness produces wrong brush size | ✅ Fixed (diameter-based, not radius-based) | Medium |
| 116 | Thickness brush rasterization pointy for ≥4 | ✅ Fixed (EllipseShape.GetCircleBrushOffsets) | Medium |
| 117 | Building wand same-type slope overwrite | ✅ Fixed (NeedsSlopeChange helper) | Medium |
| 118 | Chest destruction toggle for dismantling | ✅ Fixed (ContainerHelper + toggle) | High |
| 119 | Max outline thickness configurable | ✅ Fixed (MaxOutlineThickness config) | Low |
| 120 | Per-type infinite resource toggles | ✅ Fixed (5 per-type toggles) | Medium |
| 121 | Building wand platform same-type regression | ✅ Fixed (removed TileFrameX/18 comparison) | Medium |
| 122 | Grass seeds destroy dirt | ✅ Fixed (PlaceType.GrassSeed special path) | Medium |
| 123 | Per-type infinite thresholds | ✅ Fixed (5 per-type thresholds) | Medium |
| 124 | Demon Altars destroyed by dismantling wand | ✅ Fixed (AllowDemonAltarDestruction config) | Low |
| 125 | Pickaxe power tooltip missing | ✅ Fixed (BaseCyclingWand tooltip) | Low |
| 126 | Air counted as skipped in dismantling | ✅ Fixed (early continue for pure air) | Low |
| 127 | Triangle rasterization gaps | ✅ Fixed (column-height lerp) | High |
| 128 | Diamond rasterization gaps | ✅ Fixed (ceiling division for maxDx2) | Medium |
| 129 | Wand of Coating — new wand | ✅ Implemented (4 modes, UI, localization) | High |
| 130 | Locked chest unlock (golden/shadow keys) | ✅ Fixed (manual frameX shift for style 1) | High |
| 131 | Triangle right-angle at start corner | ✅ Fixed (orientation corrected) | Medium |
| 132 | Instant wands blocked when wand UI is open | 🔜 Debug in progress (this session) | Critical |
| 133 | Map drag activates instant wand | 🔜 Investigation needed | High |
| 134 | Wand of Coating paint color mismatches | 🔜 Debug mapping needed (this session) | Medium |
| 135 | Wand of Coating ScrapeMoss should harvest | 🔜 In progress (this session) | Medium |
| 136 | Golden key not consumed by Wand of Dismantling | 🔜 In progress (this session) | High |
| 137 | Wand of Coating: ScrapePaint redundant | 🔜 Remove ScrapePaint, expose ScrapeMoss | Low |
| 138 | Shape slicing not applied during execution | ✅ Fixed (ToShapeContext in all wands) | Critical |
| 139 | Target type deselection in replacement panel | ✅ Fixed (AllowDeselect toggle) | Medium |
| 140 | InfiniteResourceAmount config dead code | ✅ Fixed (removed; per-type thresholds work directly) | Medium |
| 141 | Overlay render mode config | ✅ Fixed (Auto/AlwaysFullShape/AlwaysBoundingBox) | Medium |
| 142 | Wall replacement deletes hanging objects | 🔍 Needs retest (check already present; may have been caused by #138) | Medium |
| 143 | WandPacketHandler missing slice/connectDiameter | ✅ Fixed (added to packet with backward compat) | Medium |
| 144 | Seed replacement behavior documentation | ✅ Documented (dev_notes/SeedReplacementBehavior.md) | Low |

---

## Deferred Issues (Open)

### #6 🔜 Improved Diamond Shape Rasterization

**Description**: Current diamond shapes are too mathematically perfect — edge offset accumulation looks ugly.

**Proposed**: UI toggle for "Organic Diamonds" with seeded random offsets (±1–2 tiles) based on position.

**Why Deferred**: Needs user testing to determine the right level of "organic" feel.

---

### #7 🔜 Sparse Straight Lines

**Description**: Straight line shapes should support sparse/organic distribution.

**Why Deferred**: Same determinism concerns as #6. Better as a new shape variant.

---

### #9 ❌ Diagonal Wiring Lines

Diagonal cardinal lines produce non-connected wires (4-way only). **Rejected** — uncommon use case with workarounds.

---

### #19 🔜 Wall Merging (Cheat Sheet)

**Description**: Cheat Sheet's paint tool missing `Framing.WallFrame()` calls. Not our bug — our `BulkTileOperations.BatchFrameUpdate` already handles both tile and wall frames.

---

### #22 🔜 Omnidirectional Lines with Thickness

**Description**: New "OmniLine" shape type allowing lines at any angle (not snapped to 8 directions).

**Recommendation**: Start with Perpendicular Expansion approach. Upgrade to Circular Brush if artifacts appear.

**Why Deferred**: Needs new shape provider, UI icon, enum value, registry entry.

---

### #34 🔜 Cross-Mod Container Support

**Description**: `Main.tileContainer[]` handles most modded chests, but some edge cases remain (dressers, barrels, modded fishing machines).

**Proposed**: JSON-driven patching system — ship a `ContainerPatches.json`.

---

### #35 🔜 Torch Tiling System

**Description**: Place torches in a diamond/rhombus pattern. Torch God's Favor must be respected. Echo coating option needed. Multi-session feature.

---

### #36 🔜 Advanced Multi-Pair Replacement Wand

**Description**: Map each tile in inventory row N to the tile directly below it in row N+1 for multi-replacement.

**Why Deferred**: New wand type, new UI, new inventory-scanning logic.

---

### #67 🔜 Thorium Gem Support

**Plan**: When Thorium Mod is loaded, add Opal and Aquamarine to the AnyGem recipe group.

---

### #74 🔜 MagicWiring Merge Schedule

See `dev_notes/MagicWiringMergeSchedule.md`.

---

### #75 🔜 Showcase Script

See `dev_notes/ShowcaseScript.md`.

---

### #77 🔜 Editor Mode Architecture Sketch

Architecture sketch added to Roadmap (Phase 8) — double-layered indexed pixel art editor for Walls and Tiles.

---

### #78 🔜 Animation Cycles & Use-Time Research

Research notes added to Roadmap. Need to test with Fargo's Souls Enchantments active.

---

### #79 🔜 Safekeeping Multiplayer Organization

Sketched in Roadmap. Requires MP packet infrastructure from MagicWiring merge.

---

### #93 🔜 Safekeeping MP Persistence

Requires implementing a full mod packet system for bidirectional sync. Architecture reference: ClonedImproveGame's `NetModule` + `[AutoSync]` pattern.

---

### #94 🔜 Building Wand MP Drops

Wand of Building replaces blocks correctly in MP but doesn't drop the former blocks. Requires same MP packet infrastructure as #93.

---

### #102 🔜 Wiring Colorblind Wire Color Icons

Would require new icon assets, a config option, and a UI toggle between icon/label modes.

---

## DevNotes #11 — Resolved

**Issues reported**: 7 items. **Resolved**: 6 implemented. **Deferred**: 1 (torch wand, #35).

### #117 ✅ Building wand same-type slope overwrite

**Problem**: When the building wand encounters a tile of the same type, it previously skipped it entirely, even if the slope was different.

**Solution**: Added `NeedsSlopeChange(x, y, slopeType)` helper. Instead of skipping same-type tiles, the wand now applies the new slope in-place using `ApplySlope()` without destroying/rebuilding the tile.

**Files**: `Content/Items/WandOfBuildingBase.cs`

---

### #118 ✅ Chest destruction toggle for dismantling

**Problem**: Chests blocked the dismantling wand because `WorldGen.CanKillTile` returns false for tiles under containers.

**Solution**: Added `DestroyContainers` boolean to `WandOfDismantlingSettings`. Added `ContainerHelper` utility class with container detection, locked chest unlocking, item dropping, and tile cleanup.

**Files**: `Common/Settings/WandOfDismantlingSettings.cs`, `Common/Utilities/ContainerHelper.cs`, `Content/Items/WandOfDismantling.cs`, `Common/UI/DismantlingSettingsPanel.cs`, all 3 localization files

---

### #119 ✅ Max outline thickness configurable

**Solution**: Added `MaxOutlineThickness` config option (default 10, range 1–100) to `WandConfig`. Updated all 5 settings panels.

---

### #120 ✅ Per-type infinite resource toggles

**Solution**: Added 5 per-type toggles: `InfiniteTiles`, `InfiniteWalls`, `InfiniteGrassSeeds`, `InfiniteWires`, `InfiniteActuators`. All default to `true`.

---

## DevNotes #12 — Resolved

**Issues reported**: 6 items. **Resolved**: 6 implemented.

### #121 ✅ Platforms break on same-type check

**Root cause**: `TileFrameX / 18` comparison for platforms was fragile — sloped platforms modify TileFrameX.

**Fix**: Removed `TileFrameX / 18` style comparison. Now uses simple `TileType == tType` for ALL tile types.

**Files**: `Content/Items/WandOfBuildingBase.cs`, `Common/Systems/ProgressiveTileProcessor.cs`

---

### #122 ✅ Grass seeds destroy dirt instead of planting

**Root cause**: The building wand treated grass seeds like any other tile — entering the replacement path which calls `WorldGen.KillTile` first.

**Fix**: Added `PlaceType.GrassSeed` special path. When source is a grass seed and a tile exists, calls `WorldGen.PlaceTile` directly WITHOUT destroying the existing tile.

**Files**: `Content/Items/WandOfBuildingBase.cs`, `Common/Systems/ProgressiveTileProcessor.cs`

---

### #123 ✅ Per-type infinite thresholds

**Root cause**: Single `InfiniteResourceAmount` threshold for all resource types.

**Fix**: Added 5 per-type threshold configs: `InfiniteTileThreshold` (999), `InfiniteWallThreshold` (999), `InfiniteGrassSeedThreshold` (50), `InfiniteWireThreshold` (999), `InfiniteActuatorThreshold` (999).

**Files**: `Common/Configs/WandConfig.cs`, `Content/Items/WandOfBuildingBase.cs`, `Content/Items/WandOfReplacementBase.cs`, `Common/Utilities/WiringHelper.cs`, all 3 localization files

---

### #124 ✅ Demon Altars destroyed by dismantling wand

**Fix**: Added `AllowDemonAltarDestruction` config toggle (default false). In dismantling pre-validation, when tile type is `TileID.DemonAltar` AND toggle is off, forces skip.

**Files**: `Common/Configs/WandConfig.cs`, `Content/Items/WandOfDismantling.cs`, all 3 localization files

---

### #125 ✅ Pickaxe power tooltip missing

**Fix**: Added `PickaxeHint` tooltip line to `BaseCyclingWand.ModifyTooltips()` for Building, Dismantling, and Replacement wands.

**Files**: `Common/Items/BaseCyclingWand.cs`, all 3 localization files

---

### #126 ✅ Air counted as skipped in dismantling

**Fix**: Added early-continue check: `if (!tileData.HasTile && tileData.WallType <= WallID.None) continue;`

**Files**: `Content/Items/WandOfDismantling.cs`

---

## DevNotes #13 — Resolved

**Issues reported**: 10 items. **Resolved**: 10 implemented.

### Issue A ✅ Dirt (TileID.Dirt = 0) replacement silently erased tiles

**Root cause**: `TileID.Dirt == 0`. Both `WorldGen.ReplaceTile(x, y, 0, ...)` and the `if (info.TargetType > 0)` guard treated Dirt as "erase to Air".

**Fix**: Added an explicit `IsErase` boolean to `TileReplacementInfo`.

**Files**: `ProgressiveTileProcessor.cs`, `WandOfReplacementBase.cs`, `WandOfBuildingBase.cs`

---

### Issue B ✅ Demon Altar hammer check used Main.hardMode

**Root cause**: `(Main.hardMode || config.BypassPickaxePower)` was logically backwards.

**Fix**: Replaced with `GetPlayerMaxHammerPower(player) < 80 && !config.BypassPickaxePower`. Added `GetPlayerMaxHammerPower(Player)` helper.

**File**: `WandOfDismantling.cs`

---

### Issue C ✅ Right-click couldn't cancel an instant wand drag

**Root cause**: The `if (Main.mouseLeft)` drag branch had no right-click check.

**Fix**: Added right-click cancel check in all 5 instant wand `HoldItem` methods.

**Files**: All 5 instant wand `.cs` files

---

### Issue D ✅ Instant wands blocked when wand settings panel was open (partial)

**Root cause (then)**: `IsMouseOverUI()` returned true when `Main.LocalPlayer.mouseInterface` was true.

**Fix (then)**: Replaced `Main.LocalPlayer.mouseInterface` with `Main.playerInventory` in `IsMouseOverUI()`.

**Note**: Issue recurred and is tracked as #132 (ongoing — see DevNotes #15).

---

### Issue E ✅ Slope overwrite didn't apply unless tile replacement mode was on

**Fix**: Moved the same-type check to BEFORE the `if (!replaceMode) continue;` gate.

**File**: `WandOfBuildingBase.cs`

---

### Issue F ✅ Cycle message named the OLD mode

**Fix**: After `SetDefaults`, reads suffix from the new instance.

**File**: `BaseCyclingWand.cs`

---

### Issue H ✅ Wall replacement destroyed wall-anchored objects

**Fix**: Added check before `KillWall`: skip positions where `IsWallAnchoredObject(t.TileType)` returns true.

**File**: `WandOfReplacementBase.cs`

---

### Issue I ✅ Thickness brush diameter=2 same as diameter=3

**Fix**: Removed Euclidean fallback. All diameters ≥2 use the IncrementalFast ellipse algorithm.

**File**: `EllipseShape.cs`

---

### Issue J ✅ Building wand replaced JungleGrass/MushroomGrass with Mud

**Fix**: Added `IsTileVariantOf(existingTile.TileType, tType)` check. Substrate-family variants are skipped.

**File**: `WandOfBuildingBase.cs`

---

### #127/#128 ✅ Triangle and Diamond rasterization gaps

**Fix — Triangle**: Column-height lerp: `height = 1 + ⌈col × (H-1) / (W-1)⌉`. Guarantees every column height ≥ 1.

**Fix — Diamond**: Ceiling division: `(int)((numer + H - 1) / H)` instead of truncating `(int)(numer / H)`.

---

## DevNotes #14 — Resolved

**Issues reported**: 5 items. **Resolved**: 4. **Ongoing**: 1 (instant wand UI blocking, #132).

### ✅ UI/map drag fix

**Problem**: Instant wands were activated while dragging the full-screen map.

**Fix**: Added `Main.mapFullscreen` check to `IsMouseOverUI()`.

---

### #131 ✅ Triangle right-angle corner orientation

**Problem**: After the rasterization fix, the start point set the opposite corner from the right angle.

**Fix**: Triangle orientation corrected so the right-angle corner is at the start position.

**File**: `Common/Geometry/Shapes/TriangleShape.cs`

---

### #130 ✅ Locked chests not unlocked by dismantling

**Root cause**: `Chest.Unlock` doesn't handle style 1 (Gold Chest). `TryUnlockChest` called `Chest.Unlock` for style 1 and returned false.

**Fix**: Added manual frameX shift for Gold Chest (style 1): detect `tileType == TileID.Containers && style == 1`, check for `ItemID.GoldenKey`, consume it, call `ForceShiftChestFrame(x, y, tileType, -36)`.

**Also added**: `/unlock_chests` command scanning 11×11 area around player.

**Files**: `Common/Utilities/ContainerHelper.cs`, `Common/Commands/ShapeTestCommand.cs`

---

### #129 ✅ Wand of Coating implemented

**Implementation**: 8 new files, 4 modified files. Modes: PaintTile, PaintWall, ScrapePaint, ScrapeMoss. Full UI panel with 31-color picker, shapes, thickness.

**New files**: `CoatingMode.cs`, `WandOfCoatingSettings.cs`, `WandOfCoatingBase.cs`, `WandOfCoatingInstant/Select/Confirm/Stamp.cs`, `CoatingSettingsPanel.cs`

**Modified files**: `WandPlayer.cs`, `WandUISystem.cs`, `WandColors.cs`, `en-US_Mods.WorldShapingWandsMod.hjson`

---

## DevNotes #15 — Current Session

### #132 🔜 Instant wands blocked when wand UI is open

**Problem**: All instant wands fail to respond when any wand settings panel is open. Persisted 3 sessions.

**Hypothesis**: `IsCursorOverPanel()` calls `CoatingUI.ContainsPoint(mousePos)` on a `UIState` whose bounds may cover the full screen, causing it to always return `true` when the panel is visible — regardless of actual cursor position.

**Plan**: Add extensive debug output to each component of `IsMouseOverUI()`. Print results every N frames when an instant wand is held and any UI is open.

**Files**: `Common/Items/BaseCyclingWand.cs`, `Common/UI/WandUISystem.cs`

---

### #133 🔜 Map drag activates instant wand

**Problem**: Dragging the minimap with left-click activates the instant wand selection.

**Root cause**: `Main.mapFullscreen` is false for minimap (not full-screen map), but `Main.mouseLeft` is true during minimap drag. The wand can't distinguish minimap interaction from tile interaction.

**Investigation**: Find the flag/field indicating minimap mouse capture. Possibly `Main.mapFullscreenScale`, mouse-captured state, or a Terraria internal lock flag.

---

### #134 🔜 Wand of Coating paint color mismatches

**Problem**: UI paint color swatches don't match in-game visual results.

**Plan**: Add `/debug_paint` command that paints a row of test tiles and labels each with its color index in chat.

---

### #135 🔜 Wand of Coating ScrapeMoss should harvest

**Problem**: `ApplyScrapeMoss` converts the tile to stone but drops nothing. Vanilla behavior drops the moss item.

**Fix plan**: After converting the tile, call `Item.NewItem(...)` to drop the moss item. Add `MossTileToItem` dict mapping moss TileIDs → their ItemIDs.

---

### #136 ✅ Golden key not consumed by Wand of Dismantling

**Problem**: Locked gold chests were not unlocked by the dismantling wand even with a golden key in inventory.

**Root cause**: `ContainerHelper.TryUnlockChest` had the wrong vanilla chest style numbers. The old code treated style 1 as "Locked Gold Chest" and style 2 as "Shadow Chest", but the actual vanilla mapping (from decompiled `Chest.cs`) is:

| Style | TileFrameX      | Identity                |
|-------|-----------------|-------------------------|
| 1     | 36–70           | Gold Chest (UNLOCKED)   |
| 2     | 72–106          | Gold Chest (LOCKED)     |
| 3     | 108–142         | Shadow Chest (UNLOCKED) |
| 4     | 144–178         | Shadow Chest (LOCKED)   |
| 18–22 | various         | Biome Chests (UNLOCKED) |
| 23–27 | 828–1006        | Biome Chests (LOCKED)   |
| 35,37,39 | various      | Other (UNLOCKED)        |
| 36,38,40 | various      | Other (LOCKED)          |

**Fix**: Corrected `TryUnlockChest` — style 2 checks for `GoldenKey` (consumed), style 4 checks for `ShadowKey` (not consumed). Simplified bypass path to delegate to `Chest.Unlock()` which handles all vanilla locked styles natively. Removed dead `ForceShiftChestFrame` method and unused imports. Also simplified `UnlockChestsCommand.ForceUnlock` to just call `Chest.Unlock`.

---

### #137 🔜 Wand of Coating ScrapePaint button redundant

**Problem**: The ScrapePaint mode is redundant — selecting paint color 0 (None) already removes paint. The slot is better used for the more useful ScrapeMoss operation.

**Fix plan**: Remove the ScrapePaint UI button from `CoatingSettingsPanel`. Remove `CoatingMode.ScrapePaint` from enum and base class. Keep the 4-button mode row but with: PaintTile | PaintWall | ScrapeMoss (harvest) | (future).

---

### #138 ✅ Shape slicing not applied during execution

**Problem**: The shape overlay correctly showed sliced shapes (half ellipse, etc.) but execution always drew the full shape. Half ellipses, half diamonds, etc. placed tiles outside the visible overlay area.

**Root cause**: All 7 wand execution paths (Building×2, Dismantling, Coating, Wiring, Safekeeping, Replacement×2) constructed `ShapeContext` manually with `new ShapeContext(start, end, fillMode, thickness, ...)` — omitting the `slice` and `connectDiameter` parameters, which defaulted to `SliceMode.Full` and `true`. The overlay used `ShapeInfo.ToShapeContext()` which correctly passed all fields.

**Fix**: Replaced all manual `new ShapeContext(...)` calls with `settings.Shape.ToShapeContext(start, end, verticalFirst)` across all wand execution paths:
- `WandOfBuildingBase.cs` (tile + wall paths)
- `WandOfDismantling.cs`
- `WandOfCoatingBase.cs`
- `WandOfWiringBase.cs`
- `WandOfSafekeepingBase.cs`
- `WandOfReplacementBase.cs` (tile + wall paths)

---

### #139 ✅ Target type deselection in replacement panel

**Problem**: In the replacement panel, when source was Wall, the target was auto-set to Wall. If the user then clicked Air (to erase walls), they couldn't switch back to "walls" because there was no Wall button in the target section and the toggle prevented re-clicking already-selected buttons.

**Fix**: Added `AllowDeselect` property to `UIIconButton` — when true and the button is a radio button, clicking an already-toggled button deselects it. All 7 target type buttons in `ReplacementSettingsPanel` use `AllowDeselect = true`. Updated `SetTargetType` to handle deselection: clicking the already-selected target type sets `NewObject = OldObject` (same type as source).

---

### #140 ✅ InfiniteResourceAmount config dead code

**Problem**: The `InfiniteResourceAmount` config field had no effect when set to a non-zero value. It was only checked as `if (InfiniteResourceAmount == 0) return 0;` in `GetThresholdForPlaceType`/`GetThresholdForObjectType`, making it a confusing gate: 0 = always infinite, non-zero = use per-type thresholds. Users expected it to be an actual threshold value.

**Fix**: Removed `InfiniteResourceAmount` entirely. The per-type thresholds (`InfiniteTileThreshold`, `InfiniteWallThreshold`, etc.) now work directly — no gate needed. A threshold of 0 means "always infinite" for that type. Removed from: `WandConfig.cs`, `en-US/fr-FR/es-ES` localization files.

---

### #141 ✅ Overlay render mode config

**Problem**: User wanted to test the full shape overlay without the debounce optimization, and also wanted the option to always use the lightweight bounding box.

**Fix**: Added `OverlayRenderMode` enum (`Auto` | `AlwaysFullShape` | `AlwaysBoundingBox`) and corresponding config in `WandConfig`. `SelectionOverlay.DrawSelection` reads the config to override the `isLargeAndDragging` logic. `AlwaysFullShape` disables bbox fallback entirely; `AlwaysBoundingBox` forces bbox for any shape > 1 tile. Added to all 3 localization files.

---

### #142 🔍 Wall replacement deletes hanging objects — needs retest

**Problem**: Wall replacement reportedly deletes torches and other wall-anchored objects, despite the `IsWallAnchoredObject` check already being present (added in #82).

**Analysis**: The `IsWallAnchoredObject` check at line ~532 of `WandOfReplacementBase.ExecuteWallReplacement` correctly skips tiles with `TileObjectData.AnchorWall == true`. The reported issue may have been caused by #138 (shape slicing not applied during execution) — the full unsliced shape would have replaced walls at unexpected positions, potentially affecting tiles with torches outside the visible overlay area. Needs retesting after #138 fix.

---

### #143 ✅ WandPacketHandler missing slice/connectDiameter

**Problem**: The wiring wand multiplayer packet format didn't include `slice` or `connectDiameter`, so server-side execution always used `SliceMode.Full` and `connectDiameter = true` regardless of client settings.

**Fix**: Added `slice` (byte) and `connectDiameter` (bool) to the packet in `SendWiringOperation`/`HandleWiringOperation`. Uses backward-compatible reading with position check (`reader.BaseStream.Position < reader.BaseStream.Length`). Updated `WandOfWiringBase` call site to pass `settings.Shape.Slice` and `settings.Shape.ConnectDiameter`.

---

### #144 ✅ Seed replacement behavior documented

**Problem**: The behavior of replacing seeds (grass-type tiles) was undocumented and the interactions between substrate families, variants, and replacement modes were unclear.

**Fix**: Created `dev_notes/SeedReplacementBehavior.md` documenting: what counts as seeds, all replacement scenarios (seeds→seeds, seeds→air, seeds→tile, tile→seeds), the substrate variant system, consumption rules, and limitations (moss has no craftable item).

---

## MP Implementation Schedule

See `dev_notes/MultiplayerFixSchedule.md` for detailed task lists covering Phases A through F with specific implementation steps for Building, Dismantling, and Replacement wands.

---

## DevNotes #17 — Testing Feedback Session

### #145 ✅ Wall replacement/building destroys torches

**Problem**: When replacing or building walls, torches placed on those walls were destroyed. Trophies (wall-anchored paintings) were correctly preserved, but torches vanished.

**Root cause**: `TileHelper.IsWallAnchoredObject(ushort tileType)` only checked `TileObjectData.AnchorWall`, which is set for trophies and paintings but NOT for torches. Torches use `AnchorBottom`/`AnchorLeft`/`AnchorRight` depending on their placement direction — they anchor to adjacent solid blocks OR the wall behind them, but their `TileObjectData` doesn't declare `AnchorWall`.

**Fix**: Added `TileHelper.WouldTileLoseSupport(int x, int y)` — a position-aware check that:
1. Returns `false` for non-frame-important tiles (never depend solely on walls)
2. Returns `true` for tiles with explicit `AnchorWall` in their TileObjectData (trophies, paintings)
3. For 1×1 frame-important tiles (torches, etc.): checks 4 cardinal neighbours for solid blocks. If no adjacent solid block exists, the tile depends on the wall → returns `true` (skip this position)

Updated both `WandOfReplacementBase.ExecuteWallReplacement` and `WandOfBuildingBase.ExecuteWallBuilding` to use `WouldTileLoseSupport(tile.X, tile.Y)` instead of `IsWallAnchoredObject(t.TileType)`.

**Files**: `Common/Utilities/TileHelper.cs`, `Content/Items/WandOfReplacementBase.cs`, `Content/Items/WandOfBuildingBase.cs`

---

### #146 ✅ Overlay fade-in flicker in AlwaysFullShape mode

**Problem**: When `OverlayRenderMode` was set to `AlwaysFullShape`, the shape overlay flickered constantly — fading in from transparent every time the selection endpoint changed.

**Root cause**: The endpoint-change detection block in `SelectionOverlay.DrawSelection` reset `_debounceFadeIn = 0f` whenever `maxDim > LargeShapeThreshold`, regardless of render mode. In Auto mode this is correct (fade in after debounce), but in AlwaysFullShape mode the full shape always renders — so resetting the fade-in alpha to 0 on every mouse movement caused constant flickering.

**Fix**: 
1. Moved `renderMode` config read above the endpoint tracking block
2. Guarded `_debounceFadeIn = 0f` reset with `renderMode == OverlayRenderMode.Auto`
3. Guarded fade-in alpha advancement with `renderMode == OverlayRenderMode.Auto`

In AlwaysFullShape mode, the shape now renders at full opacity immediately with no fade-in.

**File**: `Common/Drawing/SelectionOverlay.cs`

---

### #147 ✅ Infinite resource config redesign (3-state per-type overrides)

**Problem**: The per-type infinite resource bools (`InfiniteTiles`, `InfiniteWalls`, etc.) were simple on/off toggles that required the master `EnableInfiniteResource` to be on. Users wanted more granular control: force a specific type to always be infinite even with master off, or force it to never be infinite even with master on.

**Fix**: 
1. Created `InfiniteOverride` enum: `Default` | `ForceOn` | `ForceOff`
2. Changed all 5 per-type properties from `bool` to `InfiniteOverride` (default value: `Default`)
3. Added `ResolveOverride(InfiniteOverride)` helper method
4. Updated `IsInfiniteForPlaceType()`, `IsInfiniteForObjectType()`, `IsInfiniteForWires`, `IsInfiniteForActuators` to use `ResolveOverride()` instead of checking `EnableInfiniteResource && boolToggle`
5. Logic: `ForceOff → false`, `ForceOn → true`, `Default → follows master toggle`
6. Thresholds unchanged — still work the same way (0 = always infinite, N = need N items)
7. Updated all 3 localization files with new tooltips explaining the 3 states

**Files**: `Common/Enums/InfiniteOverride.cs` (new), `Common/Configs/WandConfig.cs`, `en-US/fr-FR/es-ES` localization files

---

### #148 ✅ Remove Seeds and Rails from Wand of Replacement UI

**Problem**: Grass seed replacement was unreliable (substrate/variant interactions are too complex for clean replacement behavior). Rail replacement was similarly problematic and rarely useful. User verdict: remove both from the replacement wand.

**Fix**: Removed `_srcRailBtn`, `_srcSeedsBtn`, `_tgtRailBtn`, `_tgtSeedsBtn` from `ReplacementSettingsPanel`:
- Removed field declarations
- Removed texture loads for Rail and Seeds icons
- Reduced source row from 7 → 5 buttons (Tile, Platform, Rope, Planter, Wall)
- Reduced target row from 7 → 5 buttons (Tile, Platform, Rope, Planter, Air)
- Removed event wiring for those 4 buttons
- Removed from `UpdateSourceButtons()` and `UpdateTargetButtons()`
- Button width calculations updated from `IconBtnSize * 7 + IconGap * 6` to `IconBtnSize * 5 + IconGap * 4`

Note: Seeds and Rails remain valid in `ObjectType` enum and building wand — they're only removed from the replacement wand's UI.

**File**: `Common/UI/ReplacementSettingsPanel.cs`

---

### #149 ✅ Container destruction tooltip

**Problem**: The Wand of Dismantling's "Destroy Chests" button requires "Destroy Tiles" to also be enabled (because chests ARE tiles), but this wasn't communicated to the user.

**Fix**: 
1. Added `HoverText` property to `UIToggleButton` (renders via `Main.hoverItemName` on hover)
2. Set `_destroyContainersBtn.HoverText` to localized tooltip explaining the requirement
3. Added localization keys in all 3 language files

**Files**: `Common/UI/Elements/UIToggleButton.cs`, `Common/UI/DismantlingSettingsPanel.cs`, `en-US/fr-FR/es-ES` localization files

---

### #150 ✅ Overlay Auto mode tooltip expanded

**Problem**: The overlay render mode tooltip didn't explain what "Auto" actually does (the 200-tile threshold, debounce behavior, bounding box transition).

**Fix**: Expanded the tooltip text for all 3 modes in all 3 language files, explaining the 200-tile threshold and debounce mechanism.

**Files**: `en-US/fr-FR/es-ES` localization files

---

### #151 ℹ️ ExhaustionMode is per-wand, not config

**Clarification**: User asked why exhaustion mode wasn't in the config. It's intentionally a per-wand UI setting in `WandOfBuildingSettings.ExhaustionMode`, not a global config option. Each building wand instance can have different exhaustion behavior (Cancel vs Truncate).

**No changes needed.**
