namespace Music.Generator.Groove
{
    // AI: purpose=Container for multiple groove presets by name; enables mid-song preset switching via object.
    // AI: invariants=Presets keyed by Identity.Name (case-insensitive); DefaultPresetName fallback when segment has no preset.
    // AI: usage=Add presets via Add(); lookup via TryGetPreset(); GetPresetForBar resolves bar->preset via segment profiles.
    // AI: change=Add presets with unique Identity.Name; DefaultPresetName used when segment.GroovePresetName is null/not found.
    public sealed class GroovePresetLibrary
    {
        public Dictionary<string, GroovePresetDefinition> Presets { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string? DefaultPresetName { get; set; }

        public void Add(GroovePresetDefinition preset)
        {
            ArgumentNullException.ThrowIfNull(preset);
            var name = preset.Identity?.Name ?? throw new ArgumentException("Preset must have Identity.Name", nameof(preset));
            Presets[name] = preset;
        }

        public bool TryGetPreset(string? name, out GroovePresetDefinition? preset)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (!string.IsNullOrEmpty(DefaultPresetName) && Presets.TryGetValue(DefaultPresetName, out preset))
                    return true;

                preset = null;
                return false;
            }

            return Presets.TryGetValue(name, out preset);
        }

        // AI: Story 5.2: Method disabled - will be deleted in Story 5.4
        // GetPresetForBar resolves bar->preset via segment profiles; returns default if no segment covers bar.
        public GroovePresetDefinition? GetPresetForBar(int bar, IReadOnlyList<object>? segmentProfiles)
        {
            // Story 5.2: SegmentGrooveProfile removed - return default preset
            TryGetPreset(DefaultPresetName, out var defaultPreset);
            return defaultPreset;
        }
    }
}
