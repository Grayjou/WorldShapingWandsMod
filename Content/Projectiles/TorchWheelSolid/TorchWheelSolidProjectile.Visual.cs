using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Geometry;

namespace WorldShapingWandsMod.Content.Projectiles;

public partial class TorchWheelSolidProjectile
{
    // ================================================================
    //  Diagnostic Logging
    // ================================================================

    /// <summary>
    /// Logs a termination event with full context for diagnosing wheel behavior.
    /// </summary>
    private void LogTermination(string reason, Point16? position = null)
    {
        var pos = position ?? new Point16(
            (int)(Projectile.Center.X / 16f),
            (int)(Projectile.Center.Y / 16f));

        Mod.Logger.Debug(
            $"[TorchWheel] TERMINATED: {reason} | " +
            $"pos=({pos.X},{pos.Y}) dir={(CardinalDirection)(int)AiDirection} " +
            $"pathIndex={_pathIndex} torches={_torchesPlaced} " +
            $"state={_state} handedness={_handedness} " +
            $"correction={_correctionApplied} anchor={_anchorFound}");
    }

    // ================================================================
    //  Sprite Animation (delegated to SpritesheetHelper)
    // ================================================================

    /// <summary>
    /// Row = distance since last placement (0–3).
    /// Row 0 = just placed (brightest), row 3 = about to place (dimmest).
    /// No rotation — user tested, projectile looks better static.
    /// Respects AnimateTorchWheel config — static frame when animation is off.
    /// </summary>
    private void UpdateRowFromPlacementProgress()
    {
        var config = WandConfigs.TorchWheel;
        if (config != null && !config.AnimateTorchWheel)
        {
            _spritesheet.CurrentRow = 3; // Static frame (brightest) when animation is disabled
            return;
        }

        float progress = Math.Min(1f, (float)_stepsSinceLastTorch / SpacingS);
        _spritesheet.CurrentRow = (int)(progress * 3.99f); // 0–3
    }

    /// <summary>
    /// Light level: full brightness after placing, dims as approaching next placement.
    /// Static brightness when animation is disabled.
    /// </summary>
    private float GetCurrentLightLevel()
    {
        var config = WandConfigs.TorchWheel;
        if (config != null && !config.AnimateTorchWheel)
            return 0.8f; // Static brightness

        float progress = Math.Min(1f, (float)_stepsSinceLastTorch / SpacingS);
        return 1.0f - progress * 0.7f; // 1.0 → 0.3
    }

    public override bool PreDraw(ref Color lightColor)
    {
        UpdateRowFromPlacementProgress();

        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        Rectangle sourceRect = _spritesheet.GetCurrentSourceRect(texture);
        Vector2 origin = _spritesheet.GetFrameOrigin(texture);

        // Draw at the smoothed visual position, not the logical position
        Vector2 drawPos = _visualPosition - Main.screenPosition;

        // The sprite is always drawn at full brightness — the brightness cycling
        // is expressed through Projectile.light (emitted light), not sprite color.
        // During coast phase, fade the sprite to transparent using smoothstep.
        float coastAlpha = _isCoasting
            ? 1f - Smoothstep((float)_coastFrame / Math.Max(1, _coastTotalFrames))
            : 1f;

        // During inactive phase (user cancelled), smooth fade to transparent.
        float inactiveAlpha = _isInactive
            ? 1f - Smoothstep((float)_inactiveFadeFrame / InactiveFadeDuration)
            : 1f;

        Color drawColor = Color.White * coastAlpha * inactiveAlpha;

        Main.EntitySpriteDraw(
            texture,
            drawPos,
            sourceRect,
            drawColor,
            0f, // No rotation — user tested, looks better static
            origin,
            Projectile.scale,
            SpriteEffects.None,
            0);

        return false; // We handled drawing
    }

    // ================================================================
    //  Acceleration Trajectory Model
    // ================================================================

