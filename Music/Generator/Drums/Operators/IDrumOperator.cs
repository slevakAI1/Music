// AI: purpose=Drum-specific operator interface extending IMusicalOperator<DrumCandidate> for drummer pipeline.
// AI: invariants=Deterministic outputs for same bar+seed; OperatorId unique within DrumOperatorRegistry.
// AI: deps=IMusicalOperator<DrumCandidate>, DrumCandidate, Bar; used by DrummerOperatorCandidates.

using Music.Generator.Core;
using Music.Generator.Drums.Operators.Candidates;

namespace Music.Generator.Drums.Operators
{
    // AI: purpose=Interface for drum operators producing DrumCandidate outputs for selection pipeline.
    // AI: contracts=Mapping->DrumCandidateMapper; grouping by OperatorFamily; filtered by PhysicalityFilter downstream.
    public interface IDrumOperator : IMusicalOperator<DrumCandidate>
    {
    }
}
