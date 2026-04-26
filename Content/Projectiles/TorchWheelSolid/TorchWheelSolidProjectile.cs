using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Utilities;
#if DEBUG
using WorldShapingWandsMod.Common.Debug;
#endif

namespace WorldShapingWandsMod.Content.Projectiles;

/// <summary>
/// A projectile that traces block outlines using a wall-following algorithm,
/// placing torches at regular intervals.
/// </summary>
/// <remarks>
/// <para>
/// Spawned by <see cref="FlyingTorchWheelSolid"/> after it hits a solid block.
/// Receives its initialization data (handedness, direction) via <c>ai[]</c>
/// and <c>localAI[]</c> slots — it never has a travel phase.
/// </para>
/// <para>
/// Two spacing parameters control torch density:
/// <list type="bullet">
///   <item><b>S (outline spacing):</b> Minimum path-distance between consecutive
///     placed torches. Measured along the traced outline.</item>
///   <item><b>D (absolute spacing):</b> Minimum Manhattan distance between any
///     two placed torches. Prevents clustering on serpentine paths.</item>
/// </list>
/// </para>
/// <para>
/// Uses a 7-state machine (Option C algorithm):
/// <list type="number">
///   <item><b>Landing:</b> Records landing site, determines direction/handedness.</item>
///   <item><b>PreScanning:</b> Traces backward S steps to find an existing torch anchor
///     and detect 45° handedness ambiguity.</item>
///   <item><b>ReturningToLanding:</b> Cosmetic walk back to the landing site for
///     visual coherence (no game logic).</item>
///   <item><b>TracingForward:</b> Main forward pass with sliding window torch placement.
///     If an anchor was found, the window is pre-warmed with pre-scan tiles.</item>
///   <item><b>BackfillingPrescan:</b> Places torches in the pre-scan region (behind
///     the landing site) using the recorded path.</item>
/// </list>
/// </para>
/// <para>
/// Biome torches are resolved via <c>Player.UsingBiomeTorches</c> (Torch God's Favor)
/// since this wand has no UI — the player's inventory flag is used directly.
/// </para>
/// <para>
/// Ported from the TorchPlacement2D R&amp;D algorithm (ModReferences/TorchPlacement2D).
/// Torch inventory/placement logic reuses the existing <see cref="TorchPlacementHelper"/>.
/// </para>
/// <para>
/// <b>File organization (partial class):</b>
/// <list type="bullet">
///   <item><c>TorchWheelSolidProjectile.cs</c> — State machine, config, fields, setup, AI dispatch</item>
///   <item><c>TorchWheelSolidProjectile.PreScan.cs</c> — Pre-scan phase (backward anchor search)</item>
///   <item><c>TorchWheelSolidProjectile.Tracing.cs</c> — Return, forward pass, backfill transition</item>
///   <item><c>TorchWheelSolidProjectile.Placement.cs</c> — Placement logic, spacing, underwater overrides</item>
///   <item><c>TorchWheelSolidProjectile.Visual.cs</c> — Diagnostics, animation, drawing</item>
/// </list>
/// </para>
/// </remarks>
public partial class TorchWheelSolidProjectile : ModProjectile
{
    // ================================================================
    //  State Machine
    // ================================================================

    /// <summary>
    /// The 7-state machine controlling the TorchWheel lifecycle.
    /// </summary>
    /// <remarks>
    /// Transitions: Landing → PreScanning → ReturningToLanding → TracingForward → BackfillingPrescan → Dead.
    /// PreScanning may self-correct (flip handedness) on 45° ambiguity before proceeding.
    /// </remarks>
    private enum TorchWheelState
    {
        /// <summary>First-frame initialization (reads spawn data, caches torch info).</summary>
        Landing,

        /// <summary>Traces backward S steps from landing to find an existing torch anchor and validate handedness.</summary>
        PreScanning,

        /// <summary>Cosmetic walk back to the landing site along the recorded pre-scan path (visual only).</summary>
        ReturningToLanding,

        /// <summary>Main forward pass: wall-follows the outline, placing torches via sliding window.</summary>
        TracingForward,

        /// <summary>Places torches in the pre-scan region (behind landing) using the recorded path.</summary>
        BackfillingPrescan,

        /// <summary>Terminal state — projectile is killed.</summary>
        Dead,
    }

    // ================================================================
    //  Configuration — read from TorchWheelConfig at runtime
    // ================================================================

