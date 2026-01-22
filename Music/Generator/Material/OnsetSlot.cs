// AI: purpose=Simple onset slot record for generator-friendly MotifRenderer overload.
// AI: deps=Used by MotifRenderer.Render simplified overload; provides pre-computed onset grid.
// AI: invariants=StartTick >= 0; DurationTicks > 0; immutable record.

namespace Music.Song.Material;

/// <summary>
/// Represents a single onset slot in a pre-computed onset grid.
/// Used by the simplified MotifRenderer overload for generator contexts
/// that have already computed harmony contexts and onset positions.
/// </summary>
/// <param name="StartTick">Absolute song tick where this onset occurs.</param>
/// <param name="DurationTicks">Duration in ticks until next onset or end of phrase.</param>
/// <param name="IsStrongBeat">True if onset falls on a strong beat (quarter note boundary).</param>
public sealed record OnsetSlot(
    long StartTick,
    int DurationTicks,
    bool IsStrongBeat);
