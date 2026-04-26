
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

### 11. ✅ Elbow GetDisplayDimensions Override
**Problem**: Elbow shape displayed raw bounding box dimensions instead of meaningful segment lengths.

**Fix**: Added `GetDisplayDimensions` override to `ElbowShape`. Returns segment lengths (vLen, hLen) for VerticalFirst or (hLen, vLen) for HorizontalFirst, giving users a meaningful representation of the L-shape's two segments.

**Files**: `Common/Geometry/Shapes/ElbowShape.cs`


### 12. ✅ Regular Shape Types — Merged with #20
Instead of adding Circle/Square/SquareDiamond shape types, a single "Equal Dimensions" toggle replaces all three. See #20.

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

### 21. 🔜 Cardinal Line Thickness
**Description**: `StraightLineShape` supports thickness but may not be exposed uniformly across all panels.

### 23. ✅ Enhanced Sound Effects
**Fix**: Added `SoundEngine.PlaySound` after successful operations:
- Building: `SoundID.Item9` (sparkle) — after tile AND wall placement
- Dismantling: `SoundID.Item14` (thud) — after successful instant dismantling
- Replacement: `SoundID.Item29` (transmutation) — after successful instant replacement

All sounds play at `player.Center` and only trigger on successful operations (placed/replaced > 0).

**Files**: `Content/Items/WandOfBuildingBase.cs`, `Content/Items/WandOfDismantling.cs`, `Content/Items/WandOfReplacementBase.cs`
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

### 37. ✅ Replacement Wall Mode
**Fix** (multi-file):
1. **ObjectType enum**: Added `Wall = 7` with doc comment.
2. **ItemTypeHelper**: Added Wall case to `GetConditions(ObjectType)` (`item => item.createWall > 0`) and `WorldTileMatchesObjectType` (returns false — wall matching uses WallType, not TileType).
3. **WandOfReplacementBase**: Added `ExecuteWallReplacement()` method (~150 lines) with complete wall replacement path using `createWall`, `WorldGen.KillWall`, `WorldGen.PlaceWall`, `Framing.WallFrame`. Supports Air target (erase walls), pick-power bypass config, inventory consumption, undo, batch sync. Added branching at top of `ExecuteReplacement()` to route Wall mode to separate path.
4. **ReplacementSettingsPanel**: Added `_srcWallBtn` and `_tgtWallBtn` icon buttons. Source row expanded from 6 to 7 buttons; target row expanded from 7 to 8 buttons (Wall at position 6, Air shifted to 7). Wired up `OnToggled` events and state sync in `UpdateSourceButtons`/`UpdateTargetButtons`.
5. **Localization**: Added `Wall: Wall` under the Replacement section.

**Files**: `Common/Enums/ObjectType.cs`, `Common/Utilities/ItemTypeHelper.cs`, `Content/Items/WandOfReplacementBase.cs`, `Common/UI/ReplacementSettingsPanel.cs`, localization

## Dev Notes: Chronological

#2
| Raw Note | Resolution |
|---|---|
| 999×999 lag when switching wands with EqualDimensions | ✅ #28 — Auto-clear selection on wand switch |
| Overlay computed 500K tiles per frame | ✅ #29 — Tile set caching |
| Wiring UI doesn't respond after first click | ✅ #30 — Use `UserInterface.Draw()` instead of direct `panel.Draw()` |
| No way to undo misclicks in multi-click modes | ✅ #31 — Backspace keybind steps back one state |
| Dual sprite shows wrong texture on real items | ✅ #32 — Static dictionary keyed by item Type |
| Building wand missing wall mode | ✅ #33 — Full wall placement path added |
| Cross-mod container support (ImproveGame reference) | 🔜 #34 — JSON-driven patching, personal storage edge cases |


