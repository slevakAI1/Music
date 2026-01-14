// AI: purpose=Tests for Story 8.2 - Motif storage/retrieval in MaterialBank and MotifSpec↔PartTrack conversion
// AI: invariants=All tests verify determinism, round-trip preservation, and query correctness
using Music.Generator;
using Music.MyMidi;
using Music.Song.Material;

namespace Music.Tests.Material;

public static class MotifStorageTests
{
    public static void RunAllTests()
    {
        Console.WriteLine("=== Story 8.2: Motif Storage and MaterialBank Tests ===\n");

        // Round-trip tests
        TestMotifSpecToPartTrackBasic();
        TestMotifSpecToPartTrackPreservesAllFields();
        TestPartTrackToMotifSpecRoundTrip();
        TestPartTrackToMotifSpecRejectsInvalidDomain();
        TestPartTrackToMotifSpecRejectsInvalidKind();

        // MaterialBank query tests
        TestMaterialBankStoresAndRetrievesMotif();
        TestGetMotifsByRole();
        TestGetMotifsByMaterialKind();
        TestGetMotifByName();
        TestQueryMethodsReturnCorrectSubsets();

        // Validation tests
        TestMotifPartTrackValidationAcceptsValid();
        TestMotifPartTrackValidationRejectsWrongDomain();
        TestMotifPartTrackValidationRejectsInvalidMaterialKind();
        TestMotifPartTrackValidationRejectsNegativeTicks();

        Console.WriteLine("\n✓ All Story 8.2 tests passed!");
    }

    // ============================================================
    // Round-trip Conversion Tests
    // ============================================================

    private static void TestMotifSpecToPartTrackBasic()
    {
        var spec = MotifSpec.Create(
            name: "Test Hook",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 240, 480, 720 },
            contour: ContourIntent.Arch,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true,
            tags: new HashSet<string> { "hooky", "test" });

        var track = spec.ToPartTrack();

        Assert(track.Meta.Domain == PartTrackDomain.MaterialLocal, "ToPartTrack: Domain must be MaterialLocal");
        Assert(track.Meta.Kind == PartTrackKind.MaterialFragment, "ToPartTrack: Kind must be MaterialFragment");
        Assert(track.Meta.MaterialKind == MaterialKind.Hook, "ToPartTrack: MaterialKind must match spec");
        Assert(track.Meta.Name == "Test Hook", "ToPartTrack: Name must match spec");
        Assert(track.Meta.IntendedRole == "Lead", "ToPartTrack: IntendedRole must match spec");
        Assert(track.PartTrackNoteEvents.Count == 4, "ToPartTrack: Event count must match rhythm shape");
        Assert(track.PartTrackNoteEvents[0].AbsoluteTimeTicks == 0, "ToPartTrack: First tick must be 0");
        Assert(track.PartTrackNoteEvents[3].AbsoluteTimeTicks == 720, "ToPartTrack: Last tick must be 720");

