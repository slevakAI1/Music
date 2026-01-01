// AI: purpose=Discrete time-signature event activated at a 1-based bar index; used by timing track and exporters.
// AI: invariants=StartBar is 1-based; Numerator > 0; Denominator usually a power-of-two; this type does not validate values.
// AI: deps=Consumed by TimingTrack, exporters, and UI editors; renaming properties breaks serialization and consumers.
// AI: change=If adding time-signature ranges or ramping, create a new type rather than extending this simple DTO.

namespace Music.Generator
{
    // AI: immutable DTO for a single time-signature change; keep init-only props to allow safe sharing.
    public sealed class TimingEvent
    {
        // AI: StartBar: 1-based bar index where this time signature becomes active. Caller must ensure >=1.
        public int StartBar { get; init; }

        // AI: Numerator: beats per bar. Domain expects positive integers; caller-side validation recommended.
        public int Numerator { get; init; }

        // AI: Denominator: note value representing one beat (commonly 2,4,8). Zero is invalid; caller must ensure correctness.
        public int Denominator { get; init; }
    }
}
