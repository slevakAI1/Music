// AI: purpose=Configurable decay curves for repetition penalty used by AgentMemory.
// AI: invariants=Enum ordinals are stable; add new curves only at end to preserve persisted values.
// AI: deps=Consumed by AgentMemory.GetRepetitionPenalty(); changing semantics affects memory penalties.
namespace Music.Generator.Core
{
    // AI: contract=Controls penalty decay shape; consumer computes numeric penalty based on this enum
    public enum DecayCurve
    {
        // AI: Linear=uniform decay across window; penalty=(windowSize-age)/windowSize
        Linear = 0,

        // AI: Exponential=faster initial drop, then slower forgiveness; penalty ~= decayFactor^age
        Exponential = 1
    }
}
