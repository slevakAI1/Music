// AI: purpose=Declarative groove preset producing per-role onset lists; used to instantiate GrooveEvent for song application.
// AI: invariants=BeatsPerBar is declarative only; callers must ensure it matches song time signature when applying preset.
// AI: deps=Consumed by groove application logic and Generator; changing property names/types breaks callers and persisted presets.
// AI: change=If adding fields, update persistence and any UI that serializes presets.

namespace Music.Generator
{
    // AI: GroovePreset is a lightweight, declarative definition; it does not normalize or validate onset lists.
    public sealed class GroovePreset
    {
        // AI: Name: human label, may be empty; not guaranteed unique and should not be used as identity key.
        public string Name { get; init; } = string.Empty;

        // AI: BeatsPerBar: time-signature numerator for interpreting onsets; callers must validate positive values.
        public int BeatsPerBar { get; init; } = 4;

        // AI: AnchorLayer: primary pattern; lists inside may be unsorted/duplicated and must be normalized by consumer.
        public GrooveInstanceLayer AnchorLayer { get; init; } = new();

        // AI: TensionLayer: optional variations; presence does not imply automatic application - consumers decide merge rules.
        public GrooveInstanceLayer TensionLayer { get; init; } = new();
    }
}