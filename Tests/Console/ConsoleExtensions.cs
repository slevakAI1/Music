using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Music.Writer
{
    public static class ConsoleExtensions
    {
        // Applies design-driven defaults into an existing Writer instance.
        // Accept a nullable receiver so callers holding a `Writer?` (e.g. Globals.GenerationData)
        // can call this extension directly without a null-forgiving operator.
        public static void UpdateFromDesigner(this ConsoleData? data, Designer.Designer? design)
        {
            if (data == null) return;

            // ========== PARTS SYNCHRONIZATION ==========
            // Update the Writer parts to be in sync with the Designer parts if there are differences.
            // Preserve existing checked states

            var availableParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (design?.PartSet?.Parts != null)
            {
                foreach (var v in design.PartSet.Parts)
                {
                    var name = v?.PartName;
                    if (!string.IsNullOrWhiteSpace(name))
                        availableParts.Add(name!);
                }
            }

            var existingParts = data.PartsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var newPartsState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (existingParts.Count == 0)
            {
                // No existing state: default all parts to checked
                foreach (var name in availableParts)
                    newPartsState[name] = true;
            }
            else
            {
                // Preserve existing checked states for parts that still exist
                foreach (var name in availableParts)
                {
                    if (existingParts.TryGetValue(name, out var isChecked))
                        newPartsState[name] = isChecked;
                    else
                        newPartsState[name] = false; // New parts default to unchecked
                }
            }

            data.PartsState = newPartsState;

            // ========== SECTIONS SYNCHRONIZATION ==========
            // Update the Writer sections to be in sync with the Designer sections if there are differences.
            // Preserve existing checked states for sections

            var availableSections = new List<string>();
            if (design?.SectionSet?.Sections != null)
            {
                foreach (var section in design.SectionSet.Sections)
                {
                    var name = section?.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                        availableSections.Add(name!);
                }
            }

            var existingSections = data.SectionsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var newSectionsState = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (existingSections.Count == 0)
            {
                // No existing state: default all sections to unchecked (unlike parts, sections default to unchecked)
                foreach (var name in availableSections)
                    newSectionsState[name] = false;
            }
            else
            {
                // Preserve existing checked states for sections that still exist
                foreach (var name in availableSections)
                {
                    if (existingSections.TryGetValue(name, out var isChecked))
                        newSectionsState[name] = isChecked;
                    else
                        newSectionsState[name] = false; // New sections default to unchecked
                }
            }

            data.SectionsState = newSectionsState;

            // ========== END BAR ADJUSTMENT ==========
            // Ensure EndBar is within valid range based on design's total bars

            var total = design?.SectionSet?.TotalBars ?? 0;
            if (total > 0)
            {
                if (!data.EndBar.HasValue)
                    data.EndBar = total;
                else
                    data.EndBar = Math.Max(1, Math.Min(total, data.EndBar.Value));
            }
        }








        /// <summary>
        /// Converts Writer data to a AppendNotesParams for use with AppendNotes.
        /// Also adds notes per NumberOfNotes specified in writer data
        /// </summary>
        public static AppendPitchEventsParams ToAppendPitchEventsParams(this ConsoleData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var parts = (data.PartsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase))
                .Where(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            var selectedStaffs = (data.SelectedStaffs ?? new List<int>()).Any() 
                ? data.SelectedStaffs 
                : new List<int> { 1 }; // Default to staff 1 if none selected

            var noteEvents = new List<NoteEvent>();

            // Get tuplet settings from writer data
            bool isTuplet = !string.IsNullOrWhiteSpace(data.TupletNumber);
            string? tupletNumber = isTuplet ? data.TupletNumber : null;
            int tupletActualNotes = isTuplet ? (data.TupletCount ?? 3) : 0;
            int tupletNormalNotes = isTuplet ? (data.TupletOf ?? 2) : 0;


            //========================================================================
            //   MOVE THIS THIS CODE BLOCK INTO AppendNote.EXECUTE

            var noteEvent = new NoteEvent();

            if (data.IsChord ?? false)  // null = false
            {

                //========================================================================
                // Create Chord pitch event
                // Chords will be resolved into their component notes byn AppendNotes.Execute()

                noteEvent = new NoteEvent
                {
                    IsChord = true,
                    ChordKey = data.ChordKey,
                    ChordDegree = (int)data.ChordDegree,
                    ChordQuality = data.ChordQuality,
                    ChordBase = data.ChordBase,
                    Octave = data.Octave,

                    Duration = GetNoteValue(data.NoteValue),
                    IsRest = data.IsRest ?? false,
                    Dots = data.Dots
                };
            }
            else
            {
                // Single note or rest
                noteEvent = new NoteEvent
                {
                    Step = data.Step,
                    Octave = data.Octave,
                    Duration = GetNoteValue(data.NoteValue),
                    IsChord = false,
                    IsRest = data.IsRest ?? false,
                    Alter = GetAlter(data.Accidental),
                    Dots = data.Dots
                };
           }

            // Add tuplet settings if specified
            if (isTuplet)
            {
                noteEvent.TupletNumber = tupletNumber;
                noteEvent.TupletActualNotes = tupletActualNotes;
                noteEvent.TupletNormalNotes = tupletNormalNotes;
            }

            // Create specified number of test Pitch Events
            for (int i = 0; i < (data.NumberOfNotes.GetValueOrDefault(1)); i++)
            {
                noteEvents.AddRange(noteEvent);
            }

            var appendNotesParams = new AppendPitchEventsParams
            {
                Parts = parts,
                Staffs = selectedStaffs!,
                StartBar = data.StartBar.GetValueOrDefault(),
                PitchEvents = noteEvents
            };

            return appendNotesParams;
        }

        private static int GetNoteValue(string? noteValueString)
        {
            if (noteValueString != null && Music.MusicConstants.NoteValueMap.TryGetValue(noteValueString, out var nv))
            {
                return nv;
            }
            return 4; // default quarter note
        }

        private static int GetAlter(string? accidental)
        {
            return (accidental ?? "Natural") switch
            {
                var s when s.Equals("Sharp", StringComparison.OrdinalIgnoreCase) => 1,
                var s when s.Equals("Flat", StringComparison.OrdinalIgnoreCase) => -1,
                _ => 0
            };
        }
    }
}