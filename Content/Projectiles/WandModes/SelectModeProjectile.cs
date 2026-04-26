namespace WorldShapingWandsMod.Content.Projectiles.WandModes;

/// <summary>
/// Mode indicator projectile for Select (TwoClick) wands.
/// Displays a blue glow above the player's head.
/// <para>
/// Lifetime: 150 ticks (2.5s). Appears after the 1st click (selection started)
/// and refreshes every frame while the player holds a Select wand with an active
/// selection. Signals "your next click will EXECUTE the operation." Dies naturally
/// via timeout when the selection is cleared or the player switches items.
/// </para>
/// </summary>
public class SelectModeProjectile : BaseModeProjectile
{
    protected override int ModeLifetime => 150; // 2.5 seconds
}
