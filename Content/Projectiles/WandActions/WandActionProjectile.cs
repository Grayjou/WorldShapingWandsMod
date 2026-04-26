using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Content.Projectiles.WandActions.Resolvers;
#if DEBUG
using WorldShapingWandsMod.Common.Debug;
#endif

namespace WorldShapingWandsMod.Content.Projectiles.WandActions;

/// <summary>
/// Single projectile type for the WandAction visual indicator system.
/// Replaces the 4 legacy BaseModeProjectile subclasses for wands that opt in
/// via <see cref=Common.Items.BaseCyclingWand.UseWandActionProjectile/>.
/// <para>
/// Each WandAction value maps to its own texture file (e.g. <c>WandAction_BuildingSolid.png</c>).
/// Textures are 4-column spritesheets where each column represents a WandMode
/// (Instant, Select, Confirm, Stamp).
/// </para>
/// </summary>
/// <remarks>
/// AI slot usage:
/// <list type=bullet>
///   <item><c>ai[0]</c>: Bob counter (float, incremented each frame)</item>
///   <item><c>ai[1]</c>: WandAction (cast from byte \u2014 determines texture)</item>
///   <item><c>localAI[0]</c>: WandMode (column 0\u20133 in spritesheet)</item>
/// </list>
/// </remarks>
public class WandActionProjectile : ModProjectile
{
    // ================================================================
    //  Spritesheet Constants
    // ================================================================

    private const int SpriteWidth = 32;
    private const int SpriteHeight = 34;
    private const int PaddingX = 2;
    private const int PaddingY = 0;
    private const int CellWidth = SpriteWidth + PaddingX;
    private const int FrameHeight = SpriteHeight + PaddingY;
    private const int NumCols = 4;   // One per WandMode
    private const int NumRows = 1;   // Single row per action

    // ================================================================
    //  Visual Constants
    // ================================================================

    private const float DefaultHoverOffsetY = -24f;
    private const float DefaultBobAmplitude = 2f;
    private const float DefaultBobSpeed = 0.05f;
    private const int FadeDuration = 60;

    /// <summary>
    /// Per-mode lifetimes in ticks, indexed by WandMode ordinal.
    /// Instant=90 (1.5s), Select=300 (5s), Confirm=300 (5s), Stamp=180 (3s).
    /// Matches legacy BaseModeProjectile values.
    /// </summary>
    private static readonly int[] ModeLifetimes = { 90, 120, 120, 120 };

    // ================================================================
    //  Instance State
    // ================================================================

    private SpritesheetHelper _spritesheet;
    private Asset<Texture2D> _actionTexture;
    private string _loadedTextureKey = ""; // full texture path for cache invalidation

    private ref float AiBobCounter => ref Projectile.ai[0];
    private ref float AiAction => ref Projectile.ai[1];
    private ref float LocalMode => ref Projectile.localAI[0];

    // ================================================================
    //  Setup
    // ================================================================

    public override string Texture => "WorldShapingWandsMod/Content/Projectiles/WandActions/Building/WandAction_BuildingSolid";

    public override void SetStaticDefaults()
    {
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
        Projectile.timeLeft = ModeLifetimes[0];
        Projectile.hide = true;
        Projectile.netImportant = true;
    }

    public override void OnSpawn(IEntitySource source)
    {
        _spritesheet = new SpritesheetHelper(columns: NumCols, rows: NumRows);
        _spritesheet.SetFrameSize(CellWidth, FrameHeight);
    }

    // ================================================================
    //  Texture Resolution
    // ================================================================

    /// <summary>
    /// Resolves the texture for the current WandAction by delegating to the
    /// <see cref="ActionSpriteResolverRegistry"/>. Each family provides its own
    /// <see cref="IActionSpriteResolver"/> that knows how to map actions to
    /// texture paths. Falls back to the projectile's default texture if the
    /// resolved asset is not found.
    /// </summary>
    private Asset<Texture2D> GetActionTexture(WandAction action)
    {
        var resolver = ActionSpriteResolverRegistry.GetResolver(action);
        string texturePath = resolver.ResolveTexturePath(action, Projectile);

        if (texturePath == _loadedTextureKey && _actionTexture != null)
            return _actionTexture;

        if (ModContent.HasAsset(texturePath))
            _actionTexture = ModContent.Request<Texture2D>(texturePath, AssetRequestMode.ImmediateLoad);
        else
            _actionTexture = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad);

        _loadedTextureKey = texturePath;
        return _actionTexture;
    }

    // ================================================================
    //  Public API
    // ================================================================

    /// <summary>
    /// Resets <c>timeLeft</c> to the current mode's lifetime.
    /// Called by <see cref=Common.Items.BaseCyclingWand.ManageWandActionProjectile/>
    /// every frame while the player holds an opted-in wand.
    /// </summary>
    public void Refresh()
    {
        int modeIdx = Math.Clamp((int)LocalMode, 0, ModeLifetimes.Length - 1);
        Projectile.timeLeft = ModeLifetimes[modeIdx];
    }

    // ================================================================
    //  AI
    // ================================================================

    public static int GetModeLifetime(WandMode mode)
    {
        int idx = Math.Clamp((int)mode, 0, ModeLifetimes.Length - 1);
        return ModeLifetimes[idx];
    }

    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];

        // ── Visual parameters (baked from DevTunable calibration) ──
        float hoverY = DefaultHoverOffsetY;
        float bobAmp = DefaultBobAmplitude;
        float bobSpd = DefaultBobSpeed;

        float bobOffset = MathF.Sin(AiBobCounter * bobSpd) * bobAmp;
        Vector2 hoverPos = owner.Top + new Vector2(0f, hoverY + bobOffset);
        Projectile.Center = hoverPos;

        // Sync spritesheet column with mode
        if (_spritesheet != null)
        {
            int col = Math.Clamp((int)LocalMode, 0, NumCols - 1);
            _spritesheet.CurrentColumn = col;
        }

        AiBobCounter++;
    }

    // ================================================================
    //  Drawing
    // ================================================================

    public override bool PreDraw(ref Color lightColor)
    {
        WandAction action = (WandAction)(byte)AiAction;
        var texAsset = GetActionTexture(action);
        if (texAsset == null || !texAsset.IsLoaded) return false;

        Texture2D texture = texAsset.Value;

        Rectangle sourceRect = new Rectangle(
            _spritesheet.CurrentColumn * CellWidth,
            _spritesheet.CurrentRow * FrameHeight,
            SpriteWidth,
            SpriteHeight);

        Vector2 origin = new Vector2(SpriteWidth / 2f, SpriteHeight / 2f);
        Vector2 drawPos = Projectile.Center - Main.screenPosition;

        float alpha = Projectile.timeLeft < FadeDuration
            ? Projectile.timeLeft / (float)FadeDuration
            : 1f;

        Player owner = Main.player[Projectile.owner];
        Color drawColor = Lighting.GetColor(
            (int)(owner.Top.X / 16f),
            (int)(owner.Top.Y / 16f));
        drawColor *= alpha;

        Main.EntitySpriteDraw(
            texture,
            drawPos,
            sourceRect,
            drawColor,
            0f,
            origin,
            Projectile.scale,
            SpriteEffects.None,
            0);

        return false;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles,
        List<int> behindNPCs, List<int> behindProjectiles,
        List<int> overPlayers, List<int> overWiresUI)
    {
        overPlayers.Add(index);
    }
}
