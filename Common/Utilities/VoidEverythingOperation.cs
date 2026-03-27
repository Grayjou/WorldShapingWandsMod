using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Systems;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Networking;
using WorldShapingWandsMod.Common.Networking.Handlers;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Common.Utilities;

/// <summary>
/// Implements the "Void Everything" operation: clears tiles, walls, liquids,
/// wires, and actuators in the current selection. Only available when
/// Carefree Mode is enabled.
/// </summary>
public static class VoidEverythingOperation
{
    /// <summary>
    /// Executes the Void Everything operation using the player's current
    /// dismantling selection and shape settings.
    /// </summary>
    public static void Execute(Player player)
    {
        if (player == null) return;

        var config = ModContent.GetInstance<WandServerConfig>();
        if (config == null || !config.EnableCarefreeMode)
        {
            Main.NewText(Get("VoidRequiresCarefree"), Color.Gray);
            return;
        }

        var wandPlayer = player.GetModPlayer<WandPlayer>();
        if (wandPlayer == null) return;

        var settings = wandPlayer.DismantlingSettings;
        var selection = wandPlayer.GetVisualSelection();

        if (!selection.IsActive)
        {
            Main.NewText(Get("NoSelectionActive"), Color.Gray);
            return;
        }

        // In multiplayer, send a packet to the server (no liquid clearing in MP)
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var mpShape = settings.Shape;
            VoidEverythingPacketHandler.SendVoidEverythingOperation(
                selection.StartTile, selection.EndTile,
                mpShape.Shape, mpShape.FillMode,
                mpShape.Thickness, mpShape.EqualDimensions,
                selection.VerticalFirst, player.whoAmI,
                mpShape.Slice, mpShape.ConnectDiameter,
                mpShape.InvertSelection);
            return;
        }

        var context = settings.Shape.ToShapeContext(
            selection.StartTile, selection.EndTile, selection.VerticalFirst);

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
        var tiles = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);

        var undoMgr = player.GetModPlayer<UndoManager>();
        var action = undoMgr.BeginAction("Void Everything");
        var snapshottedTiles = new HashSet<Point>();
        var affectedPositions = new List<Point>();

        // Sort top-to-bottom so multi-tile objects collapse correctly
        var sortedTiles = (Point[])tiles.Clone();
        Array.Sort(sortedTiles, (a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

        // Suppress all drops and effects
        bool wasGen = WorldGen.gen;
        WorldGen.gen = true;

        int voided = 0;
        int skippedProtected = 0;

        try
        {
            foreach (Point tile in sortedTiles)
            {
                if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
                if (SafekeepingSystem.IsProtected(tile.X, tile.Y))
                {
                    skippedProtected++;
                    continue;
                }

                var tileData = Main.tile[tile.X, tile.Y];

                // Check if there's anything to void at this position
                bool hasTile = tileData.HasTile;
                bool hasWall = tileData.WallType > WallID.None;
                bool hasLiquid = tileData.LiquidAmount > 0;
                bool hasWire = tileData.RedWire || tileData.BlueWire
                            || tileData.GreenWire || tileData.YellowWire;
                bool hasActuator = tileData.HasActuator;

                if (!hasTile && !hasWall && !hasLiquid && !hasWire && !hasActuator)
                    continue;

                // Snapshot before modification
                if (!snapshottedTiles.Contains(tile))
                {
                    action.AddSnapshot(tile);
                    snapshottedTiles.Add(tile);
                }

                // === DESTROY TILE ===
                if (hasTile)
                {
                    WorldGen.KillTile(tile.X, tile.Y, noItem: true);
                }

                // === DESTROY WALL ===
                if (hasWall)
                {
                    WorldGen.KillWall(tile.X, tile.Y);
                }

                // === CLEAR LIQUID ===
                if (hasLiquid)
                {
                    tileData.LiquidAmount = 0;
                    tileData.LiquidType = LiquidID.Water; // Reset type
                }

                // === CLEAR WIRES ===
                if (hasWire)
                {
                    tileData.RedWire = false;
                    tileData.BlueWire = false;
                    tileData.GreenWire = false;
                    tileData.YellowWire = false;
                }

                // === CLEAR ACTUATOR ===
                if (hasActuator)
                {
                    tileData.HasActuator = false;
                    tileData.IsActuated = false;
                }

                affectedPositions.Add(tile);
                voided++;
            }
        }
        finally
        {
            WorldGen.gen = wasGen;
        }

        if (voided == 0)
        {
            Main.NewText(Get("NothingToVoid"), Color.Gray);
            return;
        }

        // Finalize: frame update + network sync
        BulkTileOperations.FinalizeBatch(affectedPositions);

        // Commit undo
        undoMgr.CommitAction(action);

        // Settle liquids in the affected area
        var bounds = BulkTileOperations.ComputeBounds(affectedPositions);
        SettleLiquids(bounds);

        // Play sound
        var clientCfg = ModContent.GetInstance<WandClientConfig>();
        if (clientCfg?.EnableWandSounds == true)
            SoundEngine.PlaySound(SoundID.Tink, player.Center);

        // Report
        string msg = $"Voided {voided} tile(s)";
        if (skippedProtected > 0)
            msg += $", {skippedProtected} protected";
        Main.NewText(msg, Color.OrangeRed);
    }

    /// <summary>
    /// Triggers liquid settling for the affected bounds so water/lava/honey
    /// flows naturally after tile removal.
    /// </summary>
    private static void SettleLiquids(Rectangle bounds)
    {
        if (bounds.IsEmpty) return;

        // Expand slightly so adjacent liquids also settle
        int startX = Math.Max(0, bounds.X - 2);
        int startY = Math.Max(0, bounds.Y - 2);
        int endX = Math.Min(Main.maxTilesX - 1, bounds.X + bounds.Width + 1);
        int endY = Math.Min(Main.maxTilesY - 1, bounds.Y + bounds.Height + 1);

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                Liquid.AddWater(x, y);
            }
        }
    }
}