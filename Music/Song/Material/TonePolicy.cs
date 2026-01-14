// AI: purpose=Harmony tone selection policy for motif rendering; guides chord-tone vs passing-tone decisions
// AI: invariants=ChordToneBias [0..1] where 1=always chord tones, 0=free selection
// AI: change=Stage 9 renderer uses bias to weight chord-tone selection on strong beats; passing tones allowed on weak beats when flag true
namespace Music.Song.Material;

public readonly record struct TonePolicy(
    double ChordToneBias,
    bool AllowPassingTones)
{
    public static TonePolicy Create(double chordToneBias, bool allowPassingTones) =>
        new(
            Math.Clamp(chordToneBias, 0.0, 1.0),
            allowPassingTones);
}
