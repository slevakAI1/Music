// AI: purpose=Consolidated test runner for Stage 8 (Stories 8.1, 8.2, 8.3) motif system tests
// AI: invariants=All tests are deterministic; runs all test suites sequentially
// AI: deps=MaterialDefinitionsTests (Story M1), MotifStorageTests (Story 8.2), MotifLibraryTests (Story 8.3)
using Music.Song.Material.Tests;
using Music.Tests.Material;

namespace Music.Song.Material.Tests;

/// <summary>
/// Consolidated test runner for Stage 8 motif system.
/// </summary>
public static class Stage8TestRunner
{
    /// <summary>
    /// Runs all Stage 8 tests (Stories 8.1, 8.2, 8.3).
    /// </summary>
    public static void RunAllStage8Tests()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("=== STAGE 8 MOTIF SYSTEM TEST SUITE ===");
        Console.WriteLine("========================================\n");

        try
        {
            Console.WriteLine("--- Story M1: Material Definitions (Prerequisite) ---");
            MaterialDefinitionsTests.RunAll();
            Console.WriteLine();

            Console.WriteLine("--- Story 8.2: Motif Storage and MaterialBank ---");
            MotifStorageTests.RunAllTests();
            Console.WriteLine();

            Console.WriteLine("--- Story 8.3: MotifLibrary (Hardcoded Test Motifs) ---");
            MotifLibraryTests.RunAll();
            Console.WriteLine();

            Console.WriteLine("--- Story 8.4: MotifValidation (Validation Helpers) ---");
            MotifValidationTests.RunAll();
            Console.WriteLine();

            Console.WriteLine("--- Story 8.5: MotifDefinitions (Comprehensive Data Layer) ---");
            MotifDefinitionsTests.RunAll();
            Console.WriteLine();

            Console.WriteLine("========================================");
            Console.WriteLine("✓✓✓ ALL STAGE 8 TESTS PASSED ✓✓✓");
            Console.WriteLine("========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ TEST FAILURE: {ex.Message}");
            Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Quick smoke test - just run Story 8.3 tests.
    /// </summary>
    public static void RunMotifLibraryTestsOnly()
    {
        Console.WriteLine("=== Story 8.3: MotifLibrary Tests (Quick Smoke Test) ===\n");
        MotifLibraryTests.RunAll();
    }
}
