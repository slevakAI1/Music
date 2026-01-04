// AI: purpose=Result type for HarmonyValidator containing errors, warnings, and optional normalized events.
// AI: invariants=IsValid true only when Errors.Count==0; NormalizedEvents only set when ApplyFixes=true.
// AI: deps=Returned by HarmonyValidator.ValidateTrack; consumers check IsValid before using NormalizedEvents.
// AI: thread-safety=Mutable lists for errors/warnings; callers should not modify after creation or use thread-safe wrappers.

namespace Music.Generator
{
    // AI: result=IsValid derives from Errors; Warnings are advisory; NormalizedEvents only present when fixes applied.
    public sealed class HarmonyValidationResult
    {
        // AI: IsValid: true when no errors; does not consider warnings.
        public bool IsValid => Errors.Count == 0;

        // AI: Errors: fatal validation failures; each includes event index and field details.
        public List<string> Errors { get; init; } = new();

        // AI: Warnings: non-fatal issues or applied fixes; does not affect IsValid.
        public List<string> Warnings { get; init; } = new();

        // AI: NormalizedEvents: only set when ApplyFixes=true; contains normalized/clamped copies; null otherwise.
        public List<HarmonyEvent>? NormalizedEvents { get; init; }

        // AI: ToString: formats result for logging/debugging; stable message format for tests.
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Valid: {IsValid}");

            if (Errors.Count > 0)
            {
                sb.AppendLine($"Errors ({Errors.Count}):");
                foreach (var err in Errors)
                    sb.AppendLine($"  - {err}");
            }

            if (Warnings.Count > 0)
            {
                sb.AppendLine($"Warnings ({Warnings.Count}):");
                foreach (var warn in Warnings)
                    sb.AppendLine($"  - {warn}");
            }

            if (NormalizedEvents != null)
                sb.AppendLine($"NormalizedEvents: {NormalizedEvents.Count} events");

            return sb.ToString();
        }
    }
}
