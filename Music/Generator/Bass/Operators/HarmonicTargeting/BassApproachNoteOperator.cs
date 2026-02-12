// AI: purpose=Add an 8th-note approach into chord-root targets (beat 1 and chord changes).
// AI: invariants=Requires HarmonyTrack+GroovePresetDefinition; skips if approach beat < 1.0.
// AI: deps=ChordVoicingHelper; approach pitch = target root - 1 semitone.

using Music.Generator.Bass.Operators;
using Music.Generator.Core;
using Music.Generator.Groove;
using Music;

namespace Music.Generator.Bass.Operators.HarmonicTargeting
{
    public sealed class BassApproachNoteOperator : OperatorBase
    {
        private const int BaseOctave = 2;
        private const decimal ApproachOffsetBeats = 0.5m;

        public override string OperatorId => "BassApproachNote";

        public override OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            if (bar.TicksPerBeat <= 0)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (bassOnsets.Count == 0)
                yield break;

            var orderedBeats = bassOnsets.OrderBy(b => b).ToList();
            var targetBeats = new HashSet<decimal>();

            if (orderedBeats.Contains(1.0m))
                targetBeats.Add(1.0m);

            HarmonyEvent? previousEvent = null;
            foreach (decimal beat in orderedBeats)
            {
                var harmonyEvent = SongContext.HarmonyTrack.GetActiveHarmonyEvent(bar.BarNumber, beat);
                if (harmonyEvent == null)
                    continue;

                if (previousEvent != null && !IsSameHarmony(previousEvent, harmonyEvent))
                    targetBeats.Add(beat);

                previousEvent = harmonyEvent;
            }

            int durationTicks = Math.Max(1, bar.TicksPerBeat / 2);

            foreach (decimal targetBeat in targetBeats.OrderBy(b => b))
            {
                decimal approachBeat = targetBeat - ApproachOffsetBeats;
                if (approachBeat < 1.0m)
                    continue;

                var harmonyEvent = SongContext.HarmonyTrack.GetActiveHarmonyEvent(bar.BarNumber, targetBeat);
                if (harmonyEvent == null)
                    continue;

                int? rootNote = BassOperatorHelper.GetChordRootMidiNote(
                    SongContext,
                    bar.BarNumber,
                    targetBeat,
                    BaseOctave);

                if (!rootNote.HasValue)
                    continue;

                int approachNote = Math.Clamp(rootNote.Value - 1, 0, 127);

                yield return CreateCandidate(
                    role: GrooveRoles.Bass,
                    barNumber: bar.BarNumber,
                    beat: approachBeat,
                    score: 1.0,
                    midiNote: approachNote,
                    durationTicks: durationTicks);
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
