// AI: purpose=Test suite for ISongIntentQuery unified Stage 7 intent query (Story 7.9).
// AI: invariants=All tests verify determinism, valid ranges, and correct aggregation of energy/tension/variation queries.
// AI: deps=Tests DeterministicSongIntentQuery aggregation; uses existing EnergyArc, ITensionQuery, IVariationQuery test fixtures.

namespace Music.Generator.Tests;

/// <summary>
/// Test suite for Story 7.9 - Unified Stage 7 intent query for Stage 8/9 integration.
/// Verifies correct aggregation of energy, tension, and variation queries into single stable contract.
/// </summary>
public static class SongIntentQueryTests
{
    /// <summary>
    /// Runs all Song Intent Query tests.
    /// </summary>
    public static void RunAll()
    {
        TestSectionIntentAggregation();
        TestBarIntentAggregation();
        TestDeterminism();
        TestRolePresenceMapping();
        TestRegisterConstraints();
        TestDensityCapsByEnergy();
        TestVariationIntegration();
        TestTensionIntegration();
        TestMicroArcIntegration();
        TestPhrasePositionFlags();
        TestTransitionHintPropagation();
        TestInvalidSectionHandling();
        TestCacheConsistency();
        TestEffectiveEnergyCalculation();
        TestVocalBandReservation();
        TestSectionCount();

        Console.WriteLine("? All SongIntentQuery tests passed");
    }

    /// <summary>
    /// Test that section intent correctly aggregates energy profile data.
    /// </summary>
    private static void TestSectionIntentAggregation()
    {
        var (query, profiles, _, _) = CreateTestQuery();

        var intent = query.GetSectionIntent(0);

        // Verify aggregation from energy profile
        var profile = profiles[0];
        AssertEqual(intent.Energy, profile.Global.Energy, "Energy should match profile");
        AssertEqual(intent.SectionType, profile.Section.SectionType, "Section type should match");
        AssertEqual(intent.AbsoluteSectionIndex, 0, "Section index should be 0");

        // Verify role presence hints derived from orchestration
        AssertEqual(intent.RolePresence.BassPresent, profile.Orchestration.BassPresent, "Bass presence should match");
        AssertEqual(intent.RolePresence.CymbalLanguage, profile.Orchestration.CymbalLanguage, "Cymbal language should match");

        Console.WriteLine("  ? Section intent aggregation correct");
    }

    /// <summary>
    /// Test that bar intent correctly aggregates section + bar-level data.
    /// </summary>
    private static void TestBarIntentAggregation()
    {
        var (query, _, tensionQuery, _) = CreateTestQuery();

        var barIntent = query.GetBarIntent(0, 2);

        // Verify section context is embedded
        AssertNotNull(barIntent.Section, "Section context should be present");
        AssertEqual(barIntent.BarIndexWithinSection, 2, "Bar index should be 2");

        // Verify micro tension from tension query
        var expectedMicroTension = tensionQuery.GetMicroTension(0, 2);
        AssertEqual(barIntent.MicroTension, expectedMicroTension, "Micro tension should match query");

        // Verify phrase flags from tension query
        var (isPhraseEnd, isSectionEnd, isSectionStart) = tensionQuery.GetPhraseFlags(0, 2);
        AssertEqual(barIntent.IsPhraseEnd, isPhraseEnd, "Phrase end flag should match");
        AssertEqual(barIntent.IsSectionEnd, isSectionEnd, "Section end flag should match");
        AssertEqual(barIntent.IsSectionStart, isSectionStart, "Section start flag should match");

        Console.WriteLine("  ? Bar intent aggregation correct");
    }

