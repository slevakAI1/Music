using Melanchall.DryWetMidi.Core;
using Music.MyMidi;

namespace Music.Writer
{
    /// <summary>
    /// Converts a MidiSongDocument to PartTrack objects with MetaMidiEvent lists.
    /// Each track in the MIDI file produces one PartTrack.
    /// Handles conversion of DryWetMidi events to our domain MetaMidiEvent format.
    /// </summary>
    public static class ConvertMidiSongDocumentToPartTracks
    {
        /// <summary>
        /// Converts all tracks in a MIDI document to PartTrack objects with MetaMidiEvent lists.
        /// </summary>
        /// <param name="midiDoc">The MIDI document to convert</param>
        /// <returns>List of PartTrack objects, one per track</returns>
        /// <exception cref="InvalidOperationException">Thrown when an unsupported MIDI event is encountered</exception>
        public static List<Generator.PartTrack> Convert(MidiSongDocument midiDoc)
        {
            if (midiDoc == null)
                throw new ArgumentNullException(nameof(midiDoc));

            var result = new List<Generator.PartTrack>();
            int trackIndex = 0;

            foreach (var trackChunk in midiDoc.Tracks)
            {
                try
                {
                    var trackEvents = ConvertTrack(trackChunk, trackIndex);
                    var partTrack = new Generator.PartTrack(trackEvents);
                    result.Add(partTrack);
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

        /// <summary>
        /// Converts a single track chunk to a list of MetaMidiEvent objects.
        /// </summary>
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

        /// <summary>
        /// Converts a single DryWetMidi event to our domain MetaMidiEvent format.
        /// </summary>
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

        /// <summary>
        /// Gets debug information for an unsupported MIDI event.
        /// </summary>
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