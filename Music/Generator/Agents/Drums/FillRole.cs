// AI: purpose=Enum classifying candidate's role within a fill pattern; used by fill operators and memory system.
// AI: invariants=Values are stable and ordered; None is default; each candidate has exactly one FillRole.
// AI: deps=Consumed by DrumCandidate, fill operators (Story 3.3), DrummerMemory for fill shape tracking.
// AI: change=Story 2.2; extend with additional fill role values as fill patterns become more complex.

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Classifies a drum candidate's role within a fill pattern.
    /// Used by fill operators to mark candidate intent and by memory system to track fill shapes.
    /// Story 2.2: Define Drum Candidate Type.
    /// </summary>
    public enum FillRole
    {
        /// <summary>
        /// Standard groove hit, not part of a fill pattern.
        /// Default value for most candidates.
        /// </summary>
        None = 0,

        /// <summary>
        /// Pre-fill accent hit, typically on beat 4 "and" before the fill starts.
        /// Signals the transition into the fill.
        /// </summary>
        Setup = 1,

        /// <summary>
        /// First hit of the fill pattern, often on beat 3 or 3.5.
        /// Marks the beginning of the fill's rhythmic departure.
        /// </summary>
        FillStart = 2,

        /// <summary>
        /// Interior fill notes forming the body of the pattern.
        /// Typically ascending/descending tom patterns or snare rolls.
        /// </summary>
        FillBody = 3,

        /// <summary>
        /// Terminal hit of the fill, often a crash on beat 1 of the next bar.
        /// Marks resolution back to the groove.
        /// </summary>
        FillEnd = 4
    }
}
