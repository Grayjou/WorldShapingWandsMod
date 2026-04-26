using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Settings;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items
{
    /// <summary>
    /// Abstract base class for all Building wand variants (Instant, Select, Confirm, Stamp).
    /// Provides shared item behavior: UseItem dispatching, HoldItem cursor icon, right-click
    /// cancel/UI toggle, and recipe registration.
    /// <para>
    /// Execution logic is split into partial class files:
    /// <list type="bullet">
    ///   <item><c>WandOfBuildingBase.TileExecution.cs</c> - Tile placement (progressive + instant)</item>
    ///   <item><c>WandOfBuildingBase.WallExecution.cs</c> - Wall placement (progressive + instant)</item>
    ///   <item><c>WandOfBuildingBase.Helpers.cs</c> - Shared helpers (slopes, paint, actuation, consumption)</item>
    /// </list>
    /// </para>
    /// </summary>
    public abstract partial class WandOfBuildingBase : BaseCyclingWand
    {
        public override string Texture => $"WorldShapingWandsMod/Content/Items/WorldShaping/Building/{Name}";
        public override string WandBaseName => "Wand of Building";
        public override string WandLore => Get("LoreBuilding");

        // ── Template Method Pattern ────────────────────────────────────────
        protected override WandFamily Family => WandFamily.Building;
        protected override bool UsesTemplateModeDispatch => true;

        // ── WandActionProjectile opt-in ────────────────────────────────────
        protected override bool UseWandActionProjectile => true;

        protected override WandAction ResolveCurrentAction()
        {
            var wandPlayer = Main.LocalPlayer.GetModPlayer<WandPlayer>();
            return wandPlayer.BuildingSettings.Object switch
            {
                PlaceType.Solid     => WandAction.BuildingSolid,
                PlaceType.Wall      => WandAction.BuildingWalls,
                PlaceType.Platform  => WandAction.BuildingPlatforms,
                PlaceType.Rope      => WandAction.BuildingRope,
                PlaceType.GrassSeed => WandAction.BuildingGrassSeeds,
                PlaceType.Rail      => WandAction.BuildingTracks,
                PlaceType.PlantPot  => WandAction.BuildingPlanterBox,
                PlaceType.Torch     => WandAction.BuildingSolid,
                _                   => WandAction.BuildingSolid,
            };
        }

        /// <inheritdoc />
        protected override Recipe AddInstantRecipeShimmerResults(Recipe recipe)
            => recipe
                .AddCustomShimmerResult(ItemID.Wood, 10)
                .AddCustomShimmerResult(ItemID.GrayBrick, 10)
                .AddCustomShimmerResult(ItemID.RedBrick, 10)
                .AddCustomShimmerResult(ItemID.Rope, 20)
                .AddCustomShimmerResult(ItemID.ManaCrystal, 1);

        protected override void ExecuteWandOperation(Player player, WandPlayer wandPlayer)
            => ExecuteBuilding(player, wandPlayer);

        protected override ShapeInfo GetWandShape(WandPlayer wandPlayer)
            => wandPlayer.BuildingSettings.Shape;

        protected override void CancelActiveSelection(Player player, WandPlayer wandPlayer)
        {
            wandPlayer.CancelSelection(GetCancelColor(), GetWandShape(wandPlayer));
        }

        protected override void OnHoldItemFamily(Player player, WandPlayer wandPlayer)
        {
            // Display cursor item icon for the block that will be placed.
            //
            // S9 2026-04-22 (Cavendish Patch_BuildingCursorPreviewChoice): pass the active
            // choice so the cursor preview matches what ExecuteBuilding/ExecuteWallBuilding
            // will actually place. Pre-S9 this used the no-choice overload, so clicking a
            // choice in InventoryView correctly updated ChosenTileItemType/ChosenWallItemType
            // and the placement honored it, but the cursor icon kept showing the broad-scan
            // first match — making the user think the choice was inert. Wall mode reads
            // ChosenWallItemType; everything else reads ChosenTileItemType, mirroring the
            // mode-keyed pickup in TileExecution.cs / WallExecution.cs. Stale-choice fallback
            // is handled inside FindFirstItem.
            var settings = wandPlayer.BuildingSettings;
            var condition = ItemTypeHelper.GetConditions(settings.Object);
            int? choice = settings.Object == PlaceType.Wall
                ? settings.ChosenWallItemType
                : settings.ChosenTileItemType;
            Item blockItem = ItemTypeHelper.FindFirstItem(player, condition, choice);
            if (blockItem != null)
            {
                player.cursorItemIconEnabled = true;
                player.cursorItemIconID = blockItem.type;
                player.cursorItemIconPush = 26;
            }
        }

        public override bool? UseItem(Player player)
        {
            return TemplateUseItem(player);
        }

        public override void HoldItem(Player player)
        {
            TemplateHoldItem(player);
        }

        public override void AddRecipes()
        {
            // Only the Instant variant has a craftable recipe.
            // Other modes are obtained via right-click cycling in inventory.
        }
    }
}
