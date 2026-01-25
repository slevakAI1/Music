// AI: purpose=Normalized timing intent classification for drummer timing hinting; genre-agnostic at this layer.
// AI: invariants=Values are stable ordered; maps to tick offsets via StyleConfiguration; same intent→same offset per style.
// AI: deps=Consumed by DrummerTimingShaper; maps from Role/FillRole; style maps intent→numeric tick offset.
// AI: change=Story 6.2; extend with additional intents as needed; keep enum stable for determinism.

namespace Music.Generator.Agents.Drums.Performance
{
    /// <summary>
    /// Normalized timing intent classification for drummer timing hinting.
    /// Genre-agnostic at the drummer layer; style configuration maps to numeric tick offsets.
    /// Story 6.2: Implement Drummer Timing Nuance.
    /// </summary>
    public enum TimingIntent
    {
        /// <summary>
        /// On-top timing, no offset (0 ticks).
        /// Used for anchors (kick, closed hat) and fill resolutions.
        /// </summary>
        OnTop = 0,

        /// <summary>
        /// Slightly ahead timing, pushing feel (negative ticks).
        /// Creates urgency and forward momentum.
        /// Typical offset: -3 to -5 ticks.
        /// </summary>
        SlightlyAhead = 1,

        /// <summary>
        /// Slightly behind timing, laid-back pocket feel (positive ticks).
        /// Creates groove and pocket depth.
        /// Used for snare backbeats universally across genres.
        /// Typical offset: +3 to +8 ticks.
        /// </summary>
        SlightlyBehind = 2,

        /// <summary>
        /// Rushed timing, aggressive push (more negative ticks).
        /// Used for fill bodies building toward climax.
        /// Typical offset: -6 to -10 ticks.
        /// </summary>
        Rushed = 3,

        /// <summary>
        /// Laid-back timing, deep pocket (more positive ticks).
        /// Creates relaxed, behind-the-beat feel.
        /// Typical offset: +6 to +12 ticks.
        /// </summary>
        LaidBack = 4
    }
}
