// AI: purpose=Tests for Story 8.3 - Hardcoded test motifs in MotifLibrary
// AI: invariants=All tests verify determinism, valid structure, MaterialBank storage, and motif characteristics
// AI: deps=Tests MotifLibrary static methods, MaterialBank storage, MotifConversion helpers
using Music.Generator;

namespace Music.Song.Material.Tests;

/// <summary>
/// Test suite for Story 8.3 - Hardcoded test motifs in MotifLibrary.
/// Verifies motif validity, determinism, and MaterialBank integration.
/// </summary>
public static class MotifLibraryTests
{
    /// <summary>
    /// Runs all MotifLibrary tests.
    /// </summary>
    public static void RunAll()
    {
        Console.WriteLine("=== Story 8.3: MotifLibrary Tests ===\n");

        // Determinism tests
        TestClassicRockHookA_Determinism();
        TestSteadyVerseRiffA_Determinism();
        TestBrightSynthHookA_Determinism();
        TestBassTransitionFillA_Determinism();

        // Structure validation tests
        TestClassicRockHookA_ValidStructure();
        TestSteadyVerseRiffA_ValidStructure();
        TestBrightSynthHookA_ValidStructure();
        TestBassTransitionFillA_ValidStructure();

        // MaterialBank storage tests
        TestClassicRockHookA_CanBeStoredInMaterialBank();
        TestSteadyVerseRiffA_CanBeStoredInMaterialBank();
        TestBrightSynthHookA_CanBeStoredInMaterialBank();
        TestBassTransitionFillA_CanBeStoredInMaterialBank();

        // Motif characteristics tests
        TestClassicRockHookA_HasCorrectCharacteristics();
        TestSteadyVerseRiffA_HasCorrectCharacteristics();
        TestBrightSynthHookA_HasCorrectCharacteristics();
        TestBassTransitionFillA_HasCorrectCharacteristics();

        // Collection tests
        TestGetAllTestMotifs_ReturnsAllMotifs();
        TestGetAllTestMotifs_AllMotifsValid();

        Console.WriteLine("\n✓ All Story 8.3 MotifLibrary tests passed!");
    }

    // ============================================================
    // Determinism Tests
    // ============================================================

    private static void TestClassicRockHookA_Determinism()
    {
        var motif1 = MotifLibrary.ClassicRockHookA();
        var motif2 = MotifLibrary.ClassicRockHookA();

        // Note: MotifId will be different each time, but all other fields should match
        AssertEqual(motif1.Name, motif2.Name, "ClassicRockHookA: Name should be deterministic");
        AssertEqual(motif1.IntendedRole, motif2.IntendedRole, "ClassicRockHookA: IntendedRole should be deterministic");
        AssertEqual(motif1.Kind, motif2.Kind, "ClassicRockHookA: Kind should be deterministic");
        AssertEqual(motif1.RhythmShape.Count, motif2.RhythmShape.Count, "ClassicRockHookA: Rhythm shape count should be deterministic");
        AssertEqual(motif1.Contour, motif2.Contour, "ClassicRockHookA: Contour should be deterministic");

        Console.WriteLine("  ✓ ClassicRockHookA is deterministic");
    }

    private static void TestSteadyVerseRiffA_Determinism()
    {
        var motif1 = MotifLibrary.SteadyVerseRiffA();
        var motif2 = MotifLibrary.SteadyVerseRiffA();

        AssertEqual(motif1.Name, motif2.Name, "SteadyVerseRiffA: Name should be deterministic");
        AssertEqual(motif1.RhythmShape.Count, motif2.RhythmShape.Count, "SteadyVerseRiffA: Rhythm shape count should be deterministic");

        Console.WriteLine("  ✓ SteadyVerseRiffA is deterministic");
    }

    private static void TestBrightSynthHookA_Determinism()
    {
        var motif1 = MotifLibrary.BrightSynthHookA();
        var motif2 = MotifLibrary.BrightSynthHookA();

        AssertEqual(motif1.Name, motif2.Name, "BrightSynthHookA: Name should be deterministic");
        AssertEqual(motif1.RhythmShape.Count, motif2.RhythmShape.Count, "BrightSynthHookA: Rhythm shape count should be deterministic");

        Console.WriteLine("  ✓ BrightSynthHookA is deterministic");
    }

