namespace WorldShapingWandsMod.Common.Enums
{
    public enum PlaceType : byte
    {
        // ── Null-object entry (G-41 Session 4 2026-05-02) ──────────────────────────────
        // (Full supersession: Wand-of-Slopes and Wand-of-Actuators are cancelled in favour
        // of integrating these features into existing wands. PlaceType.None serves as a
        // canonical null/undefined placeholder for future extensibility or fallback scenarios.)
        /// <summary>Null/undefined entry — used for future expansion or fallback cases.</summary>
        None = 0,

        Platform = 1,
        Solid = 2,      // renamed from "Soild" to correct spelling
        Rope = 3,
        Rail = 4,
        GrassSeed = 5,
        PlantPot = 6,
        Wall = 7,
        Torch = 8
    }
}