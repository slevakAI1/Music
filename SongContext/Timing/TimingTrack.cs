// AI: purpose=Global bar/beat-aligned timing track containing discrete time-signature events for generation/export.
// AI: invariants=Events appended in chronological order; StartBar is 1-based; Numerator>0 and Denominator usually power-of-two (caller-validated).
// AI: deps=Consumed by exporters, generators, and UI. Changing property names/structure breaks serializers and consumers.
// AI: change=If supporting ranges/ramps add a new model/type; keep this class for discrete time-signature events.

namespace Music.Generator
{
    // AI: mutable container for TimingEvent list; callers may reorder/batch-edit Events but must preserve StartBar ordering for queries.
    public class Timingtrack
    {
        // AI: Events: exposed mutable list for convenience; maintain ascending StartBar order for correct GetActiveTimeSignatureEvent results.
        public List<TimingEvent> Events { get; set; } = new();

        // AI: Add appends an event and does not validate, normalize, or reorder the list; use when you append chronologically.
        public void Add(TimingEvent evt)
        {
            Events.Add(evt);
        }

        // AI: Returns the most recent TimingEvent with StartBar <= bar. Throws ArgumentOutOfRange if bar<1. Relies on Events order.
        public TimingEvent? GetActiveTimeSignatureEvent(int bar)
        {
            if (bar < 1) throw new ArgumentOutOfRangeException(nameof(bar));

            for (int i = Events.Count - 1; i >= 0; i--)
            {
                var evt = Events[i];
                var eventStartBar = evt.StartBar;

                if (eventStartBar <= bar)
                {
                    return evt;
                }
            }

            return null;
        }
    }
}
