// AI: purpose=Tests for Story 8.4 - MotifValidation helper
// AI: invariants=Tests verify validation catches common errors and is deterministic
// AI: deps=Tests MotifValidation static methods with valid and invalid MotifSpecs
using Music.Generator;

namespace Music.Song.Material.Tests;

/// <summary>
/// Test suite for Story 8.4 - MotifValidation helper.
/// </summary>
public static class MotifValidationTests
{
    /// <summary>
    /// Runs all MotifValidation tests.
    /// </summary>
    public static void RunAll()
    {
        Console.WriteLine("=== Story 8.4: MotifValidation Tests ===\n");

        // Valid motif tests
        TestValidMotif_PassesValidation();
        TestAllTestMotifs_PassValidation();

        // Invalid name/role tests
        TestEmptyName_FailsValidation();
        TestEmptyRole_FailsValidation();

        // Invalid rhythm tests
        TestEmptyRhythmShape_FailsValidation();
        TestNegativeRhythmTicks_FailsValidation();
        TestTooLargeRhythmTicks_FailsValidation();

        // Invalid register tests
        TestCenterMidiTooLow_FailsValidation();
        TestCenterMidiTooHigh_FailsValidation();
        TestNegativeRangeSemitones_FailsValidation();
        TestZeroRangeSemitones_FailsValidation();
        TestTooLargeRangeSemitones_FailsValidation();

        // Invalid tone policy tests
        TestChordToneBiasTooLow_FailsValidation();
        TestChordToneBiasTooHigh_FailsValidation();

        // Determinism test
        TestValidation_IsDeterministic();

        Console.WriteLine("\n✓ All Story 8.4 MotifValidation tests passed!");
    }

    // ============================================================
    // Valid Motif Tests
    // ============================================================

