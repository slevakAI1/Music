namespace Music.Generator
{
    // This is a design track for tempo
    // Global bar/beat-aligned tempo track
    public class TempoTrack
    {
        private readonly Dictionary<int, TempoEvent> _barHeads = new(); // bar -> event active at beat 1

        public int BeatsPerBar { get; set; } = 4;

        public List<TempoEvent> Events { get; set; } = new();

        public void ConfigureGlobal(string meter)
        {
            // Expect "x/y". For now, only x matters for bar length in beats.
            if (string.IsNullOrWhiteSpace(meter)) throw new ArgumentException(nameof(meter));
            var parts = meter.Split('/');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var beats))
                throw new ArgumentException("Invalid meter format. Expected like \"4/4\".", nameof(meter));

            BeatsPerBar = Math.Max(1, beats);
            Reindex();
        }

        public void Reset()
        {
            Events.Clear();
            _barHeads.Clear();
        }

        public void Add(TempoEvent evt)
        {
            Events.Add(evt);
            IndexEventForBars(evt);
        }

        // Fast lookup of the tempo active at the start of a bar (beat 1).
        public bool TryGetAtBar(int bar, out TempoEvent? evt)
        {
            if (_barHeads.TryGetValue(bar, out var e))
            {
                evt = e;
                return true;
            }

            // Fallback: find the most recent event at or before this bar
            var targetAbs = (bar - 1) * BeatsPerBar;
            
            TempoEvent? bestMatch = null;
            int bestStartAbs = -1;

            foreach (var te in Events)
            {
                var startAbs = (te.StartBar - 1) * BeatsPerBar + (te.StartBeat - 1);
                if (startAbs <= targetAbs && startAbs > bestStartAbs)
                {
                    bestMatch = te;
                    bestStartAbs = startAbs;
                }
            }

            if (bestMatch != null)
            {
                _barHeads[bar] = bestMatch;
                evt = bestMatch;
                return true;
            }

            evt = null;
            return false;
        }

        private void IndexEventForBars(TempoEvent evt)
        {
            // Index this event at its starting bar
            _barHeads[evt.StartBar] = evt;
        }

        private void Reindex()
        {
            _barHeads.Clear();
            foreach (var te in Events)
                IndexEventForBars(te);
        }

        public void EnsureIndexed()
        {
            Reindex();
        }

        // Helper: Get the duration in beats for a specific event (until next event or end of track)
        public int GetEventDuration(TempoEvent evt, int totalBars)
        {
            var startAbs = (evt.StartBar - 1) * BeatsPerBar + (evt.StartBeat - 1);
            
            // Find the next event
            int nextStartAbs = totalBars * BeatsPerBar; // default to end of track
            
            foreach (var te in Events)
            {
                var teStartAbs = (te.StartBar - 1) * BeatsPerBar + (te.StartBeat - 1);
                if (teStartAbs > startAbs && teStartAbs < nextStartAbs)
                {
                    nextStartAbs = teStartAbs;
                }
            }
            
            return nextStartAbs - startAbs;
        }
    }
}
