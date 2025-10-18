using System;
using System.Collections.Generic;

namespace Music.Generate
{
    // Holds the collection of voices used by the score
    public sealed class VoiceSet
    {
        private readonly List<ScoreDesign.Voice> _voices = new();
        public IReadOnlyList<ScoreDesign.Voice> Voices => _voices;

        public void Reset() => _voices.Clear();

        public ScoreDesign.Voice AddVoice(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Voice value must not be null or empty.", nameof(value));

            foreach (var v in _voices)
            {
                if (string.Equals(v.Value, value, StringComparison.Ordinal))
                    return v;
            }

            var voice = new ScoreDesign.Voice(
                Id: Guid.NewGuid().ToString("N"),
                Value: value);

            _voices.Add(voice);
            return voice;
        }

        public IReadOnlyList<ScoreDesign.Voice> AddDefaultVoices()
        {
            AddVoice("Guitar");
            AddVoice("Drum Set");
            AddVoice("Keyboard");
            AddVoice("Base Guitar"); // per requirement
            return Voices;
        }
    }
}