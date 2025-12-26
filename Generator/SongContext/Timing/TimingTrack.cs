namespace Music.Generator
{
    // This is a design track for time signature
    // Global bar/beat-aligned time signature track
    public class Timingtrack
    {
        // TO DO SUPER HIGH IMPORTANT!
        //   THIS HAS THAT OLD barhead code and globals code in it - remove
        //   Add all the start measure ticks, beats per bar, ticksperbar
        //      and any other timing related tick related calcs that are per bar
        //         - calculate and save for every individual measure for ? measures (100?, length of song - may not be known? something else?)
        //          must do here because this is where the time signatures are edited
        //   Then lookup is just the bar index
        //   Only show one entry in the grid, i.e. dont show the subsequent duplicates - they are hidden!

        private readonly Dictionary<int, TimingEvent> _barHeads = new(); // bar -> event active at beat 1

        public int BeatsPerBar { get; set; } = 4;

        public List<TimingEvent> Events { get; set; } = new();

        public void ConfigureGlobal(string meter)
        {
            // Expect "x/y". For now, only x matters for bar length in beats.
            if (string.IsNullOrWhiteSpace(meter)) throw new ArgumentException(nameof(meter));
            var parts = meter.Split('/');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var beats))
                throw new ArgumentException("Invalid meter format. Expected like \"4/4\".", nameof(meter));

            BeatsPerBar = Math.Max(1, beats);
        }

        public void Add(TimingEvent evt)
        {
            Events.Add(evt);
            // Clear cache when adding new events
            _barHeads.Clear();
        }

        /// <summary>
        /// Gets the active time signature event for a given bar.
        /// Returns the most recent time signature event that starts on or before this bar.
        /// </summary>
        public TimingEvent? GetActiveTimeSignatureEvent(int bar)
        {
            if (bar < 1) throw new ArgumentOutOfRangeException(nameof(bar));

            for (int i = Events.Count - 1; i >= 0; i--)
            {
                var evt = Events[i];
                var eventStartBar = evt.StartBar;

                if (eventStartBar <= bar)
                {
                    return evt;
                }
            }

            return null;
        }
    }
}