    private static TorchWheelConfig Config => WandConfigs.TorchWheel;

    private const int FramesPerSecond = 60;

    /// <summary>Minimum path-distance between consecutive torches.</summary>
    private static int SpacingS => Config?.TorchWheelSpacingS ?? 12;

    /// <summary>Minimum Manhattan distance between ANY two placed torches.</summary>
    private static int SpacingD => Config?.TorchWheelSpacingD ?? 8;

    /// <summary>Sliding window size = 2 * S. Warm-up period before first placement.</summary>
    private static int WindowSize => SpacingS * 2;

    /// <summary>
    /// Maximum number of tiles the wheel traces during the forward pass.
    /// Pre-scan tiles do NOT count toward this budget.
    /// </summary>
    private static int MaxTiles => Config?.TorchWheelMaxTiles ?? 500;

    /// <summary>Safety limit on total torches placed per projectile.</summary>
    private static int MaxTorches => Config?.TorchWheelMaxTorches ?? 50;

    // ── Speed: fixed at 30 tiles/sec (0.5 tiles per frame) ──────
    // No longer configurable. 30 TPS was extensively tested and provides
    // the best balance of visual clarity and placement accuracy.
    private const int DefaultTilesPerSecond = 30;

    private const int TilesPerSecond = DefaultTilesPerSecond;

    /// <summary>
    /// Fractional step increment per frame = 30 / 60 = 0.5.
    /// One step every 2 frames at 60 FPS.
    /// </summary>
    private const float StepIncrementPerFrame = (float)DefaultTilesPerSecond / FramesPerSecond;

    /// <summary>
    /// Maximum pre-scan steps before a loop-back is treated as a genuine small-structure
    /// loop rather than a 45° handedness error. If the pre-scan returns to the landing
    /// site within this many steps, handedness is flipped and the pre-scan restarts.
    /// </summary>
    private const int CorrectionThreshold = 4;

    // ================================================================
    //  Synced State (via ai[] — Terraria auto-syncs these in MP)
    // ================================================================

    /// <summary>Whether the projectile has initialized tracing (always 1 — set by FlyingTorchWheelSolid).</summary>
    private ref float AiInitialized => ref Projectile.ai[0];

    /// <summary>Current facing direction (stored as float, cast to CardinalDirection).</summary>
    private ref float AiDirection => ref Projectile.ai[1];

    // ================================================================
    //  Client-Local State (not synced)
    // ================================================================

    /// <summary>Current state machine phase.</summary>
    private TorchWheelState _state;

    /// <summary>Whether the first-frame initialization has run.</summary>
    private bool _firstFrameDone;

    // ── Core Tracing State ───────────────────────────────────────

    private Handedness _handedness;
    private WallFollower _follower;
    private Queue<Point16> _recentPath;
    private List<(Point16 pos, int pathIndex)> _placedTorches;
    private HashSet<long> _visitedStates; // packed (x, y, dir) to avoid tuple GC
    private int _pathIndex;
    private int _torchesPlaced;
    private int _stepsSinceLastTorch;

    // ── Existing Torch Tracking ─────────────────────────────────
    // Path-indices where pre-existing world torches were encountered along
    // the outline. Used by CheckOutlineSpacing (S) to respect existing
    // torches that we didn't place but are on the same path.
    private List<int> _existingTorchPathIndices;

    // ── Landing & Pre-Scan Data (Option C) ───────────────────────

    /// <summary>The tile where the wheel first touches the outline (P₀).</summary>
    private Point16 _landingSite;

    /// <summary>The corrected forward tracing direction (may be flipped during pre-scan).</summary>
    private CardinalDirection _initialDirection;

    /// <summary>The corrected forward handedness (may be flipped during pre-scan).</summary>
    private Handedness _initialHandedness;

    /// <summary>Tiles traced during the backward pre-scan (index 0 = landing site).</summary>
    private List<Point16> _prescanPath;

    /// <summary>Whether an existing torch was found during pre-scan.</summary>
    private bool _anchorFound;

    /// <summary>Position of the anchor torch (valid only if <see cref="_anchorFound"/> is true).</summary>
    private Point16 _anchorPosition;

    /// <summary>Current index walking backward through <see cref="_prescanPath"/> during cosmetic return.</summary>
    private int _prescanReturnIndex;

