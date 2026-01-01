namespace Music.Generator
{
    // AI: runtime DTO: separate from design templates; producers populate tracks then exporters consume them.
     public sealed class Song
    {
        // AI: Global tempo track; should contain at least one TempoEvent starting at bar 1 for correct export.
        public TempoTrack TempoTrack { get; set; }

        // AI: Global time-signature track; events are 1-based bar-aligned and queried during export.
        public Timingtrack TimeSignatureTrack { get; set; }

        // AI: All rendered part/instrument tracks; ordering may affect multi-track MIDI channel assignment.
        public List<PartTrack> PartTracks { get; set; }

        // AI: TotalBars: total bars in the song; exporters rely on this for timeline length and track trimming.
        public int TotalBars { get; set; }

        public Song()
        {
            TempoTrack = new TempoTrack();
            TimeSignatureTrack = new Timingtrack();
            PartTracks = new List<PartTrack>();
        }
    }
}