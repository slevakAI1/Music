// AI: purpose=Drum-specific operator interface extending IMusicalOperator<OperatorCandidate> for drummer pipeline.
// AI: invariants=Deterministic outputs for same bar+seed; OperatorId unique within DrumOperatorRegistry.
// AI: deps=IMusicalOperator<OperatorCandidate>, OperatorCandidate, Bar; used by DrummerOperatorCandidates.

using Music.Generator.Core;
using Music.Generator.Drums.Operators.Candidates;

namespace Music.Generator.Drums.Operators
{
    // AI: purpose=Interface for drum operators producing OperatorCandidate outputs for selection pipeline.
    // AI: contracts=Mapping->DrumCandidateMapper; grouping by OperatorFamily; filtered by PhysicalityFilter downstream.
    public interface IDrumOperator : IMusicalOperator<OperatorCandidate>
    {
    }
}
