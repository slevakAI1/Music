namespace Music.Generator.Groove
{
    // AI: purpose=One candidate onset event for variation; may be added or used as replacement during variation.
    // AI: invariants=OnsetBeat in 1-based quarter-note units; MaxAddsPerBar caps this candidate; ProbabilityBias in [0..1].
    // AI: change=Tags drive candidate selection (e.g., "Fill", "Pickup", "Drive"); Strength maps to VelocityRule bucket.
    public sealed class GrooveOnsetCandidate
    {
        public string Role { get; set; } = "";
        public decimal OnsetBeat { get; set; }
        public OnsetStrength Strength { get; set; }
        public int MaxAddsPerBar { get; set; }
        public double ProbabilityBias { get; set; }
        public List<string> Tags { get; set; } = new();
    }
}
