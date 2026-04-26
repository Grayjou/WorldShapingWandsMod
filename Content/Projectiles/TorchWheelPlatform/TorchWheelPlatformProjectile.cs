using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Utilities;
#if DEBUG
using WorldShapingWandsMod.Common.Debug;
#endif

namespace WorldShapingWandsMod.Content.Projectiles;

/// <summary>
/// A projectile that walks along platforms horizontally, placing torches at regular intervals.
/// </summary>
/// <remarks>
/// <para>
/// Spawned by <see cref="FlyingTorchWheelPlatform"/> after it lands on a platform.
/// Receives initial direction via <c>ai[0]</c> (1 = right, -1 = left) and
/// the platform Y level via <c>ai[1]</c>.
/// </para>
/// <para>
/// State machine:
/// <list type="number">
///   <item><b>Landing:</b> First-frame initialization.</item>
///   <item><b>Backtracking:</b> Walks backward up to S tiles looking for anchor torch.</item>
///   <item><b>TracingForward:</b> Main forward pass with sliding window torch placement.</item>
///   <item><b>Falling:</b> Gravity-affected fall; lands on platform to continue or dies on solid.</item>
///   <item><b>Dead:</b> Terminal state.</item>
/// </list>
/// </para>
/// <para>
/// Unlike <see cref="TorchWheelSolidProjectile"/>, this uses simple horizontal movement
/// rather than wall-following. Falls off platform edges and can land on lower platforms.
/// The D (absolute spacing) check is unnecessary since path distance equals horizontal
/// distance for linear platform traces.
/// </para>
/// </remarks>
public partial class TorchWheelPlatformProjectile : ModProjectile
{
    // ================================================================
    //  State Machine
    // ================================================================

    private enum PlatformWheelState
    {
        /// <summary>First-frame initialization.</summary>
        Landing,

        /// <summary>Walking backward to find anchor torch or reach S-step limit.</summary>
        Backtracking,

        /// <summary>Main forward pass with sliding window torch placement.</summary>
        TracingForward,

        /// <summary>Falling after walking off a platform edge.</summary>
        Falling,

        /// <summary>Terminal state — projectile is killed.</summary>
        Dead,
    }

    // ================================================================
    //  Configuration
    // ================================================================

    private static TorchWheelConfig Config => WandConfigs.TorchWheel;

    private const int FramesPerSecond = 60;

    /// <summary>Minimum path-distance between consecutive torches.</summary>
    private static int SpacingS => Config?.TorchWheelSpacingS ?? 12;

    /// <summary>Sliding window size = 2 * S.</summary>
    private static int WindowSize => SpacingS * 2;

    /// <summary>Maximum tiles traced during forward pass.</summary>
    private static int MaxTiles => Config?.TorchWheelMaxTiles ?? 500;

    /// <summary>Maximum torches placed per projectile.</summary>
    private static int MaxTorches => Config?.TorchWheelMaxTorches ?? 50;

    // Speed: 30 tiles/sec (matches solid version)
    // In DEBUG builds, live-tunable via /dev set TWP.TilesPerSecond
    private const int DefaultTilesPerSecond = 30;
    private const float DefaultStepIncrementPerFrame = (float)DefaultTilesPerSecond / FramesPerSecond;

    /// <summary>Gravity during falling phase.
    /// In DEBUG builds, live-tunable via <c>/dev set TWP.FallGravity</c>.</summary>
    private const float DefaultFallGravity = 0.4f;

    /// <summary>Maximum fall speed.
    /// In DEBUG builds, live-tunable via <c>/dev set TWP.MaxFallSpeed</c>.</summary>
    private const float DefaultMaxFallSpeed = 12f;

    /// <summary>Maximum tiles the projectile can fall before dying (prevents infinite fall in large caverns).</summary>
    private const int MaxFallDistance = 30;

    // ================================================================
    //  Synced State (via ai[])
    // ================================================================

    /// <summary>Initial direction: 1 = right, -1 = left.</summary>
    private ref float AiDirection => ref Projectile.ai[0];

    /// <summary>Platform Y level (tile coordinates) where we landed.</summary>
    private ref float AiPlatformY => ref Projectile.ai[1];

    // ================================================================
    //  Local State
    // ================================================================

    private PlatformWheelState _state;
    private bool _firstFrameDone;

    // Direction: 1 = right, -1 = left
    private int _direction;
    private int _initialDirection;

    // Position tracking (tile coordinates)
    private Point16 _landingSite;
    private Point16 _currentTilePos;

    // Backtrack data
    private List<Point16> _backtrackPath;
    private bool _anchorFound;

    // Forward pass data
    private Queue<Point16> _recentPath;
    private List<Point16> _placedTorches;
    private HashSet<Point16> _visitedPositions;
    private int _pathIndex;
    private int _torchesPlaced;
    private int _stepsSinceLastTorch;
    private int _prescanWindowContribution;

    // Falling state
    private float _fallVelocity;
    private int _fallStartY;

    // Last enqueued position (avoids Queue.Last() which requires LINQ)
    private Point16 _lastEnqueuedPosition;

