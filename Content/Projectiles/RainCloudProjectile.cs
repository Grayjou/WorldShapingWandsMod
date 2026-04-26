using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;

namespace WorldShapingWandsMod.Content.Projectiles;

/// <summary>
/// Purely cosmetic rain cloud that spawns above the selection during Rain Fill.
/// Does NOT place any liquid — all placement is handled by WandOfFluidsBase.
/// Spawns per-liquid-type dust rain particles for visual effect, then fades out.
/// </summary>
/// <remarks>
/// Liquid type is passed via <c>ai[2]</c>: 0=Water, 1=Lava, 2=Honey, 3=Shimmer.
/// Each liquid has its own dust composite with independent spawn cadences,
/// tint colors, and batch sizes — all tunable via <c>/dev set RainDust.*</c> in DEBUG.
/// <para>Design reference: <c>dev_notes/inbox/RainLiquidDustSystem_Design.md</c></para>
/// </remarks>
public class RainCloudProjectile : ModProjectile
{
    // ── ai[] layout (synced in MP) ──
    // ai[0]: sprite variant index (0, 1, or 2) — set at spawn
    // ai[1]: lifetime counter (incremented each frame)
    // ai[2]: liquid type (0=Water, 1=Lava, 2=Honey, 3=Shimmer)

    // ── localAI[] layout (NOT synced — client-only) ──
    // localAI[0]: random scale multiplier (0.85–1.15) — set on first frame
    // localAI[1]: random Y pixel offset (±2) — set on first frame
    // localAI[2]: fade-in variation (±5 ticks) — set on first frame

    private const int DefaultLifetime = 120;   // 2 seconds
    private const int FadeInEnd = 15;           // first 15 ticks
    private const int RainStart = 15;           // rain starts after fade-in
    private const int RainEnd = 90;             // rain stops at tick 90
    private const int FadeOutStart = 90;        // last 30 ticks fade out

    // ── Release-mode defaults (baked from DevTunable calibration) ──
    private const int DefaultWaterDustType = 154;
    private const int DefaultLavaDustType = 127;
    private const int DefaultLavaDustType2 = 259;
    private const int DefaultHoneyDustType = 153;
    private const int DefaultShimmerDustType1 = 181;
    private const int DefaultShimmerDustType2 = 257;
    private const int DefaultShimmerDustType3 = 87;

    private bool _initialized = false;
    private int _frameCounter;

