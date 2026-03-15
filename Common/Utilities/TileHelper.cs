using Terraria;
using Terraria.ID;
using Terraria.ObjectData;

namespace WorldShapingWandsMod.Common.Utilities;

/// <summary>
/// Shared tile-related helper methods.
/// </summary>
public static class TileHelper
{
    /// <summary>
    /// Returns <c>true</c> if the given tile type is a wall-anchored object
    /// (torch, candle, banner, painting, etc.).  Removing the wall behind
    /// such an object via <see cref="Terraria.WorldGen.KillWall"/> also
    /// silently destroys the tile object, so callers should skip these
    /// positions when replacing or erasing walls.
    /// </summary>
    public static bool IsWallAnchoredObject(ushort tileType)
    {
        var data = TileObjectData.GetTileData(tileType, 0);
        if (data == null) return false;
        // AnchorWall == true means the tile is attached to the wall behind it.
        return data.AnchorWall;
    }

    /// <summary>
    /// Returns <c>true</c> if the frame-important tile at (x, y) would lose
    /// its support if the wall behind it were removed.  Handles torches,
    /// candles, paintings, banners, and any tile whose <see cref="TileObjectData"/>
    /// specifies <see cref="TileObjectData.AnchorWall"/>.
    /// <para>
    /// Also catches torches and similar tiles that don't set AnchorWall but
    /// are only kept alive by the wall when no adjacent solid block exists.
    /// </para>
    /// </summary>
    public static bool WouldTileLoseSupport(int x, int y)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasTile) return false;

        ushort tileType = tile.TileType;

        // Non-frame-important tiles (solid blocks, etc.) never depend on walls.
        if (!Main.tileFrameImportant[tileType]) return false;

        // Explicit wall anchor — always loses support.
        var data = TileObjectData.GetTileData(tileType, 0);
        if (data != null && data.AnchorWall) return true;

        // Torches: frame-important 1×1 tiles that can anchor to walls.
        // They survive if at least one adjacent block is solid.
        // If none of the 4 cardinal neighbours is a solid block, the wall
        // is what keeps the torch alive → would lose support.
        if (data == null || (data.Width == 1 && data.Height == 1))
        {
            bool hasBlockSupport = false;

            // Check 4 cardinal directions for a solid block
            if (x > 0 && Main.tile[x - 1, y].HasTile && Main.tileSolid[Main.tile[x - 1, y].TileType])
                hasBlockSupport = true;
            else if (x < Main.maxTilesX - 1 && Main.tile[x + 1, y].HasTile && Main.tileSolid[Main.tile[x + 1, y].TileType])
                hasBlockSupport = true;
            else if (y > 0 && Main.tile[x, y - 1].HasTile && Main.tileSolid[Main.tile[x, y - 1].TileType])
                hasBlockSupport = true;
            else if (y < Main.maxTilesY - 1 && Main.tile[x, y + 1].HasTile && Main.tileSolid[Main.tile[x, y + 1].TileType])
                hasBlockSupport = true;

            // If the tile has no solid neighbour, it depends on the wall.
            if (!hasBlockSupport) return true;
        }

        return false;
    }
}
