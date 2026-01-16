//// AI: purpose=Story 8.0.5 tests: verify KeysModeRealizer produces mode-specific onset selection and duration multipliers.
//// AI: invariants=Determinism; output onsets are valid subset of input; duration multipliers bounded [0.5..2.0].

//namespace Music.Generator.Tests;

//internal static class KeysModeRealizerTests
//{
//    public static void RunAllTests()
//    {
//        Console.WriteLine("Running Story 8.0.5 KeysModeRealizer tests...");
        
//        Test_Determinism_SameInputs_SameOutput();
//        Test_DifferentSeeds_ProduceDifferentResults();
//        Test_NullOnsets_ReturnsEmptyResult();
//        Test_EmptyOnsets_ReturnsEmptyResult();
//        Test_Sustain_ReturnsOnlyFirstOnset();
//        Test_Sustain_DurationMultiplier();
//        Test_Pulse_PrefersStrongBeats();
//        Test_Pulse_IncludesBeat1();
//        Test_Pulse_DurationMultiplier();
//        Test_Pulse_DensityAffectsOnsetCount();
//        Test_Rhythmic_UsesMostOnsets();
//        Test_Rhythmic_DurationMultiplier();
//        Test_Rhythmic_DensityAffectsOnsetCount();
//        Test_SplitVoicing_ReturnsTwoOnsets();
//        Test_SplitVoicing_MarksSplitUpperIndex();
//        Test_SplitVoicing_SelectsFirstAndMiddle();
//        Test_SplitVoicing_DurationMultiplier();
//        Test_SplitVoicing_FallbackWithOneOnset();
//        Test_OutputOnsetsAreSubsetOfInput();
//        Test_DurationMultipliersAreBounded();
//        Test_DifferentModes_ProduceDifferentOnsetCounts();
//        Test_SelectedOnsetsAreSorted();
//        Test_SingleOnset_HandlesAllModes();
//        Test_DensityClampedToValidRange();
        
//        Console.WriteLine("? All Story 8.0.5 KeysModeRealizer tests passed.");
//    }

//    #region Test Data Helpers

//    private static IReadOnlyList<decimal> CreateTypicalPadsOnsets()
//    {
//        return new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m };
//    }

//    private static IReadOnlyList<decimal> CreateSparsePadsOnsets()
//    {
//        return new List<decimal> { 1m, 2m, 3m, 4m };
//    }

//    private static IReadOnlyList<decimal> CreateDensePadsOnsets()
//    {
//        return new List<decimal> { 1m, 1.25m, 1.5m, 1.75m, 2m, 2.25m, 2.5m, 2.75m, 3m, 3.25m, 3.5m, 3.75m, 4m, 4.25m, 4.5m, 4.75m };
//    }

//    #endregion

//    private static void Test_Determinism_SameInputs_SameOutput()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var modes = new[] { KeysRoleMode.Sustain, KeysRoleMode.Pulse, KeysRoleMode.Rhythmic, KeysRoleMode.SplitVoicing };

//        foreach (var mode in modes)
//        {
//            var result1 = KeysModeRealizer.Realize(mode, onsets, 1.0, bar: 1, seed: 42);
//            var result2 = KeysModeRealizer.Realize(mode, onsets, 1.0, bar: 1, seed: 42);

//            if (!result1.SelectedOnsets.SequenceEqual(result2.SelectedOnsets) ||
//                result1.DurationMultiplier != result2.DurationMultiplier ||
//                result1.SplitUpperOnsetIndex != result2.SplitUpperOnsetIndex)
//            {
//                throw new Exception($"Determinism failed for mode {mode}");
//            }
//        }
//    }

//    private static void Test_DifferentSeeds_ProduceDifferentResults()
//    {
//        var onsets = CreateTypicalPadsOnsets();

//        var result1 = KeysModeRealizer.Realize(KeysRoleMode.Pulse, onsets, 1.0, bar: 1, seed: 42);
//        var result2 = KeysModeRealizer.Realize(KeysRoleMode.Pulse, onsets, 1.0, bar: 1, seed: 99);

