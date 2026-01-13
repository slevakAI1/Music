// AI: purpose=Test suite for Story M1 material fragment data definitions.
// AI: invariants=Tests verify backward compatibility, ID uniqueness, validation rules, and MaterialBank operations.
// AI: deps=Tests PartTrack.Meta defaults, PartTrackMaterialValidation, MaterialBank container; PartTrackId is nested in PartTrack.

using Music.Generator;

namespace Music.Song.Material.Tests;

/// <summary>
/// Test suite for Story M1 - Material fragment data definitions.
/// Verifies backward compatibility, validation rules, and MaterialBank operations.
/// </summary>
public static class MaterialDefinitionsTests
{
    /// <summary>
    /// Runs all Material Definitions tests.
    /// </summary>
    public static void RunAll()
    {
        TestPartTrackDefaultMeta();
        TestPartTrackIdUniqueness();
        TestPartTrackIdNonEmpty();
        TestMaterialDomainValidation();
        TestMaterialNegativeTickValidation();
        TestRoleTrackValidation();
        TestMaterialBankRoundTrip();
        TestMaterialBankGetByKind();
        TestMaterialBankGetByMaterialKind();
        TestMaterialBankGetByRole();
        TestMaterialBankRemove();
        TestMaterialProvenanceDefaults();
        TestPartTrackMetaWithTags();

        Console.WriteLine("? All MaterialDefinitions tests passed");
    }

    /// <summary>
    /// Test that new PartTrack has correct default Meta values for backward compatibility.
    /// </summary>
    private static void TestPartTrackDefaultMeta()
    {
        var track = new PartTrack([]);

        AssertNotNull(track.Meta, "Meta should not be null");
        AssertEqual(track.Meta.Domain, PartTrackDomain.SongAbsolute, "Default domain should be SongAbsolute");
        AssertEqual(track.Meta.Kind, PartTrackKind.RoleTrack, "Default kind should be RoleTrack");
        AssertEqual(track.Meta.MaterialKind, MaterialKind.Unknown, "Default material kind should be Unknown");
        AssertTrue(!string.IsNullOrEmpty(track.Meta.TrackId.Value), "TrackId should be non-empty");

        Console.WriteLine("  ? PartTrack default Meta correct");
    }

    /// <summary>
    /// Test that creating two tracks yields different IDs.
    /// </summary>
    private static void TestPartTrackIdUniqueness()
    {
        var track1 = new PartTrack([]);
        var track2 = new PartTrack([]);

        AssertNotEqual(track1.Meta.TrackId, track2.Meta.TrackId, "Two tracks should have different IDs");

        Console.WriteLine("  ? PartTrackId uniqueness verified");
    }

    /// <summary>
    /// Test that PartTrackId.NewId generates non-empty values.
    /// </summary>
    private static void TestPartTrackIdNonEmpty()
    {
        var id1 = PartTrack.PartTrackId.NewId();
        var id2 = PartTrack.PartTrackId.NewId();

        AssertTrue(!string.IsNullOrEmpty(id1.Value), "NewId should produce non-empty value");
        AssertTrue(!string.IsNullOrEmpty(id2.Value), "NewId should produce non-empty value");
        AssertNotEqual(id1, id2, "NewId should produce unique values");

        Console.WriteLine("  ? PartTrackId.NewId produces non-empty unique values");
    }

    /// <summary>
    /// Test that a track with Kind=MaterialFragment and Domain=SongAbsolute produces validation issue.
    /// </summary>
    private static void TestMaterialDomainValidation()
    {
        var track = new PartTrack([])
        {
            Meta = new PartTrackMeta
            {
                Kind = PartTrackKind.MaterialFragment,
                Domain = PartTrackDomain.SongAbsolute
            }
        };

        var issues = PartTrackMaterialValidation.Validate(track);

        AssertTrue(issues.Count > 0, "Should have validation issues");
        AssertTrue(issues.Any(i => i.Contains("MaterialLocal")), "Should mention MaterialLocal domain");

        Console.WriteLine("  ? Material domain validation correct");
    }

    /// <summary>
    /// Test that a material-local track with negative ticks produces validation issue.
    /// </summary>
    private static void TestMaterialNegativeTickValidation()
    {
        var events = new List<MyMidi.PartTrackEvent>
        {
            new() { AbsoluteTimeTicks = -100, NoteNumber = 60 }
        };

        var track = new PartTrack(events)
        {
            Meta = new PartTrackMeta
            {
                Kind = PartTrackKind.MaterialFragment,
                Domain = PartTrackDomain.MaterialLocal
            }
        };

        var issues = PartTrackMaterialValidation.Validate(track);

        AssertTrue(issues.Count > 0, "Should have validation issues");
        AssertTrue(issues.Any(i => i.Contains("negative")), "Should mention negative ticks");

        Console.WriteLine("  ? Material negative tick validation correct");
    }

