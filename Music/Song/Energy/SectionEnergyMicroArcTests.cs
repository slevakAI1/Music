// AI: purpose=Story 7.8 tests: phrase-level energy micro-arcs with phrase position classification and subtle deltas.
// AI: invariants=All deltas [-0.10..+0.10]; positions deterministic; integrates with MicroTensionMap logic; seed affects jitter only.

namespace Music.Generator.Tests;

internal static class SectionEnergyMicroArcTests
{
    public static void RunAllTests()
    {
        Console.WriteLine("Running Story 7.8 SectionEnergyMicroArc tests...");

        Test_Flat_CreatesZeroDeltasAndMiddlePositions();
        Test_Build_CorrectBarCount();
        Test_Build_PhraseLengthInference();
        Test_Build_PhrasePositionClassification();
        Test_Build_EnergyDeltasWithinBounds();
        Test_Build_PeakHasPositiveDelta();
        Test_Build_CadenceHasNegativeDelta();
        Test_Build_StartHasZeroDelta();
        Test_Build_HigherEnergyProducesLargerDeltas();
        Test_Build_Determinism();
        Test_Build_SeedAffectsJitterOnly();
        Test_Build_4BarSection_Uses2BarPhrases();
        Test_Build_8BarSection_Uses4BarPhrases();
        Test_Build_GetEnergyDelta_ValidIndex();
        Test_Build_GetEnergyDelta_InvalidIndex();
        Test_Build_GetPhrasePosition_ValidIndex();
        Test_Build_GetPhrasePosition_InvalidIndex();
        Test_Integration_WithMicroTensionMap_SamePhraseLengthLogic();

        Console.WriteLine("? All Story 7.8 SectionEnergyMicroArc tests passed.");
    }

    private static void Test_Flat_CreatesZeroDeltasAndMiddlePositions()
    {
        var arc = SectionEnergyMicroArc.Flat(8);

        Assert(arc.BarCount == 8, "Flat: bar count");
        Assert(arc.EnergyDeltaByBar.All(d => d == 0.0), "Flat: all deltas zero");
        Assert(arc.PhrasePositionByBar.All(p => p == PhrasePosition.Middle), "Flat: all positions Middle");
    }

    private static void Test_Build_CorrectBarCount()
    {
        var arc = SectionEnergyMicroArc.Build(barCount: 12, sectionEnergy: 0.5);
        Assert(arc.BarCount == 12, "Build: bar count matches input");
        Assert(arc.EnergyDeltaByBar.Count == 12, "Build: delta list length");
        Assert(arc.PhrasePositionByBar.Count == 12, "Build: position list length");
    }

    private static void Test_Build_PhraseLengthInference()
    {
        // 4-bar section -> 2-bar phrases
        var arc4 = SectionEnergyMicroArc.Build(barCount: 4, sectionEnergy: 0.5);
        int cadenceCount4 = arc4.PhrasePositionByBar.Count(p => p == PhrasePosition.Cadence);
        Assert(cadenceCount4 == 2, "4-bar section: 2 cadences (2-bar phrases)");

        // 8-bar section -> 4-bar phrases
        var arc8 = SectionEnergyMicroArc.Build(barCount: 8, sectionEnergy: 0.5);
        int cadenceCount8 = arc8.PhrasePositionByBar.Count(p => p == PhrasePosition.Cadence);
        Assert(cadenceCount8 == 2, "8-bar section: 2 cadences (4-bar phrases)");

        // 16-bar section -> 4-bar phrases
        var arc16 = SectionEnergyMicroArc.Build(barCount: 16, sectionEnergy: 0.5);
        int cadenceCount16 = arc16.PhrasePositionByBar.Count(p => p == PhrasePosition.Cadence);
        Assert(cadenceCount16 == 4, "16-bar section: 4 cadences (4-bar phrases)");
    }

    private static void Test_Build_PhrasePositionClassification()
    {
        // 8-bar section with 4-bar phrases
        var arc = SectionEnergyMicroArc.Build(barCount: 8, sectionEnergy: 0.5);

        // Expected pattern: Start, Middle, Peak, Cadence, Start, Middle, Peak, Cadence
        Assert(arc.GetPhrasePosition(0) == PhrasePosition.Start, "Bar 0: Start");
        Assert(arc.GetPhrasePosition(1) == PhrasePosition.Middle, "Bar 1: Middle");
        Assert(arc.GetPhrasePosition(2) == PhrasePosition.Peak, "Bar 2: Peak");
        Assert(arc.GetPhrasePosition(3) == PhrasePosition.Cadence, "Bar 3: Cadence");
        Assert(arc.GetPhrasePosition(4) == PhrasePosition.Start, "Bar 4: Start (second phrase)");
        Assert(arc.GetPhrasePosition(5) == PhrasePosition.Middle, "Bar 5: Middle");
        Assert(arc.GetPhrasePosition(6) == PhrasePosition.Peak, "Bar 6: Peak");
        Assert(arc.GetPhrasePosition(7) == PhrasePosition.Cadence, "Bar 7: Cadence (section end)");
    }

