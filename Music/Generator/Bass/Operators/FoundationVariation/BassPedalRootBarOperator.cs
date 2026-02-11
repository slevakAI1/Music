// AI: purpose=Rewrite each bar to a single bass root pedal at beat 1; removes other bass anchors.
// AI: invariants=Requires SongContext harmony+groove; returns no candidates when missing.
// AI: deps=ChordVoicingHelper, GroovePresetDefinition.AnchorLayer; removal targets anchor beats != 1.

using Music.Generator.Core;
using Music.Generator.Groove;
using Music;

namespace Music.Generator.Bass.Operators.FoundationVariation
{
    public sealed class BassPedalRootBarOperator : OperatorBase
    {
        private const int BaseOctave = 2;
        private const string BassRoot = "root";

        public override string OperatorId => "BassPedalRootBar";

        public override OperatorFamily OperatorFamily => OperatorFamily.PatternSubstitution;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext?.HarmonyTrack == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            var harmonyEvent = SongContext.HarmonyTrack.GetActiveHarmonyEvent(bar.BarNumber, 1.0m);
            if (harmonyEvent == null)
                yield break;

            var chordMidiNotes = ChordVoicingHelper.GenerateChordMidiNotes(
                harmonyEvent.Key,
                harmonyEvent.Degree,
                harmonyEvent.Quality,
                BassRoot,
                BaseOctave);

            if (chordMidiNotes.Count == 0)
                yield break;

            long rawDurationTicks = bar.EndTick - bar.StartTick;
            if (rawDurationTicks <= 0)
                yield break;

            int durationTicks = (int)Math.Min(int.MaxValue, rawDurationTicks);

            yield return CreateCandidate(
                role: GrooveRoles.Bass,
                barNumber: bar.BarNumber,
                beat: 1.0m,
                score: 1.0,
                midiNote: chordMidiNotes[0],
                durationTicks: durationTicks);
        }

        public override IEnumerable<OperatorCandidateRemoval> GenerateRemovals(Bar bar)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext?.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            var groovePreset = SongContext.GroovePresetDefinition.GetActiveGroovePreset(bar.BarNumber);
            var bassOnsets = groovePreset.AnchorLayer.GetOnsets(GrooveRoles.Bass);
            if (bassOnsets.Count == 0)
                yield break;

            foreach (var beat in bassOnsets)
            {
                if (beat == 1.0m)
                    continue;

                yield return new OperatorCandidateRemoval
                {
                    BarNumber = bar.BarNumber,
                    Beat = beat,
                    Role = GrooveRoles.Bass,
                    OperatorId = OperatorId,
                    Reason = "Pedal root replaces non-downbeat anchors"
                };
            }
        }
    }
}
