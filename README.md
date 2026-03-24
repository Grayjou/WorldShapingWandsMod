# World Shaping Wands Mod

> **Status: Pre-release / Active Development** — not yet published on the Steam Workshop.

A [tModLoader](https://github.com/tModLoader/tModLoader) mod for Terraria that provides a suite of
geometric world-shaping wands. Build, dismantle, replace, wire, coat, and protect large areas of the
world using customisable shapes — rectangles, ellipses, diamonds, triangles, elbows, cardinal lines,
and straight lines — with a single drag or a series of clicks. Shapes can be sliced in half
horizontally or vertically, and all support filled and hollow modes.

---

## Features

### Wand Families (6 wands × 4 modes = 24 items)

| Wand | What It Does |
|---|---|
| **Wand of Building** | Place solid blocks, platforms, ropes, planter boxes, grass seeds, or walls in a shape. Respects inventory stock, supports infinite-resource mode, and configurable block-exhaustion behaviour (NextBlock / Cancel / Interrupt). Slope application included. Same-type slope overwrite supported. **Paint Sprayer** auto-paints placed tiles. **Actuation** toggle (Ignore / Apply / Remove). |
| **Wand of Dismantling** | Destroy tiles and/or walls in a shape. Validates pick power and `CanKillTile`. Configurable drop suppression. Container destruction (chests, barrels) with content dropping and locked-chest unlocking. Demon altar and delicate tile protection. **Void Everything** mode (Carefree only) clears all tile data at a position without kill effects. Optional progressive (batched) processing for large operations. |
| **Wand of Replacement** | Replace every instance of a source tile type with a target type across a shape. Supports 5 object types (Tile, Platform, Rope, PlanterBox, Wall) plus Air. Substrate variant detection (e.g., replacing "dirt" catches all grass variants). Support detection prevents replacing tiles that would cause wall/torch collapse. |
| **Wand of Wiring** | Place or remove red/green/blue/yellow wire and actuators in a shape. Defaults to the Elbow shape for vanilla Grand Design behaviour. Full multiplayer packet support. Wire/actuator inventory consumption with per-type infinite overrides. |
| **Wand of Safekeeping** | Protect or unprotect tiles and walls. Other wands refuse to modify protected positions. Protection persists across world saves via `TagCompound`. Visual overlay shows protected areas when holding the wand. |
| **Wand of Coating** | Paint tiles and walls with any of 30 colours. Apply/remove Illuminant and Echo coatings via tri-state toggles (Ignore / Apply / Remove). Scrape and harvest moss in bulk. Repaint toggle for overwriting existing paint. |

### Selection Modes

Each wand comes in 4 mode variants. Right-click in inventory to cycle between them.

| Mode | Behaviour |
|---|---|
| **Instant** | Click-and-drag, release to apply immediately |
| **Select** | Click to set start → click to set end and apply |
| **Confirm** | Click start → click end (locks selection) → click to confirm |
| **Stamp** | Click start → click end → stamp repeatedly at new positions |

### Shapes (7 types × 2 fill modes × 3 slice modes)

| Shape | Algorithm |
|---|---|
| **Rectangle** | Bounding-box fill |
| **Ellipse** | Direct `Math.Sqrt` rasterisation |
| **Diamond** | Manhattan distance with ×2 integer arithmetic |
| **Triangle** | Scanline fill with degenerate-case handling |
| **Elbow** | L-shaped right-angle joint (axis determined by first mouse movement) |
| **Cardinal Line** | 8-direction straight line (Bresenham-like) with circular brush |
| **Straight Line** | Free-angle Bresenham line with variable-thickness brush |

All shapes support **Filled** and **Hollow** modes (configurable thickness 0–50).
**Shape slicing** (Full / HalfHorizontal / HalfVertical) halves any shape along an axis —
replacing the old dedicated half-ellipse shapes with a universal mechanism.
A **Connect Diameter** toggle controls gap behaviour in sliced shapes.
An **Equal Dimensions** toggle forces square bounding boxes for perfect circles, squares, and equilateral diamonds.
**Invert Selection** flips which tiles are inside vs outside the shape within the bounding box.

### Additional Features

| Feature | Description |
|---|---|
| **Undo** | Up to 20 per-player undo steps for all tile/wall operations |
| **Preview overlay** | Screen-culled shape overlay with W×H dimension label; 3 render modes (AlwaysFullShape, OnlyOutline, OutlineAndPartialFill) |
| **Safekeeping overlay** | Colour-coded protected-tile display (cyan = tile, magenta = wall, gold = both) |
| **Cancel animation** | Colour-coded fade-out with rising "Cancelled" text per wand family |
| **Per-wand settings UI** | Draggable panel per wand type with icon buttons and hover tooltips |
| **Dual sprites** | Separate world and inventory textures per wand item |
| **Lore tooltips** | Shift-gated flavour text with shared + wand-specific lore (configurable) |
| **Progressive processing** | Optional batched execution for large operations to prevent frame drops |
| **Selection caps** | Configurable max tiles per shape (SmallCap for Elbow/Cardinal, BigCap for others) |
| **Infinite resource** | 3-state per-type overrides (Default/ForceOn/ForceOff) for tiles, walls, grass seeds, wires, actuators |
| **Carefree Mode** | Sandbox preset that enables suppress drops, bypass pick power, allow demon altar destruction, allow delicate tile destruction, and unlocks Void Everything |
| **Void Everything** | Dismantling option (Carefree Mode only) that clears all tile data without kill effects — fastest bulk erasure |
| **Config** | Infinite resources, drop suppression, pick power bypass, progressive mode, lore toggle, overlay mode, demon altar protection, delicate tile protection, and more |
| **5 keybinds** | Thickness ± (`[` / `]`), toggle UI (`.`), undo step (`Backspace`), toggle suppress drops (`;`) |

---

## How to Use

1. Craft or obtain a wand item (recipes use gems + bars at a workbench/anvil).
2. Left-click to start a selection; drag or click again to set the end point.
   - **Instant**: release the mouse button to apply immediately.
   - **Select**: left-click a second time to apply.
   - **Confirm**: click a second time to lock the selection, then a third time to apply.
   - **Stamp**: click a second time to define the shape, then click repeatedly to stamp at new positions.
3. Right-click **while selecting** to cancel the selection.
4. Right-click **while not selecting** to open the settings panel for the held wand.
5. Right-click **in inventory** to cycle through the 4 selection modes.
6. Use `[` / `]` to adjust hollow thickness, `.` to toggle the settings panel, `Backspace` to undo, `;` to toggle drop suppression.

---

## Architecture Overview

```
WorldShapingWandsMod/
├── Common/
│   ├── Commands/       — Chat commands: /shape, /undo, /undoinfo, /wsw, /paint_debug, /unlock_chests
│   ├── Configs/        — WandConfig (client), WandServerConfig (server), WandClientConfig (cosmetic)
│   ├── Drawing/        — SelectionOverlay, SafekeepingOverlay, WandColors
│   ├── Enums/          — 16 enums: ShapeType, SelectionMode, ShapeMode, SliceMode, CoatingMode,
│   │                      PlaceType, ObjectType, SlopeType, BiasType, BlockExhaustionMode,
│   │                      OperationType, PreviewMode, OverlayRenderMode, InfiniteOverride,
│   │                      ConfirmationMode, TriStateValue
│   ├── Geometry/
│   │   ├── IShapeProvider.cs   — shape contract
│   │   ├── OutlineHelper.cs    — unified Filled / Hollow logic (Chebyshev erosion)
│   │   ├── ShapeContext.cs     — immutable per-call parameters (incl. slice + inversion)
│   │   ├── ShapeRegistry.cs    — provider dictionary, Initialize / Unload
│   │   ├── ShapeTileSet.cs     — result container (tiles + boundary)
│   │   ├── ShapePages.cs       — shape page cycling for UI panels
│   │   ├── SliceHelper.cs      — shape slicing logic (Full / HalfH / HalfV)
│   │   └── Shapes/             — Rectangle, Ellipse, Diamond, Triangle, Elbow,
│   │                              CardinalLine, StraightLine
│   ├── Input/          — WandControls (5 keybinds: thickness ±, UI, undo, suppress drops)
│   ├── Items/          — BaseCyclingWand (abstract base for all 24 wands)
│   ├── Networking/     — WandPacketHandler (8 packet types, server-authoritative)
│   ├── Players/        — WandPlayer (selection state, 6 per-wand settings, dual-slot storage)
│   ├── Selection/      — SelectionState (immutable), CancelledSelectionState
│   ├── Settings/       — ShapeInfo, WandSettings, + 7 per-wand settings structs
│   ├── Systems/        — SafekeepingSystem, ProgressiveTileProcessor, WandRecipeSystem
│   ├── UI/             — WandUISystem + WandPanelBuilder + 6 settings panels
│   │   └── Elements/   — UIDraggablePanel, UIIconButton, UIToggleButton,
│   │                      UITriStateButton, UISectionTitle, UISliceGrid
│   ├── Undo/           — UndoManager, UndoAction (TileSnapshot), UndoCostSummary
│   └── Utilities/      — BulkTileOperations, GeometryHelper, ItemTypeHelper, WiringHelper,
│                          ContainerHelper, TileHelper, Msg, WandRecipeConditions,
│                          VoidEverythingOperation
├── Content/
│   └── Items/          — 6 wand bases + 24 concrete items + 58 sprites
│                          (Building, Dismantling, Replacement, Wiring, Safekeeping, Coating)
├── Assets/
│   └── Icons/          — 48 UI icons (shapes, objects, slopes, toggles, modes)
├── Localization/       — 9 locale files (en-US complete, 8 structural placeholders)
└── dev_notes/          — Architecture docs, feature analyses, status, planning, session history
```

### Key Design Patterns

- **Provider pattern** — Shapes implement `IShapeProvider`; `ShapeRegistry` maps `ShapeType` enum values to providers. Adding a shape = one class + one registry entry.
- **Immutable state** — `SelectionState` and `ShapeContext` return new instances on every mutation, preventing aliasing bugs.
- **Per-wand settings** — Each wand family has its own settings struct on `WandPlayer`. No global state leaks.
- **Client-side safety** — All rendering systems are `[Autoload(Side = ModSide.Client)]`.
- **Single-responsibility** — Shapes produce filled tiles only; `OutlineHelper` handles Hollow mode; overlays read but never write game state.
- **Server-authoritative MP** — All wand families use `WandPacketHandler` for client → server → broadcast execution with cooldown rate-limiting and max-distance validation.


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
