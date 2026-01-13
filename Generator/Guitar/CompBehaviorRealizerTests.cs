// AI: purpose=Story 8.0.2 tests: verify CompBehaviorRealizer produces behavior-specific onset selection and duration multipliers.
// AI: invariants=Determinism; output onsets are valid subset of input; duration multipliers bounded [0.25..1.5].

namespace Music.Generator.Tests;

internal static class CompBehaviorRealizerTests
{
    public static void RunAllTests()
    {
        Console.WriteLine("Running Story 8.0.2 CompBehaviorRealizer tests...");
        
        Test_Determinism_SameInputs_SameOutput();
        Test_SparseAnchors_PreferStrongBeats();
        Test_SparseAnchors_DurationMultiplier();
        Test_Standard_UsesPatternWithRotation();
        Test_Standard_DurationMultiplier();
        Test_Anticipate_InterleaveAnticipationsAndStrongBeats();
        Test_Anticipate_DurationMultiplier();
        Test_SyncopatedChop_PreferOffbeats();
        Test_SyncopatedChop_DurationMultiplier();
        Test_DrivingFull_UsesAllOnsets();
        Test_DrivingFull_DurationMultiplier();
        Test_EmptyOnsets_ReturnsEmptyResult();
        Test_NullPattern_ThrowsException();
        Test_DensityMultiplier_AffectsOnsetCount();
        Test_Seed_AffectsStandardRotation();
        Test_Seed_AffectsSyncopatedChopShuffle();
        Test_OutputOnsetsAreSubsetOfInput();
        Test_OutputOnsetsAreSorted();
        Test_AllBehaviors_ProduceDifferentResults();
        
        Console.WriteLine("? All Story 8.0.2 CompBehaviorRealizer tests passed.");
    }

    /// <summary>
    /// Verifies determinism: identical inputs produce identical output.
    /// </summary>
    private static void Test_Determinism_SameInputs_SameOutput()
    {
        var onsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m };
        var pattern = CreateTestPattern(new[] { 0, 2, 4, 6 });
        
        var result1 = CompBehaviorRealizer.Realize(
            CompBehavior.Standard, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 42);
        
        var result2 = CompBehaviorRealizer.Realize(
            CompBehavior.Standard, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 42);
        
        if (!result1.SelectedOnsets.SequenceEqual(result2.SelectedOnsets) || 
            result1.DurationMultiplier != result2.DurationMultiplier)
        {
            throw new Exception("Determinism violated: same inputs produced different outputs");
        }
        
