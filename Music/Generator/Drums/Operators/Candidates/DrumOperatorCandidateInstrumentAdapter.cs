// AI: purpose=Adapt drum candidate data into core metadata/discriminator for OperatorBase
// AI: invariants=Returns deterministic results for same DrumCandidateData
// AI: deps=Implements IOperatorCandidateInstrumentAdapter from core

using Music.Generator.Core;

namespace Music.Generator.Drums.Operators.Candidates
{
    // AI: contract=Adapter from DrumCandidateData to core candidate metadata
    public sealed class DrumOperatorCandidateInstrumentAdapter : IOperatorCandidateInstrumentAdapter
    {
        public static DrumOperatorCandidateInstrumentAdapter Instance { get; } = new();

        public Dictionary<string, object>? BuildMetadata(object? instrumentData)
        {
            if (instrumentData is not DrumCandidateData drumData)
            {
                return null;
            }

            var metadata = new Dictionary<string, object>
            {
                ["Strength"] = drumData.Strength,
                ["FillRole"] = drumData.FillRole
            };

            if (drumData.ArticulationHint.HasValue)
            {
                metadata["ArticulationHint"] = drumData.ArticulationHint.Value;
            }

            return metadata;
        }
    }
}
