# World Shaping Wands Mod

> **Status: Pre-release / Active Development** — not yet published on the Steam Workshop.

A [tModLoader](https://github.com/tModLoader/tModLoader) mod for Terraria that provides a suite of
geometric world-shaping wands. Build, dismantle, replace, wire, coat, protect, fill liquids, place
torches, sculpt custom shapes, and constrain operations to precise areas — all with
customisable shapes (rectangles, ellipses, diamonds, triangles, elbows, cardinal lines,
straight lines, plus user-sculpted **Molds**) using a single drag or a series of clicks.
Shapes can be sliced in half horizontally or vertically, and all support filled and
hollow modes.

---

## Features

### Wand Families (10 families × 4 modes = 42 items, plus 2 standalone Torch Wheel wands)

| Wand | What It Does |
|---|---|
| **Wand of Building** | Place 8 object types (solid blocks, platforms, ropes, planter boxes, grass seeds, walls, sloped tiles, half-blocks) in a shape. Per-object-type chosen-item dictionary. Slope + OverwriteSlope. Actuation tri-state (Ignore / Apply / Remove). Paint Sprayer tri-state (CoatingSettings / Inventory / Off). Configurable block-exhaustion behaviour (NextBlock / Cancel / Interrupt). |
| **Wand of Dismantling** | Destroy tiles and/or walls in a shape. Validates pick power and `CanKillTile`. Container destruction (chests, barrels) with content dropping and locked-chest unlocking. Demon altar and delicate tile protection. **Carefree + VoidEverything** mode clears all tile data without kill effects. Optional progressive (batched) processing. |
| **Wand of Replacement** | Replace one tile/wall/object type with another across a shape. 5 object types (Tile / Platform / Rope / PlanterBox / Wall) plus Air. SameType mode (replace any of a class). Per-ObjectType chosen-item dictionaries. PreservePaint toggle. PaintSprayer tri-state. Substrate variant detection. Support detection prevents collapse cascades. |
| **Wand of Wiring** | Place or remove red/green/blue/yellow wire and actuators in a shape. Five independent layer toggles packed to 1 byte. Defaults to **Elbow** shape for vanilla Grand Design parity. |
| **Wand of Safekeeping** | Mark tiles and walls as **protected** — every other wand refuses to modify them. Independent tile/wall layer toggles. Persists across world saves via `TagCompound`. Colour-coded overlay (cyan = tile, magenta = wall, gold = both). Server-side enforcement in MP. |
| **Wand of Coating** | Paint tiles and walls with any of 30 colours. Apply / remove **Illuminant** and **Echo** coatings via tri-state toggles. Scrape and harvest moss in bulk. Repaint toggle. Acts as the paint source for Building / Replacement's Paint Sprayer. |
| **Wand of Fluids** | Fill, drain, mix the four vanilla liquids (Water / Lava / Honey / Shimmer) in a shape. Three fill algorithms (FullLiquid / RainFill / PocketFill). Place **Bubble Blocks** with **Coat-in-Bubble** containment. Mix vs Overwrite toggle. SelectiveDrain per liquid type. |
| **Wand of Torches** | Lay torches on a Manhattan or Grid lattice with configurable spacing. 4 modes (Place / Replace / Remove / Convert). 7 reference modes for the lattice seed. Echo Coat tri-state. Biome torch + smart inventory lookup. AlignToExistingTorches snap. |
| **Wand of Molding** | Sculpt custom shapes (Add / Remove / Intersect) inside a canvas. The resulting **Mold** is exposed as a shape in *every* other wand for Stamp-mode reuse. |
| **Wand of Delimitation** | Define a **Delimitation Area** that scopes every other wand's operations to a precise region. Tiles outside are silently skipped. Pairs with Safekeeping for room-scoped workflows. |

Plus two **standalone projectile wands**:

| Wand | What It Does |
|---|---|
| **Torch Wheel Wand (Solid)** | Fire-and-forget projectile that wall-follows and drops torches at configurable spacing. Server-tunable max-torches / max-tiles / backtrack. Smart underwater + biome-torch handling. Shift+Right-click while held kills all active wheels. |
| **Torch Wheel Wand (Platform)** | Platform-tracing variant. Right-click in inventory cycles Solid ↔ Platform. *Tracing logic planned; placement projectile is a stub.* |

### Selection Modes

Each wand comes in 4 mode variants. Right-click in inventory to cycle between them.

| Mode | Behaviour |
|---|---|
| **Instant** | Click-and-drag, release to apply immediately |
| **Select** | Click to set start → click to set end and apply |
| **Confirm** | Click start → click end (locks selection) → click to confirm |
| **Stamp** | Click start → click end → stamp repeatedly at new positions |

### Shapes (8 types × 2 fill modes × 3 slice modes)

| Shape | Algorithm |
|---|---|
| **Rectangle** | Bounding-box fill |
| **Ellipse** | Direct `Math.Sqrt` rasterisation |
| **Diamond** | Manhattan distance with ×2 integer arithmetic |
| **Triangle** | Scanline fill with degenerate-case handling |
| **Elbow** | L-shaped right-angle joint (axis determined by first mouse movement) |
| **Cardinal Line** | 8-direction straight line (Bresenham-like) with circular brush |
| **Straight Line** | Free-angle Bresenham line with variable-thickness brush |
| **Mold** | User-sculpted custom shape from the Wand of Molding canvas — reusable across every wand |

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
│   ├── Configs/        — 10-config split: PreferencesConfig (client), LimitsConfig + ResourcesConfig +
│   │                      ProtectionConfig + DebugConfig (server), TorchWheelConfig (server),
│   │                      OverlayConfig + CanvasOverlayConfig + WandColorsConfig (cosmetic),
│   │                      LoreConfig (cosmetic)
│   ├── Drawing/        — SelectionOverlay, SafekeepingOverlay, MoldOverlay, WandColors
│   ├── Enums/          — ShapeType, SelectionMode, ShapeMode, SliceMode, CoatingMode, PlaceType,
│   │                      ObjectType, SlopeType, BiasType, BlockExhaustionMode, OperationType,
│   │                      PreviewMode, OverlayRenderMode, InfiniteOverride, ConfirmationMode,
│   │                      TriStateValue, FluidFillMode, LiquidTypeSelection, TorchMode,
│   │                      TorchReferenceMode, TilingStyle, WiringMode, SafekeepingMode,
│   │                      MoldOperation, PaintSprayerSource, OceanWaterproofTorch,
│   │                      NonOceanWaterproofTorch, …
│   ├── Geometry/
│   │   ├── IShapeProvider.cs   — shape contract
│   │   ├── OutlineHelper.cs    — unified Filled / Hollow logic (Chebyshev erosion)
│   │   ├── ShapeContext.cs     — immutable per-call parameters (incl. slice + inversion)
│   │   ├── ShapeRegistry.cs    — provider dictionary, Initialize / Unload
│   │   ├── ShapeTileSet.cs     — result container (tiles + boundary)
│   │   ├── ShapePages.cs       — shape page cycling for UI panels
│   │   ├── SliceHelper.cs      — shape slicing logic (Full / HalfH / HalfV)
│   │   └── Shapes/             — Rectangle, Ellipse, Diamond, Triangle, Elbow,
│   │                              CardinalLine, StraightLine, Mold
│   ├── Input/          — WandControls (5 keybinds: thickness ±, UI, undo, suppress drops)
│   ├── Items/          — BaseCyclingWand (abstract base for the 42 cycling wand items)
│   ├── Networking/     — WandPacketHandler (server-authoritative, cooldown rate-limited)
│   ├── Players/        — WandPlayer (selection state, per-family settings, dual-slot storage)
│   ├── Selection/      — SelectionState (immutable), CancelledSelectionState, DelimitationArea
│   ├── Settings/       — ShapeInfo, WandSettings, + per-family settings structs
│   │                      (Building, Fluids, Dismantling, Coating, Replacement, Wiring,
│   │                       Safekeeping, Torches, Molding, Delimitation)
│   ├── Systems/        — SafekeepingSystem, ProgressiveTileProcessor, WandRecipeSystem,
│   │                      DelimitationSystem, MoldSystem
│   ├── UI/             — WandUISystem + WandPanelBuilder + per-family settings panels
│   │   └── Elements/   — UIDraggablePanel, UIIconButton, UIToggleButton,
│   │                      UITriStateButton, UISectionTitle, UISliceGrid, UIInventoryView
│   ├── Undo/           — UndoManager, UndoAction (TileSnapshot), UndoCostSummary
│   └── Utilities/      — BulkTileOperations, GeometryHelper, ItemTypeHelper, WiringHelper,
│                          ContainerHelper, TileHelper, Msg, WandRecipeConditions,
│                          VoidEverythingOperation, TorchPlacementHelper, FluidFillAlgorithms
├── Content/
│   ├── Items/          — 10 wand bases × 4 modes (42 items) + 2 standalone Torch Wheel wands
│   ├── Buffs/
│   ├── Projectiles/    — FlyingTorchWheelSolid, FlyingTorchWheelPlatform,
│   │                      TorchWheelSolidProjectile, TorchWheelPlatformProjectile
│   └── Tiles/
├── Assets/
│   └── Icons/          — UI icons (shapes, objects, slopes, toggles, modes, mold ops)
├── Localization/       — 9 locale files (en-US complete, 8 structural placeholders)
├── UserGuides/         — Per-wand markdown guides for end users
└── dev_notes/          — Architecture docs, feature analyses, status, planning, session history
```

### Key Design Patterns

- **Provider pattern** — Shapes implement `IShapeProvider`; `ShapeRegistry` maps `ShapeType` enum values to providers. Adding a shape = one class + one registry entry.
- **Immutable state** — `SelectionState` and `ShapeContext` return new instances on every mutation, preventing aliasing bugs.
- **Per-wand settings** — Each wand family has its own settings struct on `WandPlayer`. No global state leaks.
- **Client-side safety** — All rendering systems are `[Autoload(Side = ModSide.Client)]`.
- **Single-responsibility** — Shapes produce filled tiles only; `OutlineHelper` handles Hollow mode; overlays read but never write game state.
- **Server-authoritative MP** — All wand families use `WandPacketHandler` for client → server → broadcast execution with cooldown rate-limiting and max-distance validation.


## User Guides

Start here:

- **[Getting Started](UserGuides/GettingStarted.md)** — 5-minute orientation for new players.
- **[Common Concepts](UserGuides/CommonConcepts.md)** — shared vocabulary across all wands (selection modes, shapes, tri-state, overlay, undo, safekeeping, delimitation, canvas, multiplayer).

Per-wand guides live under [`UserGuides/Wands/`](UserGuides/Wands/):

- [Wand of Building](UserGuides/Wands/WandOfBuilding.md)
- [Wand of Dismantling](UserGuides/Wands/WandOfDismantling.md)
- [Wand of Replacement](UserGuides/Wands/WandOfReplacement.md)
- [Wand of Wiring](UserGuides/Wands/WandOfWiring.md)
- [Wand of Safekeeping](UserGuides/Wands/WandOfSafekeeping.md)
- [Wand of Coating](UserGuides/Wands/WandOfCoating.md)
- [Wand of Fluids](UserGuides/Wands/WandOfFluids.md)
- [Wand of Torches](UserGuides/Wands/WandOfTorches.md)
- [Wand of Molding](UserGuides/Wands/WandOfMolding.md)
- [Wand of Delimitation](UserGuides/Wands/WandOfDelimitation.md)
- [Torch Wheel Wand](UserGuides/Wands/TorchWheelWand.md)


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