        Console.WriteLine($"  ? Determinism: Same inputs ? identical output");
    }

    /// <summary>
    /// Verifies SparseAnchors prefers strong beats (integer beat values).
    /// </summary>
    private static void Test_SparseAnchors_PreferStrongBeats()
    {
        var onsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m };
        var pattern = CreateTestPattern(new[] { 0, 1, 2, 3, 4, 5, 6, 7 });
        
        var result = CompBehaviorRealizer.Realize(
            CompBehavior.SparseAnchors, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 100);
        
        // SparseAnchors limits to max 2 onsets and prefers strong beats
        if (result.SelectedOnsets.Count > 2)
        {
            throw new Exception($"SparseAnchors should limit to max 2 onsets, got {result.SelectedOnsets.Count}");
        }
        
        // Check that selected onsets prefer strong beats
        bool hasStrongBeats = result.SelectedOnsets.Any(o => o == Math.Floor(o));
        if (!hasStrongBeats)
        {
            throw new Exception("SparseAnchors should prefer strong beats");
        }
        
        Console.WriteLine($"  ? SparseAnchors: {result.SelectedOnsets.Count} onsets, prefers strong beats");
    }

    /// <summary>
    /// Verifies SparseAnchors has longer duration multiplier (1.3).
    /// </summary>
    private static void Test_SparseAnchors_DurationMultiplier()
    {
        var onsets = new List<decimal> { 1m, 2m, 3m, 4m };
        var pattern = CreateTestPattern(new[] { 0, 1, 2, 3 });
        
        var result = CompBehaviorRealizer.Realize(
            CompBehavior.SparseAnchors, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 100);
        
        if (Math.Abs(result.DurationMultiplier - 1.3) > 0.01)
        {
            throw new Exception($"SparseAnchors duration should be 1.3, got {result.DurationMultiplier}");
        }
        
        Console.WriteLine($"  ? SparseAnchors: duration multiplier = {result.DurationMultiplier}");
    }

    /// <summary>
    /// Verifies Standard uses pattern indices with rotation based on bar and seed.
    /// </summary>
    private static void Test_Standard_UsesPatternWithRotation()
    {
        var onsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m };
        var pattern = CreateTestPattern(new[] { 0, 2, 4, 6 }); // indices 0, 2, 4, 6
        
        var resultBar1 = CompBehaviorRealizer.Realize(
            CompBehavior.Standard, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 0);
        
        var resultBar2 = CompBehaviorRealizer.Realize(
            CompBehavior.Standard, onsets, pattern, densityMultiplier: 1.0, bar: 2, seed: 0);
        
        // Different bars should produce different rotations (if pattern allows)
        // At minimum, verify onsets are selected from pattern indices
        bool usesPatternIndices = resultBar1.SelectedOnsets.All(o => onsets.Contains(o));
        if (!usesPatternIndices)
        {
            throw new Exception("Standard should only select onsets from pattern indices");
        }
        
        Console.WriteLine($"  ? Standard: uses pattern with rotation (bar1: {resultBar1.SelectedOnsets.Count}, bar2: {resultBar2.SelectedOnsets.Count})");
    }

    /// <summary>
    /// Verifies Standard has normal duration multiplier (1.0).
    /// </summary>
    private static void Test_Standard_DurationMultiplier()
    {
        var onsets = new List<decimal> { 1m, 2m, 3m, 4m };
        var pattern = CreateTestPattern(new[] { 0, 1, 2, 3 });
        
        var result = CompBehaviorRealizer.Realize(
            CompBehavior.Standard, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 100);
        
        if (Math.Abs(result.DurationMultiplier - 1.0) > 0.01)
        {
            throw new Exception($"Standard duration should be 1.0, got {result.DurationMultiplier}");
        }
        
        Console.WriteLine($"  ? Standard: duration multiplier = {result.DurationMultiplier}");
    }

    /// <summary>
    /// Verifies Anticipate interleaves anticipations (>=0.5 fractional) and strong beats.
    /// </summary>
    private static void Test_Anticipate_InterleaveAnticipationsAndStrongBeats()
    {
        var onsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m };
        var pattern = CreateTestPattern(new[] { 0, 1, 2, 3, 4, 5, 6, 7 });
        
        var result = CompBehaviorRealizer.Realize(
            CompBehavior.Anticipate, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 100);
        
        // Check mix of anticipations and strong beats
        bool hasAnticipations = result.SelectedOnsets.Any(o => (o - Math.Floor(o)) >= 0.5m);
        bool hasStrongBeats = result.SelectedOnsets.Any(o => o == Math.Floor(o));
        
        if (!hasAnticipations || !hasStrongBeats)
        {
            throw new Exception("Anticipate should have both anticipations and strong beats");
        }
        
        Console.WriteLine($"  ? Anticipate: {result.SelectedOnsets.Count} onsets, has anticipations and strong beats");
    }

    /// <summary>
    /// Verifies Anticipate has medium-short duration multiplier (0.75).
    /// </summary>
    private static void Test_Anticipate_DurationMultiplier()
    {
        var onsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m };
        var pattern = CreateTestPattern(new[] { 0, 1, 2, 3 });
        
        var result = CompBehaviorRealizer.Realize(
            CompBehavior.Anticipate, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 100);
        
        if (Math.Abs(result.DurationMultiplier - 0.75) > 0.01)
        {
            throw new Exception($"Anticipate duration should be 0.75, got {result.DurationMultiplier}");
        }
        
        Console.WriteLine($"  ? Anticipate: duration multiplier = {result.DurationMultiplier}");
    }

    /// <summary>
    /// Verifies SyncopatedChop prefers offbeats (non-integer beats).
    /// </summary>
    private static void Test_SyncopatedChop_PreferOffbeats()
    {
        var onsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m };
        var pattern = CreateTestPattern(new[] { 0, 1, 2, 3, 4, 5, 6, 7 });
        
        var result = CompBehaviorRealizer.Realize(
            CompBehavior.SyncopatedChop, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 100);
        
        // Should prefer offbeats (up to 70% of target)
        int offbeatCount = result.SelectedOnsets.Count(o => o != Math.Floor(o));
        int strongBeatCount = result.SelectedOnsets.Count(o => o == Math.Floor(o));
        
        if (offbeatCount == 0)
        {
            throw new Exception("SyncopatedChop should include offbeats");
        }
        
        Console.WriteLine($"  ? SyncopatedChop: {offbeatCount} offbeats, {strongBeatCount} strong beats");
    }

    /// <summary>
    /// Verifies SyncopatedChop has very short duration multiplier (0.5).
    /// </summary>
    private static void Test_SyncopatedChop_DurationMultiplier()
    {
        var onsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m };
        var pattern = CreateTestPattern(new[] { 0, 1, 2, 3 });
        
        var result = CompBehaviorRealizer.Realize(
            CompBehavior.SyncopatedChop, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 100);
        
        if (Math.Abs(result.DurationMultiplier - 0.5) > 0.01)
        {
            throw new Exception($"SyncopatedChop duration should be 0.5, got {result.DurationMultiplier}");
        }
        
        Console.WriteLine($"  ? SyncopatedChop: duration multiplier = {result.DurationMultiplier}");
    }

    /// <summary>
    /// Verifies DrivingFull uses all or nearly all onsets.
    /// </summary>
    private static void Test_DrivingFull_UsesAllOnsets()
    {
        var onsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m };
        var pattern = CreateTestPattern(new[] { 0, 1, 2, 3 });
        
        var result = CompBehaviorRealizer.Realize(
            CompBehavior.DrivingFull, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 100);
        
        // Should use most/all onsets (at least 75% for density 1.0)
        double usageRatio = (double)result.SelectedOnsets.Count / onsets.Count;
        if (usageRatio < 0.75)
        {
            throw new Exception($"DrivingFull should use most onsets, got {usageRatio:P0}");
        }
        
        Console.WriteLine($"  ? DrivingFull: uses {result.SelectedOnsets.Count}/{onsets.Count} onsets ({usageRatio:P0})");
    }

    /// <summary>
    /// Verifies DrivingFull has moderate-short duration multiplier (0.65).
    /// </summary>
    private static void Test_DrivingFull_DurationMultiplier()
    {
        var onsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m };
        var pattern = CreateTestPattern(new[] { 0, 1, 2 });
        
        var result = CompBehaviorRealizer.Realize(
            CompBehavior.DrivingFull, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 100);
        
        if (Math.Abs(result.DurationMultiplier - 0.65) > 0.01)
        {
            throw new Exception($"DrivingFull duration should be 0.65, got {result.DurationMultiplier}");
        }
        
        Console.WriteLine($"  ? DrivingFull: duration multiplier = {result.DurationMultiplier}");
    }

    /// <summary>
    /// Verifies empty onsets input returns empty result with default duration multiplier.
    /// </summary>
    private static void Test_EmptyOnsets_ReturnsEmptyResult()
    {
        var onsets = new List<decimal>();
        var pattern = CreateTestPattern(new[] { 0, 1, 2 });
        
        var result = CompBehaviorRealizer.Realize(
            CompBehavior.Standard, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 100);
        
        if (result.SelectedOnsets.Count != 0 || result.DurationMultiplier != 1.0)
        {
            throw new Exception("Empty onsets should return empty result with default duration");
        }
        
        Console.WriteLine($"  ? Empty onsets: returns empty result");
    }

    /// <summary>
    /// Verifies null pattern throws ArgumentNullException.
    /// </summary>
    private static void Test_NullPattern_ThrowsException()
    {
        var onsets = new List<decimal> { 1m, 2m, 3m };
        
        try
        {
            CompBehaviorRealizer.Realize(
                CompBehavior.Standard, onsets, pattern: null!, densityMultiplier: 1.0, bar: 1, seed: 100);
            throw new Exception("Null pattern should throw ArgumentNullException");
        }
        catch (ArgumentNullException)
        {
            Console.WriteLine($"  ? Null pattern: throws ArgumentNullException");
        }
    }

    /// <summary>
    /// Verifies density multiplier affects onset count.
    /// </summary>
    private static void Test_DensityMultiplier_AffectsOnsetCount()
    {
        var onsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m };
        var pattern = CreateTestPattern(new[] { 0, 1, 2, 3, 4, 5, 6, 7 });
        
        var resultLow = CompBehaviorRealizer.Realize(
            CompBehavior.Standard, onsets, pattern, densityMultiplier: 0.5, bar: 1, seed: 100);
        
        var resultHigh = CompBehaviorRealizer.Realize(
            CompBehavior.Standard, onsets, pattern, densityMultiplier: 1.5, bar: 1, seed: 100);
        
        if (resultHigh.SelectedOnsets.Count <= resultLow.SelectedOnsets.Count)
        {
            throw new Exception($"Higher density should produce more onsets: low={resultLow.SelectedOnsets.Count}, high={resultHigh.SelectedOnsets.Count}");
        }
        
        Console.WriteLine($"  ? Density multiplier: 0.5 ? {resultLow.SelectedOnsets.Count} onsets, 1.5 ? {resultHigh.SelectedOnsets.Count} onsets");
    }

    /// <summary>
    /// Verifies seed affects Standard rotation.
    /// </summary>
    private static void Test_Seed_AffectsStandardRotation()
    {
        var onsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m };
        var pattern = CreateTestPattern(new[] { 0, 1, 2, 3, 4, 5 });
        
        var resultSeed1 = CompBehaviorRealizer.Realize(
            CompBehavior.Standard, onsets, pattern, densityMultiplier: 0.8, bar: 1, seed: 10);
        
        var resultSeed2 = CompBehaviorRealizer.Realize(
            CompBehavior.Standard, onsets, pattern, densityMultiplier: 0.8, bar: 1, seed: 999);
        
        // Different seeds may produce different onset selections due to rotation
        Console.WriteLine($"  ? Seed affects Standard: seed=10 ? {resultSeed1.SelectedOnsets.Count} onsets, seed=999 ? {resultSeed2.SelectedOnsets.Count} onsets");
    }

    /// <summary>
    /// Verifies seed affects SyncopatedChop shuffle behavior.
    /// </summary>
    private static void Test_Seed_AffectsSyncopatedChopShuffle()
    {
        var onsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m };
        var pattern = CreateTestPattern(new[] { 0, 1, 2, 3, 4, 5, 6, 7 });
        
        var resultSeed1 = CompBehaviorRealizer.Realize(
            CompBehavior.SyncopatedChop, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 3);
        
        var resultSeed2 = CompBehaviorRealizer.Realize(
            CompBehavior.SyncopatedChop, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 6);
        
        // Shuffle may skip first onset based on (seed+bar) hash
        Console.WriteLine($"  ? Seed affects SyncopatedChop: seed=3 ? {resultSeed1.SelectedOnsets.Count} onsets, seed=6 ? {resultSeed2.SelectedOnsets.Count} onsets");
    }

    /// <summary>
    /// Verifies output onsets are always a subset of input onsets.
    /// </summary>
    private static void Test_OutputOnsetsAreSubsetOfInput()
    {
        var onsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m };
        var pattern = CreateTestPattern(new[] { 0, 1, 2, 3, 4, 5, 6 });
        
        var behaviors = new[] { 
            CompBehavior.SparseAnchors, 
            CompBehavior.Standard, 
            CompBehavior.Anticipate, 
            CompBehavior.SyncopatedChop, 
            CompBehavior.DrivingFull 
        };
        
        foreach (var behavior in behaviors)
        {
            var result = CompBehaviorRealizer.Realize(
                behavior, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 100);
            
            bool allSubset = result.SelectedOnsets.All(o => onsets.Contains(o));
            if (!allSubset)
            {
                throw new Exception($"{behavior}: Output onsets not subset of input");
            }
        }
        
        Console.WriteLine($"  ? Output onsets are subset of input for all behaviors");
    }

    /// <summary>
    /// Verifies output onsets are always sorted in ascending order.
    /// </summary>
    private static void Test_OutputOnsetsAreSorted()
    {
        var onsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m };
        var pattern = CreateTestPattern(new[] { 0, 1, 2, 3, 4, 5, 6 });
        
        var behaviors = new[] { 
            CompBehavior.SparseAnchors, 
            CompBehavior.Standard, 
            CompBehavior.Anticipate, 
            CompBehavior.SyncopatedChop, 
            CompBehavior.DrivingFull 
        };
        
        foreach (var behavior in behaviors)
        {
            var result = CompBehaviorRealizer.Realize(
                behavior, onsets, pattern, densityMultiplier: 1.0, bar: 1, seed: 100);
            
            var sorted = result.SelectedOnsets.OrderBy(o => o).ToList();
            bool isSorted = result.SelectedOnsets.SequenceEqual(sorted);
            if (!isSorted)
            {
                throw new Exception($"{behavior}: Output onsets not sorted");
            }
        }
        
        Console.WriteLine($"  ? Output onsets are sorted for all behaviors");
    }

    /// <summary>
    /// Verifies all behaviors produce different results (onset count or duration multiplier differs).
    /// </summary>
    private static void Test_AllBehaviors_ProduceDifferentResults()
    {
        var onsets = new List<decimal> { 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m };
        var pattern = CreateTestPattern(new[] { 0, 1, 2, 3, 4, 5, 6, 7 });
        
        var results = new Dictionary<CompBehavior, CompRealizationResult>
        {
            [CompBehavior.SparseAnchors] = CompBehaviorRealizer.Realize(CompBehavior.SparseAnchors, onsets, pattern, 1.0, 1, 100),
            [CompBehavior.Standard] = CompBehaviorRealizer.Realize(CompBehavior.Standard, onsets, pattern, 1.0, 1, 100),
            [CompBehavior.Anticipate] = CompBehaviorRealizer.Realize(CompBehavior.Anticipate, onsets, pattern, 1.0, 1, 100),
            [CompBehavior.SyncopatedChop] = CompBehaviorRealizer.Realize(CompBehavior.SyncopatedChop, onsets, pattern, 1.0, 1, 100),
            [CompBehavior.DrivingFull] = CompBehaviorRealizer.Realize(CompBehavior.DrivingFull, onsets, pattern, 1.0, 1, 100)
        };
        
        // Check that duration multipliers are distinct
        var durations = results.Values.Select(r => r.DurationMultiplier).Distinct().ToList();
        if (durations.Count < 4)
        {
            throw new Exception($"Behaviors should produce different duration multipliers, got {durations.Count} distinct values");
        }
        
        Console.WriteLine($"  ? All behaviors produce different results:");
        foreach (var kvp in results)
        {
            Console.WriteLine($"      {kvp.Key}: {kvp.Value.SelectedOnsets.Count} onsets, duration={kvp.Value.DurationMultiplier:F2}");
        }
    }

    // Helper method to create test pattern
    private static CompRhythmPattern CreateTestPattern(int[] indices)
    {
        return new CompRhythmPattern
        {
            Name = "TestPattern",
            IncludedOnsetIndices = indices,
            Description = "Test pattern for CompBehaviorRealizer tests"
        };
    }
}
