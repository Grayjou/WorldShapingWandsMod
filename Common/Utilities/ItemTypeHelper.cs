using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ObjectData;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Utilities
{
    public static class ItemTypeHelper
    {
        /// <summary>
        /// Returns true if the item creates a multi-tile object (e.g., Furnace, Bed, Door).
        /// Multi-tile objects require TileObjectData placement logic that bulk wand operations
        /// cannot handle correctly, so they should be skipped.
        /// </summary>
        public static bool IsMultiTileItem(Item item)
        {
            if (item.IsAir || item.createTile < TileID.Dirt) return false;
            var tileData = TileObjectData.GetTileData(item.createTile, item.placeStyle);
            return tileData != null && (tileData.Width > 1 || tileData.Height > 1);
        }

        /// <summary>
        /// Checks whether an existing tile's style matches the expected placeStyle.
        /// For tiles where multiple items share a TileID (platforms, torches, campfires, etc.),
        /// the style is encoded in TileFrameY (or TileFrameX for some tiles).
        /// Returns true if the styles match (tile does NOT need replacement).
        /// </summary>
        public static bool IsSameTileStyle(Tile existingTile, int placeStyle)
        {
            // Guard: if the tile has no TileObjectData, style is always 0
            var data = TileObjectData.GetTileData(existingTile.TileType, 0);
            if (data == null) return placeStyle == 0;

            int existingStyle = TileObjectData.GetTileStyle(existingTile);
            return existingStyle == placeStyle;
        }

        // ────────────────────────────────────────────────────────────
        //  Substrate → Variant Mapping
        // ────────────────────────────────────────────────────────────
        // When replacing "Dirt Block," these variants (grass-covered dirt) should
        // also be recognized as dirt. The mapping is substrate → grown variants.
        //
        // Dirt  → Grass, Corrupt Grass, Hallowed Grass, Crimson Grass, all Moss types
        // Mud   → Jungle Grass, Mushroom Grass
        // Ash   → Ash Grass
        // Stone → Moss-covered stone variants (moss grows on stone surface)
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Maps a substrate TileID to all its grown/variant TileIDs.
        /// For example, Dirt → [Grass, CorruptGrass, HallowedGrass, CrimsonGrass, all Mosses].
        /// </summary>
        private static readonly Dictionary<int, HashSet<int>> SubstrateVariants = new()
        {
            [TileID.Dirt] = new HashSet<int>
            {
                TileID.Grass,
                TileID.CorruptGrass,
                TileID.HallowedGrass,
                TileID.CrimsonGrass,
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
            },
            [TileID.Mud] = new HashSet<int>
            {
                TileID.JungleGrass,
                TileID.MushroomGrass
            },
            [TileID.Ash] = new HashSet<int>
            {
                TileID.AshGrass
            },
            [TileID.Stone] = new HashSet<int>
            {
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
            }
        };

        /// <summary>
        /// Reverse lookup: variant TileID → substrate TileID.
        /// Built lazily from <see cref="SubstrateVariants"/>.
        /// </summary>
        private static Dictionary<int, int> _variantToSubstrate;

        private static Dictionary<int, int> VariantToSubstrate
        {
            get
            {
                if (_variantToSubstrate == null)
                {
                    _variantToSubstrate = new Dictionary<int, int>();
                    foreach (var kvp in SubstrateVariants)
                    {
                        foreach (int variant in kvp.Value)
                        {
                            // If a variant maps to multiple substrates (e.g., Moss → Dirt AND Stone),
                            // prefer the first registration. This is acceptable because the replacement
                            // logic uses GetVariantTileTypes which checks the specific source.
                            _variantToSubstrate.TryAdd(variant, kvp.Key);
                        }
                    }
                }
                return _variantToSubstrate;
            }
        }

        /// <summary>
        /// Returns all TileIDs that should match as "the same block" for replacement purposes.
        /// For example, GetVariantTileTypes(TileID.Dirt) returns {Dirt, Grass, CorruptGrass, ...}.
        /// If the tile has no variants, returns a set containing just the original TileID.
        /// </summary>
        public static HashSet<int> GetVariantTileTypes(int tileType)
        {
            var result = new HashSet<int> { tileType };

            // Direct lookup: tileType is a substrate (e.g., Dirt → add Grass variants)
            if (SubstrateVariants.TryGetValue(tileType, out var variants))
            {
                result.UnionWith(variants);
            }

            // Reverse lookup: tileType is a variant (e.g., Grass → add Dirt + other Dirt variants)
            if (VariantToSubstrate.TryGetValue(tileType, out int substrate))
            {
                result.Add(substrate);
                if (SubstrateVariants.TryGetValue(substrate, out var siblingVariants))
                    result.UnionWith(siblingVariants);
            }

            return result;
        }

        /// <summary>
        /// Checks if worldTileType is a variant of sourceTileType (or the same type).
        /// Equivalent to GetVariantTileTypes(sourceTileType).Contains(worldTileType)
        /// but avoids allocating a HashSet for simple checks.
        /// </summary>
        public static bool IsTileVariantOf(int worldTileType, int sourceTileType)
        {
            if (worldTileType == sourceTileType) return true;

            // Check if worldTileType is a variant of sourceTileType's substrate family
            if (SubstrateVariants.TryGetValue(sourceTileType, out var variants) && variants.Contains(worldTileType))
                return true;

            // Check if both belong to the same substrate family
            if (VariantToSubstrate.TryGetValue(sourceTileType, out int srcSubstrate) &&
                VariantToSubstrate.TryGetValue(worldTileType, out int worldSubstrate) &&
                srcSubstrate == worldSubstrate)
                return true;

            // Check if worldTileType IS the substrate that sourceTileType is a variant of
            if (VariantToSubstrate.TryGetValue(sourceTileType, out int sub) && worldTileType == sub)
                return true;

            return false;
        }

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

                PlaceType.Wall => item =>
                    item.createWall > 0,

                _ => _ => false
            };
        }

        /// <summary>
        /// Finds the first item in the player's inventory that matches the given predicate.
        /// Scans hotbar (0-9) first, then main inventory (10-49).
        /// </summary>
        public static Item FindFirstItem(Player player, Func<Item, bool> condition)
            => FindFirstItem(player, condition, null);

        /// <summary>
        /// Choice-aware variant. If <paramref name="chosenItemType"/> is non-null, the
        /// chosen item type is preferred whenever it exists in inventory AND
        /// satisfies <paramref name="condition"/>. If the choice is missing or no
        /// longer matches (player consumed all of it, or switched modes), falls
        /// back to the broad scan exactly as the legacy overload does.
        /// <para>Used by InventoryView (S6 2026-04-22) so a chosen wall / tile /
        /// torch / replacement source/target survives execution and is honored
        /// before the inventory's natural scan order.</para>
        /// </summary>
        public static Item FindFirstItem(Player player, Func<Item, bool> condition, int? chosenItemType)
        {
            // 1) Honor the choice when present and still valid.
            if (chosenItemType.HasValue && chosenItemType.Value > 0)
            {
                int choice = chosenItemType.Value;
                for (int i = 0; i < 58; i++)
                {
                    Item it = player.inventory[i];
                    if (!it.IsAir && it.type == choice && condition(it))
                        return it;
                }
                // Fall through: choice is stale, run the normal scan.
            }

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
        /// Finds the first inventory index that matches the condition.
        /// Scans hotbar (0-9), main inventory (10-49), then ammo slots (50-57).
        /// Returns -1 if none found.
        /// </summary>
        public static int FindFirstItemIndex(Player player, Func<Item, bool> condition)
            => FindFirstItemIndex(player, condition, null);

        /// <summary>
        /// Choice-aware variant. If <paramref name="chosenItemType"/> is non-null and
        /// that item exists in inventory AND satisfies <paramref name="condition"/>,
        /// returns its index. Otherwise falls back to the broad scan exactly as the
        /// legacy overload. Used by InventoryView (S6 2026-04-22).
        /// </summary>
        public static int FindFirstItemIndex(Player player, Func<Item, bool> condition, int? chosenItemType)
        {
            if (chosenItemType.HasValue && chosenItemType.Value > 0)
            {
                int choice = chosenItemType.Value;
                for (int i = 0; i < 58; i++)
                {
                    Item it = player.inventory[i];
                    if (!it.IsAir && it.type == choice && condition(it))
                        return i;
                }
                // Fall through.
            }

            for (int i = 0; i < 10; i++)
            {
                Item item = player.inventory[i];
                if (!item.IsAir && condition(item))
                    return i;
            }

            for (int i = 10; i < 50; i++)
            {
                Item item = player.inventory[i];
                if (!item.IsAir && condition(item))
                    return i;
            }

            for (int i = 50; i < 58; i++)
            {
                Item item = player.inventory[i];
                if (!item.IsAir && condition(item))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Consumes up to <paramref name="amount"/> items matching <paramref name="condition"/>.
        /// Returns true when the amount is fully satisfied. Non-consumable matching items are treated as infinite.
        /// </summary>
        public static bool ConsumeItems(Item[] inv, Func<Item, bool> condition, int amount)
        {
            if (amount <= 0)
                return true;

            foreach (Item item in inv)
            {
                if (item.IsAir || !condition(item)) continue;
                if (!item.consumable) return true;
            }

            int remaining = amount;
            foreach (Item item in inv)
            {
                if (item.IsAir || !condition(item)) continue;

                int take = Math.Min(item.stack, remaining);
                item.stack -= take;
                remaining -= take;

                if (item.stack <= 0)
                    item.TurnToAir();

                if (remaining <= 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a predicate that matches inventory items based on the desired ObjectType.
        /// </summary>
        public static Func<Item, bool> GetConditions(ObjectType objectType)
        {
            return objectType switch
            {
                ObjectType.Tile => item =>
                    item.createTile > -1 &&
                    !Main.tileSolidTop[item.createTile] &&
                    Main.tileSolid[item.createTile] &&
                    !GrassSeeds.Contains(item.createTile) &&
                    item.createTile != TileID.ClosedDoor,

                ObjectType.Platform => item =>
                    item.createTile > -1 &&
                    TileID.Sets.Platforms[item.createTile],

                ObjectType.Rope => item =>
                    item.createTile > -1 &&
                    Main.tileRope[item.createTile],

                ObjectType.PlanterBox => item =>
                    item.createTile == TileID.PlanterBox,

                ObjectType.Rail => item =>
                    item.createTile == TileID.MinecartTrack,

                ObjectType.Seeds => item =>
                    GrassSeeds.Contains(item.createTile),

                ObjectType.Air => _ => false, // Air has no matching item

                ObjectType.Wall => item =>
                    item.createWall > 0,

                _ => _ => false
            };
        }

        /// <summary>
        /// Checks whether a placed world tile matches the given ObjectType category.
        /// </summary>
        public static bool WorldTileMatchesObjectType(Terraria.Tile tile, ObjectType objectType)
        {
            if (!tile.HasTile)
                return objectType == ObjectType.Air;

            int type = tile.TileType;
            return objectType switch
            {
                ObjectType.Tile =>
                    Main.tileSolid[type] &&
                    !Main.tileSolidTop[type] &&
                    !GrassSeeds.Contains(type) &&
                    type != TileID.ClosedDoor,

                ObjectType.Platform =>
                    TileID.Sets.Platforms[type],

                ObjectType.Rope =>
                    Main.tileRope[type],

                ObjectType.PlanterBox =>
                    type == TileID.PlanterBox,

                ObjectType.Rail =>
                    type == TileID.MinecartTrack,

                ObjectType.Seeds =>
                    GrassSeeds.Contains(type),

                ObjectType.Air => false, // tile.HasTile is true here so it's not Air

                ObjectType.Wall => false, // Wall matching uses WallType, not TileType

                _ => false
            };
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