    private static void TestBassTransitionFillA_Determinism()
    {
        var motif1 = MotifLibrary.BassTransitionFillA();
        var motif2 = MotifLibrary.BassTransitionFillA();

        AssertEqual(motif1.Name, motif2.Name, "BassTransitionFillA: Name should be deterministic");
        AssertEqual(motif1.RhythmShape.Count, motif2.RhythmShape.Count, "BassTransitionFillA: Rhythm shape count should be deterministic");

        Console.WriteLine("  ✓ BassTransitionFillA is deterministic");
    }

    // ============================================================
    // Structure Validation Tests
    // ============================================================

    private static void TestClassicRockHookA_ValidStructure()
    {
        var motif = MotifLibrary.ClassicRockHookA();

        AssertNotNull(motif, "ClassicRockHookA should not be null");
        AssertNotEmpty(motif.Name, "ClassicRockHookA: Name should not be empty");
        AssertNotEmpty(motif.IntendedRole, "ClassicRockHookA: IntendedRole should not be empty");
        AssertTrue(motif.RhythmShape.Count > 0, "ClassicRockHookA: RhythmShape should have entries");
        AssertTrue(motif.RhythmShape.All(t => t >= 0), "ClassicRockHookA: All rhythm ticks should be >= 0");
        AssertInRange(motif.Register.CenterMidiNote, 21, 108, "ClassicRockHookA: CenterMidiNote in valid MIDI range");
        AssertInRange(motif.Register.RangeSemitones, 1, 24, "ClassicRockHookA: RangeSemitones in valid range");
        AssertInRange(motif.TonePolicy.ChordToneBias, 0.0, 1.0, "ClassicRockHookA: ChordToneBias in [0..1]");
        AssertTrue(motif.Tags.Count > 0, "ClassicRockHookA: Should have at least one tag");

        Console.WriteLine("  ✓ ClassicRockHookA has valid structure");
    }

    private static void TestSteadyVerseRiffA_ValidStructure()
    {
        var motif = MotifLibrary.SteadyVerseRiffA();

        AssertNotNull(motif, "SteadyVerseRiffA should not be null");
        AssertNotEmpty(motif.Name, "SteadyVerseRiffA: Name should not be empty");
        AssertTrue(motif.RhythmShape.Count > 0, "SteadyVerseRiffA: RhythmShape should have entries");
        AssertTrue(motif.RhythmShape.All(t => t >= 0), "SteadyVerseRiffA: All rhythm ticks should be >= 0");
        AssertInRange(motif.Register.CenterMidiNote, 21, 108, "SteadyVerseRiffA: CenterMidiNote in valid MIDI range");
        AssertInRange(motif.Register.RangeSemitones, 1, 24, "SteadyVerseRiffA: RangeSemitones in valid range");

        Console.WriteLine("  ✓ SteadyVerseRiffA has valid structure");
    }

    private static void TestBrightSynthHookA_ValidStructure()
    {
        var motif = MotifLibrary.BrightSynthHookA();

        AssertNotNull(motif, "BrightSynthHookA should not be null");
        AssertNotEmpty(motif.Name, "BrightSynthHookA: Name should not be empty");
        AssertTrue(motif.RhythmShape.Count > 0, "BrightSynthHookA: RhythmShape should have entries");
        AssertTrue(motif.RhythmShape.All(t => t >= 0), "BrightSynthHookA: All rhythm ticks should be >= 0");
        AssertInRange(motif.Register.CenterMidiNote, 21, 108, "BrightSynthHookA: CenterMidiNote in valid MIDI range");

        Console.WriteLine("  ✓ BrightSynthHookA has valid structure");
    }

    private static void TestBassTransitionFillA_ValidStructure()
    {
        var motif = MotifLibrary.BassTransitionFillA();

        AssertNotNull(motif, "BassTransitionFillA should not be null");
        AssertNotEmpty(motif.Name, "BassTransitionFillA: Name should not be empty");
        AssertTrue(motif.RhythmShape.Count > 0, "BassTransitionFillA: RhythmShape should have entries");
        AssertTrue(motif.RhythmShape.All(t => t >= 0), "BassTransitionFillA: All rhythm ticks should be >= 0");

        Console.WriteLine("  ✓ BassTransitionFillA has valid structure");
    }

    // ============================================================
    // MaterialBank Storage Tests
    // ============================================================

