// AI: purpose=Per-role beat position matrix for pattern comparison across bars (Story 7.2a).
// AI: invariants=BarSlots indexed [barIndex][gridPosition]; null slot = no hit; GridResolution defines slot count per bar.
// AI: deps=Populated by BeatPositionMatrixBuilder; enables rapid pattern comparison across bars.
// AI: change=Story 7.2a; extend with additional metadata per slot as needed.

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Represents a single beat position slot with velocity and timing data.
/// Null slot means no hit at that position.
/// Story 7.2a: Beat Position Matrix.
/// </summary>
/// <param name="Velocity">MIDI velocity (1-127) at this position.</param>
/// <param name="TimingOffsetTicks">Timing deviation from grid position in ticks.</param>
public sealed record BeatPositionSlot(int Velocity, int TimingOffsetTicks);

/// <summary>
/// Per-role matrix of beat positions across all bars.
/// Enables rapid pattern comparison and variation detection.
/// Structure: BarSlots[barIndex][gridPosition] → slot data (or null if no hit).
/// Story 7.2a: Beat Position Matrix.
/// </summary>
public sealed record BeatPositionMatrix
{
    /// <summary>
    /// Role this matrix represents (e.g., "Kick", "Snare").
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// Total number of bars in this matrix.
    /// </summary>
    public required int TotalBars { get; init; }

    /// <summary>
    /// Grid resolution (positions per bar).
    /// Default: 16 for 16th notes in 4/4.
    /// </summary>
    public required int GridResolution { get; init; }

    /// <summary>
    /// Matrix of slots: [barIndex][gridPosition] → BeatPositionSlot or null.
    /// barIndex is 0-based (bar 1 = index 0).
    /// gridPosition is 0-based within bar.
    /// </summary>
    public required IReadOnlyList<BeatPositionSlot?[]> BarSlots { get; init; }

    /// <summary>
    /// Total hit count across all bars for this role.
    /// </summary>
    public int TotalHits => BarSlots.Sum(bar => bar.Count(s => s != null));

    /// <summary>
    /// Gets the slot at a specific bar and grid position.
    /// </summary>
    /// <param name="barIndex">0-based bar index.</param>
    /// <param name="gridPosition">0-based grid position within bar.</param>
    /// <returns>Slot data or null if no hit.</returns>
    public BeatPositionSlot? GetSlot(int barIndex, int gridPosition)
    {
        if (barIndex < 0 || barIndex >= BarSlots.Count)
            return null;

        var bar = BarSlots[barIndex];
        if (gridPosition < 0 || gridPosition >= bar.Length)
            return null;

        return bar[gridPosition];
    }

    /// <summary>
    /// Gets all grid positions with hits for a specific bar.
    /// </summary>
    /// <param name="barIndex">0-based bar index.</param>
    /// <returns>Enumerable of grid positions with hits.</returns>
    public IEnumerable<int> GetHitPositions(int barIndex)
    {
        if (barIndex < 0 || barIndex >= BarSlots.Count)
            yield break;

        var bar = BarSlots[barIndex];
        for (int pos = 0; pos < bar.Length; pos++)
        {
            if (bar[pos] != null)
                yield return pos;
        }
    }

    /// <summary>
    /// Calculates average velocity for this role across all bars.
    /// </summary>
    /// <returns>Average velocity, or 0 if no hits.</returns>
    public double GetAverageVelocity()
    {
        var velocities = BarSlots
            .SelectMany(bar => bar.Where(s => s != null).Select(s => s!.Velocity))
            .ToList();

        return velocities.Count == 0 ? 0 : velocities.Average();
    }

    /// <summary>
    /// Calculates average timing offset for this role across all bars.
    /// </summary>
    /// <returns>Average timing offset in ticks.</returns>
    public double GetAverageTimingOffset()
    {
        var offsets = BarSlots
            .SelectMany(bar => bar.Where(s => s != null).Select(s => s!.TimingOffsetTicks))
            .ToList();

        return offsets.Count == 0 ? 0 : offsets.Average();
    }
}
