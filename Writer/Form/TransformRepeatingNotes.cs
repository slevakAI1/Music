//using Music.Domain;
//using Music.Tests;

//namespace Music.Writer
//{
//    /// <summary>
//    /// Transform that creates a phrase with repeating notes, chords, or rests.
//    /// </summary>
//    internal static class TransformRepeatingNotes
//    {
//        /// <summary>
//        /// Creates a phrase with repeating note, chord, or rest based on writer form data.
//        /// Returns the phrase ready to be added to the grid.
//        /// </summary>
//        internal static Phrase ExecuteCommandWriteRepeatingNotes(WriterFormData writer)
//        {
//            // Get the params from Writer
//            var numberOfNotes = writer.NumberOfNotes ?? 1;
//            var midiProgramName = "Acoustic Grand Piano"; // Default instrument

//            // Create the repeating phrase - this is a transform!
//            var phrase = CreateRepeatingNotesPhrase.Convert(
//                writer,
//                numberOfNotes,
//                midiProgramName);

//            return phrase;
//        }
//    }
//}