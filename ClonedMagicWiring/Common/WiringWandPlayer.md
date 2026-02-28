using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using MagicWiring.Content.Items;

namespace MagicWiring.Common;

/// <summary>
/// Per-player TRANSIENT state for toggle mode.
/// Automatically resets on death, world exit, or item switch.
/// </summary>
public class WiringWandPlayer : ModPlayer
{
    /// <summary>
    /// In toggle mode: the tile coordinate of the first click.
    /// null = waiting for first click, HasValue = waiting for second click.
    /// </summary>
    public Point? PendingStartTile = null;
    
    /// <summary>
    /// WireKite orientation recorded at start of toggle mode drag.
    /// </summary>
    public bool PendingVerticalFirst = false;

    public void ClearPending()
    {
        PendingStartTile = null;
    }

    public override void OnRespawn() => ClearPending();
    public override void OnEnterWorld() => ClearPending();

    public override void PostUpdate()
    {
        if (Player.HeldItem?.ModItem is not WiringWandItem)
            ClearPending();
    }
}