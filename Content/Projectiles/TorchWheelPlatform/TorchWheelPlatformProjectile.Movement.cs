using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using WorldShapingWandsMod.Common.Items;

namespace WorldShapingWandsMod.Content.Projectiles;

public partial class TorchWheelPlatformProjectile
{
    // ================================================================
    //  Backtracking Phase
    // ================================================================

    private bool DoOneBacktrackStep()
    {
        // Try to step in backtrack direction
        int nextX = _currentTilePos.X + _direction;
        int platformY = (int)AiPlatformY;

        // Check if platform continues at the platform row
        if (!IsPlatformAt(nextX, platformY))
        {
            // Fell off edge — start forward pass
            Mod.Logger.Debug(
                $"[TorchWheelPlatform] Backtrack: fell off at x={nextX}, starting forward");
            StartForwardPass();
            return false;
        }

        // Move to next position (the tile above the platform)
        _currentTilePos = new Point16(nextX, platformY - 1);
        _backtrackPath.Add(_currentTilePos);

        // Update visual
        _targetPosition = new Vector2(nextX * 16 + 8, _currentTilePos.Y * 16 + 8);
        Projectile.Center = _targetPosition;

        // Check for existing torch at the torch position (above platform)
        int torchY = platformY - 1;
        if (WorldGen.InWorld(nextX, torchY, 1))
        {
            Tile tile = Main.tile[nextX, torchY];
            if (tile.HasTile && TileID.Sets.Torch[tile.TileType])
            {
                _anchorFound = true;

                Mod.Logger.Debug(
                    $"[TorchWheelPlatform] Backtrack: anchor found at ({nextX},{torchY}) " +
                    $"after {_backtrackPath.Count} steps");

                StartForwardPass();
                return false;
            }
        }

        // Check step limit
        if (_backtrackPath.Count > SpacingS)
        {
            Mod.Logger.Debug(
                $"[TorchWheelPlatform] Backtrack: reached S-step limit ({SpacingS})");
            StartForwardPass();
            return false;
        }

        return true;
    }

    // ================================================================
    //  Forward Pass
    // ================================================================

    private void StartForwardPass()
    {
        // Return to landing site
        _currentTilePos = _landingSite;
        Projectile.Center = new Vector2(_landingSite.X * 16 + 8, _landingSite.Y * 16 + 8);
        _visualPosition = Projectile.Center;
        _targetPosition = Projectile.Center;

        // Set forward direction
        _direction = _initialDirection;

        // Initialize sliding window with backtrack context
        _recentPath.Clear();
        _prescanWindowContribution = 0;

        // Add backtrack tiles in reverse order (anchor → landing)
        for (int i = _backtrackPath.Count - 1; i >= 0; i--)
        {
            _recentPath.Enqueue(_backtrackPath[i]);
            _prescanWindowContribution++;
            if (_recentPath.Count >= WindowSize)
                break;
        }

        // Reset tracking
        _visitedPositions.Clear();
        // existing torch indices cleared implicitly; we scan window directly instead
        _pathIndex = 0;
        _stepsSinceLastTorch = 0;

        // Record existing torches from backtrack for spacing
        // existing torches are detected directly from the queued window during placement

        _state = PlatformWheelState.TracingForward;

        Mod.Logger.Debug(
            $"[TorchWheelPlatform] Forward pass started: dir={(_direction > 0 ? "Right" : "Left")} " +
            $"windowSize={_recentPath.Count}/{WindowSize} prescanContrib={_prescanWindowContribution}");
    }

    private bool DoOneForwardStep()
    {
        // Budget check
        if (_pathIndex >= MaxTiles)
        {
            FlushSlidingWindow();
            LogTermination("MaxTiles budget exceeded");
            _state = PlatformWheelState.Dead;
            return false;
        }

        // Loop detection
        if (_visitedPositions.Contains(_currentTilePos))
        {
            LogTermination("Loop detected");
            _state = PlatformWheelState.Dead;
            return false;
        }
        _visitedPositions.Add(_currentTilePos);

        // Try to step forward
        int nextX = _currentTilePos.X + _direction;
        int platformY = (int)AiPlatformY;

        // Check if platform continues
        if (!IsPlatformAt(nextX, platformY))
        {
            // Flush remaining window positions before transitioning
            FlushSlidingWindow();
            Mod.Logger.Debug(
                $"[TorchWheelPlatform] Forward: fell off at x={nextX}, starting fall");
            StartFalling();
            return false;
        }

        // Move to next position (tile above the platform)
        Point16 newPos = new Point16(nextX, platformY - 1);
        _currentTilePos = newPos;
        _pathIndex++;
        _stepsSinceLastTorch++;

        // existing torches on the path will be considered from the sliding window

        // Update sliding window
        _recentPath.Enqueue(newPos);
        _lastEnqueuedPosition = newPos;
        if (_recentPath.Count > WindowSize)
        {
            _recentPath.Dequeue();
            if (_prescanWindowContribution > 0)
                _prescanWindowContribution--;
        }

        // Placement check
        bool windowFull = _recentPath.Count >= WindowSize;
        bool earlyPlacementAllowed = false;

        if (_prescanWindowContribution > 0 && !windowFull)
        {
            int windowCenter = _recentPath.Count / 2;
            int landingIndex = _prescanWindowContribution - 1;
            earlyPlacementAllowed = windowCenter >= landingIndex
                                 && _recentPath.Count >= SpacingS;
        }

        if (windowFull || earlyPlacementAllowed)
        {
            Point16 candidate = GetWindowCenter();

            if (CanPlaceAt(candidate))
            {
                if (TryPlaceTorch(candidate))
                {
                    _placedTorches.Add(candidate);
                    _torchesPlaced++;
                    _stepsSinceLastTorch = 0;

                    if (_torchesPlaced >= MaxTorches)
                    {
                        LogTermination($"MaxTorches limit reached ({MaxTorches})");
                        _state = PlatformWheelState.Dead;
                        return false;
                    }

                    Player owner = Main.player[Projectile.owner];
                    if (!TorchPlacementHelper.HasTorches(owner))
                    {
                        LogTermination("Player ran out of torches");
                        _state = PlatformWheelState.Dead;
                        return false;
                    }
                }
            }
        }

        // Update visual
        _targetPosition = new Vector2(nextX * 16 + 8, _currentTilePos.Y * 16 + 8);
        Projectile.Center = _targetPosition;

        return true;
    }

