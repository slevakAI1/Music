// AI: purpose=PatternSubstitution operator generating kick pattern variants (four-on-floor, syncopated, half-time).
// AI: invariants=Only applies when Kick in ActiveRoles; generates full-bar kick pattern; energy gates pattern complexity.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.4; adjust pattern selection and energy thresholds based on style and listening tests.


using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.PatternSubstitution
{
    /// <summary>
    /// Generates kick pattern variants to establish section character:
    /// - Four-on-floor for driving choruses
    /// - Syncopated for verse interest
    /// - Half-time for bridges and breakdowns
    /// Story 3.4: Pattern Substitution Operators (Groove Swaps).
    /// </summary>
    /// <remarks>
    /// Pattern selection is section-aware:
    /// - Chorus: four-on-floor for driving feel
    /// - Verse: syncopated for groove interest
    /// - Bridge: half-time (sparse) for contrast
    /// Patterns adapt to time signature (3/4, 5/4, etc.).
    /// </remarks>
    public sealed class KickPatternVariantOperator : DrumOperatorBase
    {
        private const int FourOnFloorVelocityMin = 90;
        private const int FourOnFloorVelocityMax = 110;
        private const int SyncopatedVelocityMin = 80;
        private const int SyncopatedVelocityMax = 100;
        private const int HalfTimeVelocityMin = 95;
        private const int HalfTimeVelocityMax = 115;
        private const double BaseScore = 0.5; // Lower than MicroAddition for sparing use

        /// <summary>
        /// Kick pattern variants: determines groove character.
        /// </summary>
        private enum KickPattern
        {
            /// <summary>No pattern change (use anchor pattern).</summary>
            None,
            /// <summary>Kick on every beat for driving feel.</summary>
            FourOnFloor,
            /// <summary>Syncopated pattern with offbeat accents.</summary>
            Syncopated,
            /// <summary>Sparse pattern for half-time or breakdown feel.</summary>
            HalfTime
        }

        /// <inheritdoc/>
        public override string OperatorId => "DrumKickPatternVariant";

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.PatternSubstitution;

        /// <summary>
        /// Requires moderate energy for pattern changes.
        /// </summary>
        protected override double MinEnergyThreshold => 0.3;

        /// <summary>
        /// Requires kick role.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Kick;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Need at least 2 beats for meaningful pattern
            if (context.BeatsPerBar < 2)
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override IEnumerable<DrumCandidate> GenerateCandidates(Common.AgentContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                yield break;

            if (!CanApply(drummerContext))
                yield break;

            // Select pattern based on section type and energy
            KickPattern pattern = SelectPattern(drummerContext);

            // Skip if no pattern change
            if (pattern == KickPattern.None)
                yield break;

            // Generate pattern candidates
            var positions = GetPatternPositions(pattern, drummerContext.BeatsPerBar, drummerContext);

            foreach (var (beat, strength) in positions)
            {
                // Get velocity range for pattern type
                (int velMin, int velMax) = GetVelocityRange(pattern);

                int velocityHint = GenerateVelocityHint(
                    velMin, velMax,
                    drummerContext.BarNumber, beat,
                    drummerContext.Seed);

                double score = ComputeScore(drummerContext, strength);

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: drummerContext.BarNumber,
                    beat: beat,
                    strength: strength,
                    score: score,
                    velocityHint: velocityHint);
            }
        }

        private static KickPattern SelectPattern(DrummerContext context)
        {
            return context.SectionType switch
            {
                // Chorus: four-on-floor for driving feel (high energy)
                MusicConstants.eSectionType.Chorus => context.EnergyLevel >= 0.6
                    ? KickPattern.FourOnFloor
                    : KickPattern.None,

                // Verse: syncopated for interest (moderate energy)
                MusicConstants.eSectionType.Verse => context.EnergyLevel >= 0.4 && context.EnergyLevel < 0.7
                    ? KickPattern.Syncopated
                    : KickPattern.None,

                // Bridge: half-time for contrast
                MusicConstants.eSectionType.Bridge => KickPattern.HalfTime,

                // Solo: syncopated to support soloist
                MusicConstants.eSectionType.Solo => context.EnergyLevel >= 0.5
                    ? KickPattern.Syncopated
                    : KickPattern.None,

                // Intro: half-time for building
                MusicConstants.eSectionType.Intro => context.EnergyLevel < 0.5
                    ? KickPattern.HalfTime
                    : KickPattern.None,

                // Outro: four-on-floor if high energy, otherwise none
                MusicConstants.eSectionType.Outro => context.EnergyLevel >= 0.7
                    ? KickPattern.FourOnFloor
                    : KickPattern.None,

                // Default: no pattern change
                _ => KickPattern.None
            };
        }

        private static List<(decimal beat, OnsetStrength strength)> GetPatternPositions(
            KickPattern pattern,
            int beatsPerBar,
            DrummerContext context)
        {
            var positions = new List<(decimal, OnsetStrength)>();

            switch (pattern)
            {
                case KickPattern.FourOnFloor:
                    // Kick on every beat
                    for (int b = 1; b <= beatsPerBar; b++)
                    {
                        OnsetStrength strength = b == 1 ? OnsetStrength.Downbeat : OnsetStrength.Strong;
                        positions.Add((b, strength));
                    }
                    break;

                case KickPattern.Syncopated:
                    // Syncopated: beat 1, offbeat before 3, beat 4 (in 4/4)
                    positions.Add((1, OnsetStrength.Downbeat));
                    if (beatsPerBar >= 3)
                    {
                        // Add offbeat syncopation (2.5 = & of 2)
                        positions.Add((2.5m, OnsetStrength.Offbeat));
                    }
                    if (beatsPerBar >= 4)
                    {
                        // Beat 4 for pickup feel
                        positions.Add((4, OnsetStrength.Pickup));
                    }
                    // Additional offbeat at end for longer bars
                    if (beatsPerBar >= 5)
                    {
                        positions.Add((4.5m, OnsetStrength.Offbeat));
                    }
                    break;

                case KickPattern.HalfTime:
                    // Half-time: sparse kicks on 1 and 3 (or just 1 for very low energy)
                    positions.Add((1, OnsetStrength.Downbeat));
                    if (beatsPerBar >= 3 && context.EnergyLevel >= 0.4)
                    {
                        positions.Add((3, OnsetStrength.Strong));
                    }
                    break;

                case KickPattern.None:
                default:
                    break;
            }

            return positions;
        }

        private static (int min, int max) GetVelocityRange(KickPattern pattern)
        {
            return pattern switch
            {
                KickPattern.FourOnFloor => (FourOnFloorVelocityMin, FourOnFloorVelocityMax),
                KickPattern.Syncopated => (SyncopatedVelocityMin, SyncopatedVelocityMax),
                KickPattern.HalfTime => (HalfTimeVelocityMin, HalfTimeVelocityMax),
                _ => (85, 105)
            };
        }

        private double ComputeScore(DrummerContext context, OnsetStrength strength)
        {
            double score = BaseScore;

            // Boost at section boundaries (pattern change marks section)
            if (context.IsAtSectionBoundary)
                score += 0.15;

            // Downbeats score higher
            if (strength == OnsetStrength.Downbeat)
                score += 0.1;

            // Energy scaling
            score *= 0.7 + 0.3 * context.EnergyLevel;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
