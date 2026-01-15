// AI: purpose=Deterministic implementation of ISongIntentQuery aggregating tension and variation queries.
// AI: invariants=All outputs deterministic from seed+inputs; thread-safe immutable; no new planning, only query aggregation.
// AI: deps=Aggregates ITensionQuery, IVariationQuery; consumed by Generator and motif systems.
// AI: change=Energy removed - uses fixed energy value (0.5) and simplified logic.

namespace Music.Generator;

/// <summary>
/// Deterministic implementation of unified intent query.
/// Aggregates tension and variation queries into single stable contract.
/// Precomputes section contexts at construction for O(1) query performance.
/// </summary>
public sealed class DeterministicSongIntentQuery : ISongIntentQuery
{
    private readonly SectionTrack _sectionTrack;
    private readonly ITensionQuery _tensionQuery;
    private readonly IVariationQuery _variationQuery;
    private readonly Dictionary<int, SectionIntentContext> _sectionContextCache;
    private readonly int _sectionCount;

    /// <summary>
    /// Creates deterministic song intent query from tension and variation queries.
    /// Precomputes all section contexts at construction.
    /// </summary>
    /// <param name="sectionTrack">The song's section track for accessing section types.</param>
    /// <param name="tensionQuery">Tension query providing macro/micro tension and transition hints.</param>
    /// <param name="variationQuery">Variation query providing A/A'/B variation plans.</param>
    public DeterministicSongIntentQuery(
        SectionTrack sectionTrack,
        ITensionQuery tensionQuery,
        IVariationQuery variationQuery)
    {
        _sectionTrack = sectionTrack ?? throw new ArgumentNullException(nameof(sectionTrack));
        _tensionQuery = tensionQuery ?? throw new ArgumentNullException(nameof(tensionQuery));
        _variationQuery = variationQuery ?? throw new ArgumentNullException(nameof(variationQuery));

        _sectionCount = Math.Min(_sectionTrack.Sections.Count, 
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

        // Use fixed values (no energy delta or phrase position from micro-arc)
        var energyDelta = 0.0;
        var phrasePosition = PhrasePosition.Middle;

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
        var section = _sectionTrack.Sections[absoluteSectionIndex];
        var macroTension = _tensionQuery.GetMacroTension(absoluteSectionIndex);
        var transitionHint = _tensionQuery.GetTransitionHint(absoluteSectionIndex);
        var variationPlan = _variationQuery.GetVariationPlan(absoluteSectionIndex);

        // Use fixed energy value (no longer depends on energy profiles)
        const double fixedEnergy = 0.5;

        return new SectionIntentContext
        {
            AbsoluteSectionIndex = absoluteSectionIndex,
            SectionType = section.SectionType,
            Energy = fixedEnergy,
            Tension = macroTension.MacroTension,
            TensionDrivers = macroTension.Driver,
            TransitionHint = transitionHint,
            VariationIntensity = variationPlan.VariationIntensity,
            BaseReferenceSectionIndex = variationPlan.BaseReferenceSectionIndex,
            VariationTags = variationPlan.Tags,
            RolePresence = BuildRolePresenceHints(),
            RegisterConstraints = BuildRegisterConstraints(),
            DensityCaps = BuildDensityCaps(fixedEnergy)
        };
    }

    private static RolePresenceHints BuildRolePresenceHints()
    {
        // All roles present (no orchestration gating)
        return new RolePresenceHints
        {
            BassPresent = true,
            CompPresent = true,
            KeysPresent = true,
            PadsPresent = true,
            DrumsPresent = true,
            CymbalLanguage = EnergyCymbalLanguage.Standard,
            CrashOnSectionStart = true,
            PreferRideOverHat = false
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
