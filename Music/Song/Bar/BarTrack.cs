namespace Music.Generator
{
    // AI: purpose=Represents derived sequence of Bars for one track; not the authoritative timing source.
    // AI: invariants=Bar.BarNumber is 1-based; Bars list mirrors last RebuildFromTimingTrack(); StartTick strictly increases.
    // AI: deps=Timingtrack.Events for timing; SectionTrack for section context; caller must validate both tracks.
    // AI: change=If changing timing calc update GetActiveTimingEvent and keep numbering/tick semantics stable.

    public class BarTrack
    {
        private List<Bar> _bars = new();

        // AI: Bars: live IReadOnlyList wrapper over internal list; reflects rebuild/clear; not thread-safe.
        public IReadOnlyList<Bar> Bars => _bars.AsReadOnly();

        // AI: RebuildFromTimingTrack: derive bars using Timingtrack and populate section context via SectionTrack.
        // AI: behavior: skips bars with no active event; totalBars is a maximum cap; non-positive totalBars yields no loop.
        // AI: tickcalc: StartTick starts at 0 and advances by each bar's TicksPerMeasure; relies on Bar.TicksPerMeasure.
        public void RebuildFromTimingTrack(Timingtrack timingTrack, SectionTrack sectionTrack, int totalBars = 100)
        {
            ArgumentNullException.ThrowIfNull(timingTrack);
            ArgumentNullException.ThrowIfNull(sectionTrack);

            _bars.Clear();

            if (timingTrack.Events.Count == 0)
            {
                // No timing events - can't build bars
                return;
            }

            long currentTick = 0;

            for (int barNumber = 1; barNumber <= totalBars; barNumber++)
            {
                // Use the canonical Timingtrack method to find the active time signature for this bar
                var activeEvent = timingTrack.GetActiveTimeSignatureEvent(barNumber);

                if (activeEvent == null)
                {
                    // No time signature defined yet for this bar - skip
                    continue;
                }

                sectionTrack.GetActiveSection(barNumber, out var section);
                var barWithinSection = 0;
                var barsUntilSectionEnd = 0;

                if (section != null)
                {
                    barWithinSection = barNumber - section.StartBar;
                    var sectionEndBar = section.StartBar + section.BarCount - 1;
                    barsUntilSectionEnd = sectionEndBar - barNumber;
                }

                // Create the bar with the active time signature
                var bar = new Bar
                {
                    BarNumber = barNumber,
                    Section = section,
                    BarWithinSection = barWithinSection,
                    BarsUntilSectionEnd = barsUntilSectionEnd,
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

        // AI: Clear: destructive; resets internal list only; callers must RebuildFromTimingTrack to repopulate.
        public void Clear()
        {
            _bars.Clear();
        }

        // AI: TryGetBar: primary lookup method with validation; returns false for barNumber < 1 or not found; callers should prefer this for robustness.
        public bool TryGetBar(int barNumber, out Bar bar)
        {
            bar = default!;
            if (barNumber < 1)
                return false;

            var found = _bars.FirstOrDefault(b => b.BarNumber == barNumber);
            if (found == null)
                return false;

            bar = found;
            return true;
        }

        // ==============================================================================================================================

        //   THESE ARE NEW METHODS added to BarTrack to support common operations like retrieving bars, converting bar+beat to ticks, and validating beat positions within bars. They rely on the existing structure of Bar and the way bars are built from TimingTrack events. These methods are essential for consumers of BarTrack to interact with the bar data effectively without needing to manually search through the Bars list or perform their own calculations for ticks and beat positions.

        /// <summary>
        /// Converts a bar + fractional onsetBeat (1-based, quarter-note beat units) into absolute ticks.
        /// Uses the bar's StartTick plus (onsetBeat - 1) * TicksPerQuarterNote.
        /// </summary>
        /// <remarks>
        /// Valid onsetBeat range is [1, Numerator + 1). Example in 4/4: 4.5 is valid; 5.0 is not (belongs to next bar).
        /// </remarks>
        public long ToTick(int barNumber, decimal onsetBeat)
        {
            if (!TryGetBar(barNumber, out var bar))
                throw new ArgumentOutOfRangeException(nameof(barNumber), barNumber, "Bar not found in BarTrack.");

            if (!IsBeatInBar(barNumber, onsetBeat))
                throw new ArgumentOutOfRangeException(nameof(onsetBeat), onsetBeat,
                    $"onsetBeat must be in [1, {bar.Numerator + 1}) for bar {barNumber} (Numerator={bar.Numerator}).");

            // MVP convention: onsetBeat is in quarter-note beat units.
            // (onsetBeat - 1) because beat 1 is the bar start.
            var offsetTicks = (long)((onsetBeat - 1m) * MusicConstants.TicksPerQuarterNote);
            return bar.StartTick + offsetTicks;
        }

        /// <summary>
        /// True if onsetBeat is within the bar under MVP rules:
        /// onsetBeat is 1-based and may be fractional, and must satisfy: 1 <= onsetBeat < Numerator + 1.
        /// </summary>
        public bool IsBeatInBar(int barNumber, decimal onsetBeat)
        {
            if (!TryGetBar(barNumber, out var bar))
                return false;

            return onsetBeat >= 1m && onsetBeat < (bar.Numerator + 1m);
        }

        /// <summary>
        /// Returns the bar's EndTick (exclusive).
        /// </summary>
        public long GetBarEndTick(int barNumber)
        {
            if (!TryGetBar(barNumber, out var bar))
                throw new ArgumentOutOfRangeException(nameof(barNumber), barNumber, "Bar not found in BarTrack.");

            return bar.EndTick;
        }
    }
}
