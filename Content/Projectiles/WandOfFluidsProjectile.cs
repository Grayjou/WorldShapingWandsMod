using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;
#if DEBUG
using WorldShapingWandsMod.Common.Debug;
#endif
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Content.Projectiles;

/// <summary>
/// Cosmetic held projectile for the Wand of Fluids.
/// Manually spawned and sustained by <see cref="WandOfFluidsBase.OnHoldItemFamily"/>
/// (not via <c>Item.shoot</c>). Points toward the cursor and stays alive while the
/// player is holding any Wand of Fluids variant.
/// <para>
/// The spritesheet is a single-column, 6-row grid:
/// <list type="bullet">
///   <item>Row 0: No fluid / Drain Mode</item>
///   <item>Row 1: Water</item>
///   <item>Row 2: Lava</item>
///   <item>Row 3: Honey</item>
///   <item>Row 4: Shimmer</item>
///   <item>Row 5: Bubble block</item>
/// </list>
/// </para>
/// </summary>
public class WandOfFluidsProjectile : ModProjectile
{
    #region Spritesheet Constants

    private const int SpriteWidth = 46;
    private const int SpriteHeight = 46;
    private const int Padding = 2;
    private const int FrameHeight = SpriteHeight + Padding;
    private const int NumCols = 1;
    private const int NumRows = 6;
    private const int FramesPerCol = 15;
    private const float ReferenceSpeed = 12f;

    #endregion

    #region Row Mapping Constants

    private const int RowNone = 0;
    private const int RowWater = 1;
    private const int RowLava = 2;
    private const int RowHoney = 3;
    private const int RowShimmer = 4;
    private const int RowBubble = 5;

    #endregion

    #region Handle Offset Constants

    private const float HandleOffsetX = 16f;
    private const float HandleOffsetY = 34f;
    private const float ArmLength = 12.5f;

    private const float ShoulderOffsetX = -5f;
    private const float ShoulderOffsetY = -2f;
    private static readonly float HandleOffsetRotation = (float)Math.Atan2(HandleOffsetY, HandleOffsetX);

    #endregion

    #region AI Fields

    private ref float AiSelectionMode => ref Projectile.ai[0];
    private ref float AiFluidState => ref Projectile.ai[1];

    #endregion

    #region Instance State

    private SpritesheetHelper _spritesheet;
    private int _animTimer;
    private float _armRotation;
    private Vector2 _handWorldPos;

    #endregion

