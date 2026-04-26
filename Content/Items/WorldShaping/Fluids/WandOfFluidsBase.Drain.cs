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
// Drain (selective + adjacent settle) — partial of WandOfFluidsBase. See WandOfFluidsBase.cs for the class header & overrides.
public abstract partial class WandOfFluidsBase
{
    // ── Drain ────────────────────────────────────────────────────────

    /// <summary>
    /// Drains all liquids within the selection shape.
    /// Always available regardless of liquid type or achievement gates.
    /// Processes top-down (lowest Y first in Terraria coords) to drain from top.
    /// </summary>
    private static void ExecuteDrain(Player player, List<Point> positions, WandOfFluidsSettings settings)
    {
        // Sort by Y ascending (topmost first — drain from top down)
        var sorted = positions.OrderBy(p => p.Y).ToList();

        int drained = 0;

        foreach (var pos in sorted)
        {
            int x = pos.X;
            int y = pos.Y;

            if (!WorldGen.InWorld(x, y, 1))
                continue;

            var tile = Main.tile[x, y];

            if (tile.LiquidAmount == 0)
                continue;

            // Selective drain: skip liquids that don't match the chosen type
            if (settings.SelectiveDrain && tile.LiquidType != (int)settings.LiquidType)
                continue;

            tile.LiquidAmount = 0;
            tile.LiquidType = LiquidID.Water; // Reset to vanilla default for empty

            if (Main.netMode == NetmodeID.Server)
                NetMessage.sendWater(x, y);
            else
                Liquid.AddWater(x, y);

            drained++;
        }

        // Settle adjacent liquids: notify Terraria's liquid engine about tiles
        // bordering the drained area so gravity/flow resolves naturally.
        // Without this, liquids adjacent to the bbox edges remain floating.
        if (drained > 0)
        {
            var shell = ComputeBubbleShell(sorted);
            foreach (var adj in shell)
            {
                if (!WorldGen.InWorld(adj.X, adj.Y, 1)) continue;
                var adjTile = Main.tile[adj.X, adj.Y];
                if (adjTile.LiquidAmount > 0)
                {
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.sendWater(adj.X, adj.Y);
                    else
                        Liquid.AddWater(adj.X, adj.Y);
                }
            }
        }

        Main.NewText(Get("FluidsDrained", drained),
            WandColors.MsgFluids);
    }
}
