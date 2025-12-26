namespace Music.Generator
{
    // This is a design track for the music groove

    public class GrooveTrack
    {
        //TO DO HIGH - this may change per instance right? Why is it up here?
        // or Groove maybe should be for a specified time signature!

        public int BeatsPerBar { get; set; } = 4; // config for this exact groove

        public List<GrooveEvent> Events { get; set; } = new();

        public void Reset()
        {
            Events.Clear();
        }

        public void Add(GrooveEvent evt)
        {
            Events.Add(evt);
            Events = Events.OrderBy(e => e.StartBar).ToList();
        }

        // Finds the Groove Prest at or immediate before the specified bar and returns the corresponding preset.
        // Split into two methods: one returns the active event, the other returns the preset (current behavior).
        public GrooveEvent GetActiveGrooveEvent(int startBar)
        {
            if (startBar < 1) throw new ArgumentOutOfRangeException(nameof(startBar));

            for (int i = Events.Count - 1; i >= 0; i--)
            {
                if (Events[i].StartBar <= startBar)
                {
                    return Events[i];
                }
            }

            // If you truly guarantee StartBar=1 exists and startBar>=1, you never hit this.
            throw new InvalidOperationException("No event at or before this bar. Expected StartBar = 1.");
        }

        public GroovePreset GetActiveGroovePreset(int startBar)
        {
            var grooveEvent = GetActiveGrooveEvent(startBar);
            return GroovePresets.GetByName(grooveEvent.SourcePresetName)!;
        }
    }
}