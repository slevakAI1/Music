// AI: purpose=Tests for Story 7.5.3 micro tension map derivation with phrase-aware and fallback modes.
// AI: coverage=MicroTensionMap.Build determinism, range constraints, rising tension, phrase segmentation.
// AI: validation=Deterministic by seed, monotonic-ish rise, correct map length, phrase flags.

namespace Music.Generator;

/// <summary>
/// Tests for Story 7.5.3: Derive micro tension map per section.
/// Verifies acceptance criteria:
/// - Deterministic MicroTensionMap per section keyed by bar index
/// - Fallback mode (infer phrase segmentation deterministically)
/// - Rising micro tension toward phrase ends
/// - Per-bar micro tension value + flags (IsPhraseEnd, IsSectionEnd, IsSectionStart)
/// - Correct map length, determinism by seed, monotonic-ish rise into cadence
/// 
/// To run: Call MicroTensionMapTests.RunAllTests() from test button or debug hook.
/// All tests write output to Console and throw exceptions on failure.
/// </summary>
public static class MicroTensionMapTests
{
    public static void RunAllTests()
    {
        Console.WriteLine("=== Micro Tension Map Tests (Story 7.5.3) ===");

        // Basic functionality
        TestBuildCreatesValidMap();
        TestBuildHandlesEdgeCases();
        TestBuildClampsInputs();

        // Determinism
        TestBuildIsDeterministicBySeed();
        TestBuildProducesDifferentOutputsForDifferentSeeds();

        // Fallback mode
        TestFallbackModeInfersPhraseLength();
        TestFallbackModeHandles4BarSections();
        TestFallbackModeHandlesIrregularSections();

        // Rising tension shape
        TestTensionRisesWithinPhrase();
        TestTensionShapeInfluencedByMacro();
        TestTensionShapeInfluencedByMicroDefault();

        // Flags
        TestPhraseEndFlagsSetCorrectly();
        TestSectionBoundaryFlagsSetCorrectly();
        TestFlagsForIrregularPhraseLengths();

        // Map length and range
        TestMapLengthMatchesBarCount();
        TestAllTensionValuesInValidRange();

        // Jitter
        TestJitterIsAppliedWhenSeedNonZero();
        TestNoJitterWhenSeedIsZero();

        // Integration with macro tension
        TestMicroTensionReflectsMacroTension();
        TestHighMacroTensionProducesHigherMicroBaseline();

        Console.WriteLine("All Micro Tension Map tests passed.");
    }

    #region Basic Functionality Tests

    private static void TestBuildCreatesValidMap()
    {
        var map = MicroTensionMap.Build(8, macroTension: 0.5, microDefault: 0.4, phraseLength: 4, seed: 42);

        AssertEqual(8, map.BarCount, "Map should have 8 bars");
        Assert(map.TensionByBar.Count == 8, "TensionByBar should have 8 values");
        Assert(map.IsPhraseEnd.Count == 8, "IsPhraseEnd should have 8 values");
        Assert(map.IsSectionEnd.Count == 8, "IsSectionEnd should have 8 values");
        Assert(map.IsSectionStart.Count == 8, "IsSectionStart should have 8 values");
    }

    private static void TestBuildHandlesEdgeCases()
    {
        // Single bar section
        var map1 = MicroTensionMap.Build(1, 0.5, 0.4, null, 42);
        AssertEqual(1, map1.BarCount, "Should handle 1-bar section");
        Assert(map1.IsSectionStart[0], "First bar should be section start");
        Assert(map1.IsSectionEnd[0], "First bar should be section end");
        Assert(map1.IsPhraseEnd[0], "First bar should be phrase end");

        // Two bar section
        var map2 = MicroTensionMap.Build(2, 0.5, 0.4, null, 42);
        AssertEqual(2, map2.BarCount, "Should handle 2-bar section");
    }

