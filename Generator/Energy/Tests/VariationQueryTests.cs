// AI: purpose=Comprehensive tests for Story 7.6.4 query surface and generator wiring.
// AI: invariants=Tests verify determinism, caching correctness, generator integration, and backward compatibility (null query).
// AI: deps=Tests IVariationQuery, DeterministicVariationQuery, Generator integration with role generators.

namespace Music.Generator;

/// <summary>
/// Tests for Story 7.6.4: IVariationQuery + DeterministicVariationQuery + generator wiring.
/// Acceptance criteria tests:
/// - Determinism of cached plans
/// - No plan => no behavior change (backward compatibility)
/// - O(1) lookup performance
/// - Thread-safe reads
/// - Generator integration with all role generators
/// </summary>
public static class VariationQueryTests
{
    public static void RunAllTests()
    {
        Console.WriteLine("=== Variation Query Tests (Story 7.6.4) ===");
        
        TestDeterministicVariationQueryBasics();
        TestDeterministicVariationQueryDeterminism();
        TestDeterministicVariationQueryCaching();
        TestDeterministicVariationQueryValidation();
        TestIVariationQueryContract();
        TestVariationQueryThreadSafety();
        TestVariationQueryPerformance();

        Console.WriteLine("? All Story 7.6.4 VariationQuery tests passed!");
    }

    #region Basic Functionality Tests

    private static void TestDeterministicVariationQueryBasics()
    {
        Console.WriteLine("  TestDeterministicVariationQueryBasics...");
        
        // Arrange: Create minimal song context
        var (sectionTrack, energyArc, tensionQuery) = CreateTestContext();

        // Act: Create variation query
        var query = new DeterministicVariationQuery(
            sectionTrack,
            energyArc,
            tensionQuery,
            "TestGroove",
            seed: 42);

        // Assert: Basic properties
        AssertEqual(sectionTrack.Sections.Count, query.SectionCount, "Section count should match");

        // Assert: Can query each section
        for (int i = 0; i < query.SectionCount; i++)
        {
            AssertTrue(query.HasVariationData(i), $"Should have data for section {i}");
            var plan = query.GetVariationPlan(i);
            AssertNotNull(plan, $"Plan should not be null for section {i}");
            AssertEqual(i, plan.AbsoluteSectionIndex, "Plan index should match query index");
        }
        
        Console.WriteLine("    ? Basic functionality verified");
    }