| Raw Note | Resolution |
|---|---|
| Edge → Elbow rename (more descriptive shape name) | ✅ #38 — Enum, class, registry, all 5 panels, settings, localization, assets renamed. Stale `EdgeShape.cs` deleted. |
| Wiring overlay axis-flip bug (preview showed different path than execution) | ✅ #39 — Root cause: `ShapeInfo.ToShapeContext()` hardcoded `verticalFirst: false`. Overlay used this method but execution passed `selection.VerticalFirst`. Fixed by adding `verticalFirst` parameter to `ToShapeContext()` and threading it through `SelectionOverlay` caching + rendering. |
| Protection → Safekeeping rename (distance from combat names) | ✅ #40 — All classes, files, localization keys already renamed in prior work. Confirmed complete. |
| Safekeeping overlay (can't see what's protected) | ✅ #41 — New `SafekeepingOverlay` ModSystem draws colored squares over protected positions when holding wand. Three visual states: cyan (tile-only), magenta (wall-only), gold (both). Low-opacity fill + outline, screen-culled for performance. |
| Lore tooltips (Shift-gated flavor text) | ✅ #42 — Common lore + wand-specific lore in `BaseCyclingWand.ModifyTooltips`. Shows "[Hold Shift for lore]" hint by default. Config toggle `ShowLoreTooltips` to disable entirely. Building + Dismantling have user lore; Replacement, Safekeeping, Wiring have placeholder lore. |


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
- `Assets/Icons/Shapes/ShapeElbow.png` — asset file renamed
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

---


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

### #68 — Remove Shimmer Tooltip (Cosmetic)

**Problem**: The shimmer tooltip ("[c/BBBBBB:Shimmer decrafts crafted wands into ingredients]") cluttered every wand's tooltip for no practical value — players discover shimmer naturally, and the tooltip was added in #66 before realizing it was noise.

**Fix**: Deleted the `ShimmerHint` tooltip line from `BaseCyclingWand.ModifyTooltips()`.

**Files**: `Common/Items/BaseCyclingWand.cs`

---

### #69 — MOT API Tooltip Scrubbing Verified (No Change Needed)