    /// <summary>Liquid type from ai[2]: 0=Water, 1=Lava, 2=Honey, 3=Shimmer.</summary>
    private byte LiquidType => (byte)Projectile.ai[2];

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 3; // 3 sprite variants in spritesheet
    }

    public override void SetDefaults()
    {
        Projectile.width = 54;
        Projectile.height = 28;
        Projectile.friendly = false;        // No damage
        Projectile.hostile = false;         // No damage
        Projectile.penetrate = -1;          // Doesn't interact with entities
        Projectile.tileCollide = false;     // Passes through tiles
        Projectile.ignoreWater = true;      // Unaffected by liquid
        Projectile.timeLeft = DefaultLifetime;
        Projectile.light = 0f;             // No light emission
        Projectile.alpha = 255;            // Start fully transparent (fade in)
        Projectile.netImportant = true;    // Priority MP sync
    }

    public override void OnSpawn(IEntitySource source)
    {
        // ai[0] = sprite variant (set by caller)
        // ai[2] = liquid type (set by caller: 0=Water, 1=Lava, 2=Honey, 3=Shimmer)
    }

    public override void AI()
    {
        // One-time init for client-local random values
        if (!_initialized)
        {
            Projectile.localAI[0] = 0.85f + Main.rand.NextFloat() * 0.30f; // scale: 0.85–1.15
            Projectile.localAI[1] = Main.rand.Next(-2, 3);                  // Y offset: -2 to +2
            Projectile.localAI[2] = Main.rand.Next(-5, 6);                  // fade timing variation
            Projectile.frame = (int)Projectile.ai[0] % Main.projFrames[Projectile.type];
            _initialized = true;
        }

        float life = Projectile.ai[1];
        float fadeVar = Projectile.localAI[2];
        float adjustedFadeIn = FadeInEnd + fadeVar;
        float adjustedFadeOut = FadeOutStart - fadeVar;

        // ── Fade In ──
        if (life < adjustedFadeIn)
        {
            float progress = life / adjustedFadeIn;
            Projectile.alpha = (int)(255 * (1f - progress));
        }
        // ── Fully Visible ──
        else if (life < adjustedFadeOut)
        {
            Projectile.alpha = 0;
        }
        // ── Fade Out ──
        else
        {
            float progress = (life - adjustedFadeOut) / (DefaultLifetime - adjustedFadeOut);
            Projectile.alpha = (int)(255 * MathHelper.Clamp(progress, 0f, 1f));
        }

        // ── Rain Dust Particles (per-liquid dispatch) ──
        if (life >= RainStart && life <= RainEnd && Projectile.alpha < 200
            && WandConfigs.Preferences.RainFillSpawnDusts)
        {
            _frameCounter++;
            float cloudBottom = Projectile.position.Y + Projectile.height;
            switch (LiquidType)
            {
                case 0: SpawnWaterDust(cloudBottom); break;
                case 1: SpawnLavaDust(cloudBottom); break;
                case 2: SpawnHoneyDust(cloudBottom); break;
                case 3: SpawnShimmerDust(cloudBottom); break;
            }
        }

        // ── Increment Lifetime ──
        Projectile.ai[1] += 1f;

        // Apply scale from localAI
        Projectile.scale = Projectile.localAI[0];
    }

    // ================================================================
    //  Per-Liquid Dust Spawn Methods
    // ================================================================

    // ── Tunable property helpers (DEBUG reads live, release uses constants) ──
    #if DEBUG
    private int WaterDustType => RainDustTunables.WaterDustType();
    private int LavaDustTypeId => RainDustTunables.LavaDustType();
    private int LavaDustType2Id => RainDustTunables.LavaDustType2();
    private int HoneyDustTypeId => RainDustTunables.HoneyDustType();
    private int ShimmerDust1Id => RainDustTunables.ShimmerDustType1();
    private int ShimmerDust2Id => RainDustTunables.ShimmerDustType2();
    private int ShimmerDust3Id => RainDustTunables.ShimmerDustType3();
    private int WaterFPD => RainDustTunables.WaterFramesPerDust();
    private int LavaPrimaryFPD => RainDustTunables.LavaPrimaryFPD();
    private int LavaSecondaryFPD => RainDustTunables.LavaSecondaryFPD();
    private int HoneyFPD => RainDustTunables.HoneyFramesPerDust();
    private int Shimmer1FPD => RainDustTunables.ShimmerDust1FPD();
    private int Shimmer2FPD => RainDustTunables.ShimmerDust2FPD();
    private int Shimmer3FPD => RainDustTunables.ShimmerDust3FPD();
    private float VelocityMin => RainDustTunables.VelocityMin();
    private float VelocityMax => RainDustTunables.VelocityMax();
    private float XDrift => RainDustTunables.XDrift();
    private float ScaleMin => RainDustTunables.ScaleMin();
    private float ScaleMax => RainDustTunables.ScaleMax();
    private float LavaScaleMin => RainDustTunables.LavaScaleMin();
    private float LavaScaleMax => RainDustTunables.LavaScaleMax();
    private int DustAlpha => RainDustTunables.Alpha();
    private float HorizSpread => RainDustTunables.HorizSpread();
    private int WaterBatch => RainDustTunables.WaterBatchSize();
    private int LavaPrimaryBatch => RainDustTunables.LavaPrimaryBatchSize();
    private int LavaSecondaryBatch => RainDustTunables.LavaSecondaryBatchSize();
    private int HoneyBatch => RainDustTunables.HoneyBatchSize();
    private int Shimmer1Batch => RainDustTunables.ShimmerDust1BatchSize();
    private int Shimmer2Batch => RainDustTunables.ShimmerDust2BatchSize();
    private int Shimmer3Batch => RainDustTunables.ShimmerDust3BatchSize();
    private Color LavaTint => new(RainDustTunables.LavaTintR(), RainDustTunables.LavaTintG(), RainDustTunables.LavaTintB());
    private Color HoneyTint => new(RainDustTunables.HoneyTintR(), RainDustTunables.HoneyTintG(), RainDustTunables.HoneyTintB());
    private Color ShimmerTint => new(RainDustTunables.ShimmerTintR(), RainDustTunables.ShimmerTintG(), RainDustTunables.ShimmerTintB());
    #else
    private int WaterDustType => DefaultWaterDustType;
    private int LavaDustTypeId => DefaultLavaDustType;
    private int LavaDustType2Id => DefaultLavaDustType2;
    private int HoneyDustTypeId => DefaultHoneyDustType;
    private int ShimmerDust1Id => DefaultShimmerDustType1;
    private int ShimmerDust2Id => DefaultShimmerDustType2;
    private int ShimmerDust3Id => DefaultShimmerDustType3;
    private int WaterFPD => 1;
    private int LavaPrimaryFPD => 2;
    private int LavaSecondaryFPD => 3;
    private int HoneyFPD => 4;
    private int Shimmer1FPD => 4;
    private int Shimmer2FPD => 5;
    private int Shimmer3FPD => 5;
    private float VelocityMin => 3.0f;
    private float VelocityMax => 6.0f;
    private float XDrift => 0.2f;
    private float ScaleMin => 1.0f;
    private float ScaleMax => 1.3f;
    private float LavaScaleMin => 1.5f;
    private float LavaScaleMax => 2.0f;
    private int DustAlpha => 40;
    private float HorizSpread => 48f;
    private int WaterBatch => 5;
    private int LavaPrimaryBatch => 1;
    private int LavaSecondaryBatch => 2;
    private int HoneyBatch => 2;
    private int Shimmer1Batch => 2;
    private int Shimmer2Batch => 2;
    private int Shimmer3Batch => 1;
    private Color LavaTint => new(255, 85, 0);
    private Color HoneyTint => new(255, 200, 50);
    private Color ShimmerTint => new(127, 0, 255);
    #endif

    /// <summary>Checks if the current frame is eligible for dust spawn at the given cadence.</summary>
    private bool IsEligible(int framesPerDust) => _frameCounter % framesPerDust == 0;

    private void SpawnWaterDust(float cloudBottom)
    {
        if (!IsEligible(WaterFPD)) return;

        for (int i = 0; i < WaterBatch; i++)
        {
            var dust = Dust.NewDustDirect(
                new Vector2(Projectile.Center.X + Main.rand.NextFloat(-HorizSpread, HorizSpread), cloudBottom),
                2, 2, WaterDustType);
            dust.velocity = new Vector2(Main.rand.NextFloat(-XDrift, XDrift), Main.rand.NextFloat(VelocityMin, VelocityMax));
            dust.scale = Main.rand.NextFloat(ScaleMin, ScaleMax);
            dust.alpha = DustAlpha;
            dust.noGravity = false;
        }
    }

    private void SpawnLavaDust(float cloudBottom)
    {
        Color lavaTint = LavaTint;
        var style = WandConfigs.Preferences?.LavaRainStyle ?? Common.Enums.LavaRainStyle.Embers;
        bool wantEmbers = style == Common.Enums.LavaRainStyle.Embers || style == Common.Enums.LavaRainStyle.Both;
        bool wantSolarFlare = style == Common.Enums.LavaRainStyle.SolarFlare || style == Common.Enums.LavaRainStyle.Both;

        // Primary: bright glowing embers core (dust 127, OrangeTorch).
        if (wantEmbers && IsEligible(LavaPrimaryFPD))
        {
            for (int i = 0; i < LavaPrimaryBatch; i++)
            {
                var dust = Dust.NewDustDirect(
                    new Vector2(Projectile.Center.X + Main.rand.NextFloat(-HorizSpread, HorizSpread), cloudBottom),
                    2, 2, LavaDustTypeId);
                dust.velocity = new Vector2(Main.rand.NextFloat(-XDrift, XDrift), Main.rand.NextFloat(VelocityMin, VelocityMax));
                dust.scale = Main.rand.NextFloat(ScaleMin, ScaleMax);
                dust.alpha = DustAlpha;
                dust.color = lavaTint;
                dust.noGravity = false;
                dust.noLight = false;
            }
        }

        // Secondary: SolarFlare blobs (dust 259) — slower, larger, dreamier.
        // History: removed Session 2026-04-20 S1 after the combined Embers+SolarFlare visual was
        // judged too noisy. Restored Session 2026-04-21 S1 as an opt-in style after re-testing
        // confirmed it reads cleanly on its own. Style selected via PreferencesConfig.LavaRainStyle.
        if (wantSolarFlare && IsEligible(LavaSecondaryFPD))
        {
            for (int i = 0; i < LavaSecondaryBatch; i++)
            {
                var dust = Dust.NewDustDirect(
                    new Vector2(Projectile.Center.X + Main.rand.NextFloat(-HorizSpread, HorizSpread), cloudBottom),
                    2, 2, LavaDustType2Id);
                dust.velocity = new Vector2(
                    Main.rand.NextFloat(-XDrift, XDrift),
                    Main.rand.NextFloat(VelocityMin * 0.7f, VelocityMax * 0.7f));
                dust.scale = Main.rand.NextFloat(LavaScaleMin, LavaScaleMax);
                dust.alpha = DustAlpha;
                dust.color = lavaTint;
                dust.noGravity = false;
                dust.noLight = false;
            }
        }
    }

    private void SpawnHoneyDust(float cloudBottom)
    {
        if (!IsEligible(HoneyFPD)) return;

        for (int i = 0; i < HoneyBatch; i++)
        {
            var dust = Dust.NewDustDirect(
                new Vector2(Projectile.Center.X + Main.rand.NextFloat(-HorizSpread, HorizSpread), cloudBottom),
                2, 2, HoneyDustTypeId);
            dust.velocity = new Vector2(
                Main.rand.NextFloat(-XDrift * 0.5f, XDrift * 0.5f),
                Main.rand.NextFloat(VelocityMin * 0.6f, VelocityMax * 0.6f));
            dust.scale = Main.rand.NextFloat(ScaleMin * 1.2f, ScaleMax * 1.2f);
            dust.alpha = DustAlpha - 10;
            dust.noGravity = false;
        }
    }

    private void SpawnShimmerDust(float cloudBottom)
    {
        Color shimmerTint = ShimmerTint;

        // Primary: deep purple core
        if (IsEligible(Shimmer1FPD))
        {
            for (int i = 0; i < Shimmer1Batch; i++)
            {
                var dust = Dust.NewDustDirect(
                    new Vector2(Projectile.Center.X + Main.rand.NextFloat(-HorizSpread, HorizSpread), cloudBottom),
                    2, 2, ShimmerDust1Id);
                dust.velocity = new Vector2(Main.rand.NextFloat(-XDrift, XDrift), Main.rand.NextFloat(VelocityMin, VelocityMax));
                dust.scale = Main.rand.NextFloat(ScaleMin, ScaleMax);
                dust.alpha = DustAlpha;
                dust.color = shimmerTint;
                dust.noGravity = false;
            }
        }

        // Secondary: bright sparkle overlay
        if (IsEligible(Shimmer2FPD))
        {
            for (int i = 0; i < Shimmer2Batch; i++)
            {
                var dust = Dust.NewDustDirect(
                    new Vector2(Projectile.Center.X + Main.rand.NextFloat(-HorizSpread, HorizSpread), cloudBottom),
                    2, 2, ShimmerDust2Id);
                dust.velocity = new Vector2(
                    Main.rand.NextFloat(-XDrift, XDrift),
                    Main.rand.NextFloat(VelocityMin * 0.8f, VelocityMax * 0.8f));
                dust.scale = Main.rand.NextFloat(ScaleMin * 0.7f, ScaleMax * 0.7f);
                dust.alpha = DustAlpha + 30;
                dust.color = Color.White; // Sparkle stays white
                dust.noGravity = false;
            }
        }

        // Tertiary: accent sparkle (sparsest cadence)
        if (IsEligible(Shimmer3FPD))
        {
            for (int i = 0; i < Shimmer3Batch; i++)
            {
                var dust = Dust.NewDustDirect(
                    new Vector2(Projectile.Center.X + Main.rand.NextFloat(-HorizSpread, HorizSpread), cloudBottom),
                    2, 2, ShimmerDust3Id);
                dust.velocity = new Vector2(
                    Main.rand.NextFloat(-XDrift * 0.5f, XDrift * 0.5f),
                    Main.rand.NextFloat(VelocityMin * 1.2f, VelocityMax * 1.2f));
                dust.scale = Main.rand.NextFloat(ScaleMin * 0.5f, ScaleMax * 0.6f);
                dust.alpha = DustAlpha;
                dust.color = new Color(100, 200, 220);
                dust.noGravity = false;
            }
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // Apply Y offset from localAI[1]
        // Standard drawing handles frame, alpha, and scale automatically
        return true;
    }

    public override Color? GetAlpha(Color lightColor)
    {
        // Override lighting so the cloud is always visible (not darkened underground)
        // Use white with the current alpha
        return new Color(255, 255, 255, 255 - Projectile.alpha) * Projectile.Opacity;
    }

    public override bool ShouldUpdatePosition() => false; // Cloud is stationary
}