    /// <summary>
    /// Test that query results are deterministic for same inputs.
    /// </summary>
    private static void TestDeterminism()
    {
        var (query1, profiles, tensionQuery, variationQuery) = CreateTestQuery(seed: 42);
        var (query2, _, _, _) = CreateTestQuery(seed: 42);

        // Section intent should be identical
        var intent1 = query1.GetSectionIntent(0);
        var intent2 = query2.GetSectionIntent(0);

        AssertEqual(intent1.Energy, intent2.Energy, "Energy should be deterministic");
        AssertEqual(intent1.Tension, intent2.Tension, "Tension should be deterministic");
        AssertEqual(intent1.VariationIntensity, intent2.VariationIntensity, "Variation intensity should be deterministic");

        // Bar intent should be identical
        var barIntent1 = query1.GetBarIntent(0, 3);
        var barIntent2 = query2.GetBarIntent(0, 3);

        AssertEqual(barIntent1.MicroTension, barIntent2.MicroTension, "Micro tension should be deterministic");
        AssertEqual(barIntent1.EnergyDelta, barIntent2.EnergyDelta, "Energy delta should be deterministic");
        AssertEqual(barIntent1.PhrasePosition, barIntent2.PhrasePosition, "Phrase position should be deterministic");

        Console.WriteLine("  ? Determinism verified");
    }

    /// <summary>
    /// Test that role presence hints correctly map from orchestration profile.
    /// </summary>
    private static void TestRolePresenceMapping()
    {
        var (query, _, _, _) = CreateTestQuery();

        var intent = query.GetSectionIntent(0);
        var hints = intent.RolePresence;

        // All roles should be present in test fixture
        AssertTrue(hints.BassPresent, "Bass should be present");
        AssertTrue(hints.CompPresent, "Comp should be present");
        AssertTrue(hints.KeysPresent, "Keys should be present");
        AssertTrue(hints.PadsPresent, "Pads should be present");
        AssertTrue(hints.DrumsPresent, "Drums should be present");

        Console.WriteLine("  ? Role presence mapping correct");
    }

    /// <summary>
    /// Test that register constraints are correctly standardized.
    /// </summary>
    private static void TestRegisterConstraints()
    {
        var (query, _, _, _) = CreateTestQuery();

        var intent = query.GetSectionIntent(0);
        var constraints = intent.RegisterConstraints;

        // Verify expected constants from existing generator code
        AssertEqual(constraints.LeadSpaceCeiling, 72, "Lead space ceiling should be C5 (MIDI 72)");
        AssertEqual(constraints.BassFloor, 52, "Bass floor should be E3 (MIDI 52)");
        AssertEqual(constraints.VocalBand.MinMidi, 60, "Vocal band min should be C4 (MIDI 60)");
        AssertEqual(constraints.VocalBand.MaxMidi, 76, "Vocal band max should be E5 (MIDI 76)");

        Console.WriteLine("  ? Register constraints correct");
    }

    /// <summary>
    /// Test that density caps vary appropriately with section energy.
    /// </summary>
    private static void TestDensityCapsByEnergy()
    {
        var (queryLow, _, _, _) = CreateTestQueryWithEnergy(0.2);  // Low energy
        var (queryMid, _, _, _) = CreateTestQueryWithEnergy(0.5);  // Mid energy
        var (queryHigh, _, _, _) = CreateTestQueryWithEnergy(0.85); // High energy

        var capsLow = queryLow.GetSectionIntent(0).DensityCaps;
        var capsMid = queryMid.GetSectionIntent(0).DensityCaps;
        var capsHigh = queryHigh.GetSectionIntent(0).DensityCaps;

        // Low energy should have lower caps
        AssertTrue(capsLow.Bass < capsMid.Bass, "Low energy bass cap should be lower than mid");
        AssertTrue(capsLow.Comp < capsMid.Comp, "Low energy comp cap should be lower than mid");

        // High energy should have higher caps
        AssertTrue(capsHigh.Bass > capsMid.Bass, "High energy bass cap should be higher than mid");
        AssertTrue(capsHigh.Drums > capsMid.Drums, "High energy drums cap should be higher than mid");

        // All caps should be in valid range [0..1]
        AssertInRange(capsLow.Bass, 0.0, 1.0, "Low energy bass cap should be in range");
        AssertInRange(capsHigh.Pads, 0.0, 1.0, "High energy pads cap should be in range");

        Console.WriteLine("  ? Density caps vary correctly with energy");
    }

