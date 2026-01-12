// AI: purpose=Comprehensive diagnostic output for tension decisions (Story 7.5.7); mirrors EnergyConstraintDiagnostics pattern.
// AI: invariants=Diagnostics must not affect generation (determinism maintained); output is human-readable.
// AI: deps=Consumes ITensionQuery (typically DeterministicTensionQuery); formats tension values and drivers.

namespace Music.Generator
{
    /// <summary>
    /// Provides comprehensive diagnostic output for tension decisions.
    /// Helps debugging and tuning of tension planning (Story 7.5.7).
    /// CRITICAL: Diagnostics must not affect generation; purely reporting.
    /// </summary>
    public static class TensionDiagnostics
    {
        /// <summary>
        /// Generates a full diagnostic report showing macro tension, micro tension summaries,
        /// tension drivers, and phrase flags per section.
        /// </summary>
        /// <param name="tensionQuery">The tension query to diagnose (must implement ITensionQuery).</param>
        /// <param name="sectionTrack">The section track (for section names and bar counts).</param>
        /// <param name="includeAllSections">Whether to include sections with default/neutral tension.</param>
        /// <returns>Formatted diagnostic report as a string.</returns>
        public static string GenerateFullReport(
            ITensionQuery tensionQuery,
            SectionTrack sectionTrack,
            bool includeAllSections = true)
        {
            ArgumentNullException.ThrowIfNull(tensionQuery);
            ArgumentNullException.ThrowIfNull(sectionTrack);

            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== Tension Diagnostic Report ===");
            report.AppendLine();
            
            // Query type information
            string queryType = tensionQuery.GetType().Name;
            report.AppendLine($"Query Type: {queryType}");
            report.AppendLine($"Total Sections: {tensionQuery.SectionCount}");
            report.AppendLine();

            report.AppendLine("Section-by-Section Analysis:");
            report.AppendLine(new string('-', 80));

            for (int i = 0; i < tensionQuery.SectionCount && i < sectionTrack.Sections.Count; i++)
            {
                var section = sectionTrack.Sections[i];
                
                if (!tensionQuery.HasTensionData(i))
                    continue;

                // Get macro tension profile
                var macroProfile = tensionQuery.GetMacroTension(i);
                
                // Get micro tension map
                var microMap = tensionQuery.GetMicroTensionMap(i);
                
                // Calculate micro tension stats
                double microMin = double.MaxValue;
                double microMax = double.MinValue;
                double microSum = 0.0;
                int microCount = 0;
                
                for (int barIdx = 0; barIdx < section.BarCount; barIdx++)
                {
                    double tension = microMap.GetTension(barIdx);
                    microMin = Math.Min(microMin, tension);
                    microMax = Math.Max(microMax, tension);
                    microSum += tension;
                    microCount++;
                }
                
                double microAvg = microCount > 0 ? microSum / microCount : 0.0;
                
                // Check if tension is "interesting" (non-neutral)
                bool hasNonNeutralMacro = Math.Abs(macroProfile.MacroTension - 0.5) > 0.05;
                bool hasNonFlatMicro = Math.Abs(microMax - microMin) > 0.01;
                bool hasDrivers = macroProfile.Driver != TensionDriver.None;
                
                // Skip neutral sections if requested
                if (!includeAllSections && !hasNonNeutralMacro && !hasNonFlatMicro && !hasDrivers)
                    continue;

                // Section header
                int sectionIndex = GetSectionIndex(sectionTrack, section.SectionType, i);
                string sectionName = $"{section.SectionType} {sectionIndex + 1}";
                if (!string.IsNullOrEmpty(section.Name))
                    sectionName += $" ({section.Name})";

                report.AppendLine();
                report.AppendLine($"Section #{i}: {sectionName}");
                report.AppendLine($"  Bars: {section.StartBar}-{section.StartBar + section.BarCount - 1} ({section.BarCount} bars)");
                report.AppendLine();
                
                // Macro tension
                report.AppendLine($"  Macro Tension:     {macroProfile.MacroTension:F3}");
                report.AppendLine($"  Micro Default:     {macroProfile.MicroTensionDefault:F3}");
                
                if (macroProfile.Driver != TensionDriver.None)
                {
                    report.AppendLine($"  Drivers:           {FormatTensionDrivers(macroProfile.Driver)}");
                }
                
                report.AppendLine();
                
                // Micro tension summary
                report.AppendLine($"  Micro Tension Map ({section.BarCount} bars):");
                report.AppendLine($"    Min:  {microMin:F3}");
                report.AppendLine($"    Max:  {microMax:F3}");
                report.AppendLine($"    Avg:  {microAvg:F3}");
                report.AppendLine($"    Range: {microMax - microMin:F3}");
                
                // Phrase flags summary
                int phraseEndCount = 0;
                bool isSectionStart = false;
                bool isSectionEnd = false;
                
                for (int barIdx = 0; barIdx < section.BarCount; barIdx++)
                {
                    var (isPhraseEnd, isSectEnd, isSectStart) = tensionQuery.GetPhraseFlags(i, barIdx);
                    if (isPhraseEnd) phraseEndCount++;
                    if (isSectStart) isSectionStart = true;
                    if (isSectEnd) isSectionEnd = true;
                }
                
                if (phraseEndCount > 0 || isSectionStart || isSectionEnd)
                {
                    report.AppendLine();
                    report.AppendLine($"  Phrase Flags:");
                    report.AppendLine($"    Phrase ends:    {phraseEndCount}");
                    report.AppendLine($"    Section start:  {(isSectionStart ? "Yes" : "No")}");
                    report.AppendLine($"    Section end:    {(isSectionEnd ? "Yes" : "No")}");
                }
            }

            report.AppendLine();
            report.AppendLine(new string('=', 80));
            
            return report.ToString();
        }

