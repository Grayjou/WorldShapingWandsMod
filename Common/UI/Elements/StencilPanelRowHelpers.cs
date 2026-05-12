using Microsoft.Xna.Framework.Graphics;

namespace WorldShapingWandsMod.Common.UI.Elements;

public static class StencilPanelRowHelpers
{
    public static void AddTransformOptionsRow(
        WandPanelBuilder builder,
        WorldShapingWandsMod mod,
        string headerKey,
        string autoCreateCanvasHoverText,
        string flipHorizontalHoverText,
        string flipVerticalHoverText,
        string rotateCwHoverText,
        string rotateCcwHoverText,
        out UIIconButton autoCreateCanvasBtn,
        out UIIconButton flipHorizontalBtn,
        out UIIconButton flipVerticalBtn,
        out UIIconButton rotateCwBtn,
        out UIIconButton rotateCcwBtn)
    {
        builder.AddSectionHeader(headerKey);

        var texAutoCreateCanvas = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Stencil/AutoCreateCanvas", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        var texFlipHorizontal = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Stencil/FlipHorizontal", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        var texFlipVertical = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Stencil/FlipVertical", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        var texRotateCW = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Stencil/RotateCW", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        var texRotateCCW = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Stencil/RotateCCW", ReLogic.Content.AssetRequestMode.ImmediateLoad);

        builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texAutoCreateCanvas, autoCreateCanvasHoverText, isToggle: true),
            WandPanelBuilder.IconDef.WithText(texFlipHorizontal, flipHorizontalHoverText),
            WandPanelBuilder.IconDef.WithText(texFlipVertical, flipVerticalHoverText),
            WandPanelBuilder.IconDef.WithText(texRotateCW, rotateCwHoverText),
            WandPanelBuilder.IconDef.WithText(texRotateCCW, rotateCcwHoverText),
        }, iconsPerRow: 5, out var buttons);

        autoCreateCanvasBtn = buttons[0];
        flipHorizontalBtn = buttons[1];
        flipVerticalBtn = buttons[2];
        rotateCwBtn = buttons[3];
        rotateCcwBtn = buttons[4];
    }
}