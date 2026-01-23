// AI: purpose=Hardcoded test motifs for Stage 9 placement/rendering testing; provides real, usable motifs without needing a generator
// AI: invariants=All motifs deterministic (same call → same output); non-derivative (archetype-level patterns only); valid structure
// AI: deps=Uses MotifSpec, MaterialKind; ticks at 480 PPQN (MusicConstants.TicksPerQuarterNote)
// AI: change=Stage 11+ may add procedural motif generation; these are MVP test fixtures
namespace Music.Song.Material;

/// <summary>
/// Library of hardcoded test motifs for Stage 9 placement and rendering.
/// </summary>
public static class MotifLibrary
{
    // AI: Quarter note = 480 ticks (from )
    private const int Q = MusicConstants.TicksPerQuarterNote;
    private const int E = Q / 2;     // Eighth = 240
    private const int S = Q / 4;     // Sixteenth = 120
    private const int H = Q * 2;     // Half = 960
    private const int W = Q * 4;     // Whole = 1920

    /// <summary>
    /// Chorus hook: Syncopated "da-da DUM" pattern with arch contour (2 bars).
    /// Rhythmically memorable, suitable for Lead role in high-energy sections.
    /// </summary>
    public static MotifSpec ClassicRockHookA()
    {
        // AI: Rhythm: syncopated pattern across 2 bars: E E Q+E rest E E Q+E
        // AI: Bar 1: 0, 240, 480 (eighth, eighth, quarter+eighth)
        // AI: Bar 2: 1200, 1440, 1680 (eighth, eighth, quarter+eighth)
        var rhythm = new List<int>
        {
            0,      // Beat 1
            E,      // Eighth after beat 1
            Q,      // Beat 2 (accented)
            Q*3,    // Beat 4 of bar 2
            Q*3+E,  // Eighth after beat 4
            Q*4,    // Downbeat of bar 2 (strong)
        };

        return MotifSpec.Create(
            name: "Classic Rock Hook A",
            intendedRole: "Lead",
            kind: MaterialKind.Hook,
            rhythmShape: rhythm,
            contour: ContourIntent.Arch,
            centerMidiNote: 67, // G4
            rangeSemitones: 10,
            chordToneBias: 0.8,
            allowPassingTones: true,
            tags: new HashSet<string> { "hooky", "chorus-hook", "energetic" });
    }

    /// <summary>
    /// Verse riff: Steady, repeating pattern with flat/slight-up contour (1 bar).
    /// Suitable for Guitar or Bass role in mid-energy sections.
    /// </summary>
    public static MotifSpec SteadyVerseRiffA()
    {
        // AI: Rhythm: steady eighth note pattern with slight variation
        // AI: 0, 240, 480, 600, 720, 960, 1200, 1440 (eighth-based with one sixteenth pickup)
        var rhythm = new List<int>
        {
            0,          // Beat 1
            E,          // Beat 1.5
            Q,          // Beat 2
            Q+S,        // Sixteenth pickup before beat 3
            Q+E,        // Beat 2.5
            Q*2,        // Beat 3
            Q*3,        // Beat 4
            Q*3+E,      // Beat 4.5
        };

        return MotifSpec.Create(
            name: "Steady Verse Riff A",
            intendedRole: "Guitar",
            kind: MaterialKind.Riff,
            rhythmShape: rhythm,
            contour: ContourIntent.Flat,
            centerMidiNote: 55, // G3 (lower register for riff)
            rangeSemitones: 7,
            chordToneBias: 0.85,
            allowPassingTones: false, // Tight to chord tones for verse foundation
            tags: new HashSet<string> { "verse-riff", "steady", "foundation" });
    }

    /// <summary>
    /// Synth hook: Bright, energetic pattern with arch contour (1 bar).
    /// Suitable for Keys role in high-energy sections.
    /// </summary>
    public static MotifSpec BrightSynthHookA()
    {
        // AI: Rhythm: quick ascending pattern with syncopation
        // AI: 0, 120, 240, 480, 720, 960, 1200 (sixteenth-based with quarter accents)
        var rhythm = new List<int>
        {
            0,          // Beat 1
            S,          // Sixteenth after beat 1
            E,          // Beat 1.5
            Q,          // Beat 2
            Q+E,        // Beat 2.5
            Q*2,        // Beat 3
            Q*3,        // Beat 4
        };

        return MotifSpec.Create(
            name: "Bright Synth Hook A",
            intendedRole: "Keys",
            kind: MaterialKind.Hook,
            rhythmShape: rhythm,
            contour: ContourIntent.Up,
            centerMidiNote: 72, // C5 (bright upper register)
            rangeSemitones: 12,
            chordToneBias: 0.75,
            allowPassingTones: true,
            tags: new HashSet<string> { "bright", "synth-hook", "energetic", "ascending" });
    }

    /// <summary>
    /// Bass fill: Short, punchy pattern for section transitions (1 bar).
    /// </summary>
    public static MotifSpec BassTransitionFillA()
    {
        // AI: Rhythm: approach pattern with sixteenth note run at end
        // AI: 0, 480, 1200, 1320, 1440, 1560 (quarters then sixteenth run)
        var rhythm = new List<int>
        {
            0,          // Beat 1
            Q,          // Beat 2
            Q*3,        // Beat 4
            Q*3+S,      // 16th
            Q*3+E,      // 16th
            Q*3+S*3,    // 16th
        };

        return MotifSpec.Create(
            name: "Bass Transition Fill A",
            intendedRole: "Bass",
            kind: MaterialKind.BassFill,
            rhythmShape: rhythm,
            contour: ContourIntent.Up,
            centerMidiNote: 43, // G2 (bass register)
            rangeSemitones: 12,
            chordToneBias: 0.9,
            allowPassingTones: true,
            tags: new HashSet<string> { "bass-fill", "transition", "approach" });
    }

    /// <summary>
    /// Get all hardcoded test motifs.
    /// </summary>
    public static IReadOnlyList<MotifSpec> GetAllTestMotifs()
    {
        return
        [
            ClassicRockHookA(),
            SteadyVerseRiffA(),
            BrightSynthHookA(),
            BassTransitionFillA()
        ];
    }
}
