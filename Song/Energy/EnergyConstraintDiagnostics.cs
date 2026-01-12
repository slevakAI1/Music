// AI: purpose=Comprehensive diagnostic output for energy constraint decisions (Story 7.4.4).
// AI: invariants=Diagnostics must not affect generation (determinism maintained); output is human-readable.
// AI: deps=Consumes EnergyArc, EnergyConstraintPolicy; formats constraint decision information.

namespace Music.Generator
{
    /// <summary>
    /// Provides comprehensive diagnostic output for energy constraint decisions.
    /// Helps debugging and tuning of energy arc and constraint policies (Story 7.4.4).
    /// </summary>
    public static class EnergyConstraintDiagnostics
    {
        /// <summary>
        /// Generates a full diagnostic report for an energy arc showing all constraint decisions.
        /// </summary>
        /// <param name="arc">The energy arc to diagnose.</param>
        /// <param name="includeUnchangedSections">Whether to include sections where no constraints made adjustments.</param>
        /// <returns>Formatted diagnostic report as a string.</returns>
        public static string GenerateFullReport(EnergyArc arc, bool includeUnchangedSections = true)
        {
            ArgumentNullException.ThrowIfNull(arc);

            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== Energy Constraint Diagnostic Report ===");
            report.AppendLine();
            report.AppendLine($"Arc Template: {arc.Template.Name}");
            report.AppendLine($"Groove: {arc.GrooveName}");
            report.AppendLine($"Policy: {arc.ConstraintPolicy.PolicyName} (Enabled: {arc.ConstraintPolicy.IsEnabled})");
            report.AppendLine($"Total Sections: {arc.SectionTrack.Sections.Count}");
            report.AppendLine();

            if (arc.ConstraintPolicy.IsEnabled)
            {
                report.AppendLine($"Active Rules ({arc.ConstraintPolicy.Rules.Count}):");
                foreach (var rule in arc.ConstraintPolicy.Rules)
                {
                    report.AppendLine($"  - {rule.RuleName} (strength: {rule.Strength:F2})");
                }
                report.AppendLine();
            }

            report.AppendLine("Section-by-Section Analysis:");
            report.AppendLine(new string('-', 80));

            for (int i = 0; i < arc.SectionTrack.Sections.Count; i++)
            {
                var section = arc.SectionTrack.Sections[i];
                int sectionIndex = GetSectionIndex(arc.SectionTrack, section.SectionType, i);
                
                // Get template energy (unconstrained)
                var templateTarget = arc.Template.GetTargetForSection(section.SectionType, sectionIndex);
                double templateEnergy = templateTarget.Energy;

                // Get constrained energy
                var constrainedTarget = arc.GetTargetForSection(section, sectionIndex);
                double constrainedEnergy = constrainedTarget.Energy;

                // Get diagnostics
                var diagnostics = arc.GetConstraintDiagnostics(i);

                // Check if energy changed
                bool energyChanged = Math.Abs(constrainedEnergy - templateEnergy) > 0.0001;

                // Skip unchanged sections if requested
                if (!includeUnchangedSections && !energyChanged && diagnostics.Count == 0)
                    continue;

                // Section header
                string sectionName = $"{section.SectionType} {sectionIndex + 1}";
                if (!string.IsNullOrEmpty(section.Name))
                    sectionName += $" ({section.Name})";

                report.AppendLine();
                report.AppendLine($"Section #{i}: {sectionName}");
                report.AppendLine($"  Bars: {section.StartBar}-{section.StartBar + section.BarCount - 1}");
                report.AppendLine($"  Template energy: {templateEnergy:F3}");
                report.AppendLine($"  Final energy:    {constrainedEnergy:F3}");

                if (energyChanged)
                {
                    double delta = constrainedEnergy - templateEnergy;
                    string direction = delta > 0 ? "?" : "?";
                    report.AppendLine($"  Change:          {direction} {Math.Abs(delta):F3} ({(delta / templateEnergy * 100):F1}%)");
                }
                else
                {
                    report.AppendLine($"  Change:          (none)");
                }

                if (diagnostics.Count > 0)
                {
                    report.AppendLine($"  Rules applied:");
                    foreach (var diagnostic in diagnostics)
                    {
                        report.AppendLine($"    • {diagnostic}");
                    }
                }
            }

            report.AppendLine();
            report.AppendLine(new string('=', 80));
            
            return report.ToString();
        }

