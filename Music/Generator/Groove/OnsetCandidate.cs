namespace Music.Generator.Groove;

// AI: purpose=Instrument-agnostic candidate onset for groove variation; used by all instrument agents.
// AI: invariants=OnsetBeat in 1-based quarter-note units; MaxAddsPerBar caps additions; ProbabilityBias in [0..1].
// AI: change=Created from DrumOnsetCandidate to decouple groove from drum-specific types.
public sealed class OnsetCandidate
{
    public string Role { get; set; } = "";
    public decimal OnsetBeat { get; set; }
    public OnsetStrength Strength { get; set; }
    public int MaxAddsPerBar { get; set; }
    public double ProbabilityBias { get; set; }
    public List<string> Tags { get; set; } = [];

    // AI: Optional velocity hint from instrument agent (1-127); flows to GrooveOnset.Velocity.
    public int? VelocityHint { get; set; }

    // AI: Optional timing offset hint from instrument agent (ticks); flows to GrooveOnset.TimingOffsetTicks.
    public int? TimingHint { get; set; }
}
