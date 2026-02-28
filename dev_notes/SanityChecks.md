# Sanity Checks & Safety Analysis

This document records the safety measures in place, the separation-of-concerns boundaries in the codebase, known bugs that have been fixed, and remaining limitations to be aware of.

---

## Safety Measures in Place

### Tile operations

- **World-bounds guard** — every tile loop in `ExecuteBuilding`, `ExecuteDestruction`, and `ExecuteReplacement` calls `WorldGen.InWorld(x, y, 1)` with a 1-tile padding before touching `Main.tile`. Tiles outside world bounds are silently skipped.
- **Pick-power validation** — destruction and replacement both call `player.HasEnoughPickPowerToHurtTile(x, y)` before destroying a tile. Tiles that the player cannot break are counted as `skipped` and reported back in the chat message.
- **Indestructible tile check** — `WorldGen.CanKillTile(x, y)` is called before every tile destruction. Altar-class tiles and other special tiles are skipped and counted.
- **Occupied-tile guard** (building) — `Main.tile[x, y].HasTile` is checked before placing; occupied positions are skipped to avoid overwriting existing terrain unintentionally.
- **Item availability check** (building) — total matching items are counted via `ItemTypeHelper.CountItems` before the loop begins. If stock is insufficient and infinite-resource mode is off, the operation aborts with an explanatory chat message.
- **Non-consumable item handling** (building) — `ItemTypeHelper.CountItems` detects non-consumable items (`item.consumable == false`) and returns `true` to signal infinite availability, preventing incorrect stack deduction.

### Undo

- **Snapshot before modification** — `TileSnapshot` is taken *before* any `WorldGen.KillTile`, `KillWall`, or `PlaceTile` call, capturing the true prior state (tile type, frame, slope, half-block, **wall type**).
- **Single snapshot per tile** — the destruction loop tracks snapshotted positions in a `HashSet<Point>` and never adds a second snapshot for the same coordinate, preventing incorrect restoration when both tile *and* wall destruction apply to the same position.
- **Stack size cap** — `UndoManager` limits history to 20 entries. When the cap is exceeded, the *oldest* action is dropped, preserving the most recent operations.
- **History cleared on world enter** — `UndoManager.OnEnterWorld` clears the stack to prevent stale undo data persisting across world changes.

### Selection state

- **Immutable record** — `SelectionState` is a sealed class with no setters; every mutation returns a new instance. This prevents aliasing bugs where two systems inadvertently share and mutate the same object.
- **IsLocked flag** — in `ThreeClick` mode, the selection is locked after the second click, preventing `WandPlayer.PostUpdate` from dragging the end point with the mouse until the third click or cancellation.
- **ClearSelection on respawn** — `WandPlayer.OnRespawn` clears the active selection so a respawned player does not resume a dead selection from before death.

### UI

- **Client-side autoload** — `SelectionOverlay`, `WandUISystem` are both tagged `[Autoload(Side = ModSide.Client)]`, ensuring they never run on dedicated servers.
- **Null guards** — `WandUISystem` null-checks all panel references in `CloseAllUI` and `ModifyInterfaceLayers` to tolerate cases where `Load` was not yet called (e.g., during a hot-reload).
- **Multiplayer notification** — every tile/wall modification in both singleplayer and multiplayer calls `NetMessage.SendTileSquare(-1, x, y)` when `Main.netMode == NetmodeID.MultiplayerClient`, keeping other clients in sync.

### Shape geometry

- **Empty-bounds guard** — `EllipseShape.GetTiles` and `DiamondShape` return empty `ShapeTileSet`s immediately when `Width ≤ 0` or `Height ≤ 0`.
- **Degenerate-case handling** — `TriangleShape` handles the 1×1, 1×N, and N×1 degenerate cases explicitly to avoid division-by-zero and empty output.
- **Chebyshev erosion guard** — `OutlineHelper.ErodeChebyshev` exits early if the tile set becomes empty during erosion, preventing a superfluous extra pass.
- **Registry guard** — `ShapeRegistry.GetProvider` throws `ArgumentException` for unregistered shape types rather than silently returning null and causing a later NullReferenceException.

