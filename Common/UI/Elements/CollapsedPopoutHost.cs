using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.UI.Elements;

namespace WorldShapingWandsMod.Common.UI.Elements;

public sealed class CollapsedPopoutHost : UIState
{
    private static System.Func<float> _defaultBodyWidthFn  = static () => 200f;
    private static System.Func<float> _defaultBodyHeightFn = static () => 80f;
    private static System.Func<float> _horizontalPaddingFn = static () => 6f;
    private static System.Func<float> _verticalPaddingFn   = static () => 6f;
    private static System.Func<float> _headerHeightFn      = static () => 22f;
    private static System.Func<float> _popoutXOffsetFn     = static () => 0f;
    private static System.Func<float> _popoutYOffsetFn     = static () => 0f;
    private static System.Func<float> _safeSpawnMarginFn   = static () => 16f;

    public static float DefaultBodyWidth  => _defaultBodyWidthFn();
    public static float DefaultBodyHeight => _defaultBodyHeightFn();
    public static float HorizontalPadding => _horizontalPaddingFn();
    public static float VerticalPadding   => _verticalPaddingFn();
    public static float HeaderHeight      => _headerHeightFn();
    public static float PopoutXOffset     => _popoutXOffsetFn();
    public static float PopoutYOffset     => _popoutYOffsetFn();
    public static float SafeSpawnMargin   => _safeSpawnMarginFn();

    internal static void BindTunables(
        System.Func<float> defaultBodyWidthFn,
        System.Func<float> defaultBodyHeightFn,
        System.Func<float> horizontalPaddingFn,
        System.Func<float> verticalPaddingFn,
        System.Func<float> headerHeightFn,
        System.Func<float> popoutXOffsetFn,
        System.Func<float> popoutYOffsetFn,
        System.Func<float> safeSpawnMarginFn = null)
    {
        if (defaultBodyWidthFn  != null) _defaultBodyWidthFn  = defaultBodyWidthFn;
        if (defaultBodyHeightFn != null) _defaultBodyHeightFn = defaultBodyHeightFn;
        if (horizontalPaddingFn != null) _horizontalPaddingFn = horizontalPaddingFn;
        if (verticalPaddingFn   != null) _verticalPaddingFn   = verticalPaddingFn;
        if (headerHeightFn      != null) _headerHeightFn      = headerHeightFn;
        if (popoutXOffsetFn     != null) _popoutXOffsetFn     = popoutXOffsetFn;
        if (popoutYOffsetFn     != null) _popoutYOffsetFn     = popoutYOffsetFn;
        if (safeSpawnMarginFn   != null) _safeSpawnMarginFn   = safeSpawnMarginFn;
    }

    private UIDraggablePanel _panel;
    private UIElement _body;
    private CollapsibleSection _owningSection;
    private UIText _titleText;
    private UIText _closeButton;

    private Terraria.UI.StyleDimension _bodyPreWidth;
    private Terraria.UI.StyleDimension _bodyPreHeight;
    private Terraria.UI.StyleDimension _bodyPreLeft;
    private Terraria.UI.StyleDimension _bodyPreTop;
    private float _bodyPreHAlign;
    private float _bodyPreVAlign;

    public bool IsActive => _body != null;
    public CollapsibleSection ActiveSection => _owningSection;

    /// <summary>
    /// (S6 §3) Smoothed visibility ∈ [0,1]. Driven by
    /// <see cref="SetVisibilityFromPredicate"/> and per-tick easing.
    /// </summary>
    public float VisibilityAlpha { get; private set; } = 1f;

    /// <summary>True iff visible enough to draw.</summary>
    public bool IsCurrentlyVisible => VisibilityAlpha > 0.001f;

    /// <summary>
    /// (S6 §3 bugfix) True when the host needs Update/Draw ticks —
    /// either currently visible OR fading toward visible. Without this,
    /// a fully-faded host can never ease back in because its Update is
    /// gated on <see cref="IsCurrentlyVisible"/> which reads false at α=0.
    /// </summary>
    public bool IsFadingOrVisible => VisibilityAlpha > 0.001f || _visibilityTarget > 0.001f;

