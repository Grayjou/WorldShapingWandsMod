#if DEBUG
using System;
using WorldShapingWandsMod.Common.Debug;

namespace WorldShapingWandsMod.Content.Projectiles;

/// <summary>
/// DevTunable registrations for the per-liquid rain dust system.
/// All parameters can be adjusted in real-time via <c>/dev set RainDust.X</c>.
/// <para>
/// Design reference: <c>dev_notes/inbox/RainLiquidDustSystem_Design.md</c>
/// </para>
/// </summary>
internal static class RainDustTunables
{
    // ── Dust Type IDs ──────────────────────────────────────────
    public static Func<int> WaterDustType;
    public static Func<int> LavaDustType;
    public static Func<int> LavaDustType2;
    public static Func<int> HoneyDustType;
    public static Func<int> ShimmerDustType1;
    public static Func<int> ShimmerDustType2;
    public static Func<int> ShimmerDustType3;

    // ── Spawn Rates (frames per dust — lower = faster) ─────────
    public static Func<int> WaterFramesPerDust;
    public static Func<int> LavaPrimaryFPD;
    public static Func<int> LavaSecondaryFPD;
    public static Func<int> HoneyFramesPerDust;
    public static Func<int> ShimmerDust1FPD;
    public static Func<int> ShimmerDust2FPD;
    public static Func<int> ShimmerDust3FPD;

    // ── Shared Spawn Parameters ────────────────────────────────
    public static Func<float> VelocityMin;
    public static Func<float> VelocityMax;
    public static Func<float> XDrift;
    public static Func<float> ScaleMin;
    public static Func<float> ScaleMax;
    public static Func<float> LavaScaleMin;
    public static Func<float> LavaScaleMax;
    public static Func<int> Alpha;
    public static Func<float> HorizSpread;

    // ── Lava Tint ──────────────────────────────────────────────
    public static Func<int> LavaTintR;
    public static Func<int> LavaTintG;
    public static Func<int> LavaTintB;

    // ── Honey Tint ─────────────────────────────────────────────
    public static Func<int> HoneyTintR;
    public static Func<int> HoneyTintG;
    public static Func<int> HoneyTintB;

    // ── Shimmer Colors ─────────────────────────────────────────
    public static Func<int> ShimmerTintR;
    public static Func<int> ShimmerTintG;
    public static Func<int> ShimmerTintB;
    public static Func<float> ShimmerCycleSpeed;

    // ── Per-Liquid Batch Sizes ─────────────────────────────────
    public static Func<int> WaterBatchSize;
    public static Func<int> LavaPrimaryBatchSize;
    public static Func<int> LavaSecondaryBatchSize;
    public static Func<int> HoneyBatchSize;
    public static Func<int> ShimmerDust1BatchSize;
    public static Func<int> ShimmerDust2BatchSize;
    public static Func<int> ShimmerDust3BatchSize;

