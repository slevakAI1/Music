// AI: purpose=convert DryWetMidi MidiSongDocument -> Generator.PartTrack list; single-pass, preserves absolute times
// AI: invariants=event order and absoluteTime must match input; delta accumulation must remain; pitchbend converted signed by -8192
// AI: deps=Melanchall.DryWetMidi.Core; Generator.PartTrack, PartTrackEvent factory methods; consumers expect meta events intact
// AI: errors=unsupported events throw NotSupportedException with track/time debug info; Convert wraps track errors as InvalidOperationException
// AI: perf=hotpath O(n) over events; avoid heavy allocations; GetEventDebugInfo uses limited reflection for error context
// AI: Format0=SingleTrack MIDI files have all channels interleaved in one TrackChunk; SplitFormat0ByChannel separates them
using Melanchall.DryWetMidi.Core;
using Music.MyMidi;

namespace Music.Writer
{
    // AI: class:stateless converter; input=midiDoc.Tracks; output=list with one PartTrack per TrackChunk (or per channel for Format 0)
    public static class ConvertMidiSongDocumentToPartTracks_For_Import_Only
    {
        // AI: Channel voice event types that carry a Channel parameter; used to split Format 0 tracks by channel
        private static readonly HashSet<Generator.PartTrackEventType> ChannelEventTypes =
        [
            Generator.PartTrackEventType.NoteOn,
            Generator.PartTrackEventType.NoteOff,
            Generator.PartTrackEventType.PolyKeyPressure,
            Generator.PartTrackEventType.ControlChange,
            Generator.PartTrackEventType.ProgramChange,
            Generator.PartTrackEventType.ChannelPressure,
            Generator.PartTrackEventType.PitchBend,
        ];

        // AI: Convert: null-checks input; wraps per-track exceptions into InvalidOperationException with track index
        // AI: Format0 (SingleTrack) files are split by channel into separate PartTracks so downstream processing works per-instrument
        public static List<Generator.PartTrack> Convert(MidiSongDocument midiDoc)
        {
            if (midiDoc == null)
                throw new ArgumentNullException(nameof(midiDoc));

            bool isFormat0 = midiDoc.Raw.OriginalFormat == MidiFileFormat.SingleTrack;

            var result = new List<Generator.PartTrack>();
            int trackIndex = 0;

            foreach (var trackChunk in midiDoc.Tracks)
            {
                try
                {
                    var trackEvents = ConvertTrack(trackChunk, trackIndex);

                    if (isFormat0)
                    {
                        var splitTracks = SplitFormat0ByChannel(trackEvents);
                        result.AddRange(splitTracks);
                    }
                    else
                    {
                        var partTrack = new Generator.PartTrack(trackEvents);
                        result.Add(partTrack);
                    }

                    trackIndex++;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error converting track {trackIndex}: {ex.Message}", 
                        ex);
                }
            }

            return result;
        }

        // AI: SplitFormat0ByChannel: separates a single interleaved event list into per-channel PartTracks
        // AI: Meta events (tempo, time sig, etc.) go into a dedicated track so ExtractTempoAndTimingFromPartTracks finds them
        // AI: Each channel track gets its own PartTrack with only that channel's events
        private static List<Generator.PartTrack> SplitFormat0ByChannel(List<PartTrackEvent> allEvents)
        {
            var metaEvents = new List<PartTrackEvent>();
            var channelBuckets = new SortedDictionary<int, List<PartTrackEvent>>();

            foreach (var evt in allEvents)
            {
                if (ChannelEventTypes.Contains(evt.Type) &&
                    evt.Parameters.TryGetValue("Channel", out var chObj))
                {
                    int channel = System.Convert.ToInt32(chObj);
                    if (!channelBuckets.TryGetValue(channel, out var bucket))
                    {
                        bucket = new List<PartTrackEvent>();
                        channelBuckets[channel] = bucket;
                    }
                    bucket.Add(evt);
                }
                else
                {
                    metaEvents.Add(evt);
                }
            }

            var result = new List<Generator.PartTrack>();

            // Meta track first (tempo, time signature, etc.)
            if (metaEvents.Count > 0)
            {
                result.Add(new Generator.PartTrack(metaEvents));
            }

            // One PartTrack per channel
            foreach (var kvp in channelBuckets)
            {
                result.Add(new Generator.PartTrack(kvp.Value));
            }

            return result;
        }

