using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace WorldShapingWandsMod.Common.Utilities;

/// <summary>
/// Provides batched tile frame updates and network synchronization
/// to avoid per-tile overhead. Inspired by Cheat Sheet's PaintToolsHotbar approach.
/// 
/// <para>Instead of calling <c>WorldGen.SquareTileFrame</c> and <c>NetMessage.SendTileSquare</c>
/// for each individual tile during an operation, callers should:</para>
/// <list type="number">
///   <item>Perform all tile mutations (place/kill/replace) without sending network messages.</item>
///   <item>Call <see cref="BatchFrameUpdate"/> over the affected area.</item>
///   <item>Call <see cref="BatchNetworkSync"/> to send a single network packet for the region.</item>
/// </list>
/// </summary>
public static class BulkTileOperations
{
    /// <summary>
    /// Computes the axis-aligned bounding box of a set of tile positions.
    /// Returns <see cref="Rectangle.Empty"/> when the collection is empty.
    /// </summary>
    public static Rectangle ComputeBounds(IEnumerable<Point> positions)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        bool any = false;

        foreach (var p in positions)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
            any = true;
        }

        if (!any) return Rectangle.Empty;
        return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    /// <summary>
    /// Runs <c>WorldGen.SquareTileFrame</c> for every tile in the bounding rectangle,
    /// plus a 1-tile border to ensure neighboring tiles update their frames correctly.
    /// Also updates wall frames for any walls present.
    /// </summary>
    /// <remarks>
    /// This mirrors Cheat Sheet's post-stamp frame update loop, but also handles walls
    /// (which Cheat Sheet omits, causing wall merging bugs — see Issue #19).
    /// </remarks>
    public static void BatchFrameUpdate(Rectangle bounds)
    {
        if (bounds.IsEmpty) return;

        // Expand by 1 tile on each side so border neighbors re-frame correctly
        int startX = Math.Max(0, bounds.X - 1);
        int startY = Math.Max(0, bounds.Y - 1);
        int endX = Math.Min(Main.maxTilesX - 1, bounds.X + bounds.Width);
        int endY = Math.Min(Main.maxTilesY - 1, bounds.Y + bounds.Height);

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                WorldGen.SquareTileFrame(x, y, resetFrame: false);

                if (Main.tile[x, y].WallType > WallID.None)
                    Framing.WallFrame(x, y, resetFrame: true);
            }
        }
    }

    /// <summary>
    /// Sends a single batched network sync packet covering the bounding rectangle.
    /// Only sends when <c>Main.netMode == NetmodeID.MultiplayerClient</c>.
    /// 
    /// <para>For very large areas, the packet is automatically split into chunks
    /// of at most <see cref="MaxSyncChunkSize"/>×<see cref="MaxSyncChunkSize"/> tiles
    /// to avoid exceeding Terraria's network buffer limits.</para>
    /// </summary>
    public static void BatchNetworkSync(Rectangle bounds)
    {
        if (bounds.IsEmpty) return;
        if (Main.netMode != NetmodeID.MultiplayerClient) return;

        // Terraria's SendTileSquare uses a square centered at (x,y) with half-side = size/2.
        // For regions larger than MaxSyncChunkSize, split into multiple packets.
        if (bounds.Width <= MaxSyncChunkSize && bounds.Height <= MaxSyncChunkSize)
        {
            // Single packet for the entire region
            int size = Math.Max(bounds.Width, bounds.Height);
            int centerX = bounds.X + bounds.Width / 2;
            int centerY = bounds.Y + bounds.Height / 2;
            NetMessage.SendTileSquare(-1, centerX, centerY, size);
        }
        else
        {
            // Split into chunks to avoid buffer overflow
            for (int x = bounds.X; x < bounds.X + bounds.Width; x += MaxSyncChunkSize)
            {
                for (int y = bounds.Y; y < bounds.Y + bounds.Height; y += MaxSyncChunkSize)
                {
                    int chunkW = Math.Min(MaxSyncChunkSize, bounds.X + bounds.Width - x);
                    int chunkH = Math.Min(MaxSyncChunkSize, bounds.Y + bounds.Height - y);
                    int size = Math.Max(chunkW, chunkH);
                    int cx = x + chunkW / 2;
                    int cy = y + chunkH / 2;
                    NetMessage.SendTileSquare(-1, cx, cy, size);
                }
            }
        }
    }

    /// <summary>
    /// Convenience method: computes bounds from positions, then does frame update + network sync.
    /// </summary>
    public static void FinalizeBatch(IEnumerable<Point> affectedPositions)
    {
        var bounds = ComputeBounds(affectedPositions);
        BatchFrameUpdate(bounds);
        BatchNetworkSync(bounds);
    }

    /// <summary>
    /// Convenience method: performs frame update + network sync for a known bounding rectangle.
    /// </summary>
    public static void FinalizeBatch(Rectangle bounds)
    {
        BatchFrameUpdate(bounds);
        BatchNetworkSync(bounds);
    }

    /// <summary>
    /// Maximum tile dimension for a single network sync packet.
    /// Terraria's internal buffer can handle fairly large packets, but we chunk
    /// at 200×200 to stay well within limits and avoid noticeable hitches.
    /// </summary>
    private const int MaxSyncChunkSize = 200;
}
