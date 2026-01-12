// AI: purpose=Describes why tension exists in a section for explainability and policy tuning.
// AI: invariants=Flags enum allows multiple drivers to combine.
// AI: deps=Referenced by SectionTensionProfile; consumed by tension diagnostics (Story 7.5.7).

namespace Music.Generator;

/// <summary>
/// Describes why tension exists in a section, supporting explainability and policy tuning.
/// Multiple drivers can apply simultaneously (flags enum).
/// </summary>
[Flags]
public enum TensionDriver
{
    /// <summary>No specific tension driver identified.</summary>
    None = 0,

    /// <summary>Pre-chorus section building tension toward chorus.</summary>
    PreChorusBuild = 1 << 0,

    /// <summary>Breakdown section with reduced instrumentation creating anticipation.</summary>
    Breakdown = 1 << 1,

    /// <summary>Drop section (common in EDM) creating sudden energy/tension release.</summary>
    Drop = 1 << 2,

    /// <summary>Cadence approaching section end, needing resolution.</summary>
    Cadence = 1 << 3,

    /// <summary>Bridge section providing contrast and building new tension.</summary>
    BridgeContrast = 1 << 4,

    /// <summary>Anticipation before a higher-energy section.</summary>
    Anticipation = 1 << 5,

    /// <summary>Resolution moment (end of tension arc).</summary>
    Resolution = 1 << 6,

    /// <summary>Section opening creating initial tension.</summary>
    Opening = 1 << 7,

    /// <summary>Peak moment within a section.</summary>
    Peak = 1 << 8
}
