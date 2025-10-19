namespace Music.Generate
{
    // Holds the collection of voices used by the score
    public sealed class VoiceSetClass
    {
        public readonly List<VoiceClass> _voices = new();
        public IReadOnlyList<VoiceClass> Voices => _voices;

        public void Reset() => _voices.Clear();

        public void AddVoice(string voiceName)
        {
            if (string.IsNullOrWhiteSpace(voiceName))
                throw new ArgumentException("Voice name must not be null or empty.", nameof(voiceName));

            var voice = new VoiceClass { VoiceName = voiceName };
            _voices.Add(voice);
        }

        public IReadOnlyList<VoiceClass> AddDefaultVoices()
        {
            AddVoice("Guitar");
            AddVoice("Drum Set");
            AddVoice("Keyboard");
            AddVoice("Base Guitar"); // per requirement
            return Voices;
        }
    }
}