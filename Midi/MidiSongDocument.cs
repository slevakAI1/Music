using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace Music.MyMidi
{
    /// <summary>
    /// Represents a loaded MIDI song, with access to tracks, notes, chords, and metadata.
    /// Intended for future MusicXML, device playback, and DAW round-trip.
    /// </summary>
    public class MidiSongDocument
    {
        public MidiFile Raw { get; }
        public TempoMap Tempo { get; }
        public IEnumerable<TrackChunk> Tracks { get; }
        public System.TimeSpan Duration { get; }
        public int TrackCount { get; }
        public int EventCount { get; }
        public string? FileName { get; set; }

        public MidiSongDocument(MidiFile raw)
        {
            Raw = raw;
            Tempo = raw.GetTempoMap();
            Tracks = raw.GetTrackChunks().ToList();

            // Calculate duration using the last timed event
            var lastTimedEvent = Raw.GetTimedEvents().LastOrDefault();
            if (lastTimedEvent != null)
            {
                var metric = TimeConverter.ConvertTo<MetricTimeSpan>(lastTimedEvent.Time, Tempo);
                Duration = new System.TimeSpan(0, metric.Hours, metric.Minutes, metric.Seconds, metric.Milliseconds);
            }
            else
            {
                Duration = System.TimeSpan.Zero;
            }

            TrackCount = Tracks.Count();
            EventCount = Tracks.Sum(c => c.Events.Count);
        }
    }
}