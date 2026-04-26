using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;

namespace WorldShapingWandsMod.Content.Projectiles;

public partial class TorchWheelSolidProjectile
{
    // ================================================================
    //  Phase 2: Cosmetic Return to Landing (visual only)
    // ================================================================

    /// <summary>
    /// Walks the projectile visually back to the landing site along the recorded
    /// pre-scan path. No game logic — purely cosmetic animation.
    /// </summary>
    /// <returns><c>true</c> to continue stepping; <c>false</c> if state transitioned.</returns>
    private bool DoOneReturnStep()
    {
        if (_prescanReturnIndex < 0)
        {
            StartForwardPass();
            return false;
        }

        Point16 pos = _prescanPath[_prescanReturnIndex];
        _prescanReturnIndex--;

        // Update visual position — cosmetic only
        _targetPosition = new Vector2(pos.X * 16 + 8, pos.Y * 16 + 8);
        Projectile.Center = _targetPosition;

        // Spawn a distinct dust particle during return (different color to show "returning")
        if (Main.rand.NextBool(2))
        {
            var dust = Dust.NewDustDirect(Projectile.Center - new Vector2(4f), 8, 8,
                DustID.Torch, 0f, 0f, 200, default, 0.5f);
            dust.noGravity = true;
            dust.velocity *= 0.15f;
        }

        // Check if we've reached the landing site
        if (pos.X == _landingSite.X && pos.Y == _landingSite.Y)
        {
            StartForwardPass();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Transitions from ReturningToLanding to TracingForward.
    /// Positions the projectile at the landing site, derives the correct forward
    /// direction/handedness from the pre-scan path, and pre-warms the sliding window
    /// with pre-scan tiles if an anchor was found.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The forward direction is derived from the pre-scan path rather than using
    /// <see cref="_initialDirection"/> directly. This is because the initial direction
    /// from <see cref="HandednessCalculator.GetInitialDirection"/> can be wrong when
    /// the collision face is misdetected at shallow angles (e.g., a ceiling hit
    /// registered as a side-face hit).
    /// </para>
    /// <para>
    /// <b>Key insight:</b> To reverse a wall-follower along the same contour, you must
    /// reverse the movement direction AND flip the handedness. The pre-scan traces
    /// backward from landing; its first step reveals the actual backward direction.
    /// The forward direction is the reverse of that step, with flipped handedness.
    /// </para>
    /// </remarks>
    private void StartForwardPass()
    {
        // Position at landing site
        Projectile.Center = new Vector2(_landingSite.X * 16 + 8, _landingSite.Y * 16 + 8);
        // DO NOT snap _visualPosition — the backward model may still be a few pixels away.
        // Let the forward acceleration model smoothly pull the ball from its current position.
        // Snapping here caused a visible teleport at the backward→forward phase boundary.
        _targetPosition = Projectile.Center;

        // Zero velocity and clear end history for a clean forward start
        // (direction reverses from backward to forward, so residual velocity would overshoot)
        _visualVelocity = Vector2.Zero;
        _endHistory.Clear();

        // Derive forward direction from the pre-scan path.
        // The pre-scan successfully traced backward from landing. To go forward along
        // the same contour, we reverse the pre-scan's first movement and flip handedness.
        if (_prescanPath.Count >= 2)
        {
            // Pre-scan path: [landing(0), step1(1), step2(2), ...]
            // The first step direction: landing → prescanPath[1]
            int dx = _prescanPath[1].X - _prescanPath[0].X;
            int dy = _prescanPath[1].Y - _prescanPath[0].Y;
            CardinalDirection prescanStepDir = DirectionFromDelta(dx, dy);

            // Forward = reverse of pre-scan step direction
            CardinalDirection forwardDir = (CardinalDirection)(((int)prescanStepDir + 2) % 4);
            // Flip handedness to maintain wall contact on the same contour
            Handedness forwardHand = _initialHandedness == Handedness.Right
                ? Handedness.Left : Handedness.Right;

            AiDirection = (float)forwardDir;
            _handedness = forwardHand;
        }
        else
        {
            // Minimal pre-scan (stuck immediately or single tile) — fall back to initial values
            AiDirection = (float)_initialDirection;
            _handedness = _initialHandedness;
        }

        _follower = new WallFollower(_handedness);

        // Initialize sliding window
        _recentPath.Clear();

        // Initialize prescan contribution counter and pre-warm with prescan tiles
        _prescanWindowContribution = 0;

        if (_prescanPath.Count > 0)
        {
            // Pre-scan path: [landing, ..., anchor_or_limit]
            // Add in reverse order: [anchor_or_limit, ..., landing]
            for (int i = _prescanPath.Count - 1; i >= 0; i--)
            {
                _recentPath.Enqueue(_prescanPath[i]);
                _prescanWindowContribution++;
                if (_recentPath.Count >= WindowSize)
                    break;
            }

            Mod.Logger.Debug(
                $"[TorchWheel] Forward: window pre-warmed with {_prescanWindowContribution} tiles " +
                $"(landing at window index {_prescanWindowContribution - 1})");
        }
        else
        {
            // No prescan context — window starts with just the landing site.
            _recentPath.Enqueue(_landingSite);
        }

        // Reset forward pass tracking
        _visitedStates.Clear();
        _existingTorchPathIndices.Clear();
        _pathIndex = 0;
        _stepsSinceLastTorch = 0;

        // If the window was pre-warmed with pre-scan tiles, record any existing
        // torches from those tiles for outline spacing checks.
        if (_anchorFound)
        {
            foreach (var pos in _recentPath)
            {
                if (WorldGen.InWorld(pos.X, pos.Y, 1))
                {
                    Tile tile = Main.tile[pos.X, pos.Y];
                    if (tile.HasTile && TileID.Sets.Torch[tile.TileType])
                    {
                        // Use negative indices for pre-scan torch positions
                        // (they're "before" the forward pass started)
                        _existingTorchPathIndices.Add(-_prescanWindowContribution);
                    }
                }
            }
        }

        _state = TorchWheelState.TracingForward;

        Mod.Logger.Debug(
            $"[TorchWheel] Forward pass started: dir={(CardinalDirection)(int)AiDirection} hand={_handedness} " +
            $"windowSize={_recentPath.Count}/{WindowSize} " +
            $"(initial was dir={_initialDirection} hand={_initialHandedness})");
    }

    // ================================================================
    //  Phase 3: Forward Pass — main tracing with sliding window
    // ================================================================

    /// <summary>
    /// Performs one wall-following step and (possibly) places a torch.
    /// This is the main tracing phase — identical to the original forward pass
    /// but with the sliding window potentially pre-warmed from the pre-scan.
    /// </summary>
    /// <returns><c>true</c> to continue stepping; <c>false</c> if the forward pass ended.</returns>
    private bool DoOneForwardStep()
    {
        // MaxTiles budget exceeded — end forward pass
        if (_pathIndex >= MaxTiles)
        {
            LogTermination("MaxTiles budget exceeded");
            TransitionToBackfill();
            return false;
        }

        Point16 currentPos = new(
            (int)(Projectile.Center.X / 16f),
            (int)(Projectile.Center.Y / 16f));

        CardinalDirection currentDir = (CardinalDirection)(int)AiDirection;

        // === Loop Detection ===
        long stateKey = PackState(currentPos.X, currentPos.Y, currentDir);
        if (_visitedStates.Contains(stateKey))
        {
            LogTermination($"Loop detected (visited state already in set)");
            TransitionToBackfill();
            return false;
        }

        _visitedStates.Add(stateKey);

        // === Wall-Follow Step ===
        if (_follower == null)
        {
            _follower = new WallFollower(_handedness);
        }

        var stepResult = _follower.Step(currentPos, currentDir);
        if (stepResult == null)
        {
            LogTermination("Stuck — WallFollower returned null", currentPos);
            TransitionToBackfill();
            return false;
        }

        var (newPos, newDir) = stepResult.Value;
        AiDirection = (float)newDir;
        _pathIndex++;
        _stepsSinceLastTorch++;

        // === Track Pre-Existing Torches on the Outline ===
        // If the tile we just stepped onto has a torch that was already there
        // (not placed by us), record its path-index so CheckOutlineSpacing (S)
        // respects it. Without this, the wheel ignores existing torches for
        // path-distance spacing and places too close to them.
        {
            int nx = newPos.X, ny = newPos.Y;
            if (WorldGen.InWorld(nx, ny, 1))
            {
                Tile stepTile = Main.tile[nx, ny];
                if (stepTile.HasTile && TileID.Sets.Torch[stepTile.TileType])
                {
                    _existingTorchPathIndices.Add(_pathIndex);
                }
            }
        }

        // === Update Sliding Window ===
        _recentPath.Enqueue(newPos);
        if (_recentPath.Count > WindowSize)
        {
            _recentPath.Dequeue();
            // Prescan tiles are at the front of the queue - track when they're evicted
            if (_prescanWindowContribution > 0)
                _prescanWindowContribution--;
        }

        // === Placement Check ===
        // Allow placement when the window is full OR when prescan provides back-context
        // and the window center has reached the landing position (or beyond).
        bool windowFull = _recentPath.Count >= WindowSize;
        bool earlyPlacementAllowed = false;

        if (_prescanWindowContribution > 0 && !windowFull)
        {
            int windowCenter = _recentPath.Count / 2;
            int landingIndex = _prescanWindowContribution - 1;

            // Allow placement when center is at landing or a forward tile
            // AND we have at least SpacingS tiles of total context
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
                    int candidatePathIndex = _pathIndex - (_recentPath.Count / 2);
                    _placedTorches.Add((candidate, candidatePathIndex));
                    _torchesPlaced++;
                    _stepsSinceLastTorch = 0;

                    if (_torchesPlaced >= MaxTorches)
                    {
                        LogTermination($"MaxTorches limit reached ({MaxTorches})");
                        TransitionToBackfill();
                        return false;
                    }

                    Player owner = Main.player[Projectile.owner];
                    if (!TorchPlacementHelper.HasTorches(owner))
                    {
                        LogTermination("Player ran out of torches (forward)");
                        TransitionToBackfill();
                        return false;
                    }
                }
            }
        }

        // === Update logical position ===
        _targetPosition = new Vector2(newPos.X * 16 + 8, newPos.Y * 16 + 8);
        _windowLeadingEdge = _targetPosition; // Leading edge = newest tile = where we just stepped
        Projectile.Center = _targetPosition;

        return true;
    }

