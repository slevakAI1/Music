namespace Music.Generator
{
    // This is a design track for Harmony

    public class HarmonyTrack
    {
        public List<HarmonyEvent> Events { get; set; } = new();

        public void Reset()
        {
            Events.Clear();
        }

        public void Add(HarmonyEvent evt)
        {
            Events.Add(evt);
            Events = Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat).ToList();
        }

        /// <summary>
        /// Gets the active harmony event for a given bar.
        /// Returns the most recent harmony event that starts on or before this bar.
        /// </summary>
        public HarmonyEvent? GetActiveHarmonyEvent(int bar)
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