    /// <summary>Whether the 45° handedness correction has already been applied during pre-scan.</summary>
    private bool _correctionApplied;

    /// <summary>Visited states for pre-scan loop detection (separate from forward pass).</summary>
    private HashSet<long> _prescanVisitedStates;

    /// <summary>
    /// How many tiles in _recentPath came from the pre-scan (back context).
    /// Decrements as prescan tiles are dequeued during forward walking.
    /// Used to determine when early placement checks are valid.
    /// </summary>
    private int _prescanWindowContribution;

    // ── Speed & Visual ───────────────────────────────────────────

    // Speed accumulator: accumulates fractional steps until >= 1.0
    private float _stepAccumulator;

    // Visual smoothing — prevents teleporting between tiles
    private Vector2 _visualPosition;
    private Vector2 _targetPosition;

    // ── Legacy Lerp (used when SmoothVisualPath is off, or during PreScan/Return) ──
    /// <summary>How quickly the visual position catches up to the target. 0 = instant, 1 = never.</summary>
    private const float DefaultLerpSmoothing = 0.3f;

    private const float LerpSmoothing = DefaultLerpSmoothing;

    // ── Acceleration Trajectory Model (replaces lerp when SmoothVisualPath is on) ──
    // Momentum-based: accumulates velocity via drive/centering forces with friction and per-axis braking.

    /// <summary>Forward drive toward the window leading edge.</summary>
    private const float CForwardDrive = 10.0f;

    /// <summary>Centering pull toward window center, scaled by response.</summary>
    private const float DCenteringStrength = 4.0f;

    /// <summary>Max drift radius for smoothstep response (px).</summary>
    private const float RMaxDriftRadius = 20.0f;

    /// <summary>Scales drive force (makes raw distance usable as acceleration).</summary>
    private const float DriveScale = 1f / 1000f;

    /// <summary>Scales centering force relative to drive.</summary>
    private const float CenterScale = 1f / 10f;

    /// <summary>Speed/anchor ratio clamp for friction and braking curves.</summary>
    private const float MaxSpeedAnchorRatio = 2.0f;

    /// <summary>Base friction coefficient per frame.</summary>
    private const float Friction = 0.04f;

    /// <summary>Reference speed for friction/braking scaling (px/frame).</summary>
    private const float SpeedAnchor = 5.0f;

    /// <summary>Exponent for speed-dependent friction correction.</summary>
    private const float FrictionCorrectionExp = 2.0f;

    /// <summary>Exponent for per-axis braking curve.</summary>
    private const float BrakingExp = 2.0f;

    /// <summary>Braking multiplier when velocity opposes target.</summary>
    private const float BrakingStrength = 2.25f;

    /// <summary>Number of frames to average end positions over (input smoothing).</summary>
    private const int AccelSampleFrames = 5;

    /// <summary>Bobbing amplitude perpendicular to velocity (px).</summary>
    private const float VBobAmplitude = 2.0f;

    /// <summary>Bobbing frequency (Hz, at 60 FPS).</summary>
    private const float VBobFrequency = 3.0f;

    /// <summary>Bobbing amplitude multiplier during coast phase.</summary>
    private const float CoastBobMultiplier = 2.0f;

    // ── Coast Phase Constants ───────────────────────────────────
    private const float CoastMinSpeed = 0.15f;
    private const int CoastMinFrames = 30;
    private const int CoastMaxFrames = 120;
    private const float MaxCoastAngleDeg = 30f;

    // ── Velocity Model State ────────────────────────────────────
    private Vector2 _visualVelocity;     // Current velocity of the visual ball
    private int _visualFrame;            // Frame counter for bobbing phase
    private bool _isCoasting;            // True during coast-to-stop phase
    private int _coastFrame;             // Current frame within coast phase
    private int _coastTotalFrames;       // Total frames for coast phase
    private Vector2 _coastStartPos;      // P(0) for Hermite interpolation
    private Vector2 _coastStartVel;      // V(0) for Hermite interpolation
    private Vector2 _coastTarget;        // P(T) for Hermite interpolation
    private Vector2 _windowLeadingEdge;  // World-pixel center of newest tile in window
    private Queue<Vector2> _endHistory = new(); // Rolling window of end positions for input smoothing

