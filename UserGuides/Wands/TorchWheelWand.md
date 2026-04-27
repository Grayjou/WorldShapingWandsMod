# Torch Wheel Wand — User Guide

> **Wand Family**: Torch Wheel (standalone — not part of the cycling-wand family)
> **Variants**: Solid (tile-tracing) · Platform (platform-tracing)
> **Cycle Key**: Right-click the wand in **inventory** to swap between Solid and Platform
> **Status**: Solid is fully implemented. Platform tracing is **planned** — holding the
> Platform wand currently shows an informational message and does not fire.

---

## Overview

The Torch Wheel Wand is a **fire-and-forget projectile**. Left-click and a glowing wheel
flies out, **follows the wall surface** it lands on, and drops torches at the configured
spacing as it travels — like a tiny lamplighter on a unicycle. It's the answer to "how do
I light this 200-tile cave tunnel without 200 clicks?".

Unlike every other WSW wand, the Torch Wheel is **standalone**: it doesn't extend
`BaseCyclingWand`, has **no settings panel**, no shape selection, and no four-mode
inventory cycle. There are just two items — Solid (traces along solid tiles) and Platform
(traces along platforms) — and right-clicking in inventory swaps between them.

Use it whenever you need to:

- Light a long **cave tunnel** by tossing one wheel and walking away.
- Trim torches along a **mountain ridge** without measuring spacing by eye.
- Cover a **multi-floor base** by throwing one wheel per floor.
- Light along **platform walkways** (when Platform variant ships).

For lighting *areas* rather than *paths*, see the sister tool:
**[Wand of Torches](WandOfTorches.md)**.

---

## How It Works

1. **Left-click** with the wand selected. A `FlyingTorchWheel` projectile flies in the
   direction of your cursor.
2. The projectile **looks for a wall**. When it touches a solid tile (Solid variant) or
   platform (Platform variant), it sticks and starts **wheeling along the surface**.
3. As it rolls, it drops a torch every **N tiles** (configurable spacing).
4. It stops when it hits a corner it can't navigate, runs out of torches, or reaches the
   server-configured **maximum-tiles** limit.
5. Then it **back-tracks** a configurable number of steps to fill in the gap left by the
   sliding-window warm-up near the first torch — so even the very start of the run is lit.
6. Done. No clicks needed during the run.

> **Cursor preview**: While holding the wand, the cursor shows the **first torch type**
> found in your inventory — so you can see at a glance which torch the next throw will use.

---

## The Two Variants

| Variant | Surface | Status | Inventory swap |
|---------|---------|--------|----------------|
| **Torch Wheel Wand (Solid)** | Solid tiles (block walls, ceilings, floors) | ✅ Implemented | Right-click in inventory → Platform |
| **Torch Wheel Wand (Platform)** | Platforms only | 🚧 Planned (left-click currently shows info text) | Right-click in inventory → Solid |

The two are **separate items** (not a mode flag) for multiplayer reliability — Terraria
syncs `Item` instances automatically, but custom mode fields on `ModItem` do not. The
right-click cycle is the same proven pattern used by every cycling wand.

---

## Controls

| Action | Effect |
|--------|--------|
| **Left-click** (in world) | Throw a flying torch wheel in the cursor direction |
| **Shift + Right-click** (while holding wand) | **Kill all active Torch Wheel projectiles** owned by you (Solid + Platform, flying + wheeling) — chat confirms |
| **Right-click** (in inventory) | Cycle Solid ↔ Platform |

> The **kill switch** is your panic button. If a wheel goes somewhere you didn't intend
> (around an unexpected corner, into a long pit), Shift + Right-click stops every active
> wheel of yours immediately.

---

## Spacing & Limits (Server Config)

All Torch Wheel tuning lives on `TorchWheelConfig` (server-authoritative).

| Setting | Default | Range | Effect |
|---------|---------|-------|--------|
| **TorchWheelSpacingS** | 12 | 1–50 | Spacing for the **Solid** variant (tiles between torches) |
| **TorchWheelSpacingD** | 8 | 1–50 | Spacing for the **Platform** variant |
| **TorchWheelMaxTorches** | 50 | 1–500 | Hard cap on torches dropped per throw |
| **TorchWheelMaxTiles** | 150 | 50–2000 | Hard cap on tiles the projectile traverses |
| **TorchWheelMaxBacktrackSteps** | 20 | -1–500 | How far the projectile back-traces after the forward pass to fill the warm-up gap. `-1` = unlimited (capped internally at MaxTiles) |

> **Why a backtrack pass?** The projectile needs a few tiles of "warm-up" before its
> sliding-window spacing logic can decide where the first torch goes. The backtrack pass
> walks back along the same surface and fills any gap left in front of that first torch.

---

## Behaviour Toggles (Server Config)

| Setting | Default | Effect |
|---------|---------|--------|
| **TorchWheelFriendly** | OFF | When ON, the wheel can damage critters and break pots/plants on contact. OFF makes it purely passive — recommended for build sessions. |

---

## Smart Torch Selection (Server Config)

The Torch Wheel can adapt the torch type per-cell based on the environment.

