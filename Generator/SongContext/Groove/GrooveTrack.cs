// AI: purpose=Manage ordered list of GrooveEvent instances for song grooves; used by Generator to map presets to bars.
// AI: invariants=Events list kept sorted by StartBar ascending; StartBar is 1-based; GetActiveGrooveEvent requires an event <= query or throws.
// AI: deps=Consumers expect Events ordering and StartBar semantics; changing names/types breaks serialization and Generator logic.
// AI: perf=Add reorders by allocating a new list; avoid heavy Add churn in hot loops.

namespace Music.Generator
{
    // AI: design=lightweight in-memory track; does not normalize events beyond sorting by StartBar; callers handle validation.
    public class GrooveTrack
    {
        public List<GrooveEvent> Events { get; set; } = new();

        // AI: Reset clears all events; keeps object instance stable for reuse by tests or builders.
        public void Reset()
        {
            Events.Clear();
        }

        // AI: Add: appends then re-sorts Events by StartBar; does NOT deduplicate or merge overlapping ranges.
        // AI: change=if switching to an indexed structure, update Generator and tests that rely on ordering.
        public void Add(GrooveEvent evt)
        {
            Events.Add(evt);
            Events = Events.OrderBy(e => e.StartBar).ToList();
        }

        // AI: GetActiveGrooveEvent: returns latest event with StartBar <= startBar; throws if none found (caller must ensure at least StartBar=1).
        // AI: edge=startBar must be >=1; method throws ArgumentOutOfRangeException or InvalidOperationException when contract violated.
        public GrooveEvent GetActiveGrooveEvent(int startBar)
        {
            if (startBar < 1) throw new ArgumentOutOfRangeException(nameof(startBar));

            for (int i = Events.Count - 1; i >= 0; i--)
            {
                if (Events[i].StartBar <= startBar)
                {
                    return Events[i];
                }
            }

            // If you truly guarantee StartBar=1 exists and startBar>=1, you never hit this.
            throw new InvalidOperationException("No event at or before this bar. Expected StartBar = 1.");
        }

        // AI: GetActiveGroovePreset: resolves GroovePreset by exact SourcePresetName via GroovePresets.GetByName.
        // AI: note=GetByName does case-sensitive exact matching and may return null; this method force-unwraps result ("!").
        public GroovePreset GetActiveGroovePreset(int startBar)
        {
            var grooveEvent = GetActiveGrooveEvent(startBar);
            return GroovePresets.GetByName(grooveEvent.SourcePresetName)!;
        }
    }
}