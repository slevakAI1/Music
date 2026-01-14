// AI: purpose=Validation options for HarmonyValidator; controls auto-fix behavior and diatonic policy enforcement.
// AI: invariants=ApplyFixes returns normalized events; StrictDiatonicChordTones uses KeyScalePitchClasses for validation.
// AI: deps=Used by HarmonyValidator; changing defaults affects validation behavior across all call sites.
// AI: thread-safety=Immutable record with init-only properties; safe for concurrent validation calls.

namespace Music.Generator
{
    // AI: config=Controls validation strictness and auto-fix behavior; keep defaults aligned with Stage 1 spec (MVP = strict diatonic).
    public sealed class HarmonyValidationOptions
    {
        // AI: ApplyFixes: when true, normalizes Key/Quality/Bass and clamps invalid values; returns NormalizedEvents in result.
        public bool ApplyFixes { get; init; } = true;

        // AI: StrictDiatonicChordTones: MVP option A from Stage 1 spec; all chord tones must be in KeyScalePitchClasses.
        // AI: behavior=When true, non-diatonic chord tones produce errors (prevents F# minor assert crashes).
        public bool StrictDiatonicChordTones { get; init; } = true;

        // AI: ValidationBaseOctave: used only for probing chord tone count during validation; does not affect generation output.
        public int ValidationBaseOctave { get; init; } = 4;

        // AI: ClampInvalidBassToRoot: when true + ApplyFixes, invalid bass options become "root" with warning instead of error.
        public bool ClampInvalidBassToRoot { get; init; } = true;

        // AI: AllowUnknownQuality: when false (recommended), unknown qualities are errors; when true, they pass validation.
        public bool AllowUnknownQuality { get; init; } = false;

        // AI: Policy: HarmonyPolicy controls non-diatonic tones, borrowed chords, secondary dominants (Stage 2).
        // AI: behavior=When null, uses HarmonyPolicy.Default; validator uses this to determine pitch context rules.
        public HarmonyPolicy? Policy { get; init; }
    }
}
