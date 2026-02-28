using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;

namespace WorldShapingWandsMod.Common.Items;

public abstract class BaseCyclingWand : ModItem
{
    public abstract SelectionMode WandSelectionMode { get; }
    public abstract string WandBaseName { get; }
    public abstract Color ModeColor { get; }
    public abstract int GetNextModeItemType();
    
    public virtual string ModeSuffix => WandSelectionMode switch
    {
        SelectionMode.OneClick => "Instant",
        SelectionMode.TwoClick => "Select", 
        SelectionMode.ThreeClick => "Confirm",
        _ => ""
    };

    public override void SetStaticDefaults()
    {
        // Tooltip is set via localization
    }

    public override void SetDefaults()
    {
        Item.width = 38;
        Item.height = 38;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.UseSound = SoundID.Item1;
        Item.useAnimation = 12;
        Item.useTime = 12;
        Item.channel = true; // Continuous use
        Item.rare = ItemRarityID.Blue;
        Item.autoReuse = false;
        Item.noMelee = true;
    }

    public override bool CanRightClick() => true;

    public override void RightClick(Player player)
    {
        // Check if we should cycle or open settings
        var wandPlayer = player.GetModPlayer<WandPlayer>();
        
        if (wandPlayer.Selection.IsActive)
        {
            // Cancel selection instead of cycling
            wandPlayer.ClearSelection();
            Main.NewText("Selection cancelled.", Color.Yellow);
            Item.stack++; // Prevent consumption
            return;
        }

        // Cycle to next mode
        int nextType = GetNextModeItemType();
        int stack = Item.stack;
        bool wasFavorited = Item.favorited;
        
        Item.SetDefaults(nextType);
        Item.stack = stack + 1; // +1 because right-click consumes one
        Item.favorited = wasFavorited;
        
        Main.NewText($"Switched to {ModeSuffix} mode", ModeColor);
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        var nameLine = tooltips.Find(x => x.Name == "ItemName" && x.Mod == "Terraria");
        if (nameLine != null)
        {
            nameLine.OverrideColor = ModeColor;
        }

        string hexColor = $"{ModeColor.R:X2}{ModeColor.G:X2}{ModeColor.B:X2}";
        
        string modeDescription = WandSelectionMode switch
        {
            SelectionMode.OneClick => "Click and drag to select area",
            SelectionMode.TwoClick => "Click start, then click end",
            SelectionMode.ThreeClick => "Click start, click end, click to confirm",
            _ => ""
        };
        
        tooltips.Add(new TooltipLine(Mod, "ModeInfo", $"[c/{hexColor}:Mode: {ModeSuffix}]"));
        tooltips.Add(new TooltipLine(Mod, "ModeDesc", modeDescription));
        tooltips.Add(new TooltipLine(Mod, "CycleHint", "Right-click in inventory to cycle modes"));
    }
}