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
        public static void Execute(Score score, AppendNotesParams config)
        {
            var debugConfig = Helpers.DebugObject(config);

            if (score == null) throw new ArgumentNullException(nameof(score));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.Notes == null || config.Notes.Count == 0) return;

            foreach (var scorePart in GetTargetParts(score, config.Parts))
            {
                ProcessPart(scorePart, config);
            }

            MessageBoxHelper.ShowMessage("Pattern has been applied to the score.", "Apply Pattern Set Notes");
        }

        private static IEnumerable<Part> GetTargetParts(Score score, List<string> partNames)
        {
            return (score.Parts ?? Enumerable.Empty<Part>())
                .Where(p => p?.Name != null && partNames.Contains(p.Name));
        }

        private static void ProcessPart(Part scorePart, AppendNotesParams config)
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
                    DurationPerMeasure = new Dictionary<int, long>(),
                    TupletStates = new Dictionary<string, TupletState>(StringComparer.OrdinalIgnoreCase)
                };

                ProcessNotesForStaff(scorePart, config, context);

                AddBackupElementsIfNeeded(scorePart, staffIndex, targetStaffs.Count, context.DurationPerMeasure);
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

        private static void ProcessNotesForStaff(Part scorePart, AppendNotesParams config, StaffProcessingContext context)
        {
            foreach (var writerNote in config.Notes)
            {
                if (!EnsureMeasureAvailable(scorePart, context.CurrentBar, scorePart.Name))
                    return;

                // Dispatch to chord or single-note processing
                if (writerNote.IsChord)
                {
                    ProcessChord(scorePart, writerNote, config, context);
                }
                else
                {
                    ProcessSingleNote(scorePart, writerNote, config, context);
                }
            }
        }

        private static void ProcessSingleNote(Part scorePart, PitchEvent writerNote, AppendNotesParams config, StaffProcessingContext context)
        {
            var measure = scorePart.Measures[context.CurrentBar - 1];
            var measureInfo = GetMeasureInfo(measure);

            var noteDuration = CalculateTotalNoteDuration(measureInfo.Divisions, writerNote);

            // Handle measure advancement for primary notes and full measures
            if (context.CurrentBeatPosition == measureInfo.BarLengthDivisions)
            {
                context.CurrentBar++;
                context.CurrentBeatPosition = 0;
            }

            // Handle ties across measures if needed
            if (context.CurrentBeatPosition + noteDuration > measureInfo.BarLengthDivisions)
            {
                bool success = HandleTiedNoteAcrossMeasures(
                    scorePart, writerNote, context, measureInfo, noteDuration);

                if (!success)
                    return;

                // Skip normal note composition
                return;
            }

            // Verify measure is still available after potential advancement
            if (!EnsureMeasureAvailable(scorePart, context.CurrentBar, scorePart.Name))
                return;

            // Refresh measure reference in case we advanced
            measure = scorePart.Measures[context.CurrentBar - 1];
            measureInfo = GetMeasureInfo(measure);

            // Compose and add the primary note
            var note = ComposeNote(writerNote, noteDuration, context.Staff);
            ApplyTupletSettings(note, writerNote, context.TupletStates);

            measure.MeasureElements.Add(new MeasureElement
            {
                Type = MeasureElementType.Note,
                Element = note
            });

            // Update tracking for the written note
            UpdatePositionTracking(context, writerNote, noteDuration);
        }

        private static void ProcessChord(Part scorePart, PitchEvent writerNote, AppendNotesParams config, StaffProcessingContext context)
        {
            // Convert chord to individual pitch events
            var chordNotes = ChordConverter.Convert(
                writerNote.ChordKey,
                (int)writerNote.ChordDegree!,
                writerNote.ChordQuality,
                writerNote.ChordBase,
                baseOctave: writerNote.Octave,
                noteValue: writerNote.Duration);

            // Apply dots and tuplet settings to chord notes
            foreach (var cn in chordNotes)
            {
                cn.Dots = writerNote.Dots;
            }

            if (!string.IsNullOrWhiteSpace(writerNote.TupletNumber))
            {
                foreach (var cn in chordNotes)
                {
                    cn.TupletNumber = writerNote.TupletNumber;
                    cn.TupletActualNotes = writerNote.TupletActualNotes;
                    cn.TupletNormalNotes = writerNote.TupletNormalNotes;
                    cn.Dots = writerNote.Dots;
                }
            }

            if (!EnsureMeasureAvailable(scorePart, context.CurrentBar, scorePart.Name))
                return;

            var measure = scorePart.Measures[context.CurrentBar - 1];
            var measureInfo = GetMeasureInfo(measure);

            var noteDuration = CalculateTotalNoteDuration(measureInfo.Divisions, chordNotes[0]);

            // Handle measure advancement
            if (context.CurrentBeatPosition == measureInfo.BarLengthDivisions)
            {
                context.CurrentBar++;
                context.CurrentBeatPosition = 0;
            }

            // Handle ties across measures for chord
            if (context.CurrentBeatPosition + noteDuration > measureInfo.BarLengthDivisions)
            {
                bool success = HandleTiedChordAcrossMeasures(scorePart, chordNotes, context, measureInfo, noteDuration);
                if (!success)
                    return;

                // Chord fully handled by tie-split logic
                return;
            }

            // Verify measure is still available after potential advancement
            if (!EnsureMeasureAvailable(scorePart, context.CurrentBar, scorePart.Name))
                return;

            // Refresh measure reference in case we advanced
            measure = scorePart.Measures[context.CurrentBar - 1];
            measureInfo = GetMeasureInfo(measure);

            // Compose and add primary chord note
            var primary = chordNotes[0];
            var primaryNote = ComposeNote(primary, noteDuration, context.Staff);
            ApplyTupletSettings(primaryNote, primary, context.TupletStates);

            measure.MeasureElements.Add(new MeasureElement
            {
                Type = MeasureElementType.Note,
                Element = primaryNote
            });

            // Add the remaining chord tones
            foreach (var chordNote in chordNotes.Skip(1))
            {
                var chordNoteDuration = CalculateTotalNoteDuration(measureInfo.Divisions, chordNote);
                var secondaryNote = ComposeNote(chordNote, chordNoteDuration, context.Staff);
                // Copy tie status from primary note
                secondaryNote.Tie = primaryNote.Tie;
                ApplyTupletSettings(secondaryNote, chordNote, context.TupletStates);

                measure.MeasureElements.Add(new MeasureElement
                {
                    Type = MeasureElementType.Note,
                    Element = secondaryNote
                });
            }

            // Advance position for the chord (advance once per chord)
            context.CurrentBeatPosition += noteDuration;
            UpdateDurationTracking(context.DurationPerMeasure, context.CurrentBar, noteDuration);
        }

        private static bool EnsureMeasureAvailable(Part scorePart, int currentBar, string partName)
        {
            if (currentBar > scorePart.Measures.Count)
            {
                MessageBoxHelper.ShowMessage(
                     $"Ran out of measures in part '{partName}' at bar {currentBar}. Not all notes were placed.",
                     "Insufficient Measures");
                return false;
            }
            return true;
        }

        private static int CalculateTotalNoteDuration(int divisions, PitchEvent writerNote)
        {
            var baseDuration = CalculateNoteDurationInMeasure(divisions, writerNote.Duration);
            
            // Calculate total duration including dots
            var noteDurationInMeasure = baseDuration;
            if (writerNote.Dots == 1)
            {
                noteDurationInMeasure = baseDuration + (baseDuration / 2);
            }
            else if (writerNote.Dots == 2)
            {
                noteDurationInMeasure = baseDuration + (baseDuration / 2) + (baseDuration / 4);
            }

            // Adjust duration when the note is part of a tuplet
            if (noteDurationInMeasure > 0
                && writerNote.TupletActualNotes > 0
                && writerNote.TupletNormalNotes > 0)
            {
                noteDurationInMeasure = (int)Math.Round(
                    (double)noteDurationInMeasure * writerNote.TupletNormalNotes / writerNote.TupletActualNotes);
            }

            return noteDurationInMeasure;
        }

        private static bool HandleTiedNoteAcrossMeasures(
            Part scorePart, 
            PitchEvent writerNote,
            StaffProcessingContext context,
            MeasureInfo measureInfo,
            int noteDuration)
        {
            long durationInCurrentMeasure = measureInfo.BarLengthDivisions - context.CurrentBeatPosition;
            long durationInNextMeasure = noteDuration - durationInCurrentMeasure;

            // Place first part of tied note in current measure
            var measure = scorePart.Measures[context.CurrentBar - 1];
            var firstNote = CreateTiedNote(
                writerNote, 
                (int)durationInCurrentMeasure, 
                measureInfo.Divisions, 
                context.Staff, 
                isFirstPart: true);

            measure.MeasureElements.Add(new MeasureElement
            {
                Type = MeasureElementType.Note,
                Element = firstNote
            });

            UpdateDurationTracking(context.DurationPerMeasure, context.CurrentBar, durationInCurrentMeasure);

            // Advance to next measure
            context.CurrentBar++;
            context.CurrentBeatPosition = 0;

            // Check if next measure exists
            if (context.CurrentBar > scorePart.Measures.Count)
            {
                MessageBoxHelper.ShowMessage(
                     $"Ran out of measures in part '{scorePart.Name}' at bar {context.CurrentBar}. Not all notes were placed.",
                     "Insufficient Measures");
                return false;
            }

            // Place second part of tied note in next measure
            var nextMeasure = scorePart.Measures[context.CurrentBar - 1];
            var secondNote = CreateTiedNote(
                writerNote, 
                (int)durationInNextMeasure, 
                measureInfo.Divisions, 
                context.Staff, 
                isFirstPart: false);

            nextMeasure.MeasureElements.Add(new MeasureElement
            {
                Type = MeasureElementType.Note,
                Element = secondNote
            });

            context.CurrentBeatPosition = durationInNextMeasure;
            UpdateDurationTracking(context.DurationPerMeasure, context.CurrentBar, durationInNextMeasure);

            return true;
        }

        private static bool HandleTiedChordAcrossMeasures(
            Part scorePart,
            List<PitchEvent> chordNotes,
            StaffProcessingContext context,
            MeasureInfo measureInfo,
            int noteDuration)
        {
            long durationInCurrentMeasure = measureInfo.BarLengthDivisions - context.CurrentBeatPosition;
            long durationInNextMeasure = noteDuration - durationInCurrentMeasure;

            // Place first part of tied chord notes in current measure
            var measure = scorePart.Measures[context.CurrentBar - 1];

            foreach (var chordNote in chordNotes)
            {
                var firstChordNote = CreateTiedNote(
                    chordNote,
                    (int)durationInCurrentMeasure,
                    measureInfo.Divisions,
                    context.Staff,
                    isFirstPart: true);

                measure.MeasureElements.Add(new MeasureElement
                {
                    Type = MeasureElementType.Note,
                    Element = firstChordNote
                });
            }

            UpdateDurationTracking(context.DurationPerMeasure, context.CurrentBar, durationInCurrentMeasure);

            // Advance to next measure
            context.CurrentBar++;
            context.CurrentBeatPosition = 0;

            // Check if next measure exists
            if (context.CurrentBar > scorePart.Measures.Count)
            {
                MessageBoxHelper.ShowMessage(
                     $"Ran out of measures in part '{scorePart.Name}' at bar {context.CurrentBar}. Not all notes were placed.",
                     "Insufficient Measures");
                return false;
            }

            // Place second part of tied chord notes in next measure
            var nextMeasure = scorePart.Measures[context.CurrentBar - 1];

            foreach (var chordNote in chordNotes)
            {
                var secondChordNote = CreateTiedNote(
                    chordNote,
                    (int)durationInNextMeasure,
                    measureInfo.Divisions,
                    context.Staff,
                    isFirstPart: false);

                nextMeasure.MeasureElements.Add(new MeasureElement
                {
                    Type = MeasureElementType.Note,
                    Element = secondChordNote
                });
            }

            context.CurrentBeatPosition = durationInNextMeasure;
            UpdateDurationTracking(context.DurationPerMeasure, context.CurrentBar, durationInNextMeasure);

            return true;
        }

        private static MusicXml.Domain.Note CreateTiedNote(
            PitchEvent writerNote, 
            int duration, 
            int divisions, 
            int staff, 
            bool isFirstPart)
        {
            var note = new MusicXml.Domain.Note
            {
                Type = DurationTypeForTiedNote(duration, divisions),
                Duration = duration,
                Voice = staff == 1 ? 1 : 5,
                Staff = staff,
                IsChordTone = writerNote.IsChord,
                IsRest = writerNote.IsRest,
                Tie = writerNote.IsRest ? Tie.NotTied : (isFirstPart ? Tie.Start : Tie.Stop),
                Dots = 0  // Tied notes across measures typically don't use dots
            };

            if (!writerNote.IsRest)
            {
                note.Pitch = new Pitch
                {
                    Step = char.ToUpper(writerNote.Step),
                    Octave = writerNote.Octave,
                    Alter = writerNote.Alter
                };
            }

            return note;
        }

        private static MusicXml.Domain.Note ComposeNote(PitchEvent writerNote, int duration, int staff)
        {
            var note = new MusicXml.Domain.Note
            {
                Type = DurationTypeString(writerNote.Duration),
                Duration = duration,
                Voice = staff == 1 ? 1 : 5,
                Staff = staff,
                IsChordTone = writerNote.IsChord,
                IsRest = writerNote.IsRest,
                Dots = writerNote.Dots
            };

            if (!writerNote.IsRest)
            {
                note.Pitch = new Pitch
                {
                    Step = char.ToUpper(writerNote.Step),
                    Octave = writerNote.Octave,
                    Alter = writerNote.Alter
                };
            }

            return note;
        }

        private static void ApplyTupletSettings(
            MusicXml.Domain.Note note, 
            PitchEvent writerNote, 
            Dictionary<string, TupletState> tupletStates)
        {
            if (string.IsNullOrWhiteSpace(writerNote.TupletNumber)
                || writerNote.TupletActualNotes <= 0
                || writerNote.TupletNormalNotes <= 0)
            {
                return;
            }

            var key = writerNote.TupletNumber!;
            if (!tupletStates.TryGetValue(key, out var ts))
            {
                ts = InitializeTupletState(key, writerNote);
                tupletStates[key] = ts;
            }

            // Set time modification on the note
            note.TimeModification = new TimeModification
            {
                ActualNotes = ts.Actual,
                NormalNotes = ts.Normal,
                NormalType = DurationTypeString(writerNote.Duration)
            };

            // Handle tuplet notation (start/stop markers)
            ApplyTupletNotation(note, writerNote, ts, tupletStates, key);
        }

        private static TupletState InitializeTupletState(string key, PitchEvent writerNote)
        {
            int parsedNum = 1;
            int.TryParse(key, out parsedNum);
            
            return new TupletState
            {
                Actual = writerNote.TupletActualNotes,
                Normal = writerNote.TupletNormalNotes,
                Remaining = writerNote.TupletActualNotes,
                Number = parsedNum,
                IsStarted = false
            };
        }

        private static void ApplyTupletNotation(
            MusicXml.Domain.Note note, 
            PitchEvent writerNote, 
            TupletState ts, 
            Dictionary<string, TupletState> tupletStates, 
            string key)
        {
            // Mark start on first base note
            if (!ts.IsStarted)
            {
                note.TupletNotation = new TupletNotation
                {
                    Type = "start",
                    Number = ts.Number
                };
                ts.IsStarted = true;
            }

            // Decrement and mark stop on last base note
            if (!writerNote.IsChord)
            {
                ts.Remaining--;
                if (ts.Remaining <= 0)
                {
                    note.TupletNotation = new TupletNotation
                    {
                        Type = "stop",
                        Number = ts.Number
                    };
                    tupletStates.Remove(key);
                }
            }
        }

        private static void UpdatePositionTracking(
            StaffProcessingContext context, 
            PitchEvent writerNote, 
            int noteDuration)
        {
            if (!writerNote.IsChord)
            {
                context.CurrentBeatPosition += noteDuration;
                UpdateDurationTracking(context.DurationPerMeasure, context.CurrentBar, noteDuration);
            }
        }

        private static void UpdateDurationTracking(Dictionary<int, long> durationPerMeasure, int currentBar, long duration)
        {
            if (!durationPerMeasure.ContainsKey(currentBar))
                durationPerMeasure[currentBar] = 0;
            durationPerMeasure[currentBar] += duration;
        }

        private static void AddBackupElementsIfNeeded(
            Part scorePart, 
            int staffIndex, 
            int totalStaffs, 
            Dictionary<int, long> durationPerMeasure)
        {
            if (staffIndex >= totalStaffs - 1)
                return;

            foreach (var measureEntry in durationPerMeasure.OrderBy(kvp => kvp.Key))
            {
                int measureNumber = measureEntry.Key;
                long durationWritten = measureEntry.Value;

                if (durationWritten > 0 && measureNumber <= scorePart.Measures.Count)
                {
                    var measure = scorePart.Measures[measureNumber - 1];
                    measure.MeasureElements ??= new List<MeasureElement>();
                    measure.MeasureElements.Add(new MeasureElement
                    {
                        Type = MeasureElementType.Backup,
                        Element = new Backup { Duration = (int)durationWritten }
                    });
                }
            }
        }

        private static MeasureInfo GetMeasureInfo(Measure measure)
        {
            var divisions = Math.Max(1, measure.Attributes?.Divisions ?? MusicConstants.DefaultDivisions);
            var beatsPerBar = measure.Attributes?.Time?.Beats ?? 4;
            var existingDuration = CalculateExistingDuration(measure);

            return new MeasureInfo
            {
                Divisions = divisions,
                BeatsPerBar = beatsPerBar,
                BarLengthDivisions = divisions * beatsPerBar,
                ExistingDuration = existingDuration
            };
        }

        private static long CalculateExistingDuration(Measure measure)
        {
            long duration = 0;
            foreach (var me in measure.MeasureElements ?? Enumerable.Empty<MeasureElement>())
            {
                if (me?.Type == MeasureElementType.Note &&
                    me.Element is MusicXml.Domain.Note n &&
                    n.Duration > 0 &&
                    !n.IsChordTone)
                {
                    duration += n.Duration;
                }
            }
            return duration;
        }

        private static int CalculateNoteDurationInMeasure(int divisions, int noteValue)
        {
            int numerator = divisions * 4;
            if (numerator % noteValue != 0)
            {
                // Calculate the minimum divisions needed for this note value
                // For noteValue=32, need divisions≥8; for noteValue=64, need divisions≥16
                int minDivisions = (noteValue + 3) / 4; // Round up: (32+3)/4=8, (64+3)/4=16

                var msg = $"Cannot represent note value '{noteValue}' (1/{noteValue} note) with divisions={divisions}.\n" +
                          $"Minimum required divisions: {minDivisions}.\n\n" +
                          $"To fix this:\n" +
                          $"1. Create a new score (divisions will be set to {MusicConstants.DefaultDivisions})\n" +
                          $"2. Or manually edit the score's divisions value";
                MessageBoxHelper.ShowError(msg, "Invalid Duration");
                return 0;
            }
            return numerator / noteValue;
        }

        private static string DurationTypeString(int denom) => denom switch
        {
            1 => "whole",
            2 => "half",
            4 => "quarter",
            8 => "eighth",
            16 => "16th",
            32 => "32nd",
            64 => "64th",
            _ => "quarter"
        };

        /// <summary>
        /// Determines the best note type for a tied note fragment based on its duration.
        /// </summary>
        private static string DurationTypeForTiedNote(long duration, int divisions)
        {
            // Common durations in terms of divisions
            var wholeDuration = divisions * 4;
            var halfDuration = divisions * 2;
            var quarterDuration = divisions;
            var eighthDuration = divisions / 2;
            var sixteenthDuration = divisions / 4;

            if (duration >= wholeDuration) return "whole";
            if (duration >= halfDuration) return "half";
            if (duration >= quarterDuration) return "quarter";
            if (duration >= eighthDuration) return "eighth";
            if (duration >= sixteenthDuration) return "16th";
            
            return "quarter"; // fallback
        }

        public sealed class MeasureInfo
        {
            public int Divisions { get; set; }
            public int BeatsPerBar { get; set; }
            public int BarLengthDivisions { get; set; }
            public long ExistingDuration { get; set; }
        }

        // Local helper to track tuplet lifecycle for a given tuplet id within a staff pass.
        private sealed class TupletState
        {
            public int Actual { get; set; }
            public int Normal { get; set; }
            public int Remaining { get; set; }
            public int Number { get; set; }
            public bool IsStarted { get; set; }
        }

        // Helper class to track state during staff processing
        private sealed class StaffProcessingContext
        {
            public int Staff { get; set; }
            public int CurrentBar { get; set; }
            public long CurrentBeatPosition { get; set; }
            public Dictionary<int, long> DurationPerMeasure { get; set; } = new();
            public Dictionary<string, TupletState> TupletStates { get; set; } = new();
        }
    }
}