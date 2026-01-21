// AI: purpose=Test runner for Stage 9 (Story 9.1) motif placement system
// AI: invariants=All tests are deterministic; runs all placement planner test suites sequentially
// AI: deps=MotifPlacementPlannerTests (Story 9.1)

using Music.Song.Material.Tests;

namespace Music.Song.Material.Tests;

/// <summary>
/// Consolidated test runner for Stage 9 motif placement system.
/// </summary>
public static class Stage9TestRunner
{
    /// <summary>
    /// Runs all Stage 9 tests (Story 9.1).
    /// </summary>
    public static void RunAllStage9Tests()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("=== STAGE 9 MOTIF PLACEMENT TEST SUITE ===");
        Console.WriteLine("========================================\n");

        try
        {
            Console.WriteLine("--- Story 9.1: MotifPlacementPlanner ---");
            MotifPlacementPlannerTests.RunAllTests();
            Console.WriteLine();

            Console.WriteLine("========================================");
            Console.WriteLine("✓✓✓ ALL STAGE 9 TESTS PASSED ✓✓✓");
            Console.WriteLine("========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ TEST FAILURE: {ex.Message}");
            Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
            throw;
        }
    }
}
