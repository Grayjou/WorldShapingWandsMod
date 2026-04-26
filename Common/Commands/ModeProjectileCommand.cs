using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Content.Projectiles.WandModes;

namespace WorldShapingWandsMod.Common.Commands;

/// <summary>
/// Debug chat command to spawn mode indicator projectiles at the cursor for visual testing.
/// <para>
/// Usage: <c>/modeprojectile [mode]</c> where mode is one of:
/// <c>instant</c> (0), <c>select</c> (1), <c>confirm</c> (2), <c>stamp</c> (3), or <c>all</c>.
/// </para>
/// </summary>
/// <remarks>
/// Phase 1 testing command. Projectiles spawn at cursor position, hover for 1 second,
/// then fade out. Use this to verify sprites look correct in-game before wiring
/// the automatic spawn system in <see cref="BaseCyclingWand"/>.
/// </remarks>
public class ModeProjectileCommand : ModCommand
{
    public override string Command => "modeprojectile";

    public override string Usage =>
        "/modeprojectile <mode> [family]"
        + "\nModes: instant (0), select (1), confirm (2), stamp (3), all"
        + "\nFamily: 0-9 (Building..Molding) or auto (from held wand). Default: auto"
        + "\nSpawns the mode indicator projectile at the cursor for testing.";

    public override string Description => "Spawn mode indicator projectile at cursor";

    public override CommandType Type => CommandType.Chat;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length == 0)
        {
            caller.Reply(Usage, Color.Yellow);
            return;
        }

        Player player = caller.Player;
        Vector2 spawnPos = Main.MouseWorld;

        // Parse optional family index (arg[1]): 0-9 or auto-detect from held wand
        int familyIndex = ResolveFamilyIndex(player, args.Length > 1 ? args[1] : null);

        string mode = args[0].ToLower();

        switch (mode)
        {
            case "instant":
            case "0":
                SpawnMode<InstantModeProjectile>(player, spawnPos, familyIndex);
                caller.Reply($"Spawned Instant mode projectile (family={familyIndex})", new Color(80, 220, 80));
                break;

            case "select":
            case "1":
                SpawnMode<SelectModeProjectile>(player, spawnPos, familyIndex);
                caller.Reply($"Spawned Select mode projectile (family={familyIndex})", new Color(50, 130, 255));
                break;

            case "confirm":
            case "2":
                SpawnMode<ConfirmModeProjectile>(player, spawnPos, familyIndex);
                caller.Reply($"Spawned Confirm mode projectile (family={familyIndex})", new Color(255, 170, 50));
                break;

            case "stamp":
            case "3":
                SpawnMode<StampModeProjectile>(player, spawnPos, familyIndex);
                caller.Reply($"Spawned Stamp mode projectile (family={familyIndex})", new Color(170, 80, 230));
                break;

            case "all":
                // Spawn all 4 slightly offset for comparison
                float spacing = 40f;
                SpawnMode<InstantModeProjectile>(player, spawnPos + new Vector2(-spacing * 1.5f, 0), familyIndex);
                SpawnMode<SelectModeProjectile>(player, spawnPos + new Vector2(-spacing * 0.5f, 0), familyIndex);
                SpawnMode<ConfirmModeProjectile>(player, spawnPos + new Vector2(spacing * 0.5f, 0), familyIndex);
                SpawnMode<StampModeProjectile>(player, spawnPos + new Vector2(spacing * 1.5f, 0), familyIndex);
                caller.Reply($"Spawned all 4 mode projectiles (family={familyIndex})", Color.White);
                break;

            default:
                caller.Reply($"Unknown mode: {args[0]}", Color.Red);
                caller.Reply(Usage, Color.Yellow);
                break;
        }
    }

    /// <summary>
    /// Resolve the wand family index from the optional argument or auto-detect from held item.
    /// Returns 0 (Building) as fallback if no wand is held and no arg given.
    /// </summary>
    private static int ResolveFamilyIndex(Player player, string arg)
    {
        if (arg != null && int.TryParse(arg, out int parsed) && parsed >= 0 && parsed <= 9)
            return parsed;

        // Auto-detect from held wand
        WandFamily family = BaseCyclingWand.GetCurrentFamily(player);
        if (family != WandFamily.Unknown)
            return (int)family - 1; // WandFamily enum is 1-based, column index is 0-based

        return 0; // Default to Building (column 0)
    }

    private static void SpawnMode<T>(Player player, Vector2 position, int familyIndex) where T : BaseModeProjectile
    {
        int projType = ModContent.ProjectileType<T>();
        int projIndex = Projectile.NewProjectile(
            player.GetSource_FromThis(),
            position,
            Vector2.Zero,
            projType,
            0, 0f,
            player.whoAmI,
            ai0: 0f,              // ai[0] = bobbing frame counter (starts at 0)
            ai1: familyIndex);    // ai[1] = wand family column index (0-9)
    }
}
