using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Undo;

public class TileSnapshot
{
    // ── Position ──
    public Point Position { get; }

    // ── Tile data ──
    public bool HadTile { get; }
    public int TileType { get; }
    public int TileFrameX { get; }
    public int TileFrameY { get; }
    public byte Slope { get; }
    public bool IsHalfBrick { get; }

    // ── Wall data ──
    public ushort WallType { get; }

    // ── Paint & Coating (tile) ──
    public byte TileColor { get; }
    public bool IsTileEchoCoated { get; }
    public bool IsTileIlluminant { get; }
    public bool IsTileActuated { get; }

    // ── Paint & Coating (wall) ──
    public byte WallColor { get; }
    public bool IsWallEchoCoated { get; }
    public bool IsWallIlluminant { get; }

    // ── Wires & Actuator ──
    public bool HasWireRed { get; }
    public bool HasWireBlue { get; }
    public bool HasWireGreen { get; }
    public bool HasWireYellow { get; }
    public bool HasActuator { get; }

    public TileSnapshot(Point pos)
    {
        Position = pos;
        var tile = Main.tile[pos.X, pos.Y];

        // Tile data
        HadTile = tile.HasTile;
        TileType = tile.TileType;
        TileFrameX = tile.TileFrameX;
        TileFrameY = tile.TileFrameY;
        Slope = (byte)tile.Slope;
        IsHalfBrick = tile.IsHalfBlock;

        // Wall data
        WallType = tile.WallType;

        // Paint & Coating (tile)
        TileColor = tile.TileColor;
        IsTileEchoCoated = tile.IsTileInvisible;
        IsTileIlluminant = tile.IsTileFullbright;
        IsTileActuated = tile.IsActuated;

        // Paint & Coating (wall)
        WallColor = tile.WallColor;
        IsWallEchoCoated = tile.IsWallInvisible;
        IsWallIlluminant = tile.IsWallFullbright;

        // Wires & Actuator
        HasWireRed = tile.RedWire;
        HasWireBlue = tile.BlueWire;
        HasWireGreen = tile.GreenWire;
        HasWireYellow = tile.YellowWire;
        HasActuator = tile.HasActuator;
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

        // Paint & Coating (tile)
        tile.TileColor = TileColor;
        tile.IsTileInvisible = IsTileEchoCoated;
        tile.IsTileFullbright = IsTileIlluminant;
        tile.IsActuated = IsTileActuated;

        // Paint & Coating (wall)
        tile.WallColor = WallColor;
        tile.IsWallInvisible = IsWallEchoCoated;
        tile.IsWallFullbright = IsWallIlluminant;

        // Wires & Actuator
        tile.RedWire = HasWireRed;
        tile.BlueWire = HasWireBlue;
        tile.GreenWire = HasWireGreen;
        tile.YellowWire = HasWireYellow;
        tile.HasActuator = HasActuator;
    }
}

public class UndoAction
{
    public List<TileSnapshot> Snapshots { get; } = new();
    public DateTime Timestamp { get; } = DateTime.Now;
    public string Description { get; set; }

    public void AddSnapshot(Point pos) => Snapshots.Add(new TileSnapshot(pos));

    /// <summary>
    /// Calculates what the current world state has that would be lost by undoing to
    /// the snapshot state. Returns a summary of tile types and wall types that exist
    /// now but would be reverted (i.e., what the player effectively "spent" on the
    /// operation that this undo would reverse).
    /// </summary>
    public UndoCostSummary CalculateCost()
    {
        var summary = new UndoCostSummary();

        foreach (var snap in Snapshots)
        {
            var current = Main.tile[snap.Position.X, snap.Position.Y];

            // Tile changes: current has a tile that the snapshot didn't
            // (placed by the operation, would be removed by undo)
            if (current.HasTile && !snap.HadTile)
                summary.AddTile(current.TileType);
            // Or type changed
            else if (current.HasTile && snap.HadTile && current.TileType != (ushort)snap.TileType)
            {
                summary.AddTile(current.TileType);       // current tile would be lost
                summary.AddRestoredTile(snap.TileType);   // old tile would return
            }

            // Tiles that were destroyed (snapshot had tile, current doesn't)
            // — undo would restore them, no cost to the player
            if (!current.HasTile && snap.HadTile)
                summary.AddRestoredTile(snap.TileType);

            // Wall changes
            if (current.WallType != WallID.None && snap.WallType == WallID.None)
                summary.AddWall(current.WallType);
            else if (current.WallType != WallID.None && snap.WallType != WallID.None
                     && current.WallType != snap.WallType)
            {
                summary.AddWall(current.WallType);
                summary.AddRestoredWall(snap.WallType);
            }

            if (current.WallType == WallID.None && snap.WallType != WallID.None)
                summary.AddRestoredWall(snap.WallType);
        }

        return summary;
    }

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