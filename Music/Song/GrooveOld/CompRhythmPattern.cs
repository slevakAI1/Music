// AI: purpose=Represents a comp rhythm pattern that selects a subset of groove onsets for comp part.
// AI: invariants=IncludedOnsetIndices contains valid 0-based indices into the groove's CompOnsets list.
// AI: deps=Used by CompRhythmPatternLibrary and Generator; indices must be validated against actual onset count at runtime.
// AI: change=If adding fields, update CompRhythmPatternLibrary pattern definitions and selection logic.

namespace Music.Generator
{
    /// <summary>
    /// Defines which onset slots from a groove preset's CompOnsets should be used for comp part.
    /// </summary>
    public sealed class CompRhythmPattern
    {
        // AI: Name: human-readable label for debugging/logging; not used for pattern selection.
        public required string Name { get; init; }

        // AI: IncludedOnsetIndices: 0-based indices into CompOnsets list; determines which onsets are played.
        // AI: Empty list means no onsets (rest bar); all indices must be < CompOnsets.Count at application time.
        public required IReadOnlyList<int> IncludedOnsetIndices { get; init; }

        // AI: Description: optional human-readable explanation of pattern character (e.g., "anticipate beat 1, skip beat 3").
        public string Description { get; init; } = string.Empty;
    }
}