//        if (result1.SelectedOnsets.SequenceEqual(result2.SelectedOnsets))
//        {
//            throw new Exception("Different seeds should produce different results");
//        }
//    }

//    private static void Test_NullOnsets_ReturnsEmptyResult()
//    {
//        var result = KeysModeRealizer.Realize(KeysRoleMode.Pulse, null!, 1.0, bar: 1, seed: 42);

//        if (result.SelectedOnsets.Count != 0 || result.DurationMultiplier != 1.0 || result.SplitUpperOnsetIndex != -1)
//        {
//            throw new Exception("Null onsets should return empty result");
//        }
//    }

//    private static void Test_EmptyOnsets_ReturnsEmptyResult()
//    {
//        var result = KeysModeRealizer.Realize(KeysRoleMode.Pulse, Array.Empty<decimal>(), 1.0, bar: 1, seed: 42);

//        if (result.SelectedOnsets.Count != 0)
//        {
//            throw new Exception("Empty onsets should return empty result");
//        }
//    }

//    private static void Test_Sustain_ReturnsOnlyFirstOnset()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var result = KeysModeRealizer.Realize(KeysRoleMode.Sustain, onsets, 1.0, bar: 1, seed: 42);

//        if (result.SelectedOnsets.Count != 1 || result.SelectedOnsets[0] != onsets[0])
//        {
//            throw new Exception("Sustain mode should return only first onset");
//        }
//    }

//    private static void Test_Sustain_DurationMultiplier()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var result = KeysModeRealizer.Realize(KeysRoleMode.Sustain, onsets, 1.0, bar: 1, seed: 42);

//        if (result.DurationMultiplier != 2.0)
//        {
//            throw new Exception($"Sustain duration should be 2.0, got {result.DurationMultiplier}");
//        }
//    }

//    private static void Test_Pulse_PrefersStrongBeats()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var result = KeysModeRealizer.Realize(KeysRoleMode.Pulse, onsets, 1.0, bar: 1, seed: 42);

//        if (!result.SelectedOnsets.Contains(1m))
//        {
//            throw new Exception("Pulse mode should prefer strong beats including beat 1");
//        }

//        if (result.SelectedOnsets.Count == 0 || result.SelectedOnsets.Count >= onsets.Count)
//        {
//            throw new Exception($"Pulse mode should select moderate count, got {result.SelectedOnsets.Count}");
//        }
//    }

//    private static void Test_Pulse_IncludesBeat1()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var result = KeysModeRealizer.Realize(KeysRoleMode.Pulse, onsets, 1.0, bar: 1, seed: 42);

//        if (!result.SelectedOnsets.Contains(1m))
//        {
//            throw new Exception("Pulse mode should always include beat 1 when present");
//        }
//    }

//    private static void Test_Pulse_DurationMultiplier()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var result = KeysModeRealizer.Realize(KeysRoleMode.Pulse, onsets, 1.0, bar: 1, seed: 42);

//        if (result.DurationMultiplier != 1.0)
//        {
//            throw new Exception($"Pulse duration should be 1.0, got {result.DurationMultiplier}");
//        }
//    }

//    private static void Test_Pulse_DensityAffectsOnsetCount()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var resultLow = KeysModeRealizer.Realize(KeysRoleMode.Pulse, onsets, 0.5, bar: 1, seed: 42);
//        var resultHigh = KeysModeRealizer.Realize(KeysRoleMode.Pulse, onsets, 1.5, bar: 1, seed: 42);

//        if (resultLow.SelectedOnsets.Count >= resultHigh.SelectedOnsets.Count)
//        {
//            throw new Exception($"Low density ({resultLow.SelectedOnsets.Count}) should produce fewer onsets than high density ({resultHigh.SelectedOnsets.Count})");
//        }
//    }

