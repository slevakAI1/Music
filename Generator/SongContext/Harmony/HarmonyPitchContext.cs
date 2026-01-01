// AI: purpose=Read-only pitch context for a harmony event; used by generators to choose pitches that respect key/chord.
// AI: invariants=ChordPitchClasses and KeyScalePitchClasses are sorted unique pitch classes (0-11); ChordMidiNotes match those pcs in usable register.
// AI: deps=Built by HarmonyPitchContextBuilder and consumed by PitchRandomizer/ChordVoicing; changing shapes breaks consumers.
// AI: perf=Lightweight DTO; keep init-only immutability to preserve reproducibility and thread-safety assumptions.

namespace Music.Generator
{
    // AI: SourceEvent optional reference for debugging; null in derived contexts or tests.
    public sealed class HarmonyPitchContext
    {
        public Music.Generator.HarmonyEvent? SourceEvent { get; init; }

        // AI: KeyRootPitchClass: tonic of the key, 0=C..11; not the chord root.
        public int KeyRootPitchClass { get; init; }

        // AI: ChordRootPitchClass: pitch class of the chord root derived from degree+key.
        public int ChordRootPitchClass { get; init; }

        // AI: ChordPitchClasses: chord tone pcs 0-11, sorted unique; used for chord-tone constraints.
        public IReadOnlyList<int> ChordPitchClasses { get; init; } = Array.Empty<int>();

        // AI: KeyScalePitchClasses: scale pcs (major or natural minor for MVP), sorted unique; used for scale constraints.
        public IReadOnlyList<int> KeyScalePitchClasses { get; init; } = Array.Empty<int>();

        // AI: ChordMidiNotes: concrete MIDI notes for chord tones in a usable register; order matters for voicing consumers.
        public IReadOnlyList<int> ChordMidiNotes { get; init; } = Array.Empty<int>();

        // AI: BaseOctaveUsed: octave used when constructing ChordMidiNotes; changing this affects voicing ranges.
        public int BaseOctaveUsed { get; init; }

        // AI: Key string from source event; parsed by consumers via PitchClassUtils.ParseKey when needed.
        public string Key { get; init; } = string.Empty;

        // AI: Degree: 1..7 scale degree; consumers expect validated range.
        public int Degree { get; init; }

        // AI: Quality: original chord quality string; callers typically normalize via ChordQuality.Normalize before mapping.
        public string Quality { get; init; } = string.Empty;

        // AI: Bass: inversion hint (e.g., "root","3rd","5th"); affects voicing selection.
        public string Bass { get; init; } = string.Empty;
    }
}