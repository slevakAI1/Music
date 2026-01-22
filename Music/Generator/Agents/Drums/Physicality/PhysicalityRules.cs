// AI: purpose=Configuration for physicality validation (limbs, sticking, density caps).
// AI: invariants=Immutable record; all caps are optional (null = no limit); StrictnessLevel affects validation behavior.
// AI: deps=LimbModel and StickingRules (Story 4.1, 4.2); consumed by PhysicalityFilter.
// AI: change=Story 2.4 stub; Story 4.1-4.4 add full limb/sticking configuration.

namespace Music.Generator.Agents.Drums.Physicality
{
    /// <summary>
    /// Strictness level for physicality validation.
    /// </summary>
    public enum PhysicalityStrictness
    {
        /// <summary>Strict: all rules enforced, no exceptions.</summary>
        Strict = 0,

        /// <summary>Normal: standard rules, some edge cases allowed.</summary>
        Normal = 1,

        /// <summary>Loose: relaxed rules for more creative freedom.</summary>
        Loose = 2
    }

    /// <summary>
    /// Configuration for physicality validation rules.
    /// Controls limb feasibility, sticking limits, and density caps.
    /// Story 2.4: Stub configuration; Story 4.1-4.4 add full limb/sticking rules.
    /// </summary>
    public sealed record PhysicalityRules
    {
        /// <summary>
        /// Maximum hits allowed per beat (across all roles).
        /// Null = no limit.
        /// Default: 3 (reasonable for human drummer).
        /// </summary>
        public int? MaxHitsPerBeat { get; init; } = 3;

        /// <summary>
        /// Maximum hits allowed per bar (across all roles).
        /// Null = no limit.
        /// Default: 24 (reasonable for 16th note grid in 4/4).
        /// </summary>
        public int? MaxHitsPerBar { get; init; } = 24;

        /// <summary>
        /// Maximum hits per role per bar.
        /// Null value for a role = no limit for that role.
        /// </summary>
        public IReadOnlyDictionary<string, int>? MaxHitsPerRolePerBar { get; init; }

        /// <summary>
        /// Whether double bass pedal is allowed (enables two-foot kick patterns).
        /// Default: false (single pedal).
        /// </summary>
        public bool AllowDoublePedal { get; init; } = false;

        /// <summary>
        /// Strictness level for validation.
        /// Affects how edge cases are handled.
        /// </summary>
        public PhysicalityStrictness StrictnessLevel { get; init; } = PhysicalityStrictness.Normal;

        // TODO: Story 4.1 - Add LimbModel configuration
        // TODO: Story 4.2 - Add StickingRules configuration

        /// <summary>
        /// Default physicality rules with standard limits.
        /// </summary>
        public static PhysicalityRules Default => new();

        /// <summary>
        /// Lenient rules for testing or creative scenarios.
        /// </summary>
        public static PhysicalityRules Lenient => new()
        {
            MaxHitsPerBeat = null,
            MaxHitsPerBar = null,
            MaxHitsPerRolePerBar = null,
            StrictnessLevel = PhysicalityStrictness.Loose
        };

        /// <summary>
        /// Strict rules for realistic human drumming.
        /// </summary>
        public static PhysicalityRules Strict => new()
        {
            MaxHitsPerBeat = 2,
            MaxHitsPerBar = 16,
            StrictnessLevel = PhysicalityStrictness.Strict
        };

        /// <summary>
        /// Gets the cap for a specific role, or null if no cap.
        /// </summary>
        /// <param name="role">Role to look up.</param>
        /// <returns>Cap for the role, or null if uncapped.</returns>
        public int? GetRoleCap(string role)
        {
            if (MaxHitsPerRolePerBar == null)
                return null;

            return MaxHitsPerRolePerBar.TryGetValue(role, out int cap) ? cap : null;
        }
    }
}
