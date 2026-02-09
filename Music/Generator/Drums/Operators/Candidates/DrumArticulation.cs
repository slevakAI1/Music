// AI: purpose=Enum of drum articulation hints used by operators to suggest playing technique.
// AI: invariants=Values are stable identifiers; None is default; mapper translates to MIDI note variants.
// AI: deps=Used by DrumCandidate and DrumArticulationMapper; extend cautiously to preserve backwards compatibility.

namespace Music.Generator.Drums.Operators.Candidates
{
    // Articulation hint for drum hits. Mapper converts to MIDI note/timbre variants.
    public enum DrumArticulation
    {
        // No specific articulation; use standard hit for the role
        None = 0,

        // Rimshot: head+rim strike for cutting snare sound
        Rimshot = 1,

        // SideStick (cross stick): quiet woody click on snare
        SideStick = 2,

        // Open hi-hat: sustained/open cymbal sound for accents
        OpenHat = 3,

        // Crash cymbal standard strike
        Crash = 4,

        // Ride cymbal bow hit (standard timekeeping)
        Ride = 5,

        // Ride bell hit for a bright cutting accent
        RideBell = 6,

        // Crash choke: muted crash for short punchy accent
        CrashChoke = 7,

        // Flam: grace note before main hit to thicken attack
        Flam = 8
    }
}