        /// <summary>
        /// Generates a summary report showing overall tension flow across sections.
        /// </summary>
        public static string GenerateSummaryReport(
            ITensionQuery tensionQuery,
            SectionTrack sectionTrack)
        {
            ArgumentNullException.ThrowIfNull(tensionQuery);
            ArgumentNullException.ThrowIfNull(sectionTrack);

            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== Tension Summary ===");
            report.AppendLine();
            report.AppendLine($"Query Type: {tensionQuery.GetType().Name}");
            report.AppendLine($"Sections: {tensionQuery.SectionCount}");
            report.AppendLine();

            // Collect macro tensions by section type
            var tensionsByType = new Dictionary<MusicConstants.eSectionType, List<(int Index, double Tension)>>();
            
            for (int i = 0; i < tensionQuery.SectionCount && i < sectionTrack.Sections.Count; i++)
            {
                if (!tensionQuery.HasTensionData(i))
                    continue;
                    
                var section = sectionTrack.Sections[i];
                var macroProfile = tensionQuery.GetMacroTension(i);
                int sectionIndex = GetSectionIndex(sectionTrack, section.SectionType, i);
                
                if (!tensionsByType.ContainsKey(section.SectionType))
                    tensionsByType[section.SectionType] = new List<(int, double)>();
                
                tensionsByType[section.SectionType].Add((sectionIndex + 1, macroProfile.MacroTension));
            }

            // Report macro tension progression by type
            report.AppendLine("Macro Tension Progression by Section Type:");
            foreach (var kvp in tensionsByType.OrderBy(x => x.Key))
            {
                var sectionType = kvp.Key;
                var instances = kvp.Value;
                
                report.Append($"  {sectionType}: ");
                report.AppendLine(string.Join(" ? ", instances.Select(i => $"{i.Index}:{i.Tension:F2}")));
            }

            report.AppendLine();
            
            // Find tension peak
            double maxTension = 0;
            int maxTensionSection = -1;
            string maxTensionSectionName = "";
            
            for (int i = 0; i < tensionQuery.SectionCount && i < sectionTrack.Sections.Count; i++)
            {
                if (!tensionQuery.HasTensionData(i))
                    continue;
                    
                var section = sectionTrack.Sections[i];
                var macroProfile = tensionQuery.GetMacroTension(i);
                int sectionIndex = GetSectionIndex(sectionTrack, section.SectionType, i);
                
                if (macroProfile.MacroTension > maxTension)
                {
                    maxTension = macroProfile.MacroTension;
                    maxTensionSection = i;
                    maxTensionSectionName = $"{section.SectionType} {sectionIndex + 1}";
                }
            }
            
            if (maxTensionSection >= 0)
            {
                report.AppendLine($"Tension Peak: {maxTension:F3} at Section #{maxTensionSection} ({maxTensionSectionName})");
            }
            
            // Count sections with significant tension (> 0.6)
            int highTensionCount = 0;
            for (int i = 0; i < tensionQuery.SectionCount; i++)
            {
                if (!tensionQuery.HasTensionData(i))
                    continue;
                    
                var macroProfile = tensionQuery.GetMacroTension(i);
                if (macroProfile.MacroTension > 0.6)
                    highTensionCount++;
            }
            
            report.AppendLine($"High Tension Sections (>0.6): {highTensionCount}/{tensionQuery.SectionCount}");
            
            // Driver summary
            var driverCounts = new Dictionary<TensionDriver, int>();
            for (int i = 0; i < tensionQuery.SectionCount; i++)
            {
                if (!tensionQuery.HasTensionData(i))
                    continue;
                    
                var macroProfile = tensionQuery.GetMacroTension(i);
                if (macroProfile.Driver != TensionDriver.None)
                {
                    // Count each flag separately
                    foreach (TensionDriver driver in Enum.GetValues(typeof(TensionDriver)))
                    {
                        if (driver != TensionDriver.None && macroProfile.Driver.HasFlag(driver))
                        {
                            if (!driverCounts.ContainsKey(driver))
                                driverCounts[driver] = 0;
                            driverCounts[driver]++;
                        }
                    }
                }
            }
            
            if (driverCounts.Count > 0)
            {
                report.AppendLine();
                report.AppendLine("Tension Drivers Applied:");
                foreach (var kvp in driverCounts.OrderByDescending(x => x.Value))
                {
                    report.AppendLine($"  {kvp.Key}: {kvp.Value} sections");
                }
            }
            
            return report.ToString();
        }

