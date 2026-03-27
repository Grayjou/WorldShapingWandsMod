using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Systems;

namespace WorldShapingWandsMod.Common.Networking.Handlers;

/// <summary>
/// Handles multiplayer packet sending and receiving for coating operations.
/// Covers paint tile/wall, scrape paint, scrape moss, and harvest moss.
/// Extracted from the monolithic WandPacketHandler for maintainability.
/// </summary>
public static class CoatingPacketHandler
{
    /// <summary>
    /// Sends a coating operation packet from client to server.
    /// Packet format: common header (23) + CoatingMode(1) + paintColor(1) +
    ///   applyIlluminant(1) + ignoreIlluminant(1) +
    ///   applyEcho(1) + ignoreEcho(1) = 28 bytes total.
    /// </summary>
    public static void SendCoatingOperation(
        Point start, Point end,
        ShapeType shape, ShapeMode fillMode,
        int thickness, bool equalDimensions,
        bool verticalFirst, int playerWhoAmI,
        CoatingMode mode, byte paintColor,
        bool applyIlluminant, bool ignoreIlluminant,
        bool applyEcho, bool ignoreEcho,
        SliceMode slice = SliceMode.Full, bool connectDiameter = true,
        bool invertSelection = false, bool repaint = true)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = WorldShapingWandsMod.Instance.GetPacket();
        packet.Write((byte)WandPacketType.CoatingOperation);

        var header = new WandPacketHeader(
            start, end, shape, fillMode,
            thickness, equalDimensions,
            verticalFirst, playerWhoAmI,
            slice, connectDiameter, invertSelection
        );
        WandPacketHeaderIO.WriteCommonHeader(packet, header);

