namespace Music.Generator
{
    // This is a design track for tempo
    // Global bar/beat-aligned tempo track
    public class TempoTrack
    {
        public List<TempoEvent> Events { get; set; } = new();
 
        public void Add(TempoEvent evt)
        {
            Events.Add(evt);
        }

        /// <summary>
        /// Gets the active tempo event at the specified bar.
        /// Returns the most recent tempo event that starts on or before this bar.
        /// </summary>
        public bool GetActiveTempoEvent(int bar, out TempoEvent? evt)
        {
            if (bar < 1)
            {
                evt = null;
                return false;
            }

            for (int i = Events.Count - 1; i >= 0; i--)
            {
                if (Events[i].StartBar <= bar)
                {
                    evt = Events[i];
                    return true;
                }
            }

            evt = null;
            return false;
        }
   }
}
