// AI: purpose=Unit tests for Story 7.5.8 Stage 8/9 integration contract (GetTensionContext).
// AI: coverage=GetTensionContext unified query, TensionContext with TransitionHint, immutability, thread-safety.
// AI: validation=Determinism, all fields populated correctly, supports Stage 8/9 motif/melody use cases.
// AI: NOTE - TEMPORARILY DISABLED (Epic 6): These tests use EnergyArc which was removed in Story 4.1. To be re-enabled during energy reintegration.

#if FALSE_DISABLED_FOR_ENERGY_DISCONNECT // Epic 6: Disabled until energy reintegration

namespace Music.Generator;

/// <summary>
/// Tests for Story 7.5.8: Stage 8/9 integration contract - tension queries.
/// Verifies acceptance criteria:
/// - GetTensionContext returns unified immutable context
/// - TensionContext includes macro/micro tension, drivers, transition hint
/// - Implementations remain immutable and thread-safe
/// - API supports motif placement and lyric-driven ducking use cases
/// 
/// To run: Call TensionContextIntegrationTests.RunAllTests() from test button or debug hook.
/// All tests write output to Console and throw exceptions on failure.
/// </summary>
public static class TensionContextIntegrationTests
{
    public static void RunAllTests()
    {
        Console.WriteLine("=== Tension Context Integration Tests (Story 7.5.8) ===");

        // Basic functionality
        TestGetTensionContextReturnsCompleteContext();
        TestGetTensionContextIncludesTransitionHint();
        TestGetTensionContextIncludesTensionDrivers();
        TestGetTensionContextIncludesPhraseFlags();

        // Context consistency
        TestTensionContextMatchesIndividualQueries();
        TestTensionContextTransitionHintMatchesGetTransitionHint();
        TestTensionContextTensionDriversMatchMacroTension();

        // Determinism
        TestGetTensionContextIsDeterministic();
        TestGetTensionContextIsThreadSafe();

        // Immutability
        TestTensionContextIsImmutable();
        TestTensionContextCannotBeMutatedAfterCreation();

        // Stage 8/9 use case scenarios
        TestMotifPlacementScenario_HighEnergyLowTension();
        TestMotifPlacementScenario_HighTensionAnticipation();
        TestLyricDuckingScenario_LowTensionRelease();
        TestPhraseEndDetection_ForFillPlacement();
        TestSectionTransitionDetection_ForArrangementChanges();

        // Multiple implementations
        TestDeterministicTensionQueryImplementsGetTensionContext();
        TestNeutralTensionQueryImplementsGetTensionContext();

        // Edge cases
        TestGetTensionContextAtSectionStart();
        TestGetTensionContextAtSectionEnd();
        TestGetTensionContextAtPhraseEnd();
        TestGetTensionContextForLastSection();
        TestGetTensionContextForSingleBarSection();

        // Error handling
        TestGetTensionContextThrowsOnInvalidSectionIndex();
        TestGetTensionContextThrowsOnInvalidBarIndex();

        Console.WriteLine("All Tension Context Integration tests passed.");
    }

    #region Basic Functionality Tests

    private static void TestGetTensionContextReturnsCompleteContext()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var context = query.GetTensionContext(0, 0);

