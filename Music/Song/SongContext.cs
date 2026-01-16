using MusicGen.Lyrics;
using Music.Song.Material;

namespace Music.Generator
{
    // AI: DTO used by editors and generation pipeline. Keep property names and default initializations stable.
    public sealed class SongContext
    {
        public SongContext()
        {
        }

        // AI: CurrentBar: 1-based index into the design; editors and generators rely on this convention.
        public int CurrentBar { get; set; } = 1;

        // AI: BarTrack pre-initialized to avoid null checks; contains measure tick mapping used during generation.
        public BarTrack BarTrack { get; set; } = new();


        //======================================================================================



        // AI: GrooveTrack pre-initialized; holds groove events applied during generation. Consumers must normalize lists.
        // THIS IS THE OLD GROOVE STRUCTURE
        public GrooveTrack GrooveTrack { get; set; } = new();

        // THIS IS THE NEW GROOVE STRUCTURE
        public GroovePresetDefinition GroovePresetDefinition { get; set; } = new();
        public IReadOnlyList<SegmentGrooveProfile> SegmentGrooveProfiles { get; set; } = Array.Empty<SegmentGrooveProfile>();



        //======================================================================================


        // AI: HarmonyTrack persisted with design; may be empty. Used to build HarmonyPitchContext for generators.
        public HarmonyTrack HarmonyTrack { get; set; } = new();

        // AI: SectionTrack contains ordered, contiguous song sections; builders rely on StartBar assignment logic there.
        public SectionTrack SectionTrack { get; set; } = new();

        // AI: LyricTrack: synchronized lyrics track with phonetic syllable data; used for vocal generation.
        public LyricTrack LyricTrack { get; set; } = new();

        // AI: Song: output runtime song populated by generator; contains tempo/time signature and rendered PartTracks.
        public Song Song { get; set; } = new();

        // AI: Voices: design-time voice set used to map roles to instruments; keep VoiceName stable for MIDI mapping.
        public VoiceSet Voices { get; set; } = new();

        // AI: MaterialBank: in-memory container for reusable material fragments (motifs, riffs, fills); Stage 8+9 motif system.
        public MaterialBank MaterialBank { get; set; } = new();
    }
}