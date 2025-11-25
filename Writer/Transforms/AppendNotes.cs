using Music.Designer;
using MusicXml.Domain;

namespace Music.Writer
{
    /// <summary>
    /// Inserts a set of notes based on user parameters.
    /// Method assumes the score has already been initialized with parts, measure, tempo, time signature.
    /// All parameters are expected to be pre-validated.
    /// </summary>
    public static class AppendNotes
    {
        /// <summary>
        /// Adds notes to the specified score based on the provided configuration.
        /// All parameters are expected to be pre-validated.
        /// </summary>
        public static void Execute(Score score, AppendNoteEventsToScoreParams config, MeasureMeta measureMeta)
        {
            // Pre-process: remove all backup elements before appending notes.
            AppendNotesHelper.RemoveAllBackupElements(score, config.Parts);

            var debugConfig = Helpers.DebugObject(config);

            if (score == null) throw new ArgumentNullException(nameof(score));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.NoteEvents == null || config.NoteEvents.Count == 0) return;

            foreach (var scorePart in GetTargetParts(score, config.Parts))
            {
                ProcessPart(scorePart, config, measureMeta);
            }
            
            // Post-process: Add all required backup elements after all notes are inserted
            AppendNotesHelper.AddAllRequiredBackupElements(score, config.Parts, measureMeta);
        }

        private static IEnumerable<Part> GetTargetParts(Score score, List<string> partNames)
        {
            return (score.Parts ?? Enumerable.Empty<Part>())
                .Where(p => p?.Name != null && partNames.Contains(p.Name));
        }

        private static void ProcessPart(Part scorePart, AppendNoteEventsToScoreParams config, MeasureMeta measureMeta)
        {
            if (scorePart.Measures == null || scorePart.Measures.Count == 0)
                return;

            ValidateStaffSupport(scorePart, config.Staffs);

            var targetStaffs = config.Staffs.OrderBy(s => s).ToList();

            for (int staffIndex = 0; staffIndex < targetStaffs.Count; staffIndex++)
            {
                var staff = targetStaffs[staffIndex];
                
                // Find where to start appending for this part and staff
                var (startMeasure, startPosition) = AppendNotesHelper.FindAppendStartPosition(
                    scorePart,
                    staff,
                    measureMeta);

                var context = new StaffProcessingContext
                {
                    Staff = staff,
                    CurrentBar = startMeasure,
                    CurrentBeatPosition = startPosition,
                    TupletStates = new Dictionary<string, TupletState>(StringComparer.OrdinalIgnoreCase)
                };

                ProcessNotesForStaff(scorePart, config, context, measureMeta);
            }
        }

        private static void ValidateStaffSupport(Part scorePart, List<int> staffs)
        {
            var twoStaffVoices = VoiceCatalog.GetTwoStaffVoices();
            bool allowsTwoStaves = twoStaffVoices.Contains(scorePart.Name, StringComparer.OrdinalIgnoreCase);

            if (staffs.Any(s => s == 2) && !allowsTwoStaves)
            {
                throw new InvalidOperationException(
                    $"Part '{scorePart.Name}' does not support a second staff. " +
                    $"Only these voices support two staves: {string.Join(", ", twoStaffVoices)}");
            }
        }

        private static void ProcessNotesForStaff(
            Part scorePart, 
            AppendNoteEventsToScoreParams appendNoteEventsParams, 
            StaffProcessingContext context, 
            MeasureMeta measureMeta)
        {
            foreach (var NoteEvent in appendNoteEventsParams.NoteEvents)
            {
                if (!AppendNotesHelper.EnsureMeasureAvailable(scorePart, context.CurrentBar, scorePart.Name))
                    return;

                // Dispatch to chord or single-note processing
                if (NoteEvent.IsChord)
                {
                    ProcessChord(scorePart, NoteEvent, appendNoteEventsParams, context, measureMeta);
                }
                else
                {
                    ProcessSingleNote(scorePart, NoteEvent, appendNoteEventsParams, context, measureMeta);
                }
            }
        }

        private static void ProcessSingleNote(Part scorePart, NoteEvent noteEvent, AppendNoteEventsToScoreParams config, StaffProcessingContext context, MeasureMeta measureMeta)
        {
            var measure = scorePart.Measures[context.CurrentBar - 1];
            var measureInfo = AppendNotesHelper.GetMeasureInfo(measure);

            var noteDuration = AppendNotesHelper.CalculateTotalNoteDuration(measureInfo.Divisions, noteEvent);

            // Handle measure advancement for primary notes and full measures
            if (context.CurrentBeatPosition == measureInfo.BarLengthDivisions)
            {
                context.CurrentBar++;
                context.CurrentBeatPosition = 0;
            }

            // Handle ties across measures if needed
            if (context.CurrentBeatPosition + noteDuration > measureInfo.BarLengthDivisions)
            {
                bool success = AppendNotesHelper.HandleTiedNoteAcrossMeasures(
                    scorePart, noteEvent, context, measureInfo, noteDuration, measureMeta);

                if (!success)
                    return;

                // Skip normal note composition
                return;
            }

            // Verify measure is still available after potential advancement
            if (!AppendNotesHelper.EnsureMeasureAvailable(scorePart, context.CurrentBar, scorePart.Name))
                return;

            // Refresh measure reference in case we advanced
            measure = scorePart.Measures[context.CurrentBar - 1];
            measureInfo = AppendNotesHelper.GetMeasureInfo(measure);

            // Compose and add the primary note
            var note = AppendNotesHelper.ComposeNote(noteEvent, noteDuration, context.Staff);
            AppendNotesHelper.ApplyTupletSettings(note, noteEvent, context.TupletStates);

            measure.MeasureElements.Add(new MeasureElement
            {
                Type = MeasureElementType.Note,
                Element = note
            });

            // Update tracking for the written note
            AppendNotesHelper.UpdatePositionTracking(context, noteEvent, noteDuration, scorePart.Name, measureMeta);
        }

