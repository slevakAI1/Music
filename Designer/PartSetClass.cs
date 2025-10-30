namespace Music.Design
{
    // Holds the collection of voices used by the score
    public sealed class PartSetClass
    {
        public List<VoiceClass> Parts { get; set; } = new();

        public void Reset() => Parts.Clear();

        public void AddVoice(string voiceName)
        {
            if (string.IsNullOrWhiteSpace(voiceName))
                throw new ArgumentException("Voice name must not be null or empty.", nameof(voiceName));

            var voice = new VoiceClass { PartName = voiceName };
            Parts.Add(voice);
        }

        public IReadOnlyList<VoiceClass> SetTestVoicesD1()
        {
            AddVoice("Guitar");
            AddVoice("Flute");
            AddVoice("Keyboard");
            AddVoice("Base Guitar"); // per requirement
            return Parts;
        }
    }
}