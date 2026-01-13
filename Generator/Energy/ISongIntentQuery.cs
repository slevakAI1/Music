// AI: purpose=Unified Stage 7 intent query surface for Stage 8/9 motif placement and melody generation.
// AI: invariants=All queries deterministic; thread-safe immutable reads; combines energy/tension/variation/orchestration.
// AI: deps=Aggregates EnergyArc, ITensionQuery, IVariationQuery without owning them; consumed by future motif/melody stages.
// AI: change=Story 7.9 integration contract; no new planners, only query aggregation of existing Stage 7 outputs.

namespace Music.Generator;

/// <summary>
/// Unified Stage 7 intent query for downstream stages (Stage 8 motif placement, Stage 9 melody).
/// Provides single stable query surface combining energy, tension, variation, and orchestration intent.
/// Story 7.9: future-proofs Stage 7 by aggregating existing queries without requiring refactors.
/// </summary>
public interface ISongIntentQuery
{
    /// <summary>
    /// Gets unified section-level intent context.
    /// Combines energy target, tension, variation plan, orchestration, and register constraints.
    /// Primary query method for coarse-grained section-level decisions.
    /// </summary>
    /// <param name="absoluteSectionIndex">Absolute 0-based section index in the song.</param>
    /// <returns>Immutable section intent context.</returns>
    SectionIntentContext GetSectionIntent(int absoluteSectionIndex);

    /// <summary>
    /// Gets unified bar-level intent context.
    /// Combines section intent with bar-specific micro energy/tension, phrase position, and flags.
    /// Primary query method for fine-grained bar-level decisions (motif placement, melody timing).
    /// </summary>
    /// <param name="absoluteSectionIndex">Absolute 0-based section index in the song.</param>
    /// <param name="barIndexWithinSection">0-based bar index within the section.</param>
    /// <returns>Immutable bar intent context.</returns>
    BarIntentContext GetBarIntent(int absoluteSectionIndex, int barIndexWithinSection);

    /// <summary>
    /// Gets the total number of sections in the song.
    /// </summary>
    int SectionCount { get; }

    /// <summary>
    /// Checks if intent data is available for a section.
    /// </summary>
    bool HasIntentData(int absoluteSectionIndex);
}

/// <summary>
/// Section-level intent context combining all Stage 7 macro-level planning outputs.
/// Used by Stage 8/9 for section-wide decisions (motif placement, orchestration, arrangement).
/// Story 7.9: immutable aggregation of energy/tension/variation/orchestration profiles.
/// </summary>
public sealed record SectionIntentContext
{
    /// <summary>
    /// Absolute section index in the song (0-based).
    /// </summary>
    public required int AbsoluteSectionIndex { get; init; }

    /// <summary>
    /// Section type (Verse, Chorus, Bridge, etc.).
    /// </summary>
    public required MusicConstants.eSectionType SectionType { get; init; }

    /// <summary>
    /// Target energy level [0..1] for this section.
    /// 0.0 = minimal energy; 1.0 = maximum energy.
    /// </summary>
    public required double Energy { get; init; }

    /// <summary>
    /// Target tension level [0..1] for this section.
    /// 0.0 = resolved/stable; 1.0 = maximum tension/anticipation.
    /// </summary>
    public required double Tension { get; init; }

    /// <summary>
    /// Tension drivers explaining why this section has its tension value.
    /// Used for arrangement decisions (build-up, breakdown, anticipation).
    /// </summary>
    public required TensionDriver TensionDrivers { get; init; }

    /// <summary>
    /// Section transition hint (Build/Release/Sustain/Drop) for boundary FROM this section TO next.
    /// None if last section. Used for motif placement and melodic phrase shaping.
    /// </summary>
    public required SectionTransitionHint TransitionHint { get; init; }

