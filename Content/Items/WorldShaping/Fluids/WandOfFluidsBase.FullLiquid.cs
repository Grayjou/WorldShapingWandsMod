using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Algorithms;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Content.Projectiles;
using static WorldShapingWandsMod.Common.Utilities.Msg;
#if DEBUG
using WorldShapingWandsMod.Common.Debug;
#endif

namespace WorldShapingWandsMod.Content.Items;


/// <summary>
/// Abstract base class for all Wand of Fluids variants.
/// Handles liquid placement, draining, rain fill, and pocket fill operations.
/// Four concrete subclasses (Instant, Select, Confirm, Stamp) provide mode behavior.
/// </summary>
// Full Liquid + Mix Liquids — partial of WandOfFluidsBase. See WandOfFluidsBase.cs for the class header & overrides.
public abstract partial class WandOfFluidsBase
{
    // ── Full Liquid Fill ─────────────────────────────────────────────

    /// <summary>
    /// Places liquid in every non-solid tile within the selection shape.
    /// WARNING: Can cause massive physics cascades on large open selections.
    /// Should use progressive fill for large areas.
    /// </summary>
    private static void ExecuteFullLiquid(Player player, List<Point> positions, WandOfFluidsSettings settings)
    {
        int placed = 0;
        byte liquidType = (byte)settings.LiquidType;

        foreach (var pos in positions)
        {
            int x = pos.X;
            int y = pos.Y;

            if (!WorldGen.InWorld(x, y, 1))
                continue;

            var tile = Main.tile[x, y];

            // Skip solid tiles — liquid can only go in non-solid spaces
            if (tile.HasTile && WorldGen.SolidTile(tile))
                continue;

            // S5 2026-04-23 (W-2, Cavendish S7 Diagnosis_FullLiquidPourReplacingLava.md):
            // Skip tiles that already contain a DIFFERENT liquid unless the user has
            // explicitly opted in to destructive replacement via the Overwrite Liquids
            // toggle. Mirrors the guard in ExecuteMixLiquids' non-mixing branch.
            // Empty tiles and same-liquid tiles (top-up) are always filled.
            //   • OverwriteLiquids = false (default): preservation semantics — existing
            //     different liquid blocks the pour. The user picks Mix Liquids if they
            //     want vanilla-mixing conversion (water+lava→obsidian, etc.).
            //   • OverwriteLiquids = true: destructive replace — pour clears whatever
            //     was there and refills with the chosen liquid. New in S5.
            if (!settings.OverwriteLiquids
                && tile.LiquidAmount > 0
                && tile.LiquidType != liquidType)
                continue;

            tile.LiquidType = liquidType;
            tile.LiquidAmount = 255;

            if (Main.netMode == NetmodeID.Server)
                NetMessage.sendWater(x, y);
            else
                Liquid.AddWater(x, y);

            placed++;
        }

        Main.NewText(Get("FluidsFilled", placed, settings.LiquidType),
            WandColors.MsgFluids);
    }

    // ── Mix Liquids ──────────────────────────────────────────────────

    /// <summary>
    /// Liquid mixing conversion table.
    /// Given (existing liquid in tile, liquid the wand is placing), returns
    /// the resulting tile type, or -1 if no mixing applies.
    /// 
    /// Vanilla rules:
    ///   honey  + water  → HoneyBlock  (229)
    ///   lava   + water  → Obsidian    (56)
    ///   lava   + honey  → CrispyHoneyBlock (230)
    ///   shimmer + any   → ShimmerBlock (659)
    ///   any   + shimmer → ShimmerBlock (659)
    /// </summary>
    private static int GetMixingResultTile(byte existingLiquid, byte placingLiquid)
    {
        // Shimmer wins against everything
        if (existingLiquid == LiquidID.Shimmer || placingLiquid == LiquidID.Shimmer)
            return TileID.ShimmerBlock;

        // Normalize the pair to an order-independent check
        byte lo = Math.Min(existingLiquid, placingLiquid);
        byte hi = Math.Max(existingLiquid, placingLiquid);

        // Water(0) + Lava(1) → Obsidian
        if (lo == LiquidID.Water && hi == LiquidID.Lava)
            return TileID.Obsidian;

        // Water(0) + Honey(2) → HoneyBlock
        if (lo == LiquidID.Water && hi == LiquidID.Honey)
            return TileID.HoneyBlock;

        // Lava(1) + Honey(2) → CrispyHoneyBlock
        if (lo == LiquidID.Lava && hi == LiquidID.Honey)
            return TileID.CrispyHoneyBlock;

        return -1; // No mixing rule — same liquid or unknown combo
    }

