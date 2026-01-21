// AI: purpose=Interface for agent memory tracking; prevents repetitive patterns by recording decisions.
// AI: invariants=Memory operations must be deterministic; same sequence of records = same memory state.
// AI: deps=FillShape for fill memory; MusicConstants.eSectionType for section signatures.
// AI: change=Implement AgentMemory in Story 1.2; may add additional query methods as agent needs emerge.

using Music;

namespace Music.Generator.Agents.Common
{
    /// <summary>
    /// Interface for tracking agent decisions to prevent repetition.
    /// Human musicians don't repeat the exact same pattern 8 times—memory
    /// tracks recent choices and penalizes repetition.
    /// </summary>
    public interface IAgentMemory
    {
        /// <summary>
        /// Records a decision made by an operator at a specific bar.
        /// </summary>
        /// <param name="barNumber">1-based bar number where decision was made.</param>
        /// <param name="operatorId">Stable identifier of the operator that was applied.</param>
        /// <param name="candidateId">Identifier of the specific candidate that was selected.</param>
        void RecordDecision(int barNumber, string operatorId, string candidateId);

        /// <summary>
        /// Gets usage counts for operators in the last N bars.
        /// Used to compute repetition penalties.
        /// </summary>
        /// <param name="lastNBars">Number of recent bars to consider.</param>
        /// <returns>Dictionary mapping operatorId to usage count in the window.</returns>
        IReadOnlyDictionary<string, int> GetRecentOperatorUsage(int lastNBars);

        /// <summary>
        /// Gets the most recent fill shape, if any.
        /// Used by fill operators to vary fill patterns.
        /// </summary>
        /// <returns>The last fill shape, or null if no fills recorded.</returns>
        FillShape? GetLastFillShape();

        /// <summary>
        /// Gets the signature choices made for a section type.
        /// Signature = recurring operator choices that define section identity.
        /// </summary>
        /// <param name="sectionType">The section type to query.</param>
        /// <returns>List of operator IDs that characterize this section type.</returns>
        IReadOnlyList<string> GetSectionSignature(MusicConstants.eSectionType sectionType);

        /// <summary>
        /// Records a fill shape for future reference.
        /// </summary>
        /// <param name="fillShape">The fill shape to record.</param>
        void RecordFillShape(FillShape fillShape);

        /// <summary>
        /// Records an operator as part of a section's signature.
        /// </summary>
        /// <param name="sectionType">The section type.</param>
        /// <param name="operatorId">The operator ID to add to the signature.</param>
        void RecordSectionSignature(MusicConstants.eSectionType sectionType, string operatorId);

        /// <summary>
        /// Clears all recorded decisions and resets memory state.
        /// Used when starting a new generation pass.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets the current bar number of the most recent decision.
        /// Returns 0 if no decisions have been recorded.
        /// </summary>
        int CurrentBarNumber { get; }
    }
}
