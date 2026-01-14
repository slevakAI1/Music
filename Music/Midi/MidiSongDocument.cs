// AI: purpose=Loaded MIDI container exposing Raw MidiFile, tempo map, track chunks, duration and event counts for UI/export.
// AI: invariants=Raw != null; Tempo derived from Raw; Tracks == Raw.GetTrackChunks(); Duration computed from last timed event or zero.
// AI: deps=Relies on DryWetMidi types (MidiFile, TempoMap, TrackChunk) and TimeConverter.ConvertTo; changing Raw/TimedEvent semantics breaks consumers.
// AI: perf=Constructor performs parsing work (GetTempoMap/GetTrackChunks/GetTimedEvents); intended for load-time, not per-frame.

using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace Music.MyMidi
{
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

            // AI: Duration calc: use the last timed event's absolute time converted to MetricTimeSpan; zero if no timed events.
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