    /// <summary>Hermite smoothstep: 0 at edge0, 1 at edge1, smooth in between.</summary>
    private static float Smoothstep(float edge0, float edge1, float x)
    {
        if (edge1 <= edge0) return 0f;
        float t = MathHelper.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>Single-argument smoothstep convenience (0→1 range).</summary>
    private static float Smoothstep(float x)
        => Smoothstep(0f, 1f, x);

    /// <summary>Gets the world-pixel center of the window's middle tile.</summary>
    private Vector2 GetWindowCenterWorld()
    {
        Point16 center = GetWindowCenter();
        return new Vector2(center.X * 16 + 8, center.Y * 16 + 8);
    }

    /// <summary>
    /// One frame of the acceleration trajectory model.
    /// Accumulates velocity via drive/centering forces with friction and per-axis braking.
    /// </summary>
    private void UpdateVisualVelocityModel()
    {
        Vector2 windowCenter = GetWindowCenterWorld();
        Vector2 windowEnd = _windowLeadingEdge;

        // Rolling average of end positions to smooth input jitter
        _endHistory.Enqueue(windowEnd);
        while (_endHistory.Count > AccelSampleFrames)
            _endHistory.Dequeue();

        Vector2 avgEnd = Vector2.Zero;
        foreach (var e in _endHistory)
            avgEnd += e;
        avgEnd /= _endHistory.Count;

        // Direction vectors (NOT normalized — distance-proportional)
        Vector2 toEnd = avgEnd - _visualPosition;
        Vector2 toCenter = windowCenter - _visualPosition;
        float distToCenter = toCenter.Length();

        float accelFactor = CForwardDrive * DriveScale;

        // ── Drive acceleration ──
        Vector2 aDrive = toEnd * accelFactor;

        // Per-axis braking: amplify when velocity opposes target
        if (_visualVelocity.X * toEnd.X < 0)
            aDrive.X *= GetBrakingFactor(Math.Abs(_visualVelocity.X));
        if (_visualVelocity.Y * toEnd.Y < 0)
            aDrive.Y *= GetBrakingFactor(Math.Abs(_visualVelocity.Y));

        // ── Centering acceleration ──
        float response = Smoothstep(0f, RMaxDriftRadius, distToCenter);
        Vector2 aCenter = toCenter * (response * accelFactor * DCenteringStrength * CenterScale);

        if (_visualVelocity.X * toCenter.X < 0)
            aCenter.X *= GetBrakingFactor(Math.Abs(_visualVelocity.X));
        if (_visualVelocity.Y * toCenter.Y < 0)
            aCenter.Y *= GetBrakingFactor(Math.Abs(_visualVelocity.Y));

        // ── Friction (speed-dependent) ──
        _visualVelocity.X *= GetFrictionFactor(Math.Abs(_visualVelocity.X));
        _visualVelocity.Y *= GetFrictionFactor(Math.Abs(_visualVelocity.Y));

        // ── Accumulate ──
        _visualVelocity += aDrive + aCenter;
        _visualPosition += _visualVelocity;

        // Perpendicular bobbing
        ApplyBobbing(ref _visualPosition, _visualVelocity, _visualFrame,
            coastFade: 1f, parallelScaling: false, amplitudeMultiplier: 1f);
    }

    // ================================================================
    //  Backward Smoothing (Pre-Scan & Return Phases)
    // ================================================================

    /// <summary>Bobbing amplitude multiplier during pre-scan/return (lighter feel).</summary>
    private const float BackwardBobMultiplier = 0.6f;

    /// <summary>Drive scale multiplier for backward phases (snappier than forward).</summary>
    private const float BackwardDriveMultiplier = 1.5f;

    /// <summary>
    /// Single-target acceleration model for pre-scan and return phases.
    /// Uses the same momentum/friction/braking physics as the forward model
    /// but tracks <see cref="_targetPosition"/> as a single target rather than
    /// the dual window-end/window-center targets used during forward tracing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// During pre-scan, the projectile explores backward along the contour.
    /// During return, it walks back to the landing site cosmetically.
    /// Both phases update <c>_targetPosition</c> each step — this model provides
    /// smooth visual interpolation with momentum between those tile-snapping updates.
    /// </para>
    /// <para>
    /// The slightly higher drive multiplier and lower bobbing create a "lighter"
    /// feel appropriate for the exploratory backward pass, distinguishing it from
    /// the forward trace which is the main event.
    /// </para>
    /// <para>
    /// Velocity state (<c>_visualVelocity</c>) carries across phase transitions,
    /// providing seamless handoff from PreScan → Return → Forward.
    /// </para>
    /// </remarks>
    private void UpdateVisualBackwardModel()
    {
        Vector2 target = _targetPosition;

        // Direction vector (distance-proportional, not normalized)
        Vector2 toTarget = target - _visualPosition;

        float accelFactor = CForwardDrive * DriveScale * BackwardDriveMultiplier;

        // ── Drive acceleration toward target ──
        Vector2 aDrive = toTarget * accelFactor;

        // Per-axis braking: amplify when velocity opposes target
        if (_visualVelocity.X * toTarget.X < 0)
            aDrive.X *= GetBrakingFactor(Math.Abs(_visualVelocity.X));
        if (_visualVelocity.Y * toTarget.Y < 0)
            aDrive.Y *= GetBrakingFactor(Math.Abs(_visualVelocity.Y));

        // ── Friction (speed-dependent) ──
        _visualVelocity.X *= GetFrictionFactor(Math.Abs(_visualVelocity.X));
        _visualVelocity.Y *= GetFrictionFactor(Math.Abs(_visualVelocity.Y));

        // ── Accumulate ──
        _visualVelocity += aDrive;
        _visualPosition += _visualVelocity;

        // Perpendicular bobbing (lighter amplitude during backward phases)
        ApplyBobbing(ref _visualPosition, _visualVelocity, _visualFrame,
            coastFade: 1f, parallelScaling: false, amplitudeMultiplier: BackwardBobMultiplier);
    }

    /// <summary>
    /// Speed-dependent friction: higher speed → more friction.
    /// Returns multiplicative factor (0..1) applied to velocity each frame.
    /// </summary>
    private static float GetFrictionFactor(float speed)
    {
        float ratio = SpeedAnchor > 0 ? speed / SpeedAnchor : 0f;
        float clampedRatio = Math.Min(ratio, MaxSpeedAnchorRatio);
        float exponent = (float)Math.Pow(clampedRatio, FrictionCorrectionExp);
        return Math.Max(0f, 1f - Friction * (1f + exponent));
    }

    /// <summary>
    /// Per-axis braking multiplier when velocity opposes the target direction.
    /// Returns factor > 1 to amplify corrective acceleration.
    /// </summary>
    private static float GetBrakingFactor(float speed)
    {
        float ratio = SpeedAnchor > 0 ? speed / SpeedAnchor : 0f;
        float clampedRatio = Math.Min(ratio, MaxSpeedAnchorRatio);
        float exponent = (float)Math.Pow(clampedRatio, BrakingExp);
        return 1f + BrakingStrength * (1f + exponent);
    }

    /// <summary>
    /// Sinusoidal displacement perpendicular to velocity, with angle-dependent amplitude.
    /// </summary>
    private void ApplyBobbing(ref Vector2 pos, Vector2 velocity, int frame,
        float coastFade, bool parallelScaling, float amplitudeMultiplier)
    {
        float effectiveAmplitude = VBobAmplitude * amplitudeMultiplier;
        if (effectiveAmplitude < 0.001f || coastFade < 0.001f) return;

        Vector2 velDir = velocity.SafeNormalize(Vector2.Zero);
        if (velDir == Vector2.Zero) return;

        // Angle-dependent scaling against the logical target direction
        float angleScale = 1f;
        Vector2 toLegacy = (_targetPosition - pos).SafeNormalize(Vector2.Zero);
        if (toLegacy != Vector2.Zero)
        {
            if (parallelScaling)
            {
                // |cos(θ)| via dot product — max when parallel
                angleScale = Math.Abs(Vector2.Dot(velDir, toLegacy));
            }
            else
            {
                // |sin(θ)| via 2D cross product — max when perpendicular
                angleScale = Math.Abs(velDir.X * toLegacy.Y - velDir.Y * toLegacy.X);
            }
        }

        float t = frame / (float)FramesPerSecond;
        Vector2 normal = new(-velDir.Y, velDir.X);
        float bob = effectiveAmplitude * angleScale * coastFade
                  * (float)Math.Sin(2.0 * Math.PI * VBobFrequency * t);
        pos += normal * bob;
    }

    // ================================================================
    //  Coast Phase (smooth deceleration at end of forward pass)
    // ================================================================

    private void StartCoastPhase()
    {
        _isCoasting = true;
        _coastFrame = 0;
        _coastStartPos = _visualPosition;
        _coastStartVel = _visualVelocity;

        // Target = final logical position (where the projectile actually is)
        Vector2 rawTarget = _targetPosition;

        // Angle adjustment — if velocity points away from target, cap the angle
        _coastTarget = AdjustCoastTarget(_coastStartPos, _coastStartVel, rawTarget);

        float dist = Vector2.Distance(_coastStartPos, _coastTarget);
        float speed = _coastStartVel.Length();

        if (dist < CoastMinSpeed)
        {
            _isCoasting = false;
            _visualPosition = _coastTarget;
            return;
        }

        _coastTotalFrames = speed > 0.001f
            ? (int)MathHelper.Clamp(dist / speed * 2f, CoastMinFrames, CoastMaxFrames)
            : (int)MathHelper.Clamp(dist / 2f, CoastMinFrames, CoastMaxFrames);

        // Guarantee enough timeLeft for the entire coast + half-second buffer.
        // Vanilla decrements timeLeft every frame and kills at 0 — without this,
        // a large shape can exhaust the budget during tracing, cutting the coast short.
        Projectile.timeLeft = Math.Max(Projectile.timeLeft, _coastTotalFrames + 30);
    }

    private Vector2 AdjustCoastTarget(Vector2 position, Vector2 velocity, Vector2 target)
    {
        Vector2 toTarget = target - position;
        float targetDist = toTarget.Length();
        if (targetDist < 1e-4f) return target;

        Vector2 velNorm = velocity.SafeNormalize(Vector2.Zero);
        Vector2 targetDir = toTarget / targetDist;
        if (velNorm == Vector2.Zero) return target;

        float dot = MathHelper.Clamp(Vector2.Dot(velNorm, targetDir), -1f, 1f);
        float angleDeg = MathHelper.ToDegrees((float)Math.Acos(dot));

        if (angleDeg <= MaxCoastAngleDeg) return target;

        // Rotate velocity by ±MaxCoastAngleDeg toward the target side
        float cross = velNorm.X * targetDir.Y - velNorm.Y * targetDir.X;
        float rad = MathHelper.ToRadians(MaxCoastAngleDeg);
        float cos = (float)Math.Cos(rad);
        float sin = (float)Math.Sin(rad);

        Vector2 newDir = cross > 0
            ? new Vector2(velNorm.X * cos - velNorm.Y * sin, velNorm.X * sin + velNorm.Y * cos)
            : new Vector2(velNorm.X * cos + velNorm.Y * sin, -velNorm.X * sin + velNorm.Y * cos);

        return position + newDir * targetDist;
    }

    private void UpdateVisualCoastPhase()
    {
        _coastFrame++;
        if (_coastFrame >= _coastTotalFrames)
        {
            _visualPosition = _coastTarget;
            _isCoasting = false;
            return;
        }

        float t = (float)_coastFrame / _coastTotalFrames;
        float t2 = t * t;
        float t3 = t2 * t;

        // Hermite basis functions (h11 term drops out because V(T) = 0)
        float h00 = 2f * t3 - 3f * t2 + 1f;
        float h10 = t3 - 2f * t2 + t;
        float h01 = -2f * t3 + 3f * t2;

        _visualPosition = new Vector2(
            h00 * _coastStartPos.X + h10 * _coastStartVel.X * _coastTotalFrames + h01 * _coastTarget.X,
            h00 * _coastStartPos.Y + h10 * _coastStartVel.Y * _coastTotalFrames + h01 * _coastTarget.Y);

        // Hermite derivative for bobbing direction
        float dpX = (6 * t2 - 6 * t) * _coastStartPos.X + (3 * t2 - 4 * t + 1) * _coastStartVel.X * _coastTotalFrames + (-6 * t2 + 6 * t) * _coastTarget.X;
        float dpY = (6 * t2 - 6 * t) * _coastStartPos.Y + (3 * t2 - 4 * t + 1) * _coastStartVel.Y * _coastTotalFrames + (-6 * t2 + 6 * t) * _coastTarget.Y;

        ApplyBobbing(ref _visualPosition, new Vector2(dpX, dpY), _visualFrame,
            coastFade: 1f - t,
            parallelScaling: true,
            amplitudeMultiplier: CoastBobMultiplier);
    }
}
