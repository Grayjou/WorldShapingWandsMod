# WorldShapingWands Mod — Issues & Fixes Tracker

## Status Legend
- ✅ **Fixed** — implemented and compiling
- 🔜 **Deferred** — valid issue, needs more design/testing
- ❌ **Rejected** — wrong approach or not needed

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
| 21 | Cardinal line thickness | 🔜 Needs verification | Low |
| 22 | Omnidirectional lines | 🔜 New shape feature | High |
| 23 | Enhanced sound effects | ✅ Fixed | Low |
| 24 | Global sandbox config | ✅ Fixed | Medium |
| 25 | Replace tiles under chests | ✅ Fixed (ReplaceTile + fallback) | Medium |
| 26 | Grass/dirt variant detection | ✅ Fixed (substrate mapping) | Medium |
| 27 | Dual sprites (inventory vs use) | ✅ Fixed (base class pattern) | Low |
| 28 | Cross-wand selection persistence lag | ✅ Fixed (clear on wand switch) | Medium |
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
| 65 | Shimmer decraft instead of shimmer cycle | ✅ Fixed | Low |
| 66 | Wand UI close button / Escape not working | ✅ Fixed (all 5 panels) | Medium |
| 67 | Thorium gem support (Opal, Aquamarine) | 🔜 Future roadmap | Low |

---

## Fixed Issues (Concise)

### 1. ✅ Sloped Platform Dual Behavior Bug
Added `WorldGen.SquareTileFrame(x, y)` at end of `ApplySlope()` to force tile frame recalculation after slope changes. **Files**: `Content/Items/WandOfBuildingBase.cs`

### 2. ✅ UI Click Interference Bug
Added `mouseInterface` guard in the release branch of all 5 instant wands. Selection silently cleared when releasing over UI. Fixed `WandOfReplacementInstant` missing press-branch guard. **Files**: All 5 instant wand `.cs` files.

### 3. ✅ Performance: Tile Sounds (Subsumed by #18)
Fully resolved by #18 — `WorldGen.gen = true` suppresses all per-tile sounds, dust, and gore during bulk operations.

### 4. ✅ Tile/Wall Merging (Partly)
Addressed via #1. Normal placements already call `SquareTileFrame` internally. Only sloped tiles were missing the update. Wall merging may need `SquareWallFrame` if glitches are observed.

### 5. ✅ Staff Holding Animation
Applied `Item.staff[Type] = true` in `BaseCyclingWand.SetStaticDefaults()`. Note: `Item.staff` is a static array requiring `SetStaticDefaults`, not `SetDefaults`. **Files**: `Common/Items/BaseCyclingWand.cs`

### 8. ✅ Rename StraightLine to Cardinal Lines
Renamed `ShapeType.StraightLine` → `ShapeType.CardinalLine` across: enum, ShapeRegistry, all 5 panels, localization, WandPlayer, icon references, default settings, ShapeTestCommand. **Files**: 10+ files across Common/ and Content/.

### 10. ✅ Enhanced Chat Messages with Icons
Used Terraria's `[i:{itemType}]` chat tags in replacement messages. **Files**: `Content/Items/WandOfReplacementBase.cs`

### 12. ✅ Regular Shape Types — Merged with #20
Instead of adding Circle/Square/SquareDiamond shape types, a single "Equal Dimensions" toggle replaces all three. See #20.

### 15. ✅ Wand of Destruction → Wand of Dismantling
Renamed all user-facing strings. Internal class names kept (`WandOfDestruction*`) — zero user impact. **Files**: `Content/Items/WandOfDestruction.cs`, `Common/Settings/WandOfDestructionSettings.cs`, localization.

### 16. ✅ Multi-Tile Object Handling
Added `ItemTypeHelper.IsMultiTileItem(Item)` checking `TileObjectData` for Width/Height > 1. Building wand wraps item-finding condition with `!IsMultiTileItem(item)` so multi-tile objects are transparently skipped. **Files**: `Common/Utilities/ItemTypeHelper.cs`, `Content/Items/WandOfBuildingBase.cs`

### 17. ✅ Gravity-Affected Block Placement
Detects gravity tiles via `Main.tileSand[createTile]` and sorts `tilesToProcess` by Y descending (bottom-to-top). **Files**: `Content/Items/WandOfBuildingBase.cs`

### 18. ✅ Bulk Operations Performance
Four-pronged optimization:

1. **Batch Network Sync** — `BulkTileOperations` utility: `ComputeBounds` → `BatchFrameUpdate` + `BatchNetworkSync`. N per-tile packets → 1 batched (auto-chunks >200×200).
2. **Sound Suppression** — `WorldGen.gen = true` during loops suppresses all per-tile sounds/dust/gore.
3. **Undo Batching** — `UndoAction.Undo()` restores all tile data first, then single `FinalizeBatch()`.
4. **Progressive Mode** — Configurable timed batches (e.g., 400 tiles/0.3s) with `WorldGen.gen = false` so tiles drop naturally.

**Files**: `BulkTileOperations.cs` (new), `ProgressiveTileProcessor.cs` (new), all 3 wand bases, `UndoAction.cs`.

### 20. ✅ Equal Dimension Snapping Toggle — Merged with #12
Added `EqualDimensions` property to `ShapeInfo` and `ShapeContext`. `GetBounds()` uses `Math.Max(width, height)` centered on original rectangle. Toggle added to all 5 UI panels. **Files**: `ShapeInfo.cs`, `ShapeContext.cs`, all 5 UI panels, all 5 wand bases, localization.

### 24. ✅ Global Sandbox Config: SuppressDrops & BypassPickaxePower
Moved `SuppressDrops` from per-session UI to global `WandConfig`. Added `BypassPickaxePower`. All 3 wands + ProgressiveTileProcessor read from config. Removed UI toggle from DestructionSettingsPanel. **Files**: `WandConfig.cs`, `WandOfDestructionSettings.cs`, `DestructionSettingsPanel.cs`, all 3 wand bases, `ProgressiveTileProcessor.cs`, localization.

### 25. ✅ Replace Tiles Under Chests / Multi-Tile Objects
**Problem**: `WorldGen.CanKillTile()` returns false for tiles under multi-tile objects (chests, dressers), blocking all replacements even when tiles are mineable.

**Fix**: Switched replacement to use `WorldGen.ReplaceTile()` as primary method — handles tiles under furniture without destroying it. Falls back to `KillTile()` + `PlaceTile()` only when `ReplaceTile` fails. For Air targets (erase), `CanKillTile` still checked. Applied to both instant and progressive paths.