        Console.WriteLine("✓ MotifSpec.ToPartTrack basic conversion");
    }

    private static void TestMotifSpecToPartTrackPreservesAllFields()
    {
        var spec = MotifSpec.Create(
            name: "Complex Riff",
            intendedRole: "GuitarHook",
            kind: MaterialKind.Riff,
            rhythmShape: new List<int> { 0, 120, 480 },
            contour: ContourIntent.ZigZag,
            centerMidiNote: 55,
            rangeSemitones: 18,
            chordToneBias: 0.9,
            allowPassingTones: false,
            tags: new HashSet<string> { "energetic", "syncopated" });

        var track = spec.ToPartTrack();

        Assert(track.Meta.TrackId == spec.MotifId, "ToPartTrack: TrackId must match MotifId");
        Assert(track.Meta.Tags != null && track.Meta.Tags.SetEquals(spec.Tags), "ToPartTrack: Tags must be preserved");

        Console.WriteLine("✓ MotifSpec.ToPartTrack preserves all fields");
    }

    private static void TestPartTrackToMotifSpecRoundTrip()
    {
        var original = MotifSpec.Create(
            name: "Round Trip Test",
            intendedRole: "BassRiff",
            kind: MaterialKind.BassFill,
            rhythmShape: new List<int> { 0, 240, 480, 960 },
            contour: ContourIntent.Up,
            centerMidiNote: 48,
            rangeSemitones: 12,
            chordToneBias: 0.7,
            allowPassingTones: true);

        var track = original.ToPartTrack();
        var reconstructed = MotifConversion.FromPartTrack(track);

        Assert(reconstructed != null, "FromPartTrack: Must reconstruct valid spec");
        Assert(reconstructed.MotifId == original.MotifId, "Round-trip: MotifId must be preserved");
        Assert(reconstructed.Name == original.Name, "Round-trip: Name must be preserved");
        Assert(reconstructed.IntendedRole == original.IntendedRole, "Round-trip: IntendedRole must be preserved");
        Assert(reconstructed.Kind == original.Kind, "Round-trip: MaterialKind must be preserved");
        Assert(reconstructed.RhythmShape.SequenceEqual(original.RhythmShape), "Round-trip: RhythmShape must be preserved");

        Console.WriteLine("✓ MotifSpec → PartTrack → MotifSpec round-trip preserves data");
    }

    private static void TestPartTrackToMotifSpecRejectsInvalidDomain()
    {
        var track = new PartTrack(new List<PartTrackEvent>())
        {
            Meta = new PartTrackMeta
            {
                Domain = PartTrackDomain.SongAbsolute, // Wrong domain
                Kind = PartTrackKind.MaterialFragment,
                MaterialKind = MaterialKind.Hook
            }
        };

        var result = MotifConversion.FromPartTrack(track);

        Assert(result == null, "FromPartTrack: Must reject SongAbsolute domain");

        Console.WriteLine("✓ FromPartTrack rejects invalid domain");
    }

    private static void TestPartTrackToMotifSpecRejectsInvalidKind()
    {
        var track = new PartTrack(new List<PartTrackEvent>())
        {
            Meta = new PartTrackMeta
            {
                Domain = PartTrackDomain.MaterialLocal,
                Kind = PartTrackKind.RoleTrack, // Wrong kind
                MaterialKind = MaterialKind.Hook
            }
        };

        var result = MotifConversion.FromPartTrack(track);

        Assert(result == null, "FromPartTrack: Must reject RoleTrack kind");

        Console.WriteLine("✓ FromPartTrack rejects invalid kind");
    }

    // ============================================================
    // MaterialBank Query Tests
    // ============================================================

    private static void TestMaterialBankStoresAndRetrievesMotif()
    {
        var bank = new MaterialBank();
        var spec = MotifSpec.Create(
            name: "Bank Test",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: new List<int> { 0, 480 },
            contour: ContourIntent.Flat,
            centerMidiNote: 60,
            rangeSemitones: 12,
            chordToneBias: 0.8,
            allowPassingTones: true);

        var track = spec.ToPartTrack();
        bank.Add(track);

        Assert(bank.Contains(spec.MotifId), "MaterialBank: Must contain added motif");
        
        bank.TryGet(spec.MotifId, out var retrieved);
        Assert(retrieved != null, "MaterialBank: Must retrieve added motif");
        Assert(retrieved.Meta.Name == "Bank Test", "MaterialBank: Retrieved motif must match");

        Console.WriteLine("✓ MaterialBank stores and retrieves motifs");
    }

    private static void TestGetMotifsByRole()
    {
        var bank = new MaterialBank();
        
        var lead1 = MotifSpec.Create("Lead Hook 1", "Lead", MaterialKind.Hook, 
            new List<int> { 0 }, ContourIntent.Flat, 60, 12, 0.8, true);
        var lead2 = MotifSpec.Create("Lead Riff 1", "Lead", MaterialKind.Riff, 
            new List<int> { 0 }, ContourIntent.Flat, 60, 12, 0.8, true);
        var bass = MotifSpec.Create("Bass Riff", "BassRiff", MaterialKind.BassFill, 
            new List<int> { 0 }, ContourIntent.Flat, 48, 12, 0.7, true);

        bank.Add(lead1.ToPartTrack());
        bank.Add(lead2.ToPartTrack());
        bank.Add(bass.ToPartTrack());

        var leadMotifs = bank.GetMotifsByRole("Lead");
        Assert(leadMotifs.Count == 2, "GetMotifsByRole: Must return 2 Lead motifs");
        
        var bassMotifs = bank.GetMotifsByRole("BassRiff");
        Assert(bassMotifs.Count == 1, "GetMotifsByRole: Must return 1 BassRiff motif");

        // Test case-insensitivity
        var leadMotifsCaseInsensitive = bank.GetMotifsByRole("LEAD");
        Assert(leadMotifsCaseInsensitive.Count == 2, "GetMotifsByRole: Must be case-insensitive");

        Console.WriteLine("✓ GetMotifsByRole returns correct subsets");
    }

    private static void TestGetMotifsByMaterialKind()
    {
        var bank = new MaterialBank();
        
        var hook1 = MotifSpec.Create("Hook 1", "Lead", MaterialKind.Hook, 
            new List<int> { 0 }, ContourIntent.Flat, 60, 12, 0.8, true);
        var hook2 = MotifSpec.Create("Hook 2", "Guitar", MaterialKind.Hook, 
            new List<int> { 0 }, ContourIntent.Flat, 60, 12, 0.8, true);
        var riff = MotifSpec.Create("Riff 1", "Bass", MaterialKind.Riff, 
            new List<int> { 0 }, ContourIntent.Flat, 48, 12, 0.7, true);

        bank.Add(hook1.ToPartTrack());
        bank.Add(hook2.ToPartTrack());
        bank.Add(riff.ToPartTrack());

        var hooks = bank.GetMotifsByMaterialKind(MaterialKind.Hook);
        Assert(hooks.Count == 2, "GetMotifsByMaterialKind: Must return 2 Hooks");
        
        var riffs = bank.GetMotifsByMaterialKind(MaterialKind.Riff);
        Assert(riffs.Count == 1, "GetMotifsByMaterialKind: Must return 1 Riff");

        Console.WriteLine("✓ GetMotifsByMaterialKind returns correct subsets");
    }

    private static void TestGetMotifByName()
    {
        var bank = new MaterialBank();
        
        var spec = MotifSpec.Create("Unique Hook Name", "Lead", MaterialKind.Hook, 
            new List<int> { 0 }, ContourIntent.Flat, 60, 12, 0.8, true);
        bank.Add(spec.ToPartTrack());

        var found = bank.GetMotifByName("Unique Hook Name");
        Assert(found != null, "GetMotifByName: Must find exact match");
        Assert(found.Meta.Name == "Unique Hook Name", "GetMotifByName: Must return correct motif");

        var notFound = bank.GetMotifByName("Nonexistent");
        Assert(notFound == null, "GetMotifByName: Must return null for nonexistent name");

        // Test case-insensitivity
        var foundCaseInsensitive = bank.GetMotifByName("unique hook name");
        Assert(foundCaseInsensitive != null, "GetMotifByName: Must be case-insensitive");

        Console.WriteLine("✓ GetMotifByName finds motifs correctly");
    }

    private static void TestQueryMethodsReturnCorrectSubsets()
    {
        var bank = new MaterialBank();
        
        // Add diverse motifs
        var leadHook = MotifSpec.Create("Lead Hook", "Lead", MaterialKind.Hook, 
            new List<int> { 0, 480 }, ContourIntent.Arch, 60, 12, 0.8, true,
            new HashSet<string> { "hooky" });
        var bassRiff = MotifSpec.Create("Bass Riff", "BassRiff", MaterialKind.Riff, 
            new List<int> { 0, 240, 480 }, ContourIntent.Flat, 48, 12, 0.7, true);
        var drumFill = MotifSpec.Create("Drum Fill", "Drums", MaterialKind.DrumFill, 
            new List<int> { 0, 120, 240 }, ContourIntent.Up, 55, 6, 0.5, false);

        bank.Add(leadHook.ToPartTrack());
        bank.Add(bassRiff.ToPartTrack());
        bank.Add(drumFill.ToPartTrack());

        // Verify each query method returns correct subset
        var allMotifs = bank.GetByKind(PartTrackKind.MaterialFragment);
        Assert(allMotifs.Count == 3, "Query: Bank must contain 3 motifs total");

        var leadMotifs = bank.GetMotifsByRole("Lead");
        Assert(leadMotifs.Count == 1 && leadMotifs[0].Meta.Name == "Lead Hook", 
            "Query: Must isolate Lead role");

        var hooks = bank.GetMotifsByMaterialKind(MaterialKind.Hook);
        Assert(hooks.Count == 1 && hooks[0].Meta.Name == "Lead Hook", 
            "Query: Must isolate Hook kind");

        Console.WriteLine("✓ All query methods return correct subsets");
    }

    // ============================================================
    // Validation Tests
    // ============================================================

    private static void TestMotifPartTrackValidationAcceptsValid()
    {
        var spec = MotifSpec.Create("Valid Motif", "Lead", MaterialKind.Hook,
            new List<int> { 0, 240, 480 }, ContourIntent.Arch, 60, 12, 0.8, true);
        var track = spec.ToPartTrack();

        var issues = MotifPartTrackValidation.ValidateMotifTrack(track);

        Assert(issues.Count == 0, "Validation: Valid motif track must have no issues");

        Console.WriteLine("✓ Validation accepts valid motif track");
    }

    private static void TestMotifPartTrackValidationRejectsWrongDomain()
    {
        var track = new PartTrack(new List<PartTrackEvent>())
        {
            Meta = new PartTrackMeta
            {
                Name = "Wrong Domain",
                Domain = PartTrackDomain.SongAbsolute,
                Kind = PartTrackKind.MaterialFragment,
                MaterialKind = MaterialKind.Hook
            }
        };

        var issues = MotifPartTrackValidation.ValidateMotifTrack(track);

        Assert(issues.Count > 0, "Validation: Must reject wrong domain");
        Assert(issues.Any(i => i.Contains("MaterialLocal")), "Validation: Must mention MaterialLocal requirement");

        Console.WriteLine("✓ Validation rejects wrong domain");
    }

    private static void TestMotifPartTrackValidationRejectsInvalidMaterialKind()
    {
        var track = new PartTrack(new List<PartTrackEvent>())
        {
            Meta = new PartTrackMeta
            {
                Name = "Invalid Kind",
                Domain = PartTrackDomain.MaterialLocal,
                Kind = PartTrackKind.MaterialFragment,
                MaterialKind = MaterialKind.Unknown
            }
        };

        var issues = MotifPartTrackValidation.ValidateMotifTrack(track);

        Assert(issues.Count > 0, "Validation: Must reject invalid MaterialKind");
        Assert(issues.Any(i => i.Contains("invalid MaterialKind")), "Validation: Must mention MaterialKind issue");

        Console.WriteLine("✓ Validation rejects invalid MaterialKind");
    }

    private static void TestMotifPartTrackValidationRejectsNegativeTicks()
    {
        var events = new List<PartTrackEvent>
        {
            new PartTrackEvent(60, -100, 240, 100) // Negative tick
        };

        var track = new PartTrack(events)
        {
            Meta = new PartTrackMeta
            {
                Name = "Negative Ticks",
                Domain = PartTrackDomain.MaterialLocal,
                Kind = PartTrackKind.MaterialFragment,
                MaterialKind = MaterialKind.Hook
            }
        };

        var issues = MotifPartTrackValidation.ValidateMotifTrack(track);

        Assert(issues.Count > 0, "Validation: Must reject negative ticks");
        Assert(issues.Any(i => i.Contains("negative ticks")), "Validation: Must mention negative ticks");

        Console.WriteLine("✓ Validation rejects negative ticks");
    }

    // ============================================================
    // Test Helpers
    // ============================================================

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Test failed: {message}");
        }
    }
}
