// AI: purpose=Choose chord tones across octaves to minimize melodic leaps between bass onsets.
// AI: invariants=Requires SongContext+GroovePresetDefinition; skips beats with missing harmony.
// AI: deps=BassOperatorHelper chord tone/root helpers; octave search spans 1-3.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.HarmonicTargeting
{
    public sealed class BassStepwiseVoiceLeadingOperator : OperatorBase
    {
        private const int BaseOctave = 2;

        public override string OperatorId => "BassStepwiseVoiceLeading";

        public override OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (bassOnsets.Count == 0)
                yield break;

            int? previousPitch = null;
            foreach (var beat in bassOnsets.OrderBy(b => b))
            {
                var candidates = new List<int>();
                for (int octave = 1; octave <= 3; octave++)
                {
                    var tones = BassOperatorHelper.GetChordToneMidiNotes(
                        SongContext,
                        bar.BarNumber,
                        beat,
                        "root",
                        octave);

                    if (tones.Count > 0)
                        candidates.AddRange(tones);
                }

                if (candidates.Count == 0)
                    continue;

                int targetPitch;
                if (previousPitch.HasValue)
                {
                    targetPitch = FindClosestPitch(candidates, previousPitch.Value);
                }
                else
                {
                    int? root = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, beat, BaseOctave);
                    targetPitch = root.HasValue ? FindClosestPitch(candidates, root.Value) : candidates[0];
                }

                previousPitch = targetPitch;

                yield return CreateCandidate(
                    role: GrooveRoles.Bass,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    score: 1.0,
                    midiNote: targetPitch);
            }
        }

        private static int FindClosestPitch(IReadOnlyList<int> candidates, int referencePitch)
        {
            int best = candidates[0];
            int bestDistance = Math.Abs(best - referencePitch);

            for (int i = 1; i < candidates.Count; i++)
            {
                int candidate = candidates[i];
                int distance = Math.Abs(candidate - referencePitch);
                if (distance < bestDistance || (distance == bestDistance && candidate < best))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}
