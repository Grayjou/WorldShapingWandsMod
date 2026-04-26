using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Players;

namespace WorldShapingWandsMod.Common.Utilities;

/// <summary>
/// Static helper that manages the channeling timer for Stamp mode wands.
/// Called from each Stamp wand's <c>HoldItem</c> every frame while the stamp is locked.
/// Returns <c>true</c> when the wand should execute its operation (first charged fire + repeats).
/// </summary>
public static class StampChannelingHelper
{
    /// <summary>
    /// Number of channeling executions between audible sound plays.
    /// Every Nth execution during channeling plays the execution sound.
    /// </summary>
    public const int SoundThrottleInterval = 3;

    /// <summary>
    /// Volume multiplier for execution sounds during active channeling.
    /// Lower than normal to avoid cacophony at high repeat rates.
    /// </summary>
    public const float ChannelingSoundVolume = 0.3f;

    // ── Dust constants ──────────────────────────────────────────────────

    /// <summary>Dust type used for channeling effects (DrillContainmentUnit = 230).</summary>
    private const int ChannelingDustType = DustID.DrillContainmentUnit;

    /// <summary>Frames between hover dust emissions when fully charged.</summary>
    private const int HoverDustInterval = 8;

    /// <summary>Minimum offset from center for hover dust (avoids center cluster).</summary>
    private const float HoverMinDistance = 6f;

    /// <summary>Maximum radial distance for hover dust spawn offset.</summary>
    private const float HoverMaxDistance = 16f;

    /// <summary>Minimum scale for hover dust particles.</summary>
    private const float HoverDustScaleMin = 1.0f;

    /// <summary>Maximum scale for hover dust particles.</summary>
    private const float HoverDustScaleMax = 1.4f;

    /// <summary>Fade-in value for hover dust particles.</summary>
    private const float HoverDustFadeIn = 1.4f;

    /// <summary>Maximum radial distance for draw-in dust spawn offset.</summary>
    private const float DrawInMaxDistance = 64f;

    /// <summary>Velocity multiplier for draw-in dust converging toward cursor.</summary>
    private const float DrawInVelocityMultiplier = 0.04f;

    // ── Sound constants ─────────────────────────────────────────────────

    /// <summary>Minimum frames between charging progression sounds.</summary>
    private const int ChargeSoundMinFrameGap = 15;

    /// <summary>Volume for the charging progression sound (SoundID.Item82).</summary>
    private const float ChargeSoundVolume = 0.25f;

    private const float PitchDelta = 0.5f;

    /// <summary>Frame counter tracking last charge sound playback (client-local).</summary>
    private static int _lastChargeSoundFrame;

