// AI: purpose=Shared orchestration gate for role presence checks; used by generators to decide role on/off.
// AI: invariants=Returns true when orchestrationPolicy is null or section defaults not found; non-throwing.

using System;
using System.Linq;

namespace Music.Generator
{
    public static class RolePresenceGate
    {
        // Returns true when role should be active in the given section type according to orchestration policy.
        // Behavior: null policy => present; missing section defaults => present; role not listed => fallback to "DrumKit" master switch => present.
        public static bool IsRolePresent(string sectionType, string roleName, GrooveOrchestrationPolicy? orchestrationPolicy)
        {
            if (orchestrationPolicy == null)
                return true;

            var sectionDefaults = orchestrationPolicy.DefaultsBySectionType
                .FirstOrDefault(d => string.Equals(d.SectionType, sectionType, StringComparison.OrdinalIgnoreCase));

            if (sectionDefaults == null)
                return true;

            if (string.IsNullOrWhiteSpace(roleName))
                return true;

            if (sectionDefaults.RolePresent.TryGetValue(roleName, out bool rolePresent))
                return rolePresent;

            if (sectionDefaults.RolePresent.TryGetValue("DrumKit", out bool drumKitPresent))
                return drumKitPresent;

            return true;
        }
    }
}
