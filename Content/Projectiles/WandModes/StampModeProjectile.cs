namespace WorldShapingWandsMod.Content.Projectiles.WandModes;

/// <summary>
/// Mode indicator projectile for Stamp (FourClick) wands.
/// Displays a purple glow above the player's head.
/// <para>
/// Lifetime: 180 ticks (3s). Appears on the 3rd click (stamp locked) and refreshes
/// every frame while the player holds a Stamp wand with a locked stamp template.
/// Signals "your next click/channel will STAMP the operation." Stays alive
/// throughout the stamp channeling cycle. Dies naturally via timeout when the
/// stamp is unlocked or the player switches items.
/// </para>
/// </summary>
public class StampModeProjectile : BaseModeProjectile
{
    protected override int ModeLifetime => 180; // 3 seconds
}