        private static void ProcessChord(Part scorePart, NoteEvent noteEvent, AppendNoteEventsToScoreParams config, StaffProcessingContext context, MeasureMeta measureMeta)
        {
            // TO DO - This probably doesnt need note value... that property should be applied here if not already applied!

            // Convert chord to individual pitch events
            var chordNotes = ChordConverter.Convert(
                noteEvent.ChordKey,
                (int)noteEvent.ChordDegree!,
                noteEvent.ChordQuality,
                noteEvent.ChordBase,
                baseOctave: noteEvent.Octave,
                noteValue: noteEvent.Duration);

            // Apply dots and tuplet settings to chord notes
            foreach (var cn in chordNotes)
            {
                cn.Dots = noteEvent.Dots;
            }

            if (!string.IsNullOrWhiteSpace(noteEvent.TupletNumber))
            {
                foreach (var cn in chordNotes)
                {
                    cn.TupletNumber = noteEvent.TupletNumber;
                    cn.TupletActualNotes = noteEvent.TupletActualNotes;
                    cn.TupletNormalNotes = noteEvent.TupletNormalNotes;
                    cn.Dots = noteEvent.Dots;
                }
            }

            if (!AppendNotesHelper.EnsureMeasureAvailable(scorePart, context.CurrentBar, scorePart.Name))
                return;

            var measure = scorePart.Measures[context.CurrentBar - 1];
            var measureInfo = AppendNotesHelper.GetMeasureInfo(measure);

            var noteDuration = AppendNotesHelper.CalculateTotalNoteDuration(measureInfo.Divisions, chordNotes[0]);

            // Handle measure advancement
            if (context.CurrentBeatPosition == measureInfo.BarLengthDivisions)
            {
                context.CurrentBar++;
                context.CurrentBeatPosition = 0;
            }

            // Handle ties across measures for chord
            if (context.CurrentBeatPosition + noteDuration > measureInfo.BarLengthDivisions)
            {
                bool success = AppendNotesHelper.HandleTiedChordAcrossMeasures(scorePart, chordNotes, context, measureInfo, noteDuration, measureMeta);
                if (!success)
                    return;

                // Chord fully handled by tie-split logic
                return;
            }

            // Verify measure is still available after potential advancement
            if (!AppendNotesHelper.EnsureMeasureAvailable(scorePart, context.CurrentBar, scorePart.Name))
                return;

            // Refresh measure reference in case we advanced
            measure = scorePart.Measures[context.CurrentBar - 1];
            measureInfo = AppendNotesHelper.GetMeasureInfo(measure);

            // Compose and add primary chord note
            var primary = chordNotes[0];
            var primaryNote = AppendNotesHelper.ComposeNote(primary, noteDuration, context.Staff);
            AppendNotesHelper.ApplyTupletSettings(primaryNote, primary, context.TupletStates);

            measure.MeasureElements.Add(new MeasureElement
            {
                Type = MeasureElementType.Note,
                Element = primaryNote
            });

            // Add the remaining chord tones
            foreach (var chordNote in chordNotes.Skip(1))
            {
                var chordNoteDuration = AppendNotesHelper.CalculateTotalNoteDuration(measureInfo.Divisions, chordNote);
                var secondaryNote = AppendNotesHelper.ComposeNote(chordNote, chordNoteDuration, context.Staff);
                // Copy tie status from primary note
                secondaryNote.Tie = primaryNote.Tie;
                AppendNotesHelper.ApplyTupletSettings(secondaryNote, chordNote, context.TupletStates);

                measure.MeasureElements.Add(new MeasureElement
                {
                    Type = MeasureElementType.Note,
                    Element = secondaryNote
                });
            }

            // Advance position for the chord (advance once per chord)
            context.CurrentBeatPosition += noteDuration;
            measureMeta.AddDivisionsUsed(scorePart.Name, context.Staff, context.CurrentBar, noteDuration);
        }

        public sealed class MeasureInfo
        {
            public int Divisions { get; set; }
            public int BeatsPerBar { get; set; }
            public int BarLengthDivisions { get; set; }
        }

        // Local helper to track tuplet lifecycle for a given tuplet id within a staff pass.
        public sealed class TupletState
        {
            public int Actual { get; set; }
            public int Normal { get; set; }
            public int Remaining { get; set; }
            public int Number { get; set; }
            public bool IsStarted { get; set; }
        }

        // Helper class to track state during staff processing
        public sealed class StaffProcessingContext
        {
            public int Staff { get; set; }
            public int CurrentBar { get; set; }
            public long CurrentBeatPosition { get; set; }
            public Dictionary<string, TupletState> TupletStates { get; set; } = new();
        }
    }
}