// AI: purpose=PhrasePunctuation operator: full-bar turnaround fills at section ends.
// AI: invariants=Apply when Bar.IsFillWindow && BarsUntilSectionEnd<=1; deterministic positions from (seed,bar).
// AI: deps=Bar, OperatorCandidateAddition; prefers snare-led fills with optional kick downbeats; anti-repeat handled externally.


using Music.Generator.Core;
using Music.Generator.Drums.Planning;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.PhrasePunctuation
{
    // Create a full-bar turnaround fill for section ends; distributes accents and ghosts across 16ths.
    // Note: hit density scales by energy; roles map to kick on downbeats and snare elsewhere.
    public sealed class TurnaroundFillFullOperator : OperatorBase
    {
        private const int GhostVelocityMin = 45;
        private const int GhostVelocityMax = 65;
        private const int AccentVelocityMin = 85;
        private const int AccentVelocityMax = 115;
        private const double BaseScore = 0.75;
        private const string FillTag = "TurnaroundFull";

        /// <inheritdoc/>
        public override string OperatorId => DrumFillOperatorIds.TurnaroundFillFull;

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.PhrasePunctuation;

        /// <inheritdoc/>
        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            int beatsPerBar = bar.BeatsPerBar;

            // Compute hit count based on energy (8-16 hits for full bar fill)
            int hitCount = ComputeHitCount(0.5); // default energy

            // Generate fill pattern spanning full bar
            var positions = GenerateFillPositions(beatsPerBar, hitCount, seed, bar.BarNumber);

            bool isFirst = true;
            int positionCount = positions.Count();
            int currentPosition = 0;

            foreach (var (beat, role, isAccent) in positions)
            {
                currentPosition++;
                FillRole fillRole;
                if (isFirst)
                {
                    fillRole = FillRole.FillStart;
                    isFirst = false;
                }
                else if (currentPosition == positionCount)
                {
                    fillRole = FillRole.FillEnd;
                }
                else
                {
                    fillRole = FillRole.FillBody;
                }

                int velMin = isAccent ? AccentVelocityMin : GhostVelocityMin;
                int velMax = isAccent ? AccentVelocityMax : GhostVelocityMax;

                int velocityHint = GenerateVelocityHint(
                    velMin, velMax,
                    bar.BarNumber, beat,
                    seed);

                OnsetStrength strength = isAccent ? OnsetStrength.Strong : OnsetStrength.Offbeat;

                double score = ComputeScore(bar);

                yield return CreateCandidate(
                    role: role,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    score: score,
                    velocityHint: velocityHint,
                    instrumentData: DrumCandidateData.Create(
                        strength: strength,
                        fillRole: fillRole));
            }
        }

        private static int ComputeHitCount(double energyLevel)
        {
            // 8 hits at energy 0.4, 16 hits at energy 1.0
            return 8 + (int)((energyLevel - 0.4) / 0.6 * 8);
        }

        private static IEnumerable<(decimal Beat, string Role, bool IsAccent)> GenerateFillPositions(
            int beatsPerBar, int hitCount, int seed, int barNumber)
        {
            // All 16th positions in the bar
            var allPositions = new List<decimal>();
            for (int beatInt = 1; beatInt <= beatsPerBar; beatInt++)
            {
                allPositions.Add(beatInt);
                allPositions.Add(beatInt + 0.25m);
                allPositions.Add(beatInt + 0.5m);
                allPositions.Add(beatInt + 0.75m);
            }

            // Select positions deterministically
            var selected = new List<decimal>();
            var available = new List<decimal>(allPositions);

            // Ensure we have key anchor points
            selected.Add(1.0m); // First beat
            available.Remove(1.0m);

            // Last 16th as fill end
            decimal lastPosition = beatsPerBar + 0.75m;
            if (available.Contains(lastPosition))
            {
                selected.Add(lastPosition);
                available.Remove(lastPosition);
            }

            // Add beat 3 as midpoint accent
            if (beatsPerBar >= 3 && available.Contains(3.0m))
            {
                selected.Add(3.0m);
                available.Remove(3.0m);
            }

            // Fill remaining slots deterministically
            int remainingHits = Math.Max(0, hitCount - selected.Count);
            for (int i = 0; i < remainingHits && available.Count > 0; i++)
            {
                int hash = HashCode.Combine(seed, barNumber, "TurnaroundFull", i);
                int idx = Math.Abs(hash) % available.Count;
                selected.Add(available[idx]);
                available.RemoveAt(idx);
            }

            // Sort and assign roles (mostly snare with some kicks on downbeats)
            foreach (decimal beat in selected.OrderBy(b => b))
            {
                bool isDownbeat = (beat % 1.0m) == 0.0m;
                // Use kick on beat 1, snare for the rest
                string role = (beat == 1.0m) ? GrooveRoles.Kick : GrooveRoles.Snare;
                bool isAccent = isDownbeat;

                yield return (beat, role, isAccent);
            }
        }

        private static double ComputeScore(Bar bar)
        {
            double score = BaseScore;

            // Energy scaling
            // Higher at actual section end
            if (bar.BarsUntilSectionEnd == 0)
                score *= 1.2;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
