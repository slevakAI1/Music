using Music.Designer;
using Music.MyMidi;
using Music.Writer;

namespace Music.Generator
{
    /// <summary>
    /// The complete generated song, containing all tracks and temporal data.
    /// This is the runtime representation of a composed piece, separate from design templates.
    /// </summary>
     public sealed class Song
    {
        /// <summary>
        /// Global tempo track for the song.
        /// </summary>
        public TempoTrack TempoTrack { get; set; }

        /// <summary>
        /// Global time signature track for the song.
        /// </summary>
        public TimeSignatureTrack TimeSignatureTrack { get; set; }


        /// <summary>
        /// All part/instrument tracks in the song.
        /// </summary>
        public List<PartTrack> PartTracks { get; set; }

        /// <summary>
        /// Total number of bars in the song.
        /// </summary>
        public int TotalBars { get; set; }

        public Song()
        {
            TempoTrack = new TempoTrack();
            TimeSignatureTrack = new TimeSignatureTrack();
            PartTracks = new List<PartTrack>();
        }
    }
}