**Note**: Both WorldShapingWands and ImproveGame respect pickaxe power. The difference was only multi-tile object handling, not power bypass.

**Files**: `Content/Items/WandOfReplacementBase.cs`, `Common/Systems/ProgressiveTileProcessor.cs`

### 26. ✅ Grass/Dirt Variant Detection for Replacement
**Problem**: Replacing dirt didn't detect grass-covered variants — grassed/mossed areas were left untouched.

**Fix**: Added substrate→variant mapping to `ItemTypeHelper`:
- **Dirt** → Grass, Corrupt Grass, Hallowed Grass, Crimson Grass, all 10 Moss types
- **Mud** → Jungle Grass, Mushroom Grass  
- **Ash** → Ash Grass
- **Stone** → all 10 Moss types

Added `IsTileVariantOf(worldTile, sourceTile)` for O(1) dictionary lookups and `GetVariantTileTypes(tileType)` for full set retrieval. Both instant and progressive replacement paths use variant matching.

**Cross-mod**: Vanilla-only mapping. Modded grass variants need registration via public API (future enhancement).

**Files**: `Common/Utilities/ItemTypeHelper.cs`, `Content/Items/WandOfReplacementBase.cs`, `Common/Systems/ProgressiveTileProcessor.cs`

### 27. ✅ Dual Sprites: Inventory vs In-Use
**Problem**: Wand sprites are informational (action icons) but look odd during swing animation — a wand with a block attached makes no sense while dismantling.

**Fix**: Implemented in `BaseCyclingWand` (inherited by all 20 wands) via `PreDrawInInventory` / `PreDrawInWorld`:

| Context | Texture Used | Why |
|---|---|---|
| **Swing animation** | `WandName.png` (clean wand) | Game uses main texture for use animation |
| **Inventory / Hotbar / Tooltip** | `WandName_Inventory.png` (descriptive) | `PreDrawInInventory` intercepts, returns `false` |
| **Dropped in world** | `WandName_Inventory.png` (descriptive) | `PreDrawInWorld` intercepts, returns `false` |

**Graceful degradation**: If `_Inventory.png` doesn't exist, falls back to default drawing. Wands can be updated one at a time.

**Art setup**: Copy current descriptive `.png` → `_Inventory.png`, then edit main `.png` to remove action indicator layers.

**Files**: `Common/Items/BaseCyclingWand.cs`

### 28. ✅ Cross-Wand Selection Persistence Lag
**Problem**: Switching between wands (e.g., scroll wheel) while a selection was active caused catastrophic lag. A CardinalLine selection (1000-cap) inherited by a FilledTriangle wand with EqualDimensions produced a 999×999 area — ~500K tiles computed per frame.

**Root Cause**: `WandPlayer.PostUpdate()` checked `IsHoldingWandItem()` (any wand) but never detected that the *specific* wand changed. The old selection's offset persisted and was reinterpreted by the new wand's shape settings (which had a different cap), then `GetBounds().EqualDimensions` expanded both axes to `max(width, height) = 999`.

**Fix**: Added `_lastHeldItemType` tracking in `WandPlayer`. On held-item-type change, if a selection is active (`SelectionOwnerItemType != 0`), auto-clear it. This prevents any cross-wand selection bleeding.

**Files**: `Common/Players/WandPlayer.cs`

### 29. ✅ Overlay Tile Set Caching
**Problem**: `SelectionOverlay.DrawSelection()` called `ShapeRegistry.GetShapeTiles()` every frame to compute the set of tiles to highlight. For large or complex shapes, this was extremely wasteful — the result only changes when start/end position or shape settings change, not every frame.

**Fix**: Added a caching layer with `_cachedTiles` (HashSet<Point>), `_cacheStart`/`_cacheEnd` (Point), `_cacheShape` (ShapeInfo), and `_cacheValid` (bool). New `GetOrComputeTiles()` method checks whether start, end, shape type, fill mode, thickness, or equal dimensions have changed since last computation. Cache invalidated only on actual changes. The cancelled-selection overlay still recomputes (it runs briefly and is not a performance concern).

**Files**: `Common/Drawing/SelectionOverlay.cs`

### 30. ✅ Wiring UI Click Bug
**Problem**: Wand of Wiring's settings panel appeared but didn't respond to clicks after the first interaction. All UI elements were visually correct but functionally dead.

**Root Cause**: `WandUISystem.ModifyInterfaceLayers` called `panel.Draw(spriteBatch)` directly on each settings panel, bypassing the `UserInterface` instance (`_userInterface`). In tModLoader, `UserInterface.Draw()` manages click propagation, hover states, element focus tracking, and scroll handling — calling `UIElement.Draw()` directly renders the elements but skips all input processing.

**Fix**: Replaced all 5 direct `panel.Draw(Main.spriteBatch)` calls with a single `_userInterface.Draw(Main.spriteBatch, Main._drawInterfaceGameTime)`, which is the correct tModLoader UI pattern. Also removed ~14 leftover `Main.NewText("[DEBUG]...")` calls.

**Files**: `Common/UI/WandUISystem.cs`

### 31. ✅ Undo-Step Keybind for Multi-Click Modes
**Problem**: In Select/Confirm/Stamp modes, misclicks had no recovery path. Missing the anchor point by 100 tiles meant restarting the entire shape from scratch, even if the selection area was correct.

**Fix**: Added `UndoStep` keybind (default: Backspace) in `WandControls` (renamed from `ThicknessControls` to reflect broader responsibility). The keybind steps back one state in the multi-click sequence:

| Current State | Backspace Action | Result |
|---|---|---|
| Stamp locked | `UnlockStamp()` | Returns to "click to set anchor", keeps selection |
| Selection locked | `UnlockSelection()` | Returns to "move cursor to adjust end" |
| Selection active (unlocked) | `ClearSelection()` | Returns to "click to start" |

Added `UnlockStamp()` method to `WandPlayer` — clears stamp state but preserves locked selection. Each step shows feedback via `Main.NewText`.

**Files**: `Common/Input/WandControls.cs` (renamed from `ThicknessControls.cs`), `Common/Players/WandPlayer.cs`, `Localization/en-US_Mods.WorldShapingWandsMod.hjson`

### 32. ✅ Dual Sprite Not Loading on Real Items
**Problem**: `WandOfBuildingConfirm` had its art updated with separate inventory/use sprites, but both contexts showed the non-inventory version. The dual sprite system from #27 wasn't working.

**Root Cause**: `SetStaticDefaults()` runs only on the **template instance** (one per item type). Real item instances (created when the player picks up items, opens inventory, etc.) only receive `SetDefaults()`. The instance fields `_hasInventoryTexture` and `InventoryTexture` were set on the template but were always `false`/`null` on actual drawn items.

