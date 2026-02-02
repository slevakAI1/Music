// AI: purpose=Builds BeatPositionMatrix from drum events for a single role (Story 7.2a).
// AI: invariants=Output matrix aligned with BarTrack bar count; handles variable time signatures per bar.
// AI: deps=Consumes DrumMidiEvent list, BarTrack; outputs BeatPositionMatrix.
// AI: change=Story 7.2a; extend for additional matrix metadata as needed.

using Music.Generator;

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Builds BeatPositionMatrix from drum events for pattern analysis.
/// Creates per-role matrices enabling rapid pattern comparison across bars.
/// Story 7.2a: Beat Position Matrix.
/// </summary>
public static class BeatPositionMatrixBuilder
{
    /// <summary>
    /// Default grid resolution (16th notes).
    /// </summary>
    public const int DefaultGridResolution = 16;

    /// <summary>
    /// Builds a matrix for a single role from the event list.
    /// </summary>
    /// <param name="events">All drum events (will be filtered to specified role).</param>
    /// <param name="barTrack">BarTrack for timing context.</param>
    /// <param name="role">Role to build matrix for (e.g., "Kick").</param>
    /// <param name="gridResolution">Grid positions per bar (default: 16).</param>
    /// <returns>Matrix with slot data for each bar and grid position.</returns>
    public static BeatPositionMatrix Build(
        IReadOnlyList<DrumMidiEvent> events,
        BarTrack barTrack,
        string role,
        int gridResolution = DefaultGridResolution)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(barTrack);
        ArgumentNullException.ThrowIfNull(role);

        var totalBars = barTrack.Bars.Count;
        var barSlots = new List<BeatPositionSlot?[]>();

        // Filter events to this role
        var roleEvents = events.Where(e => e.Role == role).ToList();

        // Group events by bar number
        var eventsByBar = roleEvents
            .GroupBy(e => e.BarNumber)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Build slots for each bar
        foreach (var bar in barTrack.Bars)
        {
            var slots = new BeatPositionSlot?[gridResolution];

            if (eventsByBar.TryGetValue(bar.BarNumber, out var barEvents))
            {
                foreach (var evt in barEvents)
                {
                    var gridPosition = CalculateGridPosition(
                        evt.Beat,
                        bar.BeatsPerBar,
                        gridResolution);

                    // Clamp to valid range
                    gridPosition = Math.Clamp(gridPosition, 0, gridResolution - 1);

                    // If position already occupied, keep the louder hit
                    var existingSlot = slots[gridPosition];
                    if (existingSlot == null || evt.Velocity > existingSlot.Velocity)
                    {
                        slots[gridPosition] = new BeatPositionSlot(
                            evt.Velocity,
                            evt.TimingOffsetTicks ?? 0);
                    }
                }
            }

            barSlots.Add(slots);
        }

        return new BeatPositionMatrix
        {
            Role = role,
            TotalBars = totalBars,
            GridResolution = gridResolution,
            BarSlots = barSlots
        };
    }

    /// <summary>
    /// Builds matrices for all roles present in the event list.
    /// </summary>
    /// <param name="events">All drum events.</param>
    /// <param name="barTrack">BarTrack for timing context.</param>
    /// <param name="gridResolution">Grid positions per bar.</param>
    /// <returns>Dictionary of role name to matrix.</returns>
    public static IReadOnlyDictionary<string, BeatPositionMatrix> BuildAll(
        IReadOnlyList<DrumMidiEvent> events,
        BarTrack barTrack,
        int gridResolution = DefaultGridResolution)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(barTrack);

        // Get all unique roles
        var roles = events.Select(e => e.Role).Distinct().ToList();

        var result = new Dictionary<string, BeatPositionMatrix>();

        foreach (var role in roles)
        {
            result[role] = Build(events, barTrack, role, gridResolution);
        }

        return result;
    }

    /// <summary>
    /// Calculates grid position (0-based) from beat position (1-based).
    /// </summary>
    private static int CalculateGridPosition(decimal beat, int beatsPerBar, int gridResolution)
    {
        // Convert 1-based beat to 0-based
        var beatZeroBased = beat - 1m;

        // Calculate fraction of bar
        var barFraction = beatZeroBased / beatsPerBar;

        // Map to grid position
        return (int)Math.Round(barFraction * gridResolution);
    }

    /// <summary>
    /// Compares two matrices to find differing positions.
    /// Useful for detecting pattern variations.
    /// </summary>
    /// <param name="a">First matrix.</param>
    /// <param name="b">Second matrix.</param>
    /// <returns>List of (barIndex, gridPosition) tuples where patterns differ.</returns>
    public static IReadOnlyList<(int BarIndex, int GridPosition)> FindDifferences(
        BeatPositionMatrix a,
        BeatPositionMatrix b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var differences = new List<(int, int)>();

        var maxBars = Math.Max(a.TotalBars, b.TotalBars);
        var maxGrid = Math.Max(a.GridResolution, b.GridResolution);

        for (int barIndex = 0; barIndex < maxBars; barIndex++)
        {
            for (int gridPos = 0; gridPos < maxGrid; gridPos++)
            {
                var slotA = a.GetSlot(barIndex, gridPos);
                var slotB = b.GetSlot(barIndex, gridPos);

                // Difference = one has hit and other doesn't
                if ((slotA == null) != (slotB == null))
                {
                    differences.Add((barIndex, gridPos));
                }
            }
        }

        return differences;
    }
}
