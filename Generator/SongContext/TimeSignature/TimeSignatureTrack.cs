
namespace Music.Generator
{
    // This is a design track for time signature
    // Global bar/beat-aligned time signature timeline
    public class TimeSignatureTrack
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
        }

        public void Add(TimeSignatureEvent evt)
        {
            Events.Add(evt);
            // Clear cache when adding new events
            _barHeads.Clear();
        }
    }
}