    // ── Inactive Phase (graceful fade-out on user cancel) ─────────
    private bool _isInactive;                // True when the wheel is fading out (no more torch placement)
    private int _inactiveFadeFrame;          // Current frame within inactive fade
    private const int InactiveFadeDuration = 45; // ~0.75s at 60fps

    /// <summary>Whether to use the velocity trajectory model. Read from config.</summary>
    private static bool SmoothVisualPath => Config?.SmoothVisualPath ?? true;

    // Spritesheet helper: 1 column × 4 rows (no rotation — user tested, looks better static)
    private SpritesheetHelper _spritesheet;

    // Cached torch info for the trace session
    private int _cachedTorchItemType;
    private int _cachedTorchTileType;
    private int _cachedTorchPlaceStyle;

    // Underwater torch override state — set during CanPlaceAt, consumed by TryPlaceTorch
    private bool _useUnderwaterOverride;
    private int _underwaterOverrideItemType;
    private int _underwaterOverrideTileType;
    private int _underwaterOverridePlaceStyle;



    // ================================================================
    //  Projectile Setup
    // ================================================================

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 4; // 1×4 spritesheet
    }

    public override void SetDefaults()
    {
        Projectile.width = 16;
        Projectile.height = 16;
        Projectile.friendly = ModContent.GetInstance<Common.Configs.TorchWheelConfig>().TorchWheelFriendly;
        Projectile.tileCollide = false; // Never collides — FlyingTorchWheelSolid handles travel
        Projectile.penetrate = -1;
        // Generous time limit — the wheel is killed by MaxTiles, not by time.
        // 60s safety net prevents orphaned projectiles.
        Projectile.timeLeft = 60 * FramesPerSecond;
        Projectile.light = 0.5f;
        Projectile.extraUpdates = 0;
    }

    public override void OnSpawn(IEntitySource source)
    {
        _recentPath = new Queue<Point16>(WindowSize + 1);
        _placedTorches = new List<(Point16, int)>(MaxTorches);
        _visitedStates = new HashSet<long>();
        _existingTorchPathIndices = new List<int>();
        _prescanPath = new List<Point16>(SpacingS + 2);
        _prescanVisitedStates = new HashSet<long>();
        _pathIndex = 0;
        _torchesPlaced = 0;
        _stepsSinceLastTorch = 0;
        _cachedTorchItemType = -1;
        _firstFrameDone = false;
        _state = TorchWheelState.Landing;
        _stepAccumulator = 0f;
        _spritesheet = new SpritesheetHelper(columns: 1, rows: 4);
        _visualPosition = Projectile.Center;
        _targetPosition = Projectile.Center;
        _prescanWindowContribution = 0;
        _visualVelocity = Vector2.Zero;
        _visualFrame = 0;
        _isCoasting = false;
        _coastFrame = 0;
        _coastTotalFrames = 0;
        _windowLeadingEdge = Projectile.Center;
        _endHistory = new Queue<Vector2>();
    }

    /// <summary>
    /// First-frame initialization (Landing state): reads handedness from localAI[0]
    /// (set by FlyingTorchWheelSolid), caches torch info, then immediately transitions
    /// to PreScanning with reversed direction and same handedness.
    /// </summary>
    private void DoLanding()
    {
        // Read spawn data
        _handedness = (Handedness)(int)Projectile.localAI[0];
        _initialDirection = (CardinalDirection)(int)AiDirection;
        _initialHandedness = _handedness;

        // Record landing site in tile coordinates
        _landingSite = new Point16(
            (int)(Projectile.Center.X / 16f),
            (int)(Projectile.Center.Y / 16f));

        // Cache the torch type from inventory for the entire trace session
        Player owner = Main.player[Projectile.owner];
        var (itemType, _, tileType, placeStyle) = TorchPlacementHelper.FindTorchInInventory(owner);
        _cachedTorchItemType = itemType;
        _cachedTorchTileType = tileType;
        _cachedTorchPlaceStyle = placeStyle;

        // Sync visual position with spawn location
        _visualPosition = Projectile.Center;
        _targetPosition = Projectile.Center;

        // Initialize pre-scan: reverse direction, SAME handedness
        // This traces "backward" along the outline to find an existing torch anchor.
        CardinalDirection prescanDir = (CardinalDirection)(((int)_initialDirection + 2) % 4);
        AiDirection = (float)prescanDir;
        _follower = new WallFollower(_handedness);

        _prescanPath.Clear();
        _prescanPath.Add(_landingSite);
        _anchorFound = false;
        _correctionApplied = false;
        _prescanVisitedStates.Clear();

        Mod.Logger.Debug(
            $"[TorchWheel] Landing at ({_landingSite.X},{_landingSite.Y}) " +
            $"dir={_initialDirection} hand={_initialHandedness} " +
            $"prescanDir={prescanDir}");

        _state = TorchWheelState.PreScanning;
        _firstFrameDone = true;
    }

    // ================================================================
    //  AI — main per-frame update (state-machine dispatch)
    // ================================================================

    /// <summary>
    /// Transitions the torch wheel to the inactive state: stops placing torches,
    /// continues rolling forward with a smooth opacity fade, then kills itself.
    /// Called instead of <c>Kill()</c> for a graceful wind-down.
    /// </summary>
    public void SetInactive()
    {
        if (_isInactive) return;
        _isInactive = true;
        _inactiveFadeFrame = 0;
        // Guarantee enough timeLeft for the full inactive fade + buffer
        Projectile.timeLeft = Math.Max(Projectile.timeLeft, InactiveFadeDuration + 30);
    }

    public override void AI()
    {
        // First-frame init: Landing state reads spawn data and transitions to PreScanning
        if (!_firstFrameDone)
        {
            DoLanding();
        }

        // Inactive state — fade out while continuing movement, no torch placement
        if (_isInactive)
        {
            _inactiveFadeFrame++;
            if (_inactiveFadeFrame >= InactiveFadeDuration)
            {
                Projectile.Kill();
                return;
            }

            // Continue visual updates for smooth fade
            if (SmoothVisualPath && _recentPath.Count >= 2)
                UpdateVisualVelocityModel();
            else
                _visualPosition = Vector2.Lerp(_visualPosition, _targetPosition, 1f - LerpSmoothing);

            _visualFrame++;
            Projectile.light = GetCurrentLightLevel() * (1f - (float)_inactiveFadeFrame / InactiveFadeDuration);
            return;
        }

        // Dead state — stay alive for coast animation, then kill
        if (_state == TorchWheelState.Dead)
        {
            if (_isCoasting)
            {
                UpdateVisualCoastPhase();
                _visualFrame++;
                Projectile.light = GetCurrentLightLevel();
                return; // Stay alive for coast animation
            }
            Projectile.Kill();
            return;
        }

        // Accumulator-based stepping for sub-frame precision (0.5 steps/frame at 30 TPS).
        _stepAccumulator += StepIncrementPerFrame;
        while (_stepAccumulator >= 1f)
        {
            _stepAccumulator -= 1f;

            bool continueStep = _state switch
            {
                TorchWheelState.PreScanning => DoOnePreScanStep(),
                TorchWheelState.ReturningToLanding => DoOneReturnStep(),
                TorchWheelState.TracingForward => DoOneForwardStep(),
                _ => false,
            };

            if (!continueStep)
            {
                if (_state == TorchWheelState.Dead)
                {
                    Projectile.Kill();
                    return;
                }
                // State transitioned — continue stepping (e.g., PreScan → Return → Forward)
            }
        }

        // Update visual position using acceleration model (all phases when SmoothVisualPath is on)
        if (SmoothVisualPath && _state == TorchWheelState.TracingForward && _recentPath.Count >= 2)
        {
            UpdateVisualVelocityModel();
        }
        else if (SmoothVisualPath && (_state == TorchWheelState.PreScanning || _state == TorchWheelState.ReturningToLanding))
        {
            UpdateVisualBackwardModel();
        }
        else if (_isCoasting)
        {
            UpdateVisualCoastPhase();
        }
        else
        {
            // PreScan / Return phases, or legacy mode — simple lerp
            _visualPosition = Vector2.Lerp(_visualPosition, _targetPosition, 1f - LerpSmoothing);
        }
        _visualFrame++;

        // Emit a small fire dust trail during tracing (suppress during coast — sprite is fading out)
        if (!_isCoasting && Main.rand.NextBool(3))
        {
            var dust = Dust.NewDustDirect(Projectile.Center - new Vector2(4f), 8, 8,
                DustID.Torch, 0f, 0f, 150, default, 0.8f);
            dust.noGravity = true;
            dust.velocity *= 0.3f;
        }

        // Update light level based on placement proximity
        Projectile.light = GetCurrentLightLevel();
    }
}
