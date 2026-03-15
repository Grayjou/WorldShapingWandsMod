using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Commands;

public class ShapeTestCommand : ModCommand
{
    public override string Command => "shape";
    public override string Usage => "/shape <type> [mode] [thickness]" +
        "\nTypes: rect, ellipse, diamond, triangle, edge, cardinal, straight" +
        "\nModes: filled, hollow, outline [thickness]" +
        "\nThickness: 0=slim, 1=standard, 2+=thick" +
        "\nUse /shape start|end|clear for selection";

    public override string Description => "Test shape generation";
    public override CommandType Type => CommandType.Chat;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length == 0) { caller.Reply(Usage); return; }

        var player = caller.Player;
        var wandPlayer = player.GetModPlayer<WandPlayer>();
        var settings = wandPlayer.Settings;
        Point mouseTile = GeometryHelper.WorldToTile(Main.MouseWorld);

        switch (args[0].ToLower())
        {
            case "start":
                bool vertical = MathF.Abs(Main.MouseWorld.Y - player.Center.Y) >
                               MathF.Abs(Main.MouseWorld.X - player.Center.X);
                wandPlayer.StartSelection(mouseTile, vertical);
                caller.Reply($"Selection started at ({mouseTile.X}, {mouseTile.Y})", Color.Green);
                break;

            case "end":
                wandPlayer.UpdateSelection(mouseTile);
                var sel = wandPlayer.Selection;
                caller.Reply($"Selection: {sel.Width}x{sel.Height}", Color.Green);
                break;

            case "clear":
                wandPlayer.ClearSelection();
                caller.Reply("Selection cleared", Color.Yellow);
                break;

            case "rect": case "rectangle":
                settings.ShapeType = ShapeType.Rectangle;
                caller.Reply(settings.GetDescription(), Color.Cyan);
                break;

            case "ellipse": case "circle":
                settings.ShapeType = ShapeType.Ellipse;
                caller.Reply(settings.GetDescription(), Color.Cyan);
                break;

            case "diamond":
                settings.ShapeType = ShapeType.Diamond;
                caller.Reply(settings.GetDescription(), Color.Cyan);
                break;

            case "triangle": case "tri":
                settings.ShapeType = ShapeType.Triangle;
                caller.Reply(settings.GetDescription(), Color.Cyan);
                break;

            case "edge": case "line":
                settings.ShapeType = ShapeType.Elbow;
                caller.Reply(settings.GetDescription(), Color.Cyan);
                break;

            case "cardinal":
                settings.ShapeType = ShapeType.CardinalLine;
                caller.Reply(settings.GetDescription(), Color.Cyan);
                break;

            case "straight": case "straightline": case "free":
                settings.ShapeType = ShapeType.StraightLine;
                caller.Reply(settings.GetDescription(), Color.Cyan);
                break;

            case "filled":
                settings.ShapeMode = ShapeMode.Filled;
                caller.Reply(settings.GetDescription(), Color.Cyan);
                break;

            case "hollow": case "outline":
                settings.ShapeMode = ShapeMode.Hollow;
                if (args.Length > 1 && int.TryParse(args[1], out int t))
                    settings.Thickness = Math.Clamp(t, 0, 50);
                settings.Validate();
                caller.Reply(settings.GetDescription(), Color.Cyan);
                break;

            case "toggle":
                settings.PreviewMode = settings.PreviewMode == PreviewMode.Default 
                    ? PreviewMode.Forced 
                    : PreviewMode.Default;
                caller.Reply($"Preview: {settings.PreviewMode}", Color.Yellow);
                break;

            default:
                caller.Reply($"Unknown: {args[0]}", Color.Red);
                break;
        }
    }
}