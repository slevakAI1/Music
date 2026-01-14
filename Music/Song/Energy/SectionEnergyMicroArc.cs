// AI: purpose=Per-bar energy micro-arc for within-section phrase-level energy shaping (Story 7.8).
// AI: invariants=All EnergyDelta [-0.10..+0.10]; BarCount matches section; phrase positions deterministic; integrates with MicroTensionMap.
// AI: deps=Computed from section length+energy+style; consumed by role generators for velocity/density/orchestration micro-modulation.

namespace Music.Generator;

/// <summary>
/// Immutable per-bar energy micro-arc for a section.
/// Provides phrase-level energy shaping (start ? build ? peak ? cadence) within sections.
/// Story 7.8: standardizes minimal phrase-position semantics and subtle per-bar modulation.
/// Integrates with MicroTensionMap (7.5.3) and SectionVariationPlan (7.6).
/// </summary>
public sealed record SectionEnergyMicroArc
{
    /// <summary>
    /// Energy delta [-0.10..+0.10] for each bar in the section.
    /// Applied as additive bias to role parameters (velocity, density).
    /// Index = bar index within section (0-based).
    /// </summary>
    public required IReadOnlyList<double> EnergyDeltaByBar { get; init; }

    /// <summary>
    /// Phrase position classification for each bar.
    /// Used to determine micro-level intent (start/middle/peak/cadence behavior).
    /// </summary>
    public required IReadOnlyList<PhrasePosition> PhrasePositionByBar { get; init; }

    /// <summary>
    /// Number of bars in this micro-arc (convenience property).
    /// </summary>
    public int BarCount => EnergyDeltaByBar.Count;

    /// <summary>
    /// Gets energy delta for a specific bar within the section.
    /// </summary>
    /// <param name="barIndexWithinSection">0-based bar index within section.</param>
    /// <returns>Energy delta [-0.10..+0.10].</returns>
    public double GetEnergyDelta(int barIndexWithinSection)
    {
        if (barIndexWithinSection < 0 || barIndexWithinSection >= BarCount)
            return 0.0;

        return EnergyDeltaByBar[barIndexWithinSection];
    }

    /// <summary>
    /// Gets phrase position for a specific bar within the section.
    /// </summary>
    /// <param name="barIndexWithinSection">0-based bar index within section.</param>
    /// <returns>Phrase position classification.</returns>
    public PhrasePosition GetPhrasePosition(int barIndexWithinSection)
    {
        if (barIndexWithinSection < 0 || barIndexWithinSection >= BarCount)
            return PhrasePosition.Middle;

        return PhrasePositionByBar[barIndexWithinSection];
    }

    /// <summary>
    /// Creates a flat micro-arc with zero deltas and Middle positions.
    /// Used for sections where phrase-level shaping is not desired.
    /// </summary>
    public static SectionEnergyMicroArc Flat(int barCount)
    {
        if (barCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(barCount), "Bar count must be positive");

        return new SectionEnergyMicroArc
        {
            EnergyDeltaByBar = Enumerable.Repeat(0.0, barCount).ToList(),
            PhrasePositionByBar = Enumerable.Repeat(PhrasePosition.Middle, barCount).ToList()
        };
    }

    /// <summary>
    /// Builds deterministic energy micro-arc from section characteristics.
    /// Infers phrase length using same logic as MicroTensionMap (4 bars typical, 2 for 4-bar sections).
    /// Applies subtle per-bar modulation: velocity lift at Peak, density thinning at Cadence.
    /// </summary>
    /// <param name="barCount">Number of bars in the section.</param>
    /// <param name="sectionEnergy">Base energy level for the section [0..1].</param>
    /// <param name="phraseLength">Optional explicit phrase length; inferred if null.</param>
    /// <param name="seed">Seed for deterministic tiny jitter (0 = no jitter).</param>
    /// <returns>Deterministic micro-arc with phrase positions and energy deltas.</returns>
    public static SectionEnergyMicroArc Build(
        int barCount,
        double sectionEnergy,
        int? phraseLength = null,
        int seed = 0)
    {
        if (barCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(barCount), "Bar count must be positive");

        sectionEnergy = Math.Clamp(sectionEnergy, 0.0, 1.0);

        // Fallback mode: infer sensible phrase length (same logic as MicroTensionMap)
        int phr = phraseLength ?? (barCount == 4 ? 2 : 4);
        if (phr <= 0) phr = 4;

        var energyDeltas = new double[barCount];
        var phrasePositions = new PhrasePosition[barCount];

        var rng = new SeededRandomSource(seed);

        // Scale factor based on section energy: higher energy ? more pronounced deltas
        double scaleFactor = 0.03 + (sectionEnergy * 0.07); // Range [0.03..0.10]

        for (int i = 0; i < barCount; i++)
        {
            int barInPhrase = i % phr;
            double phraseFactor = (double)barInPhrase / Math.Max(1, phr - 1);

            // Classify phrase position
            PhrasePosition position;
            if (barInPhrase == 0)
            {
                position = PhrasePosition.Start;
            }
            else if ((i + 1) % phr == 0 || i == barCount - 1)
            {
                position = PhrasePosition.Cadence;
            }
            else if (phraseFactor >= 0.65)
            {
                position = PhrasePosition.Peak;
            }
            else
            {
                position = PhrasePosition.Middle;
            }

            phrasePositions[i] = position;

            // Calculate energy delta based on position
            double delta = position switch
            {
                PhrasePosition.Start => 0.0, // Baseline
                PhrasePosition.Middle => scaleFactor * 0.3, // Slight rise
                PhrasePosition.Peak => scaleFactor * 1.0, // Maximum lift
                PhrasePosition.Cadence => -scaleFactor * 0.5, // Slight pull back
                _ => 0.0
            };

            // Apply tiny seeded jitter to avoid exact repeats
            if (seed != 0)
            {
                double jitter = (rng.NextDouble() - 0.5) * 0.01;
                delta += jitter;
            }

            energyDeltas[i] = Math.Clamp(delta, -0.10, 0.10);
        }

        return new SectionEnergyMicroArc
        {
            EnergyDeltaByBar = energyDeltas.ToList(),
            PhrasePositionByBar = phrasePositions.ToList()
        };
    }
}
