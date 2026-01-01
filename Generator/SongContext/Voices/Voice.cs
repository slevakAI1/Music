// AI: purpose=Model a playable voice mapping to a MIDI program and a groove role used by arrangers/UI.
// AI: invariants=VoiceName is required and must equal the MIDI program name; GrooveRole is optional mapping string.
// AI: deps=Consumed by UI, preset mapping, and serializers; renaming properties breaks persistence and presets.
// AI: change=If adding fields update UI mapping, preset serializers, and any voice lookup tables.

namespace Music.Generator
{
    // AI: DTO: keep properties minimal and stable; used as key in presets and to select MIDI program mappings.
    public class Voice
    {
        // AI: VoiceName: required identifier; intentionally mirrors MIDI program name used across UI and presets.
        public required string VoiceName { get; set; }

        // AI: GrooveRole: optional role tag (e.g., "bass","guitar") used to map groove layers to voices.
        public string GrooveRole { get; set; }
    }
}
