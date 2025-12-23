namespace Music.Generator
{
    // This is a design track for Harmony
    // Global bar/beat-aligned harmony timeline

    public class HarmonyTrack
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

        // TO DO - THIS IS UNTESTED CODE

        // Fast lookup of the harmony active at a given bar and beat.
        public bool TryGetAt(int bar, int beat, out HarmonyEvent? evt)
        {
            if (bar < 1 || beat < 1 || beat > BeatsPerBar)
                throw new ArgumentOutOfRangeException($"Invalid bar/beat: {bar}/{beat}");

            var targetAbs = (bar - 1) * BeatsPerBar + (beat - 1);

            // Try bar-head cache if querying beat 1
            if (beat == 1 && _barHeads.TryGetValue(bar, out var cached))
            {
                evt = cached;
                return true;
            }

            // Find the most recent event at or before targetAbs
            HarmonyEvent? bestMatch = null;
            int bestStartAbs = -1;

            foreach (var he in Events)
            {
                var startAbs = (he.StartBar - 1) * BeatsPerBar + (he.StartBeat - 1);
                if (startAbs <= targetAbs && startAbs > bestStartAbs)
                {
                    bestMatch = he;
                    bestStartAbs = startAbs;
                }
            }

            if (bestMatch != null)
            {
                // Cache only if querying beat 1
                if (beat == 1)
                {
                    _barHeads[bar] = bestMatch;
                }
                evt = bestMatch;
                return true;
            }

            evt = null;
            return false;
        }
       
        private void IndexEventForBars(HarmonyEvent evt)
        {
            // Only cache bar-head (beat 1) entries
            if (evt.StartBeat == 1)
            {
                _barHeads[evt.StartBar] = evt;
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