namespace Music.Designer
{
    // Holds the collection of voices used by the score
    public sealed class VoiceSet
    {
        // List of valid groove roles (used to populate combo boxes, etc.)
        public static IReadOnlyList<string> ValidGrooveRoles { get; } = new List<string>
        {
            "Select...",
            "Pads",
            "Comp",
            "Bass",
            "DrumKit"
        };

        public List<Voice> Voices { get; set; } = new();

        public void Reset() => Voices.Clear();

        public void AddVoice(string voiceName, string grooveRole = "")
        {
            if (string.IsNullOrWhiteSpace(voiceName))
                throw new ArgumentException("Voice name must not be null or empty.", nameof(voiceName));

            var voice = new Voice { VoiceName = voiceName, GrooveRole = grooveRole };
            Voices.Add(voice);
        }

        public IReadOnlyList<Voice> SetTestVoicesD1()
        {
            AddVoice("Techno Synth", "Pads");
            AddVoice("Electric Guitar", "Comp");
            AddVoice("Electric Bass", "Bass");
            AddVoice("Drum Set", "DrumKit");  // MIDI track 10 reserved for drum set. Does not use Program Number. Then each note is different percussion.
            return Voices;
        }
    }
}