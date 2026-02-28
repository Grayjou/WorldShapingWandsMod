using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using MagicWiring.Common;
using MagicWiring.Networking;

namespace MagicWiring.Content.Projectiles;

public class WiringWandProjectile : ModProjectile
{
    /// <summary>
    /// Maximum duration the projectile can exist (in ticks, 2 minutes at 60fps).
    /// </summary>
    private const int MaxTimeLeft = 7200;

    public Point StartTile
    {
        get => new Point((int)Projectile.ai[0], (int)Projectile.ai[1]);
        set
        {
            Projectile.ai[0] = value.X;
            Projectile.ai[1] = value.Y;
        }
    }

    public Point EndTile { get; private set; }
    public bool IsClamped { get; private set; }
    public bool VerticalFirst { get; private set; }

    private bool _initialized = false;
    private bool _cancelledByPlayer = false;
    private bool _toggleConfirmed = false;

    public override void SetDefaults()
    {
        Projectile.width = 2;
        Projectile.height = 2;
        Projectile.aiStyle = -1;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.hide = true;
        Projectile.timeLeft = MaxTimeLeft;
    }

    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];

        if (owner.dead || owner.CCed)
        {
            _cancelledByPlayer = true;
            Projectile.Kill();
            return;
        }

        if (WiringSettings.Interaction == InteractionMode.Toggle)
            ToggleModeAI(owner);
        else
            HoldModeAI(owner);
    }

    private void HoldModeAI(Player owner)
    {
        if (!owner.channel)
        {
            Projectile.Kill(); // Released mouse - execute
            return;
        }

        Projectile.timeLeft = 2;
        Point mouseTile = Main.MouseWorld.ToTileCoordinates();

        if (!_initialized)
        {
            StartTile = mouseTile;
            Vector2 toMouse = Main.MouseWorld - owner.Center;
            VerticalFirst = Math.Abs(toMouse.Y) > Math.Abs(toMouse.X);
            _initialized = true;
        }

        ApplyDistanceClamping(mouseTile);
        Projectile.Center = Main.MouseWorld;
    }

    private void ToggleModeAI(Player owner)
    {
        var wandPlayer = owner.GetModPlayer<WiringWandPlayer>();

        if (!wandPlayer.PendingStartTile.HasValue)
        {
            _cancelledByPlayer = true;
            Projectile.Kill();
            return;
        }

        if (_toggleConfirmed)
        {
            Projectile.Kill(); // Execute on next frame
            return;
        }

        // Cancel on right-click
        if (Main.mouseRight && Main.mouseRightRelease)
        {
            wandPlayer.ClearPending();
            _cancelledByPlayer = true;
            Projectile.Kill();
            return;
        }

        Projectile.timeLeft = 2;

        if (!_initialized)
        {
            StartTile = wandPlayer.PendingStartTile.Value;
            VerticalFirst = wandPlayer.PendingVerticalFirst;
            _initialized = true;
        }

        Point mouseTile = Main.MouseWorld.ToTileCoordinates();
        ApplyDistanceClamping(mouseTile);
        Projectile.Center = Main.MouseWorld;
    }

    private void ApplyDistanceClamping(Point targetTile)
    {
        var config = ModContent.GetInstance<MagicWiringConfig>();
        int maxDist = config?.MaxWiringDistance ?? 200;
        var (clamped, wasClamped) = ShapeHelper.ClampDistance(StartTile, targetTile, maxDist);
        EndTile = clamped;
        IsClamped = wasClamped;
    }

    public void ExecuteToggleConfirm()
    {
        _toggleConfirmed = true;
    }

    public override void OnKill(int timeLeft)
    {
        if (Projectile.owner != Main.myPlayer) return;

        var wandPlayer = Main.player[Projectile.owner].GetModPlayer<WiringWandPlayer>();
        wandPlayer.ClearPending();

        if (!_initialized || _cancelledByPlayer) return;
        if (!WiringSettings.HasAnySelection) return;

        Player player = Main.player[Projectile.owner];
        Point start = StartTile;
        Point end = EndTile;

        var tiles = ShapeHelper.GetShapeTiles(start, end, WiringSettings.Shape, VerticalFirst);
        if (tiles.Count == 0) return;

        WiringHelper.ExecuteWiringOperation(tiles, WiringSettings.Mode,
            WiringSettings.WireRed, WiringSettings.WireGreen, WiringSettings.WireBlue,
            WiringSettings.WireYellow, WiringSettings.Actuator, player);

        if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
        {
            WiringPacketHandler.SendWiringOperation(start, end, WiringSettings.Mode,
                WiringSettings.Shape, WiringSettings.PackWireFlags(), VerticalFirst, Projectile.owner);
        }
    }

    public static WiringWandProjectile GetActiveProjectile()
    {
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile proj = Main.projectile[i];
            if (proj.active && proj.owner == Main.myPlayer &&
                proj.ModProjectile is WiringWandProjectile wand && wand._initialized)
                return wand;
        }
        return null;
    }
}