**Fix**: Replaced instance fields with a `static Dictionary<int, Asset<Texture2D>> _inventoryTextures` keyed by item `Type`. `SetStaticDefaults()` stores into the dictionary. `PreDrawInInventory`/`PreDrawInWorld` use `_inventoryTextures.TryGetValue(Type, ...)` — works on any instance since the lookup is by type ID, not instance state.

**Files**: `Common/Items/BaseCyclingWand.cs`

### 33. ✅ Wall Mode for Building Wand
**Problem**: Building wand supported Solid, Platform, Rope, Rail, GrassSeed, and Planter objects but not Walls — a fundamental tile type requiring separate placement logic.

**Fix** (multi-file):
1. **PlaceType enum**: Added `Wall` value.
2. **ItemTypeHelper**: Added `PlaceType.Wall => item => item.createWall > 0` condition.
3. **WandOfBuildingBase**: Added `ExecuteWallBuilding()` method (~120 lines) with complete wall placement path using `createWall`, `WorldGen.PlaceWall`, `WorldGen.KillWall`, `Framing.WallFrame`. Supports replace mode, all exhaustion modes (Cancel/Interrupt/NextBlock), infinite resource, suppress drops, protection system, and undo manager.
4. **BuildingSettingsPanel**: Added `_wallBtn` with object type buttons reorganized from 1 row of 6 to 2 rows (4+3). Panel height increased 538→580. Wall icon temporarily reuses `ObjSolid.png` until dedicated `ObjWall.png` asset is created.
5. **Localization**: Added "Wall: Wall" in Building section.

**Files**: `Common/Enums/PlaceType.cs`, `Common/Utilities/ItemTypeHelper.cs`, `Content/Items/WandOfBuildingBase.cs`, `Common/UI/BuildingSettingsPanel.cs`, localization

---

## Deferred Issues (Open)

### 6. 🔜 Improved Diamond Shape Rasterization
**Description**: Current diamond shapes are too mathematically perfect — edge offset accumulation looks ugly.

**Proposed**: UI toggle for "Organic Diamonds" with seeded random offsets (±1–2 tiles) based on position. Ensures undo/redo consistency while breaking mathematical perfection. Alternative: separate `RoughDiamond` shape type.

**Why Deferred**: Needs user testing to determine the right level of "organic" feel.

### 7. 🔜 Sparse Straight Lines
**Description**: Straight line shapes should support sparse/organic distribution.

**Why Deferred**: Same determinism concerns as #6. Better as a new shape variant.

### 9. ❌ Diagonal Wiring Lines
Diagonal cardinal lines produce non-connected wires (4-way only). **Rejected** — uncommon use case with workarounds.

### 11. ✅ Elbow GetDisplayDimensions Override
**Problem**: Elbow shape displayed raw bounding box dimensions instead of meaningful segment lengths.

**Fix**: Added `GetDisplayDimensions` override to `ElbowShape`. Returns segment lengths (vLen, hLen) for VerticalFirst or (hLen, vLen) for HorizontalFirst, giving users a meaningful representation of the L-shape's two segments.

**Files**: `Common/Geometry/Shapes/ElbowShape.cs`

### 13. ✅ Area Calculation in Shape Display (Debounced)
**Implementation**: Approach B (debounced) — calculate tile count after 10 frames of stable dimensions.

- Added `_areaStableFrames`, `_lastAreaDimensions`, `_cachedAreaCount` cache fields to `SelectionOverlay`
- `DrawDimensionLabel()` now accepts `HashSet<Point> tiles` parameter
- When dimensions are stable for 10 frames (~0.17s), computes `tiles.Count`
- Displayed as "W × H  (count)" appended to dimension label
- Zero cost during active dragging — only computed once when user pauses

**Files**: `Common/Drawing/SelectionOverlay.cs`

### 14. ✅ Dimension Display Positioning
**Fix**: Improved dimension label rendering with offset (24, −32) from cursor position. Added full `Math.Clamp` for all 4 screen edges with 4px margin to prevent clipping offscreen.

**Files**: `Common/Drawing/SelectionOverlay.cs`

### 19. 🔜 Wall Merging (Cheat Sheet)
**Description**: Cheat Sheet's paint tool missing `Framing.WallFrame()` calls. Not our bug — our `BulkTileOperations.BatchFrameUpdate` already handles both tile and wall frames.

### 21. 🔜 Cardinal Line Thickness
**Description**: `StraightLineShape` supports thickness but may not be exposed uniformly across all panels.

**Status**: All panels already have thickness controls. Needs in-game verification that thickness applies correctly to cardinal lines specifically (not just outline shapes).

### 22. 🔜 Omnidirectional Lines with Thickness
**Description**: New "OmniLine" shape type allowing lines at any angle (not snapped to 8 directions).

**Thickness Approaches**:
- **A: Perpendicular Expansion** — Expand perpendicular to line angle. Simple, matches cardinal behavior. May gap at extreme angles.
- **B: Circular Brush** — All pixels within `thickness/2` distance of line segment. Consistent but expensive (distance calc per pixel).
- **C: Bresenham + Brush** — Center line via Bresenham, stamp circular brush per point. Quality/performance compromise.

**Recommendation**: Start with A. Upgrade to B if artifacts appear at steep angles.

**Why Deferred**: Needs new shape provider, UI icon, enum value, registry entry. High complexity.

### 23. ✅ Enhanced Sound Effects
**Fix**: Added `SoundEngine.PlaySound` after successful operations:
- Building: `SoundID.Item9` (sparkle) — after tile AND wall placement
- Dismantling: `SoundID.Item14` (thud) — after successful instant dismantling
- Replacement: `SoundID.Item29` (transmutation) — after successful instant replacement

All sounds play at `player.Center` and only trigger on successful operations (placed/replaced > 0).

**Files**: `Content/Items/WandOfBuildingBase.cs`, `Content/Items/WandOfDismantling.cs`, `Content/Items/WandOfReplacementBase.cs`

