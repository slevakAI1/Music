// AI: purpose=Pure, deterministic analyzer for per-bar coverage state of PartTrack events.
// AI: invariants=No mutation of inputs; half-open intervals [start,end); Locked > HasContent > Empty precedence.
// AI: deps=PartTrack.PartTrackNoteEvents; BarTrack.Bars; produces BarFillState map or BarCoverageReport.
// AI: perf=O(events * bars) worst case; acceptable for typical song sizes; optimize with interval tree if needed.

using Music.MyMidi;

namespace Music.Generator
{
    // AI: Static utility; all methods are pure; no instance state; thread-safe by design.
    public static class PartTrackBarCoverageAnalyzer
    {
        // AI: Simple analysis returning only coverage map; use AnalyzeWithDiagnostics for extended metrics.
        public static IReadOnlyDictionary<int, BarFillState> Analyze(
            PartTrack partTrack,
            BarTrack barTrack,
            IReadOnlySet<int>? lockedBars = null)
        {
            ArgumentNullException.ThrowIfNull(partTrack);
            ArgumentNullException.ThrowIfNull(barTrack);

            var report = AnalyzeWithDiagnostics(partTrack, barTrack, lockedBars);
            return report.Coverage;
        }

        // AI: Full analysis with diagnostics; preferred when debugging or validating data quality.
        public static BarCoverageReport AnalyzeWithDiagnostics(
            PartTrack partTrack,
            BarTrack barTrack,
            IReadOnlySet<int>? lockedBars = null)
        {
            ArgumentNullException.ThrowIfNull(partTrack);
            ArgumentNullException.ThrowIfNull(barTrack);

            var bars = barTrack.Bars;

            // Empty BarTrack: return empty coverage map
            if (bars.Count == 0)
            {
                return new BarCoverageReport(
                    Coverage: new Dictionary<int, BarFillState>(),
                    EventCountPerBar: new Dictionary<int, int>(),
                    OccupiedTicksPerBar: new Dictionary<int, long>(),
                    OutOfRangeEventCount: partTrack.PartTrackNoteEvents.Count,
                    DegenerateEventCount: 0,
                    NegativeTickEventCount: 0);
            }

            // Initialize per-bar state tracking
            var coverage = new Dictionary<int, BarFillState>();
            var eventCountPerBar = new Dictionary<int, int>();
            var occupiedIntervalsPerBar = new Dictionary<int, List<(long Start, long End)>>();

            foreach (var bar in bars)
            {
                coverage[bar.BarNumber] = BarFillState.Empty;
                eventCountPerBar[bar.BarNumber] = 0;
                occupiedIntervalsPerBar[bar.BarNumber] = new List<(long Start, long End)>();
            }

            // Apply locked bars (highest precedence)
            if (lockedBars != null)
            {
                foreach (var lockedBarNum in lockedBars)
                {
                    if (coverage.ContainsKey(lockedBarNum))
                    {
                        coverage[lockedBarNum] = BarFillState.Locked;
                    }
                }
            }

            // Determine bar range for out-of-range detection
            long firstBarStart = bars[0].StartTick;
            long lastBarEnd = bars[^1].EndTick;

            int outOfRangeCount = 0;
            int degenerateCount = 0;
            int negativeTickCount = 0;

            // Process each event
            foreach (var evt in partTrack.PartTrackNoteEvents)
            {
                long noteStart = evt.AbsoluteTimeTicks;
                long noteEnd = noteStart + evt.NoteDurationTicks;

                // Track negative AbsoluteTimeTicks
                if (noteStart < 0)
                {
                    negativeTickCount++;
                }

                // Track degenerate (negative duration) events
                if (evt.NoteDurationTicks < 0)
                {
                    degenerateCount++;
                    // Swap to create valid interval for overlap testing
                    (noteStart, noteEnd) = (noteEnd, noteStart);
                }

                // Check if entirely out of range
                // For zero-duration events, check if start tick is within any bar
                bool isZeroDuration = noteStart == noteEnd;
                if (isZeroDuration)
                {
                    if (noteStart < firstBarStart || noteStart >= lastBarEnd)
                    {
                        outOfRangeCount++;
                        continue;
                    }
                }
                else if (noteEnd <= firstBarStart || noteStart >= lastBarEnd)
                {
                    outOfRangeCount++;
                    continue;
                }

                // Find all intersecting bars
                foreach (var bar in bars)
                {
                    // For zero-duration: check if start tick is in [bar.StartTick, bar.EndTick)
                    // For non-zero: half-open overlap [noteStart, noteEnd) ∩ [bar.StartTick, bar.EndTick) non-empty
                    bool intersects = isZeroDuration
                        ? (noteStart >= bar.StartTick && noteStart < bar.EndTick)
                        : (noteStart < bar.EndTick && noteEnd > bar.StartTick);

                    if (intersects)
                    {
                        // Mark as HasContent only if not Locked
                        if (coverage[bar.BarNumber] != BarFillState.Locked)
                        {
                            coverage[bar.BarNumber] = BarFillState.HasContent;
                        }

                        eventCountPerBar[bar.BarNumber]++;

                        // Track occupied interval for tick calculation
                        long clampedStart = Math.Max(noteStart, bar.StartTick);
                        long clampedEnd = Math.Min(noteEnd, bar.EndTick);
                        if (clampedEnd > clampedStart)
                        {
                            occupiedIntervalsPerBar[bar.BarNumber].Add((clampedStart, clampedEnd));
                        }
                    }
                }
            }

            // Calculate deduplicated occupied ticks per bar
            var occupiedTicksPerBar = new Dictionary<int, long>();
            foreach (var bar in bars)
            {
                occupiedTicksPerBar[bar.BarNumber] = CalculateOccupiedTicks(occupiedIntervalsPerBar[bar.BarNumber]);
            }

            return new BarCoverageReport(
                Coverage: coverage,
                EventCountPerBar: eventCountPerBar,
                OccupiedTicksPerBar: occupiedTicksPerBar,
                OutOfRangeEventCount: outOfRangeCount,
                DegenerateEventCount: degenerateCount,
                NegativeTickEventCount: negativeTickCount);
        }

        // AI: Deduplicates overlapping intervals and returns total distinct tick coverage.
        private static long CalculateOccupiedTicks(List<(long Start, long End)> intervals)
        {
            if (intervals.Count == 0)
                return 0;

            // Sort by start, then by end descending for proper merging
            var sorted = intervals.OrderBy(i => i.Start).ThenByDescending(i => i.End).ToList();

            long totalTicks = 0;
            long mergedStart = sorted[0].Start;
            long mergedEnd = sorted[0].End;

            for (int i = 1; i < sorted.Count; i++)
            {
                var (start, end) = sorted[i];
                if (start <= mergedEnd)
                {
                    // Overlapping or adjacent: extend merged interval
                    mergedEnd = Math.Max(mergedEnd, end);
                }
                else
                {
                    // Gap: add previous merged interval and start new one
                    totalTicks += mergedEnd - mergedStart;
                    mergedStart = start;
                    mergedEnd = end;
                }
            }

            // Add final merged interval
            totalTicks += mergedEnd - mergedStart;

            return totalTicks;
        }
    }
}
