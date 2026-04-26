using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Players;

namespace WorldShapingWandsMod.Common.UI;

public class WandSettingsState : UIState
{
    public bool IsVisible { get; set; }
    
    private UIPanel _mainPanel;
    private UIText _titleText;
    
    // Shape buttons
    private UIText _shapeLabel;
    private UIPanel[] _shapeButtons;
    
    // Mode buttons
    private UIText _modeLabel;
    private UIPanel[] _modeButtons;
    
    // Thickness
    private UIText _thicknessLabel;
    private UIText _thicknessValue;

    public override void OnInitialize()
    {
        // Main panel - centered
        _mainPanel = new UIPanel();
        _mainPanel.Width.Set(320f, 0f);
        _mainPanel.Height.Set(280f, 0f);
        _mainPanel.HAlign = 0.5f;
        _mainPanel.VAlign = 0.5f;
        _mainPanel.BackgroundColor = WandPanelTheme.Colors.UniversalPanelBg * 0.9f;
        _mainPanel.BorderColor = WandPanelTheme.Colors.HeaderStripBorder;
        Append(_mainPanel);

        float yOffset = 10f;

        // Title
        _titleText = new UIText("Wand Settings", 0.8f, true);
        _titleText.HAlign = 0.5f;
        _titleText.Top.Set(yOffset, 0f);
        _mainPanel.Append(_titleText);

        yOffset += 40f;

        // Shape Selection
        _shapeLabel = new UIText("Shape:");
        _shapeLabel.Left.Set(10f, 0f);
        _shapeLabel.Top.Set(yOffset, 0f);
        _mainPanel.Append(_shapeLabel);

        yOffset += 25f;
        CreateShapeButtons(yOffset);
        
        yOffset += 45f;

        // Mode Selection
        _modeLabel = new UIText("Mode:");
        _modeLabel.Left.Set(10f, 0f);
        _modeLabel.Top.Set(yOffset, 0f);
        _mainPanel.Append(_modeLabel);

        yOffset += 25f;
        CreateModeButtons(yOffset);

        yOffset += 45f;

        // Thickness
        _thicknessLabel = new UIText("Thickness:");
        _thicknessLabel.Left.Set(10f, 0f);
        _thicknessLabel.Top.Set(yOffset, 0f);
        _mainPanel.Append(_thicknessLabel);

        CreateThicknessControls(yOffset);

        yOffset += 45f;

        // Close button
        var closeButton = new UITextPanel<string>("Close", 0.9f, false);
        closeButton.Width.Set(80f, 0f);
        closeButton.Height.Set(30f, 0f);
        closeButton.HAlign = 0.5f;
        closeButton.Top.Set(yOffset, 0f);
        closeButton.OnLeftClick += (evt, elem) => IsVisible = false;
        closeButton.OnMouseOver += (evt, elem) => ((UIPanel)elem).BackgroundColor = WandPanelTheme.Colors.CloseButtonHover;
        closeButton.OnMouseOut += (evt, elem) => ((UIPanel)elem).BackgroundColor = WandPanelTheme.Colors.CloseButton;
        _mainPanel.Append(closeButton);
    }

    private void CreateShapeButtons(float yOffset)
    {
        string[] shapes = { "Rect", "Ellipse", "Diamond", "Tri", "Elbow", "Simple Line", "Free Line" };
        ShapeType[] shapeTypes = { 
            ShapeType.Rectangle, 
            ShapeType.Ellipse, 
            ShapeType.Diamond, 
            ShapeType.Triangle, 
            ShapeType.Elbow,
            ShapeType.CardinalLine,
            ShapeType.StraightLine 
        };

        _shapeButtons = new UIPanel[shapes.Length];
        float buttonWidth = 55f;
        float spacing = 5f;
        float startX = 10f;

        for (int i = 0; i < shapes.Length; i++)
        {
            int index = i; // Capture for closure
            var btn = new UITextPanel<string>(shapes[i], 0.7f, false);
            btn.Width.Set(buttonWidth, 0f);
            btn.Height.Set(30f, 0f);
            btn.Left.Set(startX + i * (buttonWidth + spacing), 0f);
            btn.Top.Set(yOffset, 0f);
            
            btn.OnLeftClick += (evt, elem) =>
            {
                var settings = Main.LocalPlayer.GetModPlayer<WandPlayer>().Settings;
                settings.ShapeType = shapeTypes[index];
                UpdateButtonStates();
            };
            
            _shapeButtons[i] = btn;
            _mainPanel.Append(btn);
        }
    }

