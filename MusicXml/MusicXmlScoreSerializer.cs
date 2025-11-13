using MusicXml.Domain;
using System.Globalization;
using System.Xml;

namespace MusicXml
{
    public static class MusicXmlScoreSerializer
    {
        // Serializes a Score into MusicXML (score-partwise). Returns a pretty-printed XML string.
        // Indentation is for readability only; MusicXML accepts both spaces and tabs.
        // Default indentation: 2 spaces.
        public static string Serialize(Score score, bool indentWithTabs = false, int indentSpaces = 2)
        {
            if (score == null) throw new ArgumentNullException(nameof(score));

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = indentWithTabs ? "\t" : new string(' ', Math.Max(0, indentSpaces)),
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace,
                Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                OmitXmlDeclaration = false
            };

            using var ms = new MemoryStream();
            using (var writer = XmlWriter.Create(ms, settings))
            {
                // Emit XML declaration with standalone="no"
                writer.WriteStartDocument(standalone: false);
                writer.WriteDocType("score-partwise",
                    "-//Recordare//DTD MusicXML 3.1 Partwise//EN",
                    "http://www.musicxml.org/dtds/partwise.dtd",
                    null);

                writer.WriteStartElement("score-partwise");
                writer.WriteAttributeString("version", "3.1");

                // movement-title
                if (!string.IsNullOrWhiteSpace(score.MovementTitle))
                    writer.WriteElementString("movement-title", score.MovementTitle);

                // identification (composer, rights, encoding)
                if (score.Identification != null)
                {
                    var hasComposer = !string.IsNullOrWhiteSpace(score.Identification.Composer);
                    var hasRights = !string.IsNullOrWhiteSpace(score.Identification.Rights);
                    var enc = score.Identification.Encoding;
                    var hasEncoding = enc != null &&
                                      (!string.IsNullOrWhiteSpace(enc.Software) ||
                                       !string.IsNullOrWhiteSpace(enc.Description) ||
                                       enc.EncodingDate != default);

                    if (hasComposer || hasRights || hasEncoding)
                    {
                        writer.WriteStartElement("identification");

                        if (hasComposer)
                        {
                            writer.WriteStartElement("creator");
                            writer.WriteAttributeString("type", "composer");
                            writer.WriteString(score.Identification.Composer);
                            writer.WriteEndElement(); // creator
                        }

                        if (hasRights)
                            writer.WriteElementString("rights", score.Identification.Rights);

                        if (hasEncoding)
                        {
                            writer.WriteStartElement("encoding");
                            if (enc != null)
                            {
                                foreach (var line in SplitLines(enc.Software))
                                {
                                    writer.WriteElementString("software", line);
                                }

                                if (enc.EncodingDate != default)
                                    writer.WriteElementString("encoding-date", enc.EncodingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

                                foreach (var line in SplitLines(enc.Description))
                                {
                                    writer.WriteElementString("encoding-description", line);
                                }
                            }
                            writer.WriteEndElement(); // encoding
                        }

                        writer.WriteEndElement(); // identification
                    }
                }

                // part-list (emit instrument mapping like Notion's format)
                writer.WriteStartElement("part-list");
                foreach (var part in score.Parts ?? Enumerable.Empty<Part>())
                {
                    var partId = string.IsNullOrWhiteSpace(part.Id) ? "P1" : part.Id;

                    writer.WriteStartElement("score-part");
                    writer.WriteAttributeString("id", partId);
                    writer.WriteElementString("part-name",
                        string.IsNullOrWhiteSpace(part.Name) ? "Part" : part.Name);

                    // Optional instrument blocks
                    var instId = partId + "-I1";
                    var hasInstrumentName = !string.IsNullOrWhiteSpace(part.InstrumentName);
                    var hasMidiChannel = part.MidiChannel > 0;

                    if (hasInstrumentName)
                    {
                        writer.WriteStartElement("score-instrument");
                        writer.WriteAttributeString("id", instId);
                        writer.WriteElementString("instrument-name", part.InstrumentName);
                        writer.WriteEndElement(); // score-instrument
                    }

                    if (hasMidiChannel)
                    {
                        writer.WriteStartElement("midi-instrument");
                        writer.WriteAttributeString("id", instId);
                        writer.WriteElementString("midi-channel", part.MidiChannel.ToString());
                        writer.WriteEndElement(); // midi-instrument
                    }

                    writer.WriteEndElement(); // score-part
                }
                writer.WriteEndElement(); // part-list

                // Track whether we've written a Direction already. Per requirement: only the first measure serialized gets the Direction.
                var directionWritten = false;

                // parts and measures
                foreach (var part in score.Parts ?? Enumerable.Empty<Part>())
                {
                    var partId = string.IsNullOrWhiteSpace(part.Id) ? "P1" : part.Id;
                    writer.WriteStartElement("part");
                    writer.WriteAttributeString("id", partId);

                    var measures = part.Measures ?? new System.Collections.Generic.List<Measure>();
                    var measureNumber = 1;

                    foreach (var measure in measures)
                    {
                        writer.WriteStartElement("measure");
                        writer.WriteAttributeString("number", measureNumber.ToString());

                        // Optional width attribute if present (>0). Parser reads it.
                        if (measure.Width > 0)
                            writer.WriteAttributeString("width", measure.Width.ToString(CultureInfo.InvariantCulture));

                        // attributes (divisions, key, time, clef)
                        if (measure.Attributes != null)
                        {
                            var attr = measure.Attributes;
                            writer.WriteStartElement("attributes");

                            if (attr.Divisions > 0)
                                writer.WriteElementString("divisions", attr.Divisions.ToString());

                            // Write staves (for multi-staff instruments like Piano, Harp)
                            if (attr.Staves.HasValue && attr.Staves.Value > 1)
                            {
                                writer.WriteElementString("staves", attr.Staves.Value.ToString());
                            }

                            if (attr.Key != null)
                            {
                                writer.WriteStartElement("key");
                                writer.WriteElementString("fifths", attr.Key.Fifths.ToString());
                                if (!string.IsNullOrWhiteSpace(attr.Key.Mode))
                                    writer.WriteElementString("mode", attr.Key.Mode);
                                writer.WriteEndElement(); // key
                            }

                            if (attr.Time != null)
                            {
                                writer.WriteStartElement("time");
                                var sym = MapTimeSymbol(attr.Time.Symbol);
                                if (sym != null)
                                    writer.WriteAttributeString("symbol", sym);

                                if (attr.Time.Beats > 0)
                                    writer.WriteElementString("beats", attr.Time.Beats.ToString());
                                // Parser maps beat-type -> Time.Mode
                                if (!string.IsNullOrWhiteSpace(attr.Time.Mode))
                                    writer.WriteElementString("beat-type", attr.Time.Mode);
                                writer.WriteEndElement(); // time
                            }

                            if (attr.Clefs != null && attr.Clefs.Count > 0)
                            {
                                // New approach: write multiple clefs for multi-staff parts
                                foreach (var clef in attr.Clefs)
                                {
                                    writer.WriteStartElement("clef");

                                    // Add number attribute for multi-staff parts
                                    if (clef.Number.HasValue)
                                    {
                                        writer.WriteAttributeString("number", clef.Number.Value.ToString());
                                    }

                                    if (!string.IsNullOrWhiteSpace(clef.Sign))
                                        writer.WriteElementString("sign", clef.Sign);

                                    if (clef.Line > 0)
                                        writer.WriteElementString("line", clef.Line.ToString());

                                    if (clef.ClefOctaveChange.HasValue)
                                        writer.WriteElementString("clef-octave-change", clef.ClefOctaveChange.Value.ToString());

                                    writer.WriteEndElement(); // clef
                                }
                            }
                            else if (attr.Clef != null)
                            {
                                // Legacy single clef (backward compatibility for single-staff parts)
                                writer.WriteStartElement("clef");

                                if (!string.IsNullOrWhiteSpace(attr.Clef.Sign))
                                    writer.WriteElementString("sign", attr.Clef.Sign);

                                if (attr.Clef.Line > 0)
                                    writer.WriteElementString("line", attr.Clef.Line.ToString());

                                writer.WriteEndElement(); // clef
                            }

                            writer.WriteEndElement(); // attributes
                        }

                        // Write direction if present and not yet written for the score.
                        if (!directionWritten && measure.Direction != null)
                        {
                            writer.WriteStartElement("direction");

                            // direction-type (metronome)
                            writer.WriteStartElement("direction-type");
                            var dt = measure.Direction.DirectionType;
                            var met = dt?.Metronome;
                            if (met != null)
                            {
                                writer.WriteStartElement("metronome");
                                if (!string.IsNullOrWhiteSpace(met.BeatUnit))
                                    writer.WriteElementString("beat-unit", met.BeatUnit);
                                if (met.PerMinute > 0)
                                    writer.WriteElementString("per-minute", met.PerMinute.ToString());
                                writer.WriteEndElement(); // metronome
                            }
                            writer.WriteEndElement(); // direction-type

                            // sound (tempo attribute)
                            if (measure.Direction.Sound != null && measure.Direction.Sound.Tempo > 0)
                            {
                                writer.WriteStartElement("sound");
                                writer.WriteAttributeString("tempo", measure.Direction.Sound.Tempo.ToString(CultureInfo.InvariantCulture));
                                writer.WriteEndElement(); // sound
                            }

                            writer.WriteEndElement(); // direction

                            directionWritten = true;
                        }

                        // measure elements (notes, backup, forward)
                        foreach (var me in measure.MeasureElements ?? Enumerable.Empty<MeasureElement>())
                        {
                            switch (me.Type)
                            {
                                case MeasureElementType.Note when me.Element is Note note:
                                    WriteNote(writer, note);
                                    break;

                                case MeasureElementType.Backup when me.Element is Backup backup:
                                    writer.WriteStartElement("backup");
                                    writer.WriteElementString("duration", backup.Duration.ToString());
                                    writer.WriteEndElement();
                                    break;

                                case MeasureElementType.Forward when me.Element is Forward forward:
                                    writer.WriteStartElement("forward");
                                    writer.WriteElementString("duration", forward.Duration.ToString());
                                    writer.WriteEndElement();
                                    break;
                            }
                        }

                        writer.WriteEndElement(); // measure
                        measureNumber++;
                    }

                    writer.WriteEndElement(); // part
                }

                writer.WriteEndElement(); // score-partwise
                writer.WriteEndDocument();
            }

            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }

