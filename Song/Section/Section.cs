// AI: purpose=Model a song section used by arrangers and editors to build song structure (start bar + bar count).
// AI: invariants=Sections are ordered; first section starts at bar 1; sections should start immediately after previous section.
// AI: deps=Consumed by song-building, UI and persistence; renaming props or changing semantics breaks serializers and editors.
// AI: constraints=StartBar is 1-based >=1; BarCount is whole bars only; Name may be null; SectionType must map to MusicConstants.eSectionType.

namespace Music.Generator
{
    // AI: data-only DTO; contains minimal metadata; avoid adding logic here (editors/serializers expect plain properties).
    public class Section
    {
        /// <summary>
        /// Optional: provides a stable unique identifier for this section.
        /// It is assumed to be unique within the song.
        /// </summary>
        public int SectionId { get; set; }

        // AI: SectionType: enum value used for defaults/labels; keep enum names stable for UI mapping.
        public MusicConstants.eSectionType SectionType { get; set; }

        // AI: Name: optional human label for the section; may be null or empty; used in UI only.
        public string? Name { get; set; }

        // AI: StartBar: 1-based bar index where this section begins; validators expect >=1 and contiguous ordering.
        public int StartBar { get; set; }

        // AI: BarCount: whole bars only; default 4; changing to fractional bars requires updating all layout logic.
        public int BarCount { get; set; } = 4;
    }
}
