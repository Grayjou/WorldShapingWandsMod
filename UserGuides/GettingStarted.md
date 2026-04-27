# Getting Started — World Shaping Wands

> **Audience**: New players who just installed the mod.
> **Length**: 5–10 minutes from "I have no idea what these are" to "I can build a hollow ellipse".
> **Companion docs**: [Common Concepts](CommonConcepts.md) explains shared vocabulary; the [per-wand guides](Wands/) explain each family in depth.

---

## What this mod gives you

**10 wand families** that edit the world in **geometric shapes**, plus **2 standalone Torch Wheel wands** that auto-light walls. Each family comes in **4 selection modes** (Instant, Select, Confirm, Stamp), so the mod ships **42 cycling wands + 2 Torch Wheel wands = 44 items** in total. They share one shape engine, one overlay, one undo stack, and one settings panel template — once you learn one wand, the others feel familiar.

The 10 families:

| Family | What it does | Full guide |
|--------|--------------|------------|
| **Building** | Place tiles, walls, platforms, ropes, slopes, plant pots, grass seeds | [WandOfBuilding](Wands/WandOfBuilding.md) |
| **Dismantling** | Mass-destroy tiles & walls (with container handling, optional sandbox) | [WandOfDismantling](Wands/WandOfDismantling.md) |
| **Replacement** | Swap one tile / wall / object type for another in bulk | [WandOfReplacement](Wands/WandOfReplacement.md) |
| **Wiring** | Place / remove red, green, blue, yellow wire and actuators | [WandOfWiring](Wands/WandOfWiring.md) |
| **Safekeeping** | Mark tiles / walls as protected — every other wand respects them | [WandOfSafekeeping](Wands/WandOfSafekeeping.md) |
| **Coating** | Paint, illuminant, echo, scrape, harvest moss | [WandOfCoating](Wands/WandOfCoating.md) |
| **Fluids** | Fill / drain / mix water, lava, honey, shimmer; bubble blocks | [WandOfFluids](Wands/WandOfFluids.md) |
| **Torches** | Lay torches on a Manhattan / Grid lattice with biome smarts | [WandOfTorches](Wands/WandOfTorches.md) |
| **Molding** | Sculpt a custom shape, reusable as **Mold** in every other wand | [WandOfMolding](Wands/WandOfMolding.md) |
| **Delimitation** | Define an area that **all other wands** are clipped to | [WandOfDelimitation](Wands/WandOfDelimitation.md) |

Plus the standalone **[Torch Wheel Wand](Wands/TorchWheelWand.md)** — a fire-and-forget projectile that wall-follows along caves and tunnels and drops torches at fixed spacing.

---

## Your first 5 minutes

1. **Craft any wand**. Recipes use early-game materials (gel, wood, gems, bars) at a workbench or anvil. The simplest one to start with is the **Wand of Building (Instant)**.
2. **Hold the wand**. A *settings panel* opens automatically the first time, and the cursor shows a small preview icon for the chosen object.
3. **Open the settings**: right-click while not selecting, or press `.` (the period key — see Keybinds below).
4. **Pick a shape**: Rectangle is the default. There are 8 shapes total (Rectangle, Ellipse, Diamond, Triangle, Elbow, Cardinal Line, Straight Line, Mold). Toggle **Filled / Hollow** to switch between solid fills and outlines.
5. **Click the world**. In Instant mode, *click-drag-release* and the shape fires. In Select mode, *click → click* (start, end). In Confirm mode, you get a preview before commit. In Stamp mode, you stamp the same shape at multiple positions.

That's the whole loop. Every other family works the same way, with family-specific options on the panel.

---

## Selection modes (4 modes per family)

Right-click any wand **in your inventory** (not while held in hand) to cycle to the next mode. The wand's icon changes per mode.

| Mode | Clicks | Best for |
|------|--------|----------|
| **Instant** | 1 (click-drag-release) | Quick edits — drop a 5×5 block, draw a short wire run |
| **Select** | 2 (click start → click end) | Precise rectangles where you want to set both corners exactly |
| **Confirm** | 3 (click start → click end → click to commit) | Big operations you want to *preview* before committing |
| **Stamp** | 4+ (click start → click end → confirm → stamp, stamp, stamp…) | Repeated shapes — torch every floor of a base; mold a window template once and stamp it 12 times |

Right-clicking **while you're mid-selection** *cancels* that selection (and a colour-coded "Cancelled" splash floats up). Right-clicking **while not selecting** opens the wand's settings panel. The two right-clicks never conflict.