    private static void TestClassicRockHookA_CanBeStoredInMaterialBank()
    {
        var motif = MotifLibrary.ClassicRockHookA();
        var track = motif.ToPartTrack();
        var bank = new MaterialBank();

        bank.Add(track);

        AssertTrue(bank.Contains(motif.MotifId), "ClassicRockHookA: Should be stored in MaterialBank");
        AssertTrue(bank.TryGet(motif.MotifId, out var retrieved), "ClassicRockHookA: Should be retrievable from MaterialBank");
        AssertEqual(retrieved!.Meta.Name, motif.Name, "ClassicRockHookA: Retrieved motif should have correct name");

        Console.WriteLine("  ✓ ClassicRockHookA can be stored in MaterialBank");
    }

    private static void TestSteadyVerseRiffA_CanBeStoredInMaterialBank()
    {
        var motif = MotifLibrary.SteadyVerseRiffA();
        var track = motif.ToPartTrack();
        var bank = new MaterialBank();

        bank.Add(track);

        AssertTrue(bank.Contains(motif.MotifId), "SteadyVerseRiffA: Should be stored in MaterialBank");

        Console.WriteLine("  ✓ SteadyVerseRiffA can be stored in MaterialBank");
    }

    private static void TestBrightSynthHookA_CanBeStoredInMaterialBank()
    {
        var motif = MotifLibrary.BrightSynthHookA();
        var track = motif.ToPartTrack();
        var bank = new MaterialBank();

        bank.Add(track);

        AssertTrue(bank.Contains(motif.MotifId), "BrightSynthHookA: Should be stored in MaterialBank");

        Console.WriteLine("  ✓ BrightSynthHookA can be stored in MaterialBank");
    }

    private static void TestBassTransitionFillA_CanBeStoredInMaterialBank()
    {
        var motif = MotifLibrary.BassTransitionFillA();
        var track = motif.ToPartTrack();
        var bank = new MaterialBank();

        bank.Add(track);

        AssertTrue(bank.Contains(motif.MotifId), "BassTransitionFillA: Should be stored in MaterialBank");

        Console.WriteLine("  ✓ BassTransitionFillA can be stored in MaterialBank");
    }

    // ============================================================
    // Motif Characteristics Tests
    // ============================================================

    private static void TestClassicRockHookA_HasCorrectCharacteristics()
    {
        var motif = MotifLibrary.ClassicRockHookA();

        AssertEqual(motif.Name, "Classic Rock Hook A", "ClassicRockHookA: Name should match");
        AssertEqual(motif.IntendedRole, "Lead", "ClassicRockHookA: IntendedRole should be Lead");
        AssertEqual(motif.Kind, MaterialKind.Hook, "ClassicRockHookA: Kind should be Hook");
        AssertEqual(motif.Contour, ContourIntent.Arch, "ClassicRockHookA: Contour should be Arch");
        AssertTrue(motif.Tags.Contains("chorus-hook"), "ClassicRockHookA: Should have 'chorus-hook' tag");

        Console.WriteLine("  ✓ ClassicRockHookA has correct characteristics");
    }

    private static void TestSteadyVerseRiffA_HasCorrectCharacteristics()
    {
        var motif = MotifLibrary.SteadyVerseRiffA();

        AssertEqual(motif.Name, "Steady Verse Riff A", "SteadyVerseRiffA: Name should match");
        AssertEqual(motif.IntendedRole, "Guitar", "SteadyVerseRiffA: IntendedRole should be Guitar");
        AssertEqual(motif.Kind, MaterialKind.Riff, "SteadyVerseRiffA: Kind should be Riff");
        AssertEqual(motif.Contour, ContourIntent.Flat, "SteadyVerseRiffA: Contour should be Flat");
        AssertTrue(motif.Tags.Contains("verse-riff"), "SteadyVerseRiffA: Should have 'verse-riff' tag");

        Console.WriteLine("  ✓ SteadyVerseRiffA has correct characteristics");
    }

    private static void TestBrightSynthHookA_HasCorrectCharacteristics()
    {
        var motif = MotifLibrary.BrightSynthHookA();

        AssertEqual(motif.Name, "Bright Synth Hook A", "BrightSynthHookA: Name should match");
        AssertEqual(motif.IntendedRole, "Keys", "BrightSynthHookA: IntendedRole should be Keys");
        AssertEqual(motif.Kind, MaterialKind.Hook, "BrightSynthHookA: Kind should be Hook");
        AssertEqual(motif.Contour, ContourIntent.Up, "BrightSynthHookA: Contour should be Up");
        AssertTrue(motif.Tags.Contains("synth-hook"), "BrightSynthHookA: Should have 'synth-hook' tag");

        Console.WriteLine("  ✓ BrightSynthHookA has correct characteristics");
    }

