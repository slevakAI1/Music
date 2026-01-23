// AI: purpose=Default policy provider that returns no overrides, preserving current system behavior (Story A3).
// AI: invariants=Always returns NoOverrides; thread-safe; stateless.
// AI: deps=IGroovePolicyProvider interface; GroovePolicyDecision for result.
// AI: change=Story A3 acceptance criteria: default provider produces identical output to current system.

namespace Music.Generator.Groove
{
    /// <summary>
    /// Default policy provider that returns no overrides.
    /// Story A3: Drummer Policy Hook - baseline implementation that preserves current behavior.
    /// Thread-safe and stateless; can be used as singleton.
    /// </summary>
    public sealed class DefaultGroovePolicyProvider : IGroovePolicyProvider
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
        /// <returns>GroovePolicyDecision with no overrides set.</returns>
        public GroovePolicyDecision? GetPolicy(GrooveBarContext barContext, string role)
        {
            // Story A3: Default provider returns "no overrides" to produce identical output
            return GroovePolicyDecision.NoOverrides;
        }
    }
}
