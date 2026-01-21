// AI: purpose=Configurable decay curve for repetition penalty calculation in agent memory.
// AI: invariants=Enum values stable for determinism; Linear=simple decay, Exponential=faster initial decay.
// AI: deps=Used by AgentMemory.GetRepetitionPenalty(); affects how quickly penalty decreases with distance.
// AI: change=Add new curves at END only to preserve ordinals.

namespace Music.Generator.Agents.Common
{
    /// <summary>
    /// Defines how repetition penalty decays over the memory window.
    /// Affects how quickly the penalty decreases as decisions age.
    /// </summary>
    public enum DecayCurve
    {
        /// <summary>
        /// Linear decay: penalty decreases uniformly with distance.
        /// penalty = (windowSize - age) / windowSize
        /// More forgiving for repeated use across window.
        /// </summary>
        Linear = 0,

        /// <summary>
        /// Exponential decay: penalty drops faster initially, then slows.
        /// penalty = decayFactor ^ age
        /// Strongly penalizes recent repetition, quickly forgives older use.
        /// </summary>
        Exponential = 1
    }
}