**Problem**: Need to confirm that `WandRecipeConditions` tooltip scrubbing (added in DevNotes #5) also handles MoreObtainingTooltips' "Can be shimmered into XYZ" lines.

**Finding**: The existing `RemoveAll` in `ModifyTooltips` already prefix-matches `"Craft"`, `"Recipe"`, and `"ObtainCraft"` — which covers MOT's tooltip keys. No additional scrubbing needed.

**Files**: None (verified existing code).

---

### #70 — Localization: Move Hardcoded Strings to hjson (Major Refactor)

**Problem**: 60+ `Main.NewText(...)` calls used hardcoded English strings, making the mod untranslatable for chat messages, selection prompts, error messages, and tooltip text.

**Fix**:
- Created `Common/Utilities/Msg.cs` — static helper with `Get(key, args...)` for accessing `Messages.*` localization keys.
- Added `Messages` section to `en-US_Mods.WorldShapingWandsMod.hjson` with 40+ keys covering all chat categories (common, building, dismantling, replacement, wiring, safekeeping, undo, selection prompts, stamp prompts).
- Updated 18 `.cs` files with `using static WorldShapingWandsMod.Common.Utilities.Msg` and replaced all hardcoded strings with `Get(...)` calls.
- Complex result summaries (dynamic item icons, compound conditionals) remain hardcoded — would need message builder refactoring for proper ICU-style pluralization.

**Files**: `Common/Utilities/Msg.cs` (new), `Localization/en-US_Mods.WorldShapingWandsMod.hjson`, `Common/Items/BaseCyclingWand.cs`, `Common/Input/WandControls.cs`, `Common/Undo/UndoManager.cs`, `Common/UI/SafekeepingSettingsPanel.cs`, `Common/Systems/ProgressiveTileProcessor.cs`, `Content/Items/WandOfBuildingBase.cs`, `Content/Items/WandOfBuildingSelect.cs`, `Content/Items/WandOfBuildingConfirm.cs`, `Content/Items/WandOfBuildingStamp.cs`, `Content/Items/WandOfDismantling.cs`, `Content/Items/WandOfDismantlingStamp.cs`, `Content/Items/WandOfReplacementBase.cs`, `Content/Items/WandOfReplacementSelect.cs`, `Content/Items/WandOfReplacementConfirm.cs`, `Content/Items/WandOfReplacementStamp.cs`, `Content/Items/WandOfWiringBase.cs`, `Content/Items/WandOfWiringSelect.cs`, `Content/Items/WandOfWiringConfirm.cs`, `Content/Items/WandOfWiringStamp.cs`, `Content/Items/WandOfSafekeepingBase.cs`, `Content/Items/WandOfSafekeepingSelect.cs`, `Content/Items/WandOfSafekeepingConfirm.cs`, `Content/Items/WandOfSafekeepingStamp.cs`

---

### #71 — French Localization Placeholder

**Problem**: No French localization file existed.

**Fix**: Created `fr-FR_Mods.WorldShapingWandsMod.hjson` with translated Keybinds, RecipeGroups, and Common UI keys. Remaining sections marked with `// TODO` for manual translation by the developer.

**Files**: `Localization/fr-FR_Mods.WorldShapingWandsMod.hjson` (new)

---

### #72 — Safekeeping ClearAll Confirmation Safeguard (New Feature)

**Problem**: The "Clear All" button in the Safekeeping settings panel executed immediately with no confirmation, risking accidental loss of hundreds of protected tiles.

**Fix**:
- Added `SafekeepingClearThreshold` config option (default: 50) under new `Safekeeping` config header in `WandConfig`.
- When total protected tiles+walls ≥ threshold, first click shows "Click to Confirm!" (red background, 3-second timeout). Second click within timeout executes the clear.
- Below threshold: no confirmation needed (for casual play).
- Threshold of 0 disables the safeguard entirely.

**Files**: `Common/Configs/WandConfig.cs`, `Common/UI/SafekeepingSettingsPanel.cs`, `Localization/en-US_Mods.WorldShapingWandsMod.hjson`

---

### #73 — `/undo` Chat Command (New Feature — Dev Testing)

**Problem**: The `UndoManager.Undo()` method existed but had no way to trigger it — no keybind, no command, no UI button.

**Fix**: Created `UndoCommand.cs` following the existing `ShapeTestCommand` pattern:
- `/undo` — undo one operation
- `/undo 3` — undo N operations
- `/undo all` — undo entire stack
- `/undo status` — show stack depth
Known to be provisional for dev testing. Production undo will use a keybind or UI button.

**Files**: `Common/Commands/UndoCommand.cs` (new)


## DevNotes #8 — Resolved Issues

### #82 — Cross-Mod Recipe Groups (Thorium + MagicStorage/Calamity)

**Problem**: AnyGem recipe group only contained vanilla gems. When Thorium is installed, Opal and Aquamarine should also qualify. Additionally, needed to verify compatibility with MagicStorage's `RegisterGroupClone` (which auto-merges groups registered under vanilla keys via `UnionWith`) and Calamity's `AnyGoldBar` pattern.

**Fix**: Rewrote `WandRecipeSystem.AddRecipeGroups()` to build gem list dynamically. Added `ModLoader.TryGetMod("ThoriumMod")` + `Mod.TryFind<ModItem>("Opal")` / `TryFind<ModItem>("Aquamarine")` detection to extend the gem list before creating the RecipeGroup. Added XML documentation explaining that MagicStorage automatically merges groups registered under vanilla keys (`nameof(ItemID.SilverBar)`, `nameof(ItemID.GoldBar)`) via its `RegisterGroupClone` + `UnionWith` pattern — no additional code needed.

**Files**: `Common/Systems/WandRecipeSystem.cs`

---

### #83 — Localization Newlines for Long Labels

**Problem**: Spanish ("Grosor del contorno") and French ("Épaisseur du contour") outline-thickness labels were too long for the UI panel, causing text overflow.

**Fix**: Verified that `\n` was already inserted directly into the localization `.hjson` strings: `"Grosor \n del contorno:"` (Spanish) and `"Épaisseur\n du contour :"` (French). The UI elements already handle multi-line text — no code changes needed. The approach of embedding newlines in localization strings avoids language-specific code branching (`if lang == spanish` anti-pattern).

**Files**: `Localization/es-ES_Mods.WorldShapingWandsMod.hjson`, `Localization/fr-FR_Mods.WorldShapingWandsMod.hjson` (verified, no changes)

---

### #84 — Wiring UI Close Button Click-Through Fix

**Problem**: Clicking the close button on the Wiring UI panel would pass through to the wand, placing/removing wire behind the panel. Root cause: `HoldItem()` runs during `Player.Update()`, *before* `ModSystem.UpdateUI()` sets `Main.LocalPlayer.mouseInterface`. So checking `mouseInterface` in `HoldItem` always sees `false` — the UI hasn't had a chance to set it yet.

**Fix**: Added `WandUISystem.IsCursorOverPanel()` which uses real-time `ContainsPoint(Main.MouseScreen)` to check if any visible panel is under the cursor (doesn't depend on `mouseInterface` timing). Added `IsMouseOverUI()` helper to `BaseCyclingWand` that combines both checks: `Main.LocalPlayer.mouseInterface || WandUISystem.IsCursorOverPanel()`. Updated all 5 instant wand `HoldItem()` methods to use `IsMouseOverUI()` instead of raw `mouseInterface`.

**Files**: `Common/UI/WandUISystem.cs`, `Common/Items/BaseCyclingWand.cs`, `Content/Items/WandOfBuildingInstant.cs`, `Content/Items/WandOfWiringInstant.cs`, `Content/Items/WandOfReplacementInstant.cs`, `Content/Items/WandOfDismantling.cs`, `Content/Items/WandOfSafekeepingInstant.cs`

---

### #85 — Ellipse Rasterization Y-Axis Symmetry Fix

**Problem**: Ellipses were not symmetrical on the Y-axis. The old implementation used a floating-point `Math.Sqrt` per-column direct approach with complex parity-dependent `yLo`/`yHi` distribution logic that produced asymmetric results for certain dimension combinations (especially even heights).

**Fix**: Replaced the entire `EllipseShape` rasterization with the `IncrementalFast` algorithm from `ClonedEllipseRasterization` (owned by the developer). The new algorithm:
- **O(W + H)** time complexity, pure integer arithmetic — no floating-point operations
- Generates half-heights from the outermost column inward using incremental integer updates
- `BuildFullHeights()` mirrors half-heights with correct even/odd parity handling, guaranteeing perfect Y-axis symmetry
- `HeightsToWorldTiles()` converts centered coordinates to world-space using `YRange()` for consistent vertical centering
- `ContainsPoint()` updated to use the reference `IsPointInEllipse` formula with parity-aware reposition factors

The algorithm computes `sx² * b² + sy² * a² <= a² * b²` using pre-computed deltas that update incrementally, avoiding any `sqrt` or division operations. Symmetry is guaranteed by construction: half-heights are computed once and mirrored, not computed independently for each side.

**Files**: `Common/Geometry/Shapes/EllipseShape.cs`

---

### #86 — Triangle Jitter on Drag

**Problem**: When dragging a triangle selection and the cursor moved near the start point's Y coordinate, the triangle would jitter vertically. Caused by `startIsTop = context.Start.Y <= context.End.Y` toggling rapidly when the cursor crossed the start's Y position, flipping the triangle orientation every frame.

**Fix**: Added degenerate case handling — when the bounding box width or height is ≤ 1 tile, the triangle fills the entire bounding box (degenerates to a rectangle). This prevents the most visually jarring jitter: at exactly 1-tile dimension, the orientation flip no longer changes the visual output since both orientations produce the same filled rectangle. Updated both `GetFilledTiles()` and `IsInsideTriangle()` to match. Added XML documentation explaining the orientation determination and why degenerate cases are special-cased.

**Files**: `Common/Geometry/Shapes/TriangleShape.cs`

---

### #87 — MP Replacement & Dismantling Fix (WorldGen.gen)

**Problem**: Wand of Replacement and Wand of Dismantling displayed the tile count in chat on multiplayer but didn't actually perform any actions. Root cause: these wands conditionally set `WorldGen.gen = true` only in single-player (`if (!isMultiplayer)`), which caused `WorldGen.KillTile`/`PlaceTile` to send individual per-tile `TileManipulation` network messages to the server. These per-tile messages conflicted with the subsequent `BulkTileOperations.BatchNetworkSync` (`SendTileSquare`) call, creating a dual-sync race condition where the server's individual tile validations could reject or revert the client-side changes.

**Fix**: Changed both wands to **always set `WorldGen.gen = true`** (matching the Building wand pattern that already worked in MP). This suppresses individual per-tile network messages and relies solely on `FinalizeBatch` → `BatchNetworkSync` → `SendTileSquare` to send the final tile state to the server in a single efficient packet. Applied to three code paths: Dismantling instant mode, Replacement tile mode, and Replacement wall mode.

**Files**: `Content/Items/WandOfDismantling.cs`, `Content/Items/WandOfReplacementBase.cs`

## DevNotes #9

```txt
Ellipse rasterization is working very smoothly.
The triangle jitter wasn't general but confined to when Equal Dimensions is toggled on. It moves all other the place increasing with the area of the isoceles rect triangle :/
Wand Of Dismantling and Wand of Replacement don't work in multiplayer
Protected tiles from Wand of Safekeeping don't port to multiplayer, any multiplayer protections get undone at world close and load
Tested equal dimensions ellipse, it also jitters and moves from the starting position
It would be great if the start and end position were highlighted with an outlined cyan square, basically a single cell overlay, very helpful not only for noticing if the shape is drifting or the start position is also drifting, but also for inscribing shapes such as ellipses and diamonds in some areas without having to check really carefully or use a mechanical ruler
I just polished the es-ES localization, however, the UI shows in english for some reason. It fixed itself somehow, it seems that changing the locallization file is somewhat buggy. In any case, there is no newline. Instead the literal r"\n" is showing in the UI :/
French localization crashed the game, idk what I messed up in the syntax 

 ---> System.ArgumentException: Found '}' where a key name was expected (check your syntax or use quotes if the key name includes {}[],: or whitespace). At line 335, column 16
   at Hjson.HjsonReader.readKeyName()
   at Hjson.HjsonReader.ReadCore(Boolean objectWithoutBraces)
   at Hjson.HjsonReader.ReadCore(Boolean objectWithoutBraces)
   at Hjson.HjsonReader.ReadCore(Boolean objectWithoutBraces)
   at Hjson.HjsonReader.Read()
   at Terraria.ModLoader.LocalizationLoader.LoadTranslations(TmodFile tModFile, GameCulture culture) in tModLoader\Terraria\ModLoader\LocalizationLoader.cs:line 223
   --- End of inner exception stack trace ---
   at Terraria.ModLoader.LocalizationLoader.LoadTranslations(TmodFile tModFile, GameCulture culture) in tModLoader\Terraria\ModLoader\LocalizationLoader.cs:line 240
   at Terraria.ModLoader.LocalizationLoader.LoadModTranslations(GameCulture culture) in tModLoader\Terraria\ModLoader\LocalizationLoader.cs:line 34
   at Terraria.Localization.LanguageManager.LoadFilesForCulture(GameCulture culture) in tModLoader\Terraria\Localization\LanguageManager.cs:line 172
   at Terraria.Localization.LanguageManager.ReloadLanguage(Boolean resetValuesToKeysFirst) in tModLoader\Terraria\Localization\LanguageManager.cs:line 141
   at Terraria.Localization.LanguageManager.SetLanguage(GameCulture culture) in tModLoader\Terraria\Localization\LanguageManager.cs:line 109
   at Terraria.Localization.LanguageManager.SetLanguage(Int32 legacyId) in tModLoader\Terraria\Localization\LanguageManager.cs:line 48
   at DMD<System.Void Terraria.Main:DrawMenu(Microsoft.Xna.Framework.GameTime)>(Main this, GameTime gameTime)
   at FargowiltasSouls.FargowiltasSouls.DrawMenu(orig_DrawMenu orig, Main self, GameTime gameTime)
   at Hook<System.Void FargowiltasSouls.FargowiltasSouls::DrawMenu(Terraria.On_Main+orig_DrawMenu,Terraria.Main,Microsoft.Xna.Framework.GameTime)>(Main , GameTime )
   at SyncProxy<System.Void Terraria.Main:DrawMenu(Microsoft.Xna.Framework.GameTime)>(Main , GameTime )
   at DMD<DMD<>?19818119::Terraria.Main::DoDraw>(Main this, GameTime gameTime)
   at Hook<System.Void CalamityMod.Utilities.Daybreak.Buffers.BuffersLoadingSystem::RenderTargetPool_DoDrawHook(Terraria.On_Main+orig_DoDraw,Terraria.Main,Microsoft.Xna.Framework.GameTime)>(Main , GameTime )
   at SyncProxy<System.Void Terraria.Main:DoDraw(Microsoft.Xna.Framework.GameTime)>(Main , GameTime )
   at Terraria.Main.Draw_Inner(GameTime gameTime)
   at Terraria.Main.Draw(GameTime gameTime)
   at Microsoft.Xna.Framework.Game.Tick()
   at Microsoft.Xna.Framework.Game.RunLoop()
   at Microsoft.Xna.Framework.Game.Run()
   at Terraria.Program.RunGame()
[20:49:00.962] [Main Thread/DEBUG] [TerrariaSteamClient]: Send: shutdown

It's this: WiresPlaced: {0} segments de fil placés.
Okay I see. The difference is that both in english and spanish, the text doesn't start with {0}. Is it so? How do I fix that? smh 
Since we are going to use circular thickness, there is no longer any need to clarify any perpendicular distance shenanigans, omni directional lines can be easily implemented. We keep cardinal line because the snapping into a single axis is still really useful
While we fix the multiplayer situation, let's merge MagicWiring's Multiplayer into 
Please check C:\Users\RYZEN 9\Documents\My Games\Terraria\tModLoader\ModSources\WorldShapingWandsMod\ClonedImproveGame, to have clarity about how they use the tModLoader API to make the multiplayer placing, and replacing of blocks work.
Note: Wand of Building does replace and consume blocks in multiplayer currently, but it doesn't drop the former blocks 
```

---

### #88 — EqualDimensions Jitter Fix (Triangle + Ellipse)

**Problem**: When `EqualDimensions` was toggled on, both triangle and ellipse shapes would jitter and drift from the starting position as the selection grew. The root cause was in `ShapeContext.GetBounds()`: it computed the center of the min/max bounding box using integer division (`centerX = (minX + maxX) / 2`), then expanded outward from that center. Integer truncation caused the origin to shift unpredictably as the selection grew by 1 tile — for example, Start=(100,100), End=(103,101) → center=101, minX=99; then End=(104,101) → center=102, minX=100. The X origin jumped from 99 to 100 just because the selection grew by 1 tile.

**Fix**: Replaced center-based expansion with **Start-anchored expansion**. When `EqualDimensions` is true, the square is anchored at the Start point and extends in the direction of End. This keeps the Start corner fixed as the selection grows:
- If End is right of Start → left edge anchored at Start.X, extends right
- If End is left of Start → right edge anchored at Start.X, extends left
- Same logic for Y axis

This eliminates the integer truncation issue entirely since no center computation is involved. Both triangles and ellipses with EqualDimensions now produce stable, non-jittering shapes. The behavior is also more intuitive: the user clicks to set the anchor corner, then drags to expand the square outward from that corner.

**Files**: `Common/Geometry/ShapeContext.cs`

---

### #89 — French Localization HJSON Crash

**Problem**: Loading the French localization (`fr-FR_Mods.WorldShapingWandsMod.hjson`) crashed the game with `System.ArgumentException: Found '}' where a key name was expected` at line 335. The cause: two localization values started with `{0}`:
```
WiresPlaced: {0} segments de fil placés.
WiresRemoved: {0} segments de fil retirés.
```
HJSON interprets a `{` at the start of an unquoted value as an object literal, not as a string. In English and Spanish, the same keys don't crash because the `{0}` placeholder appears mid-sentence (e.g., `Se colocaron {0} segmentos...`), not at the start.

**Fix**: Wrapped the two affected values in double quotes so HJSON treats them as literal strings:
```
WiresPlaced: "{0} segments de fil placés."
WiresRemoved: "{0} segments de fil retirés."
```

**Files**: `Localization/fr-FR_Mods.WorldShapingWandsMod.hjson`

---

### #90 — Spanish Localization `\n` Literal in UI

**Problem**: The Spanish UI label `OutlineThickness: Grosor \n del contorno:` displayed the literal characters `\n` in the UI instead of a line break. In HJSON, backslash escapes like `\n` are **not processed** in unquoted values — they're treated as literal text. Only double-quoted strings interpret escape sequences.

**Fix**: Wrapped the value in double quotes so HJSON interprets `\n` as a newline character:
```
OutlineThickness: "Grosor\ndel contorno:"
```

**Files**: `Localization/es-ES_Mods.WorldShapingWandsMod.hjson`

---

### #91 — Start/End Position Highlight Overlay

**Problem**: During an active selection, there was no visual indicator showing the exact Start and End tile positions. This made it difficult to notice shape drift, verify anchor positions, or inscribe shapes precisely within specific areas without relying on external tools.

**Fix**: Added cyan outlined single-tile markers at both the Start and End positions during active selection. The markers are drawn as a third pass after the shape fill and outline passes, so they appear on top of the main overlay. Added to `WandColors`:
- `StartMarker` / `EndMarker` — Color.Cyan
- `MarkerOutlineOpacity` — 0.85
- `MarkerOutlineWidth` — 2px

Added `DrawPositionMarker()` helper method that draws all four edges of a single-tile outline at a given world position. The markers provide immediate visual feedback for precise placement, shape inscribing, and detecting any anchor drift.

**Files**: `Common/Drawing/SelectionOverlay.cs`, `Common/Drawing/WandColors.cs`

---

### #92 — MP Dismantling & Replacement Fix (Dual-Sync Elimination)

**Problem**: Wand of Dismantling and Wand of Replacement didn't work in multiplayer. The previous fix (#87) set `WorldGen.gen = true` unconditionally to suppress per-tile network messages, then relied on `BatchNetworkSync` (`SendTileSquare`) to send the final tile state. However, for **destructive** operations (KillTile), the server may reject or revert state changes received via `SendTileSquare` that didn't go through the proper `TileManipulation` message path. This is different from the Building wand's `PlaceTile`, which the server accepts via `SendTileSquare`.

The original #64 fix (don't set `WorldGen.gen` in MP) was on the right track but still called `BatchNetworkSync`, creating a **dual-sync race condition**: each `KillTile` sent its own `TileManipulation` message AND then `BatchNetworkSync` sent a redundant `SendTileSquare` covering the same area. The server processed both messages, and the second one could conflict with or revert the first.

**Fix**: Implemented a clean separation of SP and MP sync strategies:
- **Single-player**: Set `WorldGen.gen = true` (suppresses sounds/effects/drops), then `FinalizeBatch` (frame update + network sync, which is a no-op in SP anyway).
- **Multiplayer**: Leave `WorldGen.gen = false` (each KillTile/ReplaceTile sends its own `TileManipulation` message to the server), then `FinalizeFrameOnly` (only frame update — no `BatchNetworkSync` since per-tile messages already handled server sync).

Added `BulkTileOperations.FinalizeFrameOnly()` helper that does `BatchFrameUpdate` without `BatchNetworkSync`. Applied to four code paths:
1. Dismantling instant mode
2. Replacement tile mode (instant)
3. Replacement wall mode (instant)
4. Progressive batch processor (both dismantling and replacement batches)

**Files**: `Content/Items/WandOfDismantling.cs`, `Content/Items/WandOfReplacementBase.cs`, `Common/Utilities/BulkTileOperations.cs`, `Common/Systems/ProgressiveTileProcessor.cs`
