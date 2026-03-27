using System.IO;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Networking;

/// <summary>
/// Common header shared by all wand operation packets.
/// Contains shape parameters and player identity — everything needed
/// to recompute the shape on the server. Wire-on-the-protocol: 23 bytes.
/// </summary>
public readonly struct WandPacketHeader
{
    public readonly Point Start;
    public readonly Point End;
    public readonly ShapeType Shape;
    public readonly ShapeMode FillMode;
    public readonly int Thickness;
    public readonly bool EqualDimensions;
    public readonly bool VerticalFirst;
    public readonly int PlayerWhoAmI;
    public readonly SliceMode Slice;
    public readonly bool ConnectDiameter;
    public readonly bool InvertSelection;

    public WandPacketHeader(
        Point start, Point end,
        ShapeType shape, ShapeMode fillMode,
        int thickness, bool equalDimensions,
        bool verticalFirst, int playerWhoAmI,
        SliceMode slice = SliceMode.Full, bool connectDiameter = true,
        bool invertSelection = false)
    {
        Start = start;
        End = end;
        Shape = shape;
        FillMode = fillMode;
        Thickness = thickness;
        EqualDimensions = equalDimensions;
        VerticalFirst = verticalFirst;
        PlayerWhoAmI = playerWhoAmI;
        Slice = slice;
        ConnectDiameter = connectDiameter;
        InvertSelection = invertSelection;
    }
}

/// <summary>
/// Shared read/write helpers for the common 23-byte wand packet header.
/// Used by all per-family packet handlers and the main dispatch class.
/// </summary>
public static class WandPacketHeaderIO
{
    /// <summary>
    /// Write the common 23-byte header shared by all wand operation packets.
    /// Format: Start(8) + End(8) + Shape(1) + FillMode(1) + Thickness(1) +
    ///         EqualDimensions(1) + VerticalFirst(1) + PlayerWhoAmI(1) +
    ///         Slice(1) + ConnectDiameter(1) + InvertSelection(1) = 23 bytes.
    /// </summary>
    public static void WriteCommonHeader(ModPacket packet, WandPacketHeader header)
    {
        packet.Write(header.Start.X);
        packet.Write(header.Start.Y);
        packet.Write(header.End.X);
        packet.Write(header.End.Y);
        packet.Write((byte)header.Shape);
        packet.Write((byte)header.FillMode);
        packet.Write((byte)header.Thickness);
        packet.Write(header.EqualDimensions);
        packet.Write(header.VerticalFirst);
        packet.Write((byte)header.PlayerWhoAmI);
        packet.Write((byte)header.Slice);
        packet.Write(header.ConnectDiameter);
        packet.Write(header.InvertSelection);
    }

    /// <summary>
    /// Read the common 23-byte header from an incoming packet.
    /// </summary>
    public static WandPacketHeader ReadCommonHeader(BinaryReader reader)
    {
        return new WandPacketHeader(
            start: new Point(reader.ReadInt32(), reader.ReadInt32()),
            end: new Point(reader.ReadInt32(), reader.ReadInt32()),
            shape: (ShapeType)reader.ReadByte(),
            fillMode: (ShapeMode)reader.ReadByte(),
            thickness: reader.ReadByte(),
            equalDimensions: reader.ReadBoolean(),
            verticalFirst: reader.ReadBoolean(),
            playerWhoAmI: reader.ReadByte(),
            slice: (SliceMode)reader.ReadByte(),
            connectDiameter: reader.ReadBoolean(),
            invertSelection: reader.ReadBoolean()
        );
    }
}
