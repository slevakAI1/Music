// AI: purpose=Tests for TensionDiagnostics (Story 7.5.7); verify diagnostics don't affect generation, are deterministic, and produce valid output.
// AI: invariants=Diagnostics must not mutate query state; same inputs => same outputs; all reports must be non-empty for non-trivial input.
// AI: NOTE - TEMPORARILY DISABLED (Epic 6): These tests use EnergyArc which was removed in Story 4.1. To be re-enabled during energy reintegration.

namespace Music.Generator.Tests;

internal static class TensionDiagnosticsTests
{
    /// <summary>
    /// CRITICAL: Verifies that calling diagnostics does not change tension values.
    /// Story 7.5.7 acceptance criterion: diagnostics must not affect generation.
    /// </summary>
    public static void TestDiagnosticsDoNotAffectGeneration()
    {
        // TEMPORARILY DISABLED (Epic 6): Test uses EnergyArc which was removed in Story 4.1.
        // To be re-enabled during energy reintegration.
        Console.WriteLine("TestDiagnosticsDoNotAffectGeneration - SKIPPED (Energy Disconnected)");
        return;

        /* COMMENTED OUT UNTIL ENERGY REINTEGRATION
        // Arrange: Create a tension query with non-trivial data
        var sectionTrack = CreateTestSectionTrack();
        var energyArc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        // Capture tension values before diagnostics
        var tensionsBefore = new List<(double Macro, double MicroFirst, double MicroLast)>();
        for (int i = 0; i < tensionQuery.SectionCount; i++)
        {
            var macroProfile = tensionQuery.GetMacroTension(i);
            var microMap = tensionQuery.GetMicroTensionMap(i);
            int barCount = sectionTrack.Sections[i].BarCount;
            
            tensionsBefore.Add((
                macroProfile.MacroTension,
                microMap.GetTension(0),
                microMap.GetTension(Math.Max(0, barCount - 1))
            ));
        }

        // Act: Call all diagnostic methods
        var fullReport = TensionDiagnostics.GenerateFullReport(tensionQuery, sectionTrack, true);
        var summaryReport = TensionDiagnostics.GenerateSummaryReport(tensionQuery, sectionTrack);
        var compactReport = TensionDiagnostics.GenerateCompactReport(tensionQuery, sectionTrack);
        var transitionReport = TensionDiagnostics.GenerateTransitionHintSummary(tensionQuery, sectionTrack);

        // Capture tension values after diagnostics
        var tensionsAfter = new List<(double Macro, double MicroFirst, double MicroLast)>();
        for (int i = 0; i < tensionQuery.SectionCount; i++)
        {
            var macroProfile = tensionQuery.GetMacroTension(i);
            var microMap = tensionQuery.GetMicroTensionMap(i);
            int barCount = sectionTrack.Sections[i].BarCount;
            
            tensionsAfter.Add((
                macroProfile.MacroTension,
                microMap.GetTension(0),
                microMap.GetTension(Math.Max(0, barCount - 1))
            ));
        }

        // Assert: Tension values must be identical
        if (tensionsBefore.Count != tensionsAfter.Count)
        {
            throw new Exception("Tension query section count changed after diagnostics");
        }

        for (int i = 0; i < tensionsBefore.Count; i++)
        {
            if (Math.Abs(tensionsBefore[i].Macro - tensionsAfter[i].Macro) > 0.0001)
            {
                throw new Exception($"Macro tension changed at section {i}: {tensionsBefore[i].Macro} => {tensionsAfter[i].Macro}");
            }

            if (Math.Abs(tensionsBefore[i].MicroFirst - tensionsAfter[i].MicroFirst) > 0.0001)
            {
                throw new Exception($"Micro tension (first bar) changed at section {i}");
            }

            if (Math.Abs(tensionsBefore[i].MicroLast - tensionsAfter[i].MicroLast) > 0.0001)
            {
                throw new Exception($"Micro tension (last bar) changed at section {i}");
            }
        }

        Console.WriteLine("? TestDiagnosticsDoNotAffectGeneration passed");
        */
    }

