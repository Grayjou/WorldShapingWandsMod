using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.UI;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// The Color Replace action button (Wand of Coatings, S8 2026-04-28;
/// reworked S12 2026-04-28). Renders one of four base PNGs with a
/// runtime palette swap on the white/black template pixels (white
/// &rarr; source-colour, black &rarr; target-colour, green arrow + any
/// 128/192-grey checker pixels preserved verbatim).
/// </summary>
/// <remarks>
/// <para><b>Asset family (S12 architecture)</b>: four PNGs, each a
/// FULL-icon variant of <c>ModePaintReplace.png</c>:</para>
/// <list type="bullet">
///   <item><c>ModePaintReplace.png</c> — base; white left + black right.</item>
///   <item><c>ModePaintReplaceIgnoreSource.png</c> — checker LEFT + black right.</item>
///   <item><c>ModePaintReplaceIgnoreTarget.png</c> — white LEFT + checker right.</item>
///   <item><c>ModePaintReplaceIgnoreBoth.png</c> — checker both sides.</item>
/// </list>
///
/// <para><b>Design correction (S12; GrayJou worried-client review)</b>:
/// the original S8/S10 plan called these "overlays" and drew them OVER
/// the bake. The shipped art is actually full-icon variants — drawing
/// them over the bake replaces the tinted side with the asset's
/// untinted white/black template, producing the S11 ship's
/// *"left becomes white when target is Ignore"* / *"right becomes black
/// when source is Ignore"* defects. The S12 fix is purely code: pick
/// the matching variant per (sourceIgnoreLike, targetIgnoreLike) pair
/// and use it AS THE BAKE BASE. The checker pixels (128 / 192 grey) sit
/// outside the &lt;= 16 dark / &gt;= 240 light thresholds, so they fall
/// through the bake's *"preserve verbatim"* branch and render as the
/// authored checker pattern. No overlay overdraw needed.</para>
///
/// <para><b>None == Ignore on the icon (S12; GrayJou worried-client
/// review)</b>: <c>PaintID.None</c> (0) is now treated as Ignore for
/// purposes of base-variant selection. Per S12 verbatim:
/// *"None and Ignore should have the same effect on the icon, which is
/// to have the checkerboard pattern."* The settings layer continues to
/// distinguish None vs Ignore for the operation semantics; only the
/// icon collapses them.</para>
///
/// <para><b>Bake pipeline</b>: <see cref="Texture2D.GetData{T}(T[])"/>
/// on each of the four bases once, lifetime-cached as a 4-entry array.
/// <see cref="RebakeIfNeeded"/> runs whenever the (sourcePaint,
/// targetPaint) tuple changes, writing tinted pixels into a re-used
/// <see cref="Color"/>[] buffer and pushing via
/// <see cref="Texture2D.SetData{T}(T[])"/> on a per-instance baked
/// <see cref="Texture2D"/>.</para>
///
/// <para><b>Resource lifetime</b>: the per-instance baked
/// <see cref="Texture2D"/> is allocated lazily on first paint and
/// disposed in <see cref="OnDeactivate"/>. Per ColorReplacePlan.md
/// &sect;0.5.1 "dispose hygiene" bookkeeping cost.</para>
/// </remarks>
public class UIColorReplaceButton : UIElement
{
    /// <summary>Sentinel value matching <c>WandOfCoatingBase.IgnorePaintColor</c>.</summary>
    public const byte IgnoreSentinel = 255;

    /// <summary>
    /// (S12 2026-04-28; GrayJou worried-client review) <c>PaintID.None</c>
    /// (the *NoPaint* slot, byte 0) is treated as Ignore for icon-rendering
    /// purposes only. Settings semantics still distinguish None vs Ignore;
    /// this helper exists solely so the icon family selection collapses
    /// them to the same checker-base variant.
    /// </summary>
    private static bool IsIgnoreLike(byte paint) => paint == IgnoreSentinel || paint == 0;