    /// <summary>
    /// Test variation plan integration (base reference, intensity, tags).
    /// </summary>
    private static void TestVariationIntegration()
    {
        var (query, _, _, variationQuery) = CreateTestQuery();

        var intent = query.GetSectionIntent(0);
        var variationPlan = variationQuery.GetVariationPlan(0);

        // Verify variation data propagated correctly
        AssertEqual(intent.VariationIntensity, variationPlan.VariationIntensity, "Variation intensity should match plan");
        AssertEqual(intent.BaseReferenceSectionIndex, variationPlan.BaseReferenceSectionIndex, "Base reference should match plan");

        // Tags should match
        AssertTrue(intent.VariationTags.SetEquals(variationPlan.Tags), "Variation tags should match plan");

        Console.WriteLine("  ? Variation integration correct");
    }

    /// <summary>
    /// Test tension integration (macro tension, drivers, transition hint).
    /// </summary>
    private static void TestTensionIntegration()
    {
        var (query, _, tensionQuery, _) = CreateTestQuery();

        var intent = query.GetSectionIntent(0);
        var macroTension = tensionQuery.GetMacroTension(0);
        var transitionHint = tensionQuery.GetTransitionHint(0);

        // Verify tension data propagated correctly
        AssertEqual(intent.Tension, macroTension.MacroTension, "Tension should match macro tension");
        AssertEqual(intent.TensionDrivers, macroTension.Driver, "Tension drivers should match");
        AssertEqual(intent.TransitionHint, transitionHint, "Transition hint should match query");

        Console.WriteLine("  ? Tension integration correct");
    }

    /// <summary>
    /// Test micro-arc integration (energy delta, phrase position).
    /// </summary>
    private static void TestMicroArcIntegration()
    {
        var (query, profiles, _, _) = CreateTestQuery();

        var barIntent = query.GetBarIntent(0, 1);
        var profile = profiles[0];

        if (profile.MicroArc != null)
        {
            var expectedDelta = profile.MicroArc.GetEnergyDelta(1);
            var expectedPosition = profile.MicroArc.GetPhrasePosition(1);

            AssertEqual(barIntent.EnergyDelta, expectedDelta, "Energy delta should match micro-arc");
            AssertEqual(barIntent.PhrasePosition, expectedPosition, "Phrase position should match micro-arc");
        }
        else
        {
            // If no micro-arc, expect defaults
            AssertEqual(barIntent.EnergyDelta, 0.0, "Energy delta should be zero without micro-arc");
            AssertEqual(barIntent.PhrasePosition, PhrasePosition.Middle, "Phrase position should be Middle without micro-arc");
        }

        Console.WriteLine("  ? Micro-arc integration correct");
    }

    /// <summary>
    /// Test phrase position flags (phrase end, section start/end).
    /// </summary>
    private static void TestPhrasePositionFlags()
    {
        var (query, _, tensionQuery, _) = CreateTestQuery();

        // Test various bars for flag correctness
        for (int bar = 0; bar < 8; bar++)
        {
            var barIntent = query.GetBarIntent(0, bar);
            var (isPhraseEnd, isSectionEnd, isSectionStart) = tensionQuery.GetPhraseFlags(0, bar);

            AssertEqual(barIntent.IsPhraseEnd, isPhraseEnd, $"Bar {bar} phrase end flag should match");
            AssertEqual(barIntent.IsSectionEnd, isSectionEnd, $"Bar {bar} section end flag should match");
            AssertEqual(barIntent.IsSectionStart, isSectionStart, $"Bar {bar} section start flag should match");
        }

        Console.WriteLine("  ? Phrase position flags correct");
    }

    /// <summary>
    /// Test transition hint propagation from tension query.
    /// </summary>
    private static void TestTransitionHintPropagation()
    {
        var (query, _, tensionQuery, _) = CreateTestQuery();

        var intent = query.GetSectionIntent(0);
        var expectedHint = tensionQuery.GetTransitionHint(0);

        AssertEqual(intent.TransitionHint, expectedHint, "Transition hint should propagate from tension query");

        Console.WriteLine("  ? Transition hint propagation correct");
    }

