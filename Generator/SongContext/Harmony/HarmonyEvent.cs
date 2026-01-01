// AI: purpose=Data model for a single harmony event; active until the next HarmonyEvent starts.
// AI: invariants=StartBar/StartBeat are 1-based; Degree expected 1..7; callers normalize Key/Quality elsewhere.
// AI: deps=Consumed by Generator, HarmonyPitchContextBuilder, and UI editors; renaming props breaks serialization/UI.
// AI: change=If adding fields update editor form, persistence, and any normalization routines.

namespace Music.Generator
{
    // AI: lightweight DTO with init-only props for editor and generation pipelines; keep init semantics to preserve immutability.
    public sealed class HarmonyEvent
    {
        // AI: StartBar/StartBeat: 1-based placement. Event is active until the next HarmonyEvent.StartBar.
        public int StartBar { get; init; }
        public int StartBeat { get; init; } = 1;

        // AI: Key: free-form (e.g., "C major"); parsed by PitchClassUtils.ParseKey at usage sites.
        public string Key { get; init; } = "C major";
        // AI: Degree: scale degree 1..7. Validators/consumers expect this range.
        public int Degree { get; init; } // 1..7
        // AI: Quality: chord symbol or long name; normalized via ChordQuality.Normalize() by consumers.
        public string Quality { get; init; } = "maj"; // maj, min7, dom7, etc.
        // AI: Bass: inversion hint like "root","3rd","5th","7th"; consumers map to voicings.
        public string Bass { get; init; } = "root";


        // AI: DurationBeats: UI/data-entry support only; generators typically infer duration from next event.
        public int DurationBeats { get; init; } = 4;
    }
}