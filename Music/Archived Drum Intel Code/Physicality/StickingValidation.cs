// AI: purpose=Holds result of sticking validation; used by PhysicalityFilter and diagnostics.
// AI: invariants=Violations list empty => IsValid=true; Violations include candidate ids and positions for actionability.
using System.Collections.Generic;

namespace Music.Generator.Agents.Drums.Physicality
{
    public sealed record StickingViolation(
        string RuleId,
        string Message,
        IReadOnlyList<string> CandidateIds,
        int BarNumber,
        decimal Beat,
        Limb? LimbInvolved);

    public sealed record StickingValidation
    {
        public bool IsValid => Violations == null || Violations.Count == 0;
        public List<StickingViolation> Violations { get; init; } = new();
    }
}
