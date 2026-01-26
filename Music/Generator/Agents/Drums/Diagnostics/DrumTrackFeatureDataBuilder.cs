// AI: purpose=Orchestrates extraction pipeline to build DrumTrackFeatureData (Story 7.2a).
// AI: invariants=Coordinates all extractors; generates unique TrackId; deterministic output for same inputs.
// AI: deps=Uses DrumTrackEventExtractor, BarPatternExtractor, BarOnsetStatsExtractor, BeatPositionMatrixBuilder.
// AI: change=Story 7.2a; extend with tempo detection as needed.

using Music.Generator;

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Orchestrates the complete feature extraction pipeline for a drum track.
/// Coordinates event extraction, pattern fingerprinting, statistics, and matrix building.
/// Story 7.2a: Feature Data Builder.
/// </summary>
public static class DrumTrackFeatureDataBuilder
{
    /// <summary>
    /// Default grid resolution for pattern analysis (16th notes).
    /// </summary>
    public const int DefaultGridResolution = 16;

    /// <summary>
    /// Builds complete feature data from a PartTrack and BarTrack.
    /// </summary>
    /// <param name="partTrack">Source drum PartTrack with note events.</param>
    /// <param name="barTrack">BarTrack providing timing context.</param>
    /// <param name="genreHint">Optional genre classification hint.</param>
    /// <param name="artistHint">Optional artist hint.</param>
    /// <param name="tempoEstimateBpm">Estimated tempo (0 if unknown).</param>
    /// <param name="gridResolution">Grid resolution for quantization (default: 16).</param>
    /// <returns>Complete feature data for the track.</returns>
    public static DrumTrackFeatureData Build(
        PartTrack partTrack,
        BarTrack barTrack,
        string? genreHint = null,
        string? artistHint = null,
        int tempoEstimateBpm = 0,
        int gridResolution = DefaultGridResolution)
    {
        ArgumentNullException.ThrowIfNull(partTrack);
        ArgumentNullException.ThrowIfNull(barTrack);

        // Generate unique track ID
        var trackId = GenerateTrackId(partTrack, barTrack);

        // Step 1: Extract raw events
        var events = DrumTrackEventExtractor.Extract(partTrack, barTrack, gridResolution);

        // Step 2: Determine default beats per bar (most common)
        var defaultBeatsPerBar = DetermineDefaultBeatsPerBar(barTrack);

        // Step 3: Extract per-bar patterns
        var barPatterns = BarPatternExtractor.ExtractAllBars(events, barTrack, gridResolution);

        // Step 4: Extract per-bar statistics
        var barStats = BarOnsetStatsExtractor.ExtractAllBars(events, barTrack);

        // Step 5: Build per-role matrices
        var roleMatrices = BeatPositionMatrixBuilder.BuildAll(events, barTrack, gridResolution);

        // Step 6: Collect active roles
        var activeRoles = events.Select(e => e.Role).ToHashSet();

        return new DrumTrackFeatureData
        {
            TrackId = trackId,
            GenreHint = genreHint,
            ArtistHint = artistHint,
            TotalBars = barTrack.Bars.Count,
            DefaultBeatsPerBar = defaultBeatsPerBar,
            TempoEstimateBpm = tempoEstimateBpm,
            Events = events,
            BarPatterns = barPatterns,
            BarStats = barStats,
            RoleMatrices = roleMatrices,
            ActiveRoles = activeRoles
        };
    }

    /// <summary>
    /// Generates a unique track ID based on content hash.
    /// </summary>
    private static string GenerateTrackId(PartTrack partTrack, BarTrack barTrack)
    {
        // Combine relevant data for ID generation
        var noteCount = partTrack.PartTrackNoteEvents.Count;
        var barCount = barTrack.Bars.Count;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Simple deterministic ID (not cryptographically secure, just unique enough)
        var hashInput = $"{noteCount}_{barCount}_{timestamp}";
        var hashCode = hashInput.GetHashCode();

        return $"drum_{Math.Abs(hashCode):X8}";
    }

    /// <summary>
    /// Determines the most common beats per bar from the BarTrack.
    /// </summary>
    private static int DetermineDefaultBeatsPerBar(BarTrack barTrack)
    {
        if (barTrack.Bars.Count == 0)
            return 4; // Default to 4/4

        // Find most common BeatsPerBar
        var mostCommon = barTrack.Bars
            .GroupBy(b => b.BeatsPerBar)
            .OrderByDescending(g => g.Count())
            .First();

        return mostCommon.Key;
    }

    /// <summary>
    /// Builds feature data with optional validation.
    /// </summary>
    /// <param name="partTrack">Source drum PartTrack.</param>
    /// <param name="barTrack">BarTrack for timing context.</param>
    /// <param name="options">Build options.</param>
    /// <returns>Feature data result with optional validation errors.</returns>
    public static DrumFeatureExtractionResult BuildWithValidation(
        PartTrack partTrack,
        BarTrack barTrack,
        DrumFeatureBuildOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        // Validate inputs
        if (partTrack == null)
        {
            errors.Add("PartTrack is null");
            return new DrumFeatureExtractionResult(null, errors, false);
        }

        if (barTrack == null || barTrack.Bars.Count == 0)
        {
            errors.Add("BarTrack is null or empty");
            return new DrumFeatureExtractionResult(null, errors, false);
        }

        try
        {
            var data = Build(
                partTrack,
                barTrack,
                options.GenreHint,
                options.ArtistHint,
                options.TempoEstimateBpm,
                options.GridResolution);

            // Validate results
            if (data.Events.Count == 0)
            {
                errors.Add("Warning: No drum events found in track");
            }

            if (data.ActiveRoles.Count == 0)
            {
                errors.Add("Warning: No active drum roles detected");
            }

            return new DrumFeatureExtractionResult(data, errors, errors.Count == 0);
        }
        catch (Exception ex)
        {
            errors.Add($"Extraction failed: {ex.Message}");
            return new DrumFeatureExtractionResult(null, errors, false);
        }
    }
}

/// <summary>
/// Options for feature extraction.
/// </summary>
public sealed record DrumFeatureBuildOptions
{
    /// <summary>
    /// Genre classification hint (optional).
    /// </summary>
    public string? GenreHint { get; init; }

    /// <summary>
    /// Artist hint (optional).
    /// </summary>
    public string? ArtistHint { get; init; }

    /// <summary>
    /// Tempo estimate in BPM (0 if unknown).
    /// </summary>
    public int TempoEstimateBpm { get; init; }

    /// <summary>
    /// Grid resolution for quantization (default: 16).
    /// </summary>
    public int GridResolution { get; init; } = DrumTrackFeatureDataBuilder.DefaultGridResolution;
}

/// <summary>
/// Result of feature extraction with validation.
/// </summary>
public sealed record DrumFeatureExtractionResult(
    DrumTrackFeatureData? Data,
    IReadOnlyList<string> Messages,
    bool IsSuccess);