    /// <summary>Target visibility; eased toward in Update.</summary>
    private float _visibilityTarget = 1f;

    /// <summary>
    /// Frames of fade duration. 12 @ 60fps = 200ms.
    /// </summary>
    public static int FadeFrames { get; set; } = 12;

    internal void SetVisibilityFromPredicate(bool predicate)
    {
        _visibilityTarget = predicate ? 1f : 0f;
    }

    /// <summary>(S6 §3) Apply current fade alpha to the panel chrome.</summary>
    internal void ApplyFadeAlpha()
    {
        if (_panel != null)
            _panel.DrawAlpha = VisibilityAlpha;
    }

    public override void OnInitialize()
    {
        _panel = new UIDraggablePanel
        {
            BackgroundColor = WandPanelTheme.PanelChrome.PopoutBg,
            BorderColor     = WandPanelTheme.PanelChrome.PopoutBorder,
        };
        _panel.Width.Set(DefaultBodyWidth + HorizontalPadding * 2, 0f);
        _panel.Height.Set(HeaderHeight + VerticalPadding * 2 + DefaultBodyHeight, 0f);
        _panel.HAlign = 0.5f;
        _panel.VAlign = 0.5f;
        Append(_panel);

        _titleText = new UIText("", WandPanelTheme.Fonts.SectionTitleScale);
        _titleText.HAlign = 0f;
        _titleText.VAlign = 0f;
        _titleText.Top.Set(2f, 0f);
        _titleText.Left.Set(4f, 0f);
        _titleText.IgnoresMouseInteraction = true;
        _panel.Append(_titleText);

        _closeButton = new UIText("✕", WandPanelTheme.Fonts.SectionTitleScale);
        _closeButton.HAlign = 1f;
        _closeButton.VAlign = 0f;
        _closeButton.Top.Set(2f, 0f);
        _closeButton.Width.Set(18f, 0f);
        _closeButton.OnLeftClick += (_, _) =>
            Terraria.ModLoader.ModContent.GetInstance<WandUISystem>()?.ClosePopout();
        _panel.Append(_closeButton);
    }

    internal void OpenWith(CollapsibleSection section, UIElement body)
    {
        _owningSection = section;
        _body = body;

        _bodyPreWidth  = body.Width;
        _bodyPreHeight = body.Height;
        _bodyPreLeft   = body.Left;
        _bodyPreTop    = body.Top;
        _bodyPreHAlign = body.HAlign;
        _bodyPreVAlign = body.VAlign;

        _titleText.SetText(Language.GetTextValue(section.TitleKey));

        float bodyW = body.Width.Pixels  > 0f ? body.Width.Pixels  : DefaultBodyWidth;
        float bodyH = body.Height.Pixels > 0f ? body.Height.Pixels : DefaultBodyHeight;

        float tentativeOuterW = bodyW + HorizontalPadding * 2f;
        float tentativeOuterH = HeaderHeight + bodyH + VerticalPadding * 2f;
        _panel.Width .Set(tentativeOuterW, 0f);
        _panel.Height.Set(tentativeOuterH, 0f);
        _panel.Recalculate();

        CalculatedStyle outerDims = _panel.GetDimensions();
        CalculatedStyle innerDims = _panel.GetInnerDimensions();
        float insetL = innerDims.X - outerDims.X;
        float insetT = innerDims.Y - outerDims.Y;
        float insetR = (outerDims.X + outerDims.Width)  - (innerDims.X + innerDims.Width);
        float insetB = (outerDims.Y + outerDims.Height) - (innerDims.Y + innerDims.Height);

        _panel.Width .Set(tentativeOuterW + insetL + insetR, 0f);
        _panel.Height.Set(tentativeOuterH + insetT + insetB, 0f);

        body.Width .Set(bodyW, 0f);
        body.Height.Set(bodyH, 0f);
        body.Left  .Set(0f, 0f);
        body.Top   .Set(HeaderHeight + VerticalPadding, 0f);
        body.HAlign = 0.5f;

        _panel.Append(body);

        var wp = TryGetWandPlayer();
        if (wp != null && wp.PopoutPositions != null
            && wp.PopoutPositions.TryGetValue(section.PreferenceKey, out Vector2 saved))
        {
            _panel.HAlign = 0f;
            _panel.VAlign = 0f;
            _panel.Left.Set(saved.X + PopoutXOffset, 0f);
            _panel.Top .Set(saved.Y + PopoutYOffset, 0f);
        }
        else
        {
            Vector2 spawn = ComputeSafeSpawn(section, _panel.Width.Pixels, _panel.Height.Pixels);
            _panel.HAlign = 0f;
            _panel.VAlign = 0f;
            _panel.Left.Set(spawn.X + PopoutXOffset, 0f);
            _panel.Top .Set(spawn.Y + PopoutYOffset, 0f);
        }
        Recalculate();
    }

