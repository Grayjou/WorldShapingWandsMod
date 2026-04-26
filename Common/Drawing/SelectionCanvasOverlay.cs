using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Drawing;

/// <summary>
/// Composable overlay that renders the Select Wand's three-layer visual model:
/// <list type="number">
///   <item><b>Canvas</b> (drawn first) — fill over canvas tiles showing the working area.</item>
///   <item><b>Outside</b> (drawn second) — semi-transparent fill over tiles NOT in the canvas (dimming effect).
///   Invisible when no canvas is active, since it would darken the whole screen.</item>
///   <item><b>TileSelection</b> (drawn third) — fill over selected tiles within the canvas.</item>
/// </list>
/// After all three layers, the common <see cref="SelectionOverlay"/> draws the shape preview
/// highlight on top (drawn last, via SelectionOverlayAdapter ZOrder 0).
/// The canvas border (gold edge segments) renders on top of the three layers via
/// <see cref="CanvasBorderRenderer"/>.
/// </summary>
/// <remarks>
/// <para>
/// All overlays are configurable in alpha and color:
/// <list type="bullet">
///   <item><b>OutsideColor</b> default: Black with 0.2 alpha</item>
///   <item><b>TileSelectionColor</b> default: Olive with 0.4 alpha</item>
///   <item><b>CanvasColor</b> default: White with 0.4 alpha</item>
/// </list>
/// </para>
/// <para>
/// The overlay stays visible even when the player switches away from the Select Wand,
/// as long as the canvas or selection is active. This allows the player to define a
/// selection with the Select Wand, switch to a Building Wand, and still see the
/// canvas/selection overlay while stamping.
/// </para>
/// <para>
/// ZOrder -10 — draws before SelectionOverlay (0), SafekeepingOverlay (50), and TilingOverlay (100).
/// </para>
/// </remarks>
[Autoload(Side = ModSide.Client)]
internal sealed class SelectionCanvasOverlay : IComposableOverlay
{
    /// <summary>
    /// Canvas overlay draws behind all other overlays so that the selection shape
    /// preview and safekeeping markers render on top.
    /// </summary>
    public int ZOrder => -10;

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
    //  The common SelectionOverlay draws the shape preview on top (ZOrder 0).
    // ================================================================

    public void Draw(SpriteBatch spriteBatch, OverlayContext context)
    {
        var swp = context.Player?.GetModPlayer<DelimitationWandPlayer>();
        if (swp == null)
            return;

        // Determine if we're holding the Delimitation wand (full detail) or another wand (reduced alpha)
        bool isDelimitationWand = context.Player?.HeldItem?.ModItem is WandOfDelimitationBase;
        bool isAnyWand = context.Player?.HeldItem?.ModItem is BaseCyclingWand;

        // Only show the overlay when holding ANY wand. If it's not the Delimitation wand,
        // we still show it but with reduced alpha so the player doesn't forget about the area.
        if (!isAnyWand)
            return;

        var settings = swp.Settings;
        bool canvasActive = swp.Canvas.IsActive;
        bool selectionActive = swp.Selection.IsActive;

        if (!canvasActive && !selectionActive)
            return;

        // Read overlay colors + separate alpha sliders from client config.
        // CRITICAL: The SpriteBatch uses BlendState.AlphaBlend (premultiplied alpha).
        // Blend equation: Output = Src + (1 – SrcAlpha) × Dst
        // If we pass straight-alpha colors (e.g. Color(255,0,0,1)), the RGB is
        // added at full strength → additive blending → bright-on-dark artifacts.
        // Fix: Color * alpha performs premultiplication: (R*a, G*a, B*a, A*a),
        // which produces correct transparent blending.
        var clientConfig = WandConfigs.CanvasOverlay;
        float outsideA = clientConfig?.CanvasOutsideAlpha ?? 0.2f;
        float canvasA  = clientConfig?.CanvasFillAlpha ?? 0.4f;
        float tileSA   = clientConfig?.CanvasTileSelectionAlpha ?? 0.4f;

        // When holding a non-Delimitation wand, reduce fill/outside alpha to 40% of normal
        // so the delimitation area is visible but non-intrusive. Border stays at full alpha.
        float passiveAlphaMultiplier = isDelimitationWand ? 1f : 0.4f;
        Color outsideColor = (clientConfig?.CanvasOutsideColor ?? new Color(0, 0, 0, 255)) * (outsideA * passiveAlphaMultiplier);
        Color canvasFill   = (clientConfig?.CanvasFillColor ?? new Color(255, 255, 255, 255)) * (canvasA * passiveAlphaMultiplier);
        Color tileSelColor = (clientConfig?.CanvasTileSelectionColor ?? new Color(128, 128, 0, 255)) * (tileSA * passiveAlphaMultiplier);

        var screenBounds = context.ScreenTileBounds;

        // Layer 1 (bottom): Canvas — show the working area
        if (canvasActive)
        {
            // In CanvasEdit mode, use the edit accent color but apply the same
            // configurable alpha slider so the user can control brightness.
            // The accent RGB is premultiplied here (Color * float) just like canvasFill.
            var effectiveFill = (isDelimitationWand && settings.Mode == DelimitationWandMode.CanvasEdit)
                ? DelimitationWandSettings.CanvasEditAccentColor * canvasA
                : canvasFill;
            DrawTileFill(spriteBatch, swp.Canvas.Tiles, screenBounds, effectiveFill);
        }

        // Layer 2 (middle): Outside — dim everything outside the canvas
        // Only drawn when a canvas is active; otherwise it would darken the whole screen.
        if (canvasActive)
            DrawOutsideFill(spriteBatch, swp.Canvas.Tiles, screenBounds, outsideColor);

        // Layer 3 (top): TileSelection — highlight selected tiles
        // Hidden during Canvas Edit mode to reduce visual noise
        if (selectionActive && settings.Mode != DelimitationWandMode.CanvasEdit)
            DrawTileFill(spriteBatch, swp.Selection.Tiles, screenBounds, tileSelColor);

        // Border: Canvas edge segments on top of everything
        if (canvasActive)
            CanvasBorderRenderer.DrawBorder(spriteBatch, swp.Canvas.BorderEdges, screenBounds,
                DelimitationWandSettings.CanvasBorderColor);
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
    /// Used for both canvas fill (Layer 2) and selection fill (Layer 3).
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
}
