// AI: purpose=Default policy provider that returns no overrides, preserving current system behavior (Story A3, 4.2).
// AI: invariants=Always returns NoOverrides; thread-safe; stateless.
// AI: deps=IDrumPolicyProvider interface; DrumPolicyDecision for result.
// AI: change=Story A3, 4.2: updated to use Drum interfaces (scheduled for deletion in Story 5.4).

using Music.Generator.Agents.Drums;

namespace Music.Generator.Groove
{
    /// <summary>
    /// Default policy provider that returns no overrides.
    /// Drummer Policy Hook - baseline implementation that preserves current behavior.
    /// Thread-safe and stateless; can be used as singleton.
    /// Story 4.2: Updated to use IDrumPolicyProvider (scheduled for deletion in Story 5.4).
    /// </summary>
    public sealed class DefaultGroovePolicyProvider : IDrumPolicyProvider
    {
        /// <summary>
        /// Singleton instance for default provider.
        /// </summary>
        public static readonly DefaultGroovePolicyProvider Instance = new();

        /// <summary>
        /// Gets policy decision with no overrides (default behavior).
        /// </summary>
        /// <param name="barContext">Bar context (ignored - no overrides generated).</param>
        /// <param name="role">Role name (ignored - no overrides generated).</param>
        /// <returns>DrumPolicyDecision with no overrides set.</returns>
        public DrumPolicyDecision? GetPolicy(DrumBarContext barContext, string role)
        {
            // Story A3: Default provider returns "no overrides" to produce identical output
            return DrumPolicyDecision.NoOverrides;
        }
    }
}
