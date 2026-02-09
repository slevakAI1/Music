// AI: purpose=Interface for drum operators that remove onsets rather than add them.
// AI: invariants=GenerateRemovals returns targets to remove; applicator checks protection flags before removing.
// AI: deps=IDrumOperator, RemovalCandidate, DrummerContext; used by DrumOperatorApplicator removal path.

using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Candidates;

namespace Music.Generator.Drums.Operators
{
    // AI: purpose=Subtractive operator contract; GenerateRemovals identifies onsets to remove from current bar.
    // AI: note=GenerateCandidates defaults to empty; removal ops do not add onsets.
    public interface IDrumRemovalOperator : IDrumOperator
    {
        // AI: purpose=Produce removal targets for the given context bar. Applicator enforces protection flags.
        IEnumerable<RemovalCandidate> GenerateRemovals(DrummerContext context);
    }
}
