// AI: purpose=Immutable onset slot for precomputed onset grids used by MotifRenderer simplified overload
// AI: invariants=StartTick>=0; DurationTicks>0; IsStrongBeat indicates quarter-note boundary
// AI: deps=Mapped to absolute song ticks; keep record shape stable for consumers and tests
namespace Music.Song.Material;

// AI: contract=StartTick absolute song tick; DurationTicks length in ticks; IsStrongBeat used for dynamics
public sealed record OnsetSlot(
    long StartTick,
    int DurationTicks,
    bool IsStrongBeat);