    // (S12) Base variant indices into _baseAssets / _basePixelsByVariant.
    // Order matters — ChooseVariant returns one of these.
    private const int VariantBase         = 0; // ModePaintReplace
    private const int VariantIgnoreSource = 1; // ModePaintReplaceIgnoreSource
    private const int VariantIgnoreTarget = 2; // ModePaintReplaceIgnoreTarget
    private const int VariantIgnoreBoth   = 3; // ModePaintReplaceIgnoreBoth

    private readonly Asset<Texture2D>[] _baseAssets;

    // (S12) Lazily-loaded CPU pixel buffer per variant, indexed by Variant*.
    private Color[][] _basePixelsByVariant;
    // Per-instance baked texture (re-uploaded after each rebake).
    private Texture2D _bakedTex;
    // Re-used scratch buffer for SetData; matches base width*height.
    private Color[] _scratchPixels;
    private int _baseW, _baseH;

    // Cache key for the bake. Includes the chosen variant index so a
    // None↔Ignore swap (same byte? no, different bytes — 0 vs 255) still
    // forces a rebake even though the visual is identical — cheap and
    // simpler than a custom equivalence relation.
    private byte _bakedSource = 1; // intentionally not 255 so the first paint always rebakes
    private byte _bakedTarget = 1;
    private int _bakedVariant = -1;

    /// <summary>Current source paint (0 = NoPaint, 1..30 = vanilla, 255 = Ignore).</summary>
    public byte SourcePaint { get; set; } = IgnoreSentinel;

    /// <summary>Current target paint (same encoding as <see cref="SourcePaint"/>).</summary>
    public byte TargetPaint { get; set; } = IgnoreSentinel;

    /// <summary>
    /// Tooltip text shown on hover. Updated by the panel whenever
    /// source / target / channel changes. Empty = no tooltip.
    /// </summary>
    public string HoverText { get; set; } = string.Empty;

    /// <summary>
    /// When true (default), the button is interactive. When false, clicks are
    /// ignored. (S11 2026-04-28; GrayJou worried-client review) The visual
    /// dimming previously paired with this flag was retired — the green
    /// background already conveys mode/picker state, and reduced-alpha icons
    /// confused the player into thinking the action was unavailable when in
    /// fact only the *selection* was empty. Per S11 verbatim: *“we don't
    /// have to grey or reduce the alpha of the button because the green icon
    /// background already works. No greying always full alpha.”* The flag
    /// remains as a behavioural gate (callers may still set it false to
    /// disable clicks) but the renderer no longer changes alpha based on it.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// (S11 2026-04-28) True when the wand's CoatingMode is currently
    /// <c>ColorReplace</c> — i.e. this button is the active radio member of
    /// the Mode row. Drives the ActiveGreen background regardless of hover
    /// or SubUI-open state, matching the convention of every other Mode
    /// toggle in the same row.
    /// </summary>
    public bool Toggled { get; set; }

    /// <summary>
    /// (S10 2026-04-28; GrayJou worried-client review) ORIGINALLY: True while
    /// the associated config SubUI is currently open and anchored to this
    /// button — used to force the ActiveGreen chrome regardless of hover.
    ///
    /// (S10 2026-04-29; GrayJou worried-client clarification) SUPERSEDED.
    /// Per the S10 prompt verbatim: <i>“Whether or not the Replace Color
    /// SubPanel is on, doesn't affect it. The icon pretty much follows the
    /// other PaintMode icon rules.”</i> The background is now driven solely
    /// by <see cref="Toggled"/> + hover (mirroring <see cref="UIIconButton"/>
    /// — see <see cref="DrawSelf"/>). This field is preserved as a no-op
    /// public surface so the three existing call-sites in
    /// <c>CoatingSettingsPanel</c> (open/close/escape) keep compiling without
    /// churn; it could be retired in a future cleanup.
    /// </summary>
    public bool IsWandSubPanelOpen { get; set; }

    /// <summary>Background color when not hovered.</summary>
    public Color InactiveColor { get; set; } = WandPanelTheme.Colors.ElementInactive;

