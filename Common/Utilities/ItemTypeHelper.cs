using Terraria;
using Terraria.ID;
using WorldShapingWandsMod.Common.Enums;
using System;
using System.Collections.Generic;

namespace WorldShapingWandsMod.Common.Utilities
{
    public static class ItemTypeHelper
    {
        // All grass and moss tile IDs (vanilla + modded automatically via tML)
        private static readonly HashSet<int> GrassSeeds = new()
        {
            TileID.Grass,
            TileID.CorruptGrass,
            TileID.JungleGrass,
            TileID.MushroomGrass,
            TileID.ImmatureHerbs, // herbs are plantable, but we'll keep them here for simplicity
            TileID.HallowedGrass,
            TileID.CrimsonGrass,
            TileID.AshGrass,
            TileID.GreenMoss,
            TileID.BrownMoss,
            TileID.RedMoss,
            TileID.BlueMoss,
            TileID.PurpleMoss,
            TileID.LavaMoss,
            TileID.ArgonMoss,
            TileID.KryptonMoss,
            TileID.XenonMoss,
            TileID.VioletMoss,
            TileID.RainbowMoss
            // Any modded grass seeds will be handled via custom logic if needed, but tML already includes them in Main.tileSolid etc.
        };

        /// <summary>
        /// Returns a predicate that matches items based on the desired place type.
        /// </summary>
        public static Func<Item, bool> GetConditions(PlaceType placeType)
        {
            return placeType switch
            {
                PlaceType.Platform => item =>
                    item.createTile > -1 &&
                    TileID.Sets.Platforms[item.createTile],

                PlaceType.Solid => item =>
                    item.createTile > -1 &&
                    !Main.tileSolidTop[item.createTile] &&   // not a solid-top (like platforms)
                    Main.tileSolid[item.createTile] &&       // is solid
                    !GrassSeeds.Contains(item.createTile) && // exclude grass seeds
                    item.createTile != TileID.ClosedDoor,    // exclude doors (they are not placeable as blocks in one click)

                PlaceType.Rope => item =>
                    item.createTile > -1 &&
                    Main.tileRope[item.createTile],

                PlaceType.Rail => item =>
                    item.createTile == TileID.MinecartTrack,

                PlaceType.GrassSeed => item =>
                    GrassSeeds.Contains(item.createTile),

                PlaceType.PlantPot => item =>
                    item.createTile == TileID.PlanterBox,

                _ => _ => false
            };
        }

        /// <summary>
        /// Finds the first item in the player's inventory that matches the given predicate.
        /// Scans hotbar (0-9) first, then main inventory (10-49).
        /// </summary>
        public static Item FindFirstItem(Player player, Func<Item, bool> condition)
        {
            // Check hotbar first (indices 0-9)
            for (int i = 0; i < 10; i++)
            {
                Item item = player.inventory[i];
                if (!item.IsAir && condition(item))
                    return item;
            }

            // Then check the rest of the inventory (10-49)
            for (int i = 10; i < 50; i++)
            {
                Item item = player.inventory[i];
                if (!item.IsAir && condition(item))
                    return item;
            }

            // Check ammo slots (50-57) if you want, but typically not needed
            for (int i = 50; i < 58; i++)
            {
                Item item = player.inventory[i];
                if (!item.IsAir && condition(item))
                    return item;
            }

            return null;
        }

        /// <summary>
        /// Counts total stacks of items matching the condition, and detects if any are infinite (non-consumable).
        /// </summary>
        public static bool CountItems(Item[] inv, Func<Item, bool> condition, out int total)
        {
            bool infinite = false;
            total = 0;
            foreach (Item item in inv)
            {
                if (item.IsAir || !condition(item)) continue;
                total += item.stack;
                if (!item.consumable) infinite = true;
            }
            return infinite;
        }
    }
}