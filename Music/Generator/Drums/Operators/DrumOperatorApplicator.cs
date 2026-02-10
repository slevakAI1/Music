// AI: purpose=Copy anchors, randomly apply N operators (additive or removal) from registry, return updated onsets.
// AI: invariants=Only counts successfully applied operators toward target; dedup by (BarNumber, Beat, Role).
// AI: deps=DrumOperatorRegistry.GetAllOperators; Rng(DrumGenerator); GrooveOnset; IDrumRemovalOperator.

using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators
{
    // AI: purpose=Apply random operators to anchor onsets; supports both additive and removal operators.
    public static class DrumOperatorApplicator
    {
        // AI: entry=Copy anchors, randomly pick operators, apply if valid (add or remove), return combined onsets.
        // AI: invariants=Loop counts only applied operators; maxAttempts prevents infinite loop; removal respects protection flags.
        public static List<GrooveOnset> Apply(
            IReadOnlyList<Bar> bars,
            List<GrooveOnset> anchorOnsets,
            int totalBars,
            int numberOfOperators,
            DrumOperatorRegistry registry)
        {
            var result = new List<GrooveOnset>(anchorOnsets);

            // Build a set of existing positions to detect duplicates: (BarNumber, Beat, Role)
            var occupied = new HashSet<(int BarNumber, decimal Beat, string Role)>();
            foreach (var onset in result)
                occupied.Add((onset.BarNumber, onset.Beat, onset.Role));

            var allOperators = registry.GetAllOperators();
            if (allOperators.Count == 0)
                return result;

            var barsInScope = bars.Where(b => b.BarNumber <= totalBars).ToList();
            if (barsInScope.Count == 0)
                return result;

            int applied = 0;
            int maxAttempts = numberOfOperators * 10;
            int attempts = 0;

            while (applied < numberOfOperators && attempts < maxAttempts)
            {
                attempts++;

                // Pick a random operator
                int opIndex = Rng.NextInt(RandomPurpose.DrumGenerator, 0, allOperators.Count);
                var op = allOperators[opIndex];

                // Pick a random bar
                int barIndex = Rng.NextInt(RandomPurpose.DrumGenerator, 0, barsInScope.Count);
                var bar = barsInScope[barIndex];

                int seed = bar.BarNumber;

                // Route to removal path or additive path
                if (op is IDrumRemovalOperator removalOp)
                {
                    if (ApplyRemovals(removalOp, bar, result, occupied))
                        applied++;
                }
                else
                {
                    if (ApplyAdditions(op, bar, seed, result, occupied))
                        applied++;
                }
            }

            return result.OrderBy(o => o.BarNumber).ThenBy(o => o.Beat).ToList();
        }

        // AI: purpose=Apply additive operator candidates; skip duplicates; returns true if any onset added.
        private static bool ApplyAdditions(
            DrumOperatorBase op,
            Bar bar,
            int seed,
            List<GrooveOnset> result,
            HashSet<(int BarNumber, decimal Beat, string Role)> occupied)
        {
            var candidates = op.GenerateCandidates(bar, seed).ToList();

            bool anyApplied = false;
            foreach (var candidate in candidates)
            {
                var key = (candidate.BarNumber, candidate.Beat, candidate.Role);
                if (occupied.Contains(key))
                    continue;

                occupied.Add(key);
                result.Add(new GrooveOnset
                {
                    Role = candidate.Role,
                    BarNumber = candidate.BarNumber,
                    Beat = candidate.Beat,
                    Velocity = candidate.VelocityHint ?? 100,
                    Strength = candidate.Strength,
                    TimingOffsetTicks = candidate.TimingHint
                });
                anyApplied = true;
            }

            return anyApplied;
        }

        // AI: purpose=Apply removal operator; skip protected/must-hit/never-remove onsets; returns true if any removed.
        private static bool ApplyRemovals(
            IDrumRemovalOperator removalOp,
            Bar bar,
            List<GrooveOnset> result,
            HashSet<(int BarNumber, decimal Beat, string Role)> occupied)
        {
            var removals = removalOp.GenerateRemovals(bar).ToList();

            bool anyRemoved = false;
            foreach (RemovalCandidate removal in removals)
            {
                var key = (removal.BarNumber, removal.Beat, removal.Role);
                if (!occupied.Contains(key))
                    continue;

                // Find the onset and check protection flags
                int index = result.FindIndex(o =>
                    o.BarNumber == removal.BarNumber &&
                    o.Beat == removal.Beat &&
                    o.Role == removal.Role);

                if (index < 0)
                    continue;

                GrooveOnset target = result[index];

                // Respect protection flags from GrooveOnset
                if (target.IsMustHit || target.IsNeverRemove)
                    continue;

                result.RemoveAt(index);
                occupied.Remove(key);
                anyRemoved = true;
            }

            return anyRemoved;
        }
    }
}
