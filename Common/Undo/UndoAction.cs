using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

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
    // Add wall, liquid, etc. as needed

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
    }

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

        WorldGen.SquareTileFrame(Position.X, Position.Y);

        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendTileSquare(-1, Position.X, Position.Y);
    }
}

public class UndoAction
{
    public List<TileSnapshot> Snapshots { get; } = new();
    public DateTime Timestamp { get; } = DateTime.Now;
    public string Description { get; set; }

    public void AddSnapshot(Point pos) => Snapshots.Add(new TileSnapshot(pos));

    public void Undo()
    {
        foreach (var snapshot in Snapshots)
            snapshot.Restore();
    }
}