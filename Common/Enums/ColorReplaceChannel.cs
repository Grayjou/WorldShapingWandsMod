namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Selects which paint channel the Color Replace action operates on.
/// (S9 2026-04-28; ColorReplacePlan.md §3.4 — "Channel toggle restored".)
///
/// <para>S8 (2026-04-28) initially derived the channel from the wand's
/// <see cref="CoatingMode"/> (PaintTile → tile, PaintWall → wall). Per
/// GrayJou's S9 worried-client review, that conflation hid an originally
/// planned independent control: the player should be able to "Color
/// Replace on walls" without first switching the wand to PaintWall mode
/// and losing their Tile-mode brush state. S9 restores Channel as an
/// explicit, persistent setting on the SubUI, fully decoupled from the
/// Mode toggle row.</para>
/// </summary>
public enum ColorReplaceChannel : byte
{
    /// <summary>Operate on tile paint (default).</summary>
    Tile = 0,

    /// <summary>Operate on wall paint.</summary>
    Wall = 1,
}
