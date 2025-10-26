namespace Music.Design
{
    // Holds the collection of voices used by the score
    public sealed class VoiceSetClass
    {
        public List<VoiceClass> Voices { get; set; } = new();

        public void Reset() => Voices.Clear();

        public void AddVoice(string voiceName)
        {
            if (string.IsNullOrWhiteSpace(voiceName))
                throw new ArgumentException("Voice name must not be null or empty.", nameof(voiceName));

            var voice = new VoiceClass { VoiceName = voiceName };
            Voices.Add(voice);
        }

        public IReadOnlyList<VoiceClass> AddDefaultVoices()
        {
            //AddVoice("Guitar");
            //AddVoice("Drum Set");
            AddVoice("Keyboard");
            //AddVoice("Base Guitar"); // per requirement
            return Voices;
        }
    }
}