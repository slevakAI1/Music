// AI: purpose=Enum for drum articulation hints; used by operators to specify desired playing technique.
// AI: invariants=Values are stable; None is default; mapped to MIDI note variations by DrumArticulationMapper (Story 6.3).
// AI: deps=Consumed by DrumCandidate; mapped by DrumArticulationMapper to MIDI notes.
// AI: change=Story 2.2; extend with additional articulations as needed (e.g., BrushSwirl, MalletHit).

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Articulation hint for drum hits, specifying the desired playing technique.
    /// Operators use this to suggest how a hit should sound; the articulation mapper
    /// translates these to appropriate MIDI note variations.
    /// Story 2.2: Define Drum Candidate Type.
    /// </summary>
    public enum DrumArticulation
    {
        /// <summary>
        /// No specific articulation; use standard hit for the role.
        /// </summary>
        None = 0,

        /// <summary>
        /// Rimshot: stick strikes both head and rim simultaneously.
        /// Produces a loud, cutting snare sound.
        /// </summary>
        Rimshot = 1,

        /// <summary>
        /// Side stick (cross stick): stick laid across head, striking rim.
        /// Produces a quieter, woody click sound.
        /// </summary>
        SideStick = 2,

        /// <summary>
        /// Open hi-hat: foot releases pedal allowing cymbals to ring.
        /// Used for accents and sustained hi-hat sounds.
        /// </summary>
        OpenHat = 3,

        /// <summary>
        /// Crash cymbal hit with standard strike.
        /// </summary>
        Crash = 4,

        /// <summary>
        /// Ride cymbal hit on the bow (standard ride sound).
        /// </summary>
        Ride = 5,

        /// <summary>
        /// Ride bell hit for cutting accent.
        /// </summary>
        RideBell = 6,

        /// <summary>
        /// Crash choke: crash hit immediately muted by hand.
        /// Produces a short, punchy accent.
        /// </summary>
        CrashChoke = 7,

        /// <summary>
        /// Flam: grace note immediately before main hit.
        /// Adds thickness and emphasis to snare hits.
        /// </summary>
        Flam = 8
    }
}
