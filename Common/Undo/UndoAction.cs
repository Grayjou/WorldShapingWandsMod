using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Undo;

public class TileSnapshot
{
    public Point Position { get; }
    public bool HadTile { get; }
    public int TileType { get; }
    public int TileFrameX { get; }
    public int TileFrameY { get; }
    public byte Slope { get; }
    public bool IsHalfBrick { get; }
    public ushort WallType { get; }

    public TileSnapshot(Point pos)
    {
        Position = pos;
        var tile = Main.tile[pos.X, pos.Y];
        HadTile = tile.HasTile;
        TileType = tile.TileType;
        TileFrameX = tile.TileFrameX;
        TileFrameY = tile.TileFrameY;
        Slope = (byte)tile.Slope;
        IsHalfBrick = tile.IsHalfBlock;
        WallType = tile.WallType;
    }

    /// <summary>
    /// Restores the tile data only (no frame update or network sync).
    /// Frame updates and network sync are handled in batch by <see cref="UndoAction.Undo"/>.
    /// </summary>
    public void Restore()
    {
        var tile = Main.tile[Position.X, Position.Y];

        if (HadTile)
        {
            tile.HasTile = true;
            tile.TileType = (ushort)TileType;
            tile.TileFrameX = (short)TileFrameX;
            tile.TileFrameY = (short)TileFrameY;
            tile.Slope = (SlopeType)Slope;
            tile.IsHalfBlock = IsHalfBrick;
        }
        else
        {
            tile.HasTile = false;
        }

        tile.WallType = WallType;
    }
}

public class UndoAction
{
    public List<TileSnapshot> Snapshots { get; } = new();
    public DateTime Timestamp { get; } = DateTime.Now;
    public string Description { get; set; }

    public void AddSnapshot(Point pos) => Snapshots.Add(new TileSnapshot(pos));

    /// <summary>
    /// Restores all tile snapshots, then performs a single batched frame update
    /// and network sync for the entire affected region.
    /// </summary>
    public void Undo()
    {
        if (Snapshots.Count == 0) return;

        // Phase 1: Restore all tile data
        var positions = new List<Point>(Snapshots.Count);
        foreach (var snapshot in Snapshots)
        {
            snapshot.Restore();
            positions.Add(snapshot.Position);
        }

        // Phase 2: Batch frame update + network sync
        BulkTileOperations.FinalizeBatch(positions);
    }
}