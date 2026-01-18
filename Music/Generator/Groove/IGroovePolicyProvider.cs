// AI: purpose=Define interface for external policy providers to drive groove decisions (Story A3).
// AI: invariants=Implementations must be deterministic for same bar context + role; null return treated as NoOverrides.
// AI: deps=GrooveBarContext for bar context; GroovePolicyDecision for policy result.
// AI: change=Story A3 acceptance criteria: stable hook interface for future drummer model without refactors.

namespace Music.Generator
{
    /// <summary>
    /// Interface for external policy providers that can drive groove decisions.
    /// Story A3: Drummer Policy Hook - allows future human drummer model to override groove behavior.
    /// Implementations should be deterministic: same bar context + role => same decision.
    /// </summary>
    public interface IGroovePolicyProvider
    {
        /// <summary>
        /// Gets policy decision for a specific bar and role.
        /// Called once per bar per role during groove generation.
        /// </summary>
        /// <param name="barContext">Bar context with section, segment profile, and phrase position.</param>
        /// <param name="role">Role name (e.g., "Kick", "Snare", "Bass"). See GrooveRoles constants.</param>
        /// <returns>
        /// Policy decision with optional overrides, or null to use default behavior.
        /// Null is treated identically to GroovePolicyDecision.NoOverrides.
        /// </returns>
        /// <remarks>
        /// This method may be called multiple times during generation if the system needs to 
        /// re-evaluate decisions. Implementations should return consistent results for the same inputs.
        /// </remarks>
        GroovePolicyDecision? GetPolicy(GrooveBarContext barContext, string role);
    }
}