    /// <summary>
    /// Variation intensity [0..1] indicating how much this section varies from its base reference.
    /// 0.0 = exact repeat (A); 1.0 = maximum variation (B).
    /// Used for A/A'/B repetition and controlled evolution decisions.
    /// </summary>
    public required double VariationIntensity { get; init; }

    /// <summary>
    /// Base reference section index for variation (null if this is the base/A section).
    /// Points to earlier section whose musical decisions should be reused/varied.
    /// </summary>
    public required int? BaseReferenceSectionIndex { get; init; }

    /// <summary>
    /// Tags indicating variation relationship (A, A', B, Lift, Thin, Breakdown, etc.).
    /// Used for high-level arrangement pattern recognition.
    /// </summary>
    public required IReadOnlySet<string> VariationTags { get; init; }

    /// <summary>
    /// Role presence/orchestration hints for this section.
    /// Indicates which roles are active and cymbal language preferences.
    /// </summary>
    public required RolePresenceHints RolePresence { get; init; }

    /// <summary>
    /// Reserved register bands preventing role collisions.
    /// Defines lead-space ceiling and bass floor constraints.
    /// </summary>
    public required RegisterConstraints RegisterConstraints { get; init; }

    /// <summary>
    /// Maximum density caps per role [0..1].
    /// Prevents arrangements from becoming too crowded.
    /// </summary>
    public required RoleDensityCaps DensityCaps { get; init; }
}

/// <summary>
/// Bar-level intent context combining section intent with bar-specific micro-level modulation.
/// Used by Stage 8/9 for fine-grained timing decisions (motif note placement, melody syllable timing).
/// Story 7.9: extends section intent with phrase position and micro energy/tension deltas.
/// </summary>
public sealed record BarIntentContext
{
    /// <summary>
    /// Parent section-level intent context.
    /// Contains all macro-level planning (energy, tension, variation, orchestration).
    /// </summary>
    public required SectionIntentContext Section { get; init; }

    /// <summary>
    /// Bar index within the section (0-based).
    /// </summary>
    public required int BarIndexWithinSection { get; init; }

    /// <summary>
    /// Bar-level (micro) tension value [0..1].
    /// Modulates section-level tension for phrase-level shaping.
    /// </summary>
    public required double MicroTension { get; init; }

    /// <summary>
    /// Energy delta [-0.10..+0.10] for this bar.
    /// Applied as additive bias to role parameters (velocity, density).
    /// </summary>
    public required double EnergyDelta { get; init; }

    /// <summary>
    /// Phrase position classification (Start/Middle/Peak/Cadence).
    /// Used for micro-level intent decisions (fills, accents, releases).
    /// </summary>
    public required PhrasePosition PhrasePosition { get; init; }

    /// <summary>
    /// True if this bar is a phrase end.
    /// Typical location for fills, melodic cadences, tension releases.
    /// </summary>
    public required bool IsPhraseEnd { get; init; }

    /// <summary>
    /// True if this bar is the section end.
    /// Major structural boundary; typical for crashes, big fills, transitions.
    /// </summary>
    public required bool IsSectionEnd { get; init; }

    /// <summary>
    /// True if this bar is the section start.
    /// Onset of new section identity; typical for crashes, new motifs.
    /// </summary>
    public required bool IsSectionStart { get; init; }

    /// <summary>
    /// Computed effective energy [0..1] for this bar (Section.Energy + EnergyDelta, clamped).
    /// Convenience property for bar-level decisions.
    /// </summary>
    public double EffectiveEnergy => Math.Clamp(Section.Energy + EnergyDelta, 0.0, 1.0);
}

/// <summary>
/// Role presence and orchestration hints for a section.
/// Indicates which roles are active and stylistic preferences (cymbal language).
/// Story 7.9: derived from EnergyOrchestrationProfile.
/// </summary>
public sealed record RolePresenceHints
{
    public required bool BassPresent { get; init; }
    public required bool CompPresent { get; init; }
    public required bool KeysPresent { get; init; }
    public required bool PadsPresent { get; init; }
    public required bool DrumsPresent { get; init; }

