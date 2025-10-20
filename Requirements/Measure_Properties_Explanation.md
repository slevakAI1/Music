Measure, MeasureElement, and MeasureAttributes — music-centric explanation

Measure
- Width (decimal)
  - Engraving/layout concept: corresponds to the visual width of the measure as it should be laid out on the page.
  - In MusicXML, measure width is expressed in “tenths” (a device-independent unit relative to staff space). A value of -1 here likely means “unspecified/not set,” so layout engines can auto-calculate width based on content and spacing rules.
  - Impacts spacing, justification, and system breaking, but not playback.

- MeasureElements (List<MeasureElement>)
  - The ordered sequence of musical content inside the measure, exactly as it appears in MusicXML under a <measure> element.
  - Can include time-bearing items (e.g., notes, chords, rests) and non-time-bearing or control items (e.g., direction markings, harmony symbols, print/layout cues, barlines), as well as temporal cursor controls (backup and forward) used by MusicXML to manage voices and concurrent events.
  - Order matters: MusicXML uses a linear stream combined with backup/forward to represent multiple voices and simultaneous events. Consumers should process this in order to reconstruct voices, layering, and timing.

- Attributes (MeasureAttributes)
  - The musical state in effect at the start of the measure (or at the first <attributes> occurrence in the measure): rhythmic quantization (Divisions), key signature, time signature, and clef.
  - These attributes govern how to interpret the elements in MeasureElements (e.g., how to translate durations, how to spell pitches, and how to render on the staff).
  - Note that MusicXML allows attributes to change mid-measure; if your model captures only one Attributes snapshot per measure, it usually reflects the state at the measure start (or last seen attributes), while mid-measure changes would appear as MeasureElements of type Attributes.

MeasureElement
- Type (MeasureElementType)
  - Identifies which kind of MusicXML child this is (e.g., Note, Rest, ChordNote, Direction, Harmony, Backup, Forward, Attributes, Barline, Print, Sound, etc.).
  - Enables safe dispatch/casting of the Element payload.

- Element (object)
  - The concrete payload for the element noted by Type.
  - Examples:
    - If Type = Note, Element would be a Note-like object that includes pitch, duration, ties, articulations, notations, voice, etc.
    - If Type = Backup/Forward, Element would carry a duration (in divisions) to move the time cursor backward/forward for voice management.
    - If Type = Attributes, Element would contain a MusicXML <attributes> snapshot with changes (e.g., a mid-measure clef change).
  - Consumers typically pattern-match on Type and cast Element to the expected concrete type.

MeasureAttributes
- Divisions (int)
  - Rhythmic resolution used to quantify durations in this measure (and until changed).
  - Typically “divisions per quarter note.” For example, Divisions = 480 means a quarter note = 480 ticks, an eighth note = 240, a dotted quarter = 720, etc.
  - All time-bearing elements (notes, rests, backup/forward) express their duration in these units.

- Key (Key)
  - Fifths (int)
    - Circle-of-fifths integer: sharps are positive, flats are negative. Examples: -1 = 1 flat (F major/D minor), 0 = no accidentals (C major/A minor), +3 = 3 sharps (A major/F# minor).
  - Mode (string)
    - Tonal quality such as major, minor, or modal designations like dorian, mixolydian, etc., corresponding to MusicXML’s <mode>.
  - Together these guide pitch spelling, courtesy accidentals, and key signature rendering.

- Time (Time)
  - Beats (int)
    - The number of beats per measure (time-signature numerator). Examples: 3 for 3/4, 6 for 6/8.
  - BeatType/Mode (string)
    - Represents the beat type (time-signature denominator) as in MusicXML <beat-type>; e.g., 4 for quarter-note beat, 8 for eighth-note beat.
  - Symbol (TimeSymbol)
    - Rendering hint for how to display the time signature: e.g., common time (C), cut time (C|), numeric, single-number, additive groupings, etc. Affects engraving but not playback.

- Clef (Clef)
  - Sign (string)
    - Which clef glyph to use: G (treble), F (bass), C (alto/tenor), percussion, TAB, etc.
  - Line (int)
    - Which staff line the clef’s reference point sits on (e.g., G clef on line 2 for treble, F clef on line 4 for bass). Determines pitch-to-staff mapping and ledger line usage.

How these interact in practice
- Divisions determines how to interpret and sum durations of notes, rests, backup, and forward in MeasureElements.
- Key, Time, and Clef define the musical context for pitch spelling, rhythm grouping/measure boundaries, and staff mapping at the measure’s start.
- Mid-measure changes appear as MeasureElements (e.g., clef change or time/key changes) and update the context from that point onward.
- Width is purely for spacing/rendering; it doesn’t change musical meaning or playback.

Notes
- If the actual property name for beat type is Mode, consider renaming to BeatType for clarity.
- Consider constraining Clef.Sign and Time.Symbol to enums for stronger typing and validation.