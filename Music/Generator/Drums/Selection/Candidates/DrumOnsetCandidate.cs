using Music.Generator.Groove;

namespace Music.Generator.Drums.Selection.Candidates;

// AI: purpose=Drum-specific candidate onset for variation; may be added or used as replacement.
// AI: invariants=OnsetBeat in 1-based quarter-note units; MaxAddsPerBar caps this candidate; ProbabilityBias in [0..1].
// AI: deps=Music.Generator.Groove for OnsetStrength enum.
// AI: change=Conversion methods removed (GC-4); drum candidates stand alone, no generic conversion needed.
public sealed class DrumOnsetCandidate
{
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
