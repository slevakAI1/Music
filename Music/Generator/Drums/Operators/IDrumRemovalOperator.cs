// AI: purpose=Interface for drum operators that remove onsets rather than add them.
// AI: invariants=GenerateRemovals returns targets to remove; applicator checks protection flags before removing.
// AI: deps=IDrumOperator, RemovalCandidate, Bar; used by DrumOperatorApplicator removal path.

using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators
{
    // AI: purpose=Subtractive operator contract; GenerateRemovals identifies onsets to remove from current bar.
    // AI: note=GenerateCandidates defaults to empty; removal ops do not add onsets.
    public interface IDrumRemovalOperator : IDrumOperator
    {
        // AI: purpose=Produce removal targets for the given bar. Applicator enforces protection flags.
        IEnumerable<RemovalCandidate> GenerateRemovals(Bar bar);
    }
}
