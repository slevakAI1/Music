// AI: purpose=Structured diagnostics report for HarmonyTrack validation with per-event details.
// AI: invariants=Errors collection contains track-level and per-event fatal issues; Warnings are advisory only.
// AI: deps=Used by HarmonyValidator; consumed by HarmonyEditorForm for UI display.
// AI: thread-safety=Mutable collections; callers should not modify after creation or use thread-safe wrappers.

namespace Music.Generator
{
    // AI: diagnostics=Contains track-level errors/warnings plus per-event diagnostic details.
    public sealed class HarmonyDiagnostics
    {
        // AI: Errors: fatal validation failures at track or event level; each includes location and details.
        public List<string> Errors { get; init; } = new();

        // AI: Warnings: non-fatal issues or applied fixes; does not affect IsValid.
        public List<string> Warnings { get; init; } = new();

        // AI: EventDiagnostics: per-event detailed diagnostics (bar:beat, summary, errors, warnings).
        // AI: note=Indexed by event order in track; parallel to HarmonyTrack.Events.
        public List<HarmonyEventDiagnostic> EventDiagnostics { get; init; } = new();

        // AI: IsValid: true when no errors exist at track or event level.
        public bool IsValid => Errors.Count == 0 && EventDiagnostics.All(ed => ed.Errors.Count == 0);

        // AI: HasWarnings: true when track-level or event-level warnings exist.
        public bool HasWarnings => Warnings.Count > 0 || EventDiagnostics.Any(ed => ed.Warnings.Count > 0);

        // AI: ToString: formats diagnostics for logging/debugging with stable message format for tests.
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Valid: {IsValid}, Warnings: {HasWarnings}");

            if (Errors.Count > 0)
            {
                sb.AppendLine($"Track-level Errors ({Errors.Count}):");
                foreach (var err in Errors)
                    sb.AppendLine($"  - {err}");
            }

            if (Warnings.Count > 0)
            {
                sb.AppendLine($"Track-level Warnings ({Warnings.Count}):");
                foreach (var warn in Warnings)
                    sb.AppendLine($"  - {warn}");
            }

            if (EventDiagnostics.Count > 0)
            {
                sb.AppendLine($"Event Diagnostics ({EventDiagnostics.Count} events):");
                for (int i = 0; i < EventDiagnostics.Count; i++)
                {
                    var ed = EventDiagnostics[i];
                    if (ed.Errors.Count > 0 || ed.Warnings.Count > 0)
                    {
                        sb.AppendLine($"  Event[{i}] at {ed.Location}: {ed.Summary}");
                        foreach (var err in ed.Errors)
                            sb.AppendLine($"    ERROR: {err}");
                        foreach (var warn in ed.Warnings)
                            sb.AppendLine($"    WARN: {warn}");
                    }
                }
            }

            return sb.ToString();
        }
    }
}
