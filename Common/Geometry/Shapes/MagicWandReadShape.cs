using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

/// <summary>
/// (S10 2026-04-29; <c>StencilMagicWandSelectionPlan.md</c> §0/§5/§7.)
/// Magic Wand — Read variant. Stencil-wands only. Runs the configured
/// flood-fill match (12-cell SampleMode taxonomy + 3-mode contiguity)
/// at <see cref="ShapeContext.Start"/> against the active stencil
/// canvas, returns the matching tile set, and captures the result on
/// <c>WandPlayer.LastMagicWandShape</c> for later replay by
/// downstream stamp consumers.
/// </summary>
/// <remarks>
/// <para><b>Pure-shape contract</b>: <see cref="GetTiles"/> is the
/// canonical entry point and is deterministic given (origin + active
/// canvas snapshot + player config). The capture-into-<c>WandPlayer</c>
/// side effect runs on the LOCAL player only and only when invoked from
/// a real world click (not from preview/overlay polling) — the gate is
/// <c>Main.LocalPlayer.whoAmI &gt;= 0 &amp;&amp; Start == End</c>, the same
/// "single-point stamp" signature MoldShape uses to recognise a
/// committed cast vs a hover preview.</para>
///
/// <para><b>Domain</b>: the active <c>MoldingWandPlayer.Canvas</c>
/// (the Phase 3 passthrough getter — flips with the active stencil
/// slot). Empty canvas = empty result; the player gets the standard
/// "use the molding wand to set up a canvas first" workflow because the
/// Read shape inherently needs a domain to flood across.</para>
///
/// <para><b>S11+ scheduled</b>: chat-warning emission for the four
/// non-Success <see cref="MagicWandReadFn.ReadStatus"/> values is
/// stubbed with code-comments here. The actual <c>Msg.LocalChat</c>
/// calls will land alongside the Read SubUI in S11+ once locale keys
/// for the warnings ship — keeping the shape provider deterministic
/// (no I/O during a hover-preview pass) is more important than
/// emitting the warning at the source. The capture path already runs
/// only on real clicks, so the warning will fire from the correct
/// site when wired.</para>
///
/// <para><b>UI gating</b>: the shape's *availability* (i.e. whether it
/// shows up in the Shape Selector grid) is enforced by the panel-side
/// builder (<c>MoldingSettingsPanel</c> + sibling stencil panels), NOT
/// by the registry — the registry registers everything; consumer
/// panels decide what to render. Today the shape provider compiles +
/// runs but is not yet rendered anywhere; S11+ adds the cell.</para>
/// </remarks>
public class MagicWandReadShape : IShapeProvider
{
    private const int ReadStatusChatCooldownTicks = 30;
    private static readonly Dictionary<int, (MagicWandReadFn.ReadStatus Status, ulong Tick)> _lastStatusByPlayer = new();
    private static readonly Dictionary<int, bool> _processedReadThisMousePressByPlayer = new();
    private static readonly Dictionary<int, bool> _blockedReadThisMousePressByPlayer = new();
    private static readonly Dictionary<int, SelectionMode> _lastSelectionModeByPlayer = new();
    // (C-S1.b 2026-05-03) Tracks Main.mouseLeft per player from the previous
    // GetTiles call. The processed/blocked dicts above must reset on the
    // mouse-up edge of every physical press; previously the reset was buried
    // inside ShouldProcessReadForCommit which only ran on commit frames, so
    // an instant-mode release would set processed=true and the next hover
    // frame would short-circuit BEFORE reaching the reset path — leaving
    // processed=true forever and Read silently no-op'ing on every subsequent
    // press. Symptom: "Magic Wand only works once or twice, then never again
    // until the mod is rebuilt" (rebuild reinitialises the static dicts).
    private static readonly Dictionary<int, bool> _mouseLeftPrevFrameByPlayer = new();

    internal static void TickPerFrameState(int playerId, SelectionMode currentMode)
    {
        MaintainMousePressState(playerId);
        MaintainModeSwitchState(playerId, currentMode);
    }

    public ShapeType ShapeType => ShapeType.MagicWandRead;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var emptySet = new HashSet<Point>();
        var emptyResult = new ShapeTileSet(emptySet, emptySet);

