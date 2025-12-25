using Music.MyMidi;

namespace Music.Generator
{
    /// <summary>
    /// Represents a single part/track for composition and MIDI generation.
    /// 
    /// PartTrack encapsulates a sequence of musical events (notes, chords, rests) for a single instrument or part.
    /// It is designed to support flexible music writing, including overlapping notes and chords, and serves as the
    /// primary input for transformations into timed notes and MIDI events. This abstraction enables composers and
    /// algorithms to work with high-level musical ideas before rendering them into concrete playback or notation.
    /// </summary>
    public sealed class PartTrack
    {
        // THESE GET SET BY GRID DROPDOWN CHANGE EVENT, WHAT ABOUT DEFAULT?
        public string MidiProgramName { get; set; }
        //public string NotionPartName { get; set; }
        public int MidiProgramNumber { get; set; }

        public List<PartTrackEvent> PartTrackNoteEvents { get; set; } = new();

        public PartTrack(List<PartTrackEvent> songTrackNoteEvent)
        {
            PartTrackNoteEvents = songTrackNoteEvent;
        }
    }
}
