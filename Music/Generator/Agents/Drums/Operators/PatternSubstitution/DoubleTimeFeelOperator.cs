// AI: purpose=PatternSubstitution operator generating double-time feel (dense kicks, driving pattern).
// AI: invariants=Only applies at high energy (>=0.6); generates dense kick pattern; mutually exclusive with HalfTimeFeel.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.4; adjust energy threshold and pattern density based on style and listening tests.

namespace Music.Generator.Agents.Drums.Operators.PatternSubstitution
{
    /// <summary>
    /// Generates double-time feel pattern with denser kick patterns creating
    /// a driving, urgent feel without actually doubling the tempo.
    /// Story 3.4: Pattern Substitution Operators (Groove Swaps).
    /// </summary>
    /// <remarks>
    /// Double-time feel characteristics:
    /// - Kicks on every beat plus offbeats (8th note density)
    /// - Standard backbeats maintained (2 and 4)
    /// - Creates perception of double the energy
    /// Best used in choruses, solos, or climactic sections.
    /// Mutually exclusive with HalfTimeFeelOperator (enforced via energy thresholds).
    /// </remarks>
    public sealed class DoubleTimeFeelOperator : DrumOperatorBase
    {
        private const int KickDownbeatVelocityMin = 95;
        private const int KickDownbeatVelocityMax = 115;
        private const int KickOffbeatVelocityMin = 75;
        private const int KickOffbeatVelocityMax = 95;
        private const int SnareVelocityMin = 100;
        private const int SnareVelocityMax = 120;
        private const double BaseScore = 0.45; // Lower for sparing use

        /// <inheritdoc/>
        public override string OperatorId => "DrumDoubleTimeFeel";

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.PatternSubstitution;

        /// <summary>
        /// Requires high energy for double-time feel.
        /// This creates mutual exclusion with HalfTimeFeel (which caps at 0.6).
        /// </summary>
        protected override double MinEnergyThreshold => 0.6;

        /// <summary>
        /// Requires kick role for dense kick pattern.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Kick;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Need at least 3 beats for meaningful double-time
            if (context.BeatsPerBar < 3)
                return false;

            // Best suited for high-energy sections
            bool isSuitableSection = context.SectionType is
                MusicConstants.eSectionType.Chorus or
                MusicConstants.eSectionType.Solo or
                MusicConstants.eSectionType.Outro;

            if (!isSuitableSection)
                return false;

            // Suppress during fill windows
            if (context.IsFillWindow)
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

            double baseScore = ComputeScore(drummerContext);

            // Generate dense kick pattern (8th note density)
            foreach (var kickCandidate in GenerateKickPattern(drummerContext, baseScore))
            {
                yield return kickCandidate;
            }

            // Generate backbeat snare candidates if snare is active
            if (drummerContext.ActiveRoles.Contains(GrooveRoles.Snare))
            {
                foreach (var snareCandidate in GenerateSnarePattern(drummerContext, baseScore))
                {
                    yield return snareCandidate;
                }
            }
        }

        private IEnumerable<DrumCandidate> GenerateKickPattern(DrummerContext context, double baseScore)
        {
            int beatsPerBar = context.BeatsPerBar;

            // Generate 8th note kick pattern (every beat + offbeats)
            for (int beatInt = 1; beatInt <= beatsPerBar; beatInt++)
            {
                // Downbeat kick
                int downbeatVelocity = GenerateVelocityHint(
                    KickDownbeatVelocityMin, KickDownbeatVelocityMax,
                    context.BarNumber, beatInt,
                    context.Seed);

                OnsetStrength downbeatStrength = beatInt == 1 ? OnsetStrength.Downbeat : OnsetStrength.Strong;

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: context.BarNumber,
                    beat: beatInt,
                    strength: downbeatStrength,
                    score: baseScore,
                    velocityHint: downbeatVelocity);

                // Offbeat kick (on the &)
                decimal offbeatPosition = beatInt + 0.5m;
                if (offbeatPosition <= beatsPerBar + 0.5m)
                {
                    int offbeatVelocity = GenerateVelocityHint(
                        KickOffbeatVelocityMin, KickOffbeatVelocityMax,
                        context.BarNumber, offbeatPosition,
                        context.Seed);

                    yield return CreateCandidate(
                        role: GrooveRoles.Kick,
                        barNumber: context.BarNumber,
                        beat: offbeatPosition,
                        strength: OnsetStrength.Offbeat,
                        score: baseScore * 0.85, // Lower score for offbeats
                        velocityHint: offbeatVelocity);
                }
            }
        }

        private IEnumerable<DrumCandidate> GenerateSnarePattern(DrummerContext context, double baseScore)
        {
            // Standard backbeats (2 and 4) with high velocity for double-time energy
            foreach (int backbeat in context.BackbeatBeats)
            {
                if (backbeat > context.BeatsPerBar)
                    continue;

                int snareVelocity = GenerateVelocityHint(
                    SnareVelocityMin, SnareVelocityMax,
                    context.BarNumber, backbeat,
                    context.Seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: context.BarNumber,
                    beat: backbeat,
                    strength: OnsetStrength.Backbeat,
                    score: baseScore,
                    velocityHint: snareVelocity);
            }
        }

        private double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Boost at section boundaries
            if (context.IsAtSectionBoundary)
                score += 0.15;

            // Boost in chorus (most natural home for double-time)
            if (context.SectionType == MusicConstants.eSectionType.Chorus)
                score += 0.1;

            // Higher energy = more appropriate for double-time
            score *= 0.5 + 0.5 * context.EnergyLevel;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
