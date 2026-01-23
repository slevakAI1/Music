// AI: purpose=Captures groove plan for a single bar across all pipeline phases from Story A1.
// AI: invariants=BaseOnsets from anchors; SelectedVariationOnsets from variation selection; FinalOnsets post-constraints.
// AI: deps=GrooveOnset for onset representation; GrooveBarDiagnostics for optional decision tracing (Story G1).
// AI: change=Story G1 changed Diagnostics type from string? to GrooveBarDiagnostics? for structured tracing.

namespace Music.Generator.Groove
{
    /// <summary>
    /// Represents the groove plan for a single bar.
    /// Story A1: Stable groove output container capturing all pipeline phases.
    /// Tracks onset lists from anchors through variation selection to final constrained output.
    /// </summary>
    public sealed record GrooveBarPlan
    {
        /// <summary>
        /// Base onsets from anchor layer (Phase 2).
        /// These are the foundational pattern onsets before any variation.
        /// </summary>
        public required IReadOnlyList<GrooveOnset> BaseOnsets { get; init; }

        /// <summary>
        /// Onsets added by variation selection (Phase 3).
        /// These are additional onsets beyond the anchor pattern.
        /// </summary>
        public required IReadOnlyList<GrooveOnset> SelectedVariationOnsets { get; init; }

        /// <summary>
        /// Final onsets after constraint enforcement (Phase 4).
        /// This is the complete onset list that will be rendered to MIDI events.
        /// Includes BaseOnsets + SelectedVariationOnsets with constraints applied.
        /// </summary>
        public required IReadOnlyList<GrooveOnset> FinalOnsets { get; init; }

        /// <summary>
        /// Optional structured diagnostics for decision tracing.
        /// Null when diagnostics disabled (Story G1).
        /// When enabled, captures per-bar/per-role groove decisions: tags, candidates, filters, selections, prunes.
        /// </summary>
        public GrooveBarDiagnostics? Diagnostics { get; init; }

        /// <summary>
        /// Bar number this plan applies to (1-based).
        /// </summary>
        public required int BarNumber { get; init; }
    }
}
