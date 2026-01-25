// AI: purpose=Normalized dynamic intent classification for drummer velocity hinting; genre-agnostic at this layer.
// AI: invariants=Values are stable ordered; maps to velocity targets via StyleConfiguration; same intent→same target per style.
// AI: deps=Consumed by DrummerVelocityShaper; maps from OnsetStrength/FillRole; style maps intent→numeric velocity.
// AI: change=Story 6.1; extend with additional intents as needed; keep enum stable for determinism.

namespace Music.Generator.Agents.Drums.Performance
{
    /// <summary>
    /// Normalized dynamic intent classification for drummer velocity hinting.
    /// Genre-agnostic at the drummer layer; style configuration maps to numeric targets.
    /// Story 6.1: Implement Drummer Velocity Shaper.
    /// </summary>
    public enum DynamicIntent
    {
        /// <summary>
        /// Low intensity, soft dynamics (e.g., ghost notes).
        /// Typical velocity range: 25-50.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Medium-low intensity, supporting dynamics.
        /// Typical velocity range: 50-70.
        /// </summary>
        MediumLow = 1,

        /// <summary>
        /// Medium intensity, standard groove level.
        /// Typical velocity range: 70-90.
        /// </summary>
        Medium = 2,

        /// <summary>
        /// Strong accent, prominent dynamics (e.g., backbeats).
        /// Typical velocity range: 90-110.
        /// </summary>
        StrongAccent = 3,

        /// <summary>
        /// Peak accent, maximum intensity (e.g., crashes, fill climax).
        /// Typical velocity range: 105-127.
        /// </summary>
        PeakAccent = 4,

        /// <summary>
        /// Fill ramp start, beginning of dynamic build.
        /// Velocity determined by ramp position and direction.
        /// </summary>
        FillRampStart = 5,

        /// <summary>
        /// Fill ramp body, middle of dynamic build.
        /// Velocity determined by ramp position and direction.
        /// </summary>
        FillRampBody = 6,

        /// <summary>
        /// Fill ramp end, climax of dynamic build.
        /// Velocity determined by ramp position and direction.
        /// </summary>
        FillRampEnd = 7
    }

    /// <summary>
    /// Direction of velocity ramp for fill patterns.
    /// </summary>
    public enum FillRampDirection
    {
        /// <summary>
        /// Ascending: start soft, end loud (typical for builds).
        /// </summary>
        Ascending = 0,

        /// <summary>
        /// Descending: start loud, end soft (typical for drops/releases).
        /// </summary>
        Descending = 1,

        /// <summary>
        /// Flat: consistent velocity throughout fill.
        /// </summary>
        Flat = 2
    }
}
