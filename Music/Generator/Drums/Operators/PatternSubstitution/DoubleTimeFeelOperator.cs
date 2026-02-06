// AI: purpose=PatternSubstitution: produce double-time feel (denser kicks, driving energy) without tempo change.
// AI: invariants=Apply in high-energy suitable sections; uses Bar.BackbeatBeats and BeatsPerBar; deterministic from seed.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; integrates with section type for suitability decisions.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.PatternSubstitution
{
    // AI: purpose=Apply double-time feel: 8th-note dense kick pattern + strong backbeats.
    // AI: note=Intended for choruses/solos; lower base score to keep sparing; mutually exclusive with half-time.
    public sealed class DoubleTimeFeelOperator : DrumOperatorBase
    {
        private const int KickDownbeatVelocityMin = 95;
        private const int KickDownbeatVelocityMax = 115;
        private const int KickOffbeatVelocityMin = 75;
        private const int KickOffbeatVelocityMax = 95;
        private const int SnareVelocityMin = 100;
        private const int SnareVelocityMax = 120;
        private const double BaseScore = 0.45; // Lower for sparing use

        public override string OperatorId => "DrumDoubleTimeFeel";

        public override OperatorFamily OperatorFamily => OperatorFamily.PatternSubstitution;

        // Requires kick role; prefer high-energy, chorus/solo/outro sections. Mutual exclusion with half-time handled externally.
        protected override string? RequiredRole => GrooveRoles.Kick;

        // CanApply: check base, bar length, and section suitability for double-time feel.
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Need at least 3 beats for meaningful double-time
            if (context.Bar.BeatsPerBar < 3)
                return false;

            // Best suited for high-energy sections
            var sectionType = context.Bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            bool isSuitableSection = sectionType is
                MusicConstants.eSectionType.Chorus or
                MusicConstants.eSectionType.Solo or
                MusicConstants.eSectionType.Outro;

            if (!isSuitableSection)
                return false;

            return true;
        }

        // Generate dense kick + snare backbeat candidates to realize double-time feel.
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
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
            if (true /* role check deferred */)
            {
                foreach (var snareCandidate in GenerateSnarePattern(drummerContext, baseScore))
                {
                    yield return snareCandidate;
                }
            }
        }

        // Produce kick candidates at downbeats and offbeats (8th-note density) deterministically.
        private IEnumerable<DrumCandidate> GenerateKickPattern(DrummerContext context, double baseScore)
        {
            int beatsPerBar = context.Bar.BeatsPerBar;

            // Generate 8th note kick pattern (every beat + offbeats)
            for (int beatInt = 1; beatInt <= beatsPerBar; beatInt++)
            {
                // Downbeat kick
                int downbeatVelocity = GenerateVelocityHint(
                    KickDownbeatVelocityMin, KickDownbeatVelocityMax,
                    context.Bar.BarNumber, beatInt,
                    context.Seed);

                OnsetStrength downbeatStrength = beatInt == 1 ? OnsetStrength.Downbeat : OnsetStrength.Strong;

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: context.Bar.BarNumber,
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
                        context.Bar.BarNumber, offbeatPosition,
                        context.Seed);

                    yield return CreateCandidate(
                        role: GrooveRoles.Kick,
                        barNumber: context.Bar.BarNumber,
                        beat: offbeatPosition,
                        strength: OnsetStrength.Offbeat,
                        score: baseScore * 0.85, // Lower score for offbeats
                        velocityHint: offbeatVelocity);
                }
            }
        }

        // Produce snare backbeat candidates (from Bar.BackbeatBeats) with strong velocity hints.
        private IEnumerable<DrumCandidate> GenerateSnarePattern(DrummerContext context, double baseScore)
        {
            // Standard backbeats (2 and 4) with high velocity for double-time energy
            foreach (int backbeat in context.Bar.BackbeatBeats)
            {
                if (backbeat > context.Bar.BeatsPerBar)
                    continue;

                int snareVelocity = GenerateVelocityHint(
                    SnareVelocityMin, SnareVelocityMax,
                    context.Bar.BarNumber, backbeat,
                    context.Seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: context.Bar.BarNumber,
                    beat: backbeat,
                    strength: OnsetStrength.Backbeat,
                    score: baseScore,
                    velocityHint: snareVelocity);
            }
        }

        // Compute base score for this operator considering section and boundaries.
        private double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Boost at section boundaries
            if (context.Bar.IsAtSectionBoundary)
                score += 0.15;

            // Boost in chorus (most natural home for double-time)
            var sectionType = context.Bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            if (sectionType == MusicConstants.eSectionType.Chorus)
                score += 0.1;

            // Higher energy = more appropriate for double-time
            score *= 0.5 + 0.5 /* default energy factor */;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
