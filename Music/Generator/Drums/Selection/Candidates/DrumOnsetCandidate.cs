using Music.Generator.Groove;

namespace Music.Generator.Drums.Selection.Candidates;

// AI: purpose=DrumOnsetCandidate: drum-specific onset data used by selection and groove layers.
// AI: invariants=OnsetBeat is 1-based quarter-note units; MaxAddsPerBar caps adds; ProbabilityBias in [0,1].
// AI: deps=Uses OnsetStrength from Music.Generator.Groove; Tags used for traceability and selection hints.
public sealed class DrumOnsetCandidate
{
    // AI: contract=Mutable DTO used by selection pipeline; keep property names stable when persisting or tagging
    public string Role { get; set; } = "";
    public decimal OnsetBeat { get; set; }
    public OnsetStrength Strength { get; set; }
    public int MaxAddsPerBar { get; set; }
    public double ProbabilityBias { get; set; }
    public List<string> Tags { get; set; } = [];

    // AI: Optional velocity hint (1-127); flows to GrooveOnset.Velocity.
    public int? VelocityHint { get; set; }

    // AI: Optional timing offset hint (ticks); flows to GrooveOnset.TimingOffsetTicks.
    public int? TimingHint { get; set; }
}
