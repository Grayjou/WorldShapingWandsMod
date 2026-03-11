# WorldShapingWandsMod — Roadmap

## Current State (post-DevNotes #4 — Alpha Release Prep)

The core infrastructure is complete and stable. All five wand families compile and run, shapes
generate correctly, undo captures full tile + wall state, and crafting recipes are in place.

| System | Status | Notes |
|---|---|---|
| Shape geometry (8 shapes, 2 modes, variable thickness) | ✅ Solid | Ellipse, Diamond, CardinalLine, Half-ellipses all correct |
| Outline system (`OutlineHelper`) | ✅ Solid | Centralised; shapes only provide filled tiles; Filled and Hollow modes |
| Selection state (immutable, lockable) | ✅ Solid | 4 modes: Instant/Select/Confirm/Stamp |
| Per-player wand state (`WandPlayer`) | ✅ Solid | |
| Wand of Building | ✅ Working | Tile+wall placement, stock check, infinite-resource, undo, exhaustion modes, recipe |
| Wand of Dismantling | ✅ Working | Pick-power check, tile+wall, undo, progressive mode, recipe |
| Wand of Replacement | ✅ Working | Tile+wall replacement (#37), pick-power, inventory consumption, undo, recipe |
| Wand of Wiring | ✅ Working | Wire/actuator placement and removal, recipe |
| Wand of Safekeeping | ✅ Working | Protect/unprotect tiles+walls, persistent+session, recipe |
| Four selection modes | ✅ Working | SelectionOwnerItemType prevents cross-wand execution |
| Mode cycling via right-click inventory | ✅ Working | |
| Screen-culled selection overlay | ✅ Working | Dimension labels with area count |
| Per-wand settings panels (draggable UI) | ✅ Working | Icon-based buttons |
| Config (infinite-resource, progressive, etc.) | ✅ Working | |
| Undo stack (20-deep, tile + wall state) | ✅ Working | |
| Recipe system with recipe groups | ✅ Working | AnyGem, AnyGoldBar, AnySilverBar, AnyEvilStone, AnyWandOfBuilding, AnyWandOfDismantling |
| Sound effects (per-wand, conditional) | ✅ Working | Dismantling: Tink, Replacement: Item29, Wiring: Item64, Safekeeping: Item30 |
| Lore system (divine + per-wand) | ✅ Working | ShowDivineLore override for Wiring |
| Localization keys | ✅ Working | En-US; includes Wall type |
| Dual-sprite system | ✅ Working | Inventory vs swing textures |
| Shimmer decraft | ✅ Working | All wands decraft to Amethyst |
| Cursor highlight | ✅ Working | Pulsing highlight at cursor when holding wand |
| UI close button | ✅ Working | Properly clears UserInterface state |

---

## Phase 1 — Alpha Release (current)

**Goal**: Complete, polished, single-player-ready alpha. All 5 wand families functional with
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

### Remaining Alpha Polish
- ✅ Cardinal line thickness verified (#21 — circular brush)
- ❌ Multiplayer testing notes (basic fix in place, needs thorough testing)
- ❌ Workshop description polish
- ❌ Advanced editor mode scaffolding (roadmap only)

---

## Phase 2 — Boundary Noise System (medium priority)

Adds procedural jitter to shape boundaries for organic-looking terrain placement.

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
- Screen-edge indicators for off-screen tiles
- **Cross-mod gem support**: When Thorium Mod is loaded, add Opal and Aquamarine to AnyGem recipe group via `ModLoader.TryGetMod("ThoriumMod")` in `AddRecipeGroups()`
- Wand of Erosion: erosion algorithms for deepening holes, shaping terrain

---

## Phase 6 — Multiplayer & Polish

- Packet-based sync for all placement/destruction operations
- Server-side validation
- Multiplayer protection permissions
- `TileLoader.CanPlace()` for modded tiles
- Workshop release (description, icon, tags)

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
