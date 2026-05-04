using Terraria.ModLoader.IO;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;

namespace WorldShapingWandsMod.Common.Settings;

/// <summary>
/// (S10 2026-04-29; <c>StencilMagicWandSelectionPlan.md</c> §0.2 + §7.)
/// Per-player configuration for Magic Wand (Read) — *one* config shared
/// across every stencil wand the player owns. The Read SubUI configures
/// *the player's Read*, not any particular wand's Read (per plan §7
/// "lives on `WandPlayer` — one config per player").
/// </summary>
/// <remarks>
/// <para><b>Persistence</b>: durable. Round-trips through the existing
/// <c>WandPlayer.SaveData</c> / <c>LoadData</c> pipeline as a 2-byte tag
/// pair (<c>"MagicWand_Object"</c>, <c>"MagicWand_Cont"</c>). The
/// captured Read result (<c>StoredMagicWandShape</c>) is in-memory only
/// per <c>MultipleStencilsPlan.md</c> §8 / Cavendish C-S1 §C2 — only the
/// CONFIG persists; the STAMPABLE shape does not.</para>
///
/// <para><b>v1 narrowness</b>: this struct holds only the two fields
/// the S4 design ratified. The S3-era <c>PaintChannel</c> field was
/// dropped — the channel choice is encoded directly in the
/// <see cref="ObjectType"/> value (<c>PaintTile</c> vs <c>PaintWall</c>),
/// so adding a separate channel field would create two contradictory
/// sources of truth. The struct is intentionally tiny and forward-
/// compatible: future fields (e.g. a hypothetical *"match liquid type
/// loosely"* tolerance) can be added without breaking the tag layout
/// because absent tags read as defaults.</para>
/// </remarks>
public struct MagicWandReadConfig
{
    /// <summary>The 12-cell taxonomy pick. Default: <see cref="MagicWandObjectType.SameTile"/>.</summary>
    public MagicWandObjectType ObjectType { get; set; }

    /// <summary>The 3-option contiguity radio pick. Default: <see cref="MagicWandContinuity.FourNeighbour"/>.</summary>
    public MagicWandContinuity Continuity { get; set; }

    /// <summary>
    /// (C-S3 2026-05-03) Read-time actuation filter. Default: <see cref="ActuationFilter.Both"/>.
    /// Controls which actuated state of tiles is admitted into the flood-fill result.
    /// </summary>
    public ActuationFilter ActuationFilter { get; set; }

    /// <summary>Default ctor produces (SameTile, FourNeighbour, Both) — the safest, most-recognisable Magic-Wand behaviour.</summary>
    public static MagicWandReadConfig Default => new()
    {
        ObjectType = MagicWandObjectType.SameTile,
        Continuity = MagicWandContinuity.FourNeighbour,
        ActuationFilter = ActuationFilter.Both,
    };

    private const string TagObject = "MagicWand_Object";
    private const string TagCont   = "MagicWand_Cont";
    private const string TagActFilter = "MagicWand_ActFilter"; // C-S3 2026-05-03
    private const string TagActFilterVersion = "MagicWand_ActFilter_V";

    /// <summary>
    /// Writes this config into the given save tag. Both fields use the
    /// underlying <c>byte</c> representation so the on-disk footprint is
    /// 2 bytes total. Absent tags on load round-trip as defaults
    /// (<see cref="Default"/>) — no migration step needed.
    /// </summary>
    public void Save(TagCompound tag)
    {
        tag[TagObject] = (byte)ObjectType;
        tag[TagCont]   = (byte)Continuity;
        tag[TagActFilter] = (byte)ActuationFilter;
        tag[TagActFilterVersion] = (byte)1;
    }

    /// <summary>
    /// Reads a config from the given save tag, defaulting any absent
    /// field to <see cref="Default"/>'s value. Out-of-range bytes
    /// (e.g. from a future enum value the player downgraded away from)
    /// also fall back to defaults to keep loads non-fatal.
    /// </summary>
    public static MagicWandReadConfig Load(TagCompound tag)
    {
        var cfg = Default;
        if (tag.ContainsKey(TagObject))
        {
            byte v = tag.GetByte(TagObject);
            if (v <= (byte)MagicWandObjectType.PaintWall) cfg.ObjectType = (MagicWandObjectType)v;
        }
        if (tag.ContainsKey(TagCont))
        {
            byte v = tag.GetByte(TagCont);
            if (v <= (byte)MagicWandContinuity.NonContiguous) cfg.Continuity = (MagicWandContinuity)v;
        }
        if (tag.ContainsKey(TagActFilter))
        {
            byte v = tag.GetByte(TagActFilter);
            if (v <= (byte)ActuationFilter.ActuatedOnly)
            {
                // Back-compat guard (C-S3 hotfix, 2026-05-03): some pre-version saves
                // may carry legacy value 2 as "Both". Without a version marker this
                // would be interpreted as ActuatedOnly in the current enum ordering.
                // Heuristic: if no version tag exists and value is 2, treat it as Both.
                bool hasVersion = tag.ContainsKey(TagActFilterVersion);
                if (!hasVersion && v == 2)
                    cfg.ActuationFilter = ActuationFilter.Both;
                else
                    cfg.ActuationFilter = (ActuationFilter)v;
            }
        }
        return cfg;
    }
}