        private static void WriteNote(XmlWriter writer, Note note)
        {
            writer.WriteStartElement("note");

            // These match parser expectations and MusicXML order.
            if (note.IsGrace)
                writer.WriteElementString("grace", string.Empty);

            if (note.IsChordTone)
                writer.WriteElementString("chord", string.Empty);

            if (note.IsRest)
            {
                writer.WriteElementString("rest", string.Empty);
            }
            else if (note.Pitch != null)
            {
                writer.WriteStartElement("pitch");
                if (note.Pitch.Step != default)
                    writer.WriteElementString("step", note.Pitch.Step.ToString());
                if (note.Pitch.Alter != 0)
                    writer.WriteElementString("alter", note.Pitch.Alter.ToString());
                writer.WriteElementString("octave", note.Pitch.Octave.ToString());
                writer.WriteEndElement(); // pitch
            }

            // Tie as direct child (parser reads single <tie type="..."/>)
            if (note.Tie == Tie.Start)
            {
                writer.WriteStartElement("tie");
                writer.WriteAttributeString("type", "start");
                writer.WriteEndElement();
            }
            else if (note.Tie == Tie.Stop)
            {
                writer.WriteStartElement("tie");
                writer.WriteAttributeString("type", "stop");
                writer.WriteEndElement();
            }

            // Parser ignores duration on grace notes; we only emit for non-grace.
            if (!note.IsGrace && note.Duration >= 0)
                writer.WriteElementString("duration", note.Duration.ToString());

            if (note.Voice >= 0)
                writer.WriteElementString("voice", note.Voice.ToString());

            if (!string.IsNullOrWhiteSpace(note.Type))
                writer.WriteElementString("type", note.Type);

            if (!string.IsNullOrWhiteSpace(note.Accidental))
                writer.WriteElementString("accidental", note.Accidental);

            // Emit time-modification for tuplets (affects timing/playback)
            if (note.TimeModification != null)
            {
                writer.WriteStartElement("time-modification");
                writer.WriteElementString("actual-notes", note.TimeModification.ActualNotes.ToString());
                writer.WriteElementString("normal-notes", note.TimeModification.NormalNotes.ToString());
                if (!string.IsNullOrWhiteSpace(note.TimeModification.NormalType))
                    writer.WriteElementString("normal-type", note.TimeModification.NormalType);
                writer.WriteEndElement();
            }

            // Write staff before notations (MusicXML ordering expects staff prior to notations)
            if (note.Staff > 0)
                writer.WriteElementString("staff", note.Staff.ToString());

            // Emit tuplet notation (start/stop) inside <notations>
            // moved after <staff> to match reference MusicXML order
            if (note.TupletNotation != null && !string.IsNullOrWhiteSpace(note.TupletNotation.Type))
            {
                writer.WriteStartElement("notations");
                writer.WriteStartElement("tuplet");
                writer.WriteAttributeString("type", note.TupletNotation.Type);
                writer.WriteAttributeString("number", note.TupletNotation.Number.ToString());
                writer.WriteEndElement(); // tuplet
                writer.WriteEndElement(); // notations
            }

            // lyric
            if (note.Lyric != null &&
                (!string.IsNullOrWhiteSpace(note.Lyric.Text) || note.Lyric.Syllabic != Syllabic.None))
            {
                writer.WriteStartElement("lyric");
                var syllabic = MapSyllabic(note.Lyric.Syllabic);
                if (syllabic != null)
                    writer.WriteElementString("syllabic", syllabic);
                if (!string.IsNullOrWhiteSpace(note.Lyric.Text))
                    writer.WriteElementString("text", note.Lyric.Text);
                writer.WriteEndElement(); // lyric
            }

            writer.WriteEndElement(); // note
        }

        private static string? MapTimeSymbol(TimeSymbol symbol) =>
            symbol switch
            {
                TimeSymbol.Normal => null,           // omit
                TimeSymbol.Common => "common",
                TimeSymbol.Cut => "cut",
                TimeSymbol.SingleNumber => "single-number",
                _ => null
            };

        private static string? MapSyllabic(Syllabic s) =>
            s switch
            {
                Syllabic.None => null,
                Syllabic.Begin => "begin",
                Syllabic.Single => "single",
                Syllabic.End => "end",
                Syllabic.Middle => "middle",
                _ => null
            };

        private static string[] SplitLines(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? Array.Empty<string>()
                : value.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }
}