        // Resolve the local player's stencil-wand state. Magic Wand Read
        // is stencil-only, so we always look at MoldingWandPlayer's
        // active canvas (Phase 3 passthrough — the active slot's canvas).
        var player = Main.LocalPlayer;
        if (player?.active != true) return emptyResult;
        var mwp = player.GetModPlayer<MoldingWandPlayer>();
        var wp = player.GetModPlayer<WandPlayer>();
        if (mwp == null || wp == null) return emptyResult;

        // (C-S1.b 2026-05-03) Always run the mouse-up edge maintenance BEFORE
        // any short-circuit return below. This is the single robust trigger
        // that clears the once-per-press flags; if it lives inside the
        // commit branch instead, instant-mode releases never get cleaned up
        // and Read locks to a permanent no-op (see field comment above).
        SelectionMode heldMode = ResolveHeldSelectionMode(player);
        TickPerFrameState(player.whoAmI, heldMode);

        bool isInstantReleaseCommit = IsInstantReleaseCommit(wp);
        bool isSelectionClickCommit = IsSelectionClickCommit(wp);
        bool isCommit = context.Start == context.End || isInstantReleaseCommit || isSelectionClickCommit;

        // Hover / movement path: never rerun Read flood-fill. Return the most
        // recent committed capture so camera/player movement cannot trigger
        // per-frame flood recomputation.
        if (!isCommit)
            return BuildStoredShapeResult(wp.LastMagicWandShape, emptyResult);

        // Commit path: process at most once per physical left-mouse press.
        // Subsequent GetTiles evaluations while the button remains down reuse
        // the previously captured shape.
        bool shouldProcessCommit = ShouldProcessReadForCommit(player.whoAmI, isInstantReleaseCommit);
        if (!shouldProcessCommit)
        {
            if (WasReadBlockedThisMousePress(player.whoAmI))
            {
                wp.ClearSelectionAfterCommit();
                return emptyResult;
            }
            return BuildStoredShapeResult(wp.LastMagicWandShape, emptyResult);
        }

        var safetyCfg = WandConfigs.MagicWandReadSafety;
        bool allowReadInInstantMode = safetyCfg?.AllowReadInInstantMode ?? false;
        bool allowReadInSelectMode = safetyCfg?.AllowReadInSelectMode ?? false;
        bool allowReadWithoutCanvas = safetyCfg?.AllowReadWithoutCanvas ?? false;
        bool applyBlockedByMode = IsApplyBlockedByMode(heldMode, allowReadInInstantMode, allowReadInSelectMode);

        var canvas = mwp.Canvas;
        bool isUsingFallbackDomain = canvas == null || canvas.Count == 0;
        bool applyBlockedByCanvasGuard = isUsingFallbackDomain && !allowReadWithoutCanvas;

        // Fallback domain resolution (G-38/G-40): if no canvas or empty canvas,
        // create a cursor-centered 168×125 rectangle as the domain.
        if (isUsingFallbackDomain)
        {
            canvas = ResolveFallbackDomain();
            if (canvas == null || canvas.Count == 0)
            {
                EmitNoCanvasChat(player.whoAmI);
                wp.ClearSelectionAfterCommit();
                return emptyResult;
            }
        }

        // Cap: pick the stencil-wand cap from the server-authoritative
        // LimitsConfig. Magic Wand Read has its own area-semantics cap
        // (MagicWandReadMaxArea), independent from Molding dimension caps.
        int cap = ResolveCap();

        // (C-S1 2026-05-03) Safety gate runs BEFORE flood-fill capture.
        // Previously the capture into wp.LastMagicWandShape happened first,
        // which meant a Read blocked by config still poisoned the stored
        // shape — the next downstream stamp consumer (or anything reading
        // LastMagicWandShape) replayed the blocked tile set, making the
        // safety configs feel like they did nothing. Now: blocked → no
        // flood-fill, no capture, explicit chat. Defence-in-depth on the
        // Legacy Apply-side / downstream stamp consumers cover any pre-existing
        // capture from before the user flipped the config restrictive.
        if (applyBlockedByMode || applyBlockedByCanvasGuard)
        {
            EmitBlockedChat(player.whoAmI, applyBlockedByMode, applyBlockedByCanvasGuard, heldMode);
            MarkReadBlockedForMousePress(player.whoAmI);
            wp.ClearSelectionAfterCommit();
            return emptyResult;
        }

        var origin = context.Start;
        var (tiles, status) = MagicWandReadFn.Read(origin, wp.MagicWandReadConfig, canvas, cap);