    private static Vector2 ComputeSafeSpawn(CollapsibleSection section, float panelW, float panelH)
    {
        float screenW = Main.screenWidth;
        float screenH = Main.screenHeight;
        float margin  = SafeSpawnMargin;

        var parent = FindAncestorPanelDims(section);
        if (parent == null)
        {
            return new Vector2((screenW - panelW) * 0.5f + 40f,
                               (screenH - panelH) * 0.5f);
        }
        var p = parent.Value;

        float xRight = p.X + p.Width + margin;
        if (xRight + panelW <= screenW - margin)
            return new Vector2(xRight, MathHelper.Clamp(p.Y, margin, screenH - panelH - margin));

        float xLeft = p.X - margin - panelW;
        if (xLeft >= margin)
            return new Vector2(xLeft, MathHelper.Clamp(p.Y, margin, screenH - panelH - margin));

        return new Vector2((screenW - panelW) * 0.5f + 40f,
                           MathHelper.Clamp((screenH - panelH) * 0.5f, margin, screenH - panelH - margin));
    }

    private static CalculatedStyle? FindAncestorPanelDims(UIElement node)
    {
        for (UIElement n = node; n != null; n = n.Parent)
            if (n is UIDraggablePanel) return n.GetDimensions();
        return null;
    }

    internal UIElement ReleaseBody()
    {
        PersistPosition();
        UIElement b = _body;
        if (b != null && b.Parent == _panel)
        {
            _panel.RemoveChild(b);
            b.Width  = _bodyPreWidth;
            b.Height = _bodyPreHeight;
            b.Left   = _bodyPreLeft;
            b.Top    = _bodyPreTop;
            b.HAlign = _bodyPreHAlign;
            b.VAlign = _bodyPreVAlign;
        }
        _body = null;
        _owningSection = null;
        return b;
    }

    internal bool DebugIsBodyAttached() => _body != null && _body.Parent == _panel;

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // (S6 §3) Linear ease toward visibility target.
        float step = 1f / System.Math.Max(1, FadeFrames);
        if (VisibilityAlpha < _visibilityTarget)
            VisibilityAlpha = System.Math.Min(_visibilityTarget, VisibilityAlpha + step);
        else if (VisibilityAlpha > _visibilityTarget)
            VisibilityAlpha = System.Math.Max(_visibilityTarget, VisibilityAlpha - step);

        PersistPosition();
    }

    private void PersistPosition()
    {
        if (_owningSection == null) return;
        var wp = TryGetWandPlayer();
        if (wp == null) return;
        wp.PopoutPositions ??= new System.Collections.Generic.Dictionary<string, Vector2>();

        if (_panel.HAlign == 0f && _panel.VAlign == 0f)
        {
            wp.PopoutPositions[_owningSection.PreferenceKey] = new Vector2(_panel.Left.Pixels, _panel.Top.Pixels);
        }
    }

    private static WandPlayer TryGetWandPlayer()
    {
        var p = Main.LocalPlayer;
        if (p == null || !p.active) return null;
        return p.GetModPlayer<WandPlayer>();
    }
}