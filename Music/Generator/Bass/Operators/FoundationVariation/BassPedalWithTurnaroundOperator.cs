// AI: purpose=Pedal root for most of bar and add late-bar approach notes into next bar root.
// AI: invariants=Requires HarmonyTrack+GroovePresetDefinition; skips turnaround when next bar harmony missing.
// AI: deps=ChordVoicingHelper; durations use Bar ticks; beat 4 turnarounds assume 4+ beats.

using Music.Generator.Core;
using Music.Generator.Groove;
using Music;

namespace Music.Generator.Bass.Operators.FoundationVariation
{
    public sealed class BassPedalWithTurnaroundOperator : OperatorBase
    {
        private const int BaseOctave = 2;
        private const string BassRoot = "root";
        private const int ApproachStep1 = -2;
        private const int ApproachStep2 = -1;

        public override string OperatorId => "BassPedalWithTurnaround";

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

            int root = chordMidiNotes[0];
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

            var nextHarmonyEvent = SongContext.HarmonyTrack.GetActiveHarmonyEvent(bar.BarNumber + 1, 1.0m);
            if (nextHarmonyEvent == null)
                yield break;

            var nextChordNotes = ChordVoicingHelper.GenerateChordMidiNotes(
                nextHarmonyEvent.Key,
                nextHarmonyEvent.Degree,
                nextHarmonyEvent.Quality,
                BassRoot,
                BaseOctave);

            if (nextChordNotes.Count == 0)
                yield break;

            int targetRoot = nextChordNotes[0];
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
