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
using WorldShapingWandsMod.Common.Utilities;
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
    private static ReLogic.Content.Asset<Texture2D> _pivotIcon;
    private static ReLogic.Content.Asset<Texture2D> _centroidIcon;
    private static bool _iconsInitialized;

    private static void EnsureTransformIconsLoaded()
    {
        if (_iconsInitialized)
            return;

        _iconsInitialized = true;

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();
        if (mod?.Assets == null)
            return;

        const string pivotPath = "Assets_Build/Icons/Stencil/Pivot";
        const string centroidPath = "Assets_Build/Icons/Stencil/Centroid";

        try
        {
            _pivotIcon = mod.Assets.Request<Texture2D>(pivotPath, ReLogic.Content.AssetRequestMode.ImmediateLoad);
        }
        catch
        {
            _pivotIcon = null;
        }

        try
        {
            _centroidIcon = mod.Assets.Request<Texture2D>(centroidPath, ReLogic.Content.AssetRequestMode.ImmediateLoad);
        }
        catch
        {
            _centroidIcon = null;
        }
    }

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
            StencilOverlayRenderer.DrawFill(spriteBatch, mwp.Canvas.Tiles, screenBounds, effectiveFill);
        }

        // Layer 2 (middle): Outside — dim everything outside the canvas
        // Only drawn when a canvas is active; otherwise it would darken the whole screen.
        if (canvasActive)
            StencilOverlayRenderer.DrawOutsideFill(spriteBatch, mwp.Canvas.Tiles, screenBounds, outsideColor);

        // Layer 3 (top): TileSelection — highlight selected tiles.
        // Hidden during Canvas Edit mode to reduce visual noise.
        // Also suppressed for cross-family Read view (non-Molding wand) — the player
        // is viewing the canvas as a reference, not editing a mold selection.
        if (selectionActive && settings.Mode != MoldingWandMode.CanvasEdit && heldIsMolding)
            StencilOverlayRenderer.DrawFill(spriteBatch, mwp.Selection.Tiles, screenBounds, tileSelColor);

        // Border: Canvas edge segments on top of everything (teal border)
        if (canvasActive)
            CanvasBorderRenderer.DrawBorder(spriteBatch, mwp.Canvas.BorderEdges, screenBounds,
                MoldingWandSettings.CanvasBorderColor);

        DrawTransformIndicators(spriteBatch, mwp, settings);
        DrawMoveElbowOverlay(spriteBatch, settings, MoldingWandSettings.CanvasBorderColor);
    }

    // ================================================================
    //  Drawing helpers
    // ================================================================

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
        EnsureTransformIconsLoaded();

        var iconAsset = usePivotIcon ? _pivotIcon : _centroidIcon;
        var icon = iconAsset?.Value;
        if (icon == null)
            return;

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
            centroidTile = TransformPivotSnapHelper.SnapTileCenter(mwp.Canvas.CenterOfMass);
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
                centroidTile = TransformPivotSnapHelper.SnapTileCenter(new Vector2((float)(sumX / count), (float)(sumY / count)));
                return true;
            }
        }

        centroidTile = Vector2.Zero;
        return false;
    }

    private static void DrawMoveElbowOverlay(SpriteBatch sb, MoldingWandSettings settings, Color accentColor)
    {
        if (!settings.TransformModeEnabled ||
            settings.ActiveTransformAction != TransformActionMode.Move ||
            !settings.PendingTransformMoveStart.HasValue)
            return;

        Point startTile = settings.PendingTransformMoveStart.Value;
        Point nowTile = GeometryHelper.GetMouseTile();
        Point elbowTile = new Point(nowTile.X, startTile.Y);

        int dx = nowTile.X - startTile.X;
        int dy = nowTile.Y - startTile.Y;

        Color lineColor = accentColor * 0.55f;
        DrawElbowSegment1Tile(sb, startTile, elbowTile, lineColor);
        DrawElbowSegment1Tile(sb, elbowTile, nowTile, lineColor);

        if (dx == 0 && dy == 0)
            return;

        string label = $"{dx} x {dy}";
        Vector2 elbowWorld = new Vector2((elbowTile.X + 0.5f) * 16f, (elbowTile.Y + 0.5f) * 16f);
        Vector2 elbowScreen = elbowWorld - Main.screenPosition;
        Vector2 labelPos = elbowScreen + new Vector2(dx >= 0 ? 8f : -52f, dy >= 0 ? 8f : -24f);
        Utils.DrawBorderString(sb, label, labelPos, Color.White * 0.95f, 0.85f);
    }

    private static void DrawElbowSegment1Tile(SpriteBatch sb, Point fromTile, Point toTile, Color color)
    {
        var pixel = TextureAssets.MagicPixel.Value;

        if (fromTile.Y == toTile.Y)
        {
            int y = fromTile.Y;
            int minX = System.Math.Min(fromTile.X, toTile.X);
            int maxX = System.Math.Max(fromTile.X, toTile.X);
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 pos = new Vector2(x * 16f, y * 16f) - Main.screenPosition;
                sb.Draw(pixel, pos, new Rectangle(0, 0, 16, 16), color);
            }
            return;
        }

        int xCol = fromTile.X;
        int minY = System.Math.Min(fromTile.Y, toTile.Y);
        int maxY = System.Math.Max(fromTile.Y, toTile.Y);
        for (int y = minY; y <= maxY; y++)
        {
            Vector2 pos = new Vector2(xCol * 16f, y * 16f) - Main.screenPosition;
            sb.Draw(pixel, pos, new Rectangle(0, 0, 16, 16), color);
        }
    }
}
