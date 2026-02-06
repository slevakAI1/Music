// AI: purpose=Generic musical operator interface; instrument agents provide TCandidate-specific implementations
// AI: invariants=OperatorId must be stable; implementations deterministic given same RNG/context; Score in [0,1]
// AI: deps=Generic over TCandidate; GeneratorContext may be extended per instrument (DrummerContext etc.)
namespace Music.Generator.Core
{
    // AI: contract=Operator generates and scores candidates; keep signatures stable when evolving operator system
    public interface IMusicalOperator<TCandidate>
    {
        // AI: id=Stable operator identifier; unique per agent and consistent across runs for determinism
        string OperatorId { get; }

        // AI: family=OperatorFamily groups similar operators for weighting and selection policies
        OperatorFamily OperatorFamily { get; }

        // AI: prefilter=Fast check to skip generation; should avoid RNG and heavy computation
        bool CanApply(GeneratorContext context);

        // AI: generate=Yield candidates deterministically given same context and RNG streams
        IEnumerable<TCandidate> GenerateCandidates(GeneratorContext context);

        // AI: score=Return [0.0..1.0]; used by selection: final = Score * styleWeight * (1-memoryPenalty)
        double Score(TCandidate candidate, GeneratorContext context);
    }
}
