// AI: purpose=Test runner for Story 7.5.8 TensionContext integration tests
// AI: run=Execute from Program.cs or debug hook to verify acceptance criteria

using Music.Generator;

namespace Music.Tests;

internal static class RunTensionContextIntegrationTests
{
    public static void Run()
    {
        // TEMPORARILY DISABLED (Epic 6): Test uses EnergyArc which was removed in Story 4.1.
        // To be re-enabled during energy reintegration.
        Console.WriteLine("\n=== TensionContext Integration Tests - SKIPPED (Energy Disconnected) ===\n");
        
        /* COMMENTED OUT UNTIL ENERGY REINTEGRATION
        try
        {
            Console.WriteLine("\n=== Running Story 7.5.8 TensionContext Integration Tests ===\n");
            TensionContextIntegrationTests.RunAllTests();
            Console.WriteLine("\n? All tests passed successfully!\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n? Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        */
    }
}
