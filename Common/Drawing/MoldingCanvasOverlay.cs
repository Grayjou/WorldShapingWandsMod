using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Drawing;

/// <summary>
/// Composable overlay that renders the Molding Wand's three-layer visual model:
/// <list type="number">
///   <item><b>Canvas</b> (drawn first) — fill over canvas tiles showing the working area.</item>
///   <item><b>Outside</b> (drawn second) — semi-transparent fill over tiles NOT in the canvas (dimming effect).
///   Invisible when no canvas is active, since it would darken the whole screen.</item>
///   <item><b>TileSelection</b> (drawn third) — fill over selected tiles within the canvas.</item>
/// </list>
/// After all three layers, the common <see cref="SelectionOverlay"/> draws the shape preview
/// highlight on top (drawn last, via SelectionOverlayAdapter ZOrder 0).
/// The canvas border (teal edge segments) renders on top of the three layers via
/// <see cref="CanvasBorderRenderer"/>.
/// </summary>
/// <remarks>
/// <para>
/// This overlay is the Molding Wand's equivalent of <see cref="SelectionCanvasOverlay"/>,
/// which serves the Delimitation Wand. The two overlays are completely independent — they
/// read from different player modules (<see cref="MoldingWandPlayer"/> vs
/// <see cref="DelimitationWandPlayer"/>) so both can be active simultaneously.
/// </para>
/// <para>
/// Overlay colors use the Molding teal/cyan palette defined in <see cref="MoldingWandSettings"/>
/// to visually distinguish Molding operations from Delimitation operations.
/// </para>
/// <para>
/// ZOrder -9 — draws after SelectionCanvasOverlay (-10) and before SelectionOverlay (0).
/// </para>
/// </remarks>
[Autoload(Side = ModSide.Client)]
internal sealed class MoldingCanvasOverlay : IComposableOverlay
{
    private static readonly ReLogic.Content.Asset<Texture2D> PivotIcon = ModContent.Request<Texture2D>(
        "Assets_Build/Icons/Stencil/Pivot", ReLogic.Content.AssetRequestMode.ImmediateLoad);
    private static readonly ReLogic.Content.Asset<Texture2D> CentroidIcon = ModContent.Request<Texture2D>(
        "Assets_Build/Icons/Stencil/Centroid", ReLogic.Content.AssetRequestMode.ImmediateLoad);

    /// <summary>
    /// Draws right after SelectionCanvasOverlay (-10) so both canvas systems
    /// can coexist without z-fighting.
    /// </summary>
    public int ZOrder => -9;

    /// <inheritdoc />
    public bool Visible { get; set; } = true;

    /// <summary>Always redraw — canvas/selection state changes frequently.</summary>
    public bool NeedsRedraw => true;

    private OverlayManager _manager;

    // ================================================================
    //  Lifecycle
    // ================================================================

    public void Initialize(OverlayManager manager)
    {
        _manager = manager;
    }

    public void OnRegister() { }
    public void OnUnregister() { }

    // ================================================================
    //  Update — visibility is driven by canvas/selection state
    // ================================================================

    public void Update(OverlayContext context)
    {
        // No state to cache — we always redraw when visible.
        // Visibility is controlled externally by OverlayManagerSystem.
    }

    // ================================================================
    //  Draw — three-layer model (Canvas → Outside → Selected) + border
    // ================================================================

