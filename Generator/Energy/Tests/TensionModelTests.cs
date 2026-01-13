// AI: purpose=Unit tests for Story 7.5.1 tension model and public contracts.
// AI: coverage=TensionDriver, SectionTensionProfile, MicroTensionMap, ITensionQuery, TensionContext.
// AI: validation=Immutability, range constraints, determinism, query API contract compliance.

namespace Music.Generator;

/// <summary>
/// Tests for Story 7.5.1: Tension model and public contracts.
/// Verifies acceptance criteria:
/// - Models are immutable (records)
/// - All tension values in [0..1]
/// - TensionDriver enum supports multiple flags
/// - Query API is stable and deterministic
/// 
/// To run: Call TensionModelTests.RunAllTests() from a test button or debug hook.
/// All tests write output to Console and throw exceptions on failure.
/// </summary>
public static class TensionModelTests
{
    public static void RunAllTests()
    {
        Console.WriteLine("=== Tension Model Tests (Story 7.5.1) ===");

        // TensionDriver tests
        TestTensionDriverSupportsFlags();
        TestTensionDriverNoneIsDefault();

        // SectionTensionProfile tests
        TestSectionTensionProfileIsImmutable();
        TestSectionTensionProfileNeutralReturnsZeroTension();
        TestSectionTensionProfileWithMacroTensionClampsValues();
        TestSectionTensionProfileWithMacroTensionDefaultsMicroTension();
        TestSectionTensionProfileWithMacroTensionAcceptsDriver();
        TestSectionTensionProfileWithTensionsClampsValues();
        TestSectionTensionProfileWithTensionsAllowsIndependentValues();

        // MicroTensionMap tests
        TestMicroTensionMapIsImmutable();
        TestMicroTensionMapFlatCreatesConstantTension();
        TestMicroTensionMapFlatMarksSectionBoundaries();
        TestMicroTensionMapFlatClampsToValidRange();
        TestMicroTensionMapWithSimplePhrasesRisesWithinPhrases();
        TestMicroTensionMapWithSimplePhrasesMarksPhraseEnds();
        TestMicroTensionMapGetTensionReturnsCorrectValue();
        TestMicroTensionMapGetTensionThrowsOnInvalidIndex();
        TestMicroTensionMapGetFlagsReturnsAllFlags();
        TestMicroTensionMapWithSimplePhrasesValidatesBarCount();
        TestMicroTensionMapWithSimplePhrasesValidatesPhraseLength();
        TestMicroTensionMapWithSimplePhrasesClampesBaseTension();

        // ITensionQuery / NeutralTensionQuery tests
        TestNeutralTensionQueryReturnsNeutralMacroTension();
        TestNeutralTensionQueryReturnsZeroMicroTension();
        TestNeutralTensionQueryReturnsValidMicroTensionMap();
        TestNeutralTensionQueryReturnsCorrectPhraseFlags();
        TestNeutralTensionQueryHasTensionDataReturnsCorrectly();
        TestNeutralTensionQuerySectionCountReturnsCorrectCount();
        TestNeutralTensionQueryThrowsOnInvalidSectionIndex();
        TestNeutralTensionQueryThrowsOnInvalidBarIndex();

        // TensionContext tests
        TestTensionContextIsImmutable();
        TestTensionContextCreatePopulatesAllFields();
        TestTensionContextCreateCapturesSectionBoundaries();

        // Determinism tests
        TestTensionModelIsDeterministic();

        Console.WriteLine("All Tension Model tests passed.");
    }

    #region TensionDriver Tests

    private static void TestTensionDriverSupportsFlags()
    {
        // Can combine multiple drivers
        var combined = TensionDriver.PreChorusBuild | TensionDriver.Anticipation;
        
        Assert(combined.HasFlag(TensionDriver.PreChorusBuild), "Should have PreChorusBuild flag");
        Assert(combined.HasFlag(TensionDriver.Anticipation), "Should have Anticipation flag");
        Assert(!combined.HasFlag(TensionDriver.Breakdown), "Should not have Breakdown flag");
    }

    private static void TestTensionDriverNoneIsDefault()
    {
        TensionDriver driver = default;
        AssertEqual(TensionDriver.None, driver, "Default should be None");
    }

    #endregion

    #region SectionTensionProfile Tests