        // AI: ConvertTrack: accumulates absoluteTime from DeltaTime; may skip events if ConvertEvent returns null
        // AI: throws NotSupportedException with added context for unsupported events (do not swallow)
        private static List<PartTrackEvent> ConvertTrack(TrackChunk trackChunk, int trackIndex)
        {
            var events = new List<PartTrackEvent>();
            long absoluteTime = 0;

            foreach (var midiEvent in trackChunk.Events)
            {
                absoluteTime += midiEvent.DeltaTime;

                try
                {
                    var convertedEvent = ConvertEvent(midiEvent, absoluteTime);
                    if (convertedEvent != null)
                    {
                        events.Add(convertedEvent);
                    }
                }
                catch (NotSupportedException ex)
                {
                    // Re-throw with additional context about which track and event
                    throw new NotSupportedException(
                        $"Unsupported MIDI event in track {trackIndex} at time {absoluteTime}:\n" +
                        $"Event Type: {midiEvent.GetType().Name}\n" +
                        $"Event Status: {midiEvent.EventType}\n" +
                        $"Raw Data: {GetEventDebugInfo(midiEvent)}\n\n" +
                        $"Please update the converter to handle this event type.\n\n" +
                        $"Original error: {ex.Message}",
                        ex);
                }
            }

            return events;
        }

