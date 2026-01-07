// AI: purpose=Per-event diagnostic details for HarmonyTrack validation.
// AI: invariants=Location format is "bar:beat"; Summary is one-line description of event state.
// AI: deps=Used by HarmonyDiagnostics; populated by HarmonyValidator during validation.
// AI: use-sites=HarmonyEditorForm can display per-event advisory messages.

namespace Music.Generator
{
    // AI: diagnostic=Captures validation details for a single HarmonyEvent (bar:beat location, summary, errors, warnings).
    public sealed class HarmonyEventDiagnostic
    {
        // AI: Location: human-readable position "bar:beat" (e.g., "1:1", "2:3").
        public string Location { get; init; } = string.Empty;

        // AI: Summary: one-line description of event chord (e.g., "C major I maj root", "A minor V7 3rd").
        public string Summary { get; init; } = string.Empty;

        // AI: EventIndex: zero-based index in HarmonyTrack.Events for programmatic lookup.
        public int EventIndex { get; init; }

        // AI: Errors: event-specific validation failures; empty if event is valid.
        public List<string> Errors { get; init; } = new();

        // AI: Warnings: event-specific advisory messages (e.g., "Non-diatonic chord tones").
        public List<string> Warnings { get; init; } = new();

        // AI: IsValid: true when no errors exist for this event.
        public bool IsValid => Errors.Count == 0;

        // AI: HasWarnings: true when warnings exist for this event.
        public bool HasWarnings => Warnings.Count > 0;

        // AI: ToString: formats event diagnostic for logging/debugging.
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"[{EventIndex}] {Location}: {Summary}");
            
            if (!IsValid)
                sb.Append($" - {Errors.Count} error(s)");
            
            if (HasWarnings)
                sb.Append($" - {Warnings.Count} warning(s)");

            return sb.ToString();
        }
    }
}