    private static void TestValidMotif_PassesValidation()
    {
        var motif = MotifSpec.Create(
            name: "Valid Motif",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480, 720 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertEqual(issues.Count, 0, "Valid motif should have no validation issues");

        Console.WriteLine("  ✓ Valid motif passes validation");
    }

    private static void TestAllTestMotifs_PassValidation()
    {
        var allMotifs = MotifLibrary.GetAllTestMotifs();

        foreach (var motif in allMotifs)
        {
            var issues = MotifValidation.ValidateMotif(motif);
            AssertEqual(issues.Count, 0, $"Test motif '{motif.Name}' should pass validation");
        }

        Console.WriteLine($"  ✓ All {allMotifs.Count} test motifs from MotifLibrary pass validation");
    }

    // ============================================================
    // Invalid Name/Role Tests
    // ============================================================

    private static void TestEmptyName_FailsValidation()
    {
        var motif = MotifSpec.Create(
            name: "",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Empty name should produce validation issue");
        AssertTrue(issues.Any(i => i.Contains("Name")), "Issue should mention Name");

        Console.WriteLine("  ✓ Empty name fails validation");
    }

    private static void TestEmptyRole_FailsValidation()
    {
        var motif = MotifSpec.Create(
            name: "Valid Name",
            intendedRole: "",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Empty role should produce validation issue");
        AssertTrue(issues.Any(i => i.Contains("IntendedRole")), "Issue should mention IntendedRole");

        Console.WriteLine("  ✓ Empty role fails validation");
    }

    // ============================================================
    // Invalid Rhythm Tests
    // ============================================================

    private static void TestEmptyRhythmShape_FailsValidation()
    {
        var motif = MotifSpec.Create(
            name: "Valid Name",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int>(),
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Empty rhythm shape should produce validation issue");
        AssertTrue(issues.Any(i => i.Contains("RhythmShape")), "Issue should mention RhythmShape");

        Console.WriteLine("  ✓ Empty rhythm shape fails validation");
    }

    private static void TestNegativeRhythmTicks_FailsValidation()
    {
        var motif = MotifSpec.Create(
            name: "Valid Name",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, -100, 480 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Negative rhythm ticks should produce validation issue");
        AssertTrue(issues.Any(i => i.Contains("negative")), "Issue should mention negative ticks");

        Console.WriteLine("  ✓ Negative rhythm ticks fail validation");
    }

    private static void TestTooLargeRhythmTicks_FailsValidation()
    {
        var motif = MotifSpec.Create(
            name: "Valid Name",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 99999 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Too large rhythm ticks should produce validation issue");
        AssertTrue(issues.Any(i => i.Contains("bar length")), "Issue should mention bar length");

        Console.WriteLine("  ✓ Too large rhythm ticks fail validation");
    }

    // ============================================================
    // Invalid Register Tests
    // ============================================================

    private static void TestCenterMidiTooLow_FailsValidation()
    {
        var motif = MotifSpec.Create(
            name: "Valid Name",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480 },
            contour: ContourIntent.Arch,
            centerMidiNote: 10, // Below MIDI range
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Center MIDI too low should produce validation issue");
        AssertTrue(issues.Any(i => i.Contains("CenterMidiNote")), "Issue should mention CenterMidiNote");

        Console.WriteLine("  ✓ Center MIDI too low fails validation");
    }

    private static void TestCenterMidiTooHigh_FailsValidation()
    {
        var motif = MotifSpec.Create(
            name: "Valid Name",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480 },
            contour: ContourIntent.Arch,
            centerMidiNote: 120, // Above MIDI range
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Center MIDI too high should produce validation issue");
        AssertTrue(issues.Any(i => i.Contains("CenterMidiNote")), "Issue should mention CenterMidiNote");

        Console.WriteLine("  ✓ Center MIDI too high fails validation");
    }

    private static void TestNegativeRangeSemitones_FailsValidation()
    {
        var motif = MotifSpec.Create(
            name: "Valid Name",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: -5,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Negative range semitones should produce validation issue");
        AssertTrue(issues.Any(i => i.Contains("RangeSemitones") && i.Contains("positive")), "Issue should mention RangeSemitones must be positive");

        Console.WriteLine("  ✓ Negative range semitones fail validation");
    }

    private static void TestZeroRangeSemitones_FailsValidation()
    {
        var motif = MotifSpec.Create(
            name: "Valid Name",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 0,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Zero range semitones should produce validation issue");
        AssertTrue(issues.Any(i => i.Contains("RangeSemitones") && i.Contains("positive")), "Issue should mention RangeSemitones must be positive");

        Console.WriteLine("  ✓ Zero range semitones fail validation");
    }

    private static void TestTooLargeRangeSemitones_FailsValidation()
    {
        var motif = MotifSpec.Create(
            name: "Valid Name",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 30, // Too large
            chordToneBias: 0.8,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Too large range semitones should produce validation issue");
        AssertTrue(issues.Any(i => i.Contains("RangeSemitones") && i.Contains("reasonable")), "Issue should mention reasonable range");

        Console.WriteLine("  ✓ Too large range semitones fail validation");
    }

    // ============================================================
    // Invalid Tone Policy Tests
    // ============================================================

    private static void TestChordToneBiasTooLow_FailsValidation()
    {
        var motif = MotifSpec.Create(
            name: "Valid Name",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: -0.1,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Chord tone bias too low should produce validation issue");
        AssertTrue(issues.Any(i => i.Contains("ChordToneBias")), "Issue should mention ChordToneBias");

        Console.WriteLine("  ✓ Chord tone bias too low fails validation");
    }

    private static void TestChordToneBiasTooHigh_FailsValidation()
    {
        var motif = MotifSpec.Create(
            name: "Valid Name",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 1.5,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Chord tone bias too high should produce validation issue");
        AssertTrue(issues.Any(i => i.Contains("ChordToneBias")), "Issue should mention ChordToneBias");

        Console.WriteLine("  ✓ Chord tone bias too high fails validation");
    }

    // ============================================================
    // Determinism Test
    // ============================================================

    private static void TestValidation_IsDeterministic()
    {
        var motif = MotifSpec.Create(
            name: "Test Motif",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, -50, 99999 },
            contour: ContourIntent.Arch,
            centerMidiNote: 150,
            rangeSemitones: -10,
            chordToneBias: 2.0,
            allowPassingTones: true);

        var issues1 = MotifValidation.ValidateMotif(motif);
        var issues2 = MotifValidation.ValidateMotif(motif);

        AssertEqual(issues1.Count, issues2.Count, "Validation should be deterministic (same issue count)");

        for (int i = 0; i < issues1.Count; i++)
        {
            AssertEqual(issues1[i], issues2[i], $"Validation issue {i} should be identical");
        }

        Console.WriteLine("  ✓ Validation is deterministic");
    }

    // ============================================================
    // Helper Assertion Methods
    // ============================================================

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Assertion failed: {message}");
    }

    private static void AssertEqual<T>(T actual, T expected, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
            throw new Exception($"Assertion failed: {message}. Expected: {expected}, Actual: {actual}");
    }
}