    private static void Test_Build_EnergyDeltasWithinBounds()
    {
        var arc = SectionEnergyMicroArc.Build(barCount: 16, sectionEnergy: 1.0, seed: 42);

        foreach (var delta in arc.EnergyDeltaByBar)
        {
            Assert(delta >= -0.10 && delta <= 0.10, $"Delta {delta:F3} within [-0.10..+0.10]");
        }
    }

    private static void Test_Build_PeakHasPositiveDelta()
    {
        var arc = SectionEnergyMicroArc.Build(barCount: 8, sectionEnergy: 0.7, seed: 0);

        for (int i = 0; i < arc.BarCount; i++)
        {
            if (arc.GetPhrasePosition(i) == PhrasePosition.Peak)
            {
                Assert(arc.GetEnergyDelta(i) > 0.0, $"Bar {i} (Peak): positive delta");
            }
        }
    }

    private static void Test_Build_CadenceHasNegativeDelta()
    {
        var arc = SectionEnergyMicroArc.Build(barCount: 8, sectionEnergy: 0.7, seed: 0);

        for (int i = 0; i < arc.BarCount; i++)
        {
            if (arc.GetPhrasePosition(i) == PhrasePosition.Cadence)
            {
                Assert(arc.GetEnergyDelta(i) < 0.0, $"Bar {i} (Cadence): negative delta");
            }
        }
    }

    private static void Test_Build_StartHasZeroDelta()
    {
        var arc = SectionEnergyMicroArc.Build(barCount: 8, sectionEnergy: 0.7, seed: 0);

        for (int i = 0; i < arc.BarCount; i++)
        {
            if (arc.GetPhrasePosition(i) == PhrasePosition.Start)
            {
                Assert(Math.Abs(arc.GetEnergyDelta(i)) < 0.01, $"Bar {i} (Start): near-zero delta");
            }
        }
    }

    private static void Test_Build_HigherEnergyProducesLargerDeltas()
    {
        var arcLow = SectionEnergyMicroArc.Build(barCount: 8, sectionEnergy: 0.2, seed: 0);
        var arcHigh = SectionEnergyMicroArc.Build(barCount: 8, sectionEnergy: 0.9, seed: 0);

        // Find peak bars and compare deltas
        for (int i = 0; i < 8; i++)
        {
            if (arcLow.GetPhrasePosition(i) == PhrasePosition.Peak)
            {
                double deltaLow = arcLow.GetEnergyDelta(i);
                double deltaHigh = arcHigh.GetEnergyDelta(i);
                Assert(deltaHigh > deltaLow, $"Bar {i} (Peak): high energy delta > low energy delta");
            }
        }
    }

    private static void Test_Build_Determinism()
    {
        var arc1 = SectionEnergyMicroArc.Build(barCount: 12, sectionEnergy: 0.6, seed: 123);
        var arc2 = SectionEnergyMicroArc.Build(barCount: 12, sectionEnergy: 0.6, seed: 123);

        for (int i = 0; i < 12; i++)
        {
            Assert(arc1.GetEnergyDelta(i) == arc2.GetEnergyDelta(i), $"Bar {i}: deltas match");
            Assert(arc1.GetPhrasePosition(i) == arc2.GetPhrasePosition(i), $"Bar {i}: positions match");
        }
    }

    private static void Test_Build_SeedAffectsJitterOnly()
    {
        var arc1 = SectionEnergyMicroArc.Build(barCount: 8, sectionEnergy: 0.6, seed: 100);
        var arc2 = SectionEnergyMicroArc.Build(barCount: 8, sectionEnergy: 0.6, seed: 200);

        // Positions should be identical (seed doesn't affect classification)
        for (int i = 0; i < 8; i++)
        {
            Assert(arc1.GetPhrasePosition(i) == arc2.GetPhrasePosition(i), $"Bar {i}: positions identical across seeds");
        }

        // Deltas should differ slightly (jitter)
        bool foundDifference = false;
        for (int i = 0; i < 8; i++)
        {
            if (Math.Abs(arc1.GetEnergyDelta(i) - arc2.GetEnergyDelta(i)) > 0.0001)
            {
                foundDifference = true;
                break;
            }
        }
        Assert(foundDifference, "Different seeds: some deltas differ (jitter applied)");
    }

