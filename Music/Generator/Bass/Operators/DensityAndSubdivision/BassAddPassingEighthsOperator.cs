// AI: purpose=Insert passing 8th notes between bass anchors to step toward next pitch; caps density per bar.
// AI: invariants=Only fills gaps >= 1 beat; uses anchor pitches (root) for stepping; skips if missing harmony.
// AI: deps=BassOperatorHelper for anchor beats and root lookup; assumes 4/4 8th grid.

using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Bass.Operators.DensityAndSubdivision
{
    public sealed class BassAddPassingEighthsOperator : OperatorBase
    {
        private const int BaseOctave = 2;
        private const int MaxOnsetsPerBar = 10;
        private const decimal StepBeats = 0.5m;

        public override string OperatorId => "BassAddPassingEighths";

        public override OperatorFamily OperatorFamily => OperatorFamily.PhrasePunctuation;

        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (SongContext == null || SongContext.GroovePresetDefinition?.AnchorLayer == null)
                yield break;

            if (bar.TicksPerBeat <= 0)
                yield break;

            var bassOnsets = BassOperatorHelper.GetBassAnchorBeats(SongContext, bar.BarNumber);
            if (bassOnsets.Count < 2)
                yield break;

            var orderedBeats = bassOnsets.OrderBy(b => b).ToList();
            int added = 0;
            int durationTicks = Math.Max(1, bar.TicksPerBeat / 2);

            for (int i = 0; i < orderedBeats.Count - 1; i++)
            {
                decimal currentBeat = orderedBeats[i];
                decimal nextBeat = orderedBeats[i + 1];

                if (nextBeat - currentBeat < 1.0m)
                    continue;

                int? startPitch = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, currentBeat, BaseOctave);
                int? endPitch = BassOperatorHelper.GetChordRootMidiNote(SongContext, bar.BarNumber, nextBeat, BaseOctave);
                if (!startPitch.HasValue || !endPitch.HasValue)
                    continue;

                int steps = (int)Math.Floor((nextBeat - currentBeat) / StepBeats) - 1;
                if (steps <= 0)
                    continue;

                int direction = Math.Sign(endPitch.Value - startPitch.Value);
                if (direction == 0)
                    direction = -1;

                int stepSemitones = Math.Abs(endPitch.Value - startPitch.Value) > steps ? 2 : 1;
                int pitch = startPitch.Value;

                for (int s = 0; s < steps; s++)
                {
                    if (bassOnsets.Count + added >= MaxOnsetsPerBar)
                        yield break;

                    pitch += direction * stepSemitones;
                    decimal beat = currentBeat + StepBeats * (s + 1);

                    yield return CreateCandidate(
                        role: GrooveRoles.Bass,
                        barNumber: bar.BarNumber,
                        beat: beat,
                        score: 1.0,
                        midiNote: pitch,
                        durationTicks: durationTicks);

                    added++;
                }
            }
        }
    }
}
