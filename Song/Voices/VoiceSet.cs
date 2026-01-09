// AI: purpose=Manage collection of playable voices; maps VoiceName (MIDI program) to a GrooveRole for UI and generation.
// AI: invariants=VoiceName is required and treated as identifier; ValidGrooveRoles populates UI dropdowns and should be stable.
// AI: deps=Consumed by UI, presets, and serializers; renaming properties or changing semantics breaks persistence and mappings.
// AI: change=When adding groove roles update ValidGrooveRoles and UI code that relies on its ordering.

namespace Music.Generator
{
    // AI: lightweight container with helpers; keep methods minimal and predictable for tests and serialization.
    public sealed class VoiceSet
    {
        // AI: ValidGrooveRoles: used to populate combo boxes; first entry is placeholder and must remain.
        public static IReadOnlyList<string> ValidGrooveRoles { get; } = new List<string>
        {
            "Select...",
            "Pads",
            "Comp",
            "Bass",
            "DrumKit"
        };

        // AI: Voices list is mutable and used directly by UI and exporters; order may affect displayed lists.
        public List<Voice> Voices { get; set; } = new();

        // AI: Reset clears the set but keeps the instance for reuse in editors/tests.
        public void Reset() => Voices.Clear();

        // AI: AddVoice: VoiceName required (non-empty) and used as identity; throws ArgumentException on invalid input.
        public void AddVoice(string voiceName, string grooveRole = "")
        {
            if (string.IsNullOrWhiteSpace(voiceName))
                throw new ArgumentException("Voice name must not be null or empty.", nameof(voiceName));

            var voice = new Voice { VoiceName = voiceName, GrooveRole = grooveRole };
            Voices.Add(voice);
        }

        // AI: SetTestVoicesD1: convenience to populate a canonical test voice set; order and names expected by demos/tests.
        public IReadOnlyList<Voice> SetTestVoicesD1()
        {
            AddVoice("Electric Piano 1", "Pads");
            AddVoice("Electric Guitar (muted)", "Comp");
            AddVoice("Electric Bass (finger)", "Bass");
            AddVoice("Drum Set", "DrumKit");  // MIDI track 10 reserved for drum set; percussion mapped by note number
            return Voices;
        }
    }
}