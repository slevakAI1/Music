// AI: purpose=Demonstration of energy constraint diagnostics (Story 7.4.4).
// AI: invariants=Example code showing practical usage of diagnostic reports.
// AI: deps=Uses EnergyConstraintDiagnostics to display constraint decisions.

using Music.Generator.EnergyConstraints;

namespace Music.Generator
{
    /// <summary>
    /// Demonstrates the energy constraint diagnostics system (Story 7.4.4).
    /// Shows practical usage examples for debugging and tuning energy constraints.
    /// </summary>
    public static class EnergyDiagnosticsDemo
    {
        /// <summary>
        /// Runs all diagnostic demonstrations.
        /// </summary>
        public static void RunAllDemos()
        {
            Console.WriteLine("=== Energy Diagnostics Demonstration ===");
            Console.WriteLine();

            DemoFullReport();
            DemoSummaryReport();
            DemoCompactReport();
            DemoPolicyComparison();
            DemoEnergyChart();

            Console.WriteLine();
            Console.WriteLine("=== End of Demonstrations ===");
        }

        /// <summary>
        /// Demonstrates full detailed diagnostic report.
        /// </summary>
        public static void DemoFullReport()
        {
            Console.WriteLine("--- DEMO: Full Diagnostic Report ---");
            Console.WriteLine();

            // Create a standard pop song structure
            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Intro, 4);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);   // V1
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);  // C1
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);   // V2
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);  // C2
            sectionTrack.Add(MusicConstants.eSectionType.Bridge, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);  // C3 (final)
            sectionTrack.Add(MusicConstants.eSectionType.Outro, 4);

            var arc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            // Generate and display full report
            string report = EnergyConstraintDiagnostics.GenerateFullReport(arc, includeUnchangedSections: false);
            Console.WriteLine(report);
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates summary report for quick overview.
        /// </summary>
        public static void DemoSummaryReport()
        {
            Console.WriteLine("--- DEMO: Summary Report ---");
            Console.WriteLine();

            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);

            var arc = EnergyArc.Create(sectionTrack, "RockSteady", seed: 42);

            string summary = EnergyConstraintDiagnostics.GenerateSummaryReport(arc);
            Console.WriteLine(summary);
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates compact report suitable for logging.
        /// </summary>
        public static void DemoCompactReport()
        {
            Console.WriteLine("--- DEMO: Compact Report (for logging) ---");
            Console.WriteLine();

            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Intro, 4);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);

            var arc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            string compact = EnergyConstraintDiagnostics.GenerateCompactReport(arc);
            Console.WriteLine(compact);
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates comparing different constraint policies.
        /// </summary>
        public static void DemoPolicyComparison()
        {
            Console.WriteLine("--- DEMO: Policy Comparison (Pop vs Jazz) ---");
            Console.WriteLine();

            // Same song structure, different policies
            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);

            // Create arcs with different policies
            var popPolicy = EnergyConstraintPolicyLibrary.GetPopRockPolicy();
            var jazzPolicy = EnergyConstraintPolicyLibrary.GetJazzPolicy();

            var popArc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42, constraintPolicy: popPolicy);
            var jazzArc = EnergyArc.Create(sectionTrack, "JazzSwing", seed: 42, constraintPolicy: jazzPolicy);

            // Compare side-by-side
            string comparison = EnergyConstraintDiagnostics.CompareArcs(popArc, jazzArc, "Pop Policy", "Jazz Policy");
            Console.WriteLine(comparison);
            Console.WriteLine();

            // Show the difference in constraints
            Console.WriteLine("Pop Policy Rules:");
            foreach (var rule in popPolicy.Rules)
            {
                Console.WriteLine($"  - {rule.RuleName} (strength: {rule.Strength:F2})");
            }
            Console.WriteLine();

            Console.WriteLine("Jazz Policy Rules:");
            foreach (var rule in jazzPolicy.Rules)
            {
                Console.WriteLine($"  - {rule.RuleName} (strength: {rule.Strength:F2})");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates visual energy chart.
        /// </summary>
        public static void DemoEnergyChart()
        {
            Console.WriteLine("--- DEMO: Visual Energy Chart ---");
            Console.WriteLine();

            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Intro, 4);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Bridge, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Outro, 4);

            var arc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            string chart = EnergyConstraintDiagnostics.GenerateEnergyChart(arc);
            Console.WriteLine(chart);
            Console.WriteLine();
        }

        /// <summary>
        /// Example: Using diagnostics to debug unexpected energy values.
        /// </summary>
        public static void DemoDebuggingScenario()
        {
            Console.WriteLine("--- DEMO: Debugging Scenario ---");
            Console.WriteLine("Question: Why is Verse 2 energy higher than expected?");
            Console.WriteLine();

            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);

            var arc = EnergyArc.Create(sectionTrack, "PopGroove", seed: 42);

            // Check Verse 2 specifically
            var verse2 = sectionTrack.Sections[1];
            var verse2Target = arc.GetTargetForSection(verse2, 1);
            var verse2Diagnostics = arc.GetConstraintDiagnostics(1);

            Console.WriteLine($"Verse 2 final energy: {verse2Target.Energy:F3}");
            Console.WriteLine();
            Console.WriteLine("Constraint diagnostics:");
            foreach (var diagnostic in verse2Diagnostics)
            {
                Console.WriteLine($"  • {diagnostic}");
            }
            Console.WriteLine();
            Console.WriteLine("Answer: SameTypeSectionsMonotonicRule ensures Verse 2 >= Verse 1");
            Console.WriteLine();
        }

        /// <summary>
        /// Example: Using diagnostics to tune policy parameters.
        /// </summary>
        public static void DemoTuningScenario()
        {
            Console.WriteLine("--- DEMO: Tuning Scenario ---");
            Console.WriteLine("Goal: Adjust Rock policy to have even stronger verse progression");
            Console.WriteLine();

            var sectionTrack = new SectionTrack();
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Verse, 8);
            sectionTrack.Add(MusicConstants.eSectionType.Chorus, 8);

            // Original Rock policy
            var originalPolicy = EnergyConstraintPolicyLibrary.GetRockPolicy();
            var originalArc = EnergyArc.Create(sectionTrack, "RockSteady", seed: 42, constraintPolicy: originalPolicy);

            Console.WriteLine("Original Rock Policy:");
            string originalSummary = EnergyConstraintDiagnostics.GenerateSummaryReport(originalArc);
            Console.WriteLine(originalSummary);

            // Custom policy with even stronger progression
            var customRules = new List<EnergyConstraintRule>
            {
                new SameTypeSectionsMonotonicRule(strength: 2.0, minIncrement: 0.10), // Much stronger!
                new FinalChorusPeakRule(strength: 2.0, minPeakEnergy: 0.90, peakProximityThreshold: 1.0)
            };
            var customPolicy = new EnergyConstraintPolicy
            {
                PolicyName = "CustomStrongRock",
                Rules = customRules,
                IsEnabled = true
            };
            var customArc = EnergyArc.Create(sectionTrack, "RockSteady", seed: 42, constraintPolicy: customPolicy);

            Console.WriteLine("Custom Strong Rock Policy:");
            string customSummary = EnergyConstraintDiagnostics.GenerateSummaryReport(customArc);
            Console.WriteLine(customSummary);

            // Compare
            string comparison = EnergyConstraintDiagnostics.CompareArcs(originalArc, customArc, "Original Rock", "Strong Rock");
            Console.WriteLine(comparison);
            Console.WriteLine();
        }
    }
}
