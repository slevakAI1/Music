// AI: purpose=Configuration for harmony validation and pitch context generation rules; defines treatment of non-diatonic tones.
// AI: invariants=Immutable after construction; default policy disallows non-diatonic chord tones (current MVP behavior).
// AI: deps=Used by HarmonyValidator and HarmonyPitchContextBuilder; changing defaults impacts generation output.
// AI: change=Add new policy flags for borrowed chords/secondary dominants when implementing those features.

namespace Music.Generator
{
    /// <summary>
    /// Defines rules for harmony validation and pitch context generation.
    /// Controls treatment of non-diatonic tones, borrowed chords, and secondary dominants.
    /// </summary>
    public sealed class HarmonyPolicy
    {
        /// <summary>
        /// Default policy: strict diatonic chord tones only (current MVP behavior).
        /// </summary>
        public static HarmonyPolicy Default { get; } = new HarmonyPolicy
        {
            AllowNonDiatonicChordTones = false,
            AllowSecondaryDominants = false,
            AllowBorrowedChords = false,
            StrictChordToneScaleMembership = true
        };

        /// <summary>
        /// When false (default), all chord tones must be in the scale of the key.
        /// When true, allows chord tones outside the diatonic scale (e.g., chromatic alterations).
        /// </summary>
        public required bool AllowNonDiatonicChordTones { get; init; }

        /// <summary>
        /// When true, allows secondary dominant chords (e.g., V/V, V/ii).
        /// Reserved for future use; not yet implemented.
        /// </summary>
        public required bool AllowSecondaryDominants { get; init; }

        /// <summary>
        /// When true, allows borrowed chords from parallel minor/major.
        /// Reserved for future use; not yet implemented.
        /// </summary>
        public required bool AllowBorrowedChords { get; init; }

        /// <summary>
        /// When true (default), enforces that chord tone pitch classes must be in the scale.
        /// This is the current Stage-1 behavior.
        /// </summary>
        public required bool StrictChordToneScaleMembership { get; init; }
    }
}
