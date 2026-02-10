// AI: purpose=Carrier for drum candidate instrument data passed into core CreateCandidate
// AI: invariants=Strength required; FillRole defaults to None; ArticulationHint null means default
// AI: deps=Consumed by DrumOperatorCandidateInstrumentAdapter for metadata/discriminator

using Music.Generator.Drums.Planning;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.Candidates
{
    // AI: contract=Immutable drum instrument data passed to OperatorBase via adapter
    public sealed record DrumCandidateData
    {
        public required OnsetStrength Strength { get; init; }
        public FillRole FillRole { get; init; } = FillRole.None;
        public DrumArticulation? ArticulationHint { get; init; }

        public static DrumCandidateData Create(
            OnsetStrength strength,
            FillRole fillRole = FillRole.None,
            DrumArticulation? articulationHint = null)
        {
            return new DrumCandidateData
            {
                Strength = strength,
                FillRole = fillRole,
                ArticulationHint = articulationHint
            };
        }
    }
}
