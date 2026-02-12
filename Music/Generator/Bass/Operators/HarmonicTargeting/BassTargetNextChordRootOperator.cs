// AI: purpose=On chord changes, force first bass onset after change to new chord root.
// AI: invariants=Requires HarmonyTrack+GroovePresetDefinition; skips bars with no bass anchors.
// AI: deps=ChordVoicingHelper; change detection uses Key/Degree/Quality/Bass.

using Music.Generator.Bass.Operators;
using Music.Generator.Core;
using Music.Generator.Groove;
using Music;

namespace Music.Generator.Bass.Operators.HarmonicTargeting
{
    public sealed class BassTargetNextChordRootOperator : OperatorBase
    {
        private const int BaseOctave = 2;
        public override string OperatorId => "BassTargetNextChordRoot";

        public override OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (bassOnsets.Count == 0)
                yield break;

            var orderedBeats = bassOnsets.OrderBy(b => b).ToList();
            HarmonyEvent? previousEvent = null;

            foreach (decimal beat in orderedBeats)
            {
                var harmonyEvent = SongContext.HarmonyTrack.GetActiveHarmonyEvent(bar.BarNumber, beat);
                if (harmonyEvent == null)
                    continue;

                if (previousEvent == null)
                {
                    previousEvent = harmonyEvent;
                    continue;
                }

                if (IsSameHarmony(previousEvent, harmonyEvent))
                    continue;

                int? rootNote = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, beat, BaseOctave);
                if (!rootNote.HasValue)
                {
                    previousEvent = harmonyEvent;
                    continue;
                }

                yield return CreateCandidate(
                    role: GrooveRoles.Bass,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    score: 1.0,
                    midiNote: rootNote.Value);

                previousEvent = harmonyEvent;
            }
        }

        private static bool IsSameHarmony(HarmonyEvent left, HarmonyEvent right)
        {
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);

            return string.Equals(left.Key, right.Key, StringComparison.OrdinalIgnoreCase) &&
                   left.Degree == right.Degree &&
                   string.Equals(left.Quality, right.Quality, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(left.Bass, right.Bass, StringComparison.OrdinalIgnoreCase);
        }
    }
}