    /// <summary>
    /// Verifies diagnostic output is deterministic: same inputs produce same reports.
    /// Story 7.5.7 acceptance criterion: diagnostics must be deterministic.
    /// </summary>
    public static void TestDiagnosticsDeterminism()
    {
        // TEMPORARILY DISABLED (Epic 6): Test uses EnergyArc which was removed in Story 4.1.
        Console.WriteLine("TestDiagnosticsDeterminism - SKIPPED (Energy Disconnected)");
        return;
        
        /* COMMENTED OUT UNTIL ENERGY REINTEGRATION
        const int seed = 12345;
        
        // Create two identical tension queries
        var sectionTrack1 = CreateTestSectionTrack();
        var sectionTrack2 = CreateTestSectionTrack();
        
        var energyArc1 = EnergyArc.Create(sectionTrack1, "PopGroove", seed: seed);
        var energyArc2 = EnergyArc.Create(sectionTrack2, "PopGroove", seed: seed);
        
        var tensionQuery1 = new DeterministicTensionQuery(energyArc1, seed: seed);
        var tensionQuery2 = new DeterministicTensionQuery(energyArc2, seed: seed);

        // Generate reports from both
        var fullReport1 = TensionDiagnostics.GenerateFullReport(tensionQuery1, sectionTrack1);
        var fullReport2 = TensionDiagnostics.GenerateFullReport(tensionQuery2, sectionTrack2);
        
        var summaryReport1 = TensionDiagnostics.GenerateSummaryReport(tensionQuery1, sectionTrack1);
        var summaryReport2 = TensionDiagnostics.GenerateSummaryReport(tensionQuery2, sectionTrack2);
        
        var compactReport1 = TensionDiagnostics.GenerateCompactReport(tensionQuery1, sectionTrack1);
        var compactReport2 = TensionDiagnostics.GenerateCompactReport(tensionQuery2, sectionTrack2);

        // Assert: Reports must be identical
        if (fullReport1 != fullReport2)
        {
            throw new Exception("Full reports differ for identical inputs");
        }

        if (summaryReport1 != summaryReport2)
        {
            throw new Exception("Summary reports differ for identical inputs");
        }

        if (compactReport1 != compactReport2)
        {
            throw new Exception("Compact reports differ for identical inputs");
        }

        Console.WriteLine("? TestDiagnosticsDeterminism passed");
    }

    /// <summary>
    /// Verifies full report contains expected sections and information.
    /// </summary>
    public static void TestFullReportGeneration()
    {
        var sectionTrack = CreateTestSectionTrack();
        var energyArc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        var report = TensionDiagnostics.GenerateFullReport(tensionQuery, sectionTrack, includeAllSections: true);

        // Assert: Report should contain key information
        if (string.IsNullOrWhiteSpace(report))
        {
            throw new Exception("Full report is empty");
        }

        if (!report.Contains("Tension Diagnostic Report"))
        {
            throw new Exception("Full report missing title");
        }

        if (!report.Contains("Query Type:"))
        {
            throw new Exception("Full report missing query type");
        }

        if (!report.Contains("Macro Tension:"))
        {
            throw new Exception("Full report missing macro tension");
        }

        if (!report.Contains("Micro Tension Map"))
        {
            throw new Exception("Full report missing micro tension map summary");
        }

        // Should contain at least one section
        if (!report.Contains("Section #"))
        {
            throw new Exception("Full report missing section information");
        }

        Console.WriteLine($"? TestFullReportGeneration passed (report length: {report.Length} characters)");
    }

    /// <summary>
    /// Verifies summary report format and content.
    /// </summary>
    public static void TestSummaryReportGeneration()
    {
        var sectionTrack = CreateTestSectionTrack();
        var energyArc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        var report = TensionDiagnostics.GenerateSummaryReport(tensionQuery, sectionTrack);

        // Assert: Report should contain summary information
        if (string.IsNullOrWhiteSpace(report))
        {
            throw new Exception("Summary report is empty");
        }

        if (!report.Contains("Tension Summary"))
        {
            throw new Exception("Summary report missing title");
        }

        if (!report.Contains("Macro Tension Progression"))
        {
            throw new Exception("Summary report missing tension progression");
        }

        if (!report.Contains("Tension Peak:"))
        {
            throw new Exception("Summary report missing tension peak");
        }

        Console.WriteLine($"? TestSummaryReportGeneration passed (report length: {report.Length} characters)");
    }

    /// <summary>
    /// Verifies compact report format (one line per section).
    /// </summary>
    public static void TestCompactReportGeneration()
    {
        var sectionTrack = CreateTestSectionTrack();
        var energyArc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        var report = TensionDiagnostics.GenerateCompactReport(tensionQuery, sectionTrack);

        // Assert: Report should have one line per section (plus header)
        if (string.IsNullOrWhiteSpace(report))
        {
            throw new Exception("Compact report is empty");
        }

        var lines = report.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // Should have at least header + sections
        if (lines.Length < tensionQuery.SectionCount)
        {
            throw new Exception($"Compact report has {lines.Length} lines but expected at least {tensionQuery.SectionCount}");
        }

        // Each section line should contain macro and micro tension
        bool foundMacro = lines.Any(l => l.Contains("Macro="));
        bool foundMicro = lines.Any(l => l.Contains("Micro="));

        if (!foundMacro || !foundMicro)
        {
            throw new Exception("Compact report missing macro or micro tension indicators");
        }

        Console.WriteLine($"? TestCompactReportGeneration passed ({lines.Length} lines)");
    }

    /// <summary>
    /// Verifies transition hint summary format.
    /// </summary>
    public static void TestTransitionHintSummary()
    {
        var sectionTrack = CreateTestSectionTrack();
        var energyArc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        var report = TensionDiagnostics.GenerateTransitionHintSummary(tensionQuery, sectionTrack);

        // Assert: Report should contain transition hints
        if (string.IsNullOrWhiteSpace(report))
        {
            throw new Exception("Transition hint summary is empty");
        }

        if (!report.Contains("Section Transition Hints"))
        {
            throw new Exception("Transition hint summary missing title");
        }

        // Should mention at least one transition hint type
        bool hasHint = report.Contains("Build") || 
                      report.Contains("Release") || 
                      report.Contains("Sustain") || 
                      report.Contains("Drop") ||
                      report.Contains("None");

        if (!hasHint)
        {
            throw new Exception("Transition hint summary missing hint types");
        }

        Console.WriteLine($"? TestTransitionHintSummary passed");
    }

    /// <summary>
    /// Verifies that includeAllSections parameter filters output correctly.
    /// </summary>
    public static void TestFullReportFiltering()
    {
        var sectionTrack = CreateTestSectionTrack();
        var energyArc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        var fullReport = TensionDiagnostics.GenerateFullReport(tensionQuery, sectionTrack, includeAllSections: true);
        var filteredReport = TensionDiagnostics.GenerateFullReport(tensionQuery, sectionTrack, includeAllSections: false);

        // Assert: Filtered report should be <= full report length
        if (filteredReport.Length > fullReport.Length)
        {
            throw new Exception("Filtered report is longer than full report");
        }

        Console.WriteLine($"? TestFullReportFiltering passed (full: {fullReport.Length}, filtered: {filteredReport.Length})");
    }

    /// <summary>
    /// Verifies reports work correctly with neutral/minimal tension.
    /// </summary>
    public static void TestReportsWithNeutralTension()
    {
        // Create a tension query with minimal section track
        var sectionTrack = new SectionTrack();
        sectionTrack.Add(MusicConstants.eSectionType.Verse, 4);

        var energyArc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);
        var tensionQuery = new DeterministicTensionQuery(energyArc, seed: 42);

        // Act: Generate all reports
        var fullReport = TensionDiagnostics.GenerateFullReport(tensionQuery, sectionTrack);
        var summaryReport = TensionDiagnostics.GenerateSummaryReport(tensionQuery, sectionTrack);
        var compactReport = TensionDiagnostics.GenerateCompactReport(tensionQuery, sectionTrack);

        // Assert: All reports should be non-empty even with minimal data
        if (string.IsNullOrWhiteSpace(fullReport))
        {
            throw new Exception("Full report is empty for neutral tension");
        }

        if (string.IsNullOrWhiteSpace(summaryReport))
        {
            throw new Exception("Summary report is empty for neutral tension");
        }

        if (string.IsNullOrWhiteSpace(compactReport))
        {
            throw new Exception("Compact report is empty for neutral tension");
        }

        Console.WriteLine("? TestReportsWithNeutralTension passed");
    }

    // Helper method
    private static SectionTrack CreateTestSectionTrack()
    {
        var sectionTrack = new SectionTrack();
        
        sectionTrack.Add(MusicConstants.eSectionType.Intro, 4);
        sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Bridge, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
        sectionTrack.Add(MusicConstants.eSectionType.Outro, 4);

        return sectionTrack;
    }

    public static void RunAllTests()
    {
        // TEMPORARILY DISABLED (Epic 6): Tests use EnergyArc which was removed in Story 4.1.
        // To be re-enabled during energy reintegration.
        Console.WriteLine("=== Tension Diagnostics Tests - SKIPPED (Energy Disconnected) ===\n");
        return;

        /* COMMENTED OUT UNTIL ENERGY REINTEGRATION
        Console.WriteLine("=== Tension Diagnostics Tests ===");
        TestDiagnosticsDoNotAffectGeneration();
        TestDiagnosticsDeterminism();
        TestFullReportGeneration();
        TestSummaryReportGeneration();
        TestCompactReportGeneration();
        TestTransitionHintSummary();
        TestFullReportFiltering();
        TestReportsWithNeutralTension();
        Console.WriteLine("=== All Tension Diagnostics Tests Passed ===\n");
        */
    }
}
