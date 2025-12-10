namespace Music.Designer
{
    // Global bar/beat-aligned harmony timeline
    public class HarmonyTimeline
    {
        private readonly Dictionary<int, HarmonyEvent> _barHeads = new(); // bar -> event active at beat 1

        public int BeatsPerBar { get; set; } = 4; // Remove - this is represented elsewhere

        public List<HarmonyEvent> Events { get; set; } = new();

        public void ConfigureGlobal(string meter)
        {
            // Expect "x/y". For now, only x matters for bar length in beats.
            if (string.IsNullOrWhiteSpace(meter)) throw new ArgumentException(nameof(meter));
            var parts = meter.Split('/');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var beats))
                throw new ArgumentException("Invalid meter format. Expected like \"4/4\".", nameof(meter));   // REMOVE METER!!

            BeatsPerBar = Math.Max(1, beats); // REMOVE - this is represented elsewhere

            // Reindex any existing events with the new meter
            Reindex();
        }

        public void Add(HarmonyEvent evt)
        {
            Events.Add(evt);
            IndexEventForBars(evt);
        }
       
        private void IndexEventForBars(HarmonyEvent evt)
        {
            var startAbs = (evt.StartBar - 1) * BeatsPerBar + (evt.StartBeat - 1);
            var endAbsExcl = startAbs + evt.DurationBeats;

            var startBar = evt.StartBar;
            var endBarInclusive = (int)Math.Floor((endAbsExcl - 1) / (double)BeatsPerBar) + 1;

            for (int bar = startBar; bar <= endBarInclusive; bar++)
            {
                var barStartAbs = (bar - 1) * BeatsPerBar;
                // Only index if the event is active at beat 1 of this bar
                if (startAbs <= barStartAbs && endAbsExcl > barStartAbs)
                {
                    _barHeads[bar] = evt;
                }
            }
        }

        private void Reindex()
        {
            _barHeads.Clear();
            foreach (var he in Events)
                IndexEventForBars(he);
        }

        public void EnsureIndexed()
        {
            Reindex();
        }
    }
}