// AI: purpose=Test runner for Story 7.6 variation plan tests (7.6.1 model + 7.6.2 base reference selection)
// AI: run=Execute from Program.cs or debug hook to verify acceptance criteria

using Music.Generator;

namespace Music.Tests;

internal static class RunSectionVariationPlanTests
{
    public static void Run()
    {
        try
        {
            Console.WriteLine("\n=== Running Story 7.6 Section Variation Plan Tests ===\n");
            
            Console.WriteLine("\n--- Story 7.6.1: SectionVariationPlan Model ---\n");
            SectionVariationPlanTests.RunAllTests();
            
            Console.WriteLine("\n--- Story 7.6.2: Base Reference Selection (A/A'/B Mapping) ---\n");
            BaseReferenceSelectorRulesTests.RunAllTests();
            
            Console.WriteLine("\n? All Story 7.6 tests passed successfully!\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n? Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
