// AI: purpose=PhrasePunctuation operator generating 2-beat fills at phrase end (beats 3-4 in 4/4).
// AI: invariants=Only applies when IsFillWindow=true; generates 4-8 hits based on energy; adapts to time signature.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate, DrummerMemory for anti-repetition.
// AI: change=Story 3.3; adjust hit density and pattern selection based on listening tests.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Planning;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.PhrasePunctuation
{
    /// <summary>
    /// Generates a 2-beat turnaround fill at the end of phrases (beats 3-4 in 4/4).
    /// Provides light punctuation for phrase boundaries without interrupting flow.
    /// Story 3.3: Phrase Punctuation Operators (Boundaries &amp; Fills).
    /// </summary>
    public sealed class TurnaroundFillShortOperator : DrumOperatorBase
    {
        private const int GhostVelocityMin = 40;
        private const int GhostVelocityMax = 60;
        private const int AccentVelocityMin = 80;
        private const int AccentVelocityMax = 110;
        private const double BaseScore = 0.7;
        private const string FillTag = "TurnaroundShort";

        /// <inheritdoc/>
        public override string OperatorId => FillOperatorIds.TurnaroundFillShort;

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.PhrasePunctuation;

        /// <summary>
        /// Requires minimum energy for fill to sound musical (not too sparse).
        /// </summary>

        /// <summary>
        /// Requires snare for fill hits (primary fill role).
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Snare;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Only in fill window
            if (!context.Bar.IsFillWindow)
                return false;

            // Need at least 2 beats for a short fill
            if (context.Bar.BeatsPerBar < 2)
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                yield break;

            if (!CanApply(drummerContext))
                yield break;

            // Fill occupies last 2 beats of the bar
            int beatsPerBar = drummerContext.Bar.BeatsPerBar;
            decimal fillStartBeat = beatsPerBar - 1; // e.g., beat 3 in 4/4

            // Compute hit count based on energy (4-8 hits for 2-beat fill)
            int hitCount = ComputeHitCount(0.5); // default energy

            // Generate fill pattern positions (16th grid within last 2 beats)
            var positions = GenerateFillPositions(fillStartBeat, beatsPerBar, hitCount, drummerContext.Seed, drummerContext.Bar.BarNumber);

            bool isFirst = true;
            foreach (var (beat, isAccent) in positions)
            {
                FillRole fillRole = isFirst ? FillRole.FillStart : FillRole.FillBody;
                isFirst = false;

                int velMin = isAccent ? AccentVelocityMin : GhostVelocityMin;
                int velMax = isAccent ? AccentVelocityMax : GhostVelocityMax;

                int velocityHint = GenerateVelocityHint(
                    velMin, velMax,
                    drummerContext.Bar.BarNumber, beat,
                    drummerContext.Seed);

                OnsetStrength strength = isAccent ? OnsetStrength.Strong : OnsetStrength.Ghost;

                // Score higher at section boundaries
                double score = ComputeScore(drummerContext);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: drummerContext.Bar.BarNumber,
                    beat: beat,
                    strength: strength,
                    score: score,
                    velocityHint: velocityHint,
                    fillRole: fillRole);
            }
        }

        private static int ComputeHitCount(double energyLevel)
        {
            // 4 hits at energy 0.0, 8 hits at energy 1.0
            return 4 + (int)(energyLevel * 4);
        }

        private static IEnumerable<(decimal Beat, bool IsAccent)> GenerateFillPositions(
            decimal fillStartBeat, int beatsPerBar, int hitCount, int seed, int barNumber)
        {
            // All available 16th positions in the last 2 beats
            var allPositions = new List<decimal>();
            for (int beatOffset = 0; beatOffset < 2; beatOffset++)
            {
                decimal baseBeat = fillStartBeat + beatOffset;
                if (baseBeat > beatsPerBar)
                    break;

                allPositions.Add(baseBeat);
                allPositions.Add(baseBeat + 0.25m);
                allPositions.Add(baseBeat + 0.5m);
                allPositions.Add(baseBeat + 0.75m);
            }

            // Filter to positions within bar
            allPositions = allPositions.Where(b => b >= 1.0m && b <= beatsPerBar + 0.75m).ToList();

            // Select hitCount positions deterministically
            var selected = new List<decimal>();
            var available = new List<decimal>(allPositions);

            // Always include first and last positions for fill shape
            if (available.Count > 0)
            {
                selected.Add(available[0]);
                available.RemoveAt(0);
            }
            if (available.Count > 0)
            {
                selected.Add(available[^1]);
                available.RemoveAt(available.Count - 1);
            }

            // Fill remaining slots deterministically
            int remainingHits = Math.Max(0, hitCount - selected.Count);
            for (int i = 0; i < remainingHits && available.Count > 0; i++)
            {
                int hash = HashCode.Combine(seed, barNumber, i);
                int idx = Math.Abs(hash) % available.Count;
                selected.Add(available[idx]);
                available.RemoveAt(idx);
            }

            // Sort and mark accents (downbeats are accents)
            foreach (decimal beat in selected.OrderBy(b => b))
            {
                bool isAccent = (beat % 1.0m) == 0.0m; // Downbeats are accents
                yield return (beat, isAccent);
            }
        }

        private static double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Higher score at section boundaries
            if (context.Bar.IsAtSectionBoundary)
                score *= 1.15;

            // Energy scaling
            // Boost near section end
            if (context.Bar.BarsUntilSectionEnd <= 1)
                score *= 1.1;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
