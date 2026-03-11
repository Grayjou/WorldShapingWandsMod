# World Shaping Wands Mod

> **Status: Pre-release / Active Development** — not yet published on the Steam Workshop.

A [tModLoader](https://github.com/tModLoader/tModLoader) mod for Terraria that provides a suite of
geometric world-shaping wands. Build, dismantle, replace, wire, and protect large areas of the world
using customisable shapes — rectangles, ellipses, diamonds, triangles, elbows, cardinal lines, and
half-ellipses — with a single drag or a series of clicks.

---

## Features

### Wand Families (5 wands × 4 modes = 20 items)

| Wand | What It Does |
|---|---|
| **Wand of Building** | Place solid blocks, platforms, ropes, rails, planter boxes, grass seeds, or walls in a shape. Respects inventory stock, supports infinite-resource mode, and configurable block-exhaustion behaviour (NextBlock / Cancel / Interrupt). Slope application included. |
| **Wand of Dismantling** | Destroy tiles and/or walls in a shape. Validates pick power and `CanKillTile`. Configurable drop suppression. Optional progressive (batched) processing for large operations. |
| **Wand of Replacement** | Replace every instance of a source tile type with a target type across a shape. Supports 7 object types (Tile, Platform, Rope, PlanterBox, Rail, Seeds, Air). Air as source = fill gaps; Air as target = erase matching tiles. Substrate variant detection (e.g., replacing "dirt" catches all grass variants). |
| **Wand of Wiring** | Place or remove red/green/blue/yellow wire and actuators in a shape. Defaults to the Elbow shape for vanilla wire-kite behaviour. |
| **Wand of Safekeeping** | Protect or unprotect tiles and walls. Other wands refuse to modify protected positions. Protection persists across world saves via `TagCompound`. Visual overlay shows protected areas when holding the wand. |

### Selection Modes

Each wand comes in 4 mode variants. Right-click in inventory to cycle between them.

| Mode | Behaviour |
|---|---|
| **Instant** | Click-and-drag, release to apply immediately |
| **Select** | Click to set start → click to set end and apply |
| **Confirm** | Click start → click end (locks selection) → click to confirm |
| **Stamp** | Click start → click end → stamp repeatedly at new positions |

### Shapes (8 types × 2 fill modes)

| Shape | Algorithm |
|---|---|
| **Rectangle** | Bounding-box fill |
| **Ellipse** | Direct `Math.Sqrt` rasterisation |
| **Diamond** | Manhattan distance with ×2 integer arithmetic |
| **Triangle** | Scanline fill with degenerate-case handling |
| **Elbow** | L-shaped right-angle joint (axis determined by first mouse movement) |
| **Cardinal Line** | 8-direction straight line (Bresenham-like) |
| **Half-Ellipse (H)** | Horizontal half of a doubled ellipse |
| **Half-Ellipse (V)** | Vertical half of a doubled ellipse |

All shapes support **Filled** and **Hollow** modes (configurable thickness 0–50).
An **Equal Dimensions** toggle forces square bounding boxes for perfect circles, squares, and equilateral diamonds.

### Additional Features

| Feature | Description |
|---|---|
| **Undo** | Up to 20 per-player undo steps for all tile/wall operations |
| **Preview overlay** | Screen-culled shape overlay with W×H dimension label; orange flash when selection is clamped |
| **Safekeeping overlay** | Colour-coded protected-tile display (cyan = tile, magenta = wall, gold = both) |
| **Cancel animation** | Colour-coded fade-out with rising "Cancelled" text per wand family |
| **Per-wand settings UI** | Draggable panel per wand type with icon buttons and hover tooltips |
| **Dual sprites** | Separate world and inventory textures per wand item |
| **Lore tooltips** | Shift-gated flavour text with shared + wand-specific lore (configurable) |
| **Progressive processing** | Optional batched execution for large operations to prevent frame drops |
| **Selection caps** | Configurable max tiles per shape (SmallCap for Elbow/Cardinal, BigCap for others) |
| **Config** | Infinite resources, drop suppression, pick power bypass, progressive mode, lore toggle, and more |
| **4 keybinds** | Thickness ± (`[` / `]`), toggle UI (`.`), undo step (`Backspace`) |

---

## How to Use

1. Obtain a wand item (currently via `/give`; crafting recipes are TBD).
2. Left-click to start a selection; drag or click again to set the end point.
   - **Instant**: release the mouse button to apply immediately.
   - **Select**: left-click a second time to apply.
   - **Confirm**: click a second time to lock the selection, then a third time to apply.
   - **Stamp**: click a second time to define the shape, then click repeatedly to stamp at new positions.
3. Right-click **while selecting** to cancel the selection.
4. Right-click **while not selecting** to open the settings panel for the held wand.
5. Right-click **in inventory** to cycle through the 4 selection modes.
6. Use `[` / `]` to adjust hollow thickness, `.` to toggle the settings panel, `Backspace` to undo.

---

## Architecture Overview

```
WorldShapingWandsMod/
├── Common/
│   ├── Commands/       — /shape test command (debug)
│   ├── Configs/        — WandConfig (client-side settings)
│   ├── Drawing/        — SelectionOverlay, SafekeepingOverlay, WandColors
│   ├── Enums/          — ShapeType, ShapeMode, SelectionMode, PlaceType, ObjectType,
│   │                      SlopeType, BiasType, BlockExhaustionMode, OperationType, PreviewMode
│   ├── Geometry/
│   │   ├── IShapeProvider.cs   — shape contract
│   │   ├── OutlineHelper.cs    — unified Filled / Hollow logic (Chebyshev erosion)
│   │   ├── ShapeContext.cs     — immutable per-call parameters
│   │   ├── ShapeRegistry.cs    — provider dictionary, Initialize / Unload
│   │   ├── ShapeTileSet.cs     — result container (tiles + boundary)
│   │   └── Shapes/             — Rectangle, Ellipse, Diamond, Triangle, Elbow,
│   │                              CardinalLine, HalfEllipseH, HalfEllipseV
│   ├── Input/          — WandControls (4 keybinds)
│   ├── Items/          — BaseCyclingWand (abstract base for all 20 wands)
│   ├── Players/        — WandPlayer (per-player selection state + 5 settings structs)
│   ├── Selection/      — SelectionState (immutable), CancelledSelectionState
│   ├── Settings/       — ShapeInfo, WandSettings, + 5 per-wand settings structs
│   ├── Systems/        — SafekeepingSystem (protection data), ProgressiveTileProcessor
│   ├── UI/             — WandUISystem + 5 settings panels + Elements/
│   ├── Undo/           — UndoManager, UndoAction (TileSnapshot)
│   └── Utilities/      — BulkTileOperations, GeometryHelper, ItemTypeHelper, WiringHelper
├── Content/
│   └── Items/          — 5 wand bases + 20 concrete items + 40 sprites
│                          (Building, Dismantling, Replacement, Wiring, Safekeeping)
├── Assets/
│   └── Icons/          — 30 UI icons (shapes, objects, slopes)
├── Localization/       — en-US hjson (keybinds, UI, configs, 20 items)
└── dev_notes/          — Roadmap, TODO, SanityChecks
```

### Key Design Patterns

- **Provider pattern** — Shapes implement `IShapeProvider`; `ShapeRegistry` maps `ShapeType` enum values to providers. Adding a shape = one class + one registry entry.
- **Immutable state** — `SelectionState` and `ShapeContext` return new instances on every mutation, preventing aliasing bugs.
- **Per-wand settings** — Each wand family has its own settings struct on `WandPlayer`. No global state leaks.
- **Client-side safety** — All rendering systems are `[Autoload(Side = ModSide.Client)]`.
- **Single-responsibility** — Shapes produce filled tiles only; `OutlineHelper` handles Hollow mode; overlays read but never write game state.

---

## Documentation

| Document | Description |
|---|---|
| [`CurrentStateAndStatus.md`](CurrentStateAndStatus.md) | Comprehensive technical reference: architecture, algorithms, file inventory, patterns |
| [`IssuesAndFixes.md`](IssuesAndFixes.md) | Issue tracker: #1–42 resolved, #6–49 deferred, with full context |
| [`dev_notes/Roadmap.md`](dev_notes/Roadmap.md) | Phased development plan (Phases 1–6) |
| [`dev_notes/TODO_FEATURES.md`](dev_notes/TODO_FEATURES.md) | Granular feature checklist with status markers |
| [`dev_notes/SanityChecks.md`](dev_notes/SanityChecks.md) | Safety measures, separation of concerns, known limitations |

---

## Building

This mod requires [tModLoader](https://github.com/tModLoader/tModLoader) (targeting .NET 8.0).

```powershell
# Place the mod folder under tModLoader's Mod Sources directory, then build:
dotnet build --no-restore -p:TargetFramework=net8.0

# Or use the in-game "Mod Sources" menu to build and reload.
```

> **Note**: TML003 + MSB3073 warnings are expected when tModLoader is already running
> (packaging lock file conflict — not compile errors).

---

## License

[MIT](LICENSE) — see `LICENSE` for details.

The `ClonedMagicWiring/` directory contains reference material from the
[MagicWiring](https://github.com/Grayjou/MagicWiringMod) mod and retains its original license.
