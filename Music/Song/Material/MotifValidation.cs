// AI: purpose=Non-throwing validation for motif-specific constraints (parallel to PartTrackMaterialValidation)
// AI: invariants=Returns empty list if valid; otherwise returns issue descriptions; deterministic
// AI: deps=Validates MotifSpec fields against musical constraints; no external dependencies
using Music.Generator;

namespace Music.Song.Material;

/// <summary>
/// Non-throwing validation for motif-specific constraints.
/// Parallel to PartTrackMaterialValidation from Story M1.
/// </summary>
public static class MotifValidation
{
    private const int MaxBarLengthTicks = 480 * 8; // 8 bars at 480 PPQN
    private const int MinMidiNote = 21; // A0
    private const int MaxMidiNote = 108; // C8
    private const int MaxReasonableRangeSemitones = 24; // 2 octaves

    /// <summary>
    /// Validates a MotifSpec and returns list of issues (empty if valid).
    /// </summary>
    public static IReadOnlyList<string> ValidateMotif(MotifSpec spec)
    {
        var issues = new List<string>();

        // Validate Name
        if (string.IsNullOrWhiteSpace(spec.Name))
            issues.Add("MotifSpec.Name must not be empty");

        // Validate IntendedRole
        if (string.IsNullOrWhiteSpace(spec.IntendedRole))
            issues.Add("MotifSpec.IntendedRole must not be empty");

        // Validate RhythmShape
        if (spec.RhythmShape == null || spec.RhythmShape.Count == 0)
        {
            issues.Add("MotifSpec.RhythmShape must have at least one onset");
        }
        else
        {
            // Check for negative ticks
            var negativeTicks = spec.RhythmShape.Where(t => t < 0).ToList();
            if (negativeTicks.Any())
                issues.Add($"MotifSpec.RhythmShape contains negative ticks: {string.Join(", ", negativeTicks)}");

            // Check for unreasonably large ticks (beyond 8 bars)
            var largeTicks = spec.RhythmShape.Where(t => t > MaxBarLengthTicks).ToList();
            if (largeTicks.Any())
                issues.Add($"MotifSpec.RhythmShape contains ticks beyond reasonable bar length ({MaxBarLengthTicks}): {string.Join(", ", largeTicks)}");
        }

        // Validate RegisterIntent
        if (spec.Register.CenterMidiNote < MinMidiNote || spec.Register.CenterMidiNote > MaxMidiNote)
            issues.Add($"MotifSpec.Register.CenterMidiNote ({spec.Register.CenterMidiNote}) must be in valid MIDI range [{MinMidiNote}..{MaxMidiNote}]");

        if (spec.Register.RangeSemitones <= 0)
            issues.Add($"MotifSpec.Register.RangeSemitones ({spec.Register.RangeSemitones}) must be positive");

        if (spec.Register.RangeSemitones > MaxReasonableRangeSemitones)
            issues.Add($"MotifSpec.Register.RangeSemitones ({spec.Register.RangeSemitones}) exceeds reasonable range (<= {MaxReasonableRangeSemitones})");

        // Validate TonePolicy
        if (spec.TonePolicy.ChordToneBias < 0.0 || spec.TonePolicy.ChordToneBias > 1.0)
            issues.Add($"MotifSpec.TonePolicy.ChordToneBias ({spec.TonePolicy.ChordToneBias}) must be in range [0..1]");

        return issues;
    }
}