    /// <summary>Background color when hovered.</summary>
    public Color HoverColor { get; set; } = WandPanelTheme.Colors.ActiveGreen;

    /// <summary>
    /// Constructs the Color Replace button. The four base assets are
    /// stored at <see cref="VariantBase"/> / <see cref="VariantIgnoreSource"/> /
    /// <see cref="VariantIgnoreTarget"/> / <see cref="VariantIgnoreBoth"/>
    /// indices. Parameter names retain the legacy *"overlay"* wording for
    /// caller-side compatibility, but per the S12 architecture they're
    /// FULL-icon bake bases now — see the class docblock.
    /// </summary>
    public UIColorReplaceButton(
        Asset<Texture2D> baseAsset,
        Asset<Texture2D> ignoreSourceOverlay,
        Asset<Texture2D> ignoreTargetOverlay,
        Asset<Texture2D> ignoreBothOverlay)
    {
        if (baseAsset is null)            throw new ArgumentNullException(nameof(baseAsset));
        if (ignoreSourceOverlay is null)  throw new ArgumentNullException(nameof(ignoreSourceOverlay));
        if (ignoreTargetOverlay is null)  throw new ArgumentNullException(nameof(ignoreTargetOverlay));
        if (ignoreBothOverlay is null)    throw new ArgumentNullException(nameof(ignoreBothOverlay));

        _baseAssets = new Asset<Texture2D>[4];
        _baseAssets[VariantBase]         = baseAsset;
        _baseAssets[VariantIgnoreSource] = ignoreSourceOverlay;
        _baseAssets[VariantIgnoreTarget] = ignoreTargetOverlay;
        _baseAssets[VariantIgnoreBoth]   = ignoreBothOverlay;
    }

    /// <summary>
    /// (S12) Selects the bake-base variant for the current
    /// (<see cref="SourcePaint"/>, <see cref="TargetPaint"/>) pair, with
    /// <c>None == Ignore</c> per <see cref="IsIgnoreLike"/>.
    /// </summary>
    private int ChooseVariant()
    {
        bool s = IsIgnoreLike(SourcePaint);
        bool t = IsIgnoreLike(TargetPaint);
        if (s && t) return VariantIgnoreBoth;
        if (s)      return VariantIgnoreSource;
        if (t)      return VariantIgnoreTarget;
        return VariantBase;
    }

    /// <summary>
    /// Lazily initialises the per-variant CPU pixel caches + the
    /// per-instance baked <see cref="Texture2D"/>. Idempotent and safe to
    /// call every paint. All four variants are read up front (one-time
    /// cost; 4 × 32×32 × 4 bytes = 16 KiB total) so a paint flip never
    /// needs to reach for the GPU mid-draw.
    /// </summary>
    private void EnsureBakeResources()
    {
        if (_basePixelsByVariant != null && _bakedTex != null)
            return;

        // All four assets must be ready; if any is still loading, try next frame.
        for (int i = 0; i < _baseAssets.Length; i++)
        {
            if (_baseAssets[i].Value == null)
                return;
        }

        var baseTex = _baseAssets[VariantBase].Value;
        _baseW = baseTex.Width;
        _baseH = baseTex.Height;

        _basePixelsByVariant = new Color[_baseAssets.Length][];
        for (int i = 0; i < _baseAssets.Length; i++)
        {
            var tex = _baseAssets[i].Value;
            // All four variants are authored at the same dimensions — they're
            // siblings in the asset family. Defensive guard kept inexpensive.
            if (tex.Width != _baseW || tex.Height != _baseH)
            {
                throw new InvalidOperationException(
                    $"ColorReplace asset family size mismatch: variant {i} is "
                    + $"{tex.Width}x{tex.Height}, expected {_baseW}x{_baseH}.");
            }
            var buf = new Color[_baseW * _baseH];
            tex.GetData(buf);
            _basePixelsByVariant[i] = buf;
        }
        _scratchPixels = new Color[_baseW * _baseH];

        // Allocate the per-instance render target. Disposed in OnDeactivate.
        _bakedTex = new Texture2D(Main.graphics.GraphicsDevice, _baseW, _baseH, false, SurfaceFormat.Color);

        // Force a rebake on the next call.
        _bakedSource = (byte)(SourcePaint ^ 0x55);
        _bakedTarget = (byte)(TargetPaint ^ 0x55);
        _bakedVariant = -1;
    }