    // Speed accumulator
    private float _stepAccumulator;

    // Visual
    private Vector2 _visualPosition;
    private Vector2 _targetPosition;
    /// <summary>In DEBUG builds, live-tunable via <c>/dev set TWP.LerpSmoothing</c>.</summary>
    private const float DefaultLerpSmoothing = 0.3f;
    private SpritesheetHelper _spritesheet;

    // ── Baked constants (calibrated via DevTunable, archived 2026-04-18) ──
    private const float StepIncrementPerFrame = DefaultStepIncrementPerFrame;
    private const float FallGravity = DefaultFallGravity;
    private const float MaxFallSpeed = DefaultMaxFallSpeed;
    private const float LerpSmoothing = DefaultLerpSmoothing;

    // Cached torch info
    private int _cachedTorchItemType;
    private int _cachedTorchTileType;
    private int _cachedTorchPlaceStyle;

    // ================================================================
    //  Projectile Setup
    // ================================================================

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 4;
    }

    public override void SetDefaults()
    {
        Projectile.width = 16;
        Projectile.height = 16;
        Projectile.friendly = ModContent.GetInstance<Common.Configs.TorchWheelConfig>().TorchWheelFriendly;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 60 * FramesPerSecond;
        Projectile.light = 0.5f;
    }

    public override void OnSpawn(IEntitySource source)
    {
        _backtrackPath = new List<Point16>(SpacingS + 2);
        _recentPath = new Queue<Point16>(WindowSize + 1);
        _placedTorches = new List<Point16>(MaxTorches);
        _visitedPositions = new HashSet<Point16>();
        _pathIndex = 0;
        _torchesPlaced = 0;
        _stepsSinceLastTorch = 0;
        _prescanWindowContribution = 0;
        _fallVelocity = 0f;
        _fallStartY = 0;
        _lastEnqueuedPosition = default;
        _stepAccumulator = 0f;
        _firstFrameDone = false;
        _state = PlatformWheelState.Landing;
        _spritesheet = new SpritesheetHelper(columns: 1, rows: 4);
        _visualPosition = Projectile.Center;
        _targetPosition = Projectile.Center;
    }

    private void DoLanding()
    {
        // Read direction from ai[0]
        _direction = (int)AiDirection;
        _initialDirection = _direction;

        // Record landing site — the tile ABOVE the platform (where torches go)
        _landingSite = new Point16(
            (int)(Projectile.Center.X / 16f),
            (int)(Projectile.Center.Y / 16f));
        _currentTilePos = _landingSite;

        // Cache torch info
        Player owner = Main.player[Projectile.owner];
        var (itemType, _, tileType, placeStyle) = TorchPlacementHelper.FindTorchInInventory(owner);
        _cachedTorchItemType = itemType;
        _cachedTorchTileType = tileType;
        _cachedTorchPlaceStyle = placeStyle;

        // Sync visual
        _visualPosition = Projectile.Center;
        _targetPosition = Projectile.Center;

        // Start backtracking in opposite direction
        _direction = -_initialDirection;
        _backtrackPath.Clear();
        _backtrackPath.Add(_landingSite);
        _anchorFound = false;

        Mod.Logger.Debug(
            $"[TorchWheelPlatform] Landing at ({_landingSite.X},{_landingSite.Y}) " +
            $"initialDir={(_initialDirection > 0 ? "Right" : "Left")} " +
            $"platformY={(int)AiPlatformY}");

        _state = PlatformWheelState.Backtracking;
        _firstFrameDone = true;
    }

    // ================================================================
    //  AI — Main Update
    // ================================================================

    public override void AI()
    {
        if (!_firstFrameDone)
        {
            DoLanding();
        }

        if (_state == PlatformWheelState.Dead)
        {
            Projectile.Kill();
            return;
        }

        // Falling uses frame-based physics, not step accumulator
        if (_state == PlatformWheelState.Falling)
        {
            DoFalling();
        }
        else
        {
            // Accumulator-based stepping for walking phases
            _stepAccumulator += StepIncrementPerFrame;
            while (_stepAccumulator >= 1f)
            {
                _stepAccumulator -= 1f;

                bool continueStep = _state switch
                {
                    PlatformWheelState.Backtracking => DoOneBacktrackStep(),
                    PlatformWheelState.TracingForward => DoOneForwardStep(),
                    _ => false,
                };

                if (!continueStep)
                {
                    if (_state == PlatformWheelState.Dead)
                    {
                        Projectile.Kill();
                        return;
                    }
                }
            }
        }

        // Smooth visual position
        _visualPosition = Vector2.Lerp(_visualPosition, _targetPosition, 1f - LerpSmoothing);

        // Dust trail
        if (Main.rand.NextBool(3))
        {
            var dust = Dust.NewDustDirect(
                Projectile.Center - new Vector2(4f), 8, 8,
                DustID.Torch, 0f, 0f, 150, default, 0.8f);
            dust.noGravity = true;
            dust.velocity *= 0.3f;
        }

        Projectile.light = GetCurrentLightLevel();
    }
}