        /// <summary>
        /// Generates a compact one-line-per-section report suitable for logging.
        /// </summary>
        public static string GenerateCompactReport(
            ITensionQuery tensionQuery,
            SectionTrack sectionTrack)
        {
            ArgumentNullException.ThrowIfNull(tensionQuery);
            ArgumentNullException.ThrowIfNull(sectionTrack);

            var report = new System.Text.StringBuilder();
            
            report.AppendLine($"Tension Query: {tensionQuery.GetType().Name}");
            
            for (int i = 0; i < tensionQuery.SectionCount && i < sectionTrack.Sections.Count; i++)
            {
                if (!tensionQuery.HasTensionData(i))
                    continue;
                    
                var section = sectionTrack.Sections[i];
                var macroProfile = tensionQuery.GetMacroTension(i);
                int sectionIndex = GetSectionIndex(sectionTrack, section.SectionType, i);
                
                // Mark sections with drivers
                string marker = macroProfile.Driver != TensionDriver.None ? "*" : " ";
                
                // Get micro tension range
                var microMap = tensionQuery.GetMicroTensionMap(i);
                double microMin = double.MaxValue;
                double microMax = double.MinValue;
                
                for (int barIdx = 0; barIdx < section.BarCount; barIdx++)
                {
                    double tension = microMap.GetTension(barIdx);
                    microMin = Math.Min(microMin, tension);
                    microMax = Math.Max(microMax, tension);
                }
                
                report.AppendLine($"{marker} #{i:D2} {section.SectionType,-8} {sectionIndex + 1}: " +
                    $"Macro={macroProfile.MacroTension:F3} Micro=[{microMin:F3}-{microMax:F3}]");
            }
            
            return report.ToString();
        }

        /// <summary>
        /// Generates a transition hint summary showing section boundary feel.
        /// Requires DeterministicTensionQuery to access transition hints.
        /// </summary>
        public static string GenerateTransitionHintSummary(
            DeterministicTensionQuery tensionQuery,
            SectionTrack sectionTrack)
        {
            ArgumentNullException.ThrowIfNull(tensionQuery);
            ArgumentNullException.ThrowIfNull(sectionTrack);

            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== Section Transition Hints ===");
            report.AppendLine();

            for (int i = 0; i < tensionQuery.SectionCount && i < sectionTrack.Sections.Count; i++)
            {
                var section = sectionTrack.Sections[i];
                int sectionIndex = GetSectionIndex(sectionTrack, section.SectionType, i);
                
                var hint = tensionQuery.GetTransitionHint(i);
                
                string sectionName = $"{section.SectionType} {sectionIndex + 1}";
                report.AppendLine($"  Section #{i} ({sectionName}): {hint}");
            }
            
            return report.ToString();
        }

        // Helper methods
        
        private static int GetSectionIndex(SectionTrack sectionTrack, MusicConstants.eSectionType type, int absoluteIndex)
        {
            int count = 0;
            for (int i = 0; i <= absoluteIndex && i < sectionTrack.Sections.Count; i++)
            {
                if (sectionTrack.Sections[i].SectionType == type)
                {
                    if (i == absoluteIndex)
                        return count;
                    count++;
                }
            }
            return 0;
        }

        private static string FormatTensionDrivers(TensionDriver drivers)
        {
            if (drivers == TensionDriver.None)
                return "None";

            var parts = new List<string>();
            foreach (TensionDriver driver in Enum.GetValues(typeof(TensionDriver)))
            {
                if (driver != TensionDriver.None && drivers.HasFlag(driver))
                {
                    parts.Add(driver.ToString());
                }
            }

            return string.Join(", ", parts);
        }
    }
}