    private void CreateModeButtons(float yOffset)
    {
        string[] modes = { "Filled", "Hollow" };
        ShapeMode[] shapeModes = { ShapeMode.Filled, ShapeMode.Hollow };

        _modeButtons = new UIPanel[modes.Length];
        float buttonWidth = 70f;
        float spacing = 10f;
        float startX = 10f;

        for (int i = 0; i < modes.Length; i++)
        {
            int index = i;
            var btn = new UITextPanel<string>(modes[i], 0.8f, false);
            btn.Width.Set(buttonWidth, 0f);
            btn.Height.Set(30f, 0f);
            btn.Left.Set(startX + i * (buttonWidth + spacing), 0f);
            btn.Top.Set(yOffset, 0f);

            btn.OnLeftClick += (evt, elem) =>
            {
                var settings = Main.LocalPlayer.GetModPlayer<WandPlayer>().Settings;
                settings.ShapeMode = shapeModes[index];
                UpdateButtonStates();
            };

            _modeButtons[i] = btn;
            _mainPanel.Append(btn);
        }
    }

    private void CreateThicknessControls(float yOffset)
    {
        // Minus button
        var minusBtn = new UITextPanel<string>("-", 0.9f, false);
        minusBtn.Width.Set(30f, 0f);
        minusBtn.Height.Set(30f, 0f);
        minusBtn.Left.Set(100f, 0f);
        minusBtn.Top.Set(yOffset, 0f);
        minusBtn.OnLeftClick += (evt, elem) =>
        {
            var settings = Main.LocalPlayer.GetModPlayer<WandPlayer>().Settings;
            settings.Thickness = System.Math.Max(0, settings.Thickness - 1);
            UpdateButtonStates();
        };
        _mainPanel.Append(minusBtn);

        // Value display
        _thicknessValue = new UIText("1", 0.9f, false);
        _thicknessValue.Left.Set(145f, 0f);
        _thicknessValue.Top.Set(yOffset + 5f, 0f);
        _mainPanel.Append(_thicknessValue);

        // Plus button
        var plusBtn = new UITextPanel<string>("+", 0.9f, false);
        plusBtn.Width.Set(30f, 0f);
        plusBtn.Height.Set(30f, 0f);
        plusBtn.Left.Set(175f, 0f);
        plusBtn.Top.Set(yOffset, 0f);
        plusBtn.OnLeftClick += (evt, elem) =>
        {
            var settings = Main.LocalPlayer.GetModPlayer<WandPlayer>().Settings;
            int max = Configs.WandConfigs.Limits?.MaxOutlineThickness ?? 10;
            settings.Thickness = System.Math.Min(max, settings.Thickness + 1);
            UpdateButtonStates();
        };
        _mainPanel.Append(plusBtn);
    }

    private void UpdateButtonStates()
    {
        var settings = Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.Settings;
        if (settings == null) return;

        // Update shape buttons
        ShapeType[] shapeTypes = { 
            ShapeType.Rectangle, 
            ShapeType.Ellipse, 
            ShapeType.Diamond, 
            ShapeType.Triangle, 
            ShapeType.Elbow,
            ShapeType.CardinalLine,
            ShapeType.StraightLine 
        };
        
        for (int i = 0; i < _shapeButtons.Length; i++)
        {
            bool isSelected = settings.ShapeType == shapeTypes[i];
            _shapeButtons[i].BackgroundColor = isSelected 
                ? WandPanelTheme.Colors.CloseButtonHover 
                : WandPanelTheme.Colors.CloseButton * 0.7f;
        }

        // Update mode buttons
        ShapeMode[] shapeModes = { ShapeMode.Filled, ShapeMode.Hollow };
        
        for (int i = 0; i < _modeButtons.Length; i++)
        {
            bool isSelected = settings.ShapeMode == shapeModes[i];
            _modeButtons[i].BackgroundColor = isSelected 
                ? WandPanelTheme.Colors.CloseButtonHover 
                : WandPanelTheme.Colors.CloseButton * 0.7f;
        }

        // Update thickness display
        _thicknessValue?.SetText(settings.Thickness.ToString());
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        
        // Refresh state each frame
        UpdateButtonStates();

        // Prevent clicking through UI
        if (_mainPanel.ContainsPoint(Main.MouseScreen))
        {
            Main.LocalPlayer.mouseInterface = true;
        }

        // Close on Escape
        if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
        {
            IsVisible = false;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;
        base.Draw(spriteBatch);
    }
}