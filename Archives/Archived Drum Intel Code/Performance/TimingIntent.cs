namespace Music.Generator.Drums.Performance
{
    // AI: purpose=Normalized timing intents mapped to per-style tick offsets for timing shaping.
    // AI: invariants=Enum order must remain stable; style configs map intents->tick offsets.
    // AI: deps=Used by DrummerTimingShaper; maps from Role/FillRole; offsets are style-defined.
    public enum TimingIntent
    {
        OnTop = 0,
        SlightlyAhead = 1,
        SlightlyBehind = 2,
        Rushed = 3,
        LaidBack = 4
    }
}