    /// <summary>
    /// Test that invalid section indices throw appropriate exceptions.
    /// </summary>
    private static void TestInvalidSectionHandling()
    {
        var (query, _, _, _) = CreateTestQuery();

        // Test negative index
        AssertThrows(() => query.GetSectionIntent(-1), "Negative section index should throw");

        // Test out-of-range index
        AssertThrows(() => query.GetSectionIntent(999), "Out-of-range section index should throw");

        // Test HasIntentData for invalid indices
        AssertFalse(query.HasIntentData(-1), "HasIntentData should return false for negative index");
        AssertFalse(query.HasIntentData(999), "HasIntentData should return false for out-of-range index");

        Console.WriteLine("  ? Invalid section handling correct");
    }

    /// <summary>
    /// Test that precomputed cache is consistent with direct query results.
    /// </summary>
    private static void TestCacheConsistency()
    {
        var (query, profiles, tensionQuery, variationQuery) = CreateTestQuery();

        // Query same section multiple times - should return identical object (cached)
        var intent1 = query.GetSectionIntent(0);
        var intent2 = query.GetSectionIntent(0);

        AssertEqual(intent1, intent2, "Cached section intent should be identical object");

        // Verify cache matches fresh computation
        var profile = profiles[0];
        AssertEqual(intent1.Energy, profile.Global.Energy, "Cached energy should match profile");

        Console.WriteLine("  ? Cache consistency verified");
    }

    /// <summary>
    /// Test effective energy calculation (section energy + bar delta, clamped).
    /// </summary>
    private static void TestEffectiveEnergyCalculation()
    {
        var (query, profiles, _, _) = CreateTestQuery();

        var barIntent = query.GetBarIntent(0, 2);
        var profile = profiles[0];

        var expectedEffective = Math.Clamp(
            profile.Global.Energy + (profile.MicroArc?.GetEnergyDelta(2) ?? 0.0),
            0.0,
            1.0);

        AssertEqual(barIntent.EffectiveEnergy, expectedEffective, "Effective energy should be section + delta, clamped");

        // Verify clamping at boundaries
        var highEnergy = CreateTestQueryWithEnergy(1.0);
        var barHigh = highEnergy.query.GetBarIntent(0, 0);
        AssertTrue(barHigh.EffectiveEnergy <= 1.0, "Effective energy should not exceed 1.0");

        Console.WriteLine("  ? Effective energy calculation correct");
    }

    /// <summary>
    /// Test vocal band reservation for future melody integration.
    /// </summary>
    private static void TestVocalBandReservation()
    {
        var (query, _, _, _) = CreateTestQuery();

        var intent = query.GetSectionIntent(0);
        var vocalBand = intent.RegisterConstraints.VocalBand;

        // Verify vocal band is within lead space
        AssertTrue(vocalBand.MinMidi < intent.RegisterConstraints.LeadSpaceCeiling,
            "Vocal band min should be below lead space ceiling");
        AssertTrue(vocalBand.MaxMidi <= intent.RegisterConstraints.LeadSpaceCeiling + 12,
            "Vocal band max should be near lead space ceiling");

        // Verify vocal band is sensible range (not too wide)
        int bandWidth = vocalBand.MaxMidi - vocalBand.MinMidi;
        AssertTrue(bandWidth >= 12 && bandWidth <= 24,
            "Vocal band should be 1-2 octaves wide");

        Console.WriteLine("  ? Vocal band reservation correct");
    }

    /// <summary>
    /// Test section count property.
    /// </summary>
    private static void TestSectionCount()
    {
        var (query, profiles, _, _) = CreateTestQuery();

        AssertEqual(query.SectionCount, profiles.Count, "Section count should match profile count");
        AssertTrue(query.SectionCount > 0, "Section count should be positive");

        Console.WriteLine("  ? Section count correct");
    }

    // ============================================================================
    // Test Fixture Helpers
    // ============================================================================

