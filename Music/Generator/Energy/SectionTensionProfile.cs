// AI: purpose=Immutable tension profile for a section, separate from but coordinated with energy.
// AI: invariants=MacroTension/MicroTensionDefault [0..1]; immutable record; Driver can be None or multiple flags.
// AI: deps=Consumed by tension query API and tension hooks (Story 7.5.4); computed by tension planner (Story 7.5.2/7.5.3).

namespace Music.Generator;

/// <summary>
/// Immutable tension profile for a section.
/// Tension is distinct from energy: energy = "vigor/intensity", tension = "need for release/resolution".
/// High energy + low tension = triumphant release (chorus).
/// Low energy + high tension = anticipatory breakdown (pre-chorus build).
/// </summary>
public sealed record SectionTensionProfile
{
    /// <summary>
    /// Section-level tension target [0..1].
    /// 0.0 = resolved/stable; 1.0 = maximum tension/anticipation.
    /// Influences overall section character and phrase-level tension shaping.
    /// </summary>
    public required double MacroTension { get; init; }

    /// <summary>
    /// Default micro-tension bias [0..1] for bars within the section.
    /// Individual bars may override this via phrase position (Story 7.5.3).
    /// Provides baseline tension for within-section variation.
    /// </summary>
    public required double MicroTensionDefault { get; init; }

    /// <summary>
    /// Describes why this tension exists (for explainability and diagnostics).
    /// Multiple drivers can apply simultaneously.
    /// </summary>
    public required TensionDriver Driver { get; init; }

    /// <summary>
    /// Absolute section index in the song (for deterministic queries).
    /// </summary>
    public required int AbsoluteSectionIndex { get; init; }

    /// <summary>
    /// Creates a neutral (no tension) profile.
    /// </summary>
    public static SectionTensionProfile Neutral(int absoluteSectionIndex)
    {
        return new SectionTensionProfile
        {
            MacroTension = 0.0,
            MicroTensionDefault = 0.0,
            Driver = TensionDriver.None,
            AbsoluteSectionIndex = absoluteSectionIndex
        };
    }

    /// <summary>
    /// Creates a profile with specified macro tension.
    /// </summary>
    public static SectionTensionProfile WithMacroTension(
        double macroTension,
        int absoluteSectionIndex,
        TensionDriver driver = TensionDriver.None)
    {
        return new SectionTensionProfile
        {
            MacroTension = Math.Clamp(macroTension, 0.0, 1.0),
            MicroTensionDefault = macroTension * 0.5, // Default micro tension is half of macro
            Driver = driver,
            AbsoluteSectionIndex = absoluteSectionIndex
        };
    }

    /// <summary>
    /// Creates a profile with both macro and micro tension specified.
    /// </summary>
    public static SectionTensionProfile WithTensions(
        double macroTension,
        double microTensionDefault,
        int absoluteSectionIndex,
        TensionDriver driver = TensionDriver.None)
    {
        return new SectionTensionProfile
        {
            MacroTension = Math.Clamp(macroTension, 0.0, 1.0),
            MicroTensionDefault = Math.Clamp(microTensionDefault, 0.0, 1.0),
            Driver = driver,
            AbsoluteSectionIndex = absoluteSectionIndex
        };
    }
}
