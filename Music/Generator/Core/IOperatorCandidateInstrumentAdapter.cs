// AI: purpose=Provide instrument-specific discriminator/metadata for operator candidates without core knowing instrument types
// AI: invariants=Implementations must be deterministic for same instrumentData
// AI: deps=Used by OperatorBase.CreateCandidate to fill CandidateId discriminator and Metadata

namespace Music.Generator.Core
{
    // AI: contract=Adapter from instrument data to core candidate metadata and discriminator
    public interface IOperatorCandidateInstrumentAdapter
    {
        string? GetDiscriminator(object? instrumentData);
        Dictionary<string, object>? BuildMetadata(object? instrumentData);
    }

    // AI: purpose=No-op adapter; returns null discriminator and metadata
    internal sealed class NullOperatorCandidateInstrumentAdapter : IOperatorCandidateInstrumentAdapter
    {
        public static NullOperatorCandidateInstrumentAdapter Instance { get; } = new();

        public string? GetDiscriminator(object? instrumentData) => null;

        public Dictionary<string, object>? BuildMetadata(object? instrumentData) => null;
    }
}