| Setting | Default | Effect |
|---------|---------|--------|
| **UnderwaterTorchLookup** | ON | If your selected torch isn't waterproof and the wheel is underwater, search inventory for a waterproof torch. If OFF, underwater positions are skipped. |
| **AutoWaterproofTorches** | OFF | If no waterproof torch is found, **convert** a regular torch into a waterproof one on the fly. |
| **OceanWaterproofTorch** | CoralTorch | Which waterproof torch the auto-converter uses in Ocean biome. |
| **NonOceanWaterproofTorch** | EvilTorch | Which waterproof torch the auto-converter uses elsewhere underwater. |
| **SmartBiomeTorchLookup** | ON | If you carry the actual biome torch in inventory, the wand consumes it directly instead of converting a regular torch — preserves your collected biome stacks. |
| **EvilTorchRequiresHardmode** | ON | Block evil-biome torch (Ichor / Cursed) conversions before Hardmode. |
| **SubstituteCoralTorchPreHardmode** | OFF | When the Hardmode gate above blocks an evil torch, substitute a Coral Torch instead. |

These are the same configs honoured by the **Wand of Torches**, so torch behaviour is
consistent across both tools.

---

## Visual & Safety (Server Config)

| Setting | Default | Effect |
|---------|---------|--------|
| **SmoothVisualPath** | ON | Smooth velocity-based trajectory with gentle bobbing — an organic "firefly" feel. OFF uses legacy lerp smoothing that snaps closer to the logical tile each frame. |
| **AnimateTorchWheel** | ON | Animates the projectile sprite, cycling colour from grey to gold (the "wheel of fire" look). OFF uses a static sprite with no flicker. |
| **PhotosensitivityWarning** | ON | Show a chat warning on login when **Spacing (Solid)** is set below 6, advising about potential photosensitivity issues from high-frequency flicker. Suppressed when AnimateTorchWheel is OFF. |

> The photosensitivity warning is on by default because tightly-spaced torches plus a
> flickering projectile sprite can produce rapid bright-dark cycles. We chose to surface
> the warning rather than silently ship that risk.

---

## Multiplayer Behaviour

| Aspect | Singleplayer | Multiplayer |
|--------|--------------|-------------|
| Projectile spawn | ✅ Local | ✅ Standard `Item.shoot` netcode |
| Surface tracing logic | ✅ Local | ✅ Owner client runs trace; placements sync via tile-set packets |
| Torch consumption | ✅ Inventory | ✅ Owner client deducts; server validates |
| Kill-switch (Shift + Right-click) | ✅ | ✅ Local — kills all wheels owned by the calling player |
| Friendly damage toggle | ✅ | ✅ Server-side |

The Torch Wheel doesn't go through `WandPacketHandler` (it's a projectile, not a tile-set
operation), so it doesn't honour the unified cooldown rate-limit. The hard caps
(MaxTorches, MaxTiles, MaxBacktrackSteps) provide the equivalent abuse-prevention.

---

## Interactions With Other Systems

### Wand of Safekeeping

Protected positions are honoured — the wheel **places no torch** at any tile that is
protected as a tile, and **on no surface** that is protected as a wall (Platform variant).
The wheel itself still passes over them; it just doesn't drop a torch there.

### Wand of Delimitation

The wheel does **not** currently honour Delimitation areas — it's a free-flying
projectile. If you want path-style lighting confined to a region, prefer the **Wand of
Torches** with a custom shape inside your Delimitation area.

### Wand of Coating — Echo

Torches dropped by the wheel are **never** Echo-coated. To Echo-coat a wheeled torch
trail, run a **Wand of Coating** pass over the area afterwards (or use the **Wand of
Torches** with `Echo Coat = Apply` if you need the coating done in the same pass).

### Vanilla Torch God's Favour

If you have the **Torch God's Favour** active, the wheel uses biome-appropriate torches
automatically (subject to the smart-lookup configs above). No toggle is required.

---

## Crafting

| Variant | Recipe (at Work Bench) |
|---------|------------------------|
| **Torch Wheel Wand (Solid / Platform)** | 10 Wood + 5 Torch + 1 Fallen Star |

The recipe is intentionally cheap — both variants share it. Right-click in inventory to
cycle between them once crafted.

---

## Tips & Tricks

- **Cave tunnel**: Stand at the cave mouth, face inwards along the wall, left-click.
  Walk through your now-lit cave.
- **Ceiling lighting**: Aim **upwards** at a ceiling wall — the wheel sticks to the
  ceiling and trails torches across it.
- **Quick "stop!"**: Shift + Right-click the moment a wheel goes off-course. Every active
  wheel of yours dies instantly.
- **Multi-floor lighting**: Throw one wheel per floor; each one runs independently and
  the torch caps apply per wheel.
- **Spacing audit**: Watch the wheel for a few seconds — the spacing it produces tells
  you whether to adjust `TorchWheelSpacingS` (Solid) or `TorchWheelSpacingD` (Platform)
  before doing the next 200-tile run.
- **Photosensitivity**: If `AnimateTorchWheel` ON + tight spacing causes any discomfort,
  set spacing ≥ 6 (the warning threshold) or turn AnimateTorchWheel OFF.
- **Pair with Wand of Torches**: Wheel for *paths*, Torches wand for *areas*. Both honour
  the same smart-lookup / waterproof / biome configs, so behaviour is consistent.
- **Carefree use**: The wheel never harvests pots or critters with `TorchWheelFriendly`
  OFF (default), so it's safe to throw through a pre-cleared dungeon area without losing
  loot.

---

## See Also

- **[Wand of Torches](WandOfTorches.md)** — Lattice-based torch coverage for areas.
- **[Wand of Coating](WandOfCoating.md)** — Echo-coat the trail after the wheel runs.
- **[Wand of Safekeeping](WandOfSafekeeping.md)** — Protect a wheeled trail from accidental edits.
- **[Wand of Building](WandOfBuilding.md)** — Build the chassis the wheel will light.