    #region Projectile Setup

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = NumRows * NumCols;
    }

    public override void SetDefaults()
    {
        Projectile.width = SpriteWidth;
        Projectile.height = SpriteHeight;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 60;
        Projectile.hide = true;
        Projectile.netImportant = true;
    }

    public override void OnSpawn(IEntitySource source)
    {
        _spritesheet = new SpritesheetHelper(columns: NumCols, rows: NumRows);
        _spritesheet.SetFrameSize(SpriteWidth, FrameHeight);
        _animTimer = 0;
    }

    #endregion

    #region AI

    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];

        // Lifetime management — refresh timeLeft every frame for ALL selection modes
        // while the owner still holds left-click. Previously this was gated to
        // OneClick + FourClick (Instant + Stamp), which caused TwoClick (Select)
        // and ThreeClick (Confirm) projectiles to expire after 60 frames; the held-
        // item update would then re-spawn a fresh projectile next frame, producing
        // a visible blink. The Main.mouseLeft kill-guard below handles release.
        Projectile.timeLeft = 60;

        // Die if owner stopped holding a Wand of Fluids or released left click
        if (owner.HeldItem?.ModItem is not WandOfFluidsBase || !Main.mouseLeft)
        {
            Projectile.Kill();
            return;
        }

        // Velocity toward cursor (local player only)
        if (Main.myPlayer == Projectile.owner)
        {
            Vector2 handPos = owner.RotatedRelativePoint(owner.MountedCenter);
            float cursorX = Main.mouseX + Main.screenPosition.X - handPos.X;
            float cursorY = Main.mouseY + Main.screenPosition.Y - handPos.Y;

            if (owner.gravDir == -1f)
                cursorY = Main.screenHeight - Main.mouseY + Main.screenPosition.Y - handPos.Y;

            float dist = MathF.Sqrt(cursorX * cursorX + cursorY * cursorY);
            if (dist > 0f)
            {
                float scale = ReferenceSpeed / dist;
                cursorX *= scale;
                cursorY *= scale;
            }

            if (cursorX != Projectile.velocity.X || cursorY != Projectile.velocity.Y)
                Projectile.netUpdate = true;

            Projectile.velocity.X = cursorX;
            Projectile.velocity.Y = cursorY;
        }

        // Position and rotation (all clients)
        Projectile.spriteDirection = owner.direction;

        Vector2 shoulderOffset = new Vector2(
            ShoulderOffsetX * owner.direction,
            ShoulderOffsetY * owner.gravDir
        );

        // Apply owner.gfxOffY so the projectile follows the player's smoothed
        // walk-bob rendering rather than the raw position. Without this the
        // projectile visibly shakes while the owner is walking (the player
        // sprite is rendered at position+gfxOffY, but MountedCenter doesn't
        // include gfxOffY, so the projectile lags by up to ~half a tile each
        // step). Vanilla held projectiles like Crystyl Crusher apply the
        // equivalent compensation and don't shake.
        Vector2 shoulderPosWorld = owner.MountedCenter + shoulderOffset + new Vector2(0f, owner.gfxOffY);

        float armRotation = MathF.Atan2(
            Projectile.velocity.Y * owner.direction,
            Projectile.velocity.X * owner.direction
        );
        armRotation += -HandleOffsetRotation * owner.direction;

        Vector2 armDirection = new Vector2(
            MathF.Sin(armRotation),
            -MathF.Cos(armRotation)
        );

        Vector2 handWorldPos = shoulderPosWorld - armDirection * ArmLength;

        _armRotation = armRotation;
        _handWorldPos = handWorldPos;

        Projectile.Center = handWorldPos;

        // Wand rotation
        float cursorAngle = MathF.Atan2(Projectile.velocity.Y, Projectile.velocity.X);

        Projectile.rotation = owner.direction == 1
            ? cursorAngle + MathHelper.PiOver4
            : 3f * MathHelper.PiOver4 + cursorAngle;

        // Arm compositing
        owner.heldProj = Projectile.whoAmI;
        owner.SetCompositeArmFront(
            enabled: true,
            stretch: Player.CompositeArmStretchAmount.Full,
            rotation: armRotation
        );

        // Fluid state tracking
        UpdateRowFromSettings(owner);

        // Animation timer (for future multi-column spritesheets)
        #pragma warning disable CS0162 // Unreachable code (for now, since we only have 1 column)
        if (NumCols > 1)
        {
            _animTimer++;
            if (_animTimer >= FramesPerCol)
            {
                _animTimer = 0;
                _spritesheet.CurrentColumn = (_spritesheet.CurrentColumn + 1) % NumCols;
            }
        }
        #pragma warning restore CS0162
    }

    private void UpdateRowFromSettings(Player owner)
    {
        var wandPlayer = owner.GetModPlayer<WandPlayer>();
        var settings = wandPlayer.FluidsSettings;

        if (settings.Operation == FluidOperation.Drain)
        {
            _spritesheet.CurrentRow = RowNone;
            return;
        }

        if (settings.PlaceBubble)
        {
            _spritesheet.CurrentRow = RowBubble;
            return;
        }

        _spritesheet.CurrentRow = settings.LiquidType switch
        {
            LiquidTypeSelection.Water => RowWater,
            LiquidTypeSelection.Lava => RowLava,
            LiquidTypeSelection.Honey => RowHoney,
            LiquidTypeSelection.Shimmer => RowShimmer,
            _ => RowNone
        };
    }

    #endregion

    #region Drawing

    public override string Texture => "WorldShapingWandsMod/Content/Projectiles/WandOfFluidsInstantProjectile";

    public override bool PreDraw(ref Color lightColor) => false;

    public DrawData GetDrawData(Player owner, PlayerDrawSet drawInfo)
    {
        // Texture selection
        SelectionMode mode = (SelectionMode)(int)AiSelectionMode;
        string texturePath = mode switch
        {
            SelectionMode.OneClick => "WorldShapingWandsMod/Content/Projectiles/WandOfFluidsInstantProjectile",
            SelectionMode.TwoClick => "WorldShapingWandsMod/Content/Projectiles/WandOfFluidsSelectProjectile",
            SelectionMode.ThreeClick => "WorldShapingWandsMod/Content/Projectiles/WandOfFluidsConfirmProjectile",
            SelectionMode.FourClick => "WorldShapingWandsMod/Content/Projectiles/WandOfFluidsStampProjectile",
            _ => "WorldShapingWandsMod/Content/Projectiles/WandOfFluidsInstantProjectile"
        };

        Texture2D texture = ModContent.Request<Texture2D>(texturePath).Value;

        // Source rectangle
        Rectangle sourceRect = new Rectangle(
            _spritesheet.CurrentColumn * SpriteWidth,
            _spritesheet.CurrentRow * FrameHeight,
            SpriteWidth,
            SpriteHeight
        );

        // Hand position (use cached value or fallback)
        Vector2 handPos;
        if (_handWorldPos == default)
        {
            Vector2 shoulderOffset = new Vector2(-5f * owner.direction, -3f * owner.gravDir);
            float fallbackArmRotation = MathF.Atan2(
                Projectile.velocity.Y * owner.direction,
                Projectile.velocity.X * owner.direction
            );
            fallbackArmRotation += -HandleOffsetRotation * owner.direction;

            Vector2 shoulderPos = drawInfo.Position + owner.Size / 2f + shoulderOffset;
            Vector2 armDirection = new Vector2(MathF.Sin(fallbackArmRotation), -MathF.Cos(fallbackArmRotation));
            handPos = shoulderPos + armDirection * ArmLength - Main.screenPosition;
        }
        else
        {
            handPos = _handWorldPos - Main.screenPosition;
        }

        // Sprite effects and origin
        SpriteEffects effects = owner.direction == -1
            ? SpriteEffects.FlipHorizontally
            : SpriteEffects.None;

        float originX = owner.direction == -1
            ? SpriteWidth - HandleOffsetX
            : HandleOffsetX;
        Vector2 origin = new Vector2(originX, HandleOffsetY);

        // Lighting
        Color drawColor = Lighting.GetColor(
            (int)(owner.MountedCenter.X / 16f),
            (int)(owner.MountedCenter.Y / 16f)
        );
        drawColor = drawInfo.colorArmorBody.MultiplyRGBA(drawColor);

        return new DrawData(
            texture,
            handPos,
            sourceRect,
            drawColor,
            Projectile.rotation,
            origin,
            Projectile.scale,
            effects,
            0
        );
    }

    #endregion
}