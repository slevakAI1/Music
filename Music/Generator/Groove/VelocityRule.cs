namespace Music.Generator.Groove
{
    // AI: purpose=Velocity bounds and target for one OnsetStrength bucket; defines min/max/typical velocity + accent bias.
    // AI: invariants=Min/Max/Typical in [1..127]; AccentBias additive (can be negative); Typical used before jitter/bias.
    public sealed class VelocityRule
    {
        public int Min { get; set; }
        public int Max { get; set; }
        public int Typical { get; set; }
        public int AccentBias { get; set; }
    }
}
