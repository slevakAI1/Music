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
        public static void Execute(Score score, AppendPitchEventsParams config, MeasureMeta usedDivisionsPerMeasure)
        {
            var debugConfig = Helpers.DebugObject(config);

            if (score == null) throw new ArgumentNullException(nameof(score));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.PitchEvents == null || config.PitchEvents.Count == 0) return;

            foreach (var scorePart in GetTargetParts(score, config.Parts))
            {
                ProcessPart(scorePart, config, usedDivisionsPerMeasure);
            }
        }

        private static IEnumerable<Part> GetTargetParts(Score score, List<string> partNames)
        {
            return (score.Parts ?? Enumerable.Empty<Part>())
                .Where(p => p?.Name != null && partNames.Contains(p.Name));
        }

        private static void ProcessPart(Part scorePart, AppendPitchEventsParams config, MeasureMeta usedDivisionsPerMeasure)
        {
            if (scorePart.Measures == null || scorePart.Measures.Count == 0)
                return;

            ValidateStaffSupport(scorePart, config.Staffs);

            var targetStaffs = config.Staffs.OrderBy(s => s).ToList();

            for (int staffIndex = 0; staffIndex < targetStaffs.Count; staffIndex++)
            {
                var staff = targetStaffs[staffIndex];
                var context = new StaffProcessingContext
                {
                    Staff = staff,
                    CurrentBar = config.StartBar,
                    CurrentBeatPosition = 0,

                    TupletStates = new Dictionary<string, TupletState>(StringComparer.OrdinalIgnoreCase)
                };

                ProcessNotesForStaff(scorePart, config, context, usedDivisionsPerMeasure);

                AppendNotesHelper.AddBackupElementsIfNeeded(scorePart, staffIndex, targetStaffs.Count, usedDivisionsPerMeasure);
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

        private static void ProcessNotesForStaff(Part scorePart, AppendPitchEventsParams appendPitchEventsParams, StaffProcessingContext context, MeasureMeta usedDivisionsPerMeasure)
        {
            foreach (var pitchEvent in appendPitchEventsParams.PitchEvents)
            {
                if (!AppendNotesHelper.EnsureMeasureAvailable(scorePart, context.CurrentBar, scorePart.Name))
                    return;

                // Dispatch to chord or single-note processing
                if (pitchEvent.IsChord)
                {
                    ProcessChord(scorePart, pitchEvent, appendPitchEventsParams, context, usedDivisionsPerMeasure);
                }
                else
                {
                    ProcessSingleNote(scorePart, pitchEvent, appendPitchEventsParams, context, usedDivisionsPerMeasure);
                }
            }
        }

        private static void ProcessSingleNote(Part scorePart, PitchEvent pitchEvent, AppendPitchEventsParams config, StaffProcessingContext context, MeasureMeta usedDivisionsPerMeasure)
        {
            var measure = scorePart.Measures[context.CurrentBar - 1];
            var measureInfo = AppendNotesHelper.GetMeasureInfo(measure);

            var noteDuration = AppendNotesHelper.CalculateTotalNoteDuration(measureInfo.Divisions, pitchEvent);

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
                    scorePart, pitchEvent, context, measureInfo, noteDuration, usedDivisionsPerMeasure);

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
            var note = AppendNotesHelper.ComposeNote(pitchEvent, noteDuration, context.Staff);
            AppendNotesHelper.ApplyTupletSettings(note, pitchEvent, context.TupletStates);

            measure.MeasureElements.Add(new MeasureElement
            {
                Type = MeasureElementType.Note,
                Element = note
            });

            // Update tracking for the written note
            AppendNotesHelper.UpdatePositionTracking(context, pitchEvent, noteDuration, scorePart.Name, usedDivisionsPerMeasure);
        }

        private static void ProcessChord(Part scorePart, PitchEvent pitchEvent, AppendPitchEventsParams config, StaffProcessingContext context, MeasureMeta usedDivisionsPerMeasure)
        {
            // TO DO - This probably doesnt need note value... that property should be applied here if not already applied!

            // Convert chord to individual pitch events
            var chordNotes = ChordConverter.Convert(
                pitchEvent.ChordKey,
                (int)pitchEvent.ChordDegree!,
                pitchEvent.ChordQuality,
                pitchEvent.ChordBase,
                baseOctave: pitchEvent.Octave,
                noteValue: pitchEvent.Duration);

            // Apply dots and tuplet settings to chord notes
            foreach (var cn in chordNotes)
            {
                cn.Dots = pitchEvent.Dots;
            }

            if (!string.IsNullOrWhiteSpace(pitchEvent.TupletNumber))
            {
                foreach (var cn in chordNotes)
                {
                    cn.TupletNumber = pitchEvent.TupletNumber;
                    cn.TupletActualNotes = pitchEvent.TupletActualNotes;
                    cn.TupletNormalNotes = pitchEvent.TupletNormalNotes;
                    cn.Dots = pitchEvent.Dots;
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
                bool success = AppendNotesHelper.HandleTiedChordAcrossMeasures(scorePart, chordNotes, context, measureInfo, noteDuration, usedDivisionsPerMeasure);
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
            usedDivisionsPerMeasure.AddDivisionsUsed(scorePart.Name, context.Staff, context.CurrentBar, noteDuration);
        }

        public sealed class MeasureInfo
        {
            public int Divisions { get; set; }
            public int BeatsPerBar { get; set; }
            public int BarLengthDivisions { get; set; }
            public long ExistingDuration { get; set; }
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