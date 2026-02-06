using Music.Generator.Groove;

namespace Music.Generator.Drums.Performance
{
    // AI: purpose=Per-style timing hint settings record for DrummerTimingShaper.
    // AI: invariants=Tick offsets bounded; jitter and adjustments clamped by MaxAbsoluteOffset.
    // AI: deps=TimingIntent enum; GrooveRoles constants; stored in StyleConfiguration and consumed by shaper.
    public sealed record DrummerTimingHintSettings
    {
        // Tick offset for slightly ahead timing (negative = early).
        public int SlightlyAheadTicks { get; init; } = -5;

        // Tick offset for slightly behind timing (positive = late).
        public int SlightlyBehindTicks { get; init; } = 5;

        // Tick offset for rushed timing (more negative).
        public int RushedTicks { get; init; } = -10;

        // Tick offset for laid-back timing (more positive).
        public int LaidBackTicks { get; init; } = 10;

        // Max deterministic jitter (±ticks) applied per candidate.
        public int MaxTimingJitter { get; init; } = 3;

        // Max delta when nudging existing operator timing toward style target.
        public int MaxAdjustmentDelta { get; init; } = 5;

        // Absolute clamp for any timing hint after adjustments.
        public int MaxAbsoluteOffset { get; init; } = 20;

        // Default timing intent per role; roles not present default to OnTop.
        public IReadOnlyDictionary<string, TimingIntent> RoleTimingIntentDefaults { get; init; }
            = DefaultRoleTimingIntents;

        // Standard role timing intent defaults (genre-agnostic).
        public static readonly IReadOnlyDictionary<string, TimingIntent> DefaultRoleTimingIntents =
            new Dictionary<string, TimingIntent>
            {
                { GrooveRoles.Snare, TimingIntent.SlightlyBehind },
                { GrooveRoles.Kick, TimingIntent.OnTop },
                { GrooveRoles.ClosedHat, TimingIntent.OnTop },
                { GrooveRoles.OpenHat, TimingIntent.OnTop },
                { GrooveRoles.Ride, TimingIntent.OnTop },
                { GrooveRoles.Crash, TimingIntent.OnTop },
                { "Tom1", TimingIntent.OnTop },
                { "Tom2", TimingIntent.OnTop },
                { "FloorTom", TimingIntent.OnTop }
            };

        #region Style Presets

        // Conservative fallback defaults for neutral timing.
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

        // Pop/Rock defaults: standard pocket with moderate offsets.
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

        // Jazz defaults: more laid-back pocket and slightly wider bounds.
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
                { GrooveRoles.Snare, TimingIntent.SlightlyBehind },
                { GrooveRoles.Kick, TimingIntent.SlightlyBehind },
                { GrooveRoles.ClosedHat, TimingIntent.OnTop },
                { GrooveRoles.OpenHat, TimingIntent.OnTop },
                { GrooveRoles.Ride, TimingIntent.OnTop },
                { GrooveRoles.Crash, TimingIntent.OnTop },
                { "Tom1", TimingIntent.OnTop },
                { "Tom2", TimingIntent.OnTop },
                { "FloorTom", TimingIntent.OnTop }
            }
        };

        // Metal defaults: tighter, on-top feel with limited laid-back.
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
                { GrooveRoles.Snare, TimingIntent.OnTop },
                { GrooveRoles.Kick, TimingIntent.OnTop },
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

        // Get tick offset for a given TimingIntent (maps intent -> configured tick offset).
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

        // Get default TimingIntent for a role; unknown roles return OnTop.
        public TimingIntent GetRoleDefaultIntent(string role)
        {
            if (string.IsNullOrEmpty(role))
                return TimingIntent.OnTop;

            return RoleTimingIntentDefaults.TryGetValue(role, out var intent)
                ? intent
                : TimingIntent.OnTop;
        }

        // Compute deterministic jitter in range [-MaxTimingJitter, +MaxTimingJitter] using candidate properties.
        public int ComputeJitter(int barNumber, decimal beat, string role, string candidateId)
        {
            if (MaxTimingJitter <= 0)
                return 0;

            int hash = HashCode.Combine(barNumber, beat, role, candidateId);
            int range = 2 * MaxTimingJitter + 1;
            int jitter = (Math.Abs(hash) % range) - MaxTimingJitter;

            return jitter;
        }

        // Validate that timing values are within bounds and consistent with MaxAbsoluteOffset.
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