    private static (ISongIntentQuery query, Dictionary<int, EnergySectionProfile> profiles, ITensionQuery tensionQuery, IVariationQuery variationQuery)
        CreateTestQuery(int seed = 42)
    {
        var sectionTrack = CreateTestSectionTrack();
        var grooveName = "TestGroove";

        var energyArc = EnergyArc.Create(sectionTrack, grooveName, seed);
        var profiles = BuildTestSectionProfiles(energyArc, sectionTrack, seed);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed);
        var variationQuery = new DeterministicVariationQuery(sectionTrack, energyArc, tensionQuery, grooveName, seed);

        var query = new DeterministicSongIntentQuery(profiles, tensionQuery, variationQuery);

        return (query, profiles, tensionQuery, variationQuery);
    }

    private static (ISongIntentQuery query, Dictionary<int, EnergySectionProfile> profiles, ITensionQuery tensionQuery, IVariationQuery variationQuery)
        CreateTestQueryWithEnergy(double energy)
    {
        var sectionTrack = CreateTestSectionTrack();
        var grooveName = "TestGroove";
        int seed = 42;

        var energyArc = EnergyArc.Create(sectionTrack, grooveName, seed);
        
        // Build profiles with overridden energy
        var overriddenProfiles = new Dictionary<int, EnergySectionProfile>();
        int sectionIndex = 0;
        EnergySectionProfile? previousProfile = null;

        foreach (var section in sectionTrack.Sections)
        {
            // Build profile normally first
            var profile = EnergyProfileBuilder.BuildProfile(
                energyArc,
                section,
                sectionIndex,
                previousProfile,
                seed);

            // Create new global targets with overridden energy
            var newGlobal = new EnergyGlobalTargets
            {
                Energy = energy,
                TensionTarget = profile.Global.TensionTarget,
                ContrastBias = profile.Global.ContrastBias
            };

            // Create new profile with overridden global
            var newProfile = new EnergySectionProfile
            {
                Global = newGlobal,
                Roles = profile.Roles,
                Orchestration = profile.Orchestration,
                Section = profile.Section,
                SectionIndex = profile.SectionIndex,
                MicroArc = profile.MicroArc
            };

            overriddenProfiles[sectionIndex] = newProfile;
            previousProfile = newProfile;
            sectionIndex++;
        }

        var tensionQuery = new DeterministicTensionQuery(energyArc, seed);
        var variationQuery = new DeterministicVariationQuery(sectionTrack, energyArc, tensionQuery, grooveName, seed);
        var query = new DeterministicSongIntentQuery(overriddenProfiles, tensionQuery, variationQuery);

        return (query, overriddenProfiles, tensionQuery, variationQuery);
    }

    private static SectionTrack CreateTestSectionTrack()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        return track;
    }

    private static Dictionary<int, EnergySectionProfile> BuildTestSectionProfiles(
        EnergyArc energyArc,
        SectionTrack sectionTrack,
        int seed)
    {
        var profiles = new Dictionary<int, EnergySectionProfile>();
        int sectionIndex = 0;
        EnergySectionProfile? previousProfile = null;

        foreach (var section in sectionTrack.Sections)
        {
            var profile = EnergyProfileBuilder.BuildProfile(
                energyArc,
                section,
                sectionIndex,
                previousProfile,
                seed);

            profiles[sectionIndex] = profile;
            previousProfile = profile;
            sectionIndex++;
        }

        return profiles;
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

    private static void AssertNotNull(object? obj, string message)
    {
        if (obj == null)
        {
            throw new Exception($"FAIL: {message} (was null)");
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

    private static void AssertInRange(double value, double min, double max, string message)
    {
        if (value < min || value > max)
        {
            throw new Exception($"FAIL: {message}\n  Value {value} not in range [{min}..{max}]");
        }
    }

    private static void AssertThrows(Action action, string message)
    {
        try
        {
            action();
            throw new Exception($"FAIL: {message} (expected exception but none thrown)");
        }
        catch (Exception ex) when (ex.Message.StartsWith("FAIL:"))
        {
            throw; // Re-throw test failures
        }
        catch
        {
            // Expected exception caught, test passes
        }
    }
}