    /// <summary>
    /// Transitions from TracingForward to BackfillingPrescan.
    /// Backfill is performed instantly (no animation) because the projectile is
    /// far away on the outline at this point — teleporting back would be visually
    /// jarring. The player already saw the pre-scan region during phases 1-2.
    /// </summary>
    private void TransitionToBackfill()
    {
        // Initiate coast phase for smooth visual deceleration (if velocity model is active)
        if (SmoothVisualPath)
            StartCoastPhase();

        // Only backfill if there's a pre-scan region to fill
        if (_prescanPath.Count <= 1)
        {
            _state = TorchWheelState.Dead;
            return;
        }

        // Player out of torches → skip backfill
        Player owner = Main.player[Projectile.owner];
        if (!TorchPlacementHelper.HasTorches(owner))
        {
            _state = TorchWheelState.Dead;
            return;
        }

        // Torch limit already reached → skip backfill
        if (_torchesPlaced >= MaxTorches)
        {
            _state = TorchWheelState.Dead;
            return;
        }

        // Determine backfill range.
        // Pre-scan path: [landing(0), tile1, tile2, ..., anchor_or_limit(N)]
        // Backfill iterates from the far end toward landing, placing torches.
        int startIndex;
        if (_anchorFound)
        {
            // Start from the tile BEFORE the anchor (anchor already has a torch)
            startIndex = _prescanPath.Count - 2;
        }
        else
        {
            // No anchor — process the entire pre-scan path (except landing)
            startIndex = _prescanPath.Count - 1;
        }

        int placed = 0;
        for (int i = startIndex; i > 0; i--) // i > 0 skips landing site at index 0
        {
            Point16 pos = _prescanPath[i];

            if (CanPlaceAt(pos))
            {
                if (TryPlaceTorch(pos))
                {
                    _placedTorches.Add((pos, -i));
                    _torchesPlaced++;
                    placed++;

                    if (_torchesPlaced >= MaxTorches)
                        break;

                    if (!TorchPlacementHelper.HasTorches(Main.player[Projectile.owner]))
                        break;
                }
            }
        }

        Mod.Logger.Debug(
            $"[TorchWheel] Backfill complete (instant): placed {placed} torches " +
            $"across {startIndex} tiles, anchor={_anchorFound}");

        _state = TorchWheelState.Dead;
    }
}
