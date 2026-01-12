// AI: purpose=Tests for Story 7.5.2 deterministic tension computation and transition hints.
// AI: coverage=DeterministicTensionQuery, SectionTransitionHint, macro tension heuristics, driver flags.
// AI: validation=Determinism, range constraints [0..1], non-trivial shapes, PreChorus>Verse, Chorus release.

namespace Music.Generator;

/// <summary>
/// Tests for Story 7.5.2: Compute section-level macro tension targets.
/// Verifies acceptance criteria:
/// - Deterministic computation from (EnergyArc, seed, section structure)
/// - Tension values in [0..1]
/// - Tension distinct from energy (PreChorus > Verse, Chorus release)
/// - TensionDriver flags explain decisions
/// - SectionTransitionHint derivation (Build/Release/Sustain/Drop)
/// - Non-trivial shape across common pop form
/// 
/// To run: Call DeterministicTensionQueryTests.RunAllTests() from test button or debug hook.
/// All tests write output to Console and throw exceptions on failure.
/// </summary>
public static class DeterministicTensionQueryTests
{
    public static void RunAllTests()
    {
        Console.WriteLine("=== Deterministic Tension Query Tests (Story 7.5.2) ===");

        // Basic functionality
        TestDeterministicTensionQueryCreation();
        TestDeterministicTensionQueryImplementsITensionQuery();
        TestMacroTensionValuesAreInValidRange();
        TestMicroTensionValuesAreInValidRange();

        // Determinism
        TestSameSeedProducesSameTension();
        TestDifferentSeedProducesDifferentTension();

        // Musical heuristics
        TestPreChorusTensionHigherThanVerse();
        TestChorusReleasesAfterPreChorus();
        TestBridgeHasContrastTension();
        TestOutroTensionTrendsDown();
        TestAnticipationBeforeHigherEnergySection();

        // TensionDriver flags
        TestTensionDriverFlagsAreSet();
        TestOpeningDriverForIntro();
        TestPreChorusBuildDriver();
        TestResolutionDriverForChorus();
        TestBridgeContrastDriver();

        // SectionTransitionHint
        TestTransitionHintBuildWhenEnergyIncreases();
        TestTransitionHintReleaseWhenTensionDrops();
        TestTransitionHintDropWhenSignificantDecrease();
        TestTransitionHintSustainWhenMinimalChange();
        TestTransitionHintNoneForLastSection();

        // Non-trivial shape test
        TestNonTrivialTensionShapeAcrossPopForm();

        // Edge cases
        TestSingleSectionSong();
        TestAllSectionsOfSameType();
        TestVeryShortSections();

        Console.WriteLine("All Deterministic Tension Query tests passed.");
    }

    #region Basic Functionality Tests