For a deeper dive into mode behaviours, see [Common Concepts → Selection Modes](CommonConcepts.md#selection-modes).

---

## Shapes and slicing

Every family shares the same 8-shape menu:

| Shape | Notes |
|-------|-------|
| **Rectangle** | Fast bounding box |
| **Ellipse** | Math.Sqrt rasterisation — pixel-perfect on any size |
| **Diamond** | Manhattan-distance lattice — useful for symmetry |
| **Triangle** | Scanline fill |
| **Elbow** | Right-angle L joint — wand of Wiring's default |
| **Cardinal Line** | 8-direction snap with a circular brush |
| **Straight Line** | Free-angle Bresenham line with variable thickness |
| **Mold** | Your sculpted custom shape (see Molding) |

Each shape supports:

- **Filled / Hollow** with **Thickness** (0–50, controls outline width)
- **Slice Mode** (Full / HalfHorizontal / HalfVertical) — gives you half-domes, half-rings, etc.
- **Equal Dimensions** — locks W = H so you get perfect circles, squares, equilateral diamonds
- **Connect Diameter** — closes the cut edge in sliced Hollow shapes
- **Invert Selection** — uses the bounding-box's *negative space* instead of the shape itself

---

## Keybinds (the 5 you need)

The mod ships with 5 default keybinds, all rebindable in *Controls → Keybindings → World Shaping Wands*.

| Default key | Action |
|-------------|--------|
| `]` | Increase outline thickness |
| `[` | Decrease outline thickness |
| `.` | Toggle the held wand's settings panel |
| `Backspace` | Undo the last wand operation |
| `;` | Toggle "suppress drops" (no item drops on dismantle) |

Plus:

- **Right-click in inventory** → cycle selection mode (Instant ↔ Select ↔ Confirm ↔ Stamp)
- **Right-click while held, not selecting** → open settings panel
- **Right-click while held, mid-selection** → cancel selection
- **Shift + Right-click** while holding the **Torch Wheel Wand** → kill all your active wheels (panic button)

---

## Five things you'll want to know early

1. **Undo is your safety net.** Every operation goes onto a per-player undo stack (default 20 deep). Press `Backspace` to revert; type `/undoinfo` in chat for a cost preview before committing the undo. Across families: dismantled tiles come back, wires come back, paint reverts, etc.

2. **Safekeeping is the "lock" mechanism.** Once you've built something you don't want to accidentally lose, sweep it with the **Wand of Safekeeping (Protect)** mode. Every other wand will refuse to touch protected positions until you Unprotect. It survives world saves.

3. **Delimitation is the "scope" mechanism.** A **Wand of Delimitation** area clips *every other wand* to a region you define. Useful for "edit only this room" workflows without measuring twice.

4. **Molding gives you custom shapes.** Sculpt any shape with the Wand of Molding's canvas, and that shape automatically appears as **Mold** in the shape dropdown of every other wand — wire it, paint it, dig it, replicate it across a base.

5. **Multiplayer is built-in.** Every operation is server-authoritative — packet sync, undo, safekeeping, fluid mixing, all of it. There's no extra setup; install the mod on the server and the clients and it works.

---

## Configuration at a glance

The mod ships with **10 separate config files** (`ModConfig` panels), so options are easier to find. Server-side configs control gameplay rules; client-side configs control your visuals and sound.

| Config | Side | What it covers |
|--------|------|----------------|
| **ResourcesConfig** | Server | Infinite tiles / walls / wires / actuators / grass seeds, drop suppression |
| **SandboxConfig** | Server | Pickaxe-power bypass, demon altar / delicate tile guards, vacuum, locked-key behaviour |
| **CarefreeConfig** | Server | Carefree preset (sandbox bundle) + Void Everything availability |
| **LimitsConfig** | Server | Max operation size, selection caps (small / big), thickness cap |
| **PerformanceConfig** | Server | Progressive (batched) processing, batch size, frame delay |
| **StampConfig** | Server | Stamp repeat intervals (general + Coating-specific) |
| **TorchWheelConfig** | Server | All Torch Wheel + Wand-of-Torches tuning (spacing, smart-torch, biome gates) |
| **OverlayConfig** | Client | Selection overlay colours, render mode, opacity |
| **CanvasOverlayConfig** | Client | Molding + Delimitation overlay colours, dimming alpha |
| **PreferencesConfig** | Client | Wand sounds, lore tooltips, infinite-resource per-type overrides |

Don't worry about touching them on day one — the defaults are tuned for normal play.

---

## Where to go next

- **[Common Concepts](CommonConcepts.md)** — the shared vocabulary (selection modes, overlays, tri-state buttons, delimitation, canvas, undo).
- **[Wand of Building](Wands/WandOfBuilding.md)** — the most-used wand; once you've read its guide, the others feel familiar.
- **[Wand of Molding](Wands/WandOfMolding.md)** — the most powerful feature in the mod; turns every other wand into a stamp tool.
- **[Wand of Delimitation](Wands/WandOfDelimitation.md)** — the second most powerful feature; bounds every other wand to a region.

Welcome aboard. Build something stupendous.
