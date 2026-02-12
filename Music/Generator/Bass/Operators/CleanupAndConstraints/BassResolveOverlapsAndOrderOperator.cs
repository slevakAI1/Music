// AI: purpose=Cleanup operator to enforce monophonic bass by shortening prior notes to avoid tick overlaps.
// AI: invariants=Deterministic; safe on null MidiNote/duration; requires SongContext.BarTrack for tick math.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.CleanupAndConstraints;

public sealed class BassResolveOverlapsAndOrderOperator : OperatorBase
{
    private const int DefaultMinDurationTicks = 60;

    public override string OperatorId => "BassResolveOverlapsAndOrder";

    public override OperatorFamily OperatorFamily => OperatorFamily.SubdivisionTransform;

    public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
    {
        ArgumentNullException.ThrowIfNull(bar);

        if (SongContext?.BarTrack is null)
            yield break;

        var anchorBeats = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
        if (anchorBeats.Count < 2)
            yield break;

        var orderedBeats = anchorBeats.OrderBy(b => b).ToList();
        for (int i = 0; i < orderedBeats.Count - 1; i++)
        {
            decimal beat = orderedBeats[i];
            decimal nextBeat = orderedBeats[i + 1];

            long startTick = SongContext.BarTrack.ToTick(bar.BarNumber, beat);
            long nextTick = SongContext.BarTrack.ToTick(bar.BarNumber, nextBeat);
            long maxDurLong = nextTick - startTick;
            if (maxDurLong <= 0)
                continue;

            int maxDur = (int)Math.Clamp(maxDurLong, DefaultMinDurationTicks, int.MaxValue);
            yield return CreateCandidate(
                role: GrooveRoles.Bass,
                barNumber: bar.BarNumber,
                beat: beat,
                score: 1.0,
                durationTicks: maxDur);
        }
    }

    private long GetStartTick(int barNumber, decimal beat, int? timingOffsetTicks)
    {
        long tick = SongContext!.BarTrack!.ToTick(barNumber, beat);
        if (timingOffsetTicks.HasValue)
            tick += timingOffsetTicks.Value;

        return tick;
    }

    private static int GetDurationTicksOrMin(int? durationTicks)
    {
        if (!durationTicks.HasValue)
            return DefaultMinDurationTicks;

        return durationTicks.Value < DefaultMinDurationTicks ? DefaultMinDurationTicks : durationTicks.Value;
    }
}