    /// <summary>
    /// Registers all rain dust tunables. Called from <see cref="DevTunableDefaults.RegisterAll"/>.
    /// </summary>
    public static void RegisterAll()
    {
        // ── Dust Type IDs ──
        WaterDustType   = DevTunable.RegisterInt("RainDust.WaterDustType",   154, "Water rain DustID", min: 0, max: 300);
        LavaDustType    = DevTunable.RegisterInt("RainDust.LavaDustType",    35, "Lava primary DustID (Lava)", min: 0, max: 300); //Formerly Flare 127
        LavaDustType2   = DevTunable.RegisterInt("RainDust.LavaDustType2",   259, "Lava secondary DustID (SolarFlare)", min: 0, max: 300);
        HoneyDustType   = DevTunable.RegisterInt("RainDust.HoneyDustType",   153, "Honey rain DustID (Honey2)", min: 0, max: 300);
        ShimmerDustType1 = DevTunable.RegisterInt("RainDust.ShimmerDustType1", 181, "Shimmer primary DustID (CursedSkullBolt)", min: 0, max: 300);
        ShimmerDustType2 = DevTunable.RegisterInt("RainDust.ShimmerDustType2", 257, "Shimmer sparkle DustID (BubbleBlock)", min: 0, max: 300);
        ShimmerDustType3 = DevTunable.RegisterInt("RainDust.ShimmerDustType3",  87, "Shimmer accent DustID (GemTopaz)", min: 0, max: 300);

        // ── Spawn Rates ──
        WaterFramesPerDust     = DevTunable.RegisterInt("RainDust.WaterFramesPerDust",     1,  "Water: frames between dust spawns (1=every frame)", min: 1, max: 30);
        LavaPrimaryFPD         = DevTunable.RegisterInt("RainDust.LavaPrimaryFPD",         5,  "Lava Flare: frames per dust", min: 1, max: 30);
        LavaSecondaryFPD       = DevTunable.RegisterInt("RainDust.LavaSecondaryFPD",       3,  "Lava SolarFlare: frames per dust (slower = sparser embers)", min: 1, max: 30);
        HoneyFramesPerDust     = DevTunable.RegisterInt("RainDust.HoneyFramesPerDust",     4,  "Honey: frames per dust (viscous = slower)", min: 1, max: 30);
        ShimmerDust1FPD        = DevTunable.RegisterInt("RainDust.ShimmerDust1FPD",        4,  "Shimmer CursedSkullBolt: frames per dust", min: 1, max: 30);
        ShimmerDust2FPD        = DevTunable.RegisterInt("RainDust.ShimmerDust2FPD",        5,  "Shimmer BubbleBlock: frames per dust", min: 1, max: 30);
        ShimmerDust3FPD        = DevTunable.RegisterInt("RainDust.ShimmerDust3FPD",        5,  "Shimmer GemTopaz: frames per dust (accent = sparser)", min: 1, max: 30);

        // ── Shared Spawn Parameters ──
        VelocityMin = DevTunable.RegisterFloat("RainDust.VelocityMin", 3.0f, "Minimum downward velocity", min: 0.5f, max: 15f);
        VelocityMax = DevTunable.RegisterFloat("RainDust.VelocityMax", 6.0f, "Maximum downward velocity", min: 1f, max: 20f);
        XDrift      = DevTunable.RegisterFloat("RainDust.XDrift",      0.2f, "Horizontal drift range ±value", min: 0f, max: 3f);
        ScaleMin    = DevTunable.RegisterFloat("RainDust.ScaleMin",    1.0f, "Minimum particle scale (non-lava)", min: 0.3f, max: 3f);
        ScaleMax    = DevTunable.RegisterFloat("RainDust.ScaleMax",    1.3f, "Maximum particle scale (non-lava)", min: 0.5f, max: 5f);
        LavaScaleMin = DevTunable.RegisterFloat("RainDust.LavaScaleMin", 1.5f, "Lava SolarFlare (259) minimum scale", min: 0.3f, max: 5f);
        LavaScaleMax = DevTunable.RegisterFloat("RainDust.LavaScaleMax", 2.0f, "Lava SolarFlare (259) maximum scale", min: 0.5f, max: 5f);
        Alpha       = DevTunable.RegisterInt("RainDust.Alpha",         40,   "Particle alpha (0=opaque, 255=transparent)", min: 0, max: 255);
        HorizSpread = DevTunable.RegisterFloat("RainDust.HorizSpread", 48f,  "Horizontal spread (px) — matches tModDevVisuals test", min: 8f, max: 200f);

        // ── Lava Tint ──
        LavaTintR = DevTunable.RegisterInt("RainDust.LavaTintR", 255, "Lava tint red",   min: 0, max: 255);
        LavaTintG = DevTunable.RegisterInt("RainDust.LavaTintG",  85, "Lava tint green", min: 0, max: 255);
        LavaTintB = DevTunable.RegisterInt("RainDust.LavaTintB",   0, "Lava tint blue",  min: 0, max: 255);

        // ── Honey Tint ──
        HoneyTintR = DevTunable.RegisterInt("RainDust.HoneyTintR", 255, "Honey tint red",   min: 0, max: 255);
        HoneyTintG = DevTunable.RegisterInt("RainDust.HoneyTintG", 200, "Honey tint green", min: 0, max: 255);
        HoneyTintB = DevTunable.RegisterInt("RainDust.HoneyTintB",  50, "Honey tint blue",  min: 0, max: 255);

        // ── Shimmer Colors ──
        ShimmerTintR = DevTunable.RegisterInt("RainDust.ShimmerTintR", 127, "Shimmer primary tint red",   min: 0, max: 255);
        ShimmerTintG = DevTunable.RegisterInt("RainDust.ShimmerTintG",   0, "Shimmer primary tint green", min: 0, max: 255);
        ShimmerTintB = DevTunable.RegisterInt("RainDust.ShimmerTintB", 255, "Shimmer primary tint blue",  min: 0, max: 255);
        ShimmerCycleSpeed = DevTunable.RegisterFloat("RainDust.ShimmerCycleSpeed", 2.0f, "Shimmer color cycle frequency (Hz)", min: 0.5f, max: 10f);

        // ── Batch Sizes ──
        WaterBatchSize          = DevTunable.RegisterInt("RainDust.WaterBatchSize",          5, "Water dusts per eligible frame", min: 1, max: 20);
        LavaPrimaryBatchSize    = DevTunable.RegisterInt("RainDust.LavaPrimaryBatchSize",    1, "Lava Flare dusts per eligible frame", min: 1, max: 10);
        LavaSecondaryBatchSize  = DevTunable.RegisterInt("RainDust.LavaSecondaryBatchSize",  2, "Lava SolarFlare dusts per eligible frame", min: 0, max: 10);
        HoneyBatchSize          = DevTunable.RegisterInt("RainDust.HoneyBatchSize",          2, "Honey dusts per eligible frame", min: 1, max: 10);
        ShimmerDust1BatchSize   = DevTunable.RegisterInt("RainDust.ShimmerDust1BatchSize",   2, "Shimmer CursedSkullBolt per eligible frame", min: 1, max: 10);
        ShimmerDust2BatchSize   = DevTunable.RegisterInt("RainDust.ShimmerDust2BatchSize",   2, "Shimmer BubbleBlock per eligible frame", min: 1, max: 10);
        ShimmerDust3BatchSize   = DevTunable.RegisterInt("RainDust.ShimmerDust3BatchSize",   1, "Shimmer GemTopaz per eligible frame", min: 1, max: 10);
    }
}
#endif
