# World Shaping Wands Mod

> **Status: Pre-release / Active Development** — not yet published on the Steam Workshop.

A [tModLoader](https://github.com/tModLoader/tModLoader) mod for Terraria that provides a suite of
geometric world-shaping wands: build, destroy, replace and wire large areas in customisable shapes
with a single drag or two clicks.

---

## Features

### Implemented ✅

| Category | What works |
|---|---|
| **Shapes** | Rectangle, Ellipse (rasterised), Diamond, Triangle, L-shaped Line |
| **Shape modes** | Filled, Hollow (boundary-only), Outline (configurable thickness 0–50) |
| **Wand of Building** | Place any solid block / platform / rope / rail / planter-box / grass seed in a shape; respects inventory stock; infinite-resource config option |
| **Wand of Destruction** | Destroy tiles and/or walls in a shape; pick-power check; drop suppression toggle |
| **Wand of Replacement** | Replace the first inventory tile type with the second across a shape; pick-power check |
| **Wand of Wiring** | Place or remove red/green/blue/yellow wire and actuators in a shape |
| **Three selection modes** | *Instant* (click-and-drag), *Select* (click-start → click-end), *Confirm* (click-start → click-end → click-confirm) |
| **Mode cycling** | Right-click in inventory to cycle Instant → Select → Confirm → Instant |
| **Undo** | Up to 20 per-player undo steps for all wand operations (tile + wall state) |
| **Preview overlay** | Screen-culled shape overlay with dimension label; colour-flashes orange when clamped |
| **Per-wand settings UI** | Draggable panel per wand type (Building / Destruction / Replacement / Wiring) |
| **Config** | Infinite-resource mode with configurable threshold |

### Planned 🔄

See [`dev_notes/TODO_FEATURES.md`](dev_notes/TODO_FEATURES.md) for the full list and
[`dev_notes/Roadmap.md`](dev_notes/Roadmap.md) for phased milestones.

High-level highlights:
- Boundary **noise** (procedural jitter on shape edges)
- Full **preview / commit / discard** workflow (shows required & consumed resources)
- **Selection Wand** with boolean operations (add / remove / intersect / XOR)
- **Drawing Wand** (stage changes before committing)
- **Multi-level undo** for preview commits
- Slope application, wall operations, multiplayer sync

---

## How to Use

1. Obtain a wand item (currently cheatable via `/give`; crafting recipes are TBD).
2. Left-click to start a selection; drag or click again to set the end point.
   - **Instant mode**: release the mouse button to apply immediately.
   - **Select mode**: left-click a second time to apply.
   - **Confirm mode**: click a second time to lock the selection, then a third time to apply.
3. Right-click **while selecting** to cancel the selection.
4. Right-click **while not selecting** to open the settings panel for the held wand.
5. Right-click **in inventory** to cycle the wand between Instant / Select / Confirm modes.

---

## Architecture Overview

```
WorldShapingWandsMod/
├── Common/
│   ├── Commands/       — /shape test command
│   ├── Configs/        — WandConfig (client-side, infinite resources)
│   ├── Drawing/        — SelectionOverlay (screen-culled shape preview)
│   ├── Enums/          — ShapeType, ShapeMode, SelectionMode, PlaceType, etc.
│   ├── Geometry/
│   │   ├── IShapeProvider.cs  — shape contract
│   │   ├── OutlineHelper.cs   — unified Filled / Hollow / Outline logic
│   │   ├── ShapeContext.cs    — immutable per-call parameters
│   │   ├── ShapeRegistry.cs   — provider map, Initialize / Unload
│   │   ├── ShapeTileSet.cs    — result (tiles + boundary)
│   │   └── Shapes/            — Rectangle, Ellipse, Diamond, Triangle, Line
│   ├── Input/          — ThicknessControls keybinds
│   ├── Items/          — BaseCyclingWand base class
│   ├── Players/        — WandPlayer (per-player selection + settings)
│   ├── Selection/      — immutable SelectionState
│   ├── Settings/       — WandSettings, ShapeInfo, per-wand settings structs
│   ├── UI/             — WandUISystem + per-wand settings panels
│   ├── Undo/           — UndoManager, UndoAction, TileSnapshot
│   └── Utilities/      — GeometryHelper, ItemTypeHelper, WiringHelper
├── Content/
│   └── Items/          — WandOfBuilding{Instant,Select,Confirm},
│                          WandOfDestruction{…}, WandOfReplacement{…},
│                          WandOfWiring{…}
├── Localization/       — en-US hjson keys
└── dev_notes/          — Developer documentation
```

---

## Development Notes

- [`dev_notes/Roadmap.md`](dev_notes/Roadmap.md) — phased feature plan
- [`dev_notes/TODO_FEATURES.md`](dev_notes/TODO_FEATURES.md) — detailed feature checklist
- [`dev_notes/SanityChecks.md`](dev_notes/SanityChecks.md) — safety measures, separation of concerns, known limitations

---

## Building

This mod requires tModLoader. There is no automated CI build at this time.

```bash
# Place the repository folder under tModLoader's Mod Sources directory, then
# build with the in-game "Mod Sources" menu, or use the tModLoader CLI:
dotnet build
```

---

## License

[MIT](LICENSE) — see `LICENSE` for details.

The `ClonedMagicWiring/` directory contains reference material from the
[MagicWiring](https://github.com/Grayjou/MagicWiringMod) mod and retains its original license.