        // Capture into player-scoped slot — only on a "real cast"
        // signature (single-point stamp; Start == End). Hover previews
        // pass distinct Start/End or repeat-Start frames; we don't want
        // every preview frame to overwrite the last successful capture.
        EmitReadStatusChat(player.whoAmI, status, cap);

        if (status != MagicWandReadFn.ReadStatus.Empty)
        {
            wp.LastMagicWandShape = new StoredMagicWandShape(
                tiles: new HashSet<Point>(tiles),
                origin: origin,
                configAtCapture: wp.MagicWandReadConfig,
                capturedAtTicks: DateTime.UtcNow.Ticks);
        }
        else
        {
            // (C-S5 2026-05-04) B-1 fix: a committed Read that found no tiles
            // must not leave a ghost selection (sel=true, mw=0). The commit
            // path already advanced SelectionClickStep to 1 before GetTiles was
            // called; if we return here without clearing, the overlay reports
            // sel=True / mw=0 and the next left-click executes against a
            // zero-tile shape — dismantling nothing but consuming the click.
            // ClearSelectionAfterCommit resets sel/step/mw in one call.
            wp.ClearSelectionAfterCommit();
        }

        // Boundary: 4-neighbour edge mask, same convention as every
        // other shape in the registry. Re-uses the canvas helper that
        // already proved itself for stencil rendering.
        var boundary = GeometryHelper.GetBoundaryTiles4(tiles);
        return new ShapeTileSet(tiles, boundary);
    }

    /// <summary>
    /// (C-S1 2026-05-03) When the safety config blocks a Read, tell the
    /// player WHY the click did nothing. Without this, a user who has
    /// kept the defaults sees "I clicked Read and nothing happened" and
    /// concludes the wand is broken — exactly what was reported.
    /// </summary>
    private static void EmitBlockedChat(int playerId, bool blockedByMode, bool blockedByCanvas, SelectionMode mode)
    {
        // Cooldown ride-along: reuse the Read status throttle so a
        // hold-down doesn't spam chat. Status.Empty is the closest
        // semantic neighbour (no tiles delivered).
        if (!ShouldEmitStatus(playerId, MagicWandReadFn.ReadStatus.Empty)) return;

        if (blockedByMode)
        {
            string key = mode == SelectionMode.OneClick
                ? "MagicWandReadBlockedInstantMode"
                : "MagicWandReadBlockedSelectMode";
            Main.NewText(Msg.Get(key), WandColors.MsgWarning);
            return;
        }

        if (blockedByCanvas)
        {
            Main.NewText(Msg.Get("MagicWandReadBlockedNoCanvas"), WandColors.MsgWarning);
        }
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        // Shape membership is the live Read result; for an
        // already-captured store we'd defer to LastMagicWandShape, but
        // the more useful contract here is "is this point in the most
        // recent freshly-computed Read?" — which is what GetTiles
        // provides. ShapeTileSet returns IEnumerable; HashSet-cast (or
        // materialise) for an O(1) Contains.
        var tileSet = GetTiles(context);
        var set = tileSet.Tiles as HashSet<Point> ?? new HashSet<Point>(tileSet.Tiles);
        return set.Contains(point);
    }

    public (int Width, int Height) GetDisplayDimensions(ShapeContext context)
    {
        // Display dims for HUD / cursor — bounding-box of the matching
        // set. Cheap because the canvas is already capped.
        var tileSet = GetTiles(context);
        var set = tileSet.Tiles as HashSet<Point> ?? new HashSet<Point>(tileSet.Tiles);
        if (set.Count == 0) return (0, 0);
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var p in set)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }
        return (maxX - minX + 1, maxY - minY + 1);
    }

    private static ShapeTileSet BuildStoredShapeResult(StoredMagicWandShape stored, ShapeTileSet emptyResult)
    {
        if (stored?.Tiles == null || stored.Tiles.Count == 0)
            return emptyResult;

        var tiles = new HashSet<Point>(stored.Tiles);
        var boundary = GeometryHelper.GetBoundaryTiles4(tiles);
        return new ShapeTileSet(tiles, boundary);
    }

    /// <summary>
    /// Resolves the cap from server-authoritative <c>LimitsConfig</c>.
    /// Uses <c>MagicWandReadMaxArea</c> — an area-semantics cap tunable
    /// independently from the Molding dimension cap.
    /// Defensive default of 5000 if config is unavailable (matches the
    /// shipped default value in <c>LimitsConfig</c>).
    /// </summary>
    private static int ResolveCap()
    {
        try
        {
            var cfg = ModContent.GetInstance<LimitsConfig>();
            return cfg?.MagicWandReadMaxArea ?? 5000;
        }
        catch
        {
            return 5000;
        }
    }

    /// <summary>
    /// (G-38/G-40, S4 2026-05-02)
    /// Resolves the fallback domain for Magic Wand Read when no explicit
    /// stencil canvas exists or the canvas is empty.
    /// 
    /// Creates a rectangular domain centered on the cursor with dimensions
    /// 168×125 (width × height in tile units), clamped to world bounds
    /// (0 ≤ x < Main.maxTilesX, 0 ≤ y < Main.maxTilesY).
    /// 
    /// Returns null if the fallback rectangle would be entirely out-of-bounds
    /// or if cursor position cannot be determined.
    /// </summary>
    private static SelectionCanvas ResolveFallbackDomain()
    {
        const int FallbackWidth = 168;
        const int FallbackHeight = 125;

        try
        {
            // Cursor position in world tile coordinates.
            // Main.mouseX/Y are screen pixels; convert to world coords.
            int cursorTileX = (Main.mouseX + (int)Main.screenPosition.X) / 16;
            int cursorTileY = (Main.mouseY + (int)Main.screenPosition.Y) / 16;

            // Center the rectangle on the cursor.
            int rectLeft   = cursorTileX - FallbackWidth / 2;
            int rectTop    = cursorTileY - FallbackHeight / 2;
            int rectRight  = rectLeft + FallbackWidth - 1;  // Inclusive
            int rectBottom = rectTop + FallbackHeight - 1;  // Inclusive

            // Clamp to world bounds.
            int minX = Math.Max(0, rectLeft);
            int minY = Math.Max(0, rectTop);
            int maxX = Math.Min(Main.maxTilesX - 1, rectRight);
            int maxY = Math.Min(Main.maxTilesY - 1, rectBottom);

            // If clamped rectangle is invalid (entirely out-of-bounds), return null.
            if (minX > maxX || minY > maxY)
                return null;

            // Build the canvas from all tiles in the clamped rectangle.
            var canvas = new SelectionCanvas();
            var tilesToAdd = new HashSet<Point>();
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    tilesToAdd.Add(new Point(x, y));
                }
            }

            canvas.ApplyOperation(tilesToAdd, CanvasOperation.Add);
            return canvas;
        }
        catch
        {
            // Defensive: if cursor conversion or rectangle calculation fails, return null.
            return null;
        }
    }


    private static void EmitNoCanvasChat(int playerId)
    {
        if (!ShouldEmitStatus(playerId, MagicWandReadFn.ReadStatus.Empty)) return;
        Main.NewText(Msg.Get("MagicWandReadNoCanvas"), WandColors.MsgWarning);
    }

    private static void EmitReadStatusChat(int playerId, MagicWandReadFn.ReadStatus status, int cap)
    {
        if (status == MagicWandReadFn.ReadStatus.Success) return;
        if (!ShouldEmitStatus(playerId, status)) return;

        switch (status)
        {
            case MagicWandReadFn.ReadStatus.Empty:
                Main.NewText(Msg.Get("MagicWandReadEmpty"), WandColors.MsgHint);
                break;
            case MagicWandReadFn.ReadStatus.Capped:
                Main.NewText(Msg.Get("MagicWandReadCapped", cap), WandColors.MsgWarning);
                break;
            case MagicWandReadFn.ReadStatus.UnpaintedOrigin:
                Main.NewText(Msg.Get("MagicWandReadUnpaintedOrigin"), WandColors.MsgWarning);
                break;
        }
    }

    private static bool ShouldEmitStatus(int playerId, MagicWandReadFn.ReadStatus status)
    {
        ulong now = Main.GameUpdateCount;
        if (_lastStatusByPlayer.TryGetValue(playerId, out var last)
            && last.Status == status
            && now - last.Tick < ReadStatusChatCooldownTicks)
        {
            return false;
        }

        _lastStatusByPlayer[playerId] = (status, now);
        return true;
    }

    private static bool ShouldProcessReadForCommit(int playerId, bool isInstantReleaseCommit)
    {
        if (playerId < 0) return false;

        // One chat emission per physical left-mouse press operation.
        // GetTiles can be evaluated multiple times while the button is still
        // down (especially near domain borders); those re-evaluations should
        // stay silent after the first emission.
        if (!Main.mouseLeft)
        {
            if (isInstantReleaseCommit)
            {
                if (_processedReadThisMousePressByPlayer.TryGetValue(playerId, out bool alreadyProcessedRelease) && alreadyProcessedRelease)
                    return false;

                _processedReadThisMousePressByPlayer[playerId] = true;
                return true;
            }

            _processedReadThisMousePressByPlayer[playerId] = false;
            _blockedReadThisMousePressByPlayer[playerId] = false;
            return false;
        }

        if (_processedReadThisMousePressByPlayer.TryGetValue(playerId, out bool alreadyProcessed) && alreadyProcessed)
            return false;

        _processedReadThisMousePressByPlayer[playerId] = true;
        return true;
    }

    private static void MarkReadBlockedForMousePress(int playerId)
    {
        if (playerId < 0) return;
        _blockedReadThisMousePressByPlayer[playerId] = true;
    }

    private static bool WasReadBlockedThisMousePress(int playerId)
    {
        if (playerId < 0) return false;
        return _blockedReadThisMousePressByPlayer.TryGetValue(playerId, out bool blocked) && blocked;
    }

    /// <summary>
    /// (C-S1.b 2026-05-03) Detects the mouse-up edge per player and clears
    /// the once-per-press flags on that transition. Must be called every
    /// time <see cref="GetTiles"/> runs (BEFORE any short-circuit return)
    /// so it observes every frame, not just commit frames.
    /// <para>The transition we care about is <c>true → false</c>: that is
    /// the moment a physical left-mouse press has just ended. Resetting on
    /// that edge means the next press starts with clean flags. This frame
    /// is also the same frame an instant-release commit fires, but the
    /// commit logic runs AFTER this reset and re-sets processed=true — so
    /// no commit is lost.</para>
    /// </summary>
    private static void MaintainMousePressState(int playerId)
    {
        if (playerId < 0) return;

        bool prev = _mouseLeftPrevFrameByPlayer.TryGetValue(playerId, out bool p) && p;
        // (C-S3 2026-05-03) Ignore mouseLeft while the player has UI focus — UI mouse
        // polling can flip the global flag without a real game-world press, which would
        // keep _processedReadThisMousePressByPlayer stuck at true across frames.
        bool now = Main.mouseLeft && !Main.LocalPlayer.mouseInterface;

        if (prev && !now)
        {
            _processedReadThisMousePressByPlayer[playerId] = false;
            _blockedReadThisMousePressByPlayer[playerId] = false;
        }

        _mouseLeftPrevFrameByPlayer[playerId] = now;
    }

    private static SelectionMode ResolveHeldSelectionMode(Player player)
    {
        var wand = player?.HeldItem?.ModItem as BaseCyclingWand;
        return wand?.WandSelectionMode ?? SelectionMode.OneClick;
    }

    private static bool IsInstantReleaseCommit(WandPlayer wp)
    {
        if (wp == null) return false;
        return !Main.mouseLeft
            && wp.InstantSelection.IsActive
            && wp.IsInstantSelectionOwnedByCurrentItem();
    }

    private static bool IsSelectionClickCommit(WandPlayer wp)
    {
        if (wp == null) return false;
        return Main.mouseLeft
            && !wp.InstantSelection.IsActive
            && wp.Selection.IsActive;
    }

    private static void MaintainModeSwitchState(int playerId, SelectionMode currentMode)
    {
        if (playerId < 0) return;

        if (_lastSelectionModeByPlayer.TryGetValue(playerId, out SelectionMode previousMode)
            && previousMode != currentMode)
        {
            _processedReadThisMousePressByPlayer[playerId] = false;
            _blockedReadThisMousePressByPlayer[playerId] = false;
            _mouseLeftPrevFrameByPlayer[playerId] = false;
        }

        _lastSelectionModeByPlayer[playerId] = currentMode;
    }

    private static bool IsApplyBlockedByMode(SelectionMode mode, bool allowReadInInstantMode, bool allowReadInSelectMode)
    {
        if (mode == SelectionMode.OneClick && !allowReadInInstantMode)
            return true;
        if (mode == SelectionMode.TwoClick && !allowReadInSelectMode)
            return true;
        return false;
    }
}
