// AI: purpose=Reduce large melodic leaps by octave-shifting current onset toward previous pitch.
// AI: invariants=Only acts when leap > 9 semitones; keeps notes within [28,55].
// AI: deps=BassOperatorHelper for anchor beats and range clamp; uses chord root as base pitch.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.RegisterAndContour
{
    public sealed class BassContourSmootherOperator : OperatorBase
    {
        private const int MinRange = 28;
        private const int MaxRange = 55;
        private const int BaseOctave = 2;
        private const int MaxLeap = 9;

        public override string OperatorId => "BassContourSmoother";

        public override OperatorFamily OperatorFamily => OperatorFamily.StyleIdiom;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (bassOnsets.Count < 2)
                yield break;

            var orderedBeats = bassOnsets.OrderBy(b => b).ToList();
            int? previousPitch = null;

            foreach (var beat in orderedBeats)
            {
                int? rootNote = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, beat, BaseOctave);
                if (!rootNote.HasValue)
                    continue;

                int currentPitch = BassOperatorHelper.ClampToRange(rootNote.Value, MinRange, MaxRange);

                if (!previousPitch.HasValue)
                {
                    previousPitch = currentPitch;
                    continue;
                }

                int distance = Math.Abs(currentPitch - previousPitch.Value);
                if (distance <= MaxLeap)
                {
                    previousPitch = currentPitch;
                    continue;
                }

                int up = BassOperatorHelper.ClampToRange(currentPitch + 12, MinRange, MaxRange);
                int down = BassOperatorHelper.ClampToRange(currentPitch - 12, MinRange, MaxRange);

                int adjusted = ChooseClosest(previousPitch.Value, currentPitch, up, down);
                if (adjusted == currentPitch)
                {
                    previousPitch = currentPitch;
                    continue;
                }

                yield return CreateCandidate(
                    role: GrooveRoles.Bass,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    score: 1.0,
                    midiNote: adjusted);

                previousPitch = adjusted;
            }
        }

        private static int ChooseClosest(int previousPitch, int currentPitch, int up, int down)
        {
            int best = currentPitch;
            int bestDistance = Math.Abs(previousPitch - currentPitch);

            int upDistance = Math.Abs(previousPitch - up);
            if (upDistance < bestDistance)
            {
                best = up;
                bestDistance = upDistance;
            }

            int downDistance = Math.Abs(previousPitch - down);
            if (downDistance < bestDistance)
            {
                best = down;
            }

            return best;
        }
    }
}
