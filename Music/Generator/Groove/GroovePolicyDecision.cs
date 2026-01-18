// AI: purpose=Define policy decision with optional overrides for future drummer model (Story A3).
// AI: invariants=All overrides are nullable; null means "use default system behavior"; no overrides changes output.
// AI: deps=TimingFeel from Groove.cs for timing feel overrides.
// AI: change=Story A3 acceptance criteria: drummer policy hook with no behavior change when defaults used.

namespace Music.Generator
{
    /// <summary>
    /// Policy decision that can override groove behavior for a specific bar and role.
    /// Story A3: Drummer Policy Hook - allows future human drummer model to drive groove decisions.
    /// All fields are nullable; null means "use default system behavior".
    /// </summary>
    public sealed record GroovePolicyDecision
    {
        /// <summary>
        /// Override the enabled variation tags for this bar/role.
        /// When null, uses default tag resolution from segment profile + phrase hooks.
        /// When set, can be used to enable/disable specific variation groups dynamically.
        /// </summary>
        public List<string>? EnabledVariationTagsOverride { get; init; }

        /// <summary>
        /// Override the density target (0.0 = minimal, 1.0 = maximum busyness).
        /// When null, uses base density from RoleDensityTarget and section multipliers.
        /// Reserved for future drummer model to adjust busyness per phrase/bar.
        /// </summary>
        public double? Density01Override { get; init; }

        /// <summary>
        /// Override the maximum number of events per bar for this role.
        /// When null, uses MaxEventsPerBar from density target.
        /// Can be used by drummer model to enforce additional caps.
        /// </summary>
        public int? MaxEventsPerBarOverride { get; init; }

        /// <summary>
        /// Override the timing feel for this role (Ahead, OnTop, Behind, LaidBack).
        /// When null, uses RoleTimingFeel from GrooveTimingPolicy.
        /// Allows drummer model to adjust pocket dynamically per bar.
        /// </summary>
        public TimingFeel? RoleTimingFeelOverride { get; init; }

        /// <summary>
        /// Override the timing bias in ticks for this role (can be positive or negative).
        /// When null, uses RoleTimingBiasTicks from GrooveTimingPolicy.
        /// Allows drummer model to adjust micro-timing per bar.
        /// </summary>
        public int? RoleTimingBiasTicksOverride { get; init; }

        /// <summary>
        /// Override velocity bias (simple additive or multiplier, interpretation TBD in Story D2).
        /// When null, uses base velocity shaping from GrooveAccentPolicy.
        /// Reserved for drummer model to adjust dynamics per phrase/bar.
        /// Positive values increase velocity, negative values decrease.
        /// </summary>
        public int? VelocityBiasOverride { get; init; }

        /// <summary>
        /// Reserved for future operator-based drummer logic.
        /// Will contain list of allowed operator IDs when operator system is implemented.
        /// When null or empty, all operators allowed (or none if no operator system active).
        /// Currently unused - reserved for future Pop Rock Human Drummer epic.
        /// </summary>
        public List<string>? OperatorAllowList { get; init; }

        /// <summary>
        /// Creates a policy decision with no overrides (default behavior).
        /// </summary>
        public static GroovePolicyDecision NoOverrides => new();

        /// <summary>
        /// Checks if this decision contains any actual overrides.
        /// </summary>
        public bool HasAnyOverrides =>
            EnabledVariationTagsOverride is not null ||
            Density01Override is not null ||
            MaxEventsPerBarOverride is not null ||
            RoleTimingFeelOverride is not null ||
            RoleTimingBiasTicksOverride is not null ||
            VelocityBiasOverride is not null ||
            OperatorAllowList is not null;
    }
}