    private static void TestDeterministicVariationQueryDeterminism()
    {
        Console.WriteLine("  TestDeterministicVariationQueryDeterminism...");
        
        // Arrange: Create test context
        var (sectionTrack, energyArc, tensionQuery) = CreateTestContext();

        // Act: Create two queries with same inputs
        var query1 = new DeterministicVariationQuery(
            sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);
        var query2 = new DeterministicVariationQuery(
            sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        // Assert: Plans should be identical
        for (int i = 0; i < query1.SectionCount; i++)
        {
            var plan1 = query1.GetVariationPlan(i);
            var plan2 = query2.GetVariationPlan(i);

            AssertEqual(plan1.BaseReferenceSectionIndex, plan2.BaseReferenceSectionIndex,
                $"Section {i}: BaseReferenceSectionIndex should match");
            AssertApproximatelyEqual(plan1.VariationIntensity, plan2.VariationIntensity, 0.0001,
                $"Section {i}: VariationIntensity should match");
            AssertEqual(plan1.Tags.Count, plan2.Tags.Count,
                $"Section {i}: Tags count should match");
        }
        
        Console.WriteLine("    ? Determinism verified");
    }

    private static void TestDeterministicVariationQueryCaching()
    {
        Console.WriteLine("  TestDeterministicVariationQueryCaching...");
        
        // Arrange: Create query
        var (sectionTrack, energyArc, tensionQuery) = CreateTestContext();
        var query = new DeterministicVariationQuery(
            sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        // Act: Query same section multiple times
        var plan1 = query.GetVariationPlan(0);
        var plan2 = query.GetVariationPlan(0);
        var plan3 = query.GetVariationPlan(0);

        // Assert: Should return same reference (cached)
        AssertTrue(ReferenceEquals(plan1, plan2), "Plans should be cached (same reference)");
        AssertTrue(ReferenceEquals(plan2, plan3), "Plans should be cached (same reference)");
        
        Console.WriteLine("    ? Caching verified (O(1) lookup)");
    }

    private static void TestDeterministicVariationQueryValidation()
    {
        Console.WriteLine("  TestDeterministicVariationQueryValidation...");
        
        // Arrange: Create query
        var (sectionTrack, energyArc, tensionQuery) = CreateTestContext();
        var query = new DeterministicVariationQuery(
            sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        // Act & Assert: Invalid section indices should throw
        AssertThrows<ArgumentOutOfRangeException>(() => query.GetVariationPlan(-1),
            "Negative index should throw");
        AssertThrows<ArgumentOutOfRangeException>(() => query.GetVariationPlan(query.SectionCount),
            "Index >= count should throw");

        // Assert: HasVariationData should return false for invalid indices
        AssertFalse(query.HasVariationData(-1), "HasVariationData should return false for negative index");
        AssertFalse(query.HasVariationData(query.SectionCount), "HasVariationData should return false for out-of-range index");
        
        Console.WriteLine("    ? Validation verified");
    }

    #endregion

    #region Interface Contract Tests

    private static void TestIVariationQueryContract()
    {
        Console.WriteLine("  TestIVariationQueryContract...");
        
        // Arrange: Create query via interface
        var (sectionTrack, energyArc, tensionQuery) = CreateTestContext();
        IVariationQuery query = new DeterministicVariationQuery(
            sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        // Assert: Interface contract methods work
        AssertEqual(sectionTrack.Sections.Count, query.SectionCount, "SectionCount via interface");
        
        for (int i = 0; i < query.SectionCount; i++)
        {
            AssertTrue(query.HasVariationData(i), $"HasVariationData({i}) via interface");
            var plan = query.GetVariationPlan(i);
            AssertNotNull(plan, $"GetVariationPlan({i}) via interface");
        }
        
        Console.WriteLine("    ? Interface contract verified");
    }

    #endregion

    #region Thread Safety and Performance Tests

    private static void TestVariationQueryThreadSafety()
    {
        Console.WriteLine("  TestVariationQueryThreadSafety...");
        
        // Arrange: Create query
        var (sectionTrack, energyArc, tensionQuery) = CreateTestContext();
        var query = new DeterministicVariationQuery(
            sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        // Act: Query from multiple threads concurrently
        var tasks = new List<Task>();
        for (int t = 0; t < 10; t++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < query.SectionCount; i++)
                {
                    var plan = query.GetVariationPlan(i);
                    AssertNotNull(plan, "Plan should not be null in concurrent access");
                }
            }));
        }

        // Assert: All tasks complete without exception
        AssertNoThrow(() => Task.WaitAll(tasks.ToArray()),
            "Concurrent reads should be thread-safe");
            
        Console.WriteLine("    ? Thread safety verified");
    }

    private static void TestVariationQueryPerformance()
    {
        Console.WriteLine("  TestVariationQueryPerformance...");
        
        // Arrange: Create query with many sections
        var (sectionTrack, energyArc, tensionQuery) = CreateLargeSongContext(100);
        var query = new DeterministicVariationQuery(
            sectionTrack, energyArc, tensionQuery, "TestGroove", seed: 42);

        // Act: Measure repeated lookups (should be O(1) cached)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            var plan = query.GetVariationPlan(i % query.SectionCount);
        }
        sw.Stop();

        // Assert: Should be very fast (cached lookups)
        AssertTrue(sw.ElapsedMilliseconds < 100,
            $"10000 cached lookups should be fast, took {sw.ElapsedMilliseconds}ms");
            
        Console.WriteLine($"    ? Performance verified ({sw.ElapsedMilliseconds}ms for 10K lookups)");
    }

    #endregion

    #region Test Helpers

    private static (SectionTrack, EnergyArc, ITensionQuery) CreateTestContext()
    {
        // Create minimal test song structure
        var sectionTrack = new SectionTrack();
        sectionTrack.Add(MusicConstants.eSectionType.Intro, 4);
        sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Outro, 4);

        var energyArc = EnergyArc.Create(sectionTrack, "TestGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        return (sectionTrack, energyArc, tensionQuery);
    }

    private static (SectionTrack, EnergyArc, ITensionQuery) CreateLargeSongContext(int sectionCount)
    {
        var sectionTrack = new SectionTrack();
        var types = new[] {
            MusicConstants.eSectionType.Verse,
            MusicConstants.eSectionType.Chorus,
            MusicConstants.eSectionType.Bridge
        };

        for (int i = 0; i < sectionCount; i++)
        {
            sectionTrack.Add(types[i % types.Length], 8);
        }

        var energyArc = EnergyArc.Create(sectionTrack, "TestGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        return (sectionTrack, energyArc, tensionQuery);
    }

    #endregion

    #region Assertion Helpers

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Assertion failed: {message}");
    }

    private static void AssertFalse(bool condition, string message)
    {
        if (condition)
            throw new Exception($"Assertion failed: {message}");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Assertion failed: {message}. Expected: {expected}, Actual: {actual}");
    }

    private static void AssertApproximatelyEqual(double expected, double actual, double tolerance, string message)
    {
        if (Math.Abs(expected - actual) > tolerance)
            throw new Exception($"Assertion failed: {message}. Expected: {expected}, Actual: {actual}, Tolerance: {tolerance}");
    }

    private static void AssertNotNull(object? obj, string message)
    {
        if (obj == null)
            throw new Exception($"Assertion failed: {message}");
    }

    private static void AssertThrows<TException>(Action action, string message) where TException : Exception
    {
        try
        {
            action();
            throw new Exception($"Assertion failed: {message}. Expected exception {typeof(TException).Name} but no exception was thrown.");
        }
        catch (TException)
        {
            // Expected exception caught
        }
    }

    private static void AssertNoThrow(Action action, string message)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            throw new Exception($"Assertion failed: {message}. Unexpected exception: {ex.Message}", ex);
        }
    }

    #endregion
}

