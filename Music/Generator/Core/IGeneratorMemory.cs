// AI: purpose=Agent memory interface for anti-repetition; records decisions and exposes recent usage and fills
// AI: invariants=Implementations must be deterministic; bar numbers are 1-based; methods must be stable over time
// AI: deps=Uses FillShape and MusicConstants.eSectionType; changing surface affects consumers and tests
namespace Music.Generator.Core
{
    // AI: contract=Provides lightweight memory ops used by operators to avoid repetition; keep method signatures stable
    public interface IGeneratorMemory
    {
        // AI: record=Record operator decision at 1-based bar; used for repetition penalties and diagnostics
        void RecordDecision(int barNumber, string operatorId, string candidateId);

        // AI: query=Return sorted operator usage counts over last N bars; deterministic ordering preferred
        IReadOnlyDictionary<string, int> GetRecentOperatorUsage(int lastNBars);

        // AI: query=Return last recorded FillShape or null; used to vary/avoid repeating fills
        FillShape? GetLastFillShape();

        // AI: query=Return deterministic list of operator IDs that form the section signature
        IReadOnlyList<string> GetSectionSignature(MusicConstants.eSectionType sectionType);

        // AI: record=Store FillShape for later queries; small immutable record preferred
        void RecordFillShape(FillShape fillShape);

        // AI: record=Add operatorId to section signature; idempotent and deterministic
        void RecordSectionSignature(MusicConstants.eSectionType sectionType, string operatorId);

        // AI: operation=Clear all recorded state; used at start of new generation pass
        void Clear();

        // AI: prop=Most recent bar number recorded; 0 indicates no records
        int CurrentBarNumber { get; }
    }
}
