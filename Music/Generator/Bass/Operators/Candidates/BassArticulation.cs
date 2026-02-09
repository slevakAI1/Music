// AI: purpose=Enum of Bass articulation hints used by operators to suggest playing technique.
// AI: invariants=Values are stable identifiers; None is default; mapper translates to MIDI note variants.
// AI: deps=Used by bassCandidate and bassArticulationMapper; extend cautiously to preserve backwards compatibility.

namespace Music.Generator.Bass.Operators.Candidates
{
    // Articulation hint for Bass hits. Mapper converts to MIDI note/timbre variants.
    public enum BassArticulation
    {
        // No specific articulation; use standard hit for the role
        None = 0,
    }
}
