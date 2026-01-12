// AI: purpose=Public stable API for querying tension information without requiring planner internals.
// AI: invariants=All queries deterministic for same (sectionIndex, barIndex); thread-safe immutable reads.
// AI: deps=Implemented by tension planner (Story 7.5.2/7.5.3); consumed by role renderers and Stage 8/9.
// AI: change=Story 7.5.8 adds GetTensionContext unified query surface for Stage 8/9 motif/melody integration.

namespace Music.Generator;

/// <summary>
/// Public API for querying tension information.
/// Provides stable contract for role renderers to access tension without
/// needing knowledge of internal tension planning/computation.
/// All queries are deterministic and thread-safe.
/// </summary>
public interface ITensionQuery
{
    /// <summary>
    /// Gets section-level (macro) tension for a section.
    /// </summary>
    /// <param name="absoluteSectionIndex">Absolute 0-based section index in the song.</param>
    /// <returns>Section tension profile with macro tension, default micro tension, and driver.</returns>
    SectionTensionProfile GetMacroTension(int absoluteSectionIndex);

    /// <summary>
    /// Gets bar-level (micro) tension for a specific bar within a section.
    /// </summary>
    /// <param name="absoluteSectionIndex">Absolute 0-based section index in the song.</param>
    /// <param name="barIndexWithinSection">0-based bar index within the section.</param>
    /// <returns>Micro tension value [0..1] for the bar.</returns>
    double GetMicroTension(int absoluteSectionIndex, int barIndexWithinSection);

    /// <summary>
    /// Gets the complete micro tension map for a section.
    /// </summary>
    /// <param name="absoluteSectionIndex">Absolute 0-based section index in the song.</param>
    /// <returns>Complete micro tension map with per-bar tensions and flags.</returns>
    MicroTensionMap GetMicroTensionMap(int absoluteSectionIndex);

    /// <summary>
    /// Gets phrase position flags for a specific bar.
    /// </summary>
    /// <param name="absoluteSectionIndex">Absolute 0-based section index in the song.</param>
    /// <param name="barIndexWithinSection">0-based bar index within the section.</param>
    /// <returns>Tuple of (IsPhraseEnd, IsSectionEnd, IsSectionStart).</returns>
    (bool IsPhraseEnd, bool IsSectionEnd, bool IsSectionStart) GetPhraseFlags(
        int absoluteSectionIndex,
        int barIndexWithinSection);

    /// <summary>
    /// Gets the transition hint for a section boundary (transition FROM this section TO next).
    /// Returns None for the last section.
    /// </summary>
    /// <param name="absoluteSectionIndex">Absolute 0-based section index in the song.</param>
    /// <returns>Section transition hint (Build/Release/Sustain/Drop/None).</returns>
    SectionTransitionHint GetTransitionHint(int absoluteSectionIndex);

    /// <summary>
    /// Gets unified tension context for a specific bar, combining all tension-related information.
    /// This is the preferred query method for Stage 8/9 motif placement and melody generation.
    /// </summary>
    /// <param name="absoluteSectionIndex">Absolute 0-based section index in the song.</param>
    /// <param name="barIndexWithinSection">0-based bar index within the section.</param>
    /// <returns>Immutable context containing macro/micro tension, drivers, flags, and transition hint.</returns>
    TensionContext GetTensionContext(int absoluteSectionIndex, int barIndexWithinSection);

    /// <summary>
    /// Checks if tension data is available for a section.
    /// </summary>
    /// <param name="absoluteSectionIndex">Absolute 0-based section index in the song.</param>
    /// <returns>True if tension data exists for this section.</returns>
    bool HasTensionData(int absoluteSectionIndex);

    /// <summary>
    /// Gets the total number of sections with tension data.
    /// </summary>
    int SectionCount { get; }
}

/// <summary>
/// Context object combining tension query with section/bar position information.
/// Provided to role renderers for convenient access to all tension-related data.
/// Story 7.5.8: Expanded to include SectionTransitionHint for Stage 8/9 integration.
/// </summary>
public sealed record TensionContext
{
    /// <summary>
    /// Absolute section index in the song.
    /// </summary>
    public required int AbsoluteSectionIndex { get; init; }

    /// <summary>
    /// Bar index within the section (0-based).
    /// </summary>
    public required int BarIndexWithinSection { get; init; }

    /// <summary>
    /// Section-level (macro) tension profile.
    /// </summary>
    public required SectionTensionProfile MacroTension { get; init; }

    /// <summary>
    /// Bar-level (micro) tension value [0..1].
    /// </summary>
    public required double MicroTension { get; init; }

    /// <summary>
    /// True if this bar is a phrase end.
    /// </summary>
    public required bool IsPhraseEnd { get; init; }

    /// <summary>
    /// True if this bar is the section end.
    /// </summary>
    public required bool IsSectionEnd { get; init; }

    /// <summary>
    /// True if this bar is the section start.
    /// </summary>
    public required bool IsSectionStart { get; init; }

    /// <summary>
    /// Section transition hint (Build/Release/Sustain/Drop) for the boundary FROM this section TO next.
    /// Returns None if this is the last section or transition is not determined.
    /// Used by Stage 8/9 for motif placement and melodic phrase shaping decisions.
    /// </summary>
    public required SectionTransitionHint TransitionHint { get; init; }

    /// <summary>
    /// Tension drivers explaining why this section has its tension value.
    /// Derived from MacroTension.Driver for convenience.
    /// </summary>
    public TensionDriver TensionDrivers => MacroTension.Driver;

    /// <summary>
    /// Creates a tension context from query and position.
    /// </summary>
    public static TensionContext Create(
        ITensionQuery query,
        int absoluteSectionIndex,
        int barIndexWithinSection)
    {
        var macroTension = query.GetMacroTension(absoluteSectionIndex);
        var microTension = query.GetMicroTension(absoluteSectionIndex, barIndexWithinSection);
        var (isPhraseEnd, isSectionEnd, isSectionStart) = 
            query.GetPhraseFlags(absoluteSectionIndex, barIndexWithinSection);
        var transitionHint = query.GetTransitionHint(absoluteSectionIndex);

        return new TensionContext
        {
            AbsoluteSectionIndex = absoluteSectionIndex,
            BarIndexWithinSection = barIndexWithinSection,
            MacroTension = macroTension,
            MicroTension = microTension,
            IsPhraseEnd = isPhraseEnd,
            IsSectionEnd = isSectionEnd,
            IsSectionStart = isSectionStart,
            TransitionHint = transitionHint
        };
    }
}