    private static void TestBuildClampsInputs()
    {
        // Macro tension > 1.0
        var map1 = MicroTensionMap.Build(4, macroTension: 1.5, microDefault: 0.5, null, 42);
        Assert(map1.TensionByBar.All(t => t <= 1.0), "Should clamp macro tension to 1.0");

        // Macro tension < 0.0
        var map2 = MicroTensionMap.Build(4, macroTension: -0.5, microDefault: 0.5, null, 42);
        Assert(map2.TensionByBar.All(t => t >= 0.0), "Should clamp macro tension to 0.0");

        // MicroDefault > 1.0
        var map3 = MicroTensionMap.Build(4, macroTension: 0.5, microDefault: 1.5, null, 42);
        Assert(map3.TensionByBar.All(t => t <= 1.0), "Should clamp micro default to 1.0");

        // MicroDefault < 0.0
        var map4 = MicroTensionMap.Build(4, macroTension: 0.5, microDefault: -0.5, null, 42);
        Assert(map4.TensionByBar.All(t => t >= 0.0), "Should clamp micro default to 0.0");
    }

    #endregion

    #region Determinism Tests

    private static void TestBuildIsDeterministicBySeed()
    {
        var map1 = MicroTensionMap.Build(8, 0.6, 0.5, 4, seed: 100);
        var map2 = MicroTensionMap.Build(8, 0.6, 0.5, 4, seed: 100);

        for (int i = 0; i < 8; i++)
        {
            AssertEqual(map1.TensionByBar[i], map2.TensionByBar[i],
                $"Bar {i} tension should be deterministic");
            AssertEqual(map1.IsPhraseEnd[i], map2.IsPhraseEnd[i],
                $"Bar {i} phrase end flag should be deterministic");
        }
    }

    private static void TestBuildProducesDifferentOutputsForDifferentSeeds()
    {
        var map1 = MicroTensionMap.Build(8, 0.6, 0.5, 4, seed: 100);
        var map2 = MicroTensionMap.Build(8, 0.6, 0.5, 4, seed: 200);

        bool foundDifference = false;
        for (int i = 0; i < 8; i++)
        {
            if (Math.Abs(map1.TensionByBar[i] - map2.TensionByBar[i]) > 0.001)
            {
                foundDifference = true;
                break;
            }
        }

        Assert(foundDifference, "Different seeds should produce different tension values");
    }

    #endregion

    #region Fallback Mode Tests

    private static void TestFallbackModeInfersPhraseLength()
    {
        // 8-bar section should default to 4-bar phrases
        var map8 = MicroTensionMap.Build(8, 0.5, 0.4, phraseLength: null, seed: 42);
        int phraseCount8 = map8.IsPhraseEnd.Count(f => f);
        AssertEqual(2, phraseCount8, "8-bar section should have 2 phrases (4-bar each)");

        // 16-bar section should default to 4-bar phrases
        var map16 = MicroTensionMap.Build(16, 0.5, 0.4, phraseLength: null, seed: 42);
        int phraseCount16 = map16.IsPhraseEnd.Count(f => f);
        AssertEqual(4, phraseCount16, "16-bar section should have 4 phrases (4-bar each)");
    }

    private static void TestFallbackModeHandles4BarSections()
    {
        // 4-bar section should default to 2-bar phrases
        var map4 = MicroTensionMap.Build(4, 0.5, 0.4, phraseLength: null, seed: 42);
        int phraseCount4 = map4.IsPhraseEnd.Count(f => f);
        AssertEqual(2, phraseCount4, "4-bar section should have 2 phrases (2-bar each)");
    }

    private static void TestFallbackModeHandlesIrregularSections()
    {
        // 6-bar section with fallback (4-bar phrase default)
        var map6 = MicroTensionMap.Build(6, 0.5, 0.4, phraseLength: null, seed: 42);
        Assert(map6.IsPhraseEnd[3], "Bar 3 should be phrase end (first 4-bar phrase)");
        Assert(map6.IsPhraseEnd[5], "Bar 5 should be phrase end (last bar)");
        AssertEqual(2, map6.IsPhraseEnd.Count(f => f), "6-bar section should have 2 phrase ends");

        // 5-bar section
        var map5 = MicroTensionMap.Build(5, 0.5, 0.4, phraseLength: null, seed: 42);
        Assert(map5.IsPhraseEnd[3], "Bar 3 should be phrase end");
        Assert(map5.IsPhraseEnd[4], "Bar 4 (last) should be phrase end");
    }

