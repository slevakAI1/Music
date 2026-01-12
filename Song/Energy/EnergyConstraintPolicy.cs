// AI: purpose=Groups energy constraint rules by style/genre with configurable rule strengths.
// AI: invariants=Rules list non-null (may be empty); rule application order deterministic; all strengths >= 0.
// AI: deps=Contains EnergyConstraintRule instances; consumed by constraint application pipeline.

namespace Music.Generator
{
    /// <summary>
    /// Groups energy constraint rules for a specific style or genre.
    /// Rules can be enabled/disabled and have configurable strengths.
    /// </summary>
    public sealed class EnergyConstraintPolicy
    {
        /// <summary>
        /// Name of this policy (e.g., "Pop", "Rock", "Jazz").
        /// </summary>
        public required string PolicyName { get; init; }

        /// <summary>
        /// Ordered list of rules to apply.
        /// Rules are applied in order; earlier rules have precedence in case of conflicts.
        /// </summary>
        public required IReadOnlyList<EnergyConstraintRule> Rules { get; init; }

        /// <summary>
        /// Whether this policy is enabled.
        /// </summary>
        public bool IsEnabled { get; init; } = true;

        /// <summary>
        /// Creates an empty policy (no constraints).
        /// </summary>
        public static EnergyConstraintPolicy Empty(string policyName = "None")
        {
            return new EnergyConstraintPolicy
            {
                PolicyName = policyName,
                Rules = Array.Empty<EnergyConstraintRule>(),
                IsEnabled = false
            };
        }

        /// <summary>
        /// Evaluates all rules against the given context and returns a final adjusted energy value.
        /// Rules are applied in order with deterministic conflict resolution.
        /// </summary>
        /// <param name="context">Context containing section info and related energies.</param>
        /// <returns>Adjusted energy value [0..1] and list of diagnostic messages from rules that applied.</returns>
        public (double AdjustedEnergy, List<string> Diagnostics) Apply(EnergyConstraintContext context)
        {
            if (!IsEnabled || Rules.Count == 0)
            {
                return (context.ProposedEnergy, new List<string>());
            }

            var diagnostics = new List<string>();
            double currentEnergy = context.ProposedEnergy;

            // Apply rules in order
            // When multiple rules suggest adjustments, we blend them weighted by strength
            var adjustments = new List<(double SuggestedEnergy, double Strength, string Message)>();

            foreach (var rule in Rules)
            {
                var result = rule.Evaluate(context);

                if (result.HasAdjustment && result.AdjustedEnergy.HasValue)
                {
                    adjustments.Add((result.AdjustedEnergy.Value, rule.Strength, 
                        $"{rule.RuleName}: {result.DiagnosticMessage ?? "adjusted"}"));
                }
                else if (result.DiagnosticMessage != null)
                {
                    diagnostics.Add($"{rule.RuleName}: {result.DiagnosticMessage}");
                }
            }

            // If we have adjustments, blend them
            if (adjustments.Count > 0)
            {
                double finalEnergy = ResolveAdjustments(currentEnergy, adjustments, out var resolutionDiagnostic);
                diagnostics.AddRange(adjustments.Select(a => a.Message));
                
                if (!string.IsNullOrEmpty(resolutionDiagnostic))
                {
                    diagnostics.Add(resolutionDiagnostic);
                }

                return (finalEnergy, diagnostics);
            }

            return (currentEnergy, diagnostics);
        }

        /// <summary>
        /// Resolves multiple rule adjustments into a single energy value.
        /// Uses strength-weighted averaging for deterministic conflict resolution.
        /// </summary>
        private static double ResolveAdjustments(
            double originalEnergy, 
            List<(double SuggestedEnergy, double Strength, string Message)> adjustments,
            out string diagnostic)
        {
            if (adjustments.Count == 0)
            {
                diagnostic = string.Empty;
                return originalEnergy;
            }

            if (adjustments.Count == 1)
            {
                diagnostic = $"Applied single adjustment: {originalEnergy:F3} ? {adjustments[0].SuggestedEnergy:F3}";
                return adjustments[0].SuggestedEnergy;
            }

            // Strength-weighted average
            double totalWeight = adjustments.Sum(a => a.Strength);
            double weightedSum = adjustments.Sum(a => a.SuggestedEnergy * a.Strength);
            double resolvedEnergy = weightedSum / totalWeight;

            diagnostic = $"Resolved {adjustments.Count} adjustments: {originalEnergy:F3} ? {resolvedEnergy:F3} " +
                        $"(weighted avg of {string.Join(", ", adjustments.Select(a => $"{a.SuggestedEnergy:F3}"))})";

            return Math.Clamp(resolvedEnergy, 0.0, 1.0);
        }
    }
}
