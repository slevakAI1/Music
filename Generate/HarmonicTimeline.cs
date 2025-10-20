namespace Music.Generate
{
    // Global bar/beat-aligned harmony timeline
    public class HarmonicTimeline
    {
        private readonly List<HarmonicEvent> _events = new();
        private readonly Dictionary<int, HarmonicEvent> _barHeads = new(); // bar -> event active at beat 1

        public int BeatsPerBar { get; private set; } = 4;
        public int TempoBpm { get; private set; } = 96;

        public IReadOnlyList<HarmonicEvent> Events => _events;

        public void ConfigureGlobal(string meter, int tempoBpm)
        {
            // Expect "x/y". For now, only x matters for bar length in beats.
            if (string.IsNullOrWhiteSpace(meter)) throw new ArgumentException(nameof(meter));
            var parts = meter.Split('/');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var beats))
                throw new ArgumentException("Invalid meter format. Expected like \"4/4\".", nameof(meter));

            BeatsPerBar = Math.Max(1, beats);
            TempoBpm = tempoBpm;

            // Reindex any existing events with the new meter
            Reindex();
        }

        public void Reset()
        {
            _events.Clear();
            _barHeads.Clear();
        }

        public void Add(HarmonicEvent evt)
        {
            _events.Add(evt);
            IndexEventForBars(evt);
        }

        // Fast lookup of the harmony active at the start of a bar (beat 1).
        public bool TryGetAtBar(int bar, out HarmonicEvent? evt)
        {
            if (_barHeads.TryGetValue(bar, out var e))
            {
                evt = e;
                return true;
            }

            // Fallback scan (also memoizes)
            foreach (var he in _events)
            {
                if (he.Contains(bar, 1, BeatsPerBar))
                {
                    _barHeads[bar] = he;
                    evt = he;
                    return true;
                }
            }

            evt = null;
            return false;
        }

        private void IndexEventForBars(HarmonicEvent evt)
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
            foreach (var he in _events)
                IndexEventForBars(he);
        }
    }
}