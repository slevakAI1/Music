namespace Music.Generator.Groove
{
    // AI: purpose=One candidate onset event for variation; may be added or used as replacement during variation.
    // AI: invariants=OnsetBeat in 1-based quarter-note units; MaxAddsPerBar caps this candidate; ProbabilityBias in [0..1].
    // AI: change=VelocityHint/TimingHint flow directly from instrument agents to GrooveOnset.
    public sealed class GrooveOnsetCandidate
    {
        public string Role { get; set; } = "";
        public decimal OnsetBeat { get; set; }
        public OnsetStrength Strength { get; set; }
        public int MaxAddsPerBar { get; set; }
        public double ProbabilityBias { get; set; }
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Optional velocity hint from instrument agent (1-127).
        /// When set, flows directly to GrooveOnset.Velocity.
        /// </summary>
        public int? VelocityHint { get; set; }

        /// <summary>
        /// Optional timing offset hint from instrument agent (ticks).
        /// When set, flows directly to GrooveOnset.TimingOffsetTicks.
        /// </summary>
        public int? TimingHint { get; set; }
    }
}
