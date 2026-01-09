// AI: purpose=Model for bass pattern templates defining semitone offsets and slot indices for rendering bass lines.
// AI: invariants=SlotIndices reference onset positions in groove; semitone offsets are relative to chord root.
// AI: deps=Used by BassPatternLibrary and bass line rendering; changing structure affects pattern serialization.

namespace Music.Generator
{
    /// <summary>
    /// Bass pattern type classification for organizational purposes.
    /// </summary>
    public enum BassPatternType
    {
        RootAnchor,        // Sparse root notes on strong beats
        RootFifth,         // Root and perfect fifth movement
        OctavePop,         // Octave jumps from root
        DiatonicApproach   // Diatonic approach notes (policy-gated)
    }

    /// <summary>
    /// Represents a single bass hit with MIDI note and slot index.
    /// </summary>
    /// <param name="MidiNote">MIDI note number to play.</param>
    /// <param name="SlotIndex">Onset slot index within the bar (0-based).</param>
    public record BassHit(int MidiNote, int SlotIndex);

    /// <summary>
    /// Declarative bass pattern template defining semitone offsets and rhythm.
    /// </summary>
    public sealed class BassPattern
    {
        /// <summary>
        /// Unique identifier for this pattern.
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// Pattern type classification.
        /// </summary>
        public BassPatternType Type { get; init; }

        /// <summary>
        /// Semitone offsets from the chord root for each hit.
        /// </summary>
        public IReadOnlyList<int> RelativeSemitones { get; init; } = Array.Empty<int>();

        /// <summary>
        /// Onset slot indices (0-based) within a bar's onset grid.
        /// </summary>
        public IReadOnlyList<int> SlotIndices { get; init; } = Array.Empty<int>();

        /// <summary>
        /// Whether this pattern requires explicit policy opt-in (e.g., chromatic or advanced approaches).
        /// </summary>
        public bool IsPolicyGated { get; init; } = false;

        /// <summary>
        /// Renders the pattern into concrete bass hits given the root MIDI note and onset count.
        /// </summary>
        /// <param name="rootMidi">Root MIDI note of the current chord.</param>
        /// <param name="onsetCount">Number of onset slots in the bar's grid.</param>
        /// <returns>List of bass hits with concrete MIDI notes and slot indices.</returns>
        public IReadOnlyList<BassHit> Render(int rootMidi, int onsetCount)
        {
            if (onsetCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(onsetCount), "Onset count must be positive.");

            var hits = new List<BassHit>(Math.Min(RelativeSemitones.Count, SlotIndices.Count));

            int hitCount = Math.Min(RelativeSemitones.Count, SlotIndices.Count);
            for (int i = 0; i < hitCount; i++)
            {
                int slot = SlotIndices[i] % onsetCount; // Wrap slot index to onset grid size
                int midi = rootMidi + RelativeSemitones[i];
                hits.Add(new BassHit(midi, slot));
            }

            return hits;
        }
    }
}
