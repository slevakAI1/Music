namespace Music.Generator
{
    // This is a design track for Harmony
    // Global bar/beat-aligned harmony track

    public class HarmonyTrack
    {
        // TO DO - HIGH - THIS PROBABLY SHOULD GO AS WELL - timing is kept in the timeSignatureTrack
        public int BeatsPerBar { get; set; } = 4; // Remove - this is represented elsewhere

        public List<HarmonyEvent> Events { get; set; } = new();

        // TO DO - HIGH - WHY DOES THIS EXIST???!!!
        public void ConfigureGlobal(string meter)
        {
            // Expect "x/y". For now, only x matters for bar length in beats.
            if (string.IsNullOrWhiteSpace(meter)) throw new ArgumentException(nameof(meter));
            var parts = meter.Split('/');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var beats))
                throw new ArgumentException("Invalid meter format. Expected like \"4/4\".", nameof(meter));   // REMOVE METER!!

            BeatsPerBar = Math.Max(1, beats); // REMOVE - this is represented elsewhere
        }

        public void Reset()
        {
            Events.Clear();
        }

        public void Add(HarmonyEvent evt)
        {
            Events.Add(evt);
            Events = Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat).ToList();
        }

        public void Update(HarmonyEvent evt)
        {
            //var existing = Events.FirstOrDefault(e => e == evt);
            //if (existing != null)
            //{
            //    Events.Remove(existing);
            //    Events.Add(evt);
            //    Events = Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat).ToList();
            //}
        }

        public void Delete(HarmonyEvent evt)
        {
            //Events.Remove(evt);
            //Events = Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat).ToList();
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