    private static void TestBassTransitionFillA_HasCorrectCharacteristics()
    {
        var motif = MotifLibrary.BassTransitionFillA();

        AssertEqual(motif.Name, "Bass Transition Fill A", "BassTransitionFillA: Name should match");
        AssertEqual(motif.IntendedRole, "Bass", "BassTransitionFillA: IntendedRole should be Bass");
        AssertEqual(motif.Kind, MaterialKind.BassFill, "BassTransitionFillA: Kind should be BassFill");
        AssertEqual(motif.Contour, ContourIntent.Up, "BassTransitionFillA: Contour should be Up");

        Console.WriteLine("  ✓ BassTransitionFillA has correct characteristics");
    }

    // ============================================================
    // Collection Tests
    // ============================================================

    private static void TestGetAllTestMotifs_ReturnsAllMotifs()
    {
        var allMotifs = MotifLibrary.GetAllTestMotifs();

        AssertNotNull(allMotifs, "GetAllTestMotifs: Should not return null");
        AssertTrue(allMotifs.Count >= 4, "GetAllTestMotifs: Should return at least 4 motifs");

        // Verify specific motifs are present
        var hasHook = allMotifs.Any(m => m.Name == "Classic Rock Hook A");
        var hasRiff = allMotifs.Any(m => m.Name == "Steady Verse Riff A");
        var hasSynthHook = allMotifs.Any(m => m.Name == "Bright Synth Hook A");
        var hasBassFill = allMotifs.Any(m => m.Name == "Bass Transition Fill A");

        AssertTrue(hasHook, "GetAllTestMotifs: Should include Classic Rock Hook A");
        AssertTrue(hasRiff, "GetAllTestMotifs: Should include Steady Verse Riff A");
        AssertTrue(hasSynthHook, "GetAllTestMotifs: Should include Bright Synth Hook A");
        AssertTrue(hasBassFill, "GetAllTestMotifs: Should include Bass Transition Fill A");

        Console.WriteLine("  ✓ GetAllTestMotifs returns all motifs");
    }

    private static void TestGetAllTestMotifs_AllMotifsValid()
    {
        var allMotifs = MotifLibrary.GetAllTestMotifs();

        foreach (var motif in allMotifs)
        {
            AssertNotNull(motif, "GetAllTestMotifs: All motifs should be non-null");
            AssertNotEmpty(motif.Name, $"GetAllTestMotifs: Motif '{motif.Name}' should have name");
            AssertNotEmpty(motif.IntendedRole, $"GetAllTestMotifs: Motif '{motif.Name}' should have role");
            AssertTrue(motif.RhythmShape.Count > 0, $"GetAllTestMotifs: Motif '{motif.Name}' should have rhythm");
            AssertInRange(motif.Register.CenterMidiNote, 21, 108, $"GetAllTestMotifs: Motif '{motif.Name}' should have valid center MIDI");
            AssertInRange(motif.Register.RangeSemitones, 1, 24, $"GetAllTestMotifs: Motif '{motif.Name}' should have valid range");
            AssertInRange(motif.TonePolicy.ChordToneBias, 0.0, 1.0, $"GetAllTestMotifs: Motif '{motif.Name}' should have valid chord tone bias");
        }

        Console.WriteLine("  ✓ All motifs from GetAllTestMotifs are valid");
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

    private static void AssertNotNull(object? obj, string message)
    {
        if (obj is null)
            throw new Exception($"Assertion failed: {message}");
    }

    private static void AssertNotEmpty(string? str, string message)
    {
        if (string.IsNullOrEmpty(str))
            throw new Exception($"Assertion failed: {message}");
    }

    private static void AssertInRange(int value, int min, int max, string message)
    {
        if (value < min || value > max)
            throw new Exception($"Assertion failed: {message}. Value {value} not in range [{min}, {max}]");
    }

    private static void AssertInRange(double value, double min, double max, string message)
    {
        if (value < min || value > max)
            throw new Exception($"Assertion failed: {message}. Value {value} not in range [{min}, {max}]");
    }
}
