namespace Music.Generator
{
    /// <summary>
    /// Harmony events for the song track.
    /// </summary>
    public sealed class ProposedSongHarmonyTrack
    {
        /// <summary>
        /// Ordered list of harmony events.
        /// </summary>
        public List<SongHarmonyEvent> Events { get; set; }

        public ProposedSongHarmonyTrack()
        {
            Events = new List<SongHarmonyEvent>();
        }
    }

    /// <summary>
    /// A single harmony event in the song, representing an active chord.
    /// </summary>
    public sealed class SongHarmonyEvent
    {
        /// <summary>
        /// Bar number where this harmony starts (1-based).
        /// </summary>
        public int StartBar { get; set; }

        /// <summary>
        /// Beat within the bar (1-based).
        /// </summary>
        public int StartBeat { get; set; }

        /// <summary>
        /// Key (e.g., "C major", "A minor").
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Scale degree (1-7).
        /// </summary>
        public int Degree { get; set; }

        /// <summary>
        /// Chord quality (e.g., "maj", "min7", "dom7").
        /// </summary>
        public string Quality { get; set; }

        /// <summary>
        /// Bass/inversion setting (e.g., "root", "3rd", "5th").
        /// </summary>
        public string Bass { get; set; }

        /// <summary>
        /// Duration in beats.
        /// </summary>
        public int DurationBeats { get; set; }

        /// <summary>
        /// Absolute position in ticks.
        /// </summary>
        public long AbsolutePositionTicks { get; set; }

        // ============ Computed Pitch Context (populated during generation) ============

        /// <summary>
        /// Key root pitch class (0-11).
        /// </summary>
        public int KeyRootPitchClass { get; set; }

        /// <summary>
        /// Chord root pitch class (0-11).
        /// </summary>
        public int ChordRootPitchClass { get; set; }

        /// <summary>
        /// Pitch classes in the chord (0-11).
        /// </summary>
        public List<int> ChordPitchClasses { get; set; }

        /// <summary>
        /// Pitch classes in the key scale (0-11).
        /// </summary>
        public List<int> KeyScalePitchClasses { get; set; }

        public SongHarmonyEvent()
        {
            StartBar = 1;
            StartBeat = 1;
            Key = "C major";
            Degree = 1;
            Quality = "maj";
            Bass = "root";
            DurationBeats = 4;
            ChordPitchClasses = new List<int>();
            KeyScalePitchClasses = new List<int>();
        }
    }
}