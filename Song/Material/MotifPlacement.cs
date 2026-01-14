// AI: purpose=Immutable record describing WHERE a motif appears in song structure.
// AI: invariants=AbsoluteSectionIndex >= 0; StartBarWithinSection >= 0; DurationBars >= 1; VariationIntensity [0..1].
// AI: deps=Story 9.1 placement data; consumed by Story 9.2 renderer; references PartTrack.PartTrackId from Story M1.

using Music.Generator;

namespace Music.Song.Material;

/// <summary>
/// Describes a single placement of a motif within the song structure.
/// Specifies WHICH motif, WHERE it appears, and HOW it should be varied.
/// Story 9.1: minimal placement intent; rendering happens in Story 9.2.
/// </summary>
public sealed record MotifPlacement
{
    /// <summary>
    /// Identity of the motif to place (references PartTrack in MaterialBank).
    /// </summary>
    public required PartTrack.PartTrackId MotifId { get; init; }

    /// <summary>
    /// Absolute section index (0-based) where motif appears.
    /// </summary>
    public required int AbsoluteSectionIndex { get; init; }

    /// <summary>
    /// Bar index within section (0-based) where motif starts.
    /// </summary>
    public required int StartBarWithinSection { get; init; }

    /// <summary>
    /// Duration in bars.
    /// Must be >= 1.
    /// </summary>
    public required int DurationBars { get; init; }

    /// <summary>
    /// Variation intensity [0..1] for A/A' rendering differences.
    /// 0.0 = exact repeat; 1.0 = maximum bounded variation.
    /// </summary>
    public required double VariationIntensity { get; init; }

    /// <summary>
    /// Optional transform tags (e.g., "OctaveUp", "Invert", "Syncopate").
    /// Used by Story 9.2 renderer for deterministic variation operators.
    /// </summary>
    public IReadOnlySet<string> TransformTags { get; init; } = new HashSet<string>();

    /// <summary>
    /// Creates a motif placement with validation.
    /// </summary>
    public static MotifPlacement Create(
        PartTrack.PartTrackId motifId,
        int absoluteSectionIndex,
        int startBarWithinSection,
        int durationBars,
        double variationIntensity = 0.0,
        IEnumerable<string>? transformTags = null)
    {
        if (absoluteSectionIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(absoluteSectionIndex), "Must be >= 0");
        if (startBarWithinSection < 0)
            throw new ArgumentOutOfRangeException(nameof(startBarWithinSection), "Must be >= 0");
        if (durationBars < 1)
            throw new ArgumentOutOfRangeException(nameof(durationBars), "Must be >= 1");

        return new MotifPlacement
        {
            MotifId = motifId,
            AbsoluteSectionIndex = absoluteSectionIndex,
            StartBarWithinSection = startBarWithinSection,
            DurationBars = durationBars,
            VariationIntensity = Math.Clamp(variationIntensity, 0.0, 1.0),
            TransformTags = transformTags != null ? new HashSet<string>(transformTags) : new HashSet<string>()
        };
    }
}
