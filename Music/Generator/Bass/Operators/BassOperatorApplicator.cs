// AI: purpose=Copy anchors, randomly apply N bass operators from registry, return updated onsets.
// AI: invariants=Counts only successful applications; dedup by (BarNumber, Beat, Role).
// AI: deps=BassOperatorRegistry.GetAllOperators; Rng(DrumGenerator); GrooveOnset.

using Music.Generator;
using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators
{
    // AI: purpose=Apply random operators to anchor onsets; removal optional via base hook.
    public static class BassOperatorApplicator
    {
        // AI: cleanup=Always run after random ops; deterministic order; not part of random pool.
        private static readonly string[] CleanupOperatorIds =
        {
            "BassSnapBeatsToSubdivision",
            "BassResolveOverlapsAndOrder",
            "BassPreventOverDensity"
        };

        // AI: entry=Copy anchors, randomly pick operators, apply if valid (add/remove), return combined onsets.
        // AI: invariants=Sets SongContext on each operator so bass operators can access HarmonyTrack for pitch selection.
        public static List<GrooveOnset> Apply(
            IReadOnlyList<Bar> bars,
            List<GrooveOnset> anchorOnsets,
            int totalBars,
            int numberOfOperators,
            BassOperatorRegistry registry,
            SongContext? songContext = null)
        {
            var result = new List<GrooveOnset>(anchorOnsets);

            var occupied = new HashSet<(int BarNumber, decimal Beat, string Role)>();
            foreach (var onset in result)
                occupied.Add((onset.BarNumber, onset.Beat, onset.Role));

            var allOperators = registry.GetAllOperators()
                .Where(op => !CleanupOperatorIds.Contains(op.OperatorId))
                .ToList();
            if (allOperators.Count == 0)
                return result;

            // Provide song context to operators so bass operators can access HarmonyTrack
            if (songContext is not null)
            {
                foreach (var op in allOperators)
                    op.SongContext = songContext;
            }

            var barsInScope = bars.Where(b => b.BarNumber <= totalBars).ToList();
            if (barsInScope.Count == 0)
                return result;

            var plan = new List<(int OpIndex, int BarIndex, int Mode)>(allOperators.Count * barsInScope.Count * 2);
            for (int opIndex = 0; opIndex < allOperators.Count; opIndex++)
            {
                for (int barIndex = 0; barIndex < barsInScope.Count; barIndex++)
                {
                    plan.Add((opIndex, barIndex, 0));
                    plan.Add((opIndex, barIndex, 1));
                }
            }

            for (int i = plan.Count - 1; i > 0; i--)
            {
                int j = Rng.NextInt(RandomPurpose.DrumGenerator, 0, i + 1);
                (plan[i], plan[j]) = (plan[j], plan[i]);
            }

            int applied = 0;

            for (int k = 0; k < plan.Count && applied < numberOfOperators; k++)
            {
                var (opIndex, barIndex, mode) = plan[k];

                var op = allOperators[opIndex];
                var bar = barsInScope[barIndex];
                int seed = bar.BarNumber;

                bool changed = mode == 0
                    ? ApplyAdditions(op, bar, seed, result, occupied)
                    : ApplyRemovals(op, bar, result, occupied);

                if (changed)
                    applied++;
            }

            ApplyCleanupPostPass(registry, barsInScope, result, occupied);

            return result.OrderBy(o => o.BarNumber).ThenBy(o => o.Beat).ToList();
        }

        private static void ApplyCleanupPostPass(
            BassOperatorRegistry registry,
            IReadOnlyList<Bar> barsInScope,
            List<GrooveOnset> result,
            HashSet<(int BarNumber, decimal Beat, string Role)> occupied)
        {
            if (!result.Any(o => o.Role == GrooveRoles.Bass))
                return;

            foreach (string operatorId in CleanupOperatorIds)
            {
                var op = registry.GetOperatorById(operatorId);
                if (op is null)
                    continue;

                foreach (var bar in barsInScope)
                {
                    int seed = bar.BarNumber;
                    ApplyAdditions(op, bar, seed, result, occupied);
                    ApplyRemovals(op, bar, result, occupied);
                }
            }
        }

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
                {
                    if (!HasCandidateUpdates(candidate))
                        continue;

                    int index = result.FindIndex(o =>
                        o.BarNumber == candidate.BarNumber &&
                        o.Beat == candidate.Beat &&
                        o.Role == candidate.Role);

                    if (index < 0)
                        continue;

                    GrooveOnset existing = result[index];
                    result[index] = existing with
                    {
                        Velocity = candidate.VelocityHint ?? existing.Velocity,
                        TimingOffsetTicks = candidate.TimingHint ?? existing.TimingOffsetTicks,
                        MidiNote = candidate.MidiNote ?? existing.MidiNote,
                        DurationTicks = candidate.DurationTicks ?? existing.DurationTicks
                    };
                    anyApplied = true;
                    continue;
                }

                occupied.Add(key);
                result.Add(new GrooveOnset
                {
                    Role = candidate.Role,
                    BarNumber = candidate.BarNumber,
                    Beat = candidate.Beat,
                    Velocity = candidate.VelocityHint ?? 100,
                    TimingOffsetTicks = candidate.TimingHint,
                    MidiNote = candidate.MidiNote,
                    DurationTicks = candidate.DurationTicks
                });
                anyApplied = true;
            }

            return anyApplied;
        }

        private static bool HasCandidateUpdates(OperatorCandidateAddition candidate)
        {
            return candidate.VelocityHint.HasValue ||
                   candidate.TimingHint.HasValue ||
                   candidate.MidiNote.HasValue ||
                   candidate.DurationTicks.HasValue;
        }

        private static bool ApplyRemovals(
            OperatorBase op,
            Bar bar,
            List<GrooveOnset> result,
            HashSet<(int BarNumber, decimal Beat, string Role)> occupied)
        {
            var removals = op.GenerateRemovals(bar).ToList();

            bool anyRemoved = false;
            foreach (OperatorCandidateRemoval removal in removals)
            {
                var key = (removal.BarNumber, removal.Beat, removal.Role);
                if (!occupied.Contains(key))
                    continue;

                int index = result.FindIndex(o =>
                    o.BarNumber == removal.BarNumber &&
                    o.Beat == removal.Beat &&
                    o.Role == removal.Role);

                if (index < 0)
                    continue;

                GrooveOnset target = result[index];

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
