// AI: purpose=Per-style timing hint configuration for DrummerTimingShaper; immutable settings record.
// AI: invariants=Tick offsets bounded to reasonable range; defaults conservative; role defaults deterministic.
// AI: deps=TimingIntent enum; GrooveRoles for role constants; consumed by DrummerTimingShaper; stored in StyleConfiguration.
// AI: change=Story 6.2; add additional settings as timing hinting requirements evolve.

using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Performance
{
    /// <summary>
    /// Per-style timing hint configuration for the DrummerTimingShaper.
    /// Defines tick offsets for each timing intent and role-based defaults.
    /// Story 6.2: Implement Drummer Timing Nuance.
    /// </summary>
    public sealed record DrummerTimingHintSettings
    {
        /// <summary>
        /// Tick offset for slightly ahead timing (negative = early).
        /// Default: -5 ticks.
        /// </summary>
        public int SlightlyAheadTicks { get; init; } = -5;

        /// <summary>
        /// Tick offset for slightly behind timing (positive = late).
        /// Default: +5 ticks.
        /// </summary>
        public int SlightlyBehindTicks { get; init; } = 5;

        /// <summary>
        /// Tick offset for rushed timing (more negative).
        /// Default: -10 ticks.
        /// </summary>
        public int RushedTicks { get; init; } = -10;

        /// <summary>
        /// Tick offset for laid-back timing (more positive).
        /// Default: +10 ticks.
        /// </summary>
        public int LaidBackTicks { get; init; } = 10;

        /// <summary>
        /// Maximum timing jitter for humanization (±ticks).
        /// Applied deterministically based on candidate hash.
        /// Default: 3 ticks.
        /// </summary>
        public int MaxTimingJitter { get; init; } = 3;

        /// <summary>
        /// Maximum adjustment delta when existing hint is present.
        /// Preserves operator intent while nudging toward style target.
        /// Default: 5 ticks.
        /// </summary>
        public int MaxAdjustmentDelta { get; init; } = 5;

        /// <summary>
        /// Maximum absolute timing offset allowed after all adjustments.
        /// Hints are clamped to [-MaxAbsoluteOffset, +MaxAbsoluteOffset].
        /// Default: 20 ticks.
        /// </summary>
        public int MaxAbsoluteOffset { get; init; } = 20;

        /// <summary>
        /// Default timing intent per drum role.
        /// Roles not in this dictionary default to OnTop.
        /// </summary>
        public IReadOnlyDictionary<string, TimingIntent> RoleTimingIntentDefaults { get; init; }
            = DefaultRoleTimingIntents;

        /// <summary>
        /// Standard role timing intent defaults (genre-agnostic).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, TimingIntent> DefaultRoleTimingIntents =
            new Dictionary<string, TimingIntent>
            {
                { GrooveRoles.Snare, TimingIntent.SlightlyBehind },  // Universal pocket feel
                { GrooveRoles.Kick, TimingIntent.OnTop },            // Anchor
                { GrooveRoles.ClosedHat, TimingIntent.OnTop },       // Consistent timekeeping
                { GrooveRoles.OpenHat, TimingIntent.OnTop },         // Timekeeping
                { GrooveRoles.Ride, TimingIntent.OnTop },            // Timekeeping
                { GrooveRoles.Crash, TimingIntent.OnTop },           // Accent marker
                { "Tom1", TimingIntent.OnTop },                      // Neutral
                { "Tom2", TimingIntent.OnTop },                      // Neutral
                { "FloorTom", TimingIntent.OnTop }                   // Neutral
            };

        #region Style Presets

        /// <summary>
        /// Conservative fallback defaults when no style-specific settings exist.
        /// Smaller offsets and less jitter for safe, neutral timing.
        /// </summary>
        public static DrummerTimingHintSettings ConservativeDefaults => new()
        {
            SlightlyAheadTicks = -3,
            SlightlyBehindTicks = 3,
            RushedTicks = -6,
            LaidBackTicks = 6,
            MaxTimingJitter = 2,
            MaxAdjustmentDelta = 3,
            MaxAbsoluteOffset = 15
        };

        /// <summary>
        /// Pop/Rock timing defaults.
        /// Standard pocket feel with moderate offset values.
        /// </summary>
        public static DrummerTimingHintSettings PopRockDefaults => new()
        {
            SlightlyAheadTicks = -5,
            SlightlyBehindTicks = 5,
            RushedTicks = -10,
            LaidBackTicks = 10,
            MaxTimingJitter = 3,
            MaxAdjustmentDelta = 5,
            MaxAbsoluteOffset = 20
        };

        /// <summary>
        /// Jazz timing defaults.
        /// More laid-back overall with wider pocket.
        /// </summary>
        public static DrummerTimingHintSettings JazzDefaults => new()
        {
            SlightlyAheadTicks = -3,
            SlightlyBehindTicks = 8,
            RushedTicks = -6,
            LaidBackTicks = 12,
            MaxTimingJitter = 4,
            MaxAdjustmentDelta = 4,
            MaxAbsoluteOffset = 20,
            RoleTimingIntentDefaults = new Dictionary<string, TimingIntent>
            {
                { GrooveRoles.Snare, TimingIntent.SlightlyBehind },  // Deeper pocket in jazz
                { GrooveRoles.Kick, TimingIntent.SlightlyBehind },   // Jazz kick can lay back slightly
                { GrooveRoles.ClosedHat, TimingIntent.OnTop },       // Timekeeping stays on
                { GrooveRoles.OpenHat, TimingIntent.OnTop },
                { GrooveRoles.Ride, TimingIntent.OnTop },            // Ride is the timekeeper
                { GrooveRoles.Crash, TimingIntent.OnTop },
                { "Tom1", TimingIntent.OnTop },
                { "Tom2", TimingIntent.OnTop },
                { "FloorTom", TimingIntent.OnTop }
            }
        };

        /// <summary>
        /// Metal timing defaults.
        /// Tighter, more on-top feel with less laid-back.
        /// </summary>
        public static DrummerTimingHintSettings MetalDefaults => new()
        {
            SlightlyAheadTicks = -5,
            SlightlyBehindTicks = 2,
            RushedTicks = -12,
            LaidBackTicks = 6,
            MaxTimingJitter = 2,
            MaxAdjustmentDelta = 6,
            MaxAbsoluteOffset = 25,
            RoleTimingIntentDefaults = new Dictionary<string, TimingIntent>
            {
                { GrooveRoles.Snare, TimingIntent.OnTop },           // Metal snare is tight
                { GrooveRoles.Kick, TimingIntent.OnTop },            // Precise kicks
                { GrooveRoles.ClosedHat, TimingIntent.OnTop },
                { GrooveRoles.OpenHat, TimingIntent.OnTop },
                { GrooveRoles.Ride, TimingIntent.OnTop },
                { GrooveRoles.Crash, TimingIntent.OnTop },
                { "Tom1", TimingIntent.OnTop },
                { "Tom2", TimingIntent.OnTop },
                { "FloorTom", TimingIntent.OnTop }
            }
        };

        #endregion

        #region Methods

        /// <summary>
        /// Gets the tick offset for a given timing intent.
        /// </summary>
        public int GetTickOffset(TimingIntent intent)
        {
            return intent switch
            {
                TimingIntent.OnTop => 0,
                TimingIntent.SlightlyAhead => SlightlyAheadTicks,
                TimingIntent.SlightlyBehind => SlightlyBehindTicks,
                TimingIntent.Rushed => RushedTicks,
                TimingIntent.LaidBack => LaidBackTicks,
                _ => 0
            };
        }

        /// <summary>
        /// Gets the default timing intent for a role.
        /// Returns OnTop for unknown roles.
        /// </summary>
        public TimingIntent GetRoleDefaultIntent(string role)
        {
            if (string.IsNullOrEmpty(role))
                return TimingIntent.OnTop;

            return RoleTimingIntentDefaults.TryGetValue(role, out var intent)
                ? intent
                : TimingIntent.OnTop;
        }

        /// <summary>
        /// Computes deterministic jitter based on candidate properties.
        /// Returns value in range [-MaxTimingJitter, +MaxTimingJitter].
        /// </summary>
        public int ComputeJitter(int barNumber, decimal beat, string role, string candidateId)
        {
            if (MaxTimingJitter <= 0)
                return 0;

            // Deterministic hash-based jitter
            int hash = HashCode.Combine(barNumber, beat, role, candidateId);
            int range = 2 * MaxTimingJitter + 1;
            int jitter = (Math.Abs(hash) % range) - MaxTimingJitter;

            return jitter;
        }

        /// <summary>
        /// Validates that all timing values are within reasonable bounds.
        /// </summary>
        public bool IsValid(out string? errorMessage)
        {
            if (MaxTimingJitter < 0)
            {
                errorMessage = $"MaxTimingJitter must be >= 0, was {MaxTimingJitter}";
                return false;
            }

            if (MaxAdjustmentDelta < 0)
            {
                errorMessage = $"MaxAdjustmentDelta must be >= 0, was {MaxAdjustmentDelta}";
                return false;
            }

            if (MaxAbsoluteOffset < 0)
            {
                errorMessage = $"MaxAbsoluteOffset must be >= 0, was {MaxAbsoluteOffset}";
                return false;
            }

            if (Math.Abs(SlightlyAheadTicks) > MaxAbsoluteOffset)
            {
                errorMessage = $"SlightlyAheadTicks ({SlightlyAheadTicks}) exceeds MaxAbsoluteOffset ({MaxAbsoluteOffset})";
                return false;
            }

            if (Math.Abs(SlightlyBehindTicks) > MaxAbsoluteOffset)
            {
                errorMessage = $"SlightlyBehindTicks ({SlightlyBehindTicks}) exceeds MaxAbsoluteOffset ({MaxAbsoluteOffset})";
                return false;
            }

            if (Math.Abs(RushedTicks) > MaxAbsoluteOffset)
            {
                errorMessage = $"RushedTicks ({RushedTicks}) exceeds MaxAbsoluteOffset ({MaxAbsoluteOffset})";
                return false;
            }

            if (Math.Abs(LaidBackTicks) > MaxAbsoluteOffset)
            {
                errorMessage = $"LaidBackTicks ({LaidBackTicks}) exceeds MaxAbsoluteOffset ({MaxAbsoluteOffset})";
                return false;
            }

            errorMessage = null;
            return true;
        }

        #endregion
    }
}