    public void Draw(SpriteBatch spriteBatch, OverlayContext context)
    {
        var player = context.Player;
        if (player == null) return;

        bool heldIsMolding    = player.HeldItem?.ModItem is WandOfMoldingBase;
        bool readShapeActive  = player.GetModPlayer<WandPlayer>()
                                      ?.IsActiveShape(ShapeType.MagicWandRead) ?? false;

        // Show the canvas overlay when holding the Molding Wand OR when any wand
        // has MagicWandRead selected (cross-family stencil-read preview).
        if (!heldIsMolding && !readShapeActive)
            return;

        var mwp = player.GetModPlayer<MoldingWandPlayer>();
        if (mwp == null)
            return;

        var settings = mwp.Settings;
        bool canvasActive = mwp.Canvas.IsActive;
        bool selectionActive = mwp.Selection.IsActive;

        if (!canvasActive && !selectionActive)
            return;

        // Read overlay colors + separate alpha sliders from client config.
        // CRITICAL: Same premultiplied-alpha approach as SelectionCanvasOverlay.
        // Color × alpha premultiplies RGB so BlendState.AlphaBlend works correctly.
        // Without this, straight-alpha colors produce additive blending artifacts.
        var clientConfig = WandConfigs.CanvasOverlay;
        float outsideA = clientConfig?.MoldingOutsideAlpha ?? 0.2f;
        float canvasA  = clientConfig?.MoldingCanvasFillAlpha ?? 0.4f;
        float tileSA   = clientConfig?.MoldingTileSelectionAlpha ?? 0.4f;
        Color outsideColor = (clientConfig?.MoldingOutsideColor ?? new Color(0, 0, 0, 255)) * outsideA;
        Color canvasFill   = (clientConfig?.MoldingCanvasColor ?? new Color(200, 255, 255, 255)) * canvasA;
        Color tileSelColor = (clientConfig?.MoldingTileSelectionColor ?? new Color(0, 180, 180, 255)) * tileSA;

        var screenBounds = context.ScreenTileBounds;

        // Layer 1 (bottom): Canvas — show the working area
        if (canvasActive)
        {
            // In CanvasEdit mode, use the edit accent color but apply the same
            // configurable alpha slider so the user can control brightness.
            // The accent RGB is premultiplied here (Color * float) just like canvasFill.
            var effectiveFill = settings.Mode == MoldingWandMode.CanvasEdit
                ? MoldingWandSettings.CanvasEditAccentColor * canvasA
                : canvasFill;
            DrawTileFill(spriteBatch, mwp.Canvas.Tiles, screenBounds, effectiveFill);
        }

        // Layer 2 (middle): Outside — dim everything outside the canvas
        // Only drawn when a canvas is active; otherwise it would darken the whole screen.
        if (canvasActive)
            DrawOutsideFill(spriteBatch, mwp.Canvas.Tiles, screenBounds, outsideColor);

        // Layer 3 (top): TileSelection — highlight selected tiles.
        // Hidden during Canvas Edit mode to reduce visual noise.
        // Also suppressed for cross-family Read view (non-Molding wand) — the player
        // is viewing the canvas as a reference, not editing a mold selection.
        if (selectionActive && settings.Mode != MoldingWandMode.CanvasEdit && heldIsMolding)
            DrawTileFill(spriteBatch, mwp.Selection.Tiles, screenBounds, tileSelColor);

        // Border: Canvas edge segments on top of everything (teal border)
        if (canvasActive)
            CanvasBorderRenderer.DrawBorder(spriteBatch, mwp.Canvas.BorderEdges, screenBounds,
                MoldingWandSettings.CanvasBorderColor);

        DrawTransformIndicators(spriteBatch, mwp, settings);
    }

    // ================================================================
    //  Drawing helpers
    // ================================================================

    /// <summary>
    /// Draws a semi-transparent fill over all visible tiles that are NOT in the canvas.
    /// Creates a "spotlight" dimming effect where only the canvas area is bright.
    /// </summary>
    private static void DrawOutsideFill(
        SpriteBatch sb,
        IReadOnlySet<Point> canvasTiles,
        Rectangle screenBounds,
        Color outsideColor)
    {
        if (outsideColor.A == 0)
            return;

        var pixel = TextureAssets.MagicPixel.Value;

        for (int x = screenBounds.Left; x < screenBounds.Right; x++)
        for (int y = screenBounds.Top; y < screenBounds.Bottom; y++)
        {
            if (!canvasTiles.Contains(new Point(x, y)))
            {
                var pos = new Vector2(x * 16, y * 16) - Main.screenPosition;
                sb.Draw(pixel, pos, new Rectangle(0, 0, 16, 16), outsideColor);
            }
        }
    }

