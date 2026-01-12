// AI: purpose=Tests for Story 7.4.4 energy constraint diagnostics and explainability.
// AI: invariants=Diagnostics must not affect generation; output must be deterministic and human-readable.
// AI: deps=Tests EnergyConstraintDiagnostics formatting and reporting functions.

namespace Music.Generator
{
    /// <summary>
    /// Tests for Story 7.4.4: Constraint diagnostics and explainability.
    /// Validates diagnostic output formatting and information completeness.
    /// </summary>
    public static class EnergyConstraintDiagnosticsTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Energy Constraint Diagnostics Tests ===");

            TestFullReportGeneration();
            TestSummaryReportGeneration();
            TestCompactReportGeneration();
            TestArcComparison();
            TestEnergyChartGeneration();
            TestDiagnosticsDoNotAffectGeneration();
            TestDiagnosticsDeterminism();

            Console.WriteLine("All Energy Constraint Diagnostics tests passed.");
        }

        private static void TestFullReportGeneration()
        {
            // Create a song with known structure
            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);   // V1
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);   // V2
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);  // C1

            var arc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            // Generate full report
            string report = EnergyConstraintDiagnostics.GenerateFullReport(arc);

            // Verify report contains key information
            if (!report.Contains("Energy Constraint Diagnostic Report"))
                throw new Exception("Report missing header");
            if (!report.Contains("Arc Template:"))
                throw new Exception("Report missing template info");
            if (!report.Contains("Policy:"))
                throw new Exception("Report missing policy info");
            if (!report.Contains("Verse 1"))
                throw new Exception("Report missing section info");
            if (!report.Contains("Template energy:"))
                throw new Exception("Report missing template energy");
            if (!report.Contains("Final energy:"))
                throw new Exception("Report missing final energy");

            Console.WriteLine("  ? Full report generation");
        }

        private static void TestSummaryReportGeneration()
        {
            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);

            var arc = EnergyArc.Create(sectionTrack, "RockSteady", seed: 42);

            // Generate summary
            string summary = EnergyConstraintDiagnostics.GenerateSummaryReport(arc);

            // Verify summary contains key information
            if (!summary.Contains("Energy Arc Summary"))
                throw new Exception("Summary missing header");
            if (!summary.Contains("Energy Progression by Section Type:"))
                throw new Exception("Summary missing progression info");
            if (!summary.Contains("Energy Peak:"))
                throw new Exception("Summary missing peak info");
            if (!summary.Contains("Constraint Adjustments:"))
                throw new Exception("Summary missing adjustment count");

            Console.WriteLine("  ? Summary report generation");
        }

        private static void TestCompactReportGeneration()
        {
            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Intro, 4);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);

            var arc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            // Generate compact report
            string compact = EnergyConstraintDiagnostics.GenerateCompactReport(arc);

            // Verify compact format
            if (!compact.Contains("Energy Arc:"))
                throw new Exception("Compact report missing header");
            
            // Should have one line per section
            var lines = compact.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 4) // Header + 3 sections
                throw new Exception("Compact report missing section lines");

            // Check for section markers
            if (!compact.Contains("#00") || !compact.Contains("#01") || !compact.Contains("#02"))
                throw new Exception("Compact report missing section numbers");

            Console.WriteLine("  ? Compact report generation");
        }

        private static void TestArcComparison()
        {
            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);

            // Create two arcs with different policies
            var popPolicy = EnergyConstraintPolicyLibrary.GetPopRockPolicy();
            var jazzPolicy = EnergyConstraintPolicyLibrary.GetJazzPolicy();

            var popArc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42, constraintPolicy: popPolicy);
            var jazzArc = EnergyArc.Create(sectionTrack, "JazzGroove", seed: 42, constraintPolicy: jazzPolicy);

            // Compare arcs
            string comparison = EnergyConstraintDiagnostics.CompareArcs(popArc, jazzArc, "Pop", "Jazz");

            // Verify comparison format
            if (!comparison.Contains("Energy Arc Comparison"))
                throw new Exception("Comparison missing header");
            if (!comparison.Contains("Pop:") || !comparison.Contains("Jazz:"))
                throw new Exception("Comparison missing arc labels");
            if (!comparison.Contains("Delta"))
                throw new Exception("Comparison missing delta column");
            if (!comparison.Contains("Verse"))
                throw new Exception("Comparison missing section info");

            Console.WriteLine("  ? Arc comparison");
        }

        private static void TestEnergyChartGeneration()
        {
            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Intro, 4);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);

            var arc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            // Generate chart
            string chart = EnergyConstraintDiagnostics.GenerateEnergyChart(arc);

            // Verify chart format
            if (!chart.Contains("Energy Flow Chart"))
                throw new Exception("Chart missing header");
            if (!chart.Contains("|"))
                throw new Exception("Chart missing Y-axis");
            if (!chart.Contains("+"))
                throw new Exception("Chart missing X-axis");
            
            // Should have visual elements
            if (!chart.Contains("?") && !chart.Contains("?") && !chart.Contains(" "))
                throw new Exception("Chart missing visual elements");

            Console.WriteLine("  ? Energy chart generation");
        }

        private static void TestDiagnosticsDoNotAffectGeneration()
        {
            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);

            const int seed = 12345;

            // Create arc and collect energies
            var arc1 = EnergyArc.Create(sectionTrack, "PopGroove", seed);
            var energies1 = new List<double>();
            for (int i = 0; i < sectionTrack.Sections.Count; i++)
            {
                var section = sectionTrack.Sections[i];
                int sectionIndex = GetSectionIndexHelper(sectionTrack, i);
                var target = arc1.GetTargetForSection(section, sectionIndex);
                energies1.Add(target.Energy);
            }

            // Generate all diagnostics
            _ = EnergyConstraintDiagnostics.GenerateFullReport(arc1);
            _ = EnergyConstraintDiagnostics.GenerateSummaryReport(arc1);
            _ = EnergyConstraintDiagnostics.GenerateCompactReport(arc1);
            _ = EnergyConstraintDiagnostics.GenerateEnergyChart(arc1);

            // Create new arc with same parameters and collect energies
            var arc2 = EnergyArc.Create(sectionTrack, "PopGroove", seed);
            var energies2 = new List<double>();
            for (int i = 0; i < sectionTrack.Sections.Count; i++)
            {
                var section = sectionTrack.Sections[i];
                int sectionIndex = GetSectionIndexHelper(sectionTrack, i);
                var target = arc2.GetTargetForSection(section, sectionIndex);
                energies2.Add(target.Energy);
            }

            // Verify energies are identical
            for (int i = 0; i < energies1.Count; i++)
            {
                if (Math.Abs(energies1[i] - energies2[i]) > 0.0001)
                {
                    throw new Exception($"Diagnostics affected generation: section {i} energy changed from {energies1[i]} to {energies2[i]}");
                }
            }

            Console.WriteLine("  ? Diagnostics do not affect generation");
        }

        private static void TestDiagnosticsDeterminism()
        {
            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);

            const int seed = 99999;

            // Generate reports twice with same parameters
            var arc1 = EnergyArc.Create(sectionTrack, "RockSteady", seed);
            string report1 = EnergyConstraintDiagnostics.GenerateFullReport(arc1);
            string summary1 = EnergyConstraintDiagnostics.GenerateSummaryReport(arc1);
            string compact1 = EnergyConstraintDiagnostics.GenerateCompactReport(arc1);

            var arc2 = EnergyArc.Create(sectionTrack, "RockSteady", seed);
            string report2 = EnergyConstraintDiagnostics.GenerateFullReport(arc2);
            string summary2 = EnergyConstraintDiagnostics.GenerateSummaryReport(arc2);
            string compact2 = EnergyConstraintDiagnostics.GenerateCompactReport(arc2);

            // Verify reports are identical
            if (report1 != report2)
                throw new Exception("Full report not deterministic");
            if (summary1 != summary2)
                throw new Exception("Summary report not deterministic");
            if (compact1 != compact2)
                throw new Exception("Compact report not deterministic");

            Console.WriteLine("  ? Diagnostics are deterministic");
        }

        private static int GetSectionIndexHelper(SectionTrack sectionTrack, int absoluteIndex)
        {
            var section = sectionTrack.Sections[absoluteIndex];
            int count = 0;
            for (int i = 0; i <= absoluteIndex; i++)
            {
                if (sectionTrack.Sections[i].SectionType == section.SectionType)
                {
                    if (i == absoluteIndex)
                        return count;
                    count++;
                }
            }
            return 0;
        }
    }
}
