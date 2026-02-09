// AI: purpose=Generic musical operator interface; instrument agents provide TCandidate-specific implementations
// AI: invariants=OperatorId must be stable; implementations deterministic given same bar+seed; Score in [0,1]
// AI: deps=Generic over TCandidate; no shared context object.
namespace Music.Generator.Core
{
    // AI: contract=Operator generates and scores candidates; keep signatures stable when evolving operator system
    public interface IMusicalOperator<TCandidate>
    {
        // AI: id=Stable operator identifier; unique per agent and consistent across runs for determinism
        string OperatorId { get; }

        // AI: family=OperatorFamily groups similar operators for weighting and selection policies
        OperatorFamily OperatorFamily { get; }

        // AI: generate=Yield candidates deterministically given same bar and seed
        IEnumerable<TCandidate> GenerateCandidates(Bar bar, int seed);

        // AI: score=Return [0.0..1.0]; used by selection: final = Score * styleWeight * (1-memoryPenalty)
        double Score(TCandidate candidate, Bar bar);
    }
}