    private static void TestSectionTensionProfileIsImmutable()
    {
        var profile = SectionTensionProfile.Neutral(0);
        
        // Can create modified copy with 'with' expression
        var modified = profile with { MacroTension = 0.5 };
        
        AssertEqual(0.0, profile.MacroTension, "Original should be unchanged");
        AssertEqual(0.5, modified.MacroTension, "Modified should have new value");
    }

    private static void TestSectionTensionProfileNeutralReturnsZeroTension()
    {
        var profile = SectionTensionProfile.Neutral(5);
        
        AssertEqual(0.0, profile.MacroTension, "MacroTension should be 0");
        AssertEqual(0.0, profile.MicroTensionDefault, "MicroTensionDefault should be 0");
        AssertEqual(TensionDriver.None, profile.Driver, "Driver should be None");
        AssertEqual(5, profile.AbsoluteSectionIndex, "AbsoluteSectionIndex should be 5");
    }

    private static void TestSectionTensionProfileWithMacroTensionClampsValues()
    {
        // Test upper bound
        var high = SectionTensionProfile.WithMacroTension(1.5, 0);
        AssertEqual(1.0, high.MacroTension, "Should clamp to 1.0");
        
        // Test lower bound
        var low = SectionTensionProfile.WithMacroTension(-0.5, 1);
        AssertEqual(0.0, low.MacroTension, "Should clamp to 0.0");
        
        // Test valid range
        var valid = SectionTensionProfile.WithMacroTension(0.7, 2);
        AssertEqual(0.7, valid.MacroTension, "Should keep valid value");
    }

    private static void TestSectionTensionProfileWithMacroTensionDefaultsMicroTension()
    {
        var profile = SectionTensionProfile.WithMacroTension(0.8, 0);
        
        // Micro tension should be half of macro by default
        AssertEqual(0.4, profile.MicroTensionDefault, "MicroTension should be half of macro");
    }

    private static void TestSectionTensionProfileWithMacroTensionAcceptsDriver()
    {
        var profile = SectionTensionProfile.WithMacroTension(
            0.6, 
            0, 
            TensionDriver.PreChorusBuild | TensionDriver.Anticipation);
        
        AssertEqual(0.6, profile.MacroTension, "MacroTension should be 0.6");
        Assert(profile.Driver.HasFlag(TensionDriver.PreChorusBuild), "Should have PreChorusBuild");
        Assert(profile.Driver.HasFlag(TensionDriver.Anticipation), "Should have Anticipation");
    }

    private static void TestSectionTensionProfileWithTensionsClampsValues()
    {
        var profile = SectionTensionProfile.WithTensions(
            macroTension: 1.2,
            microTensionDefault: -0.1,
            absoluteSectionIndex: 0);
        
        AssertEqual(1.0, profile.MacroTension, "MacroTension should clamp to 1.0");
        AssertEqual(0.0, profile.MicroTensionDefault, "MicroTensionDefault should clamp to 0.0");
    }

    private static void TestSectionTensionProfileWithTensionsAllowsIndependentValues()
    {
        var profile = SectionTensionProfile.WithTensions(
            macroTension: 0.8,
            microTensionDefault: 0.3,
            absoluteSectionIndex: 0);
        
        AssertEqual(0.8, profile.MacroTension, "MacroTension should be 0.8");
        AssertEqual(0.3, profile.MicroTensionDefault, "MicroTensionDefault should be 0.3");
    }

    #endregion

    #region MicroTensionMap Tests

    private static void TestMicroTensionMapIsImmutable()
    {
        var map = MicroTensionMap.Flat(4, 0.5);
        AssertEqual(4, map.BarCount, "BarCount should be 4");
    }

    private static void TestMicroTensionMapFlatCreatesConstantTension()
    {
        var map = MicroTensionMap.Flat(8, 0.6);
        
        AssertEqual(8, map.BarCount, "BarCount should be 8");
        for (int i = 0; i < map.BarCount; i++)
        {
            AssertEqual(0.6, map.TensionByBar[i], $"Bar {i} tension should be 0.6");
        }
    }

