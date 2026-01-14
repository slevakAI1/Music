// AI: purpose=Comprehensive test suite for Story 8.5 - Complete motif data layer validation before Stage 9
// AI: invariants=Tests verify entire motif system: creation, immutability, conversion, storage, validation, determinism
// AI: deps=Consolidates tests from Stories 8.1-8.4; ensures data layer solid for Stage 9 placement work
using Music.Generator;
using Music.MyMidi;

namespace Music.Song.Material.Tests;

/// <summary>
/// Comprehensive test suite for Story 8.5 - Motif definition tests and MaterialBank integration.
/// Validates the entire motif data layer is solid before Stage 9 placement work.
/// </summary>
public static class MotifDefinitionsTests
{
    /// <summary>
    /// Runs all comprehensive motif data layer tests.
    /// </summary>
    public static void RunAll()
    {
        Console.WriteLine("=== Story 8.5: Comprehensive Motif Data Layer Tests ===\n");

        // MotifSpec creation and immutability (Story 8.1)
        TestMotifSpecCreation();
        TestMotifSpecImmutability();
        TestMotifSpecFactoryMethodClamping();

        // MotifSpec ↔ PartTrack round-trip (Story 8.2)
        TestMotifSpecToPartTrackConversion();
        TestPartTrackToMotifSpecConversion();
        TestMotifSpecRoundTripPreservesData();
        TestMotifConversionHandlesInvalidDomain();

        // MaterialBank storage and retrieval (Story 8.2)
        TestMaterialBankStoresMotif();
        TestMaterialBankRetrievesMotif();
        TestGetMotifsByRoleFiltersCorrectly();
        TestGetMotifsByKindFiltersCorrectly();
        TestGetMotifByNameFindsCorrectMotif();

        // Hardcoded test motifs validation (Story 8.3)
        TestAllHardcodedMotifsAreValid();
        TestHardcodedMotifsDeterministic();
        TestHardcodedMotifsHaveUniqueIds();
        TestHardcodedMotifsCanBeStored();

        // Validation catches common errors (Story 8.4)
        TestValidationCatchesEmptyName();
        TestValidationCatchesEmptyRole();
        TestValidationCatchesInvalidRhythm();
        TestValidationCatchesInvalidRegister();
        TestValidationCatchesInvalidTonePolicy();

        // Overall determinism verification
        TestEntireMotifSystemIsDeterministic();

        Console.WriteLine("\n✓✓✓ All Story 8.5 comprehensive tests passed! ✓✓✓");
        Console.WriteLine("Motif data layer is solid and ready for Stage 9.");
    }

    // ============================================================
    // MotifSpec Creation and Immutability Tests
    // ============================================================

    private static void TestMotifSpecCreation()
    {
        var motif = MotifSpec.Create(
            name: "Test Motif",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true,
            tags: new HashSet<string> { "test" });

        AssertNotNull(motif, "MotifSpec.Create should return non-null");
        AssertEqual(motif.Name, "Test Motif", "Name should be set");
        AssertEqual(motif.IntendedRole, "Lead", "IntendedRole should be set");
        AssertEqual(motif.Kind, MaterialKind.Hook, "Kind should be set");
        AssertEqual(motif.RhythmShape.Count, 3, "RhythmShape should have 3 onsets");
        AssertEqual(motif.Contour, ContourIntent.Arch, "Contour should be set");
        AssertNotNull(motif.MotifId, "MotifId should be generated");
        AssertTrue(!string.IsNullOrEmpty(motif.MotifId.Value), "MotifId should have value");

        Console.WriteLine("  ✓ MotifSpec creation works correctly");
    }

    private static void TestMotifSpecImmutability()
    {
        var motif = MotifSpec.Create(
            name: "Immutable Test",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240 },
            contour: ContourIntent.Flat,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.7,
            allowPassingTones: true);

        // Records are immutable - we can only create new instances with 'with' syntax
        var modified = motif with { Name = "Modified" };

        AssertNotEqual(motif.Name, modified.Name, "Original should not be modified");
        AssertEqual(motif.Name, "Immutable Test", "Original name should remain unchanged");
        AssertEqual(modified.Name, "Modified", "Modified copy should have new name");