    /// <summary>
    /// Draws a semi-transparent fill over the specified tile set, within the visible screen bounds.
    /// Used for both canvas fill (Layer 1) and selection fill (Layer 3).
    /// </summary>
    private static void DrawTileFill(
        SpriteBatch sb,
        IReadOnlySet<Point> tiles,
        Rectangle screenBounds,
        Color fillColor)
    {
        if (fillColor.A == 0 || tiles.Count == 0)
            return;

        var pixel = TextureAssets.MagicPixel.Value;

        foreach (var tile in tiles)
        {
            // Viewport culling
            if (tile.X < screenBounds.Left || tile.X >= screenBounds.Right ||
                tile.Y < screenBounds.Top || tile.Y >= screenBounds.Bottom)
                continue;

            var pos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;
            sb.Draw(pixel, pos, new Rectangle(0, 0, 16, 16), fillColor);
        }
    }

    private static void DrawTransformIndicators(
        SpriteBatch sb,
        MoldingWandPlayer mwp,
        MoldingWandSettings settings)
    {
        if (!TryResolveCentroidTile(mwp, out var centroidTile))
            return;

        var prefs = WandConfigs.Preferences;
        bool transformModeOn = settings.TransformModeEnabled;
        bool alwaysShowPivot = prefs?.AlwaysShowPivot ?? true;
        var offModeAnchor = prefs?.TransformAnchorTMOff ?? TransformAnchorTMOff.Pivot;

        bool hasPivot = false;
        Vector2 pivotTile = Vector2.Zero;

        if (settings.TemporaryPivot.HasValue)
        {
            var p = settings.TemporaryPivot.Value;
            pivotTile = new Vector2(p.X + 0.5f, p.Y + 0.5f);
            hasPivot = true;
        }
        else if (settings.PersistentPivot.HasValue)
        {
            var p = settings.PersistentPivot.Value;
            pivotTile = new Vector2(p.X + 0.5f, p.Y + 0.5f);
            hasPivot = true;
        }

        if (transformModeOn)
        {
            DrawTransformIcon(sb, centroidTile, usePivotIcon: false, MoldingWandSettings.CanvasBorderColor);
            if (hasPivot)
                DrawTransformIcon(sb, pivotTile, usePivotIcon: true, MoldingWandSettings.CanvasBorderColor);
            return;
        }

        if (!alwaysShowPivot)
            return;

        if (offModeAnchor == TransformAnchorTMOff.Centroid)
        {
            DrawTransformIcon(sb, centroidTile, usePivotIcon: false, MoldingWandSettings.CanvasBorderColor);
            return;
        }

        DrawTransformIcon(sb, centroidTile, usePivotIcon: false, MoldingWandSettings.CanvasBorderColor);
        if (hasPivot)
            DrawTransformIcon(sb, pivotTile, usePivotIcon: true, MoldingWandSettings.CanvasBorderColor);
    }

    private static void DrawTransformIcon(SpriteBatch sb, Vector2 tilePos, bool usePivotIcon, Color pivotTint)
    {
        var icon = usePivotIcon ? PivotIcon.Value : CentroidIcon.Value;
        Vector2 worldPos = tilePos * 16f;
        Vector2 drawPos = worldPos - Main.screenPosition;
        Vector2 origin = new Vector2(icon.Width * 0.5f, icon.Height * 0.5f);
        Color tint = usePivotIcon ? pivotTint : Color.White;
        sb.Draw(icon, drawPos, null, tint * 0.95f, 0f, origin, 1f, SpriteEffects.None, 0f);
    }

    private static bool TryResolveCentroidTile(MoldingWandPlayer mwp, out Vector2 centroidTile)
    {
        if (mwp.Canvas.IsActive)
        {
            centroidTile = mwp.Canvas.CenterOfMass;
            return true;
        }

        if (mwp.Selection.IsActive)
        {
            double sumX = 0d;
            double sumY = 0d;
            int count = 0;
            foreach (var tile in mwp.Selection.Tiles)
            {
                sumX += tile.X + 0.5d;
                sumY += tile.Y + 0.5d;
                count++;
            }

            if (count > 0)
            {
                centroidTile = new Vector2((float)(sumX / count), (float)(sumY / count));
                return true;
            }
        }

        centroidTile = Vector2.Zero;
        return false;
    }
}