    /// <summary>
    /// Re-bakes the action-button texture if the (source, target, variant)
    /// triple has changed since the last bake. The variant is derived from
    /// (source, target) via <see cref="ChooseVariant"/>, so the cache key
    /// covers all 4 × 32 × 32 cells of the input space exactly.
    /// </summary>
    /// <remarks>
    /// (S12) The chosen variant supplies the bake's source pixel array.
    /// Checker pixels (128 / 192 grey) sit between the LightThreshold (240)
    /// and DarkThreshold (16) bands and so fall through the *"preserve
    /// verbatim"* branch — they render unchanged and produce the visible
    /// checker pattern on the ignored side(s). White template pixels on the
    /// non-ignored side get the source-tint multiply; black template pixels
    /// get the target-tint replacement. The arrow + green chrome is also
    /// preserved verbatim by the same threshold bands.
    /// </remarks>
    private void RebakeIfNeeded()
    {
        if (_basePixelsByVariant == null || _bakedTex == null)
            return;

        int variant = ChooseVariant();
        if (_bakedSource == SourcePaint && _bakedTarget == TargetPaint && _bakedVariant == variant)
            return;

        Color[] basePixels = _basePixelsByVariant[variant];

        // For an ignore-like side the tint is irrelevant (the chosen variant's
        // checker pixels cover that half and fall through *"preserve verbatim"*).
        // Use White as a passthrough so even if a template pixel slipped through
        // in the asset, the bake stays visually neutral rather than shifted.
        Color sourceTint = IsIgnoreLike(SourcePaint) ? Color.White : PaintPalette.GetColor(SourcePaint);
        Color targetTint = IsIgnoreLike(TargetPaint) ? Color.White : PaintPalette.GetColor(TargetPaint);

        // Tolerance band catches anti-aliased white/black pixels at the arrow's
        // edges (ColorReplacePlan.md §0.5.1 "anti-alias / edge-pixel fidelity").
        const byte LightThreshold = 240;
        const byte DarkThreshold  = 16;

        for (int i = 0; i < basePixels.Length; i++)
        {
            Color p = basePixels[i];
            // Preserve fully transparent pixels exactly.
            if (p.A == 0)
            {
                _scratchPixels[i] = p;
                continue;
            }

            bool isLight = p.R >= LightThreshold && p.G >= LightThreshold && p.B >= LightThreshold;
            bool isDark  = p.R <= DarkThreshold  && p.G <= DarkThreshold  && p.B <= DarkThreshold;

            if (isLight)
            {
                // Multiply alpha by tint so anti-aliased edges fade naturally.
                _scratchPixels[i] = new Color(
                    (byte)(sourceTint.R * p.R / 255),
                    (byte)(sourceTint.G * p.G / 255),
                    (byte)(sourceTint.B * p.B / 255),
                    p.A);
            }
            else if (isDark)
            {
                // For black source pixels we cannot multiply (anything * 0 = 0),
                // so we replace with the target tint, modulated by the original
                // alpha to preserve edges.
                _scratchPixels[i] = new Color(targetTint.R, targetTint.G, targetTint.B, p.A);
            }
            else
            {
                // Arrow + chrome + checker greys — preserve verbatim.
                _scratchPixels[i] = p;
            }
        }

        _bakedTex.SetData(_scratchPixels);
        _bakedSource = SourcePaint;
        _bakedTarget = TargetPaint;
        _bakedVariant = variant;
    }

