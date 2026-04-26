namespace WorldShapingWandsMod.Content.Projectiles.WandModes;

/// <summary>
/// Mode indicator projectile for Confirm (ThreeClick) wands.
/// Displays an orange glow above the player's head.
/// <para>
/// Lifetime: 150 ticks (2.5s). Appears on the 2nd click (selection locked) and
/// refreshes every frame while the player holds a Confirm wand with a locked
/// selection. Signals "your next click will CONFIRM and execute the operation."
/// Dies naturally via timeout when the selection is cleared or the player switches items.
/// </para>
/// </summary>
public class ConfirmModeProjectile : BaseModeProjectile
{
    protected override int ModeLifetime => 150; // 2.5 seconds
}
