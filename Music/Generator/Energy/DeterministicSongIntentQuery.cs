// AI: purpose=Deterministic implementation of ISongIntentQuery aggregating existing Stage 7 queries.
// AI: invariants=All outputs deterministic from seed+inputs; thread-safe immutable; no new planning, only query aggregation.
// AI: deps=Aggregates EnergyArc, ITensionQuery, IVariationQuery; consumed by Generator and future Stage 8/9 systems.
// AI: change=Story 7.9 implementation; precomputes and caches section contexts for O(1) lookup.

namespace Music.Generator;

/// <summary>
/// Deterministic implementation of unified Stage 7 intent query.
/// Aggregates existing energy, tension, and variation queries into single stable contract.
/// Story 7.9: future-proofs Stage 7 by providing unified query surface without requiring refactors.
/// Precomputes section contexts at construction for O(1) query performance.
/// </summary>
public sealed class DeterministicSongIntentQuery : ISongIntentQuery
{
    private readonly Dictionary<int, EnergySectionProfile> _sectionProfiles;
    private readonly ITensionQuery _tensionQuery;
    private readonly IVariationQuery _variationQuery;
    private readonly Dictionary<int, SectionIntentContext> _sectionContextCache;
    private readonly int _sectionCount;

    /// <summary>
    /// Creates deterministic song intent query from existing Stage 7 infrastructure.
    /// Precomputes all section contexts at construction.
    /// </summary>
    /// <param name="sectionProfiles">Section energy profiles indexed by absolute section index.</param>
    /// <param name="tensionQuery">Tension query providing macro/micro tension and transition hints.</param>
    /// <param name="variationQuery">Variation query providing A/A'/B variation plans.</param>
    public DeterministicSongIntentQuery(
        Dictionary<int, EnergySectionProfile> sectionProfiles,
        ITensionQuery tensionQuery,
        IVariationQuery variationQuery)
    {
        _sectionProfiles = sectionProfiles ?? throw new ArgumentNullException(nameof(sectionProfiles));
        _tensionQuery = tensionQuery ?? throw new ArgumentNullException(nameof(tensionQuery));
        _variationQuery = variationQuery ?? throw new ArgumentNullException(nameof(variationQuery));

        _sectionCount = Math.Min(
            _sectionProfiles.Count,
            Math.Min(_tensionQuery.SectionCount, _variationQuery.SectionCount));

        // Precompute all section contexts for O(1) lookup
        _sectionContextCache = new Dictionary<int, SectionIntentContext>(_sectionCount);
        for (int i = 0; i < _sectionCount; i++)
        {
            _sectionContextCache[i] = BuildSectionContext(i);
        }
    }

    public int SectionCount => _sectionCount;

    public bool HasIntentData(int absoluteSectionIndex)
    {
        return absoluteSectionIndex >= 0 &&
               absoluteSectionIndex < _sectionCount &&
               _sectionContextCache.ContainsKey(absoluteSectionIndex);
    }

    public SectionIntentContext GetSectionIntent(int absoluteSectionIndex)
    {
        if (!HasIntentData(absoluteSectionIndex))
        {
            throw new ArgumentOutOfRangeException(
                nameof(absoluteSectionIndex),
                $"Section index {absoluteSectionIndex} out of range [0..{_sectionCount - 1}]");
        }

        return _sectionContextCache[absoluteSectionIndex];
    }

    public BarIntentContext GetBarIntent(int absoluteSectionIndex, int barIndexWithinSection)
    {
        var sectionContext = GetSectionIntent(absoluteSectionIndex);

        // Get micro tension and flags from tension query
        var microTension = _tensionQuery.GetMicroTension(absoluteSectionIndex, barIndexWithinSection);
        var (isPhraseEnd, isSectionEnd, isSectionStart) =
            _tensionQuery.GetPhraseFlags(absoluteSectionIndex, barIndexWithinSection);

        // Get energy delta and phrase position from section profile's micro-arc (if present)
        var profile = _sectionProfiles[absoluteSectionIndex];
        var energyDelta = profile.MicroArc?.GetEnergyDelta(barIndexWithinSection) ?? 0.0;
        var phrasePosition = profile.MicroArc?.GetPhrasePosition(barIndexWithinSection) ?? PhrasePosition.Middle;

        return new BarIntentContext
        {
            Section = sectionContext,
            BarIndexWithinSection = barIndexWithinSection,
            MicroTension = microTension,
            EnergyDelta = energyDelta,
            PhrasePosition = phrasePosition,
            IsPhraseEnd = isPhraseEnd,
            IsSectionEnd = isSectionEnd,
            IsSectionStart = isSectionStart
        };
    }

    private SectionIntentContext BuildSectionContext(int absoluteSectionIndex)
    {
        var profile = _sectionProfiles[absoluteSectionIndex];
        var macroTension = _tensionQuery.GetMacroTension(absoluteSectionIndex);
        var transitionHint = _tensionQuery.GetTransitionHint(absoluteSectionIndex);
        var variationPlan = _variationQuery.GetVariationPlan(absoluteSectionIndex);

        return new SectionIntentContext
        {
            AbsoluteSectionIndex = absoluteSectionIndex,
            SectionType = profile.Section.SectionType,
            Energy = profile.Global.Energy,
            Tension = macroTension.MacroTension,
            TensionDrivers = macroTension.Driver,
            TransitionHint = transitionHint,
            VariationIntensity = variationPlan.VariationIntensity,
            BaseReferenceSectionIndex = variationPlan.BaseReferenceSectionIndex,
            VariationTags = variationPlan.Tags,
            RolePresence = BuildRolePresenceHints(profile.Orchestration),
            RegisterConstraints = BuildRegisterConstraints(),
            DensityCaps = BuildDensityCaps(profile.Global.Energy)
        };
    }

    private static RolePresenceHints BuildRolePresenceHints(EnergyOrchestrationProfile orchestration)
    {
        return new RolePresenceHints
        {
            BassPresent = orchestration.BassPresent,
            CompPresent = orchestration.CompPresent,
            KeysPresent = orchestration.KeysPresent,
            PadsPresent = orchestration.PadsPresent,
            DrumsPresent = orchestration.DrumsPresent,
            CymbalLanguage = orchestration.CymbalLanguage,
            CrashOnSectionStart = orchestration.CrashOnSectionStart,
            PreferRideOverHat = orchestration.PreferRideOverHat
        };
    }

    private static RegisterConstraints BuildRegisterConstraints()
    {
        // Story 7.9: standardize existing ad-hoc constraints as queryable contract
        // These constants are currently hard-coded in KeysTrackGenerator and GuitarTrackGenerator
        return new RegisterConstraints
        {
            LeadSpaceCeiling = 72,  // C5 (MIDI 72) - from KeysTrackGenerator.cs line 319
            BassFloor = 52,         // E3 (MIDI 52) - from comp/guitar register guardrails
            VocalBand = (60, 76)    // C4-E5 - typical vocal range, reserved for future melody
        };
    }

    private static RoleDensityCaps BuildDensityCaps(double sectionEnergy)
    {
        // Story 7.9: derive caps from section energy level
        // Higher energy allows higher density; lower energy constrains density

        if (sectionEnergy < 0.3)
        {
            return RoleDensityCaps.Low();
        }
        else if (sectionEnergy > 0.7)
        {
            return RoleDensityCaps.High();
        }
        else
        {
            return RoleDensityCaps.Default();
        }
    }
}