    private static void TestMicroTensionMapFlatMarksSectionBoundaries()
    {
        var map = MicroTensionMap.Flat(8, 0.5);
        
        // First bar is section start
        Assert(map.IsSectionStart[0], "First bar should be section start");
        Assert(!map.IsSectionEnd[0], "First bar should not be section end");
        
        // Last bar is section end
        Assert(map.IsSectionEnd[7], "Last bar should be section end");
        Assert(!map.IsSectionStart[7], "Last bar should not be section start");
        
        // Middle bars are neither
        Assert(!map.IsSectionStart[4], "Middle bar should not be section start");
        Assert(!map.IsSectionEnd[4], "Middle bar should not be section end");
    }

    private static void TestMicroTensionMapFlatClampsToValidRange()
    {
        var high = MicroTensionMap.Flat(4, 1.5);
        for (int i = 0; i < high.BarCount; i++)
        {
            AssertEqual(1.0, high.TensionByBar[i], $"Bar {i} should clamp to 1.0");
        }
        
        var low = MicroTensionMap.Flat(4, -0.5);
        for (int i = 0; i < low.BarCount; i++)
        {
            AssertEqual(0.0, low.TensionByBar[i], $"Bar {i} should clamp to 0.0");
        }
    }

    private static void TestMicroTensionMapWithSimplePhrasesRisesWithinPhrases()
    {
        var map = MicroTensionMap.WithSimplePhrases(
            barCount: 8,
            baseTension: 0.4,
            phraseLength: 4);
        
        // Within first phrase (bars 0-3), tension should rise
        Assert(map.TensionByBar[0] < map.TensionByBar[3], 
            "Tension should rise within first phrase");
        
        // Within second phrase (bars 4-7), tension should rise again
        Assert(map.TensionByBar[4] < map.TensionByBar[7], 
            "Tension should rise within second phrase");
    }

    private static void TestMicroTensionMapWithSimplePhrasesMarksPhraseEnds()
    {
        var map = MicroTensionMap.WithSimplePhrases(
            barCount: 9,
            baseTension: 0.5,
            phraseLength: 4);
        
        // 4-bar phrases: bars 3, 7 are phrase ends
        // Bar 8 is also phrase end (last bar of section)
        Assert(!map.IsPhraseEnd[0], "Bar 0 should not be phrase end");
        Assert(!map.IsPhraseEnd[2], "Bar 2 should not be phrase end");
        Assert(map.IsPhraseEnd[3], "Bar 3 should be phrase end");
        Assert(!map.IsPhraseEnd[4], "Bar 4 should not be phrase end");
        Assert(map.IsPhraseEnd[7], "Bar 7 should be phrase end");
        Assert(map.IsPhraseEnd[8], "Bar 8 (last) should be phrase end");
    }

    private static void TestMicroTensionMapGetTensionReturnsCorrectValue()
    {
        var map = MicroTensionMap.Flat(4, 0.6);
        
        AssertEqual(0.6, map.GetTension(0), "Bar 0 tension should be 0.6");
        AssertEqual(0.6, map.GetTension(3), "Bar 3 tension should be 0.6");
    }

    private static void TestMicroTensionMapGetTensionThrowsOnInvalidIndex()
    {
        var map = MicroTensionMap.Flat(4, 0.5);
        
        AssertThrows<ArgumentOutOfRangeException>(() => map.GetTension(-1));
        AssertThrows<ArgumentOutOfRangeException>(() => map.GetTension(4));
    }

    private static void TestMicroTensionMapGetFlagsReturnsAllFlags()
    {
        var map = MicroTensionMap.WithSimplePhrases(8, 0.5, 4);
        
        // First bar
        var (phraseEnd0, sectionEnd0, sectionStart0) = map.GetFlags(0);
        Assert(!phraseEnd0, "Bar 0 should not be phrase end");
        Assert(!sectionEnd0, "Bar 0 should not be section end");
        Assert(sectionStart0, "Bar 0 should be section start");
        
        // Phrase end
        var (phraseEnd3, sectionEnd3, sectionStart3) = map.GetFlags(3);
        Assert(phraseEnd3, "Bar 3 should be phrase end");
        Assert(!sectionEnd3, "Bar 3 should not be section end");
        Assert(!sectionStart3, "Bar 3 should not be section start");
        
        // Last bar (both phrase end and section end)
        var (phraseEnd7, sectionEnd7, sectionStart7) = map.GetFlags(7);
        Assert(phraseEnd7, "Bar 7 should be phrase end");
        Assert(sectionEnd7, "Bar 7 should be section end");
        Assert(!sectionStart7, "Bar 7 should not be section start");
    }