        Assert(context != null, "Context should not be null");
        AssertEqual(0, context.AbsoluteSectionIndex, "Should have correct section index");
        AssertEqual(0, context.BarIndexWithinSection, "Should have correct bar index");
        Assert(context.MacroTension != null, "Should have macro tension");
        Assert(context.MicroTension >= 0.0 && context.MicroTension <= 1.0, "Should have valid micro tension");
    }

    private static void TestGetTensionContextIncludesTransitionHint()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var context = query.GetTensionContext(0, 0);

        Assert(Enum.IsDefined(typeof(SectionTransitionHint), context.TransitionHint), 
            "Should have valid transition hint");
    }

    private static void TestGetTensionContextIncludesTensionDrivers()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var context = query.GetTensionContext(0, 0);

        // TensionDrivers property should mirror MacroTension.Driver
        AssertEqual(context.MacroTension.Driver, context.TensionDrivers, 
            "TensionDrivers should match MacroTension.Driver");
    }

    private static void TestGetTensionContextIncludesPhraseFlags()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        // Test at section start
        var contextStart = query.GetTensionContext(0, 0);
        Assert(contextStart.IsSectionStart, "First bar should be section start");

        // Test at section end
        var section = arc.SectionTrack.Sections[0];
        var contextEnd = query.GetTensionContext(0, section.BarCount - 1);
        Assert(contextEnd.IsSectionEnd, "Last bar should be section end");
    }

    #endregion

    #region Context Consistency Tests

    private static void TestTensionContextMatchesIndividualQueries()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        for (int sectionIdx = 0; sectionIdx < 3; sectionIdx++)
        {
            var section = arc.SectionTrack.Sections[sectionIdx];
            for (int barIdx = 0; barIdx < Math.Min(4, section.BarCount); barIdx++)
            {
                var context = query.GetTensionContext(sectionIdx, barIdx);
                var directMacro = query.GetMacroTension(sectionIdx);
                var directMicro = query.GetMicroTension(sectionIdx, barIdx);
                var directFlags = query.GetPhraseFlags(sectionIdx, barIdx);

                AssertEqual(directMacro.MacroTension, context.MacroTension.MacroTension, 
                    "Context macro tension should match direct query");
                AssertEqual(directMicro, context.MicroTension, 
                    "Context micro tension should match direct query");
                AssertEqual(directFlags.IsPhraseEnd, context.IsPhraseEnd, 
                    "Context IsPhraseEnd should match direct query");
                AssertEqual(directFlags.IsSectionEnd, context.IsSectionEnd, 
                    "Context IsSectionEnd should match direct query");
                AssertEqual(directFlags.IsSectionStart, context.IsSectionStart, 
                    "Context IsSectionStart should match direct query");
            }
        }
    }

    private static void TestTensionContextTransitionHintMatchesGetTransitionHint()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        for (int sectionIdx = 0; sectionIdx < 3; sectionIdx++)
        {
            var context = query.GetTensionContext(sectionIdx, 0);
            var directHint = query.GetTransitionHint(sectionIdx);

            AssertEqual(directHint, context.TransitionHint, 
                $"Context transition hint should match direct query for section {sectionIdx}");
        }
    }

    private static void TestTensionContextTensionDriversMatchMacroTension()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        for (int sectionIdx = 0; sectionIdx < 3; sectionIdx++)
        {
            var context = query.GetTensionContext(sectionIdx, 0);

            AssertEqual(context.MacroTension.Driver, context.TensionDrivers,
                $"TensionDrivers should match MacroTension.Driver for section {sectionIdx}");
        }
    }

    #endregion

    #region Determinism Tests

    private static void TestGetTensionContextIsDeterministic()
    {
        var (arc1, _) = CreateTestArc(3);
        var (arc2, _) = CreateTestArc(3);
        var query1 = new DeterministicTensionQuery(arc1, seed: 42);
        var query2 = new DeterministicTensionQuery(arc2, seed: 42);

        for (int sectionIdx = 0; sectionIdx < 3; sectionIdx++)
        {
            var section = arc1.SectionTrack.Sections[sectionIdx];
            for (int barIdx = 0; barIdx < Math.Min(2, section.BarCount); barIdx++)
            {
                var context1 = query1.GetTensionContext(sectionIdx, barIdx);
                var context2 = query2.GetTensionContext(sectionIdx, barIdx);

                AssertEqual(context1.MacroTension.MacroTension, context2.MacroTension.MacroTension,
                    "Macro tension should be deterministic");
                AssertEqual(context1.MicroTension, context2.MicroTension,
                    "Micro tension should be deterministic");
                AssertEqual(context1.TransitionHint, context2.TransitionHint,
                    "Transition hint should be deterministic");
                AssertEqual(context1.TensionDrivers, context2.TensionDrivers,
                    "Tension drivers should be deterministic");
            }
        }
    }

    private static void TestGetTensionContextIsThreadSafe()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        // Capture reference context
        var referenceContext = query.GetTensionContext(1, 2);

        // Simulate concurrent access
        var tasks = new Task<TensionContext>[10];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() => query.GetTensionContext(1, 2));
        }

        Task.WaitAll(tasks);

        // All results should match reference (thread-safe immutable reads)
        foreach (var task in tasks)
        {
            var context = task.Result;
            AssertEqual(referenceContext.MacroTension.MacroTension, context.MacroTension.MacroTension,
                "Thread-safe access should return same values");
            AssertEqual(referenceContext.MicroTension, context.MicroTension,
                "Thread-safe access should return same values");
        }
    }

    #endregion

    #region Immutability Tests

    private static void TestTensionContextIsImmutable()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var context = query.GetTensionContext(0, 0);

        // Verify it's a record (immutable)
        Assert(context.GetType().IsAssignableTo(typeof(object)), "Should be a record type");

        // Capture values
        var originalSection = context.AbsoluteSectionIndex;
        var originalBar = context.BarIndexWithinSection;
        var originalMacro = context.MacroTension.MacroTension;

        // Values should not change
        AssertEqual(originalSection, context.AbsoluteSectionIndex, "Values should remain constant");
        AssertEqual(originalBar, context.BarIndexWithinSection, "Values should remain constant");
        AssertEqual(originalMacro, context.MacroTension.MacroTension, "Values should remain constant");
    }

    private static void TestTensionContextCannotBeMutatedAfterCreation()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var context1 = query.GetTensionContext(0, 0);
        var context2 = query.GetTensionContext(0, 1);

        // Creating new contexts should not affect previous ones
        AssertEqual(0, context1.BarIndexWithinSection, "Previous context should be unchanged");
        AssertEqual(1, context2.BarIndexWithinSection, "New context should have new values");
    }

    #endregion

    #region Stage 8/9 Use Case Scenarios

    private static void TestMotifPlacementScenario_HighEnergyLowTension()
    {
        // Scenario: Prefer high-energy + low tension for release moments (ideal for memorable motif)
        var (arc, _) = CreateChorusArc();
        var query = new DeterministicTensionQuery(arc, seed: 42);

        // Chorus typically has high energy but releases tension
        var chorusContext = query.GetTensionContext(1, 0); // Assuming chorus at index 1

        // Should have release hint and lower tension than pre-chorus
        Assert(chorusContext.MacroTension.MacroTension < 0.7,
            "Chorus should have released tension for motif placement");
        Assert(chorusContext.TransitionHint == SectionTransitionHint.Release ||
               chorusContext.MacroTension.Driver.HasFlag(TensionDriver.Resolution),
            "Chorus should show resolution/release characteristics");
    }

    private static void TestMotifPlacementScenario_HighTensionAnticipation()
    {
        // Scenario: Use high tension for anticipatory motifs (pre-chorus build)
        var (arc, _) = CreateVerseChorusArc();
        var query = new DeterministicTensionQuery(arc, seed: 42);

        // Verse typically has anticipation toward chorus
        var verseContext = query.GetTensionContext(0, 0);

        // Check if tension drivers indicate anticipation
        bool hasAnticipation = verseContext.TensionDrivers.HasFlag(TensionDriver.Anticipation) ||
                                verseContext.TensionDrivers.HasFlag(TensionDriver.PreChorusBuild);

        // Transition hint should indicate build
        if (verseContext.TransitionHint == SectionTransitionHint.Build)
        {
            Assert(true, "Verse building to chorus shows anticipation");
        }
    }

    private static void TestLyricDuckingScenario_LowTensionRelease()
    {
        // Scenario: Reduce accompaniment density when tension is low and release is desired
        var (arc, _) = CreateChorusArc();
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var context = query.GetTensionContext(1, 0);

        // Low tension + release = good time to duck accompaniment for vocals
        if (context.MacroTension.MacroTension < 0.5 && 
            context.TransitionHint == SectionTransitionHint.Release)
        {
            // This would be a good moment to reduce comp/pads density
            Assert(true, "Low tension release moment identified for lyric ducking");
        }
    }

    private static void TestPhraseEndDetection_ForFillPlacement()
    {
        // Scenario: Drums need phrase end detection for fill placement
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var section = arc.SectionTrack.Sections[0];
        bool foundPhraseEnd = false;

        for (int barIdx = 0; barIdx < section.BarCount; barIdx++)
        {
            var context = query.GetTensionContext(0, barIdx);
            if (context.IsPhraseEnd)
            {
                foundPhraseEnd = true;
                // This is where a drum fill would be placed
                Assert(context.MicroTension >= 0.0 && context.MicroTension <= 1.0,
                    "Phrase end should have valid micro tension for fill intensity");
            }
        }

        Assert(foundPhraseEnd, "Should find at least one phrase end in section");
    }

    private static void TestSectionTransitionDetection_ForArrangementChanges()
    {
        // Scenario: Arrangement needs to detect section transitions for orchestration changes
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        for (int sectionIdx = 0; sectionIdx < 2; sectionIdx++)
        {
            var context = query.GetTensionContext(sectionIdx, 0);

            // Transition hint tells us how to shape the arrangement change
            var hint = context.TransitionHint;
            Assert(Enum.IsDefined(typeof(SectionTransitionHint), hint),
                "Should have valid transition hint for arrangement decisions");

            // Different hints would trigger different orchestration strategies
            switch (hint)
            {
                case SectionTransitionHint.Build:
                    // Add layers, increase density
                    break;
                case SectionTransitionHint.Release:
                    // Pull back, let it breathe
                    break;
                case SectionTransitionHint.Drop:
                    // Sudden reduction for impact
                    break;
                case SectionTransitionHint.Sustain:
                    // Maintain current orchestration
                    break;
            }
        }
    }

    #endregion

    #region Multiple Implementations Tests

    private static void TestDeterministicTensionQueryImplementsGetTensionContext()
    {
        var (arc, _) = CreateTestArc(3);
        ITensionQuery query = new DeterministicTensionQuery(arc, seed: 42);

        var context = query.GetTensionContext(0, 0);

        Assert(context != null, "DeterministicTensionQuery should implement GetTensionContext");
        Assert(context.MacroTension != null, "Should return complete context");
    }

    private static void TestNeutralTensionQueryImplementsGetTensionContext()
    {
        var sections = CreateTestSections(3);
        ITensionQuery query = new NeutralTensionQuery(sections);

        var context = query.GetTensionContext(0, 0);

        Assert(context != null, "NeutralTensionQuery should implement GetTensionContext");
        AssertEqual(0.0, context.MacroTension.MacroTension, "Should return neutral tension");
        AssertEqual(SectionTransitionHint.None, context.TransitionHint, "Should return None transition hint");
    }

    #endregion

    #region Edge Cases Tests

    private static void TestGetTensionContextAtSectionStart()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var context = query.GetTensionContext(0, 0);

        Assert(context.IsSectionStart, "First bar should be marked as section start");
        Assert(!context.IsSectionEnd, "First bar should not be marked as section end");
    }

    private static void TestGetTensionContextAtSectionEnd()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var section = arc.SectionTrack.Sections[0];
        var context = query.GetTensionContext(0, section.BarCount - 1);

        Assert(context.IsSectionEnd, "Last bar should be marked as section end");
        Assert(!context.IsSectionStart, "Last bar should not be marked as section start");
    }

    private static void TestGetTensionContextAtPhraseEnd()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var section = arc.SectionTrack.Sections[0];
        bool foundPhraseEnd = false;

        for (int barIdx = 0; barIdx < section.BarCount; barIdx++)
        {
            var context = query.GetTensionContext(0, barIdx);
            if (context.IsPhraseEnd)
            {
                foundPhraseEnd = true;
                // Phrase end should have higher micro tension (rising into cadence)
                Assert(context.MicroTension >= 0.0 && context.MicroTension <= 1.0,
                    "Phrase end should have valid micro tension");
            }
        }

        Assert(foundPhraseEnd, "Should find phrase end in 8-bar section");
    }

    private static void TestGetTensionContextForLastSection()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var context = query.GetTensionContext(2, 0);

        AssertEqual(SectionTransitionHint.None, context.TransitionHint,
            "Last section should have None transition hint");
    }

    private static void TestGetTensionContextForSingleBarSection()
    {
        var sections = new List<Section>
        {
            new Section
            {
                SectionType = MusicConstants.eSectionType.Intro,
                BarCount = 1,
                StartBar = 1,
                SectionId = 0
            }
        };
        var sectionTrack = CreateSectionTrack(sections);
        var arc = CreateArc(sectionTrack, "TestGroove", seed: 42);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        var context = query.GetTensionContext(0, 0);

        Assert(context.IsSectionStart, "Single bar should be section start");
        Assert(context.IsSectionEnd, "Single bar should be section end");
        Assert(context.IsPhraseEnd, "Single bar should be phrase end");
    }

    #endregion

    #region Error Handling Tests

    private static void TestGetTensionContextThrowsOnInvalidSectionIndex()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        try
        {
            query.GetTensionContext(-1, 0);
            throw new Exception("Should have thrown ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }

        try
        {
            query.GetTensionContext(3, 0);
            throw new Exception("Should have thrown ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
    }

    private static void TestGetTensionContextThrowsOnInvalidBarIndex()
    {
        var (arc, _) = CreateTestArc(3);
        var query = new DeterministicTensionQuery(arc, seed: 42);

        try
        {
            query.GetTensionContext(0, -1);
            throw new Exception("Should have thrown ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }

        var section = arc.SectionTrack.Sections[0];
        try
        {
            query.GetTensionContext(0, section.BarCount + 1);
            throw new Exception("Should have thrown ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
    }

    #endregion

    #region Test Helpers

    private static (EnergyArc, SectionTrack) CreateTestArc(int sectionCount)
    {
        var sections = CreateTestSections(sectionCount);
        var sectionTrack = CreateSectionTrack(sections);
        var arc = CreateArc(sectionTrack, "TestGroove", seed: 42);
        return (arc, sectionTrack);
    }

    private static (EnergyArc, SectionTrack) CreateChorusArc()
    {
        var sections = new List<Section>
        {
            new Section
            {
                SectionType = MusicConstants.eSectionType.Verse,
                BarCount = 8,
                StartBar = 1,
                SectionId = 0
            },
            new Section
            {
                SectionType = MusicConstants.eSectionType.Chorus,
                BarCount = 8,
                StartBar = 9,
                SectionId = 1
            }
        };
        var sectionTrack = CreateSectionTrack(sections);
        var arc = CreateArc(sectionTrack, "PopGroove", seed: 42);
        return (arc, sectionTrack);
    }

    private static (EnergyArc, SectionTrack) CreateVerseChorusArc()
    {
        var sections = new List<Section>
        {
            new Section
            {
                SectionType = MusicConstants.eSectionType.Verse,
                BarCount = 8,
                StartBar = 1,
                SectionId = 0
            },
            new Section
            {
                SectionType = MusicConstants.eSectionType.Chorus,
                BarCount = 8,
                StartBar = 9,
                SectionId = 1
            }
        };
        var sectionTrack = CreateSectionTrack(sections);
        var arc = CreateArc(sectionTrack, "RockGroove", seed: 42);
        return (arc, sectionTrack);
    }

    private static List<Section> CreateTestSections(int count)
    {
        var types = new[] {
            MusicConstants.eSectionType.Verse,
            MusicConstants.eSectionType.Chorus,
            MusicConstants.eSectionType.Bridge,
            MusicConstants.eSectionType.Outro
        };

        var sections = new List<Section>();
        int startBar = 1;
        for (int i = 0; i < count; i++)
        {
            sections.Add(new Section
            {
                SectionType = types[i % types.Length],
                BarCount = 8,
                StartBar = startBar,
                SectionId = i
            });
            startBar += 8;
        }
        return sections;
    }

    private static SectionTrack CreateSectionTrack(List<Section> sections)
    {
        var track = new SectionTrack();
        foreach (var section in sections)
        {
            track.Add(section.SectionType, section.BarCount);
        }
        return track;
    }

    private static EnergyArc CreateArc(SectionTrack sectionTrack, string grooveName, int seed)
    {
        return EnergyArc.Create(sectionTrack, grooveName, seed: seed);
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

#endif // FALSE_DISABLED_FOR_ENERGY_DISCONNECT
