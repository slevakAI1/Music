// AI: purpose=Test runner for Story 7.9 SongIntentQuery tests.
// AI: invariants=Calls all test methods; reports success/failure; exits cleanly.

namespace Music.Generator.Tests;

/// <summary>
/// Test runner for Story 7.9 - Unified Stage 7 intent query tests.
/// Call Run() from main program to execute tests.
/// </summary>
public static class RunSongIntentQueryTests
{
    public static void Run()
    {
        // TEMPORARILY DISABLED (Epic 6): Test uses EnergyArc which was removed in Story 4.1.
        // To be re-enabled during energy reintegration.
        Console.WriteLine("=== Song Intent Query Tests - SKIPPED (Energy Disconnected) ===\n");
        
        /* COMMENTED OUT UNTIL ENERGY REINTEGRATION
        Console.WriteLine("=== Story 7.9: Song Intent Query Tests ===\n");

        try
        {
            SongIntentQueryTests.RunAll();
            Console.WriteLine("\n=== All tests passed ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n=== Test failure ===");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            throw; // Re-throw to signal failure
        }
        */
    }
}
