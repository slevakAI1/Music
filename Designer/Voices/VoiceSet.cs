namespace Music.Designer
{
    // Holds the collection of voices used by the score
    public sealed class VoiceSet
    {
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
            AddVoice("Acoustic Grand Piano", "Pads");
            AddVoice("Electric Guitar (clean)", "Comp");
            AddVoice("Electric Bass (finger)", "Bass");
            AddVoice("Drum Set", "DrumKit");  // MIDI track 10 reserved for drum set. Does not use Program Number. Then each note is different percussion.
            return Voices;
        }
    }
}