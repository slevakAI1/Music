// AI: purpose=Represents a discrete tempo change at a bar/beat; used by TempoTrack and MIDI export to set BPM.
// AI: invariants=StartBar/StartBeat are 1-based; TempoBpm expected >0; track should order events by StartBar/StartBeat.
// AI: deps=Consumed by arrangers, exporters, and UI; renaming properties breaks serialization and editors.
// AI: change=If adding tempo curves or ramps, add new type/field and keep this simple DTO for discrete events.

namespace Music.Generator
{
    // AI: tempo event DTO: keep immutable init props to preserve simple value semantics across pipelines.
    public sealed class TempoEvent
    {
        // AI: StartBar/StartBeat: 1-based placement; StartBeat defaults to 1 for bar anchor.
        public int StartBar { get; init; }
        public int StartBeat { get; init; } = 1;

        // AI: TempoBpm: integer BPM value (>0). Validation is expected from callers if needed.
        public int TempoBpm { get; init; }
    }
}
