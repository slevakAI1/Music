// AI: purpose=Constraint operator; prune bass anchors per bar to keep density playable.
// AI: invariants=Deterministic; never targets beat-1 if any other removal exists; safe on null pitch/duration.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.CleanupAndConstraints;

public sealed class BassPreventOverDensityOperator : OperatorBase
{
    private const int DefaultMaxOnsetsPerBar = 10;

    public override string OperatorId => "BassPreventOverDensity";

    public override OperatorFamily OperatorFamily => OperatorFamily.NoteRemoval;

    public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
    {
        ArgumentNullException.ThrowIfNull(bar);
        yield break;
    }

    public override IEnumerable<OperatorCandidateRemoval> GenerateRemovals(Bar bar)
    {
        ArgumentNullException.ThrowIfNull(bar);

        if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
            yield break;

        var beats = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
        if (beats.Count <= DefaultMaxOnsetsPerBar)
            yield break;

        var ordered = beats.OrderBy(b => b).ToList();

        int toRemove = ordered.Count - DefaultMaxOnsetsPerBar;
        var removalSet = new HashSet<decimal>();

        var candidates = ordered
            .Select((beat, idx) => new
            {
                Beat = beat,
                Index = idx,
                Score = ScoreRemovalBeat(ordered, idx)
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Beat != 1.0m)
            .ThenByDescending(x => x.Beat)
            .ToList();

        foreach (var c in candidates)
        {
            if (toRemove <= 0)
                break;

            if (c.Beat == 1.0m && candidates.Any(x => x.Beat != 1.0m && !removalSet.Contains(x.Beat)))
                continue;

            if (!removalSet.Add(c.Beat))
                continue;

            toRemove--;
        }

        foreach (decimal beat in removalSet.OrderBy(b => b))
        {
            yield return new OperatorCandidateRemoval
            {
                BarNumber = bar.BarNumber,
                Beat = beat,
                Role = GrooveRoles.Bass,
                OperatorId = OperatorId,
                Reason = "Cap bass onsets per bar"
            };
        }
    }

    private static int ScoreRemovalBeat(IReadOnlyList<decimal> orderedBeats, int index)
    {
        decimal beat = orderedBeats[index];

        int score = 0;

        if (!BassOperatorHelper.IsStrongBeat(beat))
            score += 1;

        if (beat != 1.0m)
            score += 1;

        return score;
    }
}
