namespace Music.Generator
{ 
    // AI: purpose=Represents derived sequence of Bars for one track; not the authoritative timing source.
    // AI: invariants=Bar.BarNumber is 1-based; Bars list mirrors last RebuildFromTimingTrack(); StartTick strictly increases.
    // AI: deps=Depends on Timingtrack.Events providing StartBar,Numerator,Denominator; caller must validate events.
    // AI: change=If changing timing calc update GetActiveTimingEvent and keep numbering/tick semantics stable.

    public class BarTrack
    {
        private List<Bar> _bars = new();

        // AI: Bars: live IReadOnlyList wrapper over internal list; reflects rebuild/clear; not thread-safe.
        public IReadOnlyList<Bar> Bars => _bars.AsReadOnly();

        // AI: RebuildFromTimingTrack: derive bars using latest TimingEvent with StartBar<=barNumber.
        // AI: behavior: skips bars with no active event; totalBars is a maximum cap; non-positive totalBars yields no loop.
        // AI: tickcalc: StartTick starts at 0 and advances by each bar's TicksPerMeasure; relies on Bar.TicksPerMeasure.
        public void RebuildFromTimingTrack(Timingtrack timingTrack, int totalBars = 100)
        {
            _bars.Clear();

            if (timingTrack == null || timingTrack.Events.Count == 0)
            {
                // No timing events - can't build bars
                return;
            }

            // Sort timing events by start bar
            var sortedEvents = timingTrack.Events
                .OrderBy(e => e.StartBar)
                .ToList();

            long currentTick = 0;

            for (int barNumber = 1; barNumber <= totalBars; barNumber++)
            {
                // Find the active time signature for this bar
                var activeEvent = GetActiveTimingEvent(sortedEvents, barNumber);
                
                if (activeEvent == null)
                {
                    // No time signature defined yet for this bar - skip
                    continue;
                }

                // Create the bar with the active time signature
                var bar = new Bar
                {
                    BarNumber = barNumber,
                    Numerator = activeEvent.Numerator,
                    Denominator = activeEvent.Denominator,
                    StartTick = currentTick
                };

                // Calculate end tick using the bar's computed TicksPerMeasure
                bar.EndTick = bar.StartTick + bar.TicksPerMeasure;

                _bars.Add(bar);

                // Advance to next bar's start tick
                currentTick = bar.EndTick;
            }
        }

        // AI: GetActiveTimingEvent: returns last event with StartBar<=barNumber or null; expects sortedEvents asc.
        private TimingEvent? GetActiveTimingEvent(List<TimingEvent> sortedEvents, int barNumber)
        {
            TimingEvent? activeEvent = null;

            foreach (var evt in sortedEvents)
            {
                if (evt.StartBar > barNumber)
                {
                    // We've gone past the target bar
                    break;
                }
                activeEvent = evt;
            }

            return activeEvent;
        }

        // AI: GetBar: returns matching Bar by BarNumber or null if not present (skipped or cleared).
        public Bar? GetBar(int barNumber)
        {
            return _bars.FirstOrDefault(b => b.BarNumber == barNumber);
        }

        // AI: Clear: destructive; resets internal list only; callers must RebuildFromTimingTrack to repopulate.
        public void Clear()
        {
            _bars.Clear();
        }
    }
}