        Console.WriteLine("  ✓ MotifSpec is properly immutable");
    }

    private static void TestMotifSpecFactoryMethodClamping()
    {
        // Test that Create() clamps values to safe ranges
        var motif = MotifSpec.Create(
            name: "Clamping Test",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, -100, 240, -50 }, // Negative ticks
            contour: ContourIntent.Up,
            centerMidiNote: 150, // Above MIDI range
            rangeSemitones: 30, // Too large
            chordToneBias: 1.5, // Above 1.0
            allowPassingTones: true);

        // Rhythm shape should have negatives clamped to 0
        AssertTrue(motif.RhythmShape.All(t => t >= 0), "Negative ticks should be clamped to 0");

        // Center MIDI should be clamped to valid range
        AssertInRange(motif.Register.CenterMidiNote, 21, 108, "Center MIDI should be clamped to valid range");

        // Range semitones should be clamped
        AssertInRange(motif.Register.RangeSemitones, 1, 24, "Range semitones should be clamped");

        // Chord tone bias should be clamped to [0..1]
        AssertInRange(motif.TonePolicy.ChordToneBias, 0.0, 1.0, "Chord tone bias should be clamped");

        Console.WriteLine("  ✓ MotifSpec.Create clamps invalid values");
    }

    // ============================================================
    // MotifSpec ↔ PartTrack Conversion Tests
    // ============================================================

    private static void TestMotifSpecToPartTrackConversion()
    {
        var motif = MotifSpec.Create(
            name: "Conversion Test",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480, 720 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true,
            tags: new HashSet<string> { "test", "hook" });

        var track = motif.ToPartTrack();

        AssertNotNull(track, "ToPartTrack should return non-null");
        AssertEqual(track.Meta.Domain, PartTrackDomain.MaterialLocal, "Domain should be MaterialLocal");
        AssertEqual(track.Meta.Kind, PartTrackKind.MaterialFragment, "Kind should be MaterialFragment");
        AssertEqual(track.Meta.MaterialKind, MaterialKind.Hook, "MaterialKind should match");
        AssertEqual(track.Meta.Name, "Conversion Test", "Name should match");
        AssertEqual(track.Meta.IntendedRole, "Lead", "IntendedRole should match");
        AssertEqual(track.PartTrackNoteEvents.Count, 4, "Should have 4 note events");
        AssertTrue(track.Meta.Tags.SetEquals(motif.Tags), "Tags should be preserved");

        Console.WriteLine("  ✓ MotifSpec → PartTrack conversion works");
    }

    private static void TestPartTrackToMotifSpecConversion()
    {
        var originalMotif = MotifSpec.Create(
            name: "Original",
            intendedRole: "Bass",
            kind: MaterialKind.Riff,
            rhythmShape: new List<int> { 0, 120, 240 },
            contour: ContourIntent.Down,
            centerMidiNote: 48,
            rangeSemitones: 10,
            chordToneBias: 0.9,
            allowPassingTones: false,
            tags: new HashSet<string> { "bass-riff" });

        var track = originalMotif.ToPartTrack();
        var reconstructed = MotifConversion.FromPartTrack(track);

        AssertNotNull(reconstructed, "FromPartTrack should return non-null for valid track");
        AssertEqual(reconstructed!.Name, originalMotif.Name, "Name should be preserved");
        AssertEqual(reconstructed.IntendedRole, originalMotif.IntendedRole, "IntendedRole should be preserved");
        AssertEqual(reconstructed.Kind, originalMotif.Kind, "Kind should be preserved");
        AssertEqual(reconstructed.RhythmShape.Count, originalMotif.RhythmShape.Count, "RhythmShape count should match");

        Console.WriteLine("  ✓ PartTrack → MotifSpec conversion works");
    }

    private static void TestMotifSpecRoundTripPreservesData()
    {
        var original = MotifSpec.Create(
            name: "Round Trip",
            intendedRole: "Keys",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480, 960 },
            contour: ContourIntent.ZigZag,
            centerMidiNote: 72,
            rangeSemitones: 15,
            chordToneBias: 0.75,
            allowPassingTones: true,
            tags: new HashSet<string> { "keys", "bright" });

        var track = original.ToPartTrack();
        var roundTripped = MotifConversion.FromPartTrack(track);

        AssertNotNull(roundTripped, "Round-trip should succeed");
        AssertEqual(roundTripped!.Name, original.Name, "Round-trip: Name preserved");
        AssertEqual(roundTripped.IntendedRole, original.IntendedRole, "Round-trip: Role preserved");
        AssertEqual(roundTripped.Kind, original.Kind, "Round-trip: Kind preserved");
        AssertEqual(roundTripped.RhythmShape.Count, original.RhythmShape.Count, "Round-trip: Rhythm count preserved");

        for (int i = 0; i < original.RhythmShape.Count; i++)
        {
            AssertEqual(roundTripped.RhythmShape[i], original.RhythmShape[i], $"Round-trip: Rhythm tick {i} preserved");
        }

        Console.WriteLine("  ✓ MotifSpec → PartTrack → MotifSpec round-trip preserves data");
    }

    private static void TestMotifConversionHandlesInvalidDomain()
    {
        var track = new PartTrack([])
        {
            Meta = new PartTrackMeta
            {
                Domain = PartTrackDomain.SongAbsolute, // Wrong domain
                Kind = PartTrackKind.MaterialFragment,
                MaterialKind = MaterialKind.Hook
            }
        };

        var result = MotifConversion.FromPartTrack(track);

        AssertNull(result, "FromPartTrack should return null for invalid domain");

        Console.WriteLine("  ✓ Conversion rejects invalid domain");
    }

    // ============================================================
    // MaterialBank Storage and Retrieval Tests
    // ============================================================

    private static void TestMaterialBankStoresMotif()
    {
        var bank = new MaterialBank();
        var motif = MotifLibrary.ClassicRockHookA();
        var track = motif.ToPartTrack();

        bank.Add(track);

        AssertEqual(bank.Count, 1, "Bank should contain 1 item");
        AssertTrue(bank.Contains(motif.MotifId), "Bank should contain the motif");

        Console.WriteLine("  ✓ MaterialBank stores motifs");
    }

    private static void TestMaterialBankRetrievesMotif()
    {
        var bank = new MaterialBank();
        var motif = MotifLibrary.SteadyVerseRiffA();
        var track = motif.ToPartTrack();

        bank.Add(track);

        var found = bank.TryGet(motif.MotifId, out var retrieved);

        AssertTrue(found, "TryGet should return true");
        AssertNotNull(retrieved, "Retrieved track should not be null");
        AssertEqual(retrieved!.Meta.Name, motif.Name, "Retrieved motif should have correct name");

        Console.WriteLine("  ✓ MaterialBank retrieves motifs");
    }

    private static void TestGetMotifsByRoleFiltersCorrectly()
    {
        var bank = new MaterialBank();
        var leadHook = MotifLibrary.ClassicRockHookA();
        var guitarRiff = MotifLibrary.SteadyVerseRiffA();
        var keysHook = MotifLibrary.BrightSynthHookA();

        bank.Add(leadHook.ToPartTrack());
        bank.Add(guitarRiff.ToPartTrack());
        bank.Add(keysHook.ToPartTrack());

        var leadMotifs = bank.GetMotifsByRole("Lead");
        var guitarMotifs = bank.GetMotifsByRole("Guitar");
        var keysMotifs = bank.GetMotifsByRole("Keys");

        AssertEqual(leadMotifs.Count, 1, "Should find 1 Lead motif");
        AssertEqual(guitarMotifs.Count, 1, "Should find 1 Guitar motif");
        AssertEqual(keysMotifs.Count, 1, "Should find 1 Keys motif");

        Console.WriteLine("  ✓ GetMotifsByRole filters correctly");
    }

    private static void TestGetMotifsByKindFiltersCorrectly()
    {
        var bank = new MaterialBank();
        var hook1 = MotifLibrary.ClassicRockHookA();
        var riff = MotifLibrary.SteadyVerseRiffA();
        var hook2 = MotifLibrary.BrightSynthHookA();
        var fill = MotifLibrary.BassTransitionFillA();

        bank.Add(hook1.ToPartTrack());
        bank.Add(riff.ToPartTrack());
        bank.Add(hook2.ToPartTrack());
        bank.Add(fill.ToPartTrack());

        var hooks = bank.GetMotifsByKind(MaterialKind.Hook);
        var riffs = bank.GetMotifsByKind(MaterialKind.Riff);
        var fills = bank.GetMotifsByKind(MaterialKind.BassFill);

        AssertEqual(hooks.Count, 2, "Should find 2 Hook motifs");
        AssertEqual(riffs.Count, 1, "Should find 1 Riff motif");
        AssertEqual(fills.Count, 1, "Should find 1 BassFill motif");

        Console.WriteLine("  ✓ GetMotifsByKind filters correctly");
    }

    private static void TestGetMotifByNameFindsCorrectMotif()
    {
        var bank = new MaterialBank();
        var motif1 = MotifLibrary.ClassicRockHookA();
        var motif2 = MotifLibrary.SteadyVerseRiffA();

        bank.Add(motif1.ToPartTrack());
        bank.Add(motif2.ToPartTrack());

        var found = bank.GetMotifByName("Classic Rock Hook A");

        AssertNotNull(found, "GetMotifByName should find motif");
        AssertEqual(found!.Meta.Name, "Classic Rock Hook A", "Found motif should have correct name");

        Console.WriteLine("  ✓ GetMotifByName finds correct motif");
    }

    // ============================================================
    // Hardcoded Test Motifs Validation Tests
    // ============================================================

    private static void TestAllHardcodedMotifsAreValid()
    {
        var allMotifs = MotifLibrary.GetAllTestMotifs();

        foreach (var motif in allMotifs)
        {
            var issues = MotifValidation.ValidateMotif(motif);
            AssertEqual(issues.Count, 0, $"Hardcoded motif '{motif.Name}' should be valid");
        }

        Console.WriteLine($"  ✓ All {allMotifs.Count} hardcoded motifs are valid");
    }

    private static void TestHardcodedMotifsDeterministic()
    {
        var motif1a = MotifLibrary.ClassicRockHookA();
        var motif1b = MotifLibrary.ClassicRockHookA();

        AssertEqual(motif1a.Name, motif1b.Name, "Hardcoded motif name should be deterministic");
        AssertEqual(motif1a.IntendedRole, motif1b.IntendedRole, "Hardcoded motif role should be deterministic");
        AssertEqual(motif1a.Kind, motif1b.Kind, "Hardcoded motif kind should be deterministic");
        AssertEqual(motif1a.RhythmShape.Count, motif1b.RhythmShape.Count, "Hardcoded motif rhythm count should be deterministic");

        Console.WriteLine("  ✓ Hardcoded motifs are deterministic");
    }

    private static void TestHardcodedMotifsHaveUniqueIds()
    {
        var motif1 = MotifLibrary.ClassicRockHookA();
        var motif2 = MotifLibrary.SteadyVerseRiffA();
        var motif3 = MotifLibrary.BrightSynthHookA();

        // Each call generates a new ID (this is expected)
        AssertNotEqual(motif1.MotifId, motif2.MotifId, "Different motifs should have different IDs");
        AssertNotEqual(motif2.MotifId, motif3.MotifId, "Different motifs should have different IDs");

        Console.WriteLine("  ✓ Hardcoded motifs have unique IDs");
    }

    private static void TestHardcodedMotifsCanBeStored()
    {
        var bank = new MaterialBank();
        var allMotifs = MotifLibrary.GetAllTestMotifs();

        foreach (var motif in allMotifs)
        {
            var track = motif.ToPartTrack();
            bank.Add(track);
        }

        AssertEqual(bank.Count, allMotifs.Count, "All hardcoded motifs should be stored");

        Console.WriteLine($"  ✓ All {allMotifs.Count} hardcoded motifs can be stored");
    }

    // ============================================================
    // Validation Error Detection Tests
    // ============================================================

    private static void TestValidationCatchesEmptyName()
    {
        var motif = MotifSpec.Create(
            name: "",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240 },
            contour: ContourIntent.Flat,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Validation should catch empty name");
        AssertTrue(issues.Any(i => i.Contains("Name")), "Issue should mention Name");

        Console.WriteLine("  ✓ Validation catches empty name");
    }

    private static void TestValidationCatchesEmptyRole()
    {
        var motif = MotifSpec.Create(
            name: "Valid Name",
            intendedRole: "",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240 },
            contour: ContourIntent.Flat,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Validation should catch empty role");
        AssertTrue(issues.Any(i => i.Contains("IntendedRole")), "Issue should mention IntendedRole");

        Console.WriteLine("  ✓ Validation catches empty role");
    }

    private static void TestValidationCatchesInvalidRhythm()
    {
        var motif = MotifSpec.Create(
            name: "Valid Name",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, -100 },
            contour: ContourIntent.Flat,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Validation should catch negative rhythm ticks");
        AssertTrue(issues.Any(i => i.Contains("negative")), "Issue should mention negative ticks");

        Console.WriteLine("  ✓ Validation catches invalid rhythm");
    }

    private static void TestValidationCatchesInvalidRegister()
    {
        var motif = MotifSpec.Create(
            name: "Valid Name",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240 },
            contour: ContourIntent.Flat,
            centerMidiNote: 150, // Too high
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Validation should catch invalid center MIDI");
        AssertTrue(issues.Any(i => i.Contains("CenterMidiNote")), "Issue should mention CenterMidiNote");

        Console.WriteLine("  ✓ Validation catches invalid register");
    }

    private static void TestValidationCatchesInvalidTonePolicy()
    {
        var motif = MotifSpec.Create(
            name: "Valid Name",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240 },
            contour: ContourIntent.Flat,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 1.5, // Too high
            allowPassingTones: true);

        var issues = MotifValidation.ValidateMotif(motif);

        AssertTrue(issues.Count > 0, "Validation should catch invalid chord tone bias");
        AssertTrue(issues.Any(i => i.Contains("ChordToneBias")), "Issue should mention ChordToneBias");

        Console.WriteLine("  ✓ Validation catches invalid tone policy");
    }

    // ============================================================
    // Overall Determinism Test
    // ============================================================

    private static void TestEntireMotifSystemIsDeterministic()
    {
        // Create same motif twice
        var motif1 = MotifSpec.Create(
            name: "Determinism Test",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true,
            tags: new HashSet<string> { "test" });

        var motif2 = MotifSpec.Create(
            name: "Determinism Test",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true,
            tags: new HashSet<string> { "test" });

        // Convert both to tracks
        var track1 = motif1.ToPartTrack();
        var track2 = motif2.ToPartTrack();

        // Validate both
        var issues1 = MotifValidation.ValidateMotif(motif1);
        var issues2 = MotifValidation.ValidateMotif(motif2);

        // All fields should match except MotifId (which is generated)
        AssertEqual(motif1.Name, motif2.Name, "Names should be identical");
        AssertEqual(motif1.IntendedRole, motif2.IntendedRole, "Roles should be identical");
        AssertEqual(motif1.Kind, motif2.Kind, "Kinds should be identical");
        AssertEqual(track1.PartTrackNoteEvents.Count, track2.PartTrackNoteEvents.Count, "Event counts should match");
        AssertEqual(issues1.Count, issues2.Count, "Validation results should be identical");

        Console.WriteLine("  ✓ Entire motif system is deterministic");
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

    private static void AssertNotEqual<T>(T actual, T expected, string message)
    {
        if (EqualityComparer<T>.Default.Equals(actual, expected))
            throw new Exception($"Assertion failed: {message}. Values should not be equal: {actual}");
    }

    private static void AssertNotNull(object? obj, string message)
    {
        if (obj is null)
            throw new Exception($"Assertion failed: {message}");
    }

    private static void AssertNull(object? obj, string message)
    {
        if (obj is not null)
            throw new Exception($"Assertion failed: {message}. Expected null but got: {obj}");
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