### Configuration

- **Range constraint** — `WandConfig.InfiniteResourceAmount` carries `[Range(0, 10000)]` to prevent absurdly large thresholds from being entered.
- **Thickness clamping** — `WandSettings.Validate()` and `ShapeInfo.Validate()` clamp thickness to `[0, 50]`. `WandConfig` is `ConfigScope.ClientSide` so it does not affect other players.

---

## Separation of Concerns

| Concern | Owner | Notes |
|---|---|---|
| Tile coordinate enumeration for each shape | `IShapeProvider` implementations | Shapes only produce filled tiles; they know nothing about modes |
| Mode-to-tile-set transformation (Filled / Hollow / Outline) | `OutlineHelper` | Single point of control; shapes call `OutlineHelper.Apply` |
| Per-call parameters (start, end, mode, thickness, bias) | `ShapeContext` (immutable struct) | Passed into providers; never mutated mid-operation |
| Shape provider lookup | `ShapeRegistry` (static dictionary) | Initialised once at mod load; cleared on unload |
| Per-player runtime state (active selection, settings) | `WandPlayer : ModPlayer` | Isolated per player; UI and overlays read but do not write settings directly |
| Undo history | `UndoManager : ModPlayer` | Separate `ModPlayer` to keep `WandPlayer` focused on selection logic |
| Visual overlay | `SelectionOverlay : ModSystem` | `[Autoload(Side = ModSide.Client)]` only; never touches game state |
| UI panels | `WandUISystem : ModSystem` + individual panels | Client-only; reads player settings, writes back through method calls |
| Item search logic | `ItemTypeHelper` (static utility) | No Terraria state mutation; pure query helpers |
| Geometry math | `GeometryHelper` (static utility) | No game-state access; pure math |

---

## Bug Fixes Applied

### 1. `UndoManager` — Dropped newest action instead of oldest (fixed)

**Symptom**: when the undo stack exceeded 20 entries, the loop was rebuilding the stack from index `[length-1]` down to `[1]`, which discarded `temp[0]` (the newest action) and kept the oldest. The intended behaviour is FIFO eviction — discard the oldest.

**Fix**: rebuild the stack from `temp[MaxUndoActions-1]` down to `temp[0]`, skipping all indices ≥ `MaxUndoActions`.

**File**: `Common/Undo/UndoManager.cs`

---

### 2. `SelectionOverlay` — Preview-mode check always evaluated to `false` (fixed)

**Symptom**: the guard
```csharp
if (!wandPlayer.Settings.ShouldShowPreview(isHoldingWand) && !wandPlayer.Selection.IsActive) return;
```
is unreachable as a return path because the method already returned at the earlier `if (!wandPlayer.Selection.IsActive) return;` check. The second clause `!wandPlayer.Selection.IsActive` is always `false` at that point, making the `&&` expression always `false`. The overlay therefore *always* drew when a selection was active, ignoring the `PreviewMode.Default` setting (which should only show the overlay while holding a wand).

**Fix**: simplified to `if (!wandPlayer.Settings.ShouldShowPreview(isHoldingWand)) return;`.

**File**: `Common/Drawing/SelectionOverlay.cs`

---

### 3. `WandOfDestructionBase` — Double snapshot corrupted undo for tile+wall destruction (fixed)

**Symptom**: when both `DestroyTiles` and `DestroyWalls` were enabled and a tile at position P had both a tile and a wall, the code added **two** snapshots for P:

1. Snapshot A — taken before tile destruction: `HadTile = true`, correct frame data.
2. Tile killed via `WorldGen.KillTile`.
3. Snapshot B — taken after tile destruction, before wall destruction: `HadTile = false`.
4. Wall killed.

During undo (forward iteration of the snapshot list):
- Snapshot A restored the tile correctly.
- Snapshot B then set `HasTile = false` again, **removing the just-restored tile**.

The net result was that undoing a tile+wall destruction left the tile absent and the wall absent (no wall undo was ever possible since `TileSnapshot` did not store `WallType`).