    // ================================================================
    //  Falling Phase
    // ================================================================

    private void StartFalling()
    {
        _fallVelocity = 0f;
        _fallStartY = (int)(Projectile.Center.Y / 16f);
        _state = PlatformWheelState.Falling;
    }

    private void DoFalling()
    {
        // Apply gravity
        _fallVelocity += FallGravity;
        if (_fallVelocity > MaxFallSpeed)
            _fallVelocity = MaxFallSpeed;

        // Horizontal momentum: continue in same direction at 2 px/frame
        Vector2 nextPos = Projectile.Center + new Vector2(_direction * 2f, _fallVelocity);
        int nextTileX = (int)(nextPos.X / 16f);
        int nextTileY = (int)(nextPos.Y / 16f);

        // Max fall distance check — prevent infinite fall in large caverns
        int fallDistance = nextTileY - _fallStartY;
        if (fallDistance > MaxFallDistance)
        {
            LogTermination($"Exceeded max fall distance ({MaxFallDistance} tiles)");
            SpawnImpactDust();
            _state = PlatformWheelState.Dead;
            return;
        }

        // Check for solid tile collision → die
        if (WorldGen.InWorld(nextTileX, nextTileY, 1))
        {
            Tile tile = Main.tile[nextTileX, nextTileY];
            if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
            {
                FlushSlidingWindow();
                LogTermination("Hit solid tile while falling");
                SpawnImpactDust();
                _state = PlatformWheelState.Dead;
                return;
            }
        }

        // Check for platform landing
        int belowY = (int)((nextPos.Y + 8) / 16f);
        if (WorldGen.InWorld(nextTileX, belowY, 1))
        {
            Tile belowTile = Main.tile[nextTileX, belowY];
            if (belowTile.HasTile && TileID.Sets.Platforms[belowTile.TileType])
            {
                // Land on platform — must be above it
                float platformTop = belowY * 16f;
                if (Projectile.Center.Y <= platformTop)
                {
                    // Update platform Y reference and continue forward
                    AiPlatformY = belowY;
                    _currentTilePos = new Point16(nextTileX, belowY - 1);

                    Projectile.Center = new Vector2(nextTileX * 16 + 8, (belowY - 1) * 16 + 8);
                    _targetPosition = Projectile.Center;
                    _visualPosition = Projectile.Center;

                    // Reset sliding window for the new platform — old positions
                    // at a different Y level would give stale spacing data
                    _recentPath.Clear();
                    _prescanWindowContribution = 0;
                    _stepsSinceLastTorch = 0;
                    _lastEnqueuedPosition = default;

                    Mod.Logger.Debug(
                        $"[TorchWheelPlatform] Landed on platform at y={belowY}, window reset, continuing forward");

                    _state = PlatformWheelState.TracingForward;
                    return;
                }
            }
        }

        // Continue falling
        Projectile.Center = nextPos;
        _targetPosition = nextPos;

        // Check for out-of-world
        if (!WorldGen.InWorld(nextTileX, nextTileY, 50))
        {
            LogTermination("Fell out of world");
            _state = PlatformWheelState.Dead;
        }
    }

    /// <summary>
    /// Evaluates remaining sliding window positions for a final torch placement
    /// at a transition point (platform edge, budget death, solid collision, out-of-world).
    /// Tries the window center first, then the last enqueued position as a fallback.
    /// All spacing checks are respected to prevent double-placement.
    /// </summary>
    private void FlushSlidingWindow()
    {
        if (_recentPath.Count < 2)
            return;

        // Try the window center (same logic as forward pass)
        Point16 candidate = GetWindowCenter();
        if (CanPlaceAt(candidate))
        {
            if (TryPlaceTorch(candidate))
            {
                _placedTorches.Add(candidate);
                _torchesPlaced++;
                _stepsSinceLastTorch = 0;
                return; // One placement per flush is sufficient
            }
        }

        // Fallback: try the last enqueued position if it's far enough from the last torch
        if (_stepsSinceLastTorch > SpacingS / 2
            && !_lastEnqueuedPosition.Equals(default)
            && !_lastEnqueuedPosition.Equals(candidate))
        {
            if (CanPlaceAt(_lastEnqueuedPosition))
            {
                if (TryPlaceTorch(_lastEnqueuedPosition))
                {
                    _placedTorches.Add(_lastEnqueuedPosition);
                    _torchesPlaced++;
                    _stepsSinceLastTorch = 0;
                }
            }
        }
    }
}