//    private static void Test_Rhythmic_UsesMostOnsets()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var result = KeysModeRealizer.Realize(KeysRoleMode.Rhythmic, onsets, 1.0, bar: 1, seed: 42);

//        if (result.SelectedOnsets.Count < onsets.Count * 0.8)
//        {
//            throw new Exception($"Rhythmic mode should use most onsets, got {result.SelectedOnsets.Count} of {onsets.Count}");
//        }
//    }

//    private static void Test_Rhythmic_DurationMultiplier()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var result = KeysModeRealizer.Realize(KeysRoleMode.Rhythmic, onsets, 1.0, bar: 1, seed: 42);

//        if (result.DurationMultiplier != 0.7)
//        {
//            throw new Exception($"Rhythmic duration should be 0.7, got {result.DurationMultiplier}");
//        }
//    }

//    private static void Test_Rhythmic_DensityAffectsOnsetCount()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var resultLow = KeysModeRealizer.Realize(KeysRoleMode.Rhythmic, onsets, 0.5, bar: 1, seed: 42);
//        var resultHigh = KeysModeRealizer.Realize(KeysRoleMode.Rhythmic, onsets, 1.5, bar: 1, seed: 42);

//        if (resultLow.SelectedOnsets.Count > resultHigh.SelectedOnsets.Count)
//        {
//            throw new Exception("Low density should not produce more onsets than high density");
//        }
//    }

//    private static void Test_SplitVoicing_ReturnsTwoOnsets()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var result = KeysModeRealizer.Realize(KeysRoleMode.SplitVoicing, onsets, 1.0, bar: 1, seed: 42);

//        if (result.SelectedOnsets.Count != 2)
//        {
//            throw new Exception($"SplitVoicing should return 2 onsets, got {result.SelectedOnsets.Count}");
//        }
//    }

//    private static void Test_SplitVoicing_MarksSplitUpperIndex()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var result = KeysModeRealizer.Realize(KeysRoleMode.SplitVoicing, onsets, 1.0, bar: 1, seed: 42);

//        if (result.SplitUpperOnsetIndex != 1)
//        {
//            throw new Exception($"SplitVoicing should mark index 1 as upper, got {result.SplitUpperOnsetIndex}");
//        }
//    }

//    private static void Test_SplitVoicing_SelectsFirstAndMiddle()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var result = KeysModeRealizer.Realize(KeysRoleMode.SplitVoicing, onsets, 1.0, bar: 1, seed: 42);

//        if (result.SelectedOnsets[0] != onsets[0] || result.SelectedOnsets[1] != onsets[onsets.Count / 2])
//        {
//            throw new Exception("SplitVoicing should select first and middle onsets");
//        }
//    }

//    private static void Test_SplitVoicing_DurationMultiplier()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var result = KeysModeRealizer.Realize(KeysRoleMode.SplitVoicing, onsets, 1.0, bar: 1, seed: 42);

//        if (result.DurationMultiplier != 1.2)
//        {
//            throw new Exception($"SplitVoicing duration should be 1.2, got {result.DurationMultiplier}");
//        }
//    }

//    private static void Test_SplitVoicing_FallbackWithOneOnset()
//    {
//        var onsets = new List<decimal> { 1m };
//        var result = KeysModeRealizer.Realize(KeysRoleMode.SplitVoicing, onsets, 1.0, bar: 1, seed: 42);

//        if (result.SelectedOnsets.Count != 1 || result.SplitUpperOnsetIndex != -1)
//        {
//            throw new Exception("SplitVoicing with 1 onset should fallback gracefully");
//        }
//    }

//    private static void Test_OutputOnsetsAreSubsetOfInput()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var modes = new[] { KeysRoleMode.Sustain, KeysRoleMode.Pulse, KeysRoleMode.Rhythmic, KeysRoleMode.SplitVoicing };

//        foreach (var mode in modes)
//        {
//            var result = KeysModeRealizer.Realize(mode, onsets, 1.0, bar: 1, seed: 42);

