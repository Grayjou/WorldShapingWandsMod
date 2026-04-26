using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Utilities;
#if DEBUG
using WorldShapingWandsMod.Common.Debug;
#endif

namespace WorldShapingWandsMod.Content.Projectiles.WandModes;

/// <summary>
/// Abstract base for mode indicator projectiles that hover above the player's head.
/// Each selection mode (Instant, Select, Confirm, Stamp) has its own concrete subclass
/// with an independent texture and lifetime rules.
/// <para>
/// These projectiles are purely cosmetic — they communicate the wand's current
/// selection mode visually. They NEVER mutate player state (no <c>heldProj</c>,
/// no <c>SetDummyItemTime</c>, no <c>ChangeDir</c>).
/// </para>
/// </summary>
/// <remarks>
/// Design reference: <c>dev_notes/features/mode_projectile_design.md</c>
/// Pattern reference: <see cref="WandOfFluidsProjectile"/> (Session 10 S5–S6)
/// <para>
/// Spritesheet layout: 10 columns (one per wand family) × N rows (animation frames).
/// Column is fixed at spawn time via <c>Projectile.ai[1]</c> = <see cref="WandFamily"/> index.
/// Row cycles for animation (NumRows > 1). FrameDuration = 15 ticks reserved.
/// SpritesheetHelper infrastructure handles all column×row addressing with 2px padding.
/// </para>
/// </remarks>
public abstract class BaseModeProjectile : ModProjectile
{
    // ================================================================
    //  Spritesheet Constants
    // ================================================================

    /// <summary>Width of a single sprite frame in pixels.</summary>
    protected const int SpriteWidth = 32;

    /// <summary>Height of a single sprite frame in pixels (excluding padding).</summary>
    protected const int SpriteHeight = 34;

    /// <summary>Horizontal padding between columns in the spritesheet (pixels).</summary>
    protected const int PaddingX = 2;

    /// <summary>Terraria-standard bottom padding per frame in pixels.</summary>
    protected const int PaddingY = 2;

    /// <summary>Total width of one column cell including horizontal padding.</summary>
    protected const int CellWidth = SpriteWidth + PaddingX;

    /// <summary>Total height of one frame cell including vertical padding.</summary>
    protected const int FrameHeight = SpriteHeight + PaddingY;

    /// <summary>Number of columns in the spritesheet — one per wand family (10).</summary>
    protected const int NumCols = 10;

    /// <summary>Number of rows in the spritesheet.</summary>
    protected const int NumRows = 1;

    /// <summary>Ticks between animation frame advances. 15 ticks = 0.25s per frame.</summary>
    protected const int FrameDuration = 15;

    // ================================================================
    //  Visual Constants
    // ================================================================

    /// <summary>Vertical offset above the player's Top position (negative = upward).</summary>
    private const float DefaultHoverOffsetY = -24f;

    /// <summary>Default bobbing amplitude in pixels.</summary>
    private const float DefaultBobAmplitude = 2f;

    /// <summary>Default bobbing speed in radians per tick.</summary>
    private const float DefaultBobSpeed = 0.05f;

    /// <summary>Number of ticks over which the projectile fades out before dying.</summary>
    protected const int FadeDuration = 60;

    // ================================================================
    //  Mode-Specific Overrides
    // ================================================================

    /// <summary>
    /// Default lifetime in ticks for this mode's projectile.
    /// Override in concrete subclasses (Instant=90, Select=300, Confirm=300, Stamp=180).
    /// Used by <see cref="SetDefaults"/> and <see cref="Refresh"/>.
    /// </summary>
    protected virtual int ModeLifetime => 60;

    // ================================================================
    //  Instance State
    // ================================================================

    private SpritesheetHelper _spritesheet;
    private int _animTimer;

    /// <summary>Frame counter used for bobbing animation. Stored in ai[0].</summary>
    private ref float AiFrameCounter => ref Projectile.ai[0];

    /// <summary>
    /// Wand family index (0–9) passed at spawn time via ai[1].
    /// Determines which spritesheet column to display.
    /// </summary>
    private ref float AiFamilyIndex => ref Projectile.ai[1];

    // ================================================================
    //  Projectile Setup
    // ================================================================

    public override void SetStaticDefaults()
    {
        // Columns = wand families (spatial), rows = animation frames (temporal).
        // Terraria's projFrames counts temporal frames only.
        Main.projFrames[Projectile.type] = NumRows;
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
        Projectile.timeLeft = ModeLifetime;
        Projectile.hide = true;         // We draw manually via PreDraw
        Projectile.netImportant = true;
    }

