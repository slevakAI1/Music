// AI: purpose=Simple MVP: copy anchors, randomly apply N operators from registry, skip duplicates, return updated onsets.
// AI: invariants=Only counts successfully applied operators toward target; dedup by (BarNumber, Beat, Role).
// AI: deps=DrumOperatorRegistry.GetAllOperators; DrummerContext; Rng(DrumGenerator); GrooveOnset

using Music.Generator.Drums.Context;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators
{
    // AI: purpose=Apply random operators to anchor onsets; no scoring, no weighting, no probability biases.
    public static class DrumOperatorApplicator
    {
        // AI: entry=Copy anchors, randomly pick operators, apply if valid, skip duplicates, return combined onsets.
        // AI: invariants=Loop counts only applied operators; maxAttempts prevents infinite loop if all operators fail.
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

                // Build minimal context
                var context = new DrummerContext
                {
                    Bar = bar,
                    Seed = bar.BarNumber,
                    RngStreamKey = $"Applicator_{bar.BarNumber}"
                };

                if (!op.CanApply(context))
                    continue;

                // Generate candidates and try to apply them
                var candidates = op.GenerateCandidates(context).ToList();

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

                if (anyApplied)
                    applied++;
            }

            return result.OrderBy(o => o.BarNumber).ThenBy(o => o.Beat).ToList();
        }
    }
}
