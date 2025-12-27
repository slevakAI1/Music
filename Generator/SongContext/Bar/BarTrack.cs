namespace Music.Generator
{ 
    public class BarTrack
    {
        private List<Bar> _bars = new();

        /// <summary>
        /// Gets the list of bars (read-only access).
        /// </summary>
        public IReadOnlyList<Bar> Bars => _bars.AsReadOnly();

        /// <summary>
        /// Rebuilds the entire bar track from a timing track.
        /// Each bar inherits its time signature from the active TimingEvent.
        /// </summary>
        /// <param name="timingTrack">The timing track containing time signature events</param>
        /// <param name="totalBars">Number of bars to generate (default 100)</param>
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
                    StartTick = currentTick,
                    Numerator = activeEvent.Numerator,
                    Denominator = activeEvent.Denominator,
                    AbsoluteTimeTicks = currentTick
                };

                // Calculate end tick using the bar's computed TicksPerMeasure
                bar.EndTick = bar.StartTick + bar.TicksPerMeasure;

                _bars.Add(bar);

                // Advance to next bar's start tick
                currentTick = bar.EndTick;
            }
        }

        /// <summary>
        /// Gets the active timing event for a given bar.
        /// Returns the most recent timing event that starts on or before this bar.
        /// </summary>
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

        /// <summary>
        /// Gets a bar by its bar number (1-based).
        /// </summary>
        public Bar? GetBar(int barNumber)
        {
            return _bars.FirstOrDefault(b => b.BarNumber == barNumber);
        }

        /// <summary>
        /// Clears all bars.
        /// </summary>
        public void Clear()
        {
            _bars.Clear();
        }
    }
}
