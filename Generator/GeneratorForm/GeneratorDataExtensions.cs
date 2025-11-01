using System;
using System.Collections.Generic;
using Music.Designer;
using Music.Generator;

namespace Music.Generator
{
    public static class GeneratorDataExtensions
    {
        // Applies design-driven defaults into an existing GeneratorData instance.
        // Accept a nullable receiver so callers holding a `GeneratorData?` (e.g. Globals.GenerationData)
        // can call this extension directly without a null-forgiving operator.
        public static void ApplyDesignDefaults(this GeneratorData? data, DesignerData? design)
        {
            if (data == null) return;

            var available = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (design?.PartSet?.Parts != null)
            {
                foreach (var v in design.PartSet.Parts)
                {
                    var name = v?.PartName;
                    if (!string.IsNullOrWhiteSpace(name))
                        available.Add(name!);
                }
            }

            var existing = data.PartsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var newState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (existing.Count == 0)
            {
                foreach (var name in available)
                    newState[name] = true;
            }
            else
            {
                foreach (var name in available)
                {
                    if (existing.TryGetValue(name, out var isChecked))
                        newState[name] = isChecked;
                    else
                        newState[name] = false;
                }
            }

            data.PartsState = newState;

            var total = design?.SectionSet?.TotalBars ?? 0;
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