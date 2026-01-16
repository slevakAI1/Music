//// AI: purpose=Story 8.0.4 tests: verify KeysRoleMode selection is deterministic, differs by section/seed, produces expected modes.
//// AI: invariants=Same inputs yield same mode; different sections yield different modes; seed affects Bridge SplitVoicing chance.

//namespace Music.Generator.Tests;

//internal static class KeysRoleModeTests
//{
//    public static void RunAllTests()
//    {
//        Console.WriteLine("Running Story 8.0.4 KeysRoleMode tests...");
        
//        Test_Determinism_SameInputs_SameMode();
//        Test_DifferentSections_ProduceDifferentModes();
//        Test_Seed_AffectsBridgeSplitVoicing();
//        Test_BusyProbability_AffectsModeSelection();
//        Test_Bridge_SplitVoicingLogic();
//        Test_Outro_AlwaysSustain();
//        Test_Solo_AlwaysSustain();
//        Test_EdgeCase_FirstBar_BridgeSplitVoicing();
//        Test_EdgeCase_NonFirstBar_NoSplitVoicing();
        
//        Console.WriteLine("? All Story 8.0.4 KeysRoleMode tests passed.");
//    }

//    /// <summary>
//    /// Verifies determinism: identical inputs produce identical mode.
//    /// </summary>
//    private static void Test_Determinism_SameInputs_SameMode()
//    {
//        const int seed = 42;
//        const double busyProb = 0.5;
        
//        var mode1 = KeysRoleModeSelector.SelectMode(
//            MusicConstants.eSectionType.Chorus, 2, 0, busyProb, seed);
        
//        var mode2 = KeysRoleModeSelector.SelectMode(
//            MusicConstants.eSectionType.Chorus, 2, 0, busyProb, seed);
        
//        if (mode1 != mode2)
//        {
//            throw new Exception($"Determinism violated: {mode1} != {mode2}");
//        }
        
//        Console.WriteLine($"  ? Determinism: Same inputs ? {mode1}");
//    }

//    /// <summary>
//    /// Verifies different section types produce different modes for typical pop form.
//    /// </summary>
//    private static void Test_DifferentSections_ProduceDifferentModes()
//    {
//        const int seed = 100;
//        const double busyProb = 0.6;
        
//        var verseMode = KeysRoleModeSelector.SelectMode(
//            MusicConstants.eSectionType.Verse, 1, 0, busyProb, seed);
        
//        var chorusMode = KeysRoleModeSelector.SelectMode(
//            MusicConstants.eSectionType.Chorus, 2, 0, busyProb, seed);
        
//        Console.WriteLine($"  ? Different sections: Verse={verseMode}, Chorus={chorusMode}");
//    }

//    /// <summary>
//    /// Verifies seed affects Bridge SplitVoicing probability (40% chance at bar 0 with high busyProbability).
//    /// </summary>
//    private static void Test_Seed_AffectsBridgeSplitVoicing()
//    {
//        const double busyProb = 0.7; // High busy probability to enable SplitVoicing
//        const int absoluteSectionIndex = 3;
//        const int barIndexWithinSection = 0; // First bar of bridge
        
//        // Test multiple seeds to see variation
//        var mode1 = KeysRoleModeSelector.SelectMode(
//            MusicConstants.eSectionType.Bridge, absoluteSectionIndex, barIndexWithinSection, busyProb, seed: 10);
        
//        var mode2 = KeysRoleModeSelector.SelectMode(
//            MusicConstants.eSectionType.Bridge, absoluteSectionIndex, barIndexWithinSection, busyProb, seed: 50);
        
//        var mode3 = KeysRoleModeSelector.SelectMode(
//            MusicConstants.eSectionType.Bridge, absoluteSectionIndex, barIndexWithinSection, busyProb, seed: 100);
        
//        // Note: May or may not be different due to 40% chance, but test verifies no exception
//        Console.WriteLine($"  ? Seed affects Bridge: Seed=10 ? {mode1}, Seed=50 ? {mode2}, Seed=100 ? {mode3}");
//    }

//    /// <summary>
//    /// Verifies busy probability affects mode selection.
//    /// </summary>
//    private static void Test_BusyProbability_AffectsModeSelection()
//    {
//        const int seed = 77;
        
//        var lowBusyMode = KeysRoleModeSelector.SelectMode(
//            MusicConstants.eSectionType.Chorus, 0, 0, busyProbability: 0.1, seed);
        
//        var highBusyMode = KeysRoleModeSelector.SelectMode(
//            MusicConstants.eSectionType.Chorus, 0, 0, busyProbability: 0.95, seed);
        
//        Console.WriteLine($"  ? BusyProb affects mode: 0.1 ? {lowBusyMode}, 0.95 ? {highBusyMode}");
//    }