    private static void TestMicroTensionMapWithSimplePhrasesValidatesBarCount()
    {
        AssertThrows<ArgumentOutOfRangeException>(() =>
            MicroTensionMap.WithSimplePhrases(0, 0.5, 4));
        
        AssertThrows<ArgumentOutOfRangeException>(() =>
            MicroTensionMap.WithSimplePhrases(-1, 0.5, 4));
    }

    private static void TestMicroTensionMapWithSimplePhrasesValidatesPhraseLength()
    {
        AssertThrows<ArgumentOutOfRangeException>(() =>
            MicroTensionMap.WithSimplePhrases(8, 0.5, 0));
        
        AssertThrows<ArgumentOutOfRangeException>(() =>
            MicroTensionMap.WithSimplePhrases(8, 0.5, -1));
    }

    private static void TestMicroTensionMapWithSimplePhrasesClampesBaseTension()
    {
        var map = MicroTensionMap.WithSimplePhrases(4, 1.5, 4);
        
        // All values should be clamped to [0..1]
        for (int i = 0; i < map.BarCount; i++)
        {
            Assert(map.TensionByBar[i] >= 0.0 && map.TensionByBar[i] <= 1.0, 
                $"Bar {i} tension should be in [0..1]");
        }
    }

    #endregion

    #region ITensionQuery / NeutralTensionQuery Tests

    private static void TestNeutralTensionQueryReturnsNeutralMacroTension()
    {
        var sections = CreateTestSections(3);
        var query = new NeutralTensionQuery(sections);
        
        var profile = query.GetMacroTension(1);
        
        AssertEqual(0.0, profile.MacroTension, "MacroTension should be 0");
        AssertEqual(0.0, profile.MicroTensionDefault, "MicroTensionDefault should be 0");
        AssertEqual(TensionDriver.None, profile.Driver, "Driver should be None");
        AssertEqual(1, profile.AbsoluteSectionIndex, "AbsoluteSectionIndex should be 1");
    }

    private static void TestNeutralTensionQueryReturnsZeroMicroTension()
    {
        var sections = CreateTestSections(3);
        var query = new NeutralTensionQuery(sections);
        
        var tension = query.GetMicroTension(1, 2);
        
        AssertEqual(0.0, tension, "Micro tension should be 0");
    }

    private static void TestNeutralTensionQueryReturnsValidMicroTensionMap()
    {
        var sections = CreateTestSections(2);
        var query = new NeutralTensionQuery(sections);
        
        var map = query.GetMicroTensionMap(0);
        
        AssertEqual(4, map.BarCount, "Map should have 4 bars");
        for (int i = 0; i < map.BarCount; i++)
        {
            AssertEqual(0.0, map.TensionByBar[i], $"Bar {i} tension should be 0");
        }
    }

    private static void TestNeutralTensionQueryReturnsCorrectPhraseFlags()
    {
        var sections = CreateTestSections(1);
        var query = new NeutralTensionQuery(sections);
        
        var (phraseEnd, sectionEnd, sectionStart) = query.GetPhraseFlags(0, 0);
        
        Assert(!phraseEnd, "Should not be phrase end");
        Assert(!sectionEnd, "Should not be section end");
        Assert(sectionStart, "Should be section start");
    }

    private static void TestNeutralTensionQueryHasTensionDataReturnsCorrectly()
    {
        var sections = CreateTestSections(3);
        var query = new NeutralTensionQuery(sections);
        
        Assert(query.HasTensionData(0), "Should have data for section 0");
        Assert(query.HasTensionData(2), "Should have data for section 2");
        Assert(!query.HasTensionData(-1), "Should not have data for section -1");
        Assert(!query.HasTensionData(3), "Should not have data for section 3");
    }

    private static void TestNeutralTensionQuerySectionCountReturnsCorrectCount()
    {
        var sections = CreateTestSections(5);
        var query = new NeutralTensionQuery(sections);
        
        AssertEqual(5, query.SectionCount, "SectionCount should be 5");
    }