    public override void OnDeactivate()
    {
        // Per ColorReplacePlan.md §0.5.1 "dispose hygiene".
        if (_bakedTex != null)
        {
            _bakedTex.Dispose();
            _bakedTex = null;
        }
        _basePixelsByVariant = null;
        _scratchPixels = null;
        base.OnDeactivate();
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        if (!IsActive)
            return;
        SoundEngine.PlaySound(SoundID.MenuTick);
        base.LeftClick(evt);
    }

    public override void RightClick(UIMouseEvent evt)
    {
        // Always allow right-click (configuration entry point) even when
        // IsActive is false, so the player can reach the picker before
        // switching to a paint mode. Tooltip explains the inactive state.
        SoundEngine.PlaySound(SoundID.MenuOpen);
        base.RightClick(evt);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        EnsureBakeResources();
        RebakeIfNeeded();

        var dims = GetDimensions();
        var rect = dims.ToRectangle();

        // Background chrome — canonical PaintMode tri-state.
        //
        // (S10 2026-04-29; GrayJou worried-client clarification, verbatim)
        // Spec for this button matches every other PaintMode icon in the row:
        //   Unhovered & Unselected → dark gray  (ElementInactive)
        //   Hovered  & Unselected → light gray  (lerp(Inactive, White, 0.2))
        //   Selected (Toggled)    → green       (ActiveGreen);
        //                            hover lerps lightly toward white
        // Whether or not the Color Replace SubPanel is currently open MUST
        // NOT affect this background — the radio-member Toggled bit is the
        // single source of truth for the green active-state, identical to
        // PaintTile / PaintWall / ScrapeMoss / HarvestMoss in this row.
        //
        // Why this differs from the previous (S10 2026-04-28 → S8 2026-04-29)
        // implementation: that version OR'd in subPanelOpenLive so the chrome
        // would stay green while the SubUI was anchored. GrayJou's worried-
        // client S10 2026-04-29 review reverted that: the SubUI's own visible
        // panel already conveys *“my picker is up”*, and double-signalling on
        // the radio cell broke the visual radio-row consistency.
        bool hovering = IsMouseHovering && IsActive;
        Color bg = Toggled ? HoverColor : InactiveColor;
        if (hovering)
            bg = Color.Lerp(bg, Color.White, 0.2f);
        DrawBackground(spriteBatch, rect, bg);

        // Centre the icon within the cell, preserving the asset's native size.
        if (_bakedTex != null)
        {
            int iconW = _baseW;
            int iconH = _baseH;
            int x = rect.X + (rect.Width - iconW) / 2;
            int y = rect.Y + (rect.Height - iconH) / 2;
            var iconRect = new Rectangle(x, y, iconW, iconH);

            // (S12) Single draw — the bake already encodes the correct base
            // variant per the (sourceIgnoreLike, targetIgnoreLike) pair.
            // The S8–S11 overlay overdraw was retired because the shipped
            // "overlay" PNGs are actually full-icon variants; drawing them
            // OVER the bake replaced the tinted side with the asset's
            // untinted white/black template (the S11 "left becomes white /
            // right becomes black" defect). See class docblock.
            spriteBatch.Draw(_bakedTex, iconRect, Color.White);
        }

        if (hovering && !string.IsNullOrEmpty(HoverText))
        {
            Main.instance.MouseText(HoverText);
        }
    }

    /// <summary>
    /// Mirrors <see cref="UIIconButton"/>'s 1px-edge backdrop look so the
    /// button fits visually beside the existing icon-row buttons.
    /// </summary>
    private static void DrawBackground(SpriteBatch sb, Rectangle rect, Color color)
    {
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        sb.Draw(pixel, rect, color);
    }

    // (S10 2026-04-29) The S8 2026-04-29 live-poll helper
    // `HasOpenSubPanelAnchoredHere` was retired alongside the subPanelOpenLive
    // gate in DrawSelf — the SubPanel-open state no longer drives the icon
    // background per GrayJou's S10 worried-client clarification. Removed to
    // keep the surface honest; archaeology lives in this file's git history
    // and in `Sessions/Implemented_2026-04-29-2.md` S10 entry.
}
