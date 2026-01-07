// AI: purpose=Manage ordered HarmonyEvent list for song generation; used to resolve active harmony by bar.
// AI: invariants=Events sorted by StartBar then StartBeat ascending; StartBar is 1-based. GetActiveHarmonyEvent returns latest event <= bar or null.
// AI: deps=Consumers (Generator, editors) rely on ordering and StartBar semantics; renaming properties breaks serialization/UI.
// AI: perf=Add re-sorts by allocating a new list; avoid many Adds in hot loops or change to indexed structure.

namespace Music.Generator
{
    // AI: design=lightweight mutable track; does not validate overlaps or durations; callers must ensure event consistency.
    public class HarmonyTrack
    {
        public List<HarmonyEvent> Events { get; set; } = new();

        // AI: Reset: clear all events but keep instance for reuse (tests/builders depend on this behavior).
        public void Reset()
        {
            Events.Clear();
        }

        // AI: Add: appends and re-sorts Events by StartBar then StartBeat; does not deduplicate or merge ranges.
        // AI: change=If switching to a more efficient structure, update Generator and any tests assuming ordering.
        public void Add(HarmonyEvent evt)
        {
            Events.Add(evt);
            Events = Events.OrderBy(e => e.StartBar).ThenBy(e => e.StartBeat).ToList();
        }

        // AI: Returns most recent HarmonyEvent active at the given bar and beat (beat is 1-based).
        // AI: Semantic: find the latest event whose (StartBar, StartBeat) is <= (bar, beat) ordering.
        public HarmonyEvent? GetActiveHarmonyEvent(int bar, decimal beat)
        {
            if (bar < 1) throw new ArgumentOutOfRangeException(nameof(bar));
            if (beat < 1m) throw new ArgumentOutOfRangeException(nameof(beat));

            for (int i = Events.Count - 1; i >= 0; i--)
            {
                var evt = Events[i];
                if (evt.StartBar < bar)
                {
                    return evt;
                }

                if (evt.StartBar == bar && evt.StartBeat <= beat)
                {
                    return evt;
                }
            }

            return null;
        }
    }
}