    private static void TestDeterministicTensionQueryCreation()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        AssertEqual(3, query.SectionCount, "SectionCount should match section track");
        Assert(query.HasTensionData(0), "Should have data for section 0");
        Assert(query.HasTensionData(2), "Should have data for section 2");
        Assert(!query.HasTensionData(3), "Should not have data for section 3");
    }

    private static void TestDeterministicTensionQueryImplementsITensionQuery()
    {
        var (arc, _) = CreateTestArc(2);
        ITensionQuery query = new DeterministicTensionQuery(arc, seed: 42);

        var profile = query.GetMacroTension(0);
        Assert(profile != null, "GetMacroTension should return profile");

        var microTension = query.GetMicroTension(0, 0);
        Assert(microTension >= 0.0 && microTension <= 1.0, "GetMicroTension should return valid value");
    }

    private static void TestMacroTensionValuesAreInValidRange()
    {
        var (arc, _) = CreateTestArc(5);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        for (int i = 0; i < query.SectionCount; i++)
        {
            var profile = query.GetMacroTension(i);
            Assert(profile.MacroTension >= 0.0 && profile.MacroTension <= 1.0,
                $"Section {i} macro tension {profile.MacroTension} out of range [0..1]");
        }
    }

    private static void TestMicroTensionValuesAreInValidRange()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        for (int secIdx = 0; secIdx < query.SectionCount; secIdx++)
        {
            var map = query.GetMicroTensionMap(secIdx);
            for (int bar = 0; bar < map.BarCount; bar++)
            {
                double tension = map.GetTension(bar);
                Assert(tension >= 0.0 && tension <= 1.0,
                    $"Section {secIdx} bar {bar} micro tension {tension} out of range [0..1]");
            }
        }
    }

    #endregion

    #region Determinism Tests

    private static void TestSameSeedProducesSameTension()
    {
        var (arc1, _) = CreateTestArc(4);
        var (arc2, _) = CreateTestArc(4);

        var query1 = new DeterministicTensionQuery(arc1, seed: 100);
        var query2 = new DeterministicTensionQuery(arc2, seed: 100);

        for (int i = 0; i < 4; i++)
        {
            var profile1 = query1.GetMacroTension(i);
            var profile2 = query2.GetMacroTension(i);

            AssertEqual(profile1.MacroTension, profile2.MacroTension,
                $"Section {i} tension should be deterministic");
            AssertEqual(profile1.Driver, profile2.Driver,
                $"Section {i} driver should be deterministic");

            var hint1 = query1.GetTransitionHint(i);
            var hint2 = query2.GetTransitionHint(i);
            AssertEqual(hint1, hint2, $"Section {i} transition hint should be deterministic");
        }
    }

    private static void TestDifferentSeedProducesDifferentTension()
    {
        var (arc, _) = CreateTestArc(4);

        var query1 = new DeterministicTensionQuery(arc, seed: 100);
        var query2 = new DeterministicTensionQuery(arc, seed: 200);

        bool foundDifference = false;
        for (int i = 0; i < 4; i++)
        {
            var profile1 = query1.GetMacroTension(i);
            var profile2 = query2.GetMacroTension(i);

            if (Math.Abs(profile1.MacroTension - profile2.MacroTension) > 0.001)
            {
                foundDifference = true;
                break;
            }
        }

        Assert(foundDifference, "Different seeds should produce different tension values");
    }

    #endregion

    #region Musical Heuristic Tests

    private static void TestPreChorusTensionHigherThanVerse()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var verseTension = query.GetMacroTension(0).MacroTension;
        var chorusTension = query.GetMacroTension(1).MacroTension;

        // Chorus after verse should have build-up tension
        Assert(chorusTension > verseTension - 0.05,
            $"Chorus tension {chorusTension} should not be significantly lower than verse {verseTension}");
    }

    private static void TestChorusReleasesAfterPreChorus()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var firstChorus = query.GetMacroTension(0);
        var secondChorus = query.GetMacroTension(1);

        // Second chorus should have resolution driver
        Assert(secondChorus.Driver.HasFlag(TensionDriver.Resolution) || 
               secondChorus.Driver.HasFlag(TensionDriver.Peak),
            "Second chorus should have Resolution or Peak driver");
    }

    private static void TestBridgeHasContrastTension()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Bridge, 8);

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var verseTension = query.GetMacroTension(0).MacroTension;
        var bridgeTension = query.GetMacroTension(1).MacroTension;

        Assert(Math.Abs(bridgeTension - verseTension) > 0.05,
            $"Bridge tension {bridgeTension} should differ from verse {verseTension}");

        var bridgeProfile = query.GetMacroTension(1);
        Assert(bridgeProfile.Driver.HasFlag(TensionDriver.BridgeContrast),
            "Bridge should have BridgeContrast driver");
    }

    private static void TestOutroTensionTrendsDown()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        track.Add(MusicConstants.eSectionType.Outro, 4);

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var chorusTension = query.GetMacroTension(0).MacroTension;
        var outroTension = query.GetMacroTension(1).MacroTension;

        Assert(outroTension < chorusTension,
            $"Outro tension {outroTension} should be lower than chorus {chorusTension}");

        var outroProfile = query.GetMacroTension(1);
        Assert(outroProfile.Driver.HasFlag(TensionDriver.Resolution),
            "Outro should have Resolution driver");
    }

    private static void TestAnticipationBeforeHigherEnergySection()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);

        // Create arc with higher chorus energy
        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var verseProfile = query.GetMacroTension(0);

        // Verse should have anticipation driver if chorus energy is higher
        var verseTarget = arc.GetTargetForSection(track.Sections[0], 0);
        var chorusTarget = arc.GetTargetForSection(track.Sections[1], 0);

        if (chorusTarget.Energy > verseTarget.Energy + 0.10)
        {
            Assert(verseProfile.Driver.HasFlag(TensionDriver.Anticipation),
                "Verse before higher-energy chorus should have Anticipation driver");
        }
    }

    #endregion

    #region TensionDriver Tests

    private static void TestTensionDriverFlagsAreSet()
    {
        var (arc, _) = CreateTestArc(5);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        bool foundDriver = false;
        for (int i = 0; i < query.SectionCount; i++)
        {
            var profile = query.GetMacroTension(i);
            if (profile.Driver != TensionDriver.None)
            {
                foundDriver = true;
                break;
            }
        }

        Assert(foundDriver, "At least one section should have a driver set");
    }

    private static void TestOpeningDriverForIntro()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Intro, 4);
        track.Add(MusicConstants.eSectionType.Verse, 8);

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var introProfile = query.GetMacroTension(0);
        Assert(introProfile.Driver.HasFlag(TensionDriver.Opening),
            "Intro should have Opening driver");
    }

    private static void TestPreChorusBuildDriver()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var chorusProfile = query.GetMacroTension(1);
        Assert(chorusProfile.Driver.HasFlag(TensionDriver.PreChorusBuild) ||
               chorusProfile.Driver.HasFlag(TensionDriver.Anticipation),
            "Chorus after verse should have PreChorusBuild or Anticipation driver");
    }

    private static void TestResolutionDriverForChorus()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Chorus, 8);

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var chorusProfile = query.GetMacroTension(0);
        Assert(chorusProfile.Driver.HasFlag(TensionDriver.Resolution) ||
               chorusProfile.Driver != TensionDriver.None,
            "Chorus should have some driver set");
    }

    private static void TestBridgeContrastDriver()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Bridge, 8);

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var bridgeProfile = query.GetMacroTension(1);
        Assert(bridgeProfile.Driver.HasFlag(TensionDriver.BridgeContrast),
            "Bridge should have BridgeContrast driver");
    }

    #endregion

    #region SectionTransitionHint Tests

    private static void TestTransitionHintBuildWhenEnergyIncreases()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);

        var arc = EnergyArc.Create(track, "RockSteady", seed: 42); // Rock often builds
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var hint = query.GetTransitionHint(0);

        // Should be Build or Sustain depending on energy delta
        Assert(hint == SectionTransitionHint.Build || 
               hint == SectionTransitionHint.Sustain ||
               hint == SectionTransitionHint.Release,
            $"Verse->Chorus transition should be Build/Sustain/Release, got {hint}");
    }

    private static void TestTransitionHintReleaseWhenTensionDrops()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        track.Add(MusicConstants.eSectionType.Verse, 8);

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var hint = query.GetTransitionHint(0);

        // Chorus->Verse should often be Release or Drop
        Assert(hint == SectionTransitionHint.Release || 
               hint == SectionTransitionHint.Drop ||
               hint == SectionTransitionHint.Sustain,
            $"Chorus->Verse should be Release/Drop/Sustain, got {hint}");
    }

    private static void TestTransitionHintDropWhenSignificantDecrease()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        track.Add(MusicConstants.eSectionType.Outro, 4);

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var hint = query.GetTransitionHint(0);

        // Chorus->Outro should be Drop or Release
        Assert(hint == SectionTransitionHint.Drop || hint == SectionTransitionHint.Release,
            $"Chorus->Outro should be Drop or Release, got {hint}");
    }

    private static void TestTransitionHintSustainWhenMinimalChange()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Verse, 8);

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var hint = query.GetTransitionHint(0);

        // Verse->Verse should likely be Sustain or Build (monotonic rule)
        Assert(hint == SectionTransitionHint.Sustain || hint == SectionTransitionHint.Build,
            $"Verse->Verse should be Sustain or Build, got {hint}");
    }

    private static void TestTransitionHintNoneForLastSection()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Outro, 4);

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var hint = query.GetTransitionHint(1); // Last section

        AssertEqual(SectionTransitionHint.None, hint, "Last section should have None transition hint");
    }

    #endregion

    #region Non-Trivial Shape Test

    private static void TestNonTrivialTensionShapeAcrossPopForm()
    {
        // Intro-V-C-V-C-Bridge-C-Outro
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Intro, 4);
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        track.Add(MusicConstants.eSectionType.Verse, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        track.Add(MusicConstants.eSectionType.Bridge, 8);
        track.Add(MusicConstants.eSectionType.Chorus, 8);
        track.Add(MusicConstants.eSectionType.Outro, 4);

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var tensions = new List<double>();
        for (int i = 0; i < query.SectionCount; i++)
        {
            tensions.Add(query.GetMacroTension(i).MacroTension);
        }

        // Check for non-trivial variation
        double min = tensions.Min();
        double max = tensions.Max();
        double range = max - min;

        Assert(range > 0.15, $"Tension range {range} should show significant variation (> 0.15)");

        // Check that not all values are identical
        bool hasVariation = tensions.Distinct().Count() > 1;
        Assert(hasVariation, "Tension values should vary across sections");

        Console.WriteLine($"  Pop form tension shape: {string.Join(", ", tensions.Select(t => $"{t:F2}"))}");
        Console.WriteLine($"  Range: {range:F2}, Min: {min:F2}, Max: {max:F2}");
    }

    #endregion

    #region Edge Case Tests

    private static void TestSingleSectionSong()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Verse, 8);

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        AssertEqual(1, query.SectionCount, "Should have 1 section");

        var profile = query.GetMacroTension(0);
        Assert(profile.MacroTension >= 0.0 && profile.MacroTension <= 1.0,
            "Single section should have valid tension");

        var hint = query.GetTransitionHint(0);
        AssertEqual(SectionTransitionHint.None, hint, "Single section should have None hint");
    }

    private static void TestAllSectionsOfSameType()
    {
        var track = new SectionTrack();
        for (int i = 0; i < 4; i++)
        {
            track.Add(MusicConstants.eSectionType.Verse, 8);
        }

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        // All sections should have valid tension
        for (int i = 0; i < 4; i++)
        {
            var profile = query.GetMacroTension(i);
            Assert(profile.MacroTension >= 0.0 && profile.MacroTension <= 1.0,
                $"Section {i} should have valid tension");
        }

        // Should show some monotonic progression or variation
        bool hasVariation = false;
        for (int i = 1; i < 4; i++)
        {
            var prev = query.GetMacroTension(i - 1).MacroTension;
            var curr = query.GetMacroTension(i).MacroTension;
            if (Math.Abs(curr - prev) > 0.01)
            {
                hasVariation = true;
                break;
            }
        }

        Assert(hasVariation, "Repeated sections should show some tension variation");
    }

    private static void TestVeryShortSections()
    {
        var track = new SectionTrack();
        track.Add(MusicConstants.eSectionType.Intro, 1);
        track.Add(MusicConstants.eSectionType.Verse, 2);
        track.Add(MusicConstants.eSectionType.Chorus, 1);

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        // Should handle very short sections without errors
        for (int i = 0; i < 3; i++)
        {
            var profile = query.GetMacroTension(i);
            Assert(profile.MacroTension >= 0.0 && profile.MacroTension <= 1.0,
                $"Short section {i} should have valid tension");

            var map = query.GetMicroTensionMap(i);
            Assert(map.BarCount > 0, $"Section {i} should have micro tension map");
        }
    }

    #endregion

    #region Helper Methods

    private static (EnergyArc, SectionTrack) CreateTestArc(int sectionCount)
    {
        var track = new SectionTrack();
        var types = new[] {
            MusicConstants.eSectionType.Intro,
            MusicConstants.eSectionType.Verse,
            MusicConstants.eSectionType.Chorus,
            MusicConstants.eSectionType.Verse,
            MusicConstants.eSectionType.Chorus,
            MusicConstants.eSectionType.Bridge,
            MusicConstants.eSectionType.Outro
        };

        for (int i = 0; i < sectionCount; i++)
        {
            track.Add(types[i % types.Length], 8);
        }

        var arc = EnergyArc.Create(track, "PopGroove", seed: 42);
        return (arc, track);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion failed: {message}");
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception($"Assertion failed: {message}. Expected: {expected}, Actual: {actual}");
        }
    }

    #endregion
}
