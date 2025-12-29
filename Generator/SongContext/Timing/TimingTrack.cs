namespace Music.Generator
{
    // This is a design track for time signature
    // Global bar/beat-aligned time signature track
    public class Timingtrack
    {
        public List<TimingEvent> Events { get; set; } = new();

        public void Add(TimingEvent evt)
        {
            Events.Add(evt);
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
