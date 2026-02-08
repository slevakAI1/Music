namespace Music.Generator.Drums.Operators.Candidates
{
    // AI: purpose=Provides drum candidate groups for a bar+role to the selection pipeline
    // AI: invariants=Implementations must be deterministic; same inputs => same outputs; return empty list if none
    // AI: deps=Inputs: Bar context; output: list of DrumCandidateGroup used by selection engine
    public interface IDrumOperatorCandidates
    {
        // AI: contract=Return groups already merged/filtered; groups must be in deterministic order
        // AI: inputs=Bar contains section/profile/phrase info; role is role name like "Snare"/"Kick"
        IReadOnlyList<DrumCandidateGroup> GetCandidateGroups(
            Bar bar,
            string role);
    }
}
