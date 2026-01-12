// AI: purpose=Result of energy constraint rule evaluation with adjusted energy and diagnostic message.
// AI: invariants=AdjustedEnergy [0..1] or null; HasAdjustment = (AdjustedEnergy != null).
// AI: deps=Returned by EnergyConstraintRule.Evaluate; consumed by constraint application pipeline.

namespace Music.Generator
{
    /// <summary>
    /// Result of evaluating an energy constraint rule.
    /// Contains an adjusted energy value (if the rule applies) and optional diagnostic message.
    /// </summary>
    public sealed class EnergyConstraintResult
    {
        /// <summary>
        /// Adjusted energy value [0..1], or null if the rule has no opinion.
        /// When null, the rule does not apply or suggest any change.
        /// </summary>
        public double? AdjustedEnergy { get; init; }

        /// <summary>
        /// Optional diagnostic message explaining the rule's decision.
        /// Should be concise and suitable for debugging/logging.
        /// </summary>
        public string? DiagnosticMessage { get; init; }

        /// <summary>
        /// Whether this rule made an adjustment.
        /// </summary>
        public bool HasAdjustment => AdjustedEnergy.HasValue;

        /// <summary>
        /// Rule had no opinion (does not apply to this section).
        /// </summary>
        public static EnergyConstraintResult NoOpinion()
        {
            return new EnergyConstraintResult
            {
                AdjustedEnergy = null,
                DiagnosticMessage = null
            };
        }

        /// <summary>
        /// Rule suggests an adjustment with diagnostic message.
        /// </summary>
        public static EnergyConstraintResult Adjusted(double adjustedEnergy, string diagnosticMessage)
        {
            return new EnergyConstraintResult
            {
                AdjustedEnergy = Math.Clamp(adjustedEnergy, 0.0, 1.0),
                DiagnosticMessage = diagnosticMessage
            };
        }

        /// <summary>
        /// Rule accepts the proposed energy without change.
        /// </summary>
        public static EnergyConstraintResult Accept(string? diagnosticMessage = null)
        {
            return new EnergyConstraintResult
            {
                AdjustedEnergy = null,
                DiagnosticMessage = diagnosticMessage
            };
        }
    }
}
