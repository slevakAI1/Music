namespace Music.Generate
{
    // Holds the collection of voices used by the score
    public sealed class VoiceSetClass
    {
        private readonly List<ScoreDesignClass.Voice> _voices = new();
        public IReadOnlyList<ScoreDesignClass.Voice> Voices => _voices;

        public void Reset() => _voices.Clear();

        public ScoreDesignClass.Voice AddVoice(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Voice value must not be null or empty.", nameof(value));

            foreach (var v in _voices)
            {
                if (string.Equals(v.Value, value, StringComparison.Ordinal))
                    return v;
            }

            var voice = new ScoreDesignClass.Voice(
                Id: Guid.NewGuid().ToString("N"),
                Value: value);

            _voices.Add(voice);
            return voice;
        }

        public IReadOnlyList<ScoreDesignClass.Voice> AddDefaultVoices()
        {
            AddVoice("Guitar");
            AddVoice("Drum Set");
            AddVoice("Keyboard");
            AddVoice("Base Guitar"); // per requirement
            return Voices;
        }
    }
}