        /// <summary>
        /// Generates a summary report showing overall energy flow and major adjustments.
        /// </summary>
        public static string GenerateSummaryReport(EnergyArc arc)
        {
            ArgumentNullException.ThrowIfNull(arc);

            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== Energy Arc Summary ===");
            report.AppendLine();
            report.AppendLine($"Policy: {arc.ConstraintPolicy.PolicyName}");
            report.AppendLine($"Sections: {arc.SectionTrack.Sections.Count}");
            report.AppendLine();

            // Collect energies by section type
            var energiesByType = new Dictionary<MusicConstants.eSectionType, List<(int Index, double Energy)>>();
            
            for (int i = 0; i < arc.SectionTrack.Sections.Count; i++)
            {
                var section = arc.SectionTrack.Sections[i];
                int sectionIndex = GetSectionIndex(arc.SectionTrack, section.SectionType, i);
                var target = arc.GetTargetForSection(section, sectionIndex);
                
                if (!energiesByType.ContainsKey(section.SectionType))
                    energiesByType[section.SectionType] = new List<(int, double)>();
                
                energiesByType[section.SectionType].Add((sectionIndex + 1, target.Energy));
            }

            // Report energy progression by type
            report.AppendLine("Energy Progression by Section Type:");
            foreach (var kvp in energiesByType.OrderBy(x => x.Key))
            {
                var sectionType = kvp.Key;
                var instances = kvp.Value;
                
                report.Append($"  {sectionType}: ");
                report.AppendLine(string.Join(" ? ", instances.Select(i => $"{i.Index}:{i.Energy:F2}")));
                
                // Show progression trend
                if (instances.Count > 1)
                {
                    bool allIncreasing = true;
                    for (int i = 1; i < instances.Count; i++)
                    {
                        if (instances[i].Energy < instances[i - 1].Energy - 0.001)
                        {
                            allIncreasing = false;
                            break;
                        }
                    }
                    
                    if (allIncreasing)
                        report.AppendLine("    (monotonic increase ?)");
                }
            }

            report.AppendLine();
            
            // Find energy peak
            double maxEnergy = 0;
            int maxEnergySection = -1;
            string maxEnergySectionName = "";
            
            for (int i = 0; i < arc.SectionTrack.Sections.Count; i++)
            {
                var section = arc.SectionTrack.Sections[i];
                int sectionIndex = GetSectionIndex(arc.SectionTrack, section.SectionType, i);
                var target = arc.GetTargetForSection(section, sectionIndex);
                
                if (target.Energy > maxEnergy)
                {
                    maxEnergy = target.Energy;
                    maxEnergySection = i;
                    maxEnergySectionName = $"{section.SectionType} {sectionIndex + 1}";
                }
            }
            
            report.AppendLine($"Energy Peak: {maxEnergy:F3} at Section #{maxEnergySection} ({maxEnergySectionName})");
            
            // Count adjustments
            int adjustmentCount = 0;
            for (int i = 0; i < arc.SectionTrack.Sections.Count; i++)
            {
                var section = arc.SectionTrack.Sections[i];
                int sectionIndex = GetSectionIndex(arc.SectionTrack, section.SectionType, i);
                
                var templateTarget = arc.Template.GetTargetForSection(section.SectionType, sectionIndex);
                var constrainedTarget = arc.GetTargetForSection(section, sectionIndex);
                
                if (Math.Abs(constrainedTarget.Energy - templateTarget.Energy) > 0.0001)
                    adjustmentCount++;
            }
            
            report.AppendLine($"Constraint Adjustments: {adjustmentCount}/{arc.SectionTrack.Sections.Count} sections");
            
            return report.ToString();
        }

        /// <summary>
        /// Generates a compact one-line-per-section report suitable for logging.
        /// </summary>
        public static string GenerateCompactReport(EnergyArc arc)
        {
            ArgumentNullException.ThrowIfNull(arc);

            var report = new System.Text.StringBuilder();
            
            report.AppendLine($"Energy Arc: {arc.ConstraintPolicy.PolicyName} policy");
            
            for (int i = 0; i < arc.SectionTrack.Sections.Count; i++)
            {
                var section = arc.SectionTrack.Sections[i];
                int sectionIndex = GetSectionIndex(arc.SectionTrack, section.SectionType, i);
                
                var templateTarget = arc.Template.GetTargetForSection(section.SectionType, sectionIndex);
                var constrainedTarget = arc.GetTargetForSection(section, sectionIndex);
                
                bool adjusted = Math.Abs(constrainedTarget.Energy - templateTarget.Energy) > 0.0001;
                string marker = adjusted ? "*" : " ";
                
                report.AppendLine($"{marker} #{i:D2} {section.SectionType,-8} {sectionIndex + 1}: " +
                    $"T={templateTarget.Energy:F3} ? F={constrainedTarget.Energy:F3}");
            }
            
            return report.ToString();
        }