    /// <summary>
    /// Cymbal language preference (Standard/Minimal/Driving/Sparse).
    /// Guides drum orchestration choices.
    /// </summary>
    public required EnergyCymbalLanguage CymbalLanguage { get; init; }

    /// <summary>
    /// Whether to place crash cymbal at section start.
    /// </summary>
    public required bool CrashOnSectionStart { get; init; }

    /// <summary>
    /// Whether to prefer ride cymbal over hi-hat.
    /// </summary>
    public required bool PreferRideOverHat { get; init; }
}

/// <summary>
/// Reserved register bands preventing role collisions (lead space, bass floor).
/// Story 7.9: standardizes existing ad-hoc constraints as queryable contract.
/// </summary>
public sealed record RegisterConstraints
{
    /// <summary>
    /// Lead-space ceiling (MIDI note number).
    /// Comp/keys/pads should avoid sustained notes above this to reserve space for melody/lead.
    /// Default: MIDI 72 (C5).
    /// </summary>
    public required int LeadSpaceCeiling { get; init; }

    /// <summary>
    /// Bass floor (MIDI note number).
    /// Comp should avoid notes below this to prevent low-end muddiness.
    /// Default: MIDI 52 (E3).
    /// </summary>
    public required int BassFloor { get; init; }

    /// <summary>
    /// Vocal band reserved for future melody/lyrics [min, max] MIDI range.
    /// When melody exists, pads/keys should avoid sustained notes in this band.
    /// Default: [60, 76] (C4-E5, typical vocal range).
    /// </summary>
    public required (int MinMidi, int MaxMidi) VocalBand { get; init; }
}

/// <summary>
/// Maximum density caps per role to prevent arrangements from becoming too crowded.
/// Story 7.9: standardizes existing implicit caps as queryable contract.
/// Values are normalized [0..1] where 1.0 = maximum allowed density for that role.
/// </summary>
public sealed record RoleDensityCaps
{
    /// <summary>
    /// Maximum bass density [0..1].
    /// 1.0 allows all groove slots; lower values limit approach notes/pickups.
    /// </summary>
    public required double Bass { get; init; }

    /// <summary>
    /// Maximum comp density [0..1].
    /// 1.0 allows all pattern onsets; lower values limit weak-beat hits.
    /// </summary>
    public required double Comp { get; init; }

    /// <summary>
    /// Maximum keys density [0..1].
    /// 1.0 allows all pad onsets; lower values favor sustains over re-attacks.
    /// </summary>
    public required double Keys { get; init; }

    /// <summary>
    /// Maximum pads density [0..1].
    /// Similar to keys but typically more sustained (lower re-attack rate).
    /// </summary>
    public required double Pads { get; init; }

    /// <summary>
    /// Maximum drum density [0..1].
    /// 1.0 allows all groove events + fills; lower values limit ghost notes/hats.
    /// </summary>
    public required double Drums { get; init; }

    /// <summary>
    /// Default density caps (medium-high to allow normal generation).
    /// </summary>
    public static RoleDensityCaps Default() => new()
    {
        Bass = 0.85,
        Comp = 0.90,
        Keys = 0.85,
        Pads = 0.80,
        Drums = 0.90
    };

    /// <summary>
    /// High density caps (allow almost all slots/onsets).
    /// </summary>
    public static RoleDensityCaps High() => new()
    {
        Bass = 1.0,
        Comp = 1.0,
        Keys = 1.0,
        Pads = 0.95,
        Drums = 1.0
    };

    /// <summary>
    /// Low density caps (favor sparse arrangements).
    /// </summary>
    public static RoleDensityCaps Low() => new()
    {
        Bass = 0.60,
        Comp = 0.65,
        Keys = 0.60,
        Pads = 0.50,
        Drums = 0.70
    };
}
