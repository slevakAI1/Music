using Music.Designer;
using Music.MyMidi;

namespace Music.Generator
{
    /// <summary>
    /// The complete generated song, containing all tracks and temporal data.
    /// This is the runtime representation of a composed piece, separate from design templates.
    /// </summary>
    /// 



    // TO DO - 
    //      I NEED (1) SONG TO GRID AND (2) GRID TO SONG CONVERTER METHODS
    //      Then can generate to the song object and send to the grid, and vica versa when the grid is updated.
    //          this may be tricky :-(

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


        // TO DO This needs to be a List of Tracks...make this List<MetaMidiEvent> a PartTrack!
        /// <summary>
        /// All part/instrument tracks in the song.
        /// </summary>
        public List<List<MetaMidiEvent>> PartTracks { get; set; }

        /// <summary>
        /// Total number of bars in the song.
        /// </summary>
        public int TotalBars { get; set; }

        public Song()
        {
            TempoTrack = new TempoTrack();
            TimeSignatureTrack = new TimeSignatureTrack();
            PartTracks = new List<List<MetaMidiEvent>>();
        }
    }
}