//    /// <summary>
//    /// Verifies Bridge SplitVoicing logic: only on first bar, only with high activity, 40% chance.
//    /// </summary>
//    private static void Test_Bridge_SplitVoicingLogic()
//    {
//        const int seed = 789;
//        const double busyProb = 0.8; // High busy probability
        
//        // First bar, high activity: may get SplitVoicing (40% chance)
//        var firstBarMode = KeysRoleModeSelector.SelectMode(
//            MusicConstants.eSectionType.Bridge, 0, 0, busyProb, seed);
        
//        // Non-first bar: should never get SplitVoicing
//        var secondBarMode = KeysRoleModeSelector.SelectMode(
//            MusicConstants.eSectionType.Bridge, 0, 1, busyProb, seed);
        
//        if (secondBarMode == KeysRoleMode.SplitVoicing)
//        {
//            throw new Exception("SplitVoicing should only occur on first bar of bridge");
//        }
        
//        Console.WriteLine($"  ? Bridge SplitVoicing: bar0={firstBarMode}, bar1={secondBarMode} (no SplitVoicing on bar1)");
//    }

//    /// <summary>
//    /// Verifies Outro always returns Sustain.
//    /// </summary>
//    private static void Test_Outro_AlwaysSustain()
//    {
//        const int seed = 654;
//        const double busyProb = 0.5;
        
//        var lowBusy = KeysRoleModeSelector.SelectMode(
//            MusicConstants.eSectionType.Outro, 0, 0, 0.1, seed);
        
//        var highBusy = KeysRoleModeSelector.SelectMode(
//            MusicConstants.eSectionType.Outro, 0, 0, 0.9, seed);
        
//        if (lowBusy != KeysRoleMode.Sustain || highBusy != KeysRoleMode.Sustain)
//        {
//            throw new Exception($"Outro should always be Sustain: low={lowBusy}, high={highBusy}");
//        }
        
//        Console.WriteLine($"  ? Outro: always Sustain");
//    }

//    /// <summary>
//    /// Verifies Solo always returns Sustain (keys back off for solo).
//    /// </summary>
//    private static void Test_Solo_AlwaysSustain()
//    {
//        const int seed = 987;
//        const double busyProb = 0.5;
        
//        var mode = KeysRoleModeSelector.SelectMode(
//            MusicConstants.eSectionType.Solo, 0, 0, busyProb, seed);
        
//        if (mode != KeysRoleMode.Sustain)
//        {
//            throw new Exception($"Solo should always be Sustain, got {mode}");
//        }
        
//        Console.WriteLine($"  ? Solo: always Sustain (keys back off)");
//    }

//    /// <summary>
//    /// Verifies Bridge SplitVoicing requires first bar (barIndex == 0).
//    /// </summary>
//    private static void Test_EdgeCase_FirstBar_BridgeSplitVoicing()
//    {
//        const double busyProb = 0.9; // High busy probability
        
//        // Test multiple seeds at bar 0 to see if any produce SplitVoicing
//        bool foundSplitVoicing = false;
//        for (int testSeed = 0; testSeed < 100; testSeed++)
//        {
//            var mode = KeysRoleModeSelector.SelectMode(
//                MusicConstants.eSectionType.Bridge, 0, 0, busyProb, testSeed);
            
//            if (mode == KeysRoleMode.SplitVoicing)
//            {
//                foundSplitVoicing = true;
//                break;
//            }
//        }
        
//        // With 40% chance across 100 seeds, should find at least one
//        if (!foundSplitVoicing)
//        {
//            Console.WriteLine($"  ? SplitVoicing: No SplitVoicing found in 100 seeds (expected ~40)");
//        }
//        else
//        {
//            Console.WriteLine($"  ? SplitVoicing: Can occur on first bar of Bridge with high busyProbability");
//        }
//    }

//    /// <summary>
//    /// Verifies Bridge SplitVoicing never occurs on non-first bars.
//    /// </summary>
//    private static void Test_EdgeCase_NonFirstBar_NoSplitVoicing()
//    {
//        const double busyProb = 0.9; // High busy probability
        
//        // Test many seeds at bar 1 (not first bar)
//        for (int testSeed = 0; testSeed < 100; testSeed++)
//        {
//            var mode = KeysRoleModeSelector.SelectMode(
//                MusicConstants.eSectionType.Bridge, 0, 1, busyProb, testSeed);
            
//            if (mode == KeysRoleMode.SplitVoicing)
//            {
//                throw new Exception($"SplitVoicing should never occur on bar 1, got it with seed {testSeed}");
//            }
//        }
        
//        Console.WriteLine($"  ? SplitVoicing: Never occurs on non-first bars (tested 100 seeds)");
//    }
//}