    /// <summary>
    /// Test that a valid RoleTrack passes validation.
    /// </summary>
    private static void TestRoleTrackValidation()
    {
        var events = new List<MyMidi.PartTrackEvent>
        {
            new() { AbsoluteTimeTicks = 0, NoteNumber = 60 },
            new() { AbsoluteTimeTicks = 480, NoteNumber = 62 }
        };

        var track = new PartTrack(events);

        var issues = PartTrackMaterialValidation.Validate(track);

        AssertEqual(issues.Count, 0, "RoleTrack should have no validation issues");

        Console.WriteLine("  ? RoleTrack validation passes");
    }

    /// <summary>
    /// Test MaterialBank add ? tryget ? same track id.
    /// </summary>
    private static void TestMaterialBankRoundTrip()
    {
        var bank = new MaterialBank();
        var track = new PartTrack([])
        {
            Meta = new PartTrackMeta
            {
                Kind = PartTrackKind.MaterialFragment,
                Domain = PartTrackDomain.MaterialLocal,
                Name = "Test Fragment"
            }
        };

        bank.Add(track);

        var found = bank.TryGet(track.Meta.TrackId, out var retrieved);

        AssertTrue(found, "TryGet should return true");
        AssertNotNull(retrieved, "Retrieved track should not be null");
        AssertEqual(retrieved!.Meta.TrackId, track.Meta.TrackId, "Retrieved track should have same ID");
        AssertEqual(retrieved.Meta.Name, "Test Fragment", "Retrieved track should have same name");

        Console.WriteLine("  ? MaterialBank round-trip correct");
    }

    /// <summary>
    /// Test MaterialBank GetByKind filtering.
    /// </summary>
    private static void TestMaterialBankGetByKind()
    {
        var bank = new MaterialBank();

        var fragment = CreateTestTrack(PartTrackKind.MaterialFragment, "Fragment");
        var variant = CreateTestTrack(PartTrackKind.MaterialVariant, "Variant");
        var roleTrack = CreateTestTrack(PartTrackKind.RoleTrack, "RoleTrack");

        bank.Add(fragment);
        bank.Add(variant);
        bank.Add(roleTrack);

        var fragments = bank.GetByKind(PartTrackKind.MaterialFragment);
        var variants = bank.GetByKind(PartTrackKind.MaterialVariant);
        var roleTracks = bank.GetByKind(PartTrackKind.RoleTrack);

        AssertEqual(fragments.Count, 1, "Should have 1 fragment");
        AssertEqual(variants.Count, 1, "Should have 1 variant");
        AssertEqual(roleTracks.Count, 1, "Should have 1 role track");

        Console.WriteLine("  ? MaterialBank GetByKind filtering correct");
    }

    /// <summary>
    /// Test MaterialBank GetByMaterialKind filtering.
    /// </summary>
    private static void TestMaterialBankGetByMaterialKind()
    {
        var bank = new MaterialBank();

        var riff = CreateTestTrackWithMaterialKind(MaterialKind.Riff, "Riff");
        var hook = CreateTestTrackWithMaterialKind(MaterialKind.Hook, "Hook");
        var riff2 = CreateTestTrackWithMaterialKind(MaterialKind.Riff, "Riff2");

        bank.Add(riff);
        bank.Add(hook);
        bank.Add(riff2);

        var riffs = bank.GetByMaterialKind(MaterialKind.Riff);
        var hooks = bank.GetByMaterialKind(MaterialKind.Hook);

        AssertEqual(riffs.Count, 2, "Should have 2 riffs");
        AssertEqual(hooks.Count, 1, "Should have 1 hook");

        Console.WriteLine("  ? MaterialBank GetByMaterialKind filtering correct");
    }

    /// <summary>
    /// Test MaterialBank GetByRole filtering (case-insensitive).
    /// </summary>
    private static void TestMaterialBankGetByRole()
    {
        var bank = new MaterialBank();

        var bass1 = CreateTestTrackWithRole("Bass", "Bass1");
        var bass2 = CreateTestTrackWithRole("bass", "Bass2");
        var comp = CreateTestTrackWithRole("Comp", "Comp1");

        bank.Add(bass1);
        bank.Add(bass2);
        bank.Add(comp);

        var bassTracks = bank.GetByRole("BASS");
        var compTracks = bank.GetByRole("comp");

        AssertEqual(bassTracks.Count, 2, "Should have 2 bass tracks (case-insensitive)");
        AssertEqual(compTracks.Count, 1, "Should have 1 comp track");

        Console.WriteLine("  ? MaterialBank GetByRole filtering correct");
    }

