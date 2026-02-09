// AI: purpose=Normalized drummer dynamic intents mapped to per-style velocity targets.
// AI: invariants=Enum order must remain stable; style configs map intents->numeric velocities.
// AI: deps=Used by DrummerVelocityShaper and phrase generators; maps from OnsetStrength/FillRole.
// AI: note=FillRamp intents represent ramp positions, not fixed velocities.

using Music.Generator.Groove;

namespace Music.Generator.Drums.Performance
{
    // AI: intent=Normalized dynamic intent set for velocity shaping; add intents carefully to preserve ordering
    public enum DynamicIntent
    {
        Low = 0,
        MediumLow = 1,
        Medium = 2,
        StrongAccent = 3,
        PeakAccent = 4,
        FillRampStart = 5,
        FillRampBody = 6,
        FillRampEnd = 7
    }

    // AI: FillRampDirection=Controls how fill ramp positions map to velocities; keep values stable if persisted
    public enum FillRampDirection
    {
        Ascending = 0,
        Descending = 1,
        Flat = 2
    }
}
