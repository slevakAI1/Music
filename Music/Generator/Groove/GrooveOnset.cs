// AI: purpose=Define groove onset with all metadata required for complete groove pipeline from Story A1.
// AI: invariants=Beat is 1-based within bar; BarNumber is 1-based; Velocity 1-127 when set (hint only, not final); Role is instrument-agnostic string.
// AI: deps=OnsetStrength from Groove.cs for strength classification; GrooveOnsetProvenance for provenance tracking (Story G2).
// AI: change=clarified Velocity is operator hint only; final velocity determined by Part Generator (DrummerVelocityShaper).
// AI: change=updated to use GrooveOnsetProvenance instead of MaterialProvenance for groove-specific tracking.

namespace Music.Generator.Groove
{
    /// <summary>
    /// Represents a single onset (note event) in the groove system.
    /// Stable groove output type for instrument-agnostic generation.
    /// Contains all metadata required for the complete groove pipeline:
    /// - Position (bar/beat)
    /// - Musical intent (strength, velocity, timing)
    /// - Origin tracking (provenance)
    /// - Protection rules
    /// </summary>
    public sealed record GrooveOnset
    {
        /// <summary>
        /// Role name (e.g., "Kick", "Snare", "Lead", "Bass"). Instrument-agnostic.
        /// </summary>
        public required string Role { get; init; }

        /// <summary>
        /// Bar number (1-based) in the song.
        /// </summary>
        public required int BarNumber { get; init; }

        /// <summary>
        /// Beat position (1-based) within the bar. Can be fractional (e.g., 1.5 for eighth offbeat).
        /// </summary>
        public required decimal Beat { get; init; }

        /// <summary>
        /// Onset strength classification (Downbeat, Backbeat, Strong, Offbeat, Pickup, Ghost).
        /// Nullable: will be computed by strength classifier in Story D1 if not provided.
        /// </summary>
        public OnsetStrength? Strength { get; init; }

        /// <summary>
        /// MIDI velocity hint (1-127) from operator or shaper.
        /// Nullable: operator provides initial hint; DrummerVelocityShaper refines it.
        /// NOT the final MIDI velocity — final determination is Part Generator's responsibility.
        /// Story 5.1: Clarified this is a hint for velocity shaping, not authoritative output.
        /// </summary>
        public int? Velocity { get; init; }

        /// <summary>
        /// Timing offset in ticks (can be positive or negative).
        /// Nullable: will be computed by timing adjustment in Stories E1-E2 if not provided.
        /// </summary>
        public int? TimingOffsetTicks { get; init; }

        /// <summary>
        /// MIDI note number [0..127] set by pitched instrument operators (e.g., bass).
        /// Nullable: drum onsets derive note from role mapping instead.
        /// </summary>
        public int? MidiNote { get; init; }

        /// <summary>
        /// Note duration in ticks set by pitched instrument operators.
        /// Nullable: when null, converter uses a default duration.
        /// </summary>
        public int? DurationTicks { get; init; }

        /// <summary>
        /// Origin tracking: where this onset came from (anchor, variation, etc.).
        /// Nullable: populated by Story G2 provenance system if diagnostics enabled.
        /// </summary>
        public GrooveOnsetProvenance? Provenance { get; init; }

        /// <summary>
        /// Must-hit onset: always included in final output, never removed by constraints.
        /// Set by protection system in Story 9.
        /// </summary>
        public bool IsMustHit { get; init; }

        /// <summary>
        /// Never-remove onset: protected from removal during pruning/constraint enforcement.
        /// Set by protection system in Story 9.
        /// </summary>
        public bool IsNeverRemove { get; init; }

        /// <summary>
        /// Protected onset: discouraged but not forbidden to remove during pruning.
        /// Set by protection system in Story 9.
        /// </summary>
        public bool IsProtected { get; init; }
    }
}