    /// <summary>
    /// Resets <c>timeLeft</c> to the mode's full lifetime.
    /// Called by <see cref="BaseCyclingWand"/> every frame while the player holds
    /// a wand whose mode matches this projectile's type.
    /// </summary>
    public void Refresh()
    {
        Projectile.timeLeft = ModeLifetime;
    }

    public override void OnSpawn(IEntitySource source)
    {
        _spritesheet = new SpritesheetHelper(columns: NumCols, rows: NumRows);
        _spritesheet.SetFrameSize(CellWidth, FrameHeight);
        _animTimer = 0;

        // Set the column to the wand family index passed via ai[1]
        int familyCol = Math.Clamp((int)AiFamilyIndex, 0, NumCols - 1);
        _spritesheet.CurrentColumn = familyCol;
    }

    // ================================================================
    //  AI — Hover Above Player
    // ================================================================

    /// <summary>
    /// Pure cosmetic AI — positions the projectile above the player's head
    /// with a gentle bobbing motion. No player state mutation whatsoever.
    /// In DEBUG builds, reads hover/bob parameters from <see cref="DevTunableDefaults"/>
    /// for live calibration via the <c>/dev</c> command.
    /// </summary>
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];

        // ── Visual parameters (baked from DevTunable calibration) ──
        float hoverY = DefaultHoverOffsetY;
        float bobAmp = DefaultBobAmplitude;
        float bobSpd = DefaultBobSpeed;

        // ── Position above player's head ──
        float bobOffset = MathF.Sin(AiFrameCounter * bobSpd) * bobAmp;
        Vector2 hoverPos = owner.Top + new Vector2(0f, hoverY + bobOffset);
        Projectile.Center = hoverPos;

        // ── Sync spritesheet column with ai[1] (family may change live) ──
        if (_spritesheet != null)
        {
            int targetCol = Math.Clamp((int)AiFamilyIndex, 0, NumCols - 1);
            _spritesheet.CurrentColumn = targetCol;
        }

        // Increment frame counter for bobbing
        AiFrameCounter++;

        // ── Animation row advance (future multi-row animation support) ──
        // Columns = wand family (fixed at spawn), Rows = animation frames (cycle)
#pragma warning disable CS0162 // Unreachable code — intentional guard for NumRows > 1 future expansion
        if (NumRows > 1)
        {
            _animTimer++;
            if (_animTimer >= FrameDuration)
            {
                _animTimer = 0;
                _spritesheet.CurrentRow = (_spritesheet.CurrentRow + 1) % NumRows;
            }
        }
#pragma warning restore CS0162
    }

    // ================================================================
    //  Drawing
    // ================================================================

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

        // Source rectangle from spritesheet (columns include horizontal padding in stride)
        Rectangle sourceRect = new Rectangle(
            _spritesheet.CurrentColumn * CellWidth,
            _spritesheet.CurrentRow * FrameHeight,
            SpriteWidth,
            SpriteHeight);

        // Origin at center of sprite
        Vector2 origin = new Vector2(SpriteWidth / 2f, SpriteHeight / 2f);

        // Draw position (world to screen)
        Vector2 drawPos = Projectile.Center - Main.screenPosition;

        // Alpha fade during last FadeDuration ticks
        float alpha = Projectile.timeLeft < FadeDuration
            ? Projectile.timeLeft / (float)FadeDuration
            : 1f;

        // Use player position for lighting
        Player owner = Main.player[Projectile.owner];
        Color drawColor = Lighting.GetColor(
            (int)(owner.Top.X / 16f),
            (int)(owner.Top.Y / 16f));

        // Apply fade alpha
        drawColor *= alpha;

        Main.EntitySpriteDraw(
            texture,
            drawPos,
            sourceRect,
            drawColor,
            0f,             // No rotation — always upright
            origin,
            Projectile.scale,
            SpriteEffects.None,
            0);

        return false; // We handled drawing
    }

    /// <summary>
    /// Draw above players (same layer as <see cref="WandOfFluidsProjectile"/>).
    /// </summary>
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles,
        List<int> behindNPCs, List<int> behindProjectiles,
        List<int> overPlayers, List<int> overWiresUI)
    {
        overPlayers.Add(index);
    }
}
