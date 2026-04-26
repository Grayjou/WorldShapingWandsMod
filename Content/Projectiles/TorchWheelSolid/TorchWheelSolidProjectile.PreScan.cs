using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using WorldShapingWandsMod.Common.Geometry;

namespace WorldShapingWandsMod.Content.Projectiles;

public partial class TorchWheelSolidProjectile
{
    // ================================================================
    //  Phase 1: Pre-Scanning — trace backward to find anchor torch
    // ================================================================

    /// <summary>
    /// Performs one pre-scan step: wall-follows backward from the landing site,
    /// looking for an existing torch (anchor) or reaching the S-step limit.
    /// Detects 45° handedness ambiguity if the pre-scan loops back to the
    /// landing site within <see cref="CorrectionThreshold"/> steps.
    /// </summary>
    /// <returns><c>true</c> to continue stepping this frame; <c>false</c> if state transitioned.</returns>
    private bool DoOnePreScanStep()
    {
        Point16 currentPos = new(
            (int)(Projectile.Center.X / 16f),
            (int)(Projectile.Center.Y / 16f));

        CardinalDirection currentDir = (CardinalDirection)(int)AiDirection;

        // === Pre-scan loop detection ===
        long stateKey = PackState(currentPos.X, currentPos.Y, currentDir);
        if (_prescanVisitedStates.Contains(stateKey))
        {
            // Already visited this exact (pos, dir) — stuck in a loop
            Mod.Logger.Debug($"[TorchWheel] PreScan: loop detected at ({currentPos.X},{currentPos.Y}) dir={currentDir}");
            FinishPreScan();
            return false;
        }
        _prescanVisitedStates.Add(stateKey);

        // === Wall-Follow Step ===
        var stepResult = _follower.Step(currentPos, currentDir);
        if (stepResult == null)
        {
            // Stuck — wall-follower can't find a valid move
            Mod.Logger.Debug($"[TorchWheel] PreScan: stuck at ({currentPos.X},{currentPos.Y})");
            FinishPreScan();
            return false;
        }

        var (newPos, newDir) = stepResult.Value;
        AiDirection = (float)newDir;

        // === 45° Correction Check ===
        // If we looped back to the landing site within CorrectionThreshold steps,
        // the handedness was wrong. Flip it and restart the pre-scan.
        if (newPos.X == _landingSite.X && newPos.Y == _landingSite.Y)
        {
            if (_prescanPath.Count <= CorrectionThreshold && !_correctionApplied)
            {
                // Flip handedness for BOTH pre-scan AND the eventual forward pass
                _handedness = _handedness == Handedness.Right ? Handedness.Left : Handedness.Right;
                _initialHandedness = _handedness;
                _correctionApplied = true;

                Mod.Logger.Debug(
                    $"[TorchWheel] PreScan: 45° correction — flipped to hand={_handedness}. " +
                    $"Restarting pre-scan.");

                // Reset pre-scan state
                _prescanPath.Clear();
                _prescanPath.Add(_landingSite);
                _prescanVisitedStates.Clear();
                _follower = new WallFollower(_handedness);

                // Reset position back to landing site
                Projectile.Center = new Vector2(_landingSite.X * 16 + 8, _landingSite.Y * 16 + 8);
                _targetPosition = Projectile.Center;

                // Re-set direction to reversed initial (with corrected handedness)
                CardinalDirection prescanDir = (CardinalDirection)(((int)_initialDirection + 2) % 4);
                AiDirection = (float)prescanDir;

                return true; // Continue stepping — restart pre-scan
            }

            // Loop-back AFTER threshold → genuine small structure. Finish pre-scan.
            Mod.Logger.Debug(
                $"[TorchWheel] PreScan: real loop back to landing after {_prescanPath.Count} steps");
            FinishPreScan();
            return false;
        }

        // === Check for existing torch (anchor) ===
        if (WorldGen.InWorld(newPos.X, newPos.Y, 1))
        {
            Tile tile = Main.tile[newPos.X, newPos.Y];
            if (tile.HasTile && TileID.Sets.Torch[tile.TileType])
            {
                _anchorFound = true;
                _anchorPosition = newPos;
                _prescanPath.Add(newPos);

                Mod.Logger.Debug(
                    $"[TorchWheel] PreScan: anchor torch found at ({newPos.X},{newPos.Y}) " +
                    $"after {_prescanPath.Count} steps");

                FinishPreScan();
                return false;
            }
        }

        // === Record position and continue ===
        _prescanPath.Add(newPos);

        // Update visual position so the projectile animates during pre-scan
        _targetPosition = new Vector2(newPos.X * 16 + 8, newPos.Y * 16 + 8);
        Projectile.Center = _targetPosition;

        // === Check step limit (S steps from landing) ===
        if (_prescanPath.Count > SpacingS)
        {
            Mod.Logger.Debug(
                $"[TorchWheel] PreScan: reached S-step limit ({SpacingS}), " +
                $"anchorFound={_anchorFound}");
            FinishPreScan();
            return false;
        }

        return true; // Continue stepping
    }

    /// <summary>
    /// Completes the pre-scan phase and transitions to ReturningToLanding.
    /// Sets up the cosmetic return animation index.
    /// </summary>
    private void FinishPreScan()
    {
        _prescanReturnIndex = _prescanPath.Count - 1;
        _state = TorchWheelState.ReturningToLanding;

        Mod.Logger.Debug(
            $"[TorchWheel] PreScan complete: {_prescanPath.Count} tiles, " +
            $"anchor={_anchorFound}, correction={_correctionApplied}");
    }
}