    #endregion

    #region Rising Tension Shape Tests

    private static void TestTensionRisesWithinPhrase()
    {
        var map = MicroTensionMap.Build(8, 0.5, 0.4, phraseLength: 4, seed: 0);

        // First phrase (bars 0-3)
        Assert(map.TensionByBar[0] <= map.TensionByBar[3],
            "Tension should rise or stay flat from phrase start to end (phrase 1)");

        // Second phrase (bars 4-7)
        Assert(map.TensionByBar[4] <= map.TensionByBar[7],
            "Tension should rise or stay flat from phrase start to end (phrase 2)");

        // Check monotonic-ish rise within first phrase
        for (int i = 1; i < 4; i++)
        {
            Assert(map.TensionByBar[i] >= map.TensionByBar[i - 1] - 0.05,
                $"Tension should generally rise within phrase (bar {i - 1} to {i})");
        }
    }

    private static void TestTensionShapeInfluencedByMacro()
    {
        var mapLow = MicroTensionMap.Build(8, macroTension: 0.2, microDefault: 0.5, 4, seed: 0);
        var mapHigh = MicroTensionMap.Build(8, macroTension: 0.8, microDefault: 0.5, 4, seed: 0);

        double avgLow = mapLow.TensionByBar.Average();
        double avgHigh = mapHigh.TensionByBar.Average();

        Assert(avgHigh > avgLow,
            $"Higher macro tension should produce higher average micro tension (low: {avgLow:F2}, high: {avgHigh:F2})");
    }

    private static void TestTensionShapeInfluencedByMicroDefault()
    {
        var mapLow = MicroTensionMap.Build(8, macroTension: 0.5, microDefault: 0.2, 4, seed: 0);
        var mapHigh = MicroTensionMap.Build(8, macroTension: 0.5, microDefault: 0.8, 4, seed: 0);

        double avgLow = mapLow.TensionByBar.Average();
        double avgHigh = mapHigh.TensionByBar.Average();

        Assert(avgHigh > avgLow,
            $"Higher micro default should produce higher average tension (low: {avgLow:F2}, high: {avgHigh:F2})");
    }

    #endregion

    #region Flag Tests

    private static void TestPhraseEndFlagsSetCorrectly()
    {
        var map = MicroTensionMap.Build(8, 0.5, 0.4, phraseLength: 4, seed: 42);

        Assert(map.IsPhraseEnd[3], "Bar 3 should be phrase end (first phrase)");
        Assert(map.IsPhraseEnd[7], "Bar 7 should be phrase end (second phrase)");
        Assert(!map.IsPhraseEnd[0], "Bar 0 should not be phrase end");
        Assert(!map.IsPhraseEnd[4], "Bar 4 should not be phrase end (phrase start)");

        int phraseEndCount = map.IsPhraseEnd.Count(f => f);
        AssertEqual(2, phraseEndCount, "Should have exactly 2 phrase ends");
    }

    private static void TestSectionBoundaryFlagsSetCorrectly()
    {
        var map = MicroTensionMap.Build(8, 0.5, 0.4, phraseLength: 4, seed: 42);

        Assert(map.IsSectionStart[0], "First bar should be section start");
        Assert(!map.IsSectionStart[1], "Bar 1 should not be section start");
        Assert(map.IsSectionEnd[7], "Last bar should be section end");
        Assert(!map.IsSectionEnd[6], "Bar 6 should not be section end");

        AssertEqual(1, map.IsSectionStart.Count(f => f), "Should have exactly 1 section start");
        AssertEqual(1, map.IsSectionEnd.Count(f => f), "Should have exactly 1 section end");
    }

