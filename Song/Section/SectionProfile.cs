// AI: purpose=Lightweight arrangement hints per section type for voice-leading and density control.
// AI: invariants=RegisterLift in semitones (±12 = ±1 octave); MaxDensity is note count; ColorToneProbability is 0.0-1.0.
// AI: deps=Used by VoiceLeadingSelector and PitchRandomizer; changing defaults affects musical output character.

namespace Music.Generator
{
    /// <summary>
    /// Arrangement profile for a section type (verse, chorus, etc.) controlling register, density, and color.
    /// </summary>
    public sealed class SectionProfile
    {
        // AI: RegisterLift: semitone shift for voicing register (e.g., +12 for chorus lift, 0 for verse).
        public int RegisterLift { get; init; }

        // AI: MaxDensity: maximum simultaneous notes (3-5 typical for keys/pads).
        public int MaxDensity { get; init; }

        // AI: ColorToneProbability: 0.0-1.0 probability of adding color tones (add9, etc.).
        public double ColorToneProbability { get; init; }

        // AI: Default profiles per section type; keep musical defaults aligned with common arrangement practice.
        public static SectionProfile GetForSectionType(MusicConstants.eSectionType sectionType)
        {
            return sectionType switch
            {
                MusicConstants.eSectionType.Verse => new SectionProfile
                {
                    RegisterLift = 0,           // Normal register
                    MaxDensity = 3,             // Sparse (triads)
                    ColorToneProbability = 0.2  // Low color tone usage
                },

                MusicConstants.eSectionType.Chorus => new SectionProfile
                {
                    RegisterLift = 12,          // Lift one octave
                    MaxDensity = 5,             // Rich (7ths + extensions)
                    ColorToneProbability = 0.6  // High color tone usage
                },

                MusicConstants.eSectionType.Bridge => new SectionProfile
                {
                    RegisterLift = 0,           // Normal register
                    MaxDensity = 4,             // Medium density
                    ColorToneProbability = 0.4  // Medium color tone usage
                },

                MusicConstants.eSectionType.Intro => new SectionProfile
                {
                    RegisterLift = 0,           // Normal register
                    MaxDensity = 3,             // Sparse
                    ColorToneProbability = 0.3  // Low-medium color
                },

                MusicConstants.eSectionType.Outro => new SectionProfile
                {
                    RegisterLift = 0,           // Normal register
                    MaxDensity = 3,             // Sparse
                    ColorToneProbability = 0.2  // Low color
                },

                MusicConstants.eSectionType.Solo => new SectionProfile
                {
                    RegisterLift = 0,           // Normal register (leave space for lead)
                    MaxDensity = 3,             // Sparse backing
                    ColorToneProbability = 0.5  // Colorful backing
                },

                MusicConstants.eSectionType.Custom or _ => new SectionProfile
                {
                    RegisterLift = 0,           // Default: normal
                    MaxDensity = 4,             // Default: medium
                    ColorToneProbability = 0.4  // Default: medium
                }
            };
        }
    }
}