    /// <summary>
    /// Test MaterialBank Remove operation.
    /// </summary>
    private static void TestMaterialBankRemove()
    {
        var bank = new MaterialBank();
        var track = new PartTrack([]);

        bank.Add(track);
        AssertTrue(bank.Contains(track.Meta.TrackId), "Bank should contain track");
        AssertEqual(bank.Count, 1, "Bank should have 1 track");

        var removed = bank.Remove(track.Meta.TrackId);
        AssertTrue(removed, "Remove should return true");
        AssertFalse(bank.Contains(track.Meta.TrackId), "Bank should not contain track after remove");
        AssertEqual(bank.Count, 0, "Bank should be empty");

        Console.WriteLine("  ? MaterialBank Remove correct");
    }

    /// <summary>
    /// Test MaterialProvenance has correct defaults.
    /// </summary>
    private static void TestMaterialProvenanceDefaults()
    {
        var provenance = new MaterialProvenance();

        AssertEqual(provenance.BaseSeed, 0, "Default BaseSeed should be 0");
        AssertEqual(provenance.DerivedSeed, 0, "Default DerivedSeed should be 0");
        AssertEqual(provenance.AttemptIndex, 0, "Default AttemptIndex should be 0");
        AssertNull(provenance.SourceFragmentId, "Default SourceFragmentId should be null");
        AssertEqual(provenance.TransformTags.Count, 0, "Default TransformTags should be empty");

        Console.WriteLine("  ? MaterialProvenance defaults correct");
    }

    /// <summary>
    /// Test PartTrackMeta with custom tags.
    /// </summary>
    private static void TestPartTrackMetaWithTags()
    {
        var meta = new PartTrackMeta
        {
            Name = "Test",
            Tags = new HashSet<string> { "dark", "energetic", "syncopated" }
        };

        AssertEqual(meta.Tags.Count, 3, "Should have 3 tags");
        AssertTrue(meta.Tags.Contains("dark"), "Should contain 'dark' tag");
        AssertTrue(meta.Tags.Contains("energetic"), "Should contain 'energetic' tag");

        Console.WriteLine("  ? PartTrackMeta with tags correct");
    }

    // ============================================================================
    // Test Fixture Helpers
    // ============================================================================

    private static PartTrack CreateTestTrack(PartTrackKind kind, string name)
    {
        return new PartTrack([])
        {
            Meta = new PartTrackMeta
            {
                Kind = kind,
                Domain = kind == PartTrackKind.RoleTrack 
                    ? PartTrackDomain.SongAbsolute 
                    : PartTrackDomain.MaterialLocal,
                Name = name
            }
        };
    }

    private static PartTrack CreateTestTrackWithMaterialKind(MaterialKind materialKind, string name)
    {
        return new PartTrack([])
        {
            Meta = new PartTrackMeta
            {
                Kind = PartTrackKind.MaterialFragment,
                Domain = PartTrackDomain.MaterialLocal,
                MaterialKind = materialKind,
                Name = name
            }
        };
    }

    private static PartTrack CreateTestTrackWithRole(string role, string name)
    {
        return new PartTrack([])
        {
            Meta = new PartTrackMeta
            {
                Kind = PartTrackKind.MaterialFragment,
                Domain = PartTrackDomain.MaterialLocal,
                IntendedRole = role,
                Name = name
            }
        };
    }

    // ============================================================================
    // Assertion Helpers
    // ============================================================================

    private static void AssertEqual<T>(T actual, T expected, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
        {
            throw new Exception($"FAIL: {message}\n  Expected: {expected}\n  Actual: {actual}");
        }
    }

    private static void AssertNotEqual<T>(T actual, T notExpected, string message)
    {
        if (EqualityComparer<T>.Default.Equals(actual, notExpected))
        {
            throw new Exception($"FAIL: {message}\n  Should not equal: {notExpected}");
        }
    }

    private static void AssertNotNull(object? obj, string message)
    {
        if (obj == null)
        {
            throw new Exception($"FAIL: {message} (was null)");
        }
    }

    private static void AssertNull(object? obj, string message)
    {
        if (obj != null)
        {
            throw new Exception($"FAIL: {message} (expected null but was {obj})");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"FAIL: {message}");
        }
    }

    private static void AssertFalse(bool condition, string message)
    {
        if (condition)
        {
            throw new Exception($"FAIL: {message}");
        }
    }
}