    /// <summary>
    /// Call from <c>HoldItem</c> when the stamp is locked. Manages the channeling timer
    /// and returns <c>true</c> when an execution should occur.
    /// Returns <c>false</c> immediately if channeling is disabled in client config.
    /// </summary>
    /// <param name="player">The player holding the wand.</param>
    /// <param name="wandPlayer">The player's wand state.</param>
    /// <param name="channelFrames">Frames required to charge before first execution (from config).</param>
    /// <param name="repeatFrames">Frames between repeat executions while channeling (from config).</param>
    /// <returns><c>true</c> when the wand should fire its Execute method.</returns>
    public static bool UpdateChanneling(
        Player player,
        WandPlayer wandPlayer,
        int channelFrames,
        int repeatFrames)
    {
        // Client-side toggle: AllowChanneling = false disables channeling entirely
        var clientConfig = WandConfigs.Preferences;
        if (clientConfig != null && !clientConfig.AllowChanneling)
        {
            wandPlayer.ResetStampChanneling();
            // TODO: If OnClick is also disabled, send a chat message stating that having both channeling and instant click disabled 
            // results in no functionality for the stamp wands, and recommend enabling at least one of them.
            return false;
        }

        if (!wandPlayer.IsStampChanneling || !player.controlUseItem)
        {
            // Not channeling or mouse released — reset
            wandPlayer.ResetStampChanneling();
            return false;
        }

        wandPlayer.IncrementChannelTimer();

        if (!wandPlayer.StampChannelCharged)
        {
            // Still charging — check if threshold reached
            if (wandPlayer.StampChannelTimer >= channelFrames)
            {
                wandPlayer.SetStampChannelCharged();
                return true; // First execution
            }
            return false;
        }
        else
        {
            // Already charged — check repeat interval
            wandPlayer.IncrementRepeatTimer();
            if (wandPlayer.StampRepeatTimer >= repeatFrames)
            {
                wandPlayer.ResetRepeatTimer();
                wandPlayer.IncrementChannelSoundCounter();
                return true; // Repeat execution
            }
            return false;
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the execution sound should be played during channeling.
    /// Throttles to every <see cref="SoundThrottleInterval"/>th execution.
    /// </summary>
    public static bool ShouldPlayChannelingSound(WandPlayer wandPlayer)
    {
        return wandPlayer.StampChannelSoundCounter % SoundThrottleInterval == 0;
    }

    /// <summary>
    /// Returns <c>true</c> if channeling is enabled in the client config.
    /// Use this in HandleUseItem to decide whether to begin channeling or fall back to single-click.
    /// </summary>
    public static bool IsChannelingEnabled()
    {
        var clientConfig = WandConfigs.Preferences;
        return clientConfig?.AllowChanneling ?? true;
    }

    /// <summary>
    /// Centralized handler for the 4th+ click on a stamp wand.
    /// Handles scenarios based on config:
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>ExecuteOnClick + channelFrames &gt; 0:</b> Execute immediately on click,
    ///     then begin channeling for subsequent repeats via HoldItem.
    ///   </description></item>
    ///   <item><description>
    ///     <b>!ExecuteOnClick + channelFrames &gt; 0:</b> Begin channeling only;
    ///     the operation does NOT execute until the charge completes.
    ///   </description></item>
    ///   <item><description>
    ///     <b>channelFrames == 0:</b> Instant-channel — execute immediately, channeling
    ///     starts with no charge delay so repeats begin on next HoldItem tick.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Channeling disabled:</b> Single-click execution with no channeling state.
    ///   </description></item>
    /// </list>
    /// Returns <c>true</c> if the wand should execute its operation now (the caller
    /// is responsible for actually calling the Execute method).
    /// </summary>
    /// <param name="wandPlayer">The player's wand state.</param>
    /// <param name="channelFrames">Frames required to charge (from config).</param>
    /// <param name="isOnCooldown">Whether the rate limiter is blocking execution.</param>
    /// <returns><c>true</c> if the caller should execute the wand operation.</returns>
    public static bool HandleStampClick(WandPlayer wandPlayer, int channelFrames, bool isOnCooldown)
    {
        if (IsChannelingEnabled())
        {
            // Always begin channeling state on click
            wandPlayer.BeginStampChanneling();

            if (channelFrames == 0)
            {
                // Zero-channel: mark as instantly charged
                wandPlayer.SetStampChannelCharged();
            }

            // Check whether the first click should execute immediately.
            // When StampExecuteOnClick is false and there's a real charge time,
            // the player gets a "preview period" before the operation fires.
            var serverConfig = WandConfigs.Stamp;
            bool executeOnClick = serverConfig?.StampExecuteOnClick ?? true;

            if (!executeOnClick && channelFrames > 0)
            {
                // Don't execute — just channeling begins; operation fires
                // when charge completes via UpdateChanneling in HoldItem.
                return false;
            }

            // Execute on first click (subsequent repeats come from
            // HoldItem → UpdateChanneling)
            if (isOnCooldown)
                return false;
            return true;
        }
        else
        {
            // Channeling disabled: single-click execution (pre-channeling behavior)
            if (isOnCooldown)
                return false;
            return true;
        }
    }

    // ── Dust Effects ────────────────────────────────────────────────────

    /// <summary>
    /// Emits channeling dust particles appropriate to the current channeling state.
    /// Call from each stamp wand's <c>HoldItem</c> after <see cref="UpdateChanneling"/>.
    /// <list type="bullet">
    ///   <item><description>Charging (not yet reached threshold): draw-in dust converges toward cursor, frequency ramps with progress.</description></item>
    ///   <item><description>Fully charged (repeating executions): gentle hover dust orbits cursor position.</description></item>
    /// </list>
    /// </summary>
    /// <param name="wandPlayer">The player's wand state.</param>
    /// <param name="channelFrames">Total frames required to charge (for progress calculation).</param>
    /// <param name="mouseTile">The cursor tile position (dust emits around this point).</param>
    public static void EmitChannelingDust(WandPlayer wandPlayer, int channelFrames, Point mouseTile)
    {
        var clientConfig = WandConfigs.Preferences;
        if (clientConfig != null && !clientConfig.AllowChannelingDust)
            return;

        if (!wandPlayer.IsStampChanneling)
            return;

        int frameCounter = (int)Main.GameUpdateCount;
        Vector2 worldCenter = mouseTile.ToWorldCoordinates(8f, 8f);

        if (wandPlayer.StampChannelCharged)
        {
            // Fully charged — hover dust
            EmitHoverDust(frameCounter, worldCenter);
        }
        else
        {
            // Charging — draw-in dust with ramping frequency
            EmitDrawInDust(frameCounter, worldCenter, wandPlayer.StampChannelTimer, channelFrames);
        }
    }

    /// <summary>
    /// Hover dust: gentle ambient particles around the cursor when fully channeled.
    /// Frequency: 1 dust per <see cref="HoverDustInterval"/> frames.
    /// </summary>
    private static void EmitHoverDust(int frameCounter, Vector2 worldCenter)
    {
        if (frameCounter % HoverDustInterval != 0)
            return;

        Vector2 offset = GetOffsetWithMinDistance(HoverMaxDistance, HoverMinDistance);
        Vector2 position = worldCenter + offset;
        Dust dust = Dust.NewDustDirect(position, 0, 0, ChannelingDustType);
        dust.velocity = Vector2.Zero;
        dust.noGravity = true;
        dust.fadeIn = HoverDustFadeIn;
        dust.scale = Main.rand.NextFloat(HoverDustScaleMin, HoverDustScaleMax);
    }

    /// <summary>
    /// Draw-in dust: particles converge toward cursor during charging phase.
    /// Frequency ramps with charge progress:
    ///   0–25%: every 6 frames, 25–50%: every 5, 50–75%: every 4, 75–100%: every 3.
    /// </summary>
    private static void EmitDrawInDust(int frameCounter, Vector2 worldCenter, int timer, int channelFrames)
    {
        if (channelFrames <= 0) return;

        int chargePercent = (int)(100f * timer / channelFrames);
        int dustInterval = chargePercent switch
        {
            < 25 => 4,
            < 50 => 3,
            < 75 => 2,
            _ => 1
        };

        if (frameCounter % dustInterval != 0)
            return;

        Vector2 offset = Main.rand.NextVector2Circular(DrawInMaxDistance, DrawInMaxDistance);
        Vector2 position = worldCenter + offset;
        Dust dust = Dust.NewDustDirect(position, 0, 0, ChannelingDustType);
        dust.velocity = -offset * DrawInVelocityMultiplier;
        dust.noGravity = true;
    }

    /// <summary>
    /// Generates a random offset vector with a minimum distance from the origin.
    /// Used by hover dust to prevent particles clustering at the exact center.
    /// </summary>
    private static Vector2 GetOffsetWithMinDistance(float maxDist, float minDist)
    {
        Vector2 offset;
        do
        {
            offset = new Vector2(
                Main.rand.NextFloat(-maxDist, maxDist),
                Main.rand.NextFloat(-maxDist, maxDist));
        } while (offset.Length() < minDist);
        return offset;
    }

    // ── Sound Effects ───────────────────────────────────────────────────

    /// <summary>
    /// Plays the charging progression sound (SoundID.Item82) at 25%, 50%, and 75%
    /// of the charge threshold. Throttled to at most once every <see cref="ChargeSoundMinFrameGap"/>
    /// frames. Pitch increases with progress (-0.2 → 0.0 → +0.2).
    /// Call from each stamp wand's <c>HoldItem</c> after <see cref="UpdateChanneling"/>.
    /// </summary>
    /// <param name="wandPlayer">The player's wand state.</param>
    /// <param name="channelFrames">Total frames required to charge.</param>
    public static void TryPlayChargeSound(WandPlayer wandPlayer, int channelFrames)
    {
        var clientConfig = WandConfigs.Preferences;
        if (clientConfig != null && !clientConfig.AllowChannelingSound)
            return;

        if (!wandPlayer.IsStampChanneling || wandPlayer.StampChannelCharged)
            return;

        if (channelFrames <= 0) return;

        int timer = wandPlayer.StampChannelTimer;
        int currentFrame = (int)Main.GameUpdateCount;
        if (currentFrame - _lastChargeSoundFrame < ChargeSoundMinFrameGap)
            return;

        // Check if the timer just crossed 25%, 50%, or 75% thresholds
        int threshold25 = channelFrames / 4;
        int threshold50 = channelFrames / 2;
        int threshold75 = channelFrames * 3 / 4;

        float? pitch = null;
        if (timer == threshold25)
            pitch = -PitchDelta;
        else if (timer == threshold50)
            pitch = 0.0f;
        else if (timer == threshold75)
            pitch = PitchDelta;
        if (pitch == null)
            return;

        SoundEngine.PlaySound(SoundID.Item82 with
        {
            Volume = ChargeSoundVolume,
            Pitch = pitch.Value
        }, Main.LocalPlayer.Center);

        _lastChargeSoundFrame = currentFrame;
    }
}
