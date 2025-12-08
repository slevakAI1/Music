using Melanchall.DryWetMidi.Core;
using Music.MyMidi;
using Music.Tests;

namespace Music.Writer
{
    /// <summary>
    /// Converts a MidiSongDocument to lists of high-level MetaMidiEvent objects.
    /// Each track in the MIDI file produces one List&lt;MetaMidiEvent&gt;.
    /// Handles conversion of DryWetMidi events to our domain MetaMidiEvent format.
    /// </summary>
    public static class ConvertMidiSongDocumentToMidiEventLists
    {
        /// <summary>
        /// Converts all tracks in a MIDI document to lists of MetaMidiEvent objects.
        /// </summary>
        /// <param name="midiDoc">The MIDI document to convert</param>
        /// <returns>List of MetaMidiEvent lists, one per track</returns>
        /// <exception cref="InvalidOperationException">Thrown when an unsupported MIDI event is encountered</exception>
        public static List<List<MetaMidiEvent>> Convert(MidiSongDocument midiDoc)
        {
            if (midiDoc == null)
                throw new ArgumentNullException(nameof(midiDoc));

            var result = new List<List<MetaMidiEvent>>();
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
        /// Converts a single track chunk to a list of MetaMidiEvent objects.
        /// </summary>
        private static List<MetaMidiEvent> ConvertTrack(TrackChunk trackChunk, int trackIndex)
        {
            var events = new List<MetaMidiEvent>();
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
        private static MetaMidiEvent? ConvertEvent(Melanchall.DryWetMidi.Core.MidiEvent dryWetEvent, long absoluteTime)
        {
            return dryWetEvent switch
            {
                // ============================================================
                // Meta Events
                // ============================================================

                SequenceNumberEvent sequenceNumberEvent => 
                    MetaMidiEvent.CreateSequenceNumber(absoluteTime, sequenceNumberEvent.Number),

                TextEvent textEvent => 
                    MetaMidiEvent.CreateText(absoluteTime, textEvent.Text),

                CopyrightNoticeEvent copyrightEvent => 
                    MetaMidiEvent.CreateCopyrightNotice(absoluteTime, copyrightEvent.Text),

                SequenceTrackNameEvent trackNameEvent => 
                    MetaMidiEvent.CreateSequenceTrackName(absoluteTime, trackNameEvent.Text),

                InstrumentNameEvent instrumentEvent => 
                    MetaMidiEvent.CreateInstrumentName(absoluteTime, instrumentEvent.Text),

                LyricEvent lyricEvent => 
                    MetaMidiEvent.CreateLyric(absoluteTime, lyricEvent.Text),

                MarkerEvent markerEvent => 
                    MetaMidiEvent.CreateMarker(absoluteTime, markerEvent.Text),

                CuePointEvent cuePointEvent => 
                    MetaMidiEvent.CreateCuePoint(absoluteTime, cuePointEvent.Text),

                ProgramNameEvent programNameEvent => 
                    MetaMidiEvent.CreateProgramName(absoluteTime, programNameEvent.Text),

                DeviceNameEvent deviceNameEvent => 
                    MetaMidiEvent.CreateDeviceName(absoluteTime, deviceNameEvent.Text),

                ChannelPrefixEvent channelPrefixEvent => 
                    MetaMidiEvent.CreateMidiChannelPrefix(absoluteTime, channelPrefixEvent.Channel),

                PortPrefixEvent portPrefixEvent => 
                    MetaMidiEvent.CreateMidiPort(absoluteTime, portPrefixEvent.Port),

                EndOfTrackEvent => 
                    MetaMidiEvent.CreateEndOfTrack(absoluteTime),

                SetTempoEvent tempoEvent => 
                    MetaMidiEvent.CreateSetTempo(
                        absoluteTime, 
                        microsecondsPerQuarterNote: (int)tempoEvent.MicrosecondsPerQuarterNote),

                SmpteOffsetEvent smpteEvent => 
                    MetaMidiEvent.CreateSmpteOffset(
                        absoluteTime,
                        (int)smpteEvent.Format,
                        smpteEvent.Hours,
                        smpteEvent.Minutes,
                        smpteEvent.Seconds,
                        smpteEvent.Frames,
                        smpteEvent.SubFrames),

                TimeSignatureEvent timeSignatureEvent => 
                    MetaMidiEvent.CreateTimeSignature(
                        absoluteTime,
                        timeSignatureEvent.Numerator,
                        1 << timeSignatureEvent.Denominator,
                        timeSignatureEvent.ClocksPerClick,
                        timeSignatureEvent.ThirtySecondNotesPerBeat),

                KeySignatureEvent keySignatureEvent => 
                    MetaMidiEvent.CreateKeySignature(
                        absoluteTime,
                        keySignatureEvent.Key,
                        keySignatureEvent.Scale),

                SequencerSpecificEvent sequencerEvent => 
                    MetaMidiEvent.CreateSequencerSpecific(absoluteTime, sequencerEvent.Data),

                UnknownMetaEvent unknownMetaEvent => 
                    MetaMidiEvent.CreateUnknownMeta(
                        absoluteTime, 
                        unknownMetaEvent.StatusByte, 
                        unknownMetaEvent.Data),

                // ============================================================
                // Channel Voice Messages
                // ============================================================

                NoteOffEvent noteOffEvent => 
                    MetaMidiEvent.CreateNoteOff(
                        absoluteTime,
                        (int)noteOffEvent.Channel,
                        noteOffEvent.NoteNumber,
                        noteOffEvent.Velocity),

                NoteOnEvent noteOnEvent => 
                    MetaMidiEvent.CreateNoteOn(
                        absoluteTime,
                        (int)noteOnEvent.Channel,
                        noteOnEvent.NoteNumber,
                        noteOnEvent.Velocity),

                NoteAftertouchEvent aftertouchEvent => 
                    MetaMidiEvent.CreatePolyKeyPressure(
                        absoluteTime,
                        (int)aftertouchEvent.Channel,
                        aftertouchEvent.NoteNumber,
                        aftertouchEvent.AftertouchValue),

                ControlChangeEvent controlChangeEvent => 
                    MetaMidiEvent.CreateControlChange(
                        absoluteTime,
                        (int)controlChangeEvent.Channel,
                        controlChangeEvent.ControlNumber,
                        controlChangeEvent.ControlValue),

                ProgramChangeEvent programChangeEvent => 
                    MetaMidiEvent.CreateProgramChange(
                        absoluteTime,
                        (int)programChangeEvent.Channel,
                        programChangeEvent.ProgramNumber),

                ChannelAftertouchEvent channelAftertouchEvent => 
                    MetaMidiEvent.CreateChannelPressure(
                        absoluteTime,
                        (int)channelAftertouchEvent.Channel,
                        channelAftertouchEvent.AftertouchValue),

                PitchBendEvent pitchBendEvent => 
                    MetaMidiEvent.CreatePitchBend(
                        absoluteTime,
                        (int)pitchBendEvent.Channel,
                        pitchBendEvent.PitchValue - 8192), // Convert from unsigned to signed

                // ============================================================
                // System Exclusive Events
                // ============================================================

                NormalSysExEvent normalSysExEvent => 
                    MetaMidiEvent.CreateNormalSysEx(absoluteTime, normalSysExEvent.Data),

                EscapeSysExEvent escapeSysExEvent => 
                    MetaMidiEvent.CreateEscapeSysEx(absoluteTime, escapeSysExEvent.Data),

                // ============================================================
                // System Common Messages
                // ============================================================

                MidiTimeCodeEvent mtcEvent => 
                    MetaMidiEvent.CreateMtcQuarterFrame(
                        absoluteTime, 
                        (byte)mtcEvent.Component, 
                        mtcEvent.ComponentValue),

                SongPositionPointerEvent songPositionEvent => 
                    MetaMidiEvent.CreateSongPositionPointer(absoluteTime, songPositionEvent.PointerValue),

                SongSelectEvent songSelectEvent => 
                    MetaMidiEvent.CreateSongSelect(absoluteTime, songSelectEvent.Number),

                TuneRequestEvent => 
                    MetaMidiEvent.CreateTuneRequest(absoluteTime),

                // ============================================================
                // System Real-Time Messages
                // ============================================================

                TimingClockEvent => 
                    MetaMidiEvent.CreateTimingClock(absoluteTime),

                StartEvent => 
                    MetaMidiEvent.CreateStart(absoluteTime),

                ContinueEvent => 
                    MetaMidiEvent.CreateContinue(absoluteTime),

                StopEvent => 
                    MetaMidiEvent.CreateStop(absoluteTime),

                ActiveSensingEvent => 
                    MetaMidiEvent.CreateActiveSensing(absoluteTime),

                ResetEvent => 
                    MetaMidiEvent.CreateSystemReset(absoluteTime),

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