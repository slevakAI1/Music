using Melanchall.DryWetMidi.Core;
using Music.MyMidi;
using Music.Tests;

namespace Music.Writer
{
    /// <summary>
    /// Converts a MidiSongDocument to lists of high-level MidiEvent objects.
    /// Each track in the MIDI file produces one List&lt;MidiEvent&gt;.
    /// Handles conversion of DryWetMidi events to our domain MidiEvent format.
    /// </summary>
    public static class ConvertMidiSongDocumentToMidiEventLists
    {
        /// <summary>
        /// Converts all tracks in a MIDI document to lists of MidiEvent objects.
        /// </summary>
        /// <param name="midiDoc">The MIDI document to convert</param>
        /// <returns>List of MidiEvent lists, one per track</returns>
        /// <exception cref="InvalidOperationException">Thrown when an unsupported MIDI event is encountered</exception>
        public static List<List<MidiEvent>> Convert(MidiSongDocument midiDoc)
        {
            if (midiDoc == null)
                throw new ArgumentNullException(nameof(midiDoc));

            var result = new List<List<MidiEvent>>();
            int trackIndex = 0;

            foreach (var trackChunk in midiDoc.Tracks)
            {
                try
                {
                    var trackEvents = ConvertTrack(trackChunk, trackIndex);
                    result.Add(trackEvents);
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
        /// Converts a single track chunk to a list of MidiEvent objects.
        /// </summary>
        private static List<MidiEvent> ConvertTrack(TrackChunk trackChunk, int trackIndex)
        {
            var events = new List<MidiEvent>();
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
        /// Converts a single DryWetMidi event to our domain MidiEvent format.
        /// </summary>
        private static MidiEvent? ConvertEvent(Melanchall.DryWetMidi.Core.MidiEvent dryWetEvent, long absoluteTime)
        {
            return dryWetEvent switch
            {
                // ============================================================
                // Meta Events
                // ============================================================

                SequenceNumberEvent sequenceNumberEvent => 
                    MidiEvent.CreateSequenceNumber(absoluteTime, sequenceNumberEvent.Number),

                TextEvent textEvent => 
                    MidiEvent.CreateText(absoluteTime, textEvent.Text),

                CopyrightNoticeEvent copyrightEvent => 
                    MidiEvent.CreateCopyrightNotice(absoluteTime, copyrightEvent.Text),

                SequenceTrackNameEvent trackNameEvent => 
                    MidiEvent.CreateSequenceTrackName(absoluteTime, trackNameEvent.Text),

                InstrumentNameEvent instrumentEvent => 
                    MidiEvent.CreateInstrumentName(absoluteTime, instrumentEvent.Text),

                LyricEvent lyricEvent => 
                    MidiEvent.CreateLyric(absoluteTime, lyricEvent.Text),

                MarkerEvent markerEvent => 
                    MidiEvent.CreateMarker(absoluteTime, markerEvent.Text),

                CuePointEvent cuePointEvent => 
                    MidiEvent.CreateCuePoint(absoluteTime, cuePointEvent.Text),

                ProgramNameEvent programNameEvent => 
                    MidiEvent.CreateProgramName(absoluteTime, programNameEvent.Text),

                DeviceNameEvent deviceNameEvent => 
                    MidiEvent.CreateDeviceName(absoluteTime, deviceNameEvent.Text),

                ChannelPrefixEvent channelPrefixEvent => 
                    MidiEvent.CreateMidiChannelPrefix(absoluteTime, channelPrefixEvent.Channel),

                PortPrefixEvent portPrefixEvent => 
                    MidiEvent.CreateMidiPort(absoluteTime, portPrefixEvent.Port),

                EndOfTrackEvent => 
                    MidiEvent.CreateEndOfTrack(absoluteTime),

                SetTempoEvent tempoEvent => 
                    MidiEvent.CreateSetTempo(
                        absoluteTime, 
                        microsecondsPerQuarterNote: (int)tempoEvent.MicrosecondsPerQuarterNote),

                SmpteOffsetEvent smpteEvent => 
                    MidiEvent.CreateSmpteOffset(
                        absoluteTime,
                        (int)smpteEvent.Format,
                        smpteEvent.Hours,
                        smpteEvent.Minutes,
                        smpteEvent.Seconds,
                        smpteEvent.Frames,
                        smpteEvent.SubFrames),

                TimeSignatureEvent timeSignatureEvent => 
                    MidiEvent.CreateTimeSignature(
                        absoluteTime,
                        timeSignatureEvent.Numerator,
                        1 << timeSignatureEvent.Denominator,
                        timeSignatureEvent.ClocksPerClick,
                        timeSignatureEvent.ThirtySecondNotesPerBeat),

                KeySignatureEvent keySignatureEvent => 
                    MidiEvent.CreateKeySignature(
                        absoluteTime,
                        keySignatureEvent.Key,
                        keySignatureEvent.Scale),

                SequencerSpecificEvent sequencerEvent => 
                    MidiEvent.CreateSequencerSpecific(absoluteTime, sequencerEvent.Data),

                UnknownMetaEvent unknownMetaEvent => 
                    MidiEvent.CreateUnknownMeta(
                        absoluteTime, 
                        unknownMetaEvent.StatusByte, 
                        unknownMetaEvent.Data),

                // ============================================================
                // Channel Voice Messages
                // ============================================================

                NoteOffEvent noteOffEvent => 
                    MidiEvent.CreateNoteOff(
                        absoluteTime,
                        (int)noteOffEvent.Channel,
                        noteOffEvent.NoteNumber,
                        noteOffEvent.Velocity),

                NoteOnEvent noteOnEvent => 
                    MidiEvent.CreateNoteOn(
                        absoluteTime,
                        (int)noteOnEvent.Channel,
                        noteOnEvent.NoteNumber,
                        noteOnEvent.Velocity),

                NoteAftertouchEvent aftertouchEvent => 
                    MidiEvent.CreatePolyKeyPressure(
                        absoluteTime,
                        (int)aftertouchEvent.Channel,
                        aftertouchEvent.NoteNumber,
                        aftertouchEvent.AftertouchValue),

                ControlChangeEvent controlChangeEvent => 
                    MidiEvent.CreateControlChange(
                        absoluteTime,
                        (int)controlChangeEvent.Channel,
                        controlChangeEvent.ControlNumber,
                        controlChangeEvent.ControlValue),

                ProgramChangeEvent programChangeEvent => 
                    MidiEvent.CreateProgramChange(
                        absoluteTime,
                        (int)programChangeEvent.Channel,
                        programChangeEvent.ProgramNumber),

                ChannelAftertouchEvent channelAftertouchEvent => 
                    MidiEvent.CreateChannelPressure(
                        absoluteTime,
                        (int)channelAftertouchEvent.Channel,
                        channelAftertouchEvent.AftertouchValue),

                PitchBendEvent pitchBendEvent => 
                    MidiEvent.CreatePitchBend(
                        absoluteTime,
                        (int)pitchBendEvent.Channel,
                        pitchBendEvent.PitchValue - 8192), // Convert from unsigned to signed

                // ============================================================
                // System Exclusive Events
                // ============================================================

                NormalSysExEvent normalSysExEvent => 
                    MidiEvent.CreateNormalSysEx(absoluteTime, normalSysExEvent.Data),

                EscapeSysExEvent escapeSysExEvent => 
                    MidiEvent.CreateEscapeSysEx(absoluteTime, escapeSysExEvent.Data),

                // ============================================================
                // System Common Messages
                // ============================================================

                MidiTimeCodeEvent mtcEvent => 
                    MidiEvent.CreateMtcQuarterFrame(
                        absoluteTime, 
                        (byte)mtcEvent.Component, 
                        mtcEvent.ComponentValue),

                SongPositionPointerEvent songPositionEvent => 
                    MidiEvent.CreateSongPositionPointer(absoluteTime, songPositionEvent.PointerValue),

                SongSelectEvent songSelectEvent => 
                    MidiEvent.CreateSongSelect(absoluteTime, songSelectEvent.Number),

                TuneRequestEvent => 
                    MidiEvent.CreateTuneRequest(absoluteTime),

                // ============================================================
                // System Real-Time Messages
                // ============================================================

                TimingClockEvent => 
                    MidiEvent.CreateTimingClock(absoluteTime),

                StartEvent => 
                    MidiEvent.CreateStart(absoluteTime),

                ContinueEvent => 
                    MidiEvent.CreateContinue(absoluteTime),

                StopEvent => 
                    MidiEvent.CreateStop(absoluteTime),

                ActiveSensingEvent => 
                    MidiEvent.CreateActiveSensing(absoluteTime),

                ResetEvent => 
                    MidiEvent.CreateSystemReset(absoluteTime),

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