//            foreach (var onset in result.SelectedOnsets)
//            {
//                if (!onsets.Contains(onset))
//                {
//                    throw new Exception($"Mode {mode} produced onset {onset} not in input");
//                }
//            }
//        }
//    }

//    private static void Test_DurationMultipliersAreBounded()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var modes = new[] { KeysRoleMode.Sustain, KeysRoleMode.Pulse, KeysRoleMode.Rhythmic, KeysRoleMode.SplitVoicing };

//        foreach (var mode in modes)
//        {
//            var result = KeysModeRealizer.Realize(mode, onsets, 1.0, bar: 1, seed: 42);

//            if (result.DurationMultiplier < 0.5 || result.DurationMultiplier > 2.0)
//            {
//                throw new Exception($"Mode {mode} duration {result.DurationMultiplier} out of bounds [0.5..2.0]");
//            }
//        }
//    }

//    private static void Test_DifferentModes_ProduceDifferentOnsetCounts()
//    {
//        var onsets = CreateTypicalPadsOnsets();

//        var sustainResult = KeysModeRealizer.Realize(KeysRoleMode.Sustain, onsets, 1.0, bar: 1, seed: 42);
//        var pulseResult = KeysModeRealizer.Realize(KeysRoleMode.Pulse, onsets, 1.0, bar: 1, seed: 42);
//        var rhythmicResult = KeysModeRealizer.Realize(KeysRoleMode.Rhythmic, onsets, 1.0, bar: 1, seed: 42);

//        if (sustainResult.SelectedOnsets.Count >= pulseResult.SelectedOnsets.Count ||
//            pulseResult.SelectedOnsets.Count >= rhythmicResult.SelectedOnsets.Count)
//        {
//            throw new Exception($"Expected Sustain < Pulse < Rhythmic, got {sustainResult.SelectedOnsets.Count} < {pulseResult.SelectedOnsets.Count} < {rhythmicResult.SelectedOnsets.Count}");
//        }
//    }

//    private static void Test_SelectedOnsetsAreSorted()
//    {
//        var onsets = CreateTypicalPadsOnsets();
//        var modes = new[] { KeysRoleMode.Pulse, KeysRoleMode.Rhythmic, KeysRoleMode.SplitVoicing };

//        foreach (var mode in modes)
//        {
//            var result = KeysModeRealizer.Realize(mode, onsets, 1.0, bar: 1, seed: 42);
//            var sorted = result.SelectedOnsets.OrderBy(o => o).ToList();

//            if (!result.SelectedOnsets.SequenceEqual(sorted))
//            {
//                throw new Exception($"Mode {mode} onsets are not sorted");
//            }
//        }
//    }

//    private static void Test_SingleOnset_HandlesAllModes()
//    {
//        var onsets = new List<decimal> { 1m };
//        var modes = new[] { KeysRoleMode.Sustain, KeysRoleMode.Pulse, KeysRoleMode.Rhythmic, KeysRoleMode.SplitVoicing };

//        foreach (var mode in modes)
//        {
//            var result = KeysModeRealizer.Realize(mode, onsets, 1.0, bar: 1, seed: 42);

//            if (result.SelectedOnsets.Count == 0 || !result.SelectedOnsets.Contains(1m))
//            {
//                throw new Exception($"Mode {mode} failed to handle single onset");
//            }
//        }
//    }

//    private static void Test_DensityClampedToValidRange()
//    {
//        var onsets = CreateTypicalPadsOnsets();

//        // Test with out-of-range density values - should not crash
//        var resultLow = KeysModeRealizer.Realize(KeysRoleMode.Pulse, onsets, -1.0, bar: 1, seed: 42);
//        var resultHigh = KeysModeRealizer.Realize(KeysRoleMode.Pulse, onsets, 5.0, bar: 1, seed: 42);

//        if (resultLow.SelectedOnsets.Count == 0 || resultHigh.SelectedOnsets.Count == 0)
//        {
//            throw new Exception("Invalid density should be clamped, not produce empty result");
//        }
//    }
//}
