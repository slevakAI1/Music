namespace Music.Designer
{
    // Global bar/beat-aligned groove timeline
    public class GrooveTimeline
    {
        private readonly Dictionary<int, GrooveEvent> _barHeads = new(); // bar -> event active at beat 1

        public int BeatsPerBar { get; set; } = 4;

        public List<GrooveEvent> Events { get; set; } = new();

        public void Reset()
        {
            Events.Clear();
            _barHeads.Clear();
        }

        public void Add(GrooveEvent evt)
        {
            Events.Add(evt);
            IndexEventForBars(evt);
        }

        // Fast lookup of the groove active at the start of a bar (beat 1).
        public bool TryGetAtBar(int bar, out GrooveEvent? evt)
        {
            if (_barHeads.TryGetValue(bar, out var e))
            {
                evt = e;
                return true;
            }

            // Fallback: find the most recent event at or before this bar
            var targetAbs = (bar - 1) * BeatsPerBar;
            
            GrooveEvent? bestMatch = null;
            int bestStartAbs = -1;

            foreach (var ge in Events)
            {
                var startAbs = (ge.StartBar - 1) * BeatsPerBar + (ge.StartBeat - 1);
                if (startAbs <= targetAbs && startAbs > bestStartAbs)
                {
                    bestMatch = ge;
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

        private void IndexEventForBars(GrooveEvent evt)
        {
            // Index this event at its starting bar
            _barHeads[evt.StartBar] = evt;
        }

        private void Reindex()
        {
            _barHeads.Clear();
            foreach (var ge in Events)
                IndexEventForBars(ge);
        }

        public void EnsureIndexed()
        {
            Reindex();
        }
    }
}