**Fix**:
- `TileSnapshot` now captures and restores `WallType` alongside tile data.
- The destruction loop uses a `HashSet<Point> snapshotted` to take exactly **one** snapshot per tile coordinate, before any modification. Both the tile-kill and wall-kill decisions are made before the snapshot is taken, so the single snapshot reflects the complete pre-operation state.

**Files**: `Common/Undo/UndoAction.cs`, `Content/Items/WandOfDestruction.cs`

---

## Known Limitations & Open Issues

### Undo of multi-tile objects

`TileSnapshot` restores tile data (type, frame, slope, wall) but does **not** account for multi-tile objects (furniture, doors, platforms with extended frame state). Restoring a tile that was part of a 2×2 piece of furniture by writing raw frame values may leave the neighbouring frame tiles in a broken visual state. Terraria uses frame coordinates to index into sprite sheets, and multi-tile objects require all frame tiles to be consistent.

**Workaround until proper fix**: the snapshot does call `WorldGen.SquareTileFrame` after restore, which re-evaluates neighbour connections, but this is insufficient for furniture that relies on specific multi-tile frame patterns.

**Suggested fix** (not yet implemented): before snapshotting, detect multi-tile object anchors using `TileObjectData.GetTileData`, snapshot all associated tiles together, and restore them as a unit.

---

### Undo of building does not restore drops

`WorldGen.KillTile` with `noItem: true` suppresses drops when the undo system is snapshotting during building operations. However, if a tile naturally drops an item when killed (e.g., ore → ore item), and the building wand places over it and then is undone, the original item is not re-granted to the player. The tile data is restored, but any items that should have dropped before the placement are not replicated.

**Severity**: low — the player still retains the tiles they placed (which are removed on undo), but may lose the original ore drop.

---

### Building wand `required` count includes occupied tiles

Before the operation, `required = tileSet.Tiles.Count()` counts **all** tiles in the shape, including already-occupied ones. The actual number of placed tiles may be significantly lower (occupied positions are skipped). This can cause the upfront stock check to fail even if enough blocks are available for the empty positions.

**Severity**: low — the user is told they need N blocks but the actual requirement may be ≤ N. Conservative but potentially confusing.

**Suggested fix**: do a pre-pass counting unoccupied tiles and use that value for the stock check.

---

### `GeometryHelper.IsInWorld` always returns `true`

The method currently has a hardcoded `return true` with a comment noting it should use `Terraria.Main.maxTilesX/Y`. The method appears unused in the current codebase (all callers use `WorldGen.InWorld` directly), but if referenced in the future it will not guard against out-of-bounds tile access.

---

### `WandPlayer` and `SelectionOverlay` both duplicate `IsHoldingWandItem`

Both classes contain nearly identical private methods enumerating all wand base types. New wand types added in the future must be added to **both** independently.

**Suggested fix**: move `IsHoldingWandItem` to `WandPlayer` as a public property and have `SelectionOverlay` call it via `wandPlayer.IsHoldingWandItem`.

---

### Non-consumable items not respected by infinite-resource config

`ItemTypeHelper.CountItems` correctly returns `infinite = true` when a non-consumable matching item is found. However, the `WandOfBuildingBase.ExecuteBuilding` method checks `config.EnableInfiniteResource` to decide `shouldConsume`, **ignoring** the `hasInfinite` return value. A non-consumable item will be detected (and the stock check passes), but then `shouldConsume = true` may still decrement its stack.

In practice the stack of a non-consumable item cannot reach 0 via `TurnToAir` (it would be replaced by the next item of the same type), but the decrement itself is incorrect.

**Suggested fix**: set `shouldConsume = false` when `hasInfinite` is true, before the config check.

---

### Multiplayer: client-authority only

All tile modifications are applied on the local client and then broadcasted via `NetMessage.SendTileSquare`. There is no server-side validation. A malicious client could modify tiles beyond their pick power or in protected regions. This is acceptable for a mod in active development but must be addressed before a public release.