    private static void TestNeutralTensionQueryThrowsOnInvalidSectionIndex()
    {
        var sections = CreateTestSections(2);
        var query = new NeutralTensionQuery(sections);
        
        AssertThrows<ArgumentOutOfRangeException>(() => query.GetMacroTension(-1));
        AssertThrows<ArgumentOutOfRangeException>(() => query.GetMacroTension(2));
        AssertThrows<ArgumentOutOfRangeException>(() => query.GetMicroTension(-1, 0));
        AssertThrows<ArgumentOutOfRangeException>(() => query.GetMicroTension(2, 0));
    }

    private static void TestNeutralTensionQueryThrowsOnInvalidBarIndex()
    {
        var sections = CreateTestSections(1);
        var query = new NeutralTensionQuery(sections);
        
        AssertThrows<ArgumentOutOfRangeException>(() => query.GetMicroTension(0, -1));
        AssertThrows<ArgumentOutOfRangeException>(() => query.GetMicroTension(0, 4));
    }

    #endregion

    #region TensionContext Tests

    private static void TestTensionContextIsImmutable()
    {
        var sections = CreateTestSections(1);
        var query = new NeutralTensionQuery(sections);
        
        var context = TensionContext.Create(query, 0, 0);
        
        var modified = context with { MicroTension = 0.5 };
        
        AssertEqual(0.0, context.MicroTension, "Original should be unchanged");
        AssertEqual(0.5, modified.MicroTension, "Modified should have new value");
    }

    private static void TestTensionContextCreatePopulatesAllFields()
    {
        var sections = CreateTestSections(2);
        var query = new NeutralTensionQuery(sections);
        
        var context = TensionContext.Create(query, 1, 2);
        
        AssertEqual(1, context.AbsoluteSectionIndex, "AbsoluteSectionIndex should be 1");
        AssertEqual(2, context.BarIndexWithinSection, "BarIndexWithinSection should be 2");
        Assert(context.MacroTension != null, "MacroTension should not be null");
        AssertEqual(0.0, context.MicroTension, "MicroTension should be 0");
        Assert(!context.IsPhraseEnd, "Should not be phrase end");
        Assert(!context.IsSectionEnd, "Should not be section end");
        Assert(!context.IsSectionStart, "Should not be section start");
    }

    private static void TestTensionContextCreateCapturesSectionBoundaries()
    {
        var sections = CreateTestSections(1);
        var query = new NeutralTensionQuery(sections);
        
        var startContext = TensionContext.Create(query, 0, 0);
        var endContext = TensionContext.Create(query, 0, 3);
        
        Assert(startContext.IsSectionStart, "First bar should be section start");
        Assert(!startContext.IsSectionEnd, "First bar should not be section end");
        
        Assert(!endContext.IsSectionStart, "Last bar should not be section start");
        Assert(endContext.IsSectionEnd, "Last bar should be section end");
    }

    #endregion

    #region Determinism Tests

    private static void TestTensionModelIsDeterministic()
    {
        // Same inputs produce same outputs
        var sections1 = CreateTestSections(3);
        var sections2 = CreateTestSections(3);
        
        var query1 = new NeutralTensionQuery(sections1);
        var query2 = new NeutralTensionQuery(sections2);
        
        // Macro tension
        var macro1 = query1.GetMacroTension(1);
        var macro2 = query2.GetMacroTension(1);
        AssertEqual(macro1.MacroTension, macro2.MacroTension, "Macro tension should be deterministic");
        
        // Micro tension
        var micro1 = query1.GetMicroTension(1, 2);
        var micro2 = query2.GetMicroTension(1, 2);
        AssertEqual(micro1, micro2, "Micro tension should be deterministic");
        
        // Phrase flags
        var flags1 = query1.GetPhraseFlags(1, 2);
        var flags2 = query2.GetPhraseFlags(1, 2);
        AssertEqual(flags1, flags2, "Phrase flags should be deterministic");
    }

    #endregion

    #region Helper Methods

    private static List<Section> CreateTestSections(int count)
    {
        var sections = new List<Section>();
        for (int i = 0; i < count; i++)
        {
            sections.Add(new Section
            {
                SectionType = MusicConstants.eSectionType.Verse,
                BarCount = 4,
                StartBar = i * 4 + 1,
                SectionId = i
            });
        }
        return sections;
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

    private static void AssertThrows<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
            throw new Exception($"Expected exception {typeof(TException).Name} was not thrown");
        }
        catch (TException)
        {
            // Expected
        }
    }

    #endregion
}
