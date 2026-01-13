// AI: purpose=Phrase position labels for within-section bar classification (Story 7.8).
// AI: invariants=Four positions cover all phrase bars; Start=first, Cadence=last, Peak near 75%, Middle=rest.
// AI: deps=Used by SectionEnergyMicroArc to classify bars; consumed by role generators for micro-level intent.

namespace Music.Generator;

/// <summary>
/// Phrase position classification for bars within a phrase.
/// Used to apply phrase-aware micro-level energy/tension modulation.
/// Story 7.8: standardizes minimal phrase-position semantics before formal PhraseMap (Stage 8).
/// </summary>
public enum PhrasePosition
{
    /// <summary>
    /// First bar(s) of a phrase: phrase start, downbeat anchor.
    /// Typical use: establish energy baseline, section-start impacts.
    /// </summary>
    Start,

    /// <summary>
    /// Middle bar(s) of a phrase: transitional, rising toward peak.
    /// Typical use: moderate activity, building momentum.
    /// </summary>
    Middle,

    /// <summary>
    /// Near-end bar(s) of a phrase: phrase climax, highest intensity point.
    /// Typical use: velocity lift, accent placement, register emphasis.
    /// </summary>
    Peak,

    /// <summary>
    /// Final bar(s) of a phrase: phrase end, cadence point.
    /// Typical use: density thinning, pull/fill events, releases.
    /// </summary>
    Cadence
}
