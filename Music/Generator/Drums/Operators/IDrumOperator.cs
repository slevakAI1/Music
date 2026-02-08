// AI: purpose=Drum-specific operator interface extending IMusicalOperator<DrumCandidate> for drummer pipeline.
// AI: invariants=Deterministic outputs for same DrummerContext; OperatorId unique within DrumOperatorRegistry.
// AI: deps=IMusicalOperator<DrumCandidate>, DrumCandidate, DrummerContext; used by DrummerOperatorCandidates.

using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Candidates;

namespace Music.Generator.Drums.Operators
{
    // AI: purpose=Interface for drum operators producing DrumCandidate outputs for selection pipeline.
    // AI: note=Default DrummerContext overloads delegate to GeneratorContext variants; override when context-specific checks needed.
    // AI: contracts=Mapping->DrumCandidateMapper; grouping by OperatorFamily; filtered by PhysicalityFilter downstream.
    public interface IDrumOperator : IMusicalOperator<DrumCandidate>
    {
        // AI: purpose=Pre-filter using DrummerContext. Default: delegate to GeneratorContext CanApply.
        // AI: override when operator needs hat/fill/role-specific gating.
        bool CanApply(DrummerContext context)
        {
            // Default: delegate to base CanApply
            return CanApply((GeneratorContext)context);
        }
        // AI: purpose=Generate DrumCandidate instances from DrummerContext. Default delegates to GeneratorContext overload.
        IEnumerable<DrumCandidate> GenerateCandidates(DrummerContext context)
        {
            // Default: delegate to base GenerateCandidates
            return GenerateCandidates((GeneratorContext)context);
        }
        // AI: purpose=Score a DrumCandidate using DrummerContext. Default delegates to GeneratorContext Score.
        double Score(DrumCandidate candidate, DrummerContext context)
        {
            // Default: delegate to base Score
            return Score(candidate, (GeneratorContext)context);
        }
    }
}