        /// <summary>
        /// Compares two energy arcs (e.g., with different policies) side-by-side.
        /// </summary>
        public static string CompareArcs(EnergyArc arc1, EnergyArc arc2, string arc1Label = "Arc 1", string arc2Label = "Arc 2")
        {
            ArgumentNullException.ThrowIfNull(arc1);
            ArgumentNullException.ThrowIfNull(arc2);

            if (arc1.SectionTrack.Sections.Count != arc2.SectionTrack.Sections.Count)
            {
                throw new ArgumentException("Arcs must have same number of sections for comparison");
            }

            var report = new System.Text.StringBuilder();
            
            report.AppendLine($"=== Energy Arc Comparison: {arc1Label} vs {arc2Label} ===");
            report.AppendLine();
            report.AppendLine($"{arc1Label}: {arc1.ConstraintPolicy.PolicyName} policy");
            report.AppendLine($"{arc2Label}: {arc2.ConstraintPolicy.PolicyName} policy");
            report.AppendLine();
            report.AppendLine($"{"Section",-20} | {arc1Label,-12} | {arc2Label,-12} | Delta");
            report.AppendLine(new string('-', 70));

            for (int i = 0; i < arc1.SectionTrack.Sections.Count; i++)
            {
                var section = arc1.SectionTrack.Sections[i];
                int sectionIndex = GetSectionIndex(arc1.SectionTrack, section.SectionType, i);
                
                var target1 = arc1.GetTargetForSection(section, sectionIndex);
                var target2 = arc2.GetTargetForSection(section, sectionIndex);
                
                double delta = target2.Energy - target1.Energy;
                string deltaStr = delta >= 0 ? $"+{delta:F3}" : $"{delta:F3}";
                
                string sectionName = $"{section.SectionType} {sectionIndex + 1}";
                report.AppendLine($"{sectionName,-20} | {target1.Energy:F3,-12} | {target2.Energy:F3,-12} | {deltaStr}");
            }
            
            return report.ToString();
        }

        /// <summary>
        /// Generates a visual ASCII chart of energy across sections.
        /// </summary>
        public static string GenerateEnergyChart(EnergyArc arc, int chartHeight = 10)
        {
            ArgumentNullException.ThrowIfNull(arc);

            var report = new System.Text.StringBuilder();
            
            report.AppendLine("Energy Flow Chart:");
            report.AppendLine();

            // Collect energies
            var energies = new List<double>();
            for (int i = 0; i < arc.SectionTrack.Sections.Count; i++)
            {
                var section = arc.SectionTrack.Sections[i];
                int sectionIndex = GetSectionIndex(arc.SectionTrack, section.SectionType, i);
                var target = arc.GetTargetForSection(section, sectionIndex);
                energies.Add(target.Energy);
            }

            // Draw chart (0.0 at bottom, 1.0 at top)
            for (int row = chartHeight; row >= 0; row--)
            {
                double threshold = row / (double)chartHeight;
                report.Append($"{threshold:F1} |");
                
                foreach (var energy in energies)
                {
                    if (energy >= threshold)
                        report.Append("?");
                    else if (energy >= threshold - (0.5 / chartHeight))
                        report.Append("?");
                    else
                        report.Append(" ");
                }
                
                report.AppendLine();
            }
            
            // X-axis
            report.Append("    +");
            report.AppendLine(new string('-', energies.Count));
            
            // Section labels
            report.Append("     ");
            for (int i = 0; i < arc.SectionTrack.Sections.Count; i++)
            {
                var section = arc.SectionTrack.Sections[i];
                report.Append(section.SectionType.ToString()[0]);
            }
            report.AppendLine();
            
            return report.ToString();
        }

        /// <summary>
        /// Helper to get section index within same-type sections.
        /// </summary>
        private static int GetSectionIndex(SectionTrack sectionTrack, MusicConstants.eSectionType sectionType, int absoluteIndex)
        {
            int count = 0;
            for (int i = 0; i <= absoluteIndex; i++)
            {
                if (sectionTrack.Sections[i].SectionType == sectionType)
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
