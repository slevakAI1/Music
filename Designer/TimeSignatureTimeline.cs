namespace Music.Designer
{
    // Global bar/beat-aligned time signature timeline
    public class TimeSignatureTimeline
    {
        private readonly Dictionary<int, TimeSignatureEvent> _barHeads = new(); // bar -> event active at beat 1

        public int BeatsPerBar { get; set; } = 4;

        public List<TimeSignatureEvent> Events { get; set; } = new();

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

        public void Add(TimeSignatureEvent evt)
        {
            Events.Add(evt);
            IndexEventForBars(evt);
        }

        // Fast lookup of the signature active at the start of a bar (beat 1).
        public bool TryGetAtBar(int bar, out TimeSignatureEvent? evt)
        {
            if (_barHeads.TryGetValue(bar, out var e))
            {
                evt = e;
                return true;
            }

            // Fallback scan (also memoizes)
            foreach (var se in Events)
            {
                if (se.Contains(bar, 1, BeatsPerBar))
                {
                    _barHeads[bar] = se;
                    evt = se;
                    return true;
                }
            }

            evt = null;
            return false;
        }

        private void IndexEventForBars(TimeSignatureEvent evt)
        {
            var startAbs = (evt.StartBar - 1) * BeatsPerBar + (evt.StartBeat - 1);
            var endAbsExcl = startAbs + evt.DurationBeats;

            var startBar = evt.StartBar;
            var endBarInclusive = (int)Math.Floor((endAbsExcl - 1) / (double)BeatsPerBar) + 1;

            for (int bar = startBar; bar <= endBarInclusive; bar++)
            {
                var barStartAbs = (bar - 1) * BeatsPerBar;
                if (startAbs <= barStartAbs && endAbsExcl > barStartAbs)
                {
                    _barHeads[bar] = evt;
                }
            }
        }

        private void Reindex()
        {
            _barHeads.Clear();
            foreach (var se in Events)
                IndexEventForBars(se);
        }

        public void EnsureIndexed()
        {
            Reindex();
        }
    }
}
