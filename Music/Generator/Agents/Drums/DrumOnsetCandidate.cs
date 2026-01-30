using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums;

// AI: purpose=Drum-specific candidate onset for variation; may be added or used as replacement.
// AI: invariants=OnsetBeat in 1-based quarter-note units; MaxAddsPerBar caps this candidate; ProbabilityBias in [0..1].
// AI: change=Conversion methods added to interop with generic Groove.OnsetCandidate.
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

    // AI: Converts from generic OnsetCandidate for drum processing.
    public static DrumOnsetCandidate FromOnsetCandidate(OnsetCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return new DrumOnsetCandidate
        {
            Role = candidate.Role,
            OnsetBeat = candidate.OnsetBeat,
            Strength = candidate.Strength,
            MaxAddsPerBar = candidate.MaxAddsPerBar,
            ProbabilityBias = candidate.ProbabilityBias,
            Tags = [.. candidate.Tags],
            VelocityHint = candidate.VelocityHint,
            TimingHint = candidate.TimingHint
        };
    }

    // AI: Converts to generic OnsetCandidate for groove system.
    public OnsetCandidate ToOnsetCandidate()
    {
        return new OnsetCandidate
        {
            Role = Role,
            OnsetBeat = OnsetBeat,
            Strength = Strength,
            MaxAddsPerBar = MaxAddsPerBar,
            ProbabilityBias = ProbabilityBias,
            Tags = [.. Tags],
            VelocityHint = VelocityHint,
            TimingHint = TimingHint
        };
    }
}