    private static void TestFlagsForIrregularPhraseLengths()
    {
        // 6-bar section with 4-bar phrase length
        var map = MicroTensionMap.Build(6, 0.5, 0.4, phraseLength: 4, seed: 42);

        Assert(map.IsPhraseEnd[3], "Bar 3 should be phrase end (4-bar phrase)");
        Assert(map.IsPhraseEnd[5], "Bar 5 (last) should be phrase end (remainder)");
        AssertEqual(2, map.IsPhraseEnd.Count(f => f), "Should have 2 phrase ends");
    }

    #endregion

    #region Map Length and Range Tests

    private static void TestMapLengthMatchesBarCount()
    {
        for (int barCount = 1; barCount <= 16; barCount++)
        {
            var map = MicroTensionMap.Build(barCount, 0.5, 0.4, null, 42);
            AssertEqual(barCount, map.BarCount, $"Map should have {barCount} bars");
            AssertEqual(barCount, map.TensionByBar.Count, $"TensionByBar should have {barCount} values");
        }
    }

    private static void TestAllTensionValuesInValidRange()
    {
        var map = MicroTensionMap.Build(16, 0.7, 0.6, 4, seed: 42);

        for (int i = 0; i < map.BarCount; i++)
        {
            double tension = map.TensionByBar[i];
            Assert(tension >= 0.0 && tension <= 1.0,
                $"Bar {i} tension {tension} out of range [0..1]");
        }
    }

    #endregion

    #region Jitter Tests

    private static void TestJitterIsAppliedWhenSeedNonZero()
    {
        var mapWithJitter = MicroTensionMap.Build(8, 0.5, 0.5, 4, seed: 42);
        var mapNoJitter = MicroTensionMap.Build(8, 0.5, 0.5, 4, seed: 0);

        bool foundDifference = false;
        for (int i = 0; i < 8; i++)
        {
            if (Math.Abs(mapWithJitter.TensionByBar[i] - mapNoJitter.TensionByBar[i]) > 0.001)
            {
                foundDifference = true;
                break;
            }
        }

        Assert(foundDifference, "Seed != 0 should apply jitter producing different values than seed = 0");
    }

    private static void TestNoJitterWhenSeedIsZero()
    {
        var map1 = MicroTensionMap.Build(8, 0.5, 0.5, 4, seed: 0);
        var map2 = MicroTensionMap.Build(8, 0.5, 0.5, 4, seed: 0);

        for (int i = 0; i < 8; i++)
        {
            AssertEqual(map1.TensionByBar[i], map2.TensionByBar[i],
                $"Bar {i} tension should be identical when seed = 0 (no jitter)");
        }
    }

    #endregion

    #region Integration Tests

    private static void TestMicroTensionReflectsMacroTension()
    {
        var mapLow = MicroTensionMap.Build(8, macroTension: 0.3, microDefault: 0.5, 4, seed: 0);
        var mapMid = MicroTensionMap.Build(8, macroTension: 0.5, microDefault: 0.5, 4, seed: 0);
        var mapHigh = MicroTensionMap.Build(8, macroTension: 0.7, microDefault: 0.5, 4, seed: 0);

        double avgLow = mapLow.TensionByBar.Average();
        double avgMid = mapMid.TensionByBar.Average();
        double avgHigh = mapHigh.TensionByBar.Average();

        Assert(avgLow < avgMid && avgMid < avgHigh,
            $"Average micro tension should increase with macro tension (low: {avgLow:F2}, mid: {avgMid:F2}, high: {avgHigh:F2})");
    }

    private static void TestHighMacroTensionProducesHigherMicroBaseline()
    {
        var map = MicroTensionMap.Build(8, macroTension: 0.9, microDefault: 0.3, 4, seed: 0);

        // With high macro tension, even early phrase bars should be elevated
        Assert(map.TensionByBar[0] > 0.3,
            $"High macro tension should elevate micro baseline (bar 0: {map.TensionByBar[0]:F2})");
    }

    #endregion

    #region Helper Methods

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
