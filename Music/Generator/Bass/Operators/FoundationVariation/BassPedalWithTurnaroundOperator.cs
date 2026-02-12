// AI: purpose=Pedal root for most of bar and add late-bar approach notes into next bar root.
// AI: invariants=Requires HarmonyTrack+GroovePresetDefinition; skips turnaround when next bar harmony missing.
// AI: deps=ChordVoicingHelper; durations use Bar ticks; beat 4 turnarounds assume 4+ beats.

using Music.Generator.Bass.Operators;
using Music.Generator.Core;
using Music.Generator.Groove;
using Music;

namespace Music.Generator.Bass.Operators.FoundationVariation
{
    public sealed class BassPedalWithTurnaroundOperator : OperatorBase
    {
        private const int BaseOctave = 2;
        private const int ApproachStep1 = -2;
        private const int ApproachStep2 = -1;

        public override string OperatorId => "BassPedalWithTurnaround";

        public override OperatorFamily OperatorFamily => OperatorFamily.PatternSubstitution;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            int? rootNote = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, 1.0m, BaseOctave);
            if (!rootNote.HasValue)
                yield break;

            int root = rootNote.Value;
            int beatsPerBar = bar.BeatsPerBar;
            if (beatsPerBar <= 0 || bar.TicksPerBeat <= 0)
                yield break;

            int sustainBeats = beatsPerBar >= 4 ? 3 : Math.Max(1, beatsPerBar - 1);
            int sustainTicks = bar.TicksPerBeat * sustainBeats;
            if (sustainTicks <= 0)
                yield break;

            yield return CreateCandidate(
                role: GrooveRoles.Bass,
                barNumber: bar.BarNumber,
                beat: 1.0m,
                score: 1.0,
                midiNote: root,
                durationTicks: sustainTicks);

            if (beatsPerBar < 4)
                yield break;

            int totalBars = SongContext.SectionTrack.TotalBars;
            if (totalBars <= 0 || bar.BarNumber >= totalBars)
                yield break;

            int? nextRoot = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber + 1, 1.0m, BaseOctave);
            if (!nextRoot.HasValue)
                yield break;

            int targetRoot = nextRoot.Value;
            int halfBeatTicks = Math.Max(1, bar.TicksPerBeat / 2);

            yield return CreateCandidate(
                role: GrooveRoles.Bass,
                barNumber: bar.BarNumber,
                beat: 4.0m,
                score: 1.0,
                midiNote: Math.Clamp(targetRoot + ApproachStep1, 0, 127),
                durationTicks: halfBeatTicks);

            yield return CreateCandidate(
                role: GrooveRoles.Bass,
                barNumber: bar.BarNumber,
                beat: 4.5m,
                score: 1.0,
                midiNote: Math.Clamp(targetRoot + ApproachStep2, 0, 127),
                durationTicks: halfBeatTicks);
        }
    }
}
