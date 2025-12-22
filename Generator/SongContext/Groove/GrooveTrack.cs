namespace Music.Generator
{
    // This is a design track for Groove
    // Global bar/beat-aligned groove timeline
    public class GrooveTrack
    {
        private readonly Dictionary<int, GrooveInstance> _barHeads = new(); // bar -> event active at beat 1


        //TO DO - this may change per instance right? Why is it up here?

        public int BeatsPerBar { get; set; } = 4;

        public List<GrooveInstance> Events { get; set; } = new();

        public void Reset()
        {
            Events.Clear();
            _barHeads.Clear();
        }

        public void Add(GrooveInstance evt)
        {
            Events.Add(evt);
            IndexEventForBars(evt);
        }

        // Fast lookup of the groove active at the start of a bar (beat 1).
        public bool TryGetAtBar(int bar, out GrooveInstance? evt)
        {
            if (_barHeads.TryGetValue(bar, out var e))
            {
                evt = e;
                return true;
            }

            // Fallback: find the most recent event at or before this bar
            var targetAbs = (bar - 1) * BeatsPerBar;

            GrooveInstance? bestMatch = null;
            int bestStartAbs = -1;

            foreach (var ge in Events)
            {
                // GrooveInstance always starts at beat 1 of StartBar, so no beat offset needed
                var startAbs = (ge.StartBar - 1) * BeatsPerBar;
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

        private void IndexEventForBars(GrooveInstance evt)
        {
            // GrooveInstance always starts at beat 1 of StartBar (spans entire bars).
            // Index it at its starting bar for fast lookup.
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