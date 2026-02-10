// AI: purpose=Copy anchors, randomly apply N operators from registry, return updated onsets.
// AI: invariants=Only counts successfully applied operators toward target; dedup by (BarNumber, Beat, Role).
// AI: deps=DrumOperatorRegistry.GetAllOperators; Rng(DrumGenerator); GrooveOnset.

using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;
using Music.Generator.Core;

namespace Music.Generator.Drums.Operators
{
    // AI: purpose=Apply random operators to anchor onsets; removal is optional via base hook.
    public static class DrumOperatorApplicator
    {
        // AI: entry=Copy anchors, randomly pick operators, apply if valid (add/remove), return combined onsets.
        // AI: invariants=Counts only successful applications; avoids infinite loops by iterating a shuffled plan.
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

            // Build a uniform "plan": every (operator, bar, mode) combination appears exactly once.
            // mode = 0 => additions only, mode = 1 => removals only
            var plan = new List<(int OpIndex, int BarIndex, int Mode)>(allOperators.Count * barsInScope.Count * 2);
            for (int opIndex = 0; opIndex < allOperators.Count; opIndex++)
            {
                for (int barIndex = 0; barIndex < barsInScope.Count; barIndex++)
                {
                    plan.Add((opIndex, barIndex, 0));
                    plan.Add((opIndex, barIndex, 1));
                }
            }

            // Shuffle plan uniformly using Rng (Fisher-Yates).
            for (int i = plan.Count - 1; i > 0; i--)
            {
                int j = Rng.NextInt(RandomPurpose.DrumGenerator, 0, i + 1);
                (plan[i], plan[j]) = (plan[j], plan[i]);
            }

            int applied = 0;

            // Walk the shuffled plan until we've successfully applied enough operations.
            // Each step is ONE decision: add OR remove (not both).
            for (int k = 0; k < plan.Count && applied < numberOfOperators; k++)
            {
                var (opIndex, barIndex, mode) = plan[k];

                var op = allOperators[opIndex];
                var bar = barsInScope[barIndex];
                int seed = bar.BarNumber;

                bool changed;
                if (mode == 0)
                {
                    changed = ApplyAdditions(op, bar, seed, result, occupied);
                }
                else
                {
                    changed = ApplyRemovals(op, bar, result, occupied);
                }

                if (changed)
                    applied++;
            }

            return result.OrderBy(o => o.BarNumber).ThenBy(o => o.Beat).ToList();
        }

        // AI: purpose=Apply additive operator candidates; skip duplicates; returns true if any onset added.
        private static bool ApplyAdditions(
            OperatorBase op,
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

        // AI: purpose=Apply removal targets; skip protected/must-hit/never-remove onsets; returns true if any removed.
        private static bool ApplyRemovals(
            OperatorBase op,
            Bar bar,
            List<GrooveOnset> result,
            HashSet<(int BarNumber, decimal Beat, string Role)> occupied)
        {
            var removals = op.GenerateRemovals(bar).ToList();

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
