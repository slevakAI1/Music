namespace Music.Generator
{
    // AI: purpose=Orchestration policy by section type; turns roles on/off and provides density/register hints per section.
    // AI: invariants=DefaultsBySectionType list holds SectionRolePresenceDefaults; lookup by SectionType string.
    // AI: change=Add SectionRolePresenceDefaults for new section types; update RolePresent to control role presence.
    public sealed class GrooveOrchestrationPolicy
    {
        public List<SectionRolePresenceDefaults> DefaultsBySectionType { get; set; } = new();
    }
}