        // Coating-specific fields (6 bytes)
        packet.Write((byte)mode);
        packet.Write(paintColor);
        packet.Write(applyIlluminant);
        packet.Write(ignoreIlluminant);
        packet.Write(applyEcho);
        packet.Write(ignoreEcho);
        packet.Write(repaint);
        packet.Send();
    }

    /// <summary>
    /// Handles an incoming coating operation packet.
    /// On server: validates, executes server-side coating, syncs.
    /// </summary>
    internal static void HandleCoatingOperation(BinaryReader reader, int whoAmI)
    {
        var header = WandPacketHeaderIO.ReadCommonHeader(reader);

        // Coating-specific fields
        var mode = (CoatingMode)reader.ReadByte();
        byte paintColor = reader.ReadByte();
        bool applyIlluminant = reader.ReadBoolean();
        bool ignoreIlluminant = reader.ReadBoolean();
        bool applyEcho = reader.ReadBoolean();
        bool ignoreEcho = reader.ReadBoolean();
        bool repaint = reader.ReadBoolean();

        if (!PacketUtilities.ValidatePlayer(header.PlayerWhoAmI))
            return;

        if (Main.netMode == NetmodeID.Server)
        {
            header = PacketUtilities.EnforceDistanceCap(header);
            var tileSet = PacketUtilities.ComputeShapeTiles(header);

            ServerExecuteCoating(
                tileSet.Tiles, header.PlayerWhoAmI,
                mode, paintColor,
                applyIlluminant, ignoreIlluminant,
                applyEcho, ignoreEcho,
                repaint);
        }
    }

    /// <summary>
    /// Server-side coating execution.
    /// Applies paint/coating/scrape operations to tiles or walls in the shape,
    /// using the same logic as client-side WandOfCoatingBase.ApplyCoating.
    /// </summary>
    private static void ServerExecuteCoating(
        IEnumerable<Point> tiles,
        int playerWhoAmI,
        CoatingMode mode,
        byte paintColor,
        bool applyIlluminant, bool ignoreIlluminant,
        bool applyEcho, bool ignoreEcho,
        bool repaint)
    {
        /// <summary>IgnorePaintColor sentinel — same as WandOfCoatingBase.IgnorePaintColor.</summary>
        const byte IgnorePaintColor = 255;

        int changed = 0;

        foreach (Point tile in tiles)
        {
            int x = tile.X;
            int y = tile.Y;
            if (!WorldGen.InWorld(x, y, 1)) continue;
            if (SafekeepingSystem.IsProtected(x, y)) continue;

            bool wasChanged = false;

#pragma warning disable CS0618
            switch (mode)
            {
                case CoatingMode.PaintTile:
                    wasChanged = ServerApplyPaintTile(x, y, paintColor, IgnorePaintColor,
                        applyIlluminant, ignoreIlluminant, applyEcho, ignoreEcho, repaint);
                    break;
                case CoatingMode.PaintWall:
                    wasChanged = ServerApplyPaintWall(x, y, paintColor, IgnorePaintColor,
                        applyIlluminant, ignoreIlluminant, applyEcho, ignoreEcho, repaint);
                    break;
                case CoatingMode.ScrapePaint:
                    wasChanged = ServerApplyScrapePaint(x, y);
                    break;
                case CoatingMode.ScrapeMoss:
                    wasChanged = ServerApplyScrapeMoss(x, y);
                    break;
                case CoatingMode.HarvestMoss:
                    wasChanged = ServerApplyHarvestMoss(x, y);
                    break;
            }
#pragma warning restore CS0618

            if (wasChanged)
            {
                changed++;
                // SendTileSquare is still needed for moss operations (ScrapeMoss/HarvestMoss)
                // which change tile type. For paint/coating, the helpers now use broadCast:true
                // which sends dedicated MessageID 63/64 packets, but SendTileSquare provides
                // an extra safety sync for any tile-level state changes.
                NetMessage.SendTileSquare(-1, x, y, 1);
            }
        }

        WandPacketHandler.SendOperationResult(playerWhoAmI, WandPacketType.CoatingOperation, changed, true);
    }

    // ── Coating server helpers (mirror WandOfCoatingBase.Apply* methods) ──

    private static bool ServerApplyPaintTile(int x, int y, byte color, byte ignorePaintColor,
        bool applyIlluminant, bool ignoreIlluminant,
        bool applyEcho, bool ignoreEcho,
        bool repaint = true)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasTile) return false;

        bool changed = false;

        if (color != ignorePaintColor && tile.TileColor != color)
        {
            if (!repaint && tile.TileColor != PaintID.None)
            {
                // Tile already painted and repaint is off — skip paint but still apply coatings below
            }
            else
            {
                // broadCast: true → sends MessageID.PaintTile (63) which correctly
                // handles color=0 (paint removal). SendTileSquare (20) silently skips
                // paint bytes when color==0, leaving clients with stale paint.
                WorldGen.paintTile(x, y, color, true);
                changed = true;
            }
        }

        bool hasIlluminant = tile.IsTileFullbright;
        bool hasEcho = tile.IsTileInvisible;
        bool wantIlluminant = ignoreIlluminant ? hasIlluminant : applyIlluminant;
        bool wantEcho = ignoreEcho ? hasEcho : applyEcho;

        if (hasIlluminant != wantIlluminant || hasEcho != wantEcho)
        {
            if ((hasIlluminant && !wantIlluminant) || (hasEcho && !wantEcho))
                WorldGen.paintCoatTile(x, y, 0, true);
            if (wantIlluminant && !tile.IsTileFullbright)
                WorldGen.paintCoatTile(x, y, 1, true);
            if (wantEcho && !tile.IsTileInvisible)
                WorldGen.paintCoatTile(x, y, 2, true);
            changed = true;
        }

        return changed;
    }

    private static bool ServerApplyPaintWall(int x, int y, byte color, byte ignorePaintColor,
        bool applyIlluminant, bool ignoreIlluminant,
        bool applyEcho, bool ignoreEcho,
        bool repaint = true)
    {
        var tile = Main.tile[x, y];
        if (tile.WallType == WallID.None) return false;

        bool changed = false;

        if (color != ignorePaintColor && tile.WallColor != color)
        {
            if (!repaint && tile.WallColor != PaintID.None)
            {
                // Wall already painted and repaint is off — skip paint but still apply coatings below
            }
            else
            {
                // broadCast: true → sends MessageID.PaintWall (64) for correct sync.
                WorldGen.paintWall(x, y, color, true);
                changed = true;
            }
        }

        bool hasIlluminant = tile.IsWallFullbright;
        bool hasEcho = tile.IsWallInvisible;
        bool wantIlluminant = ignoreIlluminant ? hasIlluminant : applyIlluminant;
        bool wantEcho = ignoreEcho ? hasEcho : applyEcho;

        if (hasIlluminant != wantIlluminant || hasEcho != wantEcho)
        {
            if ((hasIlluminant && !wantIlluminant) || (hasEcho && !wantEcho))
                WorldGen.paintCoatWall(x, y, 0, true);
            if (wantIlluminant && !tile.IsWallFullbright)
                WorldGen.paintCoatWall(x, y, 1, true);
            if (wantEcho && !tile.IsWallInvisible)
                WorldGen.paintCoatWall(x, y, 2, true);
            changed = true;
        }

        return changed;
    }

    private static bool ServerApplyScrapePaint(int x, int y)
    {
        var tile = Main.tile[x, y];
        bool changed = false;

        if (tile.HasTile && tile.TileColor != PaintID.None)
        {
            WorldGen.paintTile(x, y, PaintID.None, true);
            changed = true;
        }
        if (tile.HasTile && (tile.IsTileFullbright || tile.IsTileInvisible))
        {
            WorldGen.paintCoatTile(x, y, 0, true);
            changed = true;
        }
        if (tile.WallType != WallID.None && tile.WallColor != PaintID.None)
        {
            WorldGen.paintWall(x, y, PaintID.None, true);
            changed = true;
        }
        if (tile.WallType != WallID.None && (tile.IsWallFullbright || tile.IsWallInvisible))
        {
            WorldGen.paintCoatWall(x, y, 0, true);
            changed = true;
        }

        return changed;
    }

    private static bool ServerApplyScrapeMoss(int x, int y)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasTile) return false;

        // Moss tiles are converted back to Stone by checking known moss→substrate mappings.
        // This mirrors WandOfCoatingBase.ApplyScrapeMoss but uses inline checks
        // since the server doesn't have access to the client-side dictionary.
        int tileType = tile.TileType;
        int substrate = -1;

        if (tileType == TileID.GreenMoss || tileType == TileID.BrownMoss ||
            tileType == TileID.RedMoss || tileType == TileID.BlueMoss ||
            tileType == TileID.PurpleMoss || tileType == TileID.LavaMoss ||
            tileType == TileID.ArgonMoss || tileType == TileID.KryptonMoss ||
            tileType == TileID.XenonMoss || tileType == TileID.VioletMoss ||
            tileType == TileID.RainbowMoss)
        {
            substrate = TileID.Stone;
        }

        if (substrate < 0) return false;

        Main.tile[x, y].TileType = (ushort)substrate;
        if (Main.tile[x, y].TileColor != PaintID.None)
            WorldGen.paintTile(x, y, PaintID.None, true);
        WorldGen.SquareTileFrame(x, y, true);

        return true;
    }

    private static bool ServerApplyHarvestMoss(int x, int y)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasTile) return false;
        if (tile.TileType != TileID.LongMoss) return false;

        WorldGen.KillTile(x, y);
        if (tile.HasTile) return false; // Tile wasn't actually killed

        return true;
    }
}
