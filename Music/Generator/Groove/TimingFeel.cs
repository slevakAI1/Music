namespace Music.Generator
{
    // AI: purpose=High-level pocket feel per role; biases micro-timing deterministically (ahead/on/behind grid).
    // AI: invariants=Used by GrooveTimingPolicy to apply tick bias; Ahead=negative offset, Behind=positive offset.
    public enum TimingFeel
    {
        Ahead,
        OnTop,
        Behind,
        LaidBack
    }
}
