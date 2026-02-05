using Music.Generator;

namespace Music.Generator.Agents.Drums;

// AI: purpose=Default drum policy provider that returns no overrides, preserving baseline behavior.
// AI: invariants=Always returns NoOverrides; thread-safe; stateless singleton.
// AI: deps=IDrumPolicyProvider interface; DrumPolicyDecision for result.
// AI: change=Moved from Groove.DefaultGroovePolicyProvider; drum system owns policy contracts.
public sealed class DefaultDrumPolicyProvider : IDrumPolicyProvider
{
    public static readonly DefaultDrumPolicyProvider Instance = new();

    private DefaultDrumPolicyProvider() { }

    // AI: Returns NoOverrides for all contexts; preserves current system behavior without modification.
    public DrumPolicyDecision? GetPolicy(Bar bar, string role)
    {
        return DrumPolicyDecision.NoOverrides;
    }
}
