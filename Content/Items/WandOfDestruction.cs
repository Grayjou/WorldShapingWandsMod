using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Content.Items;

// Base destruction logic shared by all modes
public abstract class WandOfDestructionBase : BaseCyclingWand
{
    public override string WandBaseName => "Wand of Destruction";
    
    protected abstract bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile);

    public override bool? UseItem(Player player)
    {
        // Don't do anything if the mouse is over UI
        if (Main.LocalPlayer.mouseInterface)
            return false;

        var wandPlayer = player.GetModPlayer<WandPlayer>();

        if (WandSelectionMode != SelectionMode.OneClick && !wandPlayer.TryConsumeFreshLeftClick())
            return false;

        Point mouseTile = GeometryHelper.WorldToTile(Main.MouseWorld);

        return HandleUseItem(player, wandPlayer, mouseTile);
    }

    // NEW: Base HoldItem handles right-click cancellation for all modes
    public override void HoldItem(Player player)
    {
        var wandPlayer = player.GetModPlayer<WandPlayer>();
        
        // Cancel on right-click while selecting
        if (wandPlayer.Selection.IsActive && Main.mouseRight && Main.mouseRightRelease)
        {
            CancelSelection(wandPlayer);
            Main.mouseRightRelease = false; // Consume the click to prevent other actions
        }
    }

    // NEW: Virtual method so derived classes can add behavior on cancel
    protected virtual void CancelSelection(WandPlayer wandPlayer)
    {
        wandPlayer.CancelSelection(WandColors.CancelDestruction, wandPlayer.DestructionSettings.Shape);
    }

    protected void ExecuteDestruction(Player player, WandPlayer wandPlayer)
    {
        var settings = wandPlayer.DestructionSettings;
        var selection = wandPlayer.Selection;
        
        var context = new ShapeContext(
            selection.StartTile,
            selection.EndTile,
            settings.Shape.FillMode,
            settings.Shape.Thickness,
            HorizontalBias.None,
            VerticalBias.None,
            selection.VerticalFirst
        );

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
        var undoMgr = player.GetModPlayer<UndoManager>();
        var action = undoMgr.BeginAction("Destruction");

        int destroyedTiles = 0, skipped = 0;
        var snapshottedTiles = new HashSet<Point>();

        foreach (Point tile in tileSet.Tiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;

            bool willDestroyTile = settings.DestroyTiles
                && Main.tile[tile.X, tile.Y].HasTile
                && player.HasEnoughPickPowerToHurtTile(tile.X, tile.Y)
                && WorldGen.CanKillTile(tile.X, tile.Y);

            bool willDestroyWall = settings.DestroyWalls
                && Main.tile[tile.X, tile.Y].WallType > WallID.None;

            if (!willDestroyTile && !willDestroyWall) { skipped++; continue; }

            // Take a single snapshot per tile before any modification
            if (!snapshottedTiles.Contains(tile))
            {
                action.AddSnapshot(tile);
                snapshottedTiles.Add(tile);
            }

            if (willDestroyTile)
            {
                WorldGen.KillTile(tile.X, tile.Y, fail: false, effectOnly: false, noItem: settings.SuppressDrops);
                destroyedTiles++;

                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendTileSquare(-1, tile.X, tile.Y);
            }

            if (willDestroyWall)
            {
                WorldGen.KillWall(tile.X, tile.Y);

                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendTileSquare(-1, tile.X, tile.Y);
            }
        }

        if (snapshottedTiles.Count > 0)
        {
            undoMgr.CommitAction(action);
            Main.NewText($"Destroyed {destroyedTiles} tile(s)" + 
                (skipped > 0 ? $", {skipped} skipped" : ""),
                Color.OrangeRed);
        }
        else
        {
            Main.NewText("No tiles were destroyed.", Color.Gray);
        }
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player)
    {
        // This still handles right-click when NOT channeling
        if (player.altFunctionUse == 2)
        {
            var wandPlayer = player.GetModPlayer<WandPlayer>();
            if (wandPlayer.Selection.IsActive)
            {
                CancelSelection(wandPlayer);
            }
            else
            {
                // Only toggle UI on the client
                if (Main.myPlayer == player.whoAmI)
                {
                    ModContent.GetInstance<WandUISystem>().ToggleUIForCurrentWand();
                }
            }
            return false;
        }
        return true;
    }
}

// OneClick Mode - Click and drag
public class WandOfDestructionInstant : WandOfDestructionBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
    public override Color ModeColor => new Color(255, 80, 80); // Red — Instant (dangerous)
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfDestructionSelect>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true; // Enable channeling for drag
    }

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        // All logic handled in HoldItem for instant/drag mode
        return false;
    }

    public override void HoldItem(Player player)
    {
        base.HoldItem(player);

        if (Main.myPlayer != player.whoAmI)
            return;

        var wandPlayer = player.GetModPlayer<WandPlayer>();
        Point mouseTile = GeometryHelper.WorldToTile(Main.MouseWorld);

        if (Main.mouseLeft)
        {
            // Don't start selection if mouse is over UI
            if (Main.LocalPlayer.mouseInterface)
                return;

            // Don't restart selection immediately after cancellation
            if (!wandPlayer.CanStartNewSelection())
                return;

            if (!wandPlayer.Selection.IsActive)
            {
                bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                                Math.Abs(Main.MouseWorld.X - player.Center.X);
                wandPlayer.StartSelection(mouseTile, vertical);
            }
            wandPlayer.UpdateSelection(mouseTile);
        }
        else if (wandPlayer.Selection.IsActive)
        {
            // Mouse released - execute only if this wand started the selection
            if (wandPlayer.IsSelectionOwnedByCurrentItem())
            {
                ExecuteDestruction(player, wandPlayer);
            }
            wandPlayer.ClearSelection();
        }
    }
}

// TwoClick Mode - Click start, click end
public class WandOfDestructionSelect : WandOfDestructionBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
    public override Color ModeColor => new Color(255, 255, 80); // Yellow — Select (caution)
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfDestructionConfirm>();

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            // First click - start selection
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText("Selection started. Click again to confirm area.", Color.Cyan);
            return false; // Don't consume the wand
        }
        else
        {
            // Second click - execute
            wandPlayer.UpdateSelection(mouseTile);
            ExecuteDestruction(player, wandPlayer);
            wandPlayer.ClearSelection();
            return false; // Don't consume the wand
        }
    }
}

// ThreeClick Mode - Click start, click end, click confirm
public class WandOfDestructionConfirm : WandOfDestructionBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => new Color(80, 255, 80); // Green — Confirm (safe)
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfDestructionStamp>();

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            // First click - start selection
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText("Selection started. Click to set end point.", Color.Cyan);
            return false; // Don't consume the wand
        }
        else if (!wandPlayer.Selection.IsLocked)
        {
            // Second click - lock selection, await confirmation
            wandPlayer.UpdateSelection(mouseTile);
            wandPlayer.LockSelection();  // LOCK IT HERE
            Main.NewText("Click again to confirm, or right-click to cancel.", Color.Yellow);
            return false; // Don't consume the wand
        }
        else
        {
            // Third click - execute
            ExecuteDestruction(player, wandPlayer);
            wandPlayer.ClearSelection();
            return false; // Don't consume the wand
        }
    }

    public override bool CanUseItem(Player player)
    {
        return base.CanUseItem(player);
    }
}
