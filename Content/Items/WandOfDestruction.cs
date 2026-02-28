using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
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
        var wandPlayer = player.GetModPlayer<WandPlayer>();
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
        wandPlayer.ClearSelection();
        Main.NewText("Selection cancelled.", Color.Yellow);
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

        foreach (Point tile in tileSet.Tiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;

            if (settings.DestroyTiles && Main.tile[tile.X, tile.Y].HasTile)
            {
                if (!player.HasEnoughPickPowerToHurtTile(tile.X, tile.Y))
                {
                    skipped++;
                    continue;
                }
                if (!WorldGen.CanKillTile(tile.X, tile.Y))
                {
                    skipped++;
                    continue;
                }

                action.AddSnapshot(tile);
                WorldGen.KillTile(tile.X, tile.Y, fail: false, effectOnly: false, noItem: settings.SuppressDrops);
                destroyedTiles++;

                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendTileSquare(-1, tile.X, tile.Y);
            }

            if (settings.DestroyWalls && Main.tile[tile.X, tile.Y].WallType > WallID.None)
            {
                action.AddSnapshot(tile);
                WorldGen.KillWall(tile.X, tile.Y);
                
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendTileSquare(-1, tile.X, tile.Y);
            }
        }

        if (destroyedTiles > 0)
        {
            undoMgr.CommitAction(action);
            Main.NewText($"Destroyed {destroyedTiles} tiles" + 
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
                ModContent.GetInstance<WandUISystem>().ToggleUIForCurrentWand();
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
    public override Color ModeColor => new Color(255, 100, 100); // Light red
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfDestructionSelect>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true; // Enable channeling for drag
    }

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
        }
        
        wandPlayer.UpdateSelection(mouseTile);
        return true;
    }

    public override void HoldItem(Player player)
    {
        base.HoldItem(player); // Handle right-click cancel first
        
        var wandPlayer = player.GetModPlayer<WandPlayer>();
        
        // If selection was cancelled by right-click, don't proceed
        if (!wandPlayer.Selection.IsActive)
            return;
        
        // Execute on mouse release (left button released)
        if (!Main.mouseLeft)
        {
            ExecuteDestruction(player, wandPlayer);
            wandPlayer.ClearSelection();
        }
    }
}

// TwoClick Mode - Click start, click end
public class WandOfDestructionSelect : WandOfDestructionBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
    public override Color ModeColor => new Color(255, 200, 100); // Orange
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
            return true;
        }
        else
        {
            // Second click - execute
            wandPlayer.UpdateSelection(mouseTile);
            ExecuteDestruction(player, wandPlayer);
            wandPlayer.ClearSelection();
            return true;
        }
    }
}

// ThreeClick Mode - Click start, click end, click confirm
public class WandOfDestructionConfirm : WandOfDestructionBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => new Color(100, 255, 100); // Light green
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfDestructionInstant>();

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            // First click - start selection
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText("Selection started. Click to set end point.", Color.Cyan);
            return true;
        }
        else if (!wandPlayer.Selection.IsLocked)
        {
            // Second click - lock selection, await confirmation
            wandPlayer.UpdateSelection(mouseTile);
            wandPlayer.LockSelection();  // LOCK IT HERE
            Main.NewText("Click again to confirm, or right-click to cancel.", Color.Yellow);
            return true;
        }
        else
        {
            // Third click - execute
            ExecuteDestruction(player, wandPlayer);
            wandPlayer.ClearSelection();
            return true;
        }
    }

    public override bool CanUseItem(Player player)
    {
        if (player.altFunctionUse == 2)
        {
            var wandPlayer = player.GetModPlayer<WandPlayer>();
            if (wandPlayer.Selection.IsActive)
            {
                wandPlayer.ClearSelection();
                Main.NewText("Selection cancelled.", Color.Yellow);
            }
            return false;
        }
        return true;
    }
}