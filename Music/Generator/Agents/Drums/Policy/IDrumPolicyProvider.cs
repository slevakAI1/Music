// AI: purpose=Define interface for drum policy providers to drive generation decisions (Story 4.2).
// AI: invariants=Implementations must be deterministic for same bar context + role; null return treated as NoOverrides.
// AI: deps=Bar for bar context; DrumPolicyDecision for policy result.
// AI: change=Story 5.2: Updated to use Bar (moved from Groove.DrumBarContext).

using Music.Generator;
using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Interface for drum policy providers that drive generation decisions.
    /// Story 4.2: Moved from Groove namespace - Drum generator owns policy contracts.
    /// Implementations should be deterministic: same bar context + role => same decision.
    /// </summary>
    public interface IDrumPolicyProvider
    {
        /// <summary>
        /// Gets policy decision for a specific bar and role.
        /// Called once per bar per role during drum generation.
        /// </summary>
        /// <param name="bar">Bar context with section, segment profile, and phrase position.</param>
        /// <param name="role">Role name (e.g., "Kick", "Snare", "ClosedHat"). See GrooveRoles constants.</param>
        /// <returns>
        /// Policy decision with optional overrides, or null to use default behavior.
        /// Null is treated identically to DrumPolicyDecision.NoOverrides.
        /// </returns>
        /// <remarks>
        /// This method may be called multiple times during generation if the system needs to 
        /// re-evaluate decisions. Implementations should return consistent results for the same inputs.
        /// </remarks>
        DrumPolicyDecision? GetPolicy(Bar bar, string role);
    }
}
