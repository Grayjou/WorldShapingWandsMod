# Common Concepts

> **Audience**: Players who've used at least one wand and want to understand the *shared* mechanics that show up in every wand guide.
> **Companion**: [Getting Started](GettingStarted.md) for the 5-minute orientation; the [per-wand guides](Wands/) for family-specific behaviour.

This guide is the single source of truth for anything that's not specific to one wand. When a per-wand guide says *"see Common Concepts → Selection Modes"*, this is where it links to.

---

## Table of contents

- [Selection Modes](#selection-modes)
- [Shapes, Slicing, and Inversion](#shapes-slicing-and-inversion)
- [Filled vs Hollow vs Thickness](#filled-vs-hollow-vs-thickness)
- [Tri-State Buttons (Ignore / Apply / Remove)](#tri-state-buttons-ignore--apply--remove)
- [The Selection Overlay](#the-selection-overlay)
- [Cancellation](#cancellation)
- [Undo and Redo](#undo-and-redo)
- [Safekeeping (the lock layer)](#safekeeping-the-lock-layer)
- [Delimitation (the scope layer)](#delimitation-the-scope-layer)
- [Molding and the Canvas](#molding-and-the-canvas)
- [Inventory View — Chosen Items vs Pinned Items](#inventory-view--chosen-items-vs-pinned-items)
- [Carefree Mode and Sandbox flags](#carefree-mode-and-sandbox-flags)
- [Progressive Processing](#progressive-processing)
- [Multiplayer Authority](#multiplayer-authority)

---

## Selection Modes

Every cycling wand has **four selection modes**. Right-click the wand **in your inventory** to cycle.

| Mode | Click pattern | Behaviour |
|------|---------------|-----------|
| **Instant** | hold-down → drag → release | Shape is centred between mouse-down and mouse-up; fires on release |
| **Select** | click → click | First click sets the start corner; second click sets the end corner and fires immediately |
| **Confirm** | click → click → click | First two clicks lock the shape and show a preview; third click commits, or right-click to cancel |
| **Stamp** | click → click → click → click → click… | First two clicks define the shape; third click locks the anchor; subsequent clicks stamp the same shape at new positions |

In Select / Confirm / Stamp, **right-click while mid-selection** cancels the selection. **Right-click while not selecting** opens the wand's settings panel.

**Why four modes?** Different tasks want different ergonomics. Instant is fastest for tiny edits. Select is most precise for fixed rectangles. Confirm is safest for big destructive ops. Stamp is unbeatable for repeated patterns.

---

## Shapes, Slicing, and Inversion

The shape engine produces a `HashSet<Point16>` of tile positions, then the family-specific code runs on each position. Eight shapes are available across every family.

| Shape | Algorithm |
|-------|-----------|
| **Rectangle** | Bounding-box fill |
| **Ellipse** | `Math.Sqrt` rasterisation |
| **Diamond** | Manhattan-distance lattice (×2 integer arithmetic) |
| **Triangle** | Scanline fill with degenerate-case handling |
| **Elbow** | Two-segment L-shape, axis chosen by the first mouse movement |
| **Cardinal Line** | 8-direction Bresenham with a circular brush |
| **Straight Line** | Free-angle Bresenham with a variable-thickness brush |
| **Mold** | A custom shape sculpted with the Wand of Molding (see [Molding and the Canvas](#molding-and-the-canvas)) |

### Slicing

Any shape can be **sliced** along its bounding-box centre via the **Slice** dropdown:

| Slice | Effect |
|-------|--------|
| **Full** (default) | The full shape |
| **HalfHorizontal — Top** | Top half only |
| **HalfHorizontal — Bottom** | Bottom half only |
| **HalfVertical — Left** | Left half only |
| **HalfVertical — Right** | Right half only |

Slicing is a single mechanism that replaced the older dedicated half-shapes. It works on every shape, including Mold.

### Equal Dimensions

The **Equal Dimensions** toggle forces the bounding box to be square. It turns Ellipse into a perfect circle, Rectangle into a square, Diamond into an equilateral diamond, regardless of how non-square your two clicks were.

### Connect Diameter

For *sliced Hollow* shapes (e.g. half-ellipse hollow), the cut edge is open by default. **Connect Diameter** closes it with a row of tiles, giving you a closed half-shell.

### Invert Selection

**Invert Selection** flips which positions are inside vs outside the shape, *within the shape's bounding box*. For example, an inverted Hollow Ellipse gives you a rectangular frame with an ellipse-shaped hole in the middle.

---

## Filled vs Hollow vs Thickness

| Mode | Result |
|------|--------|
| **Filled** | Every tile inside the shape |
| **Hollow** | Only the outline of the shape |

The **Thickness** slider (0–50, default 1) sets how wide the outline is. Hollow with thickness 1 is a single-tile outline; thickness 5 is a 5-tile-thick band; thickness 0 collapses to nothing.

Thickness uses **Chebyshev erosion** — `OutlineHelper` removes any tile whose neighbours are all also inside the shape, and repeats `Thickness` times. The result is uniform-width outlines on every shape.

Adjust thickness in real time with `[` and `]` while holding the wand.

---

## Tri-State Buttons (Ignore / Apply / Remove)

Several wand options aren't simple on/off — they're three-state.

| State | Symbol on the panel | Meaning |
|-------|---------------------|---------|
| **Ignore** (default) | grey / neutral icon | Don't touch this property; leave whatever's there alone |
| **Apply** | green / "✓" icon | Force the property ON for every affected position |
| **Remove** | red / "✗" icon | Force the property OFF for every affected position |

Wands that use tri-state controls (and what they control):

| Wand | Tri-state options |
|------|-------------------|
| **Coating** | Illuminant, Echo |
| **Building** | Actuation, Paint Sprayer |
| **Replacement** | Paint Sprayer |
| **Torches** | Echo Coat |
| **Dismantling** | (none — boolean toggles are simpler here) |

**Why tri-state and not two booleans?** Because "I don't care" is meaningfully different from "I want this OFF". Ignore preserves whatever's there; Remove actively strips it. Two booleans would force you to combine them with logic that's harder to reason about.

---

## The Selection Overlay

While you have a selection in progress (Select / Confirm / Stamp modes), the mod renders a **preview overlay** on the world showing exactly which tiles will be affected, along with a `W × H` dimension label.

The overlay has three render modes (settable in **OverlayConfig**):

| Render mode | What you see |
|-------------|--------------|
| **AlwaysFullShape** | Every affected tile is highlighted |
| **OnlyOutline** | Just the boundary tiles |
| **OutlineAndPartialFill** (default) | Outline highlighted; inside tinted lightly |

The overlay's **colour** is family-specific (Building = green, Dismantling = red, Coating = teal, Fluids = liquid-aware blue/orange/honey/violet, Torches = warm yellow, etc.) so you always know which wand is firing without looking at your hotbar.

The overlay is **screen-culled** — only on-screen chunks render — so it stays cheap on huge maps.

---

## Cancellation

Right-click during an active selection (any mode after the first click) to cancel it. Three things happen:

1. The preview overlay disappears.
2. A **colour-coded "Cancelled" splash** rises from the cursor — same colour as the family's overlay, so you get visual confirmation that *this wand specifically* cancelled.
3. Selection state resets; the wand is ready for a fresh first click.

Cancellation costs nothing and never partially applies.

---

## Undo and Redo

Every wand operation that modifies the world is recorded as a **TileSnapshot** on the per-player **UndoManager**.

| Operation | Stored as |
|-----------|-----------|
| Tile place | Snapshot of pre-place tile state at every affected position |
| Tile destroy | Snapshot of pre-destroy tile state (so undo brings the tile back) |
| Wall place / destroy | Snapshot of pre-op wall state |
| Paint, illuminant, echo, moss, coating | Snapshot of pre-op tile/wall colour/coating |
| Wire / actuator place / remove | Snapshot of pre-op wire byte |
| Liquid fill / drain / mix | Snapshot of pre-op liquid state |
| Safekeeping protect / unprotect | Snapshot of pre-op protection map |

### Controls

| Action | Effect |
|--------|--------|
| `Backspace` (default keybind) | Undo the most recent operation |
| `/undo` | Same as Backspace |
| `/undoinfo` | Preview the cost of the next undo (how many tiles, what kind of changes) without committing |

The default stack depth is 20 (configurable via **PreferencesConfig → MaxUndoStackSize**). Older entries fall off the bottom as new ones are pushed.

There is **no separate redo stack**. Undo + redo is a deliberate v1 simplification — if you undo too far, redo by re-running the wand operation manually. (Undo of an undo is on the v1.x backlog.)

---

## Safekeeping (the lock layer)

The **Wand of Safekeeping** marks individual tile positions or wall positions as **protected**. Every other wand checks against the `SafekeepingSystem` before touching a position and silently skips protected ones.

- Tile and wall protection are **independent layers** — you can protect just the wall and let the tile remain editable, or vice versa.
- Protection is **persisted to the world save** via `TagCompound`. Re-loading the world preserves it.
- In multiplayer, protection is **server-authoritative** — clients can't edit-config their way past it.
- The visual overlay (cyan = tile, magenta = wall, gold = both) only renders while you're holding the Wand of Safekeeping.

See [WandOfSafekeeping](Wands/WandOfSafekeeping.md) for the full feature breakdown.

---

## Delimitation (the scope layer)

The **Wand of Delimitation** defines a **delimitation area** — a region of tile positions that *every other wand* is clipped to. Positions outside the delimitation area are silently skipped.

- Use it to scope a wand operation to a single room, a single floor, or any irregular region.
- The area is built with the same Add / Remove / Intersect operations as Molding.
- A toast message warns you if a wand operation produced *zero* changes because the entire shape fell outside the delimitation area: *"No action executed. Delimitation Area is active and does not overlap with the selection."*
- The area persists per-player across world saves.

See [WandOfDelimitation](Wands/WandOfDelimitation.md) for the full feature breakdown.

> **Tip — Scope + Lock pattern**: Use Delimitation to scope your edits to a room, then use Safekeeping (with an oversized Rectangle) to lock everything inside that room. Two wands, one room, locked.

---

## Molding and the Canvas

The **Wand of Molding** sculpts a custom shape inside a working **canvas** region. The result is exposed as a new shape — `Mold` — in the shape dropdown of every other wand family.

Three core ideas:

1. **Canvas** = the maximum bounding region your mold can occupy. Drawn first, resized in *Canvas Edit Mode*. Rendered with a teal/cyan overlay.
2. **Mold selection** = the actual tile offsets that are part of your shape. Built up with Add / Remove / Intersect operations inside the canvas.
3. **Auto-promotion** = the mold is always live. Every other wand can pick `Mold` as a shape and stamp it; you don't have to "save" or "export" anything.

Combine Molding with Stamp mode on any other wand for arbitrary repeating templates: sculpt a window, stamp it 12 times along a tower; sculpt a circuit footprint, wire it across an entire floor.

See [WandOfMolding](Wands/WandOfMolding.md) for the full feature breakdown.

---

## Inventory View — Chosen Items vs Pinned Items

Several wand families let you pick which exact item type the wand operates on (e.g. *which* tile the Wand of Building places, *which* torch the Wand of Torches lays down). This is exposed as the **Inventory View** panel — a strip of slots that mirror your inventory and let you click to **choose** an item.

Two distinct concepts:

| Concept | What it means | Lifetime |
|---------|---------------|----------|
| **Chosen** | The item the wand will use *right now* for the current sub-mode (e.g. "Wood for Solid blocks", "Stone Platform for Platforms"). | Resets when you switch sub-mode unless re-chosen |
| **Pinned** *(planned for v1.1.0)* | A persistent override that survives sub-mode switches and even non-empty inventory. | Cleared explicitly via right-click |

In v1.0.0 only the **Chosen** concept is shipped — the wand uses the chosen item if it's still in your inventory; otherwise it falls back to the first compatible item it finds. Each sub-mode has its own independent chosen slot, so picking "Wood for Solid blocks" does **not** make Wood the chosen Platform item too.

The **ghost-pin toast** ("Wood will be remembered for next time you build solid blocks") flashes on the *first* time you chose an item per sub-mode, as a discoverability hint.

---

## Carefree Mode and Sandbox flags

The mod ships with a set of **sandbox flags** on the server's `SandboxConfig` that relax normal play restrictions:

| Flag | Default | What it relaxes |
|------|---------|-----------------|
| **SuppressDrops** | OFF | Destroyed tiles drop no items |
| **BypassPickaxePower** | OFF | Wands ignore tile pick-power requirements |
| **AllowDemonAltarDestruction** | OFF | Demon Altars can be wand-destroyed (vanilla effects still fire) |
| **AllowDelicateTileDestruction** | OFF | Crystal Hearts, Life Fruit, etc. can be wand-destroyed |
| **IgnoreLockedKeyRequirements** | OFF | Locked chests / doors can be wand-edited without keys |
| **AutoOpenChests** | OFF | Wand-destroyed chests dump contents to the ground |
| **VacuumItems** | ON | Drops auto-pickup if inventory has space |

**Carefree Mode** (on `CarefreeConfig`) is a *preset* that flips the first 4 flags ON in one click and additionally unlocks **Void Everything** (the Wand of Dismantling's nuclear option that erases tile data without firing kill effects).

Carefree is intended for build-mode / creative servers. It's a single config toggle so non-Carefree servers stay strict by default.

---

## Progressive Processing

Large operations (thousands of tiles) can chunk-process across multiple frames to avoid frame drops, controlled by **PerformanceConfig → EnableProgressiveProcessing** (ON by default).

| Side | Behaviour |
|------|-----------|
| **Singleplayer** | Progressive batching is supported and visualised — you'll see the operation paint in across a few frames |
| **Multiplayer** | The server processes the whole operation atomically; progressive batching is bypassed for sync correctness |

Batch size and frame delay are configurable. The default values are tuned for stable 60 FPS on a mid-range machine running a 10,000-tile operation.

---

## Multiplayer Authority

Every wand family is **server-authoritative**. The flow is identical across families:

1. **Client** assembles the operation (selection state + family settings) and sends a packet via `WandPacketHandler`.
2. **Server** validates: max-distance, max-operation-size, cooldown rate-limit, sandbox flags, safekeeping, delimitation.
3. **Server** executes the operation on the canonical world.
4. **Server** broadcasts the resulting tile set to all clients.

Consequences:

- A client cannot bypass server-side limits by editing their own config.
- Safekeeping is enforced on the server, so no rogue client can edit a protected build.
- Undo is per-player on the server; `/undo` always undoes *your* most recent operation, not anybody else's.
- Cooldown rate-limiting (configurable on **LimitsConfig**) prevents packet flooding.

Every wand family except the Torch Wheel uses `WandPacketHandler`. The Torch Wheel ships its tile placements via the standard projectile / tile-set netcode (it's a projectile, not a single-shot operation), and applies its hard caps (MaxTorches / MaxTiles / MaxBacktrackSteps) for equivalent abuse-prevention.

---

## Where to go next

- **[Getting Started](GettingStarted.md)** — the 5-minute orientation if you skipped it.
- **[Wand of Building](Wands/WandOfBuilding.md)** — the most common wand, exercises most of these concepts.
- **[Wand of Molding](Wands/WandOfMolding.md)** — the canvas system in depth.
- **[Wand of Delimitation](Wands/WandOfDelimitation.md)** — the scoping layer in depth.
- **[Wand of Safekeeping](Wands/WandOfSafekeeping.md)** — the lock layer in depth.