    private static void Test_Build_4BarSection_Uses2BarPhrases()
    {
        var arc = SectionEnergyMicroArc.Build(barCount: 4, sectionEnergy: 0.5);

        // Expected: Start, Cadence, Start, Cadence
        Assert(arc.GetPhrasePosition(0) == PhrasePosition.Start, "4-bar: bar 0 is Start");
        Assert(arc.GetPhrasePosition(1) == PhrasePosition.Cadence, "4-bar: bar 1 is Cadence");
        Assert(arc.GetPhrasePosition(2) == PhrasePosition.Start, "4-bar: bar 2 is Start");
        Assert(arc.GetPhrasePosition(3) == PhrasePosition.Cadence, "4-bar: bar 3 is Cadence");
    }

    private static void Test_Build_8BarSection_Uses4BarPhrases()
    {
        var arc = SectionEnergyMicroArc.Build(barCount: 8, sectionEnergy: 0.5);

        // Expected: 4-bar phrases
        var startCount = arc.PhrasePositionByBar.Count(p => p == PhrasePosition.Start);
        var cadenceCount = arc.PhrasePositionByBar.Count(p => p == PhrasePosition.Cadence);

        Assert(startCount == 2, "8-bar: 2 starts (2 phrases)");
        Assert(cadenceCount == 2, "8-bar: 2 cadences (2 phrases)");
    }

    private static void Test_Build_GetEnergyDelta_ValidIndex()
    {
        var arc = SectionEnergyMicroArc.Build(barCount: 8, sectionEnergy: 0.6);
        double delta = arc.GetEnergyDelta(3);
        Assert(delta >= -0.10 && delta <= 0.10, "GetEnergyDelta: valid index returns bounded value");
    }

    private static void Test_Build_GetEnergyDelta_InvalidIndex()
    {
        var arc = SectionEnergyMicroArc.Build(barCount: 8, sectionEnergy: 0.6);
        double delta1 = arc.GetEnergyDelta(-1);
        double delta2 = arc.GetEnergyDelta(100);
        Assert(delta1 == 0.0, "GetEnergyDelta: negative index returns 0");
        Assert(delta2 == 0.0, "GetEnergyDelta: out-of-range index returns 0");
    }

    private static void Test_Build_GetPhrasePosition_ValidIndex()
    {
        var arc = SectionEnergyMicroArc.Build(barCount: 8, sectionEnergy: 0.6);
        var position = arc.GetPhrasePosition(0);
        Assert(position == PhrasePosition.Start, "GetPhrasePosition: valid index returns position");
    }

    private static void Test_Build_GetPhrasePosition_InvalidIndex()
    {
        var arc = SectionEnergyMicroArc.Build(barCount: 8, sectionEnergy: 0.6);
        var position1 = arc.GetPhrasePosition(-1);
        var position2 = arc.GetPhrasePosition(100);
        Assert(position1 == PhrasePosition.Middle, "GetPhrasePosition: negative index returns Middle");
        Assert(position2 == PhrasePosition.Middle, "GetPhrasePosition: out-of-range index returns Middle");
    }

    private static void Test_Integration_WithMicroTensionMap_SamePhraseLengthLogic()
    {
        // Verify that SectionEnergyMicroArc uses same phrase length inference as MicroTensionMap
        int barCount = 8;
        var energyArc = SectionEnergyMicroArc.Build(barCount, sectionEnergy: 0.6, phraseLength: null);
        var tensionMap = MicroTensionMap.Build(barCount, macroTension: 0.5, microDefault: 0.4, phraseLength: null);

        // Both should use 4-bar phrases for 8-bar section
        var energyCadences = energyArc.PhrasePositionByBar
            .Select((p, i) => (p, i))
            .Where(x => x.p == PhrasePosition.Cadence)
            .Select(x => x.i)
            .ToList();

        var tensionPhraseEnds = tensionMap.IsPhraseEnd
            .Select((flag, i) => (flag, i))
            .Where(x => x.flag)
            .Select(x => x.i)
            .ToList();

        Assert(energyCadences.Count == tensionPhraseEnds.Count, "Integration: same number of phrase ends");
        for (int i = 0; i < energyCadences.Count; i++)
        {
            Assert(energyCadences[i] == tensionPhraseEnds[i], $"Integration: phrase end {i} at same bar");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"Test failed: {message}");
    }
}
