// AI: purpose=Per-bar micro tension map for a section, enabling within-section tension shaping.
// AI: invariants=All tension values [0..1]; BarCount matches section length; flags deterministic by bar index.
// AI: deps=Computed by micro tension mapper (Story 7.5.3); consumed by tension hooks (Story 7.5.4).

namespace Music.Generator;

/// <summary>
/// Immutable per-bar micro tension map for a section.
/// Enables within-section tension shaping (rise toward phrase peaks/cadences).
/// Computed deterministically from section characteristics and phrase segmentation.
/// </summary>
public sealed record MicroTensionMap
{
    /// <summary>
    /// Micro tension value [0..1] for each bar in the section.
    /// Index = bar index within section (0-based).
    /// </summary>
    public required IReadOnlyList<double> TensionByBar { get; init; }

    /// <summary>
    /// Flags indicating whether each bar is a phrase end.
    /// True for last bar of a phrase within the section.
    /// </summary>
    public required IReadOnlyList<bool> IsPhraseEnd { get; init; }

    /// <summary>
    /// Flags indicating whether each bar is a section end.
    /// True only for the last bar of the section.
    /// </summary>
    public required IReadOnlyList<bool> IsSectionEnd { get; init; }

    /// <summary>
    /// Flags indicating whether each bar is a section start.
    /// True only for the first bar of the section.
    /// </summary>
    public required IReadOnlyList<bool> IsSectionStart { get; init; }

    /// <summary>
    /// Number of bars in this map (convenience property).
    /// </summary>
    public int BarCount => TensionByBar.Count;

    /// <summary>
    /// Gets micro tension for a specific bar within the section.
    /// </summary>
    /// <param name="barIndexWithinSection">0-based bar index within section.</param>
    /// <returns>Micro tension value [0..1].</returns>
    public double GetTension(int barIndexWithinSection)
    {
        if (barIndexWithinSection < 0 || barIndexWithinSection >= BarCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(barIndexWithinSection),
                $"Bar index {barIndexWithinSection} out of range [0..{BarCount - 1}]");
        }

        return TensionByBar[barIndexWithinSection];
    }

    /// <summary>
    /// Gets all flags for a specific bar within the section.
    /// </summary>
    public (bool IsPhraseEnd, bool IsSectionEnd, bool IsSectionStart) GetFlags(int barIndexWithinSection)
    {
        if (barIndexWithinSection < 0 || barIndexWithinSection >= BarCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(barIndexWithinSection),
                $"Bar index {barIndexWithinSection} out of range [0..{BarCount - 1}]");
        }

        return (
            IsPhraseEnd[barIndexWithinSection],
            IsSectionEnd[barIndexWithinSection],
            IsSectionStart[barIndexWithinSection]
        );
    }

    /// <summary>
    /// Creates a flat (constant) micro tension map for a section.
    /// </summary>
    public static MicroTensionMap Flat(int barCount, double tension)
    {
        var tensionValues = Enumerable.Repeat(Math.Clamp(tension, 0.0, 1.0), barCount).ToList();
        var phraseEnds = new bool[barCount];
        var sectionEnds = new bool[barCount];
        var sectionStarts = new bool[barCount];

        // Mark first and last bars
        if (barCount > 0)
        {
            sectionStarts[0] = true;
            sectionEnds[barCount - 1] = true;
        }

        return new MicroTensionMap
        {
            TensionByBar = tensionValues,
            IsPhraseEnd = phraseEnds,
            IsSectionEnd = sectionEnds,
            IsSectionStart = sectionStarts
        };
    }

    /// <summary>
    /// Creates a micro tension map with default 4-bar phrase segmentation.
    /// Tension rises toward phrase ends (simple linear increase within each phrase).
    /// </summary>
    public static MicroTensionMap WithSimplePhrases(
        int barCount,
        double baseTension,
        int phraseLength = 4)
    {
        if (barCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(barCount), "Bar count must be positive");
        }

        if (phraseLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(phraseLength), "Phrase length must be positive");
        }

        baseTension = Math.Clamp(baseTension, 0.0, 1.0);

        var tensionValues = new double[barCount];
        var phraseEnds = new bool[barCount];
        var sectionEnds = new bool[barCount];
        var sectionStarts = new bool[barCount];

        // Mark section boundaries
        sectionStarts[0] = true;
        sectionEnds[barCount - 1] = true;

        // Compute tension and phrase boundaries
        for (int i = 0; i < barCount; i++)
        {
            int barInPhrase = i % phraseLength;
            bool isLastBarInPhrase = (i + 1) % phraseLength == 0 || i == barCount - 1;

            // Linear rise within phrase: baseTension at start, baseTension * 1.5 at end
            double phraseFactor = (double)barInPhrase / (phraseLength - 1);
            double tensionMultiplier = 1.0 + (phraseFactor * 0.5);
            tensionValues[i] = Math.Clamp(baseTension * tensionMultiplier, 0.0, 1.0);

            phraseEnds[i] = isLastBarInPhrase;
        }

        return new MicroTensionMap
        {
            TensionByBar = tensionValues,
            IsPhraseEnd = phraseEnds,
            IsSectionEnd = sectionEnds,
            IsSectionStart = sectionStarts
        };
    }
}
