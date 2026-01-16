// AI: purpose=Static library of reusable bass patterns keyed by groove preset name and section type.
// AI: invariants=Pattern selection is deterministic by (groovePreset, sectionType, barIndex); no external Rng.
// AI: deps=Used by Generator.GenerateBassTrack; patterns must align with GroovePresets BassOnsets structure.
// AI: change=When adding patterns, ensure semitone offsets and slot indices are musically valid for bass range.

namespace Music.Generator
{
    /// <summary>
    /// Provides deterministic bass pattern selection based on groove, section type, and bar position.
    /// Implements Story 5.1: Bass pattern library keyed by groove + section type.
    /// </summary>
    public static class BassPatternLibrary
    {
        // Core reusable bass pattern templates
        private static readonly IReadOnlyList<BassPattern> CorePatterns = new[]
        {
            // Root anchors: sparse root on strong beats
            new BassPattern
            {
                Id = "root_anchor_standard",
                Type = BassPatternType.RootAnchor,
                RelativeSemitones = new[] { 0, 0 },          // Root, root
                SlotIndices = new[] { 0, 2 },                 // Typical strong beats (slot 0 and 2)
                IsPolicyGated = false
            },

            new BassPattern
            {
                Id = "root_anchor_sparse",
                Type = BassPatternType.RootAnchor,
                RelativeSemitones = new[] { 0 },             // Single root hit
                SlotIndices = new[] { 0 },                    // First beat only
                IsPolicyGated = false
            },

            // Root-fifth movement: root then perfect fifth
            new BassPattern
            {
                Id = "root_fifth_basic",
                Type = BassPatternType.RootFifth,
                RelativeSemitones = new[] { 0, 7 },          // Root -> perfect fifth
                SlotIndices = new[] { 0, 2 },                 // Strong beats
                IsPolicyGated = false
            },

            new BassPattern
            {
                Id = "root_fifth_alternating",
                Type = BassPatternType.RootFifth,
                RelativeSemitones = new[] { 0, 7, 0, 7 },    // Root-fifth alternation
                SlotIndices = new[] { 0, 1, 2, 3 },          // All main onset slots
                IsPolicyGated = false
            },

            // Octave pop: root with octave jumps
            new BassPattern
            {
                Id = "octave_pop_basic",
                Type = BassPatternType.OctavePop,
                RelativeSemitones = new[] { 0, 12, 0 },      // Root, octave up, root
                SlotIndices = new[] { 0, 1, 3 },             // Hit pattern
                IsPolicyGated = false
            },

            new BassPattern
            {
                Id = "octave_pop_dense",
                Type = BassPatternType.OctavePop,
                RelativeSemitones = new[] { 0, 12, 0, 12 },  // Dense octave pattern
                SlotIndices = new[] { 0, 1, 2, 3 },          // All slots
                IsPolicyGated = false
            },

            // Diatonic approach: approach note from below (policy-gated)
            new BassPattern
            {
                Id = "diatonic_approach_below",
                Type = BassPatternType.DiatonicApproach,
                RelativeSemitones = new[] { -2, 0 },         // Whole step below, then root
                SlotIndices = new[] { 3, 0 },                // Pickup (slot 3), then downbeat (slot 0 of next logical beat)
                IsPolicyGated = true
            },

            new BassPattern
            {
                Id = "diatonic_approach_above",
                Type = BassPatternType.DiatonicApproach,
                RelativeSemitones = new[] { 2, 0 },          // Whole step above, then root
                SlotIndices = new[] { 3, 0 },                // Pickup pattern
                IsPolicyGated = true
            }
        };

        /// <summary>
        /// Selects a bass pattern deterministically based on groove, section, and bar index.
        /// Pattern selection is deterministic: same inputs always produce same pattern.
        /// </summary>
        /// <param name="groovePreset">Name of the groove preset (e.g., "PopRockBasic").</param>
        /// <param name="sectionType">Section type for arrangement context.</param>
        /// <param name="barIndex">Bar index for variation across bars.</param>
        /// <param name="allowPolicyGated">Whether to include policy-gated patterns (default: false).</param>
        /// <returns>Selected bass pattern.</returns>
        public static BassPattern SelectPattern(
            string groovePreset,
            MusicConstants.eSectionType sectionType,
            int barIndex,
            bool allowPolicyGated = false)
        {
            if (groovePreset is null) groovePreset = string.Empty;

            // Filter candidates based on policy gate
            var candidates = CorePatterns
                .Where(p => allowPolicyGated || !p.IsPolicyGated)
                .ToArray();

            if (candidates.Length == 0)
            {
                // Defensive fallback: return simplest pattern if all filtered out
                return CorePatterns[0];
            }

            // Deterministic selection using hash of (groovePreset, sectionType, barIndex)
            int hash = ComputeDeterministicHash(groovePreset, sectionType, barIndex);
            int index = Math.Abs(hash) % candidates.Length;

            return candidates[index];
        }

        /// <summary>
        /// Computes a deterministic hash from groove, section, and bar index.
        /// Same inputs always produce same hash value.
        /// </summary>
        private static int ComputeDeterministicHash(string groovePreset, MusicConstants.eSectionType sectionType, int barIndex)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + (groovePreset?.GetHashCode() ?? 0);
                h = h * 31 + (int)sectionType;
                h = h * 31 + barIndex;
                return h;
            }
        }

        /// <summary>
        /// Gets all available patterns for inspection (e.g., UI, testing).
        /// </summary>
        /// <param name="includePolicyGated">Whether to include policy-gated patterns.</param>
        /// <returns>Read-only list of available patterns.</returns>
        public static IReadOnlyList<BassPattern> GetAvailablePatterns(bool includePolicyGated = false)
        {
            return CorePatterns
                .Where(p => includePolicyGated || !p.IsPolicyGated)
                .ToArray();
        }
    }
}
