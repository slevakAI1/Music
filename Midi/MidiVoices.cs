// AI: purpose=Canonical General MIDI voice list for UI and mapping to program numbers; includes sentinel Drum Set entry (255).
// AI: invariants=ProgramNumber in 0-127 for GM voices, sentinel 255 for Drum Set; callers treat returned list as read-only.
// AI: deps=Used by UI voice pickers and presets; changing names or order may break persisted settings and tests.
// AI: change=When adding/removing voices update s_cachedVoices and any UI that expects indices or exact names.

namespace Music.MyMidi
{
    // AI: MidiVoices: simple immutable pair of program number and display name; used to populate voice lists.
    public sealed class MidiVoices
    {
        public int ProgramNumber { get; }
        public string Name { get; }

        public MidiVoices(int programNumber, string name)
        {
            ProgramNumber = programNumber;
            Name = name;
        }

        // Cached list created once at type initialization.
        // NOTE: callers should treat this as read-only. If callers must not be
        // able to modify the collection, change the public API to return
        // IReadOnlyList<MidiVoices> or expose a ReadOnlyCollection<MidiVoices>.
        private static readonly List<MidiVoices> s_cachedVoices = new List<MidiVoices>
        {
            new (-1, "Select..."),

            // Piano (0-7)
            new(0, "Acoustic Grand Piano"),
            new(1, "Bright Acoustic Piano"),
            new(2, "Electric Grand Piano"),
            new(3, "Honky-tonk Piano"),
            new(4, "Electric Piano 1"),
            new(5, "Electric Piano 2"),
            new(6, "Harpsichord"),
            new(7, "Clavinet"),

            // Chromatic Percussion (8-15)
            new(8, "Celesta"),
            new(9, "Glockenspiel"),
            new(10, "Music Box"),
            new(11, "Vibraphone"),
            new(12, "Marimba"),
            new(13, "Xylophone"),
            new(14, "Tubular Bells"),
            new(15, "Dulcimer"),

            // Organ (16-23)
            new(16, "Drawbar Organ"),
            new(17, "Percussive Organ"),
            new(18, "Rock Organ"),
            new(19, "Church Organ"),
            new(20, "Reed Organ"),
            new(21, "Accordion"),
            new(22, "Harmonica"),
            new(23, "Tango Accordion"),

            // Guitar (24-31)
            new(24, "Acoustic Guitar (nylon)"),
            new(25, "Acoustic Guitar (steel)"),
            new(26, "Electric Guitar (jazz)"),
            new(27, "Electric Guitar (clean)"),
            new(28, "Electric Guitar (muted)"),
            new(29, "Overdriven Guitar"),
            new(30, "Distortion Guitar"),
            new(31, "Guitar Harmonics"),

            // Bass (32-39)
            new(32, "Acoustic Bass"),
            new(33, "Electric Bass (finger)"),
            new(34, "Electric Bass (pick)"),
            new(35, "Fretless Bass"),
            new(36, "Slap Bass 1"),
            new(37, "Slap Bass 2"),
            new(38, "Synth Bass 1"),
            new(39, "Synth Bass 2"),

            // Strings (40-47)
            new(40, "Violin"),
            new(41, "Viola"),
            new(42, "Cello"),
            new(43, "Contrabass"),
            new(44, "Tremolo Strings"),
            new(45, "Pizzicato Strings"),
            new(46, "Orchestral Harp"),
            new(47, "Timpani"),

            // Ensemble (48-55)
            new(48, "String Ensemble 1"),
            new(49, "String Ensemble 2"),
            new(50, "Synth Strings 1"),
            new(51, "Synth Strings 2"),
            new(52, "Choir Aahs"),
            new(53, "Voice Oohs"),
            new(54, "Synth Voice"),
            new(55, "Orchestra Hit"),

            // Brass (56-63)
            new(56, "Trumpet"),
            new(57, "Trombone"),
            new(58, "Tuba"),
            new(59, "Muted Trumpet"),
            new(60, "French Horn"),
            new(61, "Brass Section"),
            new(62, "Synth Brass 1"),
            new(63, "Synth Brass 2"),

            // Reed (64-71)
            new(64, "Soprano Sax"),
            new(65, "Alto Sax"),
            new(66, "Tenor Sax"),
            new(67, "Baritone Sax"),
            new(68, "Oboe"),
            new(69, "English Horn"),
            new(70, "Bassoon"),
            new(71, "Clarinet"),

            // Pipe (72-79)
            new(72, "Piccolo"),
            new(73, "Flute"),
            new(74, "Recorder"),
            new(75, "Pan Flute"),
            new(76, "Blown Bottle"),
            new(77, "Shakuhachi"),
            new(78, "Whistle"),
            new(79, "Ocarina"),

            // Synth Lead (80-87)
            new(80, "Lead 1 (square)"),
            new(81, "Lead 2 (sawtooth)"),
            new(82, "Lead 3 (calliope)"),
            new(83, "Lead 4 (chiff)"),
            new(84, "Lead 5 (charang)"),
            new(85, "Lead 6 (voice)"),
            new(86, "Lead 7 (fifths)"),
            new(87, "Lead 8 (bass + lead)"),

            // Synth Pad (88-95)
            new(88, "Pad 1 (new age)"),
            new(89, "Pad 2 (warm)"),
            new(90, "Pad 3 (polysynth)"),
            new(91, "Pad 4 (choir)"),
            new(92, "Pad 5 (bowed)"),
            new(93, "Pad 6 (metallic)"),
            new(94, "Pad 7 (halo)"),
            new(95, "Pad 8 (sweep)"),

            // Synth Effects (96-103)
            new(96, "FX 1 (rain)"),
            new(97, "FX 2 (soundtrack)"),
            new(98, "FX 3 (crystal)"),
            new(99, "FX 4 (atmosphere)"),
            new(100, "FX 5 (brightness)"),
            new(101, "FX 6 (goblins)"),
            new(102, "FX 7 (echoes)"),
            new(103, "FX 8 (sci-fi)"),

            // Ethnic (104-111)
            new(104, "Sitar"),
            new(105, "Banjo"),
            new(106, "Shamisen"),
            new(107, "Koto"),
            new(108, "Kalimba"),
            new(109, "Bag pipe"),
            new(110, "Fiddle"),
            new(111, "Shanai"),

            // Percussive (112-119)
            new(112, "Tinkle Bell"),
            new(113, "Agogo"),
            new(114, "Steel Drums"),
            new(115, "Woodblock"),
            new(116, "Taiko Drum"),
            new(117, "Melodic Tom"),
            new(118, "Synth Drum"),
            new(119, "Reverse Cymbal"),

            // Sound Effects (120-127)
            new(120, "Guitar Fret Noise"),
            new(121, "Breath Noise"),
            new(122, "Seashore"),
            new(123, "Bird Tweet"),
            new(124, "Telephone Ring"),
            new(125, "Helicopter"),
            new(126, "Applause"),
            new(127, "Gunshot"),

            // Drums (Channel 10, program ignored) - sentinel entry for UI
            new(255, "Drum Set")
        };

        // AI: MidiVoiceList returns the prebuilt cached list instance; callers should not mutate it.
        public static List<MidiVoices> MidiVoiceList()
        {
            return s_cachedVoices;
        }
    }
}