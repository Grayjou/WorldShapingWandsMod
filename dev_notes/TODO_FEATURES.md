# TODO Features

> **Legend:** ✅ Implemented &nbsp;|&nbsp; 🔄 In Progress / Partial &nbsp;|&nbsp; ❌ Not Started
> 
> Updated after DevNotes #4 (alpha release prep)

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
- ❌ Screen-edge indicators for off-screen tiles
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

### Wand of Replacement ✅

- ✅ Replaces every instance of source-type tiles with target-type tiles in selection
- ✅ Pick-power validation
- ✅ `CanKillTile` check
- ✅ Inventory consumption of replacement tiles
- ✅ Undo support
- ✅ Settings UI: Source / Target ObjectType selectors (Tile, Platform, Rope, PlanterBox, Rail, Seeds, Wall, Air)
- ✅ Air as target type — erases matching tiles
- ✅ Wall-to-wall replacement mode (#37) — uses WallType/KillWall/PlaceWall/WallFrame
- ✅ Substrate-variant awareness (dirt ↔ grass, mud ↔ jungle grass, etc.)
- ✅ Progressive replacement mode (batched waves)
- ✅ Sound effect on successful replacement (SoundID.Item29 @ 0.25 vol, conditional)
- ✅ Recipe (Instant only): 1 Any Wand of Building + 1 Any Wand of Dismantling + 1 Mana Crystal @Anvil
- ✅ Multiplayer fix: skip WorldGen.gen in MP for proper per-tile network sync
- ❌ Air as source type (fill empty gaps within selection) — Air is target-only by design

### Wand of Wiring ✅

- ✅ Place or remove red/green/blue/yellow wire and actuators
- ✅ Shape-based area wiring
- ✅ Four selection modes
- ✅ Settings UI with wire type toggles and place/remove mode   
- ✅ Sound effect on successful wiring (SoundID.Item64, conditional: EnableWandSounds)
- ✅ Recipe (Instant only): 1 Wire Kite, 50 Wire, 10 Actuator @Tinkerer's Workbench

#### Missing from MagicWiring integration

- ❌ **Per-wire colour overlay** — wiring-specific colour coding
- ❌ **Distance clamping with visual feedback**
- ❌ **Wire/actuator inventory consumption** — currently places for free
- ❌ **Proper multiplayer wiring packets**

### Wand of Safekeeping ✅

- ✅ Protect/unprotect tiles and walls in a shape
- ✅ Persistent protection (saved with world via TagCompound)
- ✅ Session protection (cleared on world exit)
- ✅ Protection overlay rendering
- ✅ All other wands check `SafekeepingSystem.IsProtected()` before modifying tiles
- ✅ Settings UI: Mode toggle, tile/wall protection toggles, shape selector
- ✅ Sound effect on successful protection (SoundID.Item30, conditional: EnableWandSounds)
- ✅ Recipe (Instant only): 5 Any Gold Bar, 10 Any Silver Bar, 5 Gemstone, 20 Obsidian, 10 Any Evil Stone, 1 Mana Crystal @Anvil

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
- ✅ Safekeeping: SoundID.Item30 / IceBlockPlace (conditional: EnableWandSounds)
- ✅ EnableWandSounds config toggle (default: true)

### Selection Overlay ✅

- ✅ Screen-culled tile rendering
- ✅ Cancelled selection fade animation with rising "Cancelled" text
- ✅ Tile set caching (invalidated on change)

---

## Recipe Groups ✅

- ✅ Any Gem (Amethyst through Amber) — labeled "Gemstone"
- ✅ Any Gold Bar (Gold / Platinum)
- ✅ Any Silver Bar (Silver / Tungsten)
- ✅ Any Evil Stone (Ebonstone / Crimstone)
- ✅ Any Wand of Building (all 4 modes)
- ✅ Any Wand of Dismantling (all 4 modes)
- ❌ **Thorium gem support**: Add Opal and Aquamarine to AnyGem when Thorium Mod is loaded

---

## Lore System ✅

- ✅ Common divine lore: "The Gods of Space let you enact your thoughts into reality."
- ✅ Per-wand specific lore (override `WandLore`)
- ✅ `ShowDivineLore` virtual property — Wiring wand overrides to false (non-divine lore)
- ✅ Shift-gated tooltip display

---

## Shape & Outline System ✅

- ✅ 8 shape types: Rectangle, Ellipse, Diamond, Triangle, Elbow, CardinalLine, HalfEllipseH, HalfEllipseV
- ✅ 2 fill modes: Filled, Hollow
- ✅ Variable outline thickness (0–50)
- ✅ Equal Dimensions toggle (square/circle constraint)
- ✅ CardinalLine circular brush thickness (Euclidean distance, cached offsets)
- ✅ Centralised OutlineHelper
- ❌ Boundary noise system (procedural jitter)
- ❌ Additional shape modes (concentric rings, spiral)

---

## Configuration ✅

- ✅ `WandConfig` — infinite resource threshold, suppress drops, bypass pickaxe power
- ✅ Progressive mode with configurable batch size and interval
- ✅ Selection distance caps (Small/Big)
- ✅ Show lore tooltips toggle
- ✅ Show dimensions toggle
- ✅ EnableWandSounds toggle (default: true)

---

## Undo System ✅

- ✅ Per-player undo stack (20-deep)
- ✅ Tile state captured (type, frame, slope, half-block, wall type)
- ✅ Undo command (configurable keybind)
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
- ❌ Packet-based sync for all operations (currently basic SendTileSquare)
- ❌ Server-side validation
- ❌ Multiplayer protection permission system
- ❌ `TileLoader.CanPlace()` validation for modded tiles
- ❌ Workshop release polish

---

## Future — Cross-Mod Compatibility

- ❌ **Thorium gem support**: Add Opal, Aquamarine to AnyGem recipe group via `ModLoader.TryGetMod("ThoriumMod")` + `Mod.TryFind<ModItem>()`
- ❌ **Wand of Erosion**: Erosion algorithms for deepening holes, shaping terrain

---

## Concerns & Open Questions

- **Undo complexity**: reversing operations may leave multi-tile objects (doors, furniture) in broken states
- **Flood fill**: potential future feature — fill contiguous regions of matching tiles
- **Wiring consumption**: currently free — should match vanilla wire consumption eventually
- **Max canvas limit**: large operations can cause frame spikes — progressive mode mitigates but doesn't cap