### 34. 🔜 Cross-Mod Container Support
**Description**: `TileID.Sets.IsAContainer` is false for most modded chests (e.g., CalamityMod's RustedChest), causing the dismantling wand to fail on them. ImproveGame has a similar limitation — it also misses dressers, barrels, and modded fishing machines.

**Proposed Approach**: JSON-driven patching system.
1. Ship a `ContainerPatches.json` mapping mod names → tile type names → `IsAContainer = true`.
2. On `PostSetupContent`, iterate registered mods, resolve tile types, patch the flag.
3. Covers Calamity, Thorium, and other popular mods without hardcoding.
4. Users can add custom entries via config or external JSON.

**Additional Considerations**:
- **Dressers & Barrels**: These are containers but not in `IsAContainer`. Need to identify by `TileObjectData` or tile set membership.
- **Personal Storage (Void Vault, Safe, Defender's Forge)**: These are gates to personal inventories (Ender Chest-style), not world containers. They should be destroyable but should **not** drop items — the personal storage persists. Need special handling: destroy tile without calling chest-emptying logic.

**Why Deferred**: Requires cross-mod testing, JSON schema design, and careful handling of personal-storage edge cases. High impact but high effort.

### 35. 🔜 Torch Tiling System
**Description**: Place torches in a diamond/rhombus pattern filling a selected area, since torch light intensity falls off linearly with Manhattan distance.

**Proposed Design**:
- Two configurable values: X spacing and Y spacing between torches.
- Rhombus pattern (offset every other row/column) for optimal light coverage.
- Calculating optimal distance from light intensity is impractical — torch intensities vary and color data isn't easily accessible. Fixed configurable spacing gives maximum freedom.

**Separate Wand Consideration**: Torch tiling is fundamentally different from shape-based building (it's a fill pattern, not a shape). Mixing it into the building wand UI would be confusing. Recommend a dedicated "Torch Wand" or "Lighting Wand" with its own settings panel showing X/Y spacing sliders.

**Why Deferred**: Needs new wand type, new UI panel, rhombus pattern algorithm, and careful UX design to avoid overloading the building wand.

### 36. 🔜 Advanced Multi-Pair Replacement Wand
**Description**: Instead of single source→target replacement, map each tile in inventory row N to the tile directly below it in row N+1, enabling multiple simultaneous replacements (even walls).

**Why Separate Wand**: This is a fundamentally different interaction model — the current replacement wand uses held-item-to-cursor logic. Multi-pair replacement needs a dedicated UI showing the mapping grid, and cramming it into the existing panel would confuse users.

**Why Deferred**: New wand type, new UI, new inventory-scanning logic. Significant design and implementation effort.

### 37. ✅ Replacement Wall Mode
**Fix** (multi-file):
1. **ObjectType enum**: Added `Wall = 7` with doc comment.
2. **ItemTypeHelper**: Added Wall case to `GetConditions(ObjectType)` (`item => item.createWall > 0`) and `WorldTileMatchesObjectType` (returns false — wall matching uses WallType, not TileType).
3. **WandOfReplacementBase**: Added `ExecuteWallReplacement()` method (~150 lines) with complete wall replacement path using `createWall`, `WorldGen.KillWall`, `WorldGen.PlaceWall`, `Framing.WallFrame`. Supports Air target (erase walls), pick-power bypass config, inventory consumption, undo, batch sync. Added branching at top of `ExecuteReplacement()` to route Wall mode to separate path.
4. **ReplacementSettingsPanel**: Added `_srcWallBtn` and `_tgtWallBtn` icon buttons. Source row expanded from 6 to 7 buttons; target row expanded from 7 to 8 buttons (Wall at position 6, Air shifted to 7). Wired up `OnToggled` events and state sync in `UpdateSourceButtons`/`UpdateTargetButtons`.
5. **Localization**: Added `Wall: Wall` under the Replacement section.

**Files**: `Common/Enums/ObjectType.cs`, `Common/Utilities/ItemTypeHelper.cs`, `Content/Items/WandOfReplacementBase.cs`, `Common/UI/ReplacementSettingsPanel.cs`, localization

---

## Deferred Design Notes

### Tiered Wand System (Long-term Vision)
The mod is at a "lovely" state but has infinite range and no progression. Long-term goal: lesser wands with configurable limits, balanced recipes, and batch sizes.

**Considerations**:
- Internal names must be clean from the start (lessons from StraightLine→CardinalLine and WandOfDestruction→WandOfDismantling renames)
- Old items need migration patching before removal (transform old→new before removing old type IDs)
- Tiered limits: range, selection area, batch size, cooldowns
- Recipe progression: wood→iron→adamantite→luminite tiers

**Status**: Brainstorming phase. Needs dedicated design document.

---

## Dev Notes (Chronological)

- Renamed all instances of WandOfDestruction to WandOfDismantling (build + in-game test passed). Done via fine-controlled replacement script.
- Grass/mud variant detection implemented — replacing dirt now catches all grass-covered variants. Cross-mod concern: vanilla mapping only, modded variants need explicit registration.
- Approach B (debounced) chosen for area calculation (#13).
- Replace-under-chests fixed (#25) via `WorldGen.ReplaceTile` with `KillTile`+`PlaceTile` fallback.
- Dual sprite system ready (#27) — just needs `_Inventory.png` art assets per wand. Zero-cost if assets don't exist.
- Equal Dimensions toggle works perfectly for circles, squares, and equilateral diamonds.

## Dev Notes #2 — Resolved

**Issues reported**: 9 items. **Resolved**: 6 implemented (#28-33). **Deferred**: 4 items (#34-37).

| Raw Note | Resolution |
|---|---|
| 999×999 lag when switching wands with EqualDimensions | ✅ #28 — Auto-clear selection on wand switch |
| Overlay computed 500K tiles per frame | ✅ #29 — Tile set caching |
| Wiring UI doesn't respond after first click | ✅ #30 — Use `UserInterface.Draw()` instead of direct `panel.Draw()` |
| No way to undo misclicks in multi-click modes | ✅ #31 — Backspace keybind steps back one state |
| Dual sprite shows wrong texture on real items | ✅ #32 — Static dictionary keyed by item Type |
| Building wand missing wall mode | ✅ #33 — Full wall placement path added |
| Cross-mod container support (ImproveGame reference) | 🔜 #34 — JSON-driven patching, personal storage edge cases |
| Torch tiling in diamond pattern | 🔜 #35 — Separate wand recommended |
| Multi-pair replacement from inventory rows | 🔜 #36 — Separate wand recommended |
| Replacement wall mode | 🔜 #37 — Medium effort, auto-detect from held item |

**Additional cleanup**: Renamed `ThicknessControls` class → `WandControls` (file renamed to `WandControls.cs`). Removed ~14 debug `Main.NewText` calls from `WandUISystem`. `ObjWall.png` icon still needed (using `ObjSolid.png` placeholder).

## DevNotes #3 — Resolved

**Issues reported**: ~12 items. **Resolved**: 5 implemented (#38-42). **Deferred**: 7 items (#43-49).

| Raw Note | Resolution |
|---|---|
| Edge → Elbow rename (more descriptive shape name) | ✅ #38 — Enum, class, registry, all 5 panels, settings, localization, assets renamed. Stale `EdgeShape.cs` deleted. |
| Wiring overlay axis-flip bug (preview showed different path than execution) | ✅ #39 — Root cause: `ShapeInfo.ToShapeContext()` hardcoded `verticalFirst: false`. Overlay used this method but execution passed `selection.VerticalFirst`. Fixed by adding `verticalFirst` parameter to `ToShapeContext()` and threading it through `SelectionOverlay` caching + rendering. |
| Protection → Safekeeping rename (distance from combat names) | ✅ #40 — All classes, files, localization keys already renamed in prior work. Confirmed complete. |
| Safekeeping overlay (can't see what's protected) | ✅ #41 — New `SafekeepingOverlay` ModSystem draws colored squares over protected positions when holding wand. Three visual states: cyan (tile-only), magenta (wall-only), gold (both). Low-opacity fill + outline, screen-culled for performance. |
| Lore tooltips (Shift-gated flavor text) | ✅ #42 — Common lore + wand-specific lore in `BaseCyclingWand.ModifyTooltips`. Shows "[Hold Shift for lore]" hint by default. Config toggle `ShowLoreTooltips` to disable entirely. Building + Dismantling have user lore; Replacement, Safekeeping, Wiring have placeholder lore. |
| Ultimate Wand of Building (actuation + paint sprayer) | 🔜 #43 — New wand type; needs UI toggles for actuation on/off, overwrite actuation, paint sprayer. Recipe: Building + Actuation Rod + Paint Sprayer. Separate infinite resource configs for tiles, walls, paints. |
| Lihzahrd brick actuation post-Golem restriction research | 🔜 #44 — Research needed: vanilla vs FTW/getfixedboi seed scope. Calamity abyss considerations unknown. |
| Wand of Slopes (7 slope types + cycle) | 🔜 #45 — New wand family with 8 sprites (4 modes × 2 sprites). Cycle mode lets Terraria handle slope progression; explicit modes set specific slopes. UI assets partially exist from Building wand. |
| Wand of Painting/Coating (shape-based paint applicator) | 🔜 #46 — "Wand of Coating" proposed name (covers paint + moss). Like ImproveGame's but with full shape system. Contact with ImproveGame unsuccessful; independent implementation needed. |
| Essence of Creation progression system | 🔜 #47 — Addon/supermod design. Altar-based ore progression. Needs design: drop mechanics, treasure hunt, divine theme integration. |
| Animated wand sprites | 🔜 #48 — Instant wands need looping animation; non-instant need use-time-length animation. Requires tModLoader animation research. |
| README update | 🔜 #49 — Outdated; needs full rewrite reflecting current feature set. |

### #38 — Edge → Elbow Rename (Cosmetic)

**What**: The `ShapeType.Edge` enum value and all references renamed to `ShapeType.Elbow` for clarity — "elbow" properly describes an L-shaped right-angle joint.

**Files changed** (complete rename was done across sessions):
- `ShapeType.cs` — `Edge = 4` → `Elbow = 4`
- `ElbowShape.cs` — new file with `ElbowShape` class (replaces `EdgeShape`)
- `ShapeRegistry.cs` — `Register(new ElbowShape())`
- All 5 settings panels — icon refs `ShapeElbow`, event wiring `ShapeType.Elbow`
- `WandSettingsState.cs` — shape cycle arrays updated
- `WandPlayer.cs` — cap check updated
- `WandOfWiringSettings.cs` — default shape updated
- `ShapeTestCommand.cs` — case updated
- Localization — `ShapeElbow: Elbow`
- `Assets/Icons/ShapeElbow.png` — asset file renamed
- **Stale `EdgeShape.cs` deleted** (duplicate of `ElbowShape.cs`, would cause compile conflict)

### #39 — Wiring Overlay Axis-Flip Bug (Critical Fix)

**Symptom**: When placing wiring over long distances, the preview overlay showed a different elbow path than the actual wiring execution. E.g., preview showed "horizontal first" but execution followed "vertical first."

**Root cause**: `ShapeInfo.ToShapeContext()` hardcoded `verticalFirst: false`:
```csharp
// Before (broken):
public ShapeContext ToShapeContext(Point start, Point end)
{
    return new ShapeContext(start, end, FillMode, effectiveThickness,
        HorizontalBias.None, VerticalBias.None, false, EqualDimensions);
    //                                          ^^^^^ always false!
}
```

The `SelectionOverlay` called `ToShapeContext()` (always `false`), while wand execution code passed `selection.VerticalFirst` directly to `ShapeContext`. For the Elbow shape, `VerticalFirst` determines which segment is drawn first — the preview and execution computed different paths.

**Fix** (3 files):
1. **`ShapeInfo.cs`** — Added `bool verticalFirst = false` parameter to `ToShapeContext()`. Default preserves backward compat.
2. **`SelectionOverlay.cs`** — Thread `selection.VerticalFirst` through:
   - `_cacheVerticalFirst` field added to cache validation
   - `GetOrComputeTiles()` accepts and caches `verticalFirst`
   - `DrawSelection()` passes `selection.VerticalFirst`
   - `DrawDimensionLabel` and cancelled selection pass `verticalFirst`

### #40 — Protection → Safekeeping Rename (Cosmetic)

Already completed in prior sessions. All class names (`WandOfSafekeepingBase`, `SafekeepingSystem`, `SafekeepingSettingsPanel`, etc.), file names, and localization keys use "Safekeeping." Confirmed no remaining "Protection" references in C# code (only in SafekeepingSystem's internal variable names like `_protectedTiles` and user-facing action text like "Protect/Unprotect" which remain semantically correct).

### #41 — Safekeeping Overlay (New Feature)

**What**: When holding any Wand of Safekeeping, semi-transparent colored squares are drawn over protected positions in the world. Three visual states distinguished by color:
- **Cyan** — tile-only protection
- **Magenta** — wall-only protection
- **Gold** — both tile and wall protected

**Implementation**: New `SafekeepingOverlay` ModSystem (`Common/Drawing/SafekeepingOverlay.cs`):
- `[Autoload(Side = ModSide.Client)]` — rendering is client-only
- Screen-culled: only draws tiles within the visible screen bounds + margin
- Uses `TextureAssets.MagicPixel` (no custom icon assets needed)
- Low opacity (fill: 22%, outline: 45%) so it doesn't obscure the world
- Iterates `SafekeepingSystem.ProtectedTiles` then `ProtectedWalls`, skipping wall positions already drawn as "both"

**Design decision**: The user specifically said "I don't want to mess with having yet another toggle at the top left of the inventory unnecessarily" — so the overlay activates automatically when holding the wand, with no separate toggle. This matches how vanilla wiring mode automatically shows wires when `Item.mech = true`.

### #42 — Lore Tooltips (New Feature)

**What**: Shift-gated lore text in wand tooltips. When hovering a wand:
- Default: shows `[c/888888:Hold [Shift] for lore]` hint
- Shift held: shows common lore (shared by all wands) + wand-specific lore
- Config toggle `ShowLoreTooltips` (default: true) to hide lore entirely

**Implementation**:
- `BaseCyclingWand` — added `CommonLore` const, virtual `WandLore` property, Shift detection in `ModifyTooltips`
- Each wand base overrides `WandLore` with its flavor text
- `WandConfig` — added `ShowLoreTooltips` bool with localized label/tooltip
- Localization — added `Tooltips.Header` section with `ShowLoreTooltips` config strings

**Lore text**:
| Wand | Common | Specific |
|---|---|---|
| All | "The Gods of Space let you enact your thoughts into reality." | — |
| Building | + | "The Deity of Creation lets you manifest order and existence at will." |
| Dismantling | + | "The Deity of Erasure lets you restore nothingness as you wish." |
| Replacement | + | "The Deity of Transmutation lets you reshape what already exists." *(placeholder)* |
| Safekeeping | + | "The Deity of Timelessness lets you separate space from the powers of creation." |
| Wiring | + | "An advanced mechanism wrench, forged from the ingenuity of the Grand Design." *(non-divine, as requested)* |

**Shift key compatibility**: Uses `Main.keyState.IsKeyDown(Keys.LeftShift/RightShift)` directly — no keybind registration needed. Since wands are crafted items, there's no conflict with "More Obtaining Tooltips" mod (which uses Shift for loot source info on dropped items).

## DevNotes 3.1 @Brainstorming — Resolved

 See #42 (lore tooltips) and #43-49 (deferred items) above. The brainstorming section covered:
- Lore entries → ✅ Implemented as #42
- Essence of Creation → 🔜 #47 (addon/supermod design needed)
- Animated wands → 🔜 #48 (tModLoader animation research needed)
- Contributions welcome → noted in dev docs

## DevNotes #4 — Alpha Release Prep — Resolved

**Issues reported**: ~15 items. **Resolved**: 14 implemented (#11, #13, #14, #23, #37, #50-56). **Deferred**: 1 (cardinal line thickness #21 needs in-game verification only).

| Raw Note | Resolution |
|---|---|
| build.txt metadata (author, homepage) | ✅ #50 — `author = Grayjou`, `homepage = https://github.com/Grayjou/WorldShapingWandsMod` |
| description.txt rewrite | ✅ #51 — Full rewrite with 5 families, 4 modes, features, known limitations |
| Replacement lore text update | ✅ #52 — "The Deity of Transmutation lets you reweave the threads of what already is." |
| Wiring divine header suppression | ✅ #53 — `ShowDivineLore` virtual bool in `BaseCyclingWand`; Wiring overrides to `false` |
| Sound effects per wand | ✅ #23 — Building: Item9, Dismantling: Item14, Replacement: Item29 |
| Elbow dimension display | ✅ #11 — `GetDisplayDimensions()` override showing segment lengths |
| Dimension positioning offscreen | ✅ #14 — Offset (24,−32), Math.Clamp all 4 edges |
| Area tile count display | ✅ #13 — Debounced (10 frames), appended to dimension label |
| Crafting recipes: Building | ✅ #54 — 10 Wood, 10 GrayBrick, 10 RedBrick, 20 Rope, 1 ManaCrystal @Anvil |
| Crafting recipes: Dismantling | ✅ #54 — 10 Wood, 20 Rope, 5 AnyGem, 50 Dynamite, 1 ManaCrystal @Anvil |
| Crafting recipes: Replacement | ✅ #54 — 1 BuildingInstant + 1 DismantlingInstant + 1 ManaCrystal @Anvil |
| Recipe groups | ✅ #55 — `WandRecipeSystem.cs` with AnyGem, AnyGoldBar, AnySilverBar, AnyEvilStone |
| Safekeeping recipe update | ✅ #56 — Updated to use recipe groups (AnyGoldBar, AnySilverBar, AnyGem, AnyEvilStone) |
| Wall replacement mode | ✅ #37 — Full pipeline: ObjectType.Wall, ExecuteWallReplacement, UI buttons, localization |
| Cardinal line thickness verify | 🔜 #21 — Code already implemented; needs in-game verification only |

### #50 — build.txt Metadata
Updated `author = Grayjou` and added `homepage = https://github.com/Grayjou/WorldShapingWandsMod`.
**Files**: `build.txt`

### #51 — description.txt Rewrite
Full rewrite describing all 5 wand families, 4 selection modes, 8 shape types, key features, and known alpha limitations.
**Files**: `description.txt`

### #52 — Replacement Lore Update
Changed lore from placeholder to: "The Deity of Transmutation lets you reweave the threads of what already is."
**Files**: `Content/Items/WandOfReplacementBase.cs`

### #53 — Wiring Divine Header Suppression
Added `public virtual bool ShowDivineLore => true;` to `BaseCyclingWand` and `ModifyTooltips` conditionally shows the common divine lore based on this property. `WandOfWiringBase` overrides to `false` since it uses a non-divine, engineering-themed lore instead.
**Files**: `Common/Items/BaseCyclingWand.cs`, `Content/Items/WandOfWiringBase.cs`

### #54 — Crafting Recipes (Building, Dismantling, Replacement)
Added `AddRecipes()` overrides to all three wand bases. Each recipe uses the Instant variant as the result item:
- **Building**: 10 Wood + 10 GrayBrick + 10 RedBrick + 20 Rope + 1 ManaCrystal @WorkBench (Anvil)
- **Dismantling**: 10 Wood + 20 Rope + 5 AnyGem (group) + 50 Dynamite + 1 ManaCrystal @Anvil
- **Replacement**: 1 BuildingInstant + 1 DismantlingInstant + 1 ManaCrystal @Anvil

**Files**: `Content/Items/WandOfBuildingBase.cs`, `Content/Items/WandOfDismantling.cs`, `Content/Items/WandOfReplacementBase.cs`

### #55 — Recipe Groups (WandRecipeSystem)
Created `Common/Systems/WandRecipeSystem.cs` — a new `ModSystem` that registers 4 recipe groups in `AddRecipeGroups()`:
- **AnyGem**: Amethyst, Topaz, Sapphire, Emerald, Ruby, Diamond, Amber
- **AnyGoldBar**: GoldBar, PlatinumBar
- **AnySilverBar**: SilverBar, TungstenBar
- **AnyEvilStone**: EbonstoneBlock, CrimstoneBlock

**Files**: `Common/Systems/WandRecipeSystem.cs` (NEW)

### #56 — Safekeeping Recipe Update
Updated `WandOfSafekeepingBase.AddRecipes()` to use recipe groups instead of hardcoded items: AnyGoldBar(5), AnySilverBar(10), AnyGem(5), Obsidian(20), AnyEvilStone(10), ManaCrystal(1) @Anvil.
**Files**: `Content/Items/WandOfSafekeepingBase.cs`

## Devnotes #5 
After testing line thickness on cardinal lines, I gotta say that although it works for axis lines, it doesn't show at all in the overlay, and in diagonal lines it actually applies a checker pattern. So I went and put my wand in stamp mode, use different radius for a rectangle that moves diagonally and a circle that moves diagonally, and the circle one just feels better. So line thickness should definitely be treated as a circle that moves accross the path, just like regular brushes do. Now for this, I imagine there is some algorithm that uses some sort of pattern and only applies a circle to the edges, I'm pretty sure about that. My main concern is the thickness not showing in the overlay shape. 
I noticed that programs actually have the "origin" of the pen off center when the width is even. If we can make it so that the overlay displays the base circle properly when setting the start pos, we'd likely be able to get away with that. The problem is that it would feel a little flimsy because the display doesn't show before clicking. We can do two things to solve this issue, (That I can think of)
    1. (Easier) Outline the selection tile every time so that if the user does not like where the selection started, they can cancel it and move it accordingly (Requieres more intuition by the user)
    2. (Harder) Always display the starting area, from a single tile for every other mode, and a circle of diameter Thickness for lines. This one actually sounds great, even for regular wands, having a tile highlight actually provides QoL. Additionally, we can cap the thickness (Radius) and simply cache the circles as we go. I really like this option. It also opens opportunity for draw building in the future with our four modes, pretty neat Drag and Release for instant, double click mode for just creating a line art attack style, etc!


Now, for the recipes, this is not what I had on my mind, I wanted only one wand mode per variant having a recipe while all of them are shimmerable. Any Amethyst is not what I wanted, I'd rather have a localization key called Gemstone whan have Any Amethyst 
So for the Replacement Wand I'd rather have groups called Any Wand of Building  and Any Wand of Dismantling (Those names are already in the localization file)

As for the sounds, they are only necessary (And should be able to be turned off) when there is no terraria handling of the placement or removing. For example, if replacing or dropping doesn't preserve tiles, in that case, we shall use a sound, otherwise the block placing and block removing sounds are more than anough. So these should the use cases

Wand of Building:
If for some reason we skip placing blocks and we do it like CheatSheet, where it places the tiles without the API. I don't remember a case right now implemented, but if there is one, use SoundID.Dig

Wand of Dismantling: 
On Action Success (End Drag for instant, Second Click on Select, Confirm on Confirm Mode, Stamp use on Stamp) and if drops are supressed and if allowed in the config. Should use SoundID.Tink (Please add some identifier such as SoundOreHit, because terraria enum names drives me insane)

Wand of Replacing
On Action Success (End Drag for instant, Second Click on Select, Confirm on Confirm Mode, Stamp use on Stamp) and if drops are supressed and if allowed in the config. I think Mana Crystal is alright, but it's too loud, probably set the volume to 0.25f if possible, otherwise use SoundID.Item35, which is the Bell Soound, Please also identify it somehow

Wand Of Safekeeping shall use this sound when protecting  as its a very short and crispy freeze
Item 30 (Identifier if possible please)
Magical Ice Block placed

Wand of Wiring should use this one, naturally
Item64	
Item 64 (Identifier if possible please)
Blowgun, The Grand Design 

Far Future Idea: Wand of Erosion. Several Erosion Algorithms for deepening holes, crevices, shaping terrain, etc

Tested in Multiplayer
Wand of Building working
Wand of Dismantling not working at all
Wand of Replacement not working at all 
Wand of Wiring Working, UI not closing with close button
Wand of Safekeeping working

ImproveGame Wands do work in multiplayer for comparison
Also ImproveGame updated and now uses an UI library, interesting whatsoever

## Devnotes #5 — Resolved Issues

### #57 — Cardinal Line Thickness: Circular Brush Algorithm
**Problem**: Diagonal cardinal lines with thickness > 1 produced a checker pattern. The perpendicular expansion approach (`GetPerpendicularDirection`) rotated (1,1) → (1,-1), creating diagonal parallel lines with gaps.
**Fix**: Replaced perpendicular expansion in `CardinalLineShape.cs` with a "circular brush" algorithm. For each point along the center line, all tiles within Euclidean distance `radius = thickness/2` are included. Circle offsets are cached per-radius in a static dictionary for performance. Also updated `ContainsPoint()` and `GetDisplayDimensions()` to use the new approach.
**Files**: `Common/Geometry/Shapes/CardinalLineShape.cs`

### #58 — Cursor Highlight / Starting Area Preview
**Problem**: No visual feedback at the cursor when holding a wand before starting a selection.
**Fix**: Added `DrawCursorHighlight()` to `SelectionOverlay`. When holding any wand and no selection is active, shows a pulsing tile highlight at the cursor. For cardinal lines with thickness > 1, shows the full circle brush preview. Uses the same outline rendering as the main selection overlay.
**Files**: `Common/Drawing/SelectionOverlay.cs`

### #59 — Recipe: Only Instant Mode Has Crafting Recipe
**Problem**: All 4 modes of each wand family had the same crafting recipe. User wanted only one mode per family to be craftable.
**Fix**: Moved `AddRecipes()` from each base class to the Instant variant only. Base class `AddRecipes()` is now empty with a comment. Each `*Instant.cs` file overrides with the actual recipe.
**Files**: `Content/Items/WandOfBuildingInstant.cs`, `Content/Items/WandOfDismantling.cs` (WandOfDismantlingInstant), `Content/Items/WandOfReplacementInstant.cs`, `Content/Items/WandOfWiringInstant.cs`, `Content/Items/WandOfSafekeepingInstant.cs`, all Base classes

### #60 — Recipe: Gemstone Label Instead of "Any Amethyst"
**Problem**: AnyGem recipe group displayed "Any Amethyst" — user wanted "Gemstone".
**Fix**: Changed the AnyGem label to use a localization key `RecipeGroups.Gemstone` → "Gemstone". Updated the recipe group registration key from `nameof(ItemID.Amethyst)` to `"WorldShapingWandsMod:AnyGem"` for clarity.
**Files**: `Common/Systems/WandRecipeSystem.cs`, `Localization/en-US_Mods.WorldShapingWandsMod.hjson`

### #61 — Recipe: Replacement Wand Uses Wand Groups
**Problem**: Replacement recipe used specific `WandOfBuildingInstant` + `WandOfDismantlingInstant`. User wanted recipe groups "Any Wand of Building" and "Any Wand of Dismantling".
**Fix**: Added two new `RecipeGroup`s in `WandRecipeSystem` containing all 4 modes of each family. Updated the Replacement Instant recipe to use these groups.
**Files**: `Common/Systems/WandRecipeSystem.cs`, `Content/Items/WandOfReplacementInstant.cs`, `Localization/en-US_Mods.WorldShapingWandsMod.hjson`

### #62 — Shimmer Decraft (Replaces Shimmer Cycling)
**Problem**: Shimmer cycling was redundant because players already cycle modes via right-click in inventory. Vanilla convention (Shellphone, etc.) is for shimmer to decraft items into components, not cycle variants.
**Fix**: Removed custom `ShimmerTransformToItem` override from `BaseCyclingWand` so crafted wands use normal recipe decrafting. This returns full ingredients from the selected recipe (e.g., `10 Wood + 5 Amethyst` when the gem ingredient comes from the AnyGem recipe group). Updated tooltip hint text to reflect ingredient-based decrafting.
**Files**: `Common/Items/BaseCyclingWand.cs`

### #63 — Sound Effects: Correct IDs, Conditional Playback, Config Toggle
**Problem**: Sound effects were wrong IDs, played unconditionally, and had no config toggle.
**Fix**:
- **Building**: Removed `SoundID.Item9` — Terraria API already plays placement sounds via `PlaceTile`.
- **Dismantling**: Changed to `SoundID.Tink` (SoundOreHit). Only plays when `SuppressDrops && EnableWandSounds` (otherwise natural break sounds play).
- **Replacement**: Changed to `SoundID.Item29` (ManaCrystalPickup) at `Volume = 0.25f`. Only plays when `SuppressDrops && EnableWandSounds`.
- **Safekeeping**: Added `SoundID.Item30` (IceBlockPlace) when protecting. Plays when `EnableWandSounds`.
- **Wiring**: Added `SoundID.Item64` (GrandDesignSound / Blowgun). Plays when `EnableWandSounds`.
- Added `EnableWandSounds` config toggle (default: true) to `WandConfig`.
**Files**: `Common/Configs/WandConfig.cs`, `Content/Items/WandOfBuildingBase.cs`, `Content/Items/WandOfDismantling.cs`, `Content/Items/WandOfReplacementBase.cs`, `Content/Items/WandOfWiringBase.cs`, `Content/Items/WandOfSafekeepingBase.cs`, `Localization/en-US_Mods.WorldShapingWandsMod.hjson`

### #64 — Multiplayer: Dismantling and Replacement Not Working
**Problem**: Dismantling and Replacement operations had no effect in multiplayer. Root cause: `WorldGen.gen = true` was set unconditionally, which suppresses per-tile network messages. `SendTileSquare` alone wasn't sufficient for the server to process tile modifications.
**Fix**: In multiplayer (`Main.netMode == NetmodeID.MultiplayerClient`), skip setting `WorldGen.gen = true` so that `KillTile`, `PlaceTile`, `KillWall`, and `PlaceWall` send their own `MessageID.TileManipulation` network messages per-tile. In single-player, `WorldGen.gen = true` is still used for silent/clean operation. Applied to both instant and wall replacement paths.
**Files**: `Content/Items/WandOfDismantling.cs`, `Content/Items/WandOfReplacementBase.cs`

### Wand of Erosion — NOTED
Future idea acknowledged: Wand of Erosion with erosion algorithms for deepening holes, shaping terrain, etc. Deferred to future roadmap.

---

## DevNotes #6 — Shimmer Decraft, UI Close Fix, Thorium Gems

### #65 — Shimmer Decraft Instead of Shimmer Cycling
**Problem**: Shimmer cycling between wand modes was redundant — players already cycle via right-click in inventory. Vanilla convention (Shellphone, etc.) uses shimmer to decraft into crafting components.
**Fix**: Removed `ShimmerTransformToItem` override from `BaseCyclingWand.SetStaticDefaults()` so decrafting is recipe-driven instead of forced to a single item. Crafted instant-mode wands now shimmer back into their actual ingredients, with recipe group ingredients resolving to a concrete item (AnyGem defaults to Amethyst in decraft output).
**Files**: `Common/Items/BaseCyclingWand.cs`

### #66 — Wand UI Close Button and Escape Key Not Working
**Problem**: Close button and Escape key on all 5 wand settings panels only set `IsVisible = false` without clearing the `UserInterface` state. The `_userInterface` kept the panel as its active state, causing ghost interactions and preventing proper closure. This affected **all** panels (Building, Dismantling, Replacement, Wiring, Safekeeping), not just Wiring, and was **not** multiplayer-specific.
**Root Cause**: `closeBtn.OnLeftClick += (_, _) => IsVisible = false;` — this hides the panel visually but `WandUISystem._userInterface` still has the panel set as its state, so `UpdateUI` continues processing it and mouse interactions remain captured.
**Fix**: Changed all close button handlers and Escape key handlers in all 5 panels to call `ModContent.GetInstance<WandUISystem>().CloseAllUI()` which properly sets `IsVisible = false` on all panels AND calls `_userInterface.SetState(null)` to fully release the UI state.
**Files**: `Common/UI/BuildingSettingsPanel.cs`, `Common/UI/DismantlingSettingsPanel.cs`, `Common/UI/ReplacementSettingsPanel.cs`, `Common/UI/WiringSettingsPanel.cs`, `Common/UI/SafekeepingSettingsPanel.cs`

### #67 — Thorium Gem Support (Opal, Aquamarine) — ROADMAP
**Status**: Not implemented. Noted for future cross-mod compatibility.
**Plan**: When Thorium Mod is loaded, add Opal and Aquamarine to the AnyGem recipe group via `Mod.TryFind` or `ModLoader.TryGetMod("ThoriumMod")` in `AddRecipeGroups()`. This allows Thorium gems to be used in wand recipes.
**Files**: Future — `Common/Systems/WandRecipeSystem.cs`