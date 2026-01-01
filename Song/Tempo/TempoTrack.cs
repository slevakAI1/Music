// AI: purpose=Global bar/beat-aligned tempo track; holds discrete TempoEvent instances for exporters/playback.
// AI: invariants=Events are bar/beat aligned; StartBar/StartBeat are 1-based; GetActiveTempoEvent returns latest event <= query bar.
// AI: deps=Used by exporters, generators, and test fixtures; renaming props or changing semantics breaks consumers.
// AI: change=If supporting ramps/curves add a new type rather than extending this discrete event model.

namespace Music.Generator
{
    // AI: design=Mutable, minimal container. Add appends only (no sorting); callers must append events in chronological order.
    public class TempoTrack
    {
        public List<TempoEvent> Events { get; set; } = new();
 
        public void Add(TempoEvent evt)
        {
            Events.Add(evt);
        }

        // AI: GetActiveTempoEvent: returns the most recent TempoEvent with StartBar <= bar.
        // AI: edge=bar<1 => returns false; for correctness Events should be appended in chronological order.
        public bool GetActiveTempoEvent(int bar, out TempoEvent? evt)
        {
            if (bar < 1)
            {
                evt = null;
                return false;
            }

            for (int i = Events.Count - 1; i >= 0; i--)
            {
                if (Events[i].StartBar <= bar)
                {
                    evt = Events[i];
                    return true;
                }
            }

            evt = null;
            return false;
        }
   }
}
