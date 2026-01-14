// AI: purpose=Build OnsetGrid from bar number, onset list, and BarTrack; handles duration/tick calculation once.
// AI: invariants=Output slots have strictly increasing StartTick; DurationTicks computed as next onset or bar end.
// AI: deps=Relies on BarTrack.ToTick, BarTrack.GetBarEndTick, and onset beat validity; throws on invalid onsets.
// AI: perf=Single-pass construction; minimal allocations; callers should cache grid per bar if reused.
// AI: change=If adding normalization/validation, update Build to reject duplicates or unsorted onsets consistently.

namespace Music.Generator
{
    // AI: OnsetGrid: factory for producing ordered list of OnsetSlot from raw onset beats; centralizes tick math.
    public static class OnsetGrid
    {
        // AI: Build: converts onset beats to slots with precomputed ticks; throws ArgumentOutOfRangeException on invalid onsets.
        // AI: behavior=IsStrongBeat true when onsetBeat is integer (beat 1, 2, 3...); validates onset order and bar bounds.
        public static IReadOnlyList<OnsetSlot> Build(int bar, IReadOnlyList<decimal> onsetBeats, BarTrack barTrack)
        {
            if (bar < 1)
                throw new ArgumentOutOfRangeException(nameof(bar), bar, "Bar must be >= 1");

            ArgumentNullException.ThrowIfNull(onsetBeats);
            ArgumentNullException.ThrowIfNull(barTrack);

            if (onsetBeats.Count == 0)
                return Array.Empty<OnsetSlot>();

            if (!barTrack.TryGetBar(bar, out var currentBar))
                throw new ArgumentException($"Bar {bar} not found in BarTrack", nameof(bar));

            long barEndTick = barTrack.GetBarEndTick(bar);
            var slots = new List<OnsetSlot>(onsetBeats.Count);

            decimal previousOnsetBeat = 0m;

            for (int i = 0; i < onsetBeats.Count; i++)
            {
                decimal onsetBeat = onsetBeats[i];

                // Validate onset is within bar
                if (!barTrack.IsBeatInBar(bar, onsetBeat))
                {
                    throw new ArgumentOutOfRangeException(nameof(onsetBeats),
                        $"Onset beat {onsetBeat} at index {i} is outside bar {bar}");
                }

                // Validate onset order (detect unsorted or duplicate onsets)
                if (i > 0 && onsetBeat <= previousOnsetBeat)
                {
                    throw new ArgumentException(
                        $"Onset beats must be strictly ascending. Found {onsetBeat} at index {i} after {previousOnsetBeat}.",
                        nameof(onsetBeats));
                }

                long startTick = barTrack.ToTick(bar, onsetBeat);

                // Calculate end tick: next onset or bar end
                long endTick = (i + 1 < onsetBeats.Count)
                    ? barTrack.ToTick(bar, onsetBeats[i + 1])
                    : barEndTick;

                // Strong beat detection: integer beat values (1.0, 2.0, 3.0...)
                bool isStrongBeat = onsetBeat == Math.Floor(onsetBeat);

                slots.Add(new OnsetSlot
                {
                    Bar = bar,
                    OnsetBeat = onsetBeat,
                    StartTick = startTick,
                    EndTick = endTick,
                    DurationTicks = (int)(endTick - startTick),
                    IsStrongBeat = isStrongBeat
                });

                previousOnsetBeat = onsetBeat;
            }

            return slots;
        }
    }
}