    /// <summary>
    /// Returns the appropriate mixing sound for a given tile result.
    /// Uses vanilla splash/sizzle sounds that approximate the mixing feel.
    /// </summary>
    private static SoundStyle? GetMixingSound(int resultTileType)
    {
        return resultTileType switch
        {
            TileID.Obsidian         => SoundID.Item45 with { Volume = 0.6f },  // lava sizzle
            TileID.HoneyBlock       => SoundID.Item85 with { Volume = 0.6f },  // honey splash
            TileID.CrispyHoneyBlock => SoundID.Item45 with { Volume = 0.5f },  // lava + honey sizzle
            TileID.ShimmerBlock     => SoundID.Item176 with { Volume = 0.6f }, // shimmer sparkle
            _                       => null
        };
    }

    /// <summary>
    /// Mix Liquids mode: iterates the selection and for each tile that already
    /// contains a different liquid, replaces it with the vanilla mixing result tile.
    /// Tiles that are empty or contain the same liquid are filled normally.
    ///
    /// Sound handling: each distinct conversion type that occurs plays its sound
    /// once at the end, resulting in up to 3 overlapping sounds for a mixed-liquid
    /// area. This mirrors vanilla behavior where mixing is instantaneous.
    /// </summary>
    private static void ExecuteMixLiquids(Player player, List<Point> positions, WandOfFluidsSettings settings)
    {
        int placed = 0;
        int mixed = 0;
        byte liquidType = (byte)settings.LiquidType;

        // Track which mixing results occurred so we can play sounds
        var soundsToPlay = new HashSet<int>();

        foreach (var pos in positions)
        {
            int x = pos.X;
            int y = pos.Y;

            if (!WorldGen.InWorld(x, y, 1))
                continue;

            var tile = Main.tile[x, y];

            // Skip solid tiles — liquid can only go in non-solid spaces
            if (tile.HasTile && WorldGen.SolidTile(tile))
                continue;

            // If tile has a different liquid, attempt mixing
            if (tile.LiquidAmount > 0 && tile.LiquidType != liquidType)
            {
                int resultTile = GetMixingResultTile((byte)tile.LiquidType, liquidType);

                if (resultTile >= 0)
                {
                    // Remove the liquid first
                    tile.LiquidAmount = 0;
                    tile.LiquidType = LiquidID.Water;

                    // Place the result tile
                    WorldGen.PlaceTile(x, y, (ushort)resultTile, mute: true, forced: true);

                    if (Main.netMode != NetmodeID.SinglePlayer)
                        NetMessage.SendTileSquare(-1, x, y, 1);

                    soundsToPlay.Add(resultTile);
                    mixed++;
                    continue;
                }
                // If no mixing rule applies (shouldn't happen), fall through to normal fill
            }

            // Normal fill: empty tile or same liquid
            if (tile.LiquidAmount == 0 || tile.LiquidType == liquidType)
            {
                tile.LiquidType = liquidType;
                tile.LiquidAmount = 255;

                if (Main.netMode == NetmodeID.Server)
                    NetMessage.sendWater(x, y);
                else
                    Liquid.AddWater(x, y);

                placed++;
            }
        }

        // Play mixing sounds — one per distinct conversion type, all at once
        foreach (int resultType in soundsToPlay)
        {
            var sound = GetMixingSound(resultType);
            if (sound.HasValue)
                SoundEngine.PlaySound(sound.Value, player.Center);
        }

        if (mixed > 0 && placed > 0)
            Main.NewText(Get("FluidsMixed", mixed, placed, settings.LiquidType), WandColors.MsgFluids);
        else if (mixed > 0)
            Main.NewText(Get("FluidsMixedOnly", mixed), WandColors.MsgFluids);
        else
            Main.NewText(Get("FluidsFilled", placed, settings.LiquidType), WandColors.MsgFluids);
    }
}
