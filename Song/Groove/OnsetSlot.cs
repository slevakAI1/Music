// AI: purpose=Represents a single onset slot with precomputed tick values for generation; immutable after construction.
// AI: invariants=StartTick < EndTick; DurationTicks = EndTick - StartTick; Bar >= 1; OnsetBeat >= 1.
// AI: deps=Used by generators to avoid repeated tick calculations; produced by OnsetGrid.
// AI: perf=Lightweight struct; no allocations after creation; pass by value or ref in tight loops.

namespace Music.Generator
{
    // AI: OnsetSlot: precomputed timing for a single onset; consumers use StartTick/DurationTicks directly.
    public readonly struct OnsetSlot
    {
        public int Bar { get; init; }
        public decimal OnsetBeat { get; init; }
        public long StartTick { get; init; }
        public long EndTick { get; init; }
        public int DurationTicks { get; init; }
        public bool IsStrongBeat { get; init; }
    }
}
