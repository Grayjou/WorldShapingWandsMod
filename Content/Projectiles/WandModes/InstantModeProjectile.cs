namespace WorldShapingWandsMod.Content.Projectiles.WandModes;

/// <summary>
/// Mode indicator projectile for Instant (OneClick) wands.
/// Displays a green glow above the player's head.
/// <para>
/// Lifetime: 90 ticks (1.5s). Refreshed every frame while the player holds an
/// Instant wand via <see cref="BaseModeProjectile.Refresh"/>. Dies naturally
/// ~1.5s after the player switches away.
/// </para>
/// </summary>
public class InstantModeProjectile : BaseModeProjectile
{
    /// <summary>
    /// Instant mode uses a short lifetime that is continuously refreshed
    /// while the wand is held. The 90-tick buffer gives a smooth fade-out
    /// after switching away rather than an abrupt kill.
    /// </summary>
    protected override int ModeLifetime => 90; // 1.5 seconds
}