        // AI: ConvertEvent: exhaustive pattern-match mapping to PartTrackEvent factory methods
        // AI: DON'T change mapping order or payload conversions (eg pitch bend: value-8192, tempo cast to int)
        private static PartTrackEvent? ConvertEvent(Melanchall.DryWetMidi.Core.MidiEvent dryWetEvent, long absoluteTime)
        {
            return dryWetEvent switch
            {
                // ============================================================
                // Meta Events
                // ============================================================

                SequenceNumberEvent sequenceNumberEvent => 
                    PartTrackEvent.CreateSequenceNumber(absoluteTime, sequenceNumberEvent.Number),

                TextEvent textEvent => 
                    PartTrackEvent.CreateText(absoluteTime, textEvent.Text),

                CopyrightNoticeEvent copyrightEvent => 
                    PartTrackEvent.CreateCopyrightNotice(absoluteTime, copyrightEvent.Text),

                SequenceTrackNameEvent trackNameEvent => 
                    PartTrackEvent.CreateSequenceTrackName(absoluteTime, trackNameEvent.Text),

                InstrumentNameEvent instrumentEvent => 
                    PartTrackEvent.CreateInstrumentName(absoluteTime, instrumentEvent.Text),

                LyricEvent lyricEvent => 
                    PartTrackEvent.CreateLyric(absoluteTime, lyricEvent.Text),

                MarkerEvent markerEvent => 
                    PartTrackEvent.CreateMarker(absoluteTime, markerEvent.Text),

                CuePointEvent cuePointEvent => 
                    PartTrackEvent.CreateCuePoint(absoluteTime, cuePointEvent.Text),

                ProgramNameEvent programNameEvent => 
                    PartTrackEvent.CreateProgramName(absoluteTime, programNameEvent.Text),

                DeviceNameEvent deviceNameEvent => 
                    PartTrackEvent.CreateDeviceName(absoluteTime, deviceNameEvent.Text),

                ChannelPrefixEvent channelPrefixEvent => 
                    PartTrackEvent.CreateMidiChannelPrefix(absoluteTime, channelPrefixEvent.Channel),

                PortPrefixEvent portPrefixEvent => 
                    PartTrackEvent.CreateMidiPort(absoluteTime, portPrefixEvent.Port),

                EndOfTrackEvent => 
                    PartTrackEvent.CreateEndOfTrack(absoluteTime),

                SetTempoEvent tempoEvent => 
                    PartTrackEvent.CreateSetTempo(
                        absoluteTime, 
                        microsecondsPerQuarterNote: (int)tempoEvent.MicrosecondsPerQuarterNote),

                SmpteOffsetEvent smpteEvent => 
                    PartTrackEvent.CreateSmpteOffset(
                        absoluteTime,
                        (int)smpteEvent.Format,
                        smpteEvent.Hours,
                        smpteEvent.Minutes,
                        smpteEvent.Seconds,
                        smpteEvent.Frames,
                        smpteEvent.SubFrames),

                TimeSignatureEvent timeSignatureEvent => 
                    PartTrackEvent.CreateTimeSignature(
                        absoluteTime,
                        timeSignatureEvent.Numerator,
                        timeSignatureEvent.Denominator,  // Store the exponent as-is, don't convert to note value
                        timeSignatureEvent.ClocksPerClick,
                        timeSignatureEvent.ThirtySecondNotesPerBeat),

                KeySignatureEvent keySignatureEvent => 
                    PartTrackEvent.CreateKeySignature(
                        absoluteTime,
                        keySignatureEvent.Key,
                        keySignatureEvent.Scale),

                SequencerSpecificEvent sequencerEvent => 
                    PartTrackEvent.CreateSequencerSpecific(absoluteTime, sequencerEvent.Data),

                UnknownMetaEvent unknownMetaEvent => 
                    PartTrackEvent.CreateUnknownMeta(
                        absoluteTime, 
                        unknownMetaEvent.StatusByte, 
                        unknownMetaEvent.Data),

                // ============================================================
                // Channel Voice Messages
                // ============================================================

                NoteOffEvent noteOffEvent => 
                    PartTrackEvent.CreateNoteOff(
                        absoluteTime,
                        (int)noteOffEvent.Channel,
                        noteOffEvent.NoteNumber,
                        noteOffEvent.Velocity),

                NoteOnEvent noteOnEvent => 
                    PartTrackEvent.CreateNoteOn(
                        absoluteTime,
                        (int)noteOnEvent.Channel,
                        noteOnEvent.NoteNumber,
                        noteOnEvent.Velocity),

                NoteAftertouchEvent aftertouchEvent => 
                    PartTrackEvent.CreatePolyKeyPressure(
                        absoluteTime,
                        (int)aftertouchEvent.Channel,
                        aftertouchEvent.NoteNumber,
                        aftertouchEvent.AftertouchValue),

                ControlChangeEvent controlChangeEvent => 
                    PartTrackEvent.CreateControlChange(
                        absoluteTime,
                        (int)controlChangeEvent.Channel,
                        controlChangeEvent.ControlNumber,
                        controlChangeEvent.ControlValue),

                ProgramChangeEvent programChangeEvent => 
                    PartTrackEvent.CreateProgramChange(
                        absoluteTime,
                        (int)programChangeEvent.Channel,
                        programChangeEvent.ProgramNumber),

                ChannelAftertouchEvent channelAftertouchEvent => 
                    PartTrackEvent.CreateChannelPressure(
                        absoluteTime,
                        (int)channelAftertouchEvent.Channel,
                        channelAftertouchEvent.AftertouchValue),

                PitchBendEvent pitchBendEvent => 
                    PartTrackEvent.CreatePitchBend(
                        absoluteTime,
                        (int)pitchBendEvent.Channel,
                        pitchBendEvent.PitchValue - 8192), // Convert from unsigned to signed

                // ============================================================
                // System Exclusive Events
                // ============================================================

                NormalSysExEvent normalSysExEvent => 
                    PartTrackEvent.CreateNormalSysEx(absoluteTime, normalSysExEvent.Data),

                EscapeSysExEvent escapeSysExEvent => 
                    PartTrackEvent.CreateEscapeSysEx(absoluteTime, escapeSysExEvent.Data),

                // ============================================================
                // System Common Messages
                // ============================================================

                MidiTimeCodeEvent mtcEvent => 
                    PartTrackEvent.CreateMtcQuarterFrame(
                        absoluteTime, 
                        (byte)mtcEvent.Component, 
                        mtcEvent.ComponentValue),

                SongPositionPointerEvent songPositionEvent => 
                    PartTrackEvent.CreateSongPositionPointer(absoluteTime, songPositionEvent.PointerValue),

                SongSelectEvent songSelectEvent => 
                    PartTrackEvent.CreateSongSelect(absoluteTime, songSelectEvent.Number),

                TuneRequestEvent => 
                    PartTrackEvent.CreateTuneRequest(absoluteTime),

                // ============================================================
                // System Real-Time Messages
                // ============================================================

                TimingClockEvent => 
                    PartTrackEvent.CreateTimingClock(absoluteTime),

                StartEvent => 
                    PartTrackEvent.CreateStart(absoluteTime),

                ContinueEvent => 
                    PartTrackEvent.CreateContinue(absoluteTime),

                StopEvent => 
                    PartTrackEvent.CreateStop(absoluteTime),

                ActiveSensingEvent => 
                    PartTrackEvent.CreateActiveSensing(absoluteTime),

                ResetEvent => 
                    PartTrackEvent.CreateSystemReset(absoluteTime),

                // ============================================================
                // Unknown/Unsupported
                // ============================================================

                _ => throw new NotSupportedException(
                    $"MIDI event type '{dryWetEvent.GetType().Name}' is not currently supported.")
            };
        }

        // AI: GetEventDebugInfo: limited reflection; returns up to 5 property=value pairs; swallow errors and return fallback
        private static string GetEventDebugInfo(Melanchall.DryWetMidi.Core.MidiEvent midiEvent)
        {
            try
            {
                // Try to get some useful information about the event
                var props = midiEvent.GetType().GetProperties()
                    .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                    .Take(5) // Limit to first 5 properties
                    .Select(p => 
                    {
                        try
                        {
                            var value = p.GetValue(midiEvent);
                            return $"{p.Name}={value}";
                        }
                        catch
                        {
                            return $"{p.Name}=(error reading value)";
                        }
                    });

                return string.Join(", ", props);
            }
            catch
            {
                return "(unable to extract debug info)";
            }
        }
    }
}
