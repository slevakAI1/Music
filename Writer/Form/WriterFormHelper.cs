using Music.Domain;
using Music.Tests;

namespace Music.Writer
{
    // Helper class extracted from WriterForm for non-event, non-lifecycle logic.
    internal static class WriterFormHelper
    {
        /// <summary>
        /// Adds a set of repeating note, chords or rests to the phrase grid.
        /// (Moved from WriterForm.ExecuteCommandWriteRepeatingNotes)
        /// </summary>
        internal static void ExecuteCommandWriteRepeatingNotes(
            WriterFormData writer,
            List<MidiInstrument> midiInstruments,
            DataGridView dgvPhrase,
            ref int phraseNumber)
        {

            // Get the params from Writer
            // tbd

            // Create the repeating phrase - this is a transform!
            // tbd



            // Add the phrase to the grid (just appends a new row for now


            var phrase = writer.ToPhrase();



            // Set phrase name/number
            phraseNumber++;
            var phraseName = phraseNumber.ToString();

            // Get part name from the phrase
            var partName = phrase.MidiPartName ?? "Unknown";

            // Add new row
            int newRowIndex = dgvPhrase.Rows.Add();
            var row = dgvPhrase.Rows[newRowIndex];

            // Column 0: Hidden data (Phrase object)
            row.Cells["colData"].Value = phrase;

            // Column 1: MIDI Instrument dropdown - set to first instrument (Acoustic Grand Piano) as default
            // User can change this by clicking the dropdown
            row.Cells["colInstrument"].Value = midiInstruments[0].ProgramNumber;

            // Column 2: Stave - adds +1 each time new row is added with same instrument as existing row(s)
            row.Cells["colStave"].Value = dgvPhrase.Rows.Cast<DataGridViewRow>().Count(r => r.Cells["colInstrument"].Value?.Equals(midiInstruments[0].ProgramNumber) == true);

            // Column 3: Event number
            row.Cells["colEventNumber"].Value = phraseName;

            // Column 4: Description
            row.Cells["colDescription"].Value = $"Part: {partName}";

            // Column 5: Phrase details (placeholder)
            row.Cells["colPhrase"].Value = "tbd";
        }

        /// <summary>
        /// Plays a MIDI document and releases the MIDI device after playback completes.
        /// (Moved from WriterForm.PlayMidiFromPhrasesAsync)
        /// </summary>
        internal static async Task PlayMidiFromPhrasesAsync(
            IMidiPlaybackService playbackService,
            MidiSongDocument midiDoc,
            Form owner)
        {
            if (midiDoc == null)
                throw new ArgumentNullException(nameof(midiDoc));

            // Always stop any existing playback first
            playbackService.Stop();

            // Select first available output device
            var devices = playbackService.EnumerateOutputDevices();
            var first = default(string);
            foreach (var d in devices)
            {
                first = d;
                break;
            }
            if (first == null)
            {
                MessageBox.Show(owner, "No MIDI output device found.", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            playbackService.SelectOutput(first);
            playbackService.Play(midiDoc);

            // Wait for playback duration plus buffer
            var duration = midiDoc?.Duration ?? TimeSpan.Zero;
            var totalDelay = duration.TotalMilliseconds + 250;

            if (totalDelay > 0)
                await Task.Delay((int)Math.Min(totalDelay, int.MaxValue));

            // Always stop to release resources
            playbackService.Stop();
        }
    }
}