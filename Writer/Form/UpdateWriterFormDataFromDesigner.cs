using Melanchall.DryWetMidi.Interaction;
using Music.Designer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Music.Writer
{
    public static class UpdateWriterFormDataFromDesigner
    {
        // Applies design-driven defaults into an existing Writer instance.
        // Accept a nullable receiver so callers holding a `Writer?` (e.g. Globals.GenerationData)
        // can call this extension directly without a null-forgiving operator.
        public static void Update(this WriterFormData? data, Designer.Designer? design)
        {
            if (data == null) return;

            // ========== PARTS SYNCHRONIZATION ==========
            // Update the Writer parts to be in sync with the Designer parts if there are differences.
            // Preserve existing checked states

            var availableParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (design?.Voices?.Voices != null)
            {
                foreach (var v in design.Voices.Voices)
                {
                    var name = v?.VoiceName;
                    if (!string.IsNullOrWhiteSpace(name))
                        availableParts.Add(name!);
                }
            }

            var existingParts = data.PartsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var newPartsState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (existingParts.Count == 0)
            {
                // No existing state: default all parts to checked
                foreach (var name in availableParts)
                    newPartsState[name] = true;
            }
            else
            {
                // Preserve existing checked states for parts that still exist
                foreach (var name in availableParts)
                {
                    if (existingParts.TryGetValue(name, out var isChecked))
                        newPartsState[name] = isChecked;
                    else
                        newPartsState[name] = false; // New parts default to unchecked
                }
            }

            data.PartsState = newPartsState;

            // ========== SECTIONS SYNCHRONIZATION ==========
            // Update the Writer sections to be in sync with the Designer sections if there are differences.
            // Preserve existing checked states for sections

            var availableSections = new List<string>();
            if (design?.SectionTimeline?.Sections != null)
            {
                foreach (var section in design.SectionTimeline.Sections)
                {
                    var name = section?.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                        availableSections.Add(name!);
                }
            }

            var existingSections = data.SectionsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var newSectionsState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (existingSections.Count == 0)
            {
                // No existing state: default all sections to unchecked (unlike parts, sections default to unchecked)
                foreach (var name in availableSections)
                    newSectionsState[name] = false;
            }
            else
            {
                // Preserve existing checked states for sections that still exist
                foreach (var name in availableSections)
                {
                    if (existingSections.TryGetValue(name, out var isChecked))
                        newSectionsState[name] = isChecked;
                    else
                        newSectionsState[name] = false; // New sections default to unchecked
                }
            }

            data.SectionsState = newSectionsState;

            // ========== END BAR ADJUSTMENT ==========
            // Ensure EndBar is within valid range based on design's total bars

            var total = design?.SectionTimeline?.TotalBars ?? 0;
            if (total > 0)
            {
                if (!data.EndBar.HasValue)
                    data.EndBar = total;
                else
                    data.EndBar = Math.Max(1, Math.Min(total, data.EndBar.Value));
            }
        }
    }
}