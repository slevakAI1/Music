// AI: purpose=Helper methods for golden test fixture creation and snapshot comparison.
// AI: deps=SongContext, SectionTrack, BarTrack, GrooveSetupFactory, StyleConfigurationLibrary.
// AI: change=Story 10.8.3: End-to-end regression snapshot (golden test).

using Music.Generator;
using Music.Generator.Agents.Common;
using Music.Generator.Agents.Drums;
using Music.Generator.Groove;
using Music.MyMidi;
using System.Text;
using System.Text.Json;

namespace Music.Tests.Generator.Agents.Drums.Snapshots
{
    public static class GoldenTestHelpers
    {
        public const int ExpectedSchemaVersion = 1;
        private const int MaxDifferencesToReport = 10;

        public static SongContext CreateStandardFixture(int seed)
        {
            Rng.Initialize(seed);
            
            var songContext = new SongContext();

            songContext.SectionTrack = new SectionTrack();
            songContext.SectionTrack.Add(MusicConstants.eSectionType.Intro, barCount: 4);
            songContext.SectionTrack.Add(MusicConstants.eSectionType.Verse, barCount: 8);
            songContext.SectionTrack.Add(MusicConstants.eSectionType.Chorus, barCount: 8);
            songContext.SectionTrack.Add(MusicConstants.eSectionType.Verse, barCount: 8);
            songContext.SectionTrack.Add(MusicConstants.eSectionType.Chorus, barCount: 8);
            songContext.SectionTrack.Add(MusicConstants.eSectionType.Bridge, barCount: 4);
            songContext.SectionTrack.Add(MusicConstants.eSectionType.Chorus, barCount: 8);
            songContext.SectionTrack.Add(MusicConstants.eSectionType.Outro, barCount: 4);

            int totalBars = songContext.SectionTrack.TotalBars;

            songContext.Song.TimeSignatureTrack = new Timingtrack();
            var timeSignatureEvent = new TimingEvent { StartBar = 1, Numerator = 4, Denominator = 4 };
            songContext.Song.TimeSignatureTrack.Events.Add(timeSignatureEvent);

            songContext.BarTrack = new BarTrack();
            songContext.BarTrack.RebuildFromTimingTrack(songContext.Song.TimeSignatureTrack, totalBars);

            IReadOnlyList<SegmentGrooveProfile> segmentProfiles;
            songContext.GroovePresetDefinition = GrooveSetupFactory.BuildPopRockBasicGrooveForTestSong(
                songContext.SectionTrack,
                out segmentProfiles,
                beatsPerBar: 4);

            songContext.SegmentGrooveProfiles = segmentProfiles;

            songContext.Voices = new VoiceSet();
            songContext.Voices.AddVoice("Standard Kit", "DrumKit");

            return songContext;
        }

        public static GoldenSnapshot PartTrackToSnapshot(
            PartTrack track,
            SongContext songContext,
            int seed,
            string styleId)
        {
            var snapshot = new GoldenSnapshot
            {
                SchemaVersion = ExpectedSchemaVersion,
                Seed = seed,
                StyleId = styleId,
                TotalBars = songContext.SectionTrack.TotalBars,
                Bars = new List<BarSnapshot>()
            };

            int ticksPerBeat = MusicConstants.TicksPerQuarterNote;
            int ticksPerBar = ticksPerBeat * 4;

            var eventsByBar = track.PartTrackNoteEvents
                .GroupBy(e => (int)(e.AbsoluteTimeTicks / ticksPerBar) + 1)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.AbsoluteTimeTicks).ToList());

            for (int barNumber = 1; barNumber <= songContext.SectionTrack.TotalBars; barNumber++)
            {
                songContext.SectionTrack.GetActiveSection(barNumber, out var section);
                string sectionType = section?.SectionType.ToString() ?? "Unknown";

                var barEvents = eventsByBar.TryGetValue(barNumber, out var events) 
                    ? events 
                    : new List<PartTrackEvent>();

                var barSnapshot = new BarSnapshot
                {
                    BarNumber = barNumber,
                    SectionType = sectionType,
                    Events = new List<EventSnapshot>(),
                    OperatorsUsed = new List<string>()
                };

                var operatorsInBar = new HashSet<string>();

                foreach (var ev in barEvents)
                {
                    long barStartTick = (barNumber - 1) * ticksPerBar;
                    long tickWithinBar = ev.AbsoluteTimeTicks - barStartTick;
                    decimal beat = 1.0m + (decimal)tickWithinBar / ticksPerBeat;

                    string role = MapMidiNoteToRole(ev.NoteNumber);
                    string provenance = "Anchor";

                    operatorsInBar.Add(provenance);

                    barSnapshot.Events.Add(new EventSnapshot
                    {
                        Beat = Math.Round(beat, 4),
                        Role = role,
                        Velocity = ev.NoteOnVelocity,
                        TimingOffset = 0,
                        Provenance = provenance
                    });
                }

                barSnapshot.OperatorsUsed.AddRange(operatorsInBar.OrderBy(o => o));
                snapshot.Bars.Add(barSnapshot);
            }

            return snapshot;
        }

        public static string SerializeSnapshot(GoldenSnapshot snapshot)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            return JsonSerializer.Serialize(snapshot, options);
        }

        public static GoldenSnapshot DeserializeSnapshot(string json)
        {
            return JsonSerializer.Deserialize<GoldenSnapshot>(json)
                ?? throw new InvalidOperationException("Failed to deserialize snapshot");
        }

        public static (bool IsMatch, string DiffReport) CompareSnapshots(
            GoldenSnapshot expected,
            GoldenSnapshot actual)
        {
            if (expected.SchemaVersion != ExpectedSchemaVersion)
            {
                return (false, $"Snapshot schema version mismatch: expected {ExpectedSchemaVersion}, got {expected.SchemaVersion}. Regenerate snapshot with UPDATE_SNAPSHOTS=true.");
            }

            var differences = new List<string>();

            if (expected.Seed != actual.Seed)
                differences.Add($"Seed mismatch: expected {expected.Seed}, actual {actual.Seed}");

            if (expected.StyleId != actual.StyleId)
                differences.Add($"StyleId mismatch: expected {expected.StyleId}, actual {actual.StyleId}");

            if (expected.TotalBars != actual.TotalBars)
                differences.Add($"TotalBars mismatch: expected {expected.TotalBars}, actual {actual.TotalBars}");

            int maxBars = Math.Max(expected.Bars.Count, actual.Bars.Count);
            for (int i = 0; i < maxBars && differences.Count < 100; i++)
            {
                if (i >= expected.Bars.Count)
                {
                    differences.Add($"Bar {i + 1}: Extra bar in actual (not in expected)");
                    continue;
                }
                if (i >= actual.Bars.Count)
                {
                    differences.Add($"Bar {i + 1}: Missing bar in actual");
                    continue;
                }

                var expectedBar = expected.Bars[i];
                var actualBar = actual.Bars[i];

                if (expectedBar.BarNumber != actualBar.BarNumber)
                    differences.Add($"Bar {i + 1}: BarNumber mismatch: expected {expectedBar.BarNumber}, actual {actualBar.BarNumber}");

                if (expectedBar.SectionType != actualBar.SectionType)
                    differences.Add($"Bar {expectedBar.BarNumber}: SectionType mismatch: expected {expectedBar.SectionType}, actual {actualBar.SectionType}");

                CompareEvents(expectedBar, actualBar, differences);
            }

            if (differences.Count == 0)
                return (true, string.Empty);

            var report = new StringBuilder();
            report.AppendLine("Snapshot mismatch detected:");
            report.AppendLine($"- Total differences: {differences.Count}");
            report.AppendLine($"- First {Math.Min(MaxDifferencesToReport, differences.Count)} differences:");

            for (int i = 0; i < Math.Min(MaxDifferencesToReport, differences.Count); i++)
            {
                report.AppendLine($"  {i + 1}. {differences[i]}");
            }

            if (differences.Count > MaxDifferencesToReport)
            {
                report.AppendLine($"  ... and {differences.Count - MaxDifferencesToReport} more differences");
            }

            return (false, report.ToString());
        }

        private static void CompareEvents(BarSnapshot expectedBar, BarSnapshot actualBar, List<string> differences)
        {
            int maxEvents = Math.Max(expectedBar.Events.Count, actualBar.Events.Count);

            for (int j = 0; j < maxEvents && differences.Count < 100; j++)
            {
                if (j >= expectedBar.Events.Count)
                {
                    var extra = actualBar.Events[j];
                    differences.Add($"Bar {expectedBar.BarNumber}, Beat {extra.Beat}: Extra event in actual ({extra.Role})");
                    continue;
                }
                if (j >= actualBar.Events.Count)
                {
                    var missing = expectedBar.Events[j];
                    differences.Add($"Bar {expectedBar.BarNumber}, Beat {missing.Beat}: Missing event in actual ({missing.Role})");
                    continue;
                }

                var exp = expectedBar.Events[j];
                var act = actualBar.Events[j];

                if (exp.Beat != act.Beat)
                    differences.Add($"Bar {expectedBar.BarNumber}, Event {j + 1}: Beat mismatch: expected {exp.Beat}, actual {act.Beat}");

                if (exp.Role != act.Role)
                    differences.Add($"Bar {expectedBar.BarNumber}, Beat {exp.Beat}: Role mismatch: expected {exp.Role}, actual {act.Role}");

                if (exp.Velocity != act.Velocity)
                    differences.Add($"Bar {expectedBar.BarNumber}, Beat {exp.Beat}: Velocity mismatch: expected {exp.Velocity}, actual {act.Velocity}");

                if (exp.TimingOffset != act.TimingOffset)
                    differences.Add($"Bar {expectedBar.BarNumber}, Beat {exp.Beat}: TimingOffset mismatch: expected {exp.TimingOffset}, actual {act.TimingOffset}");

                if (exp.Provenance != act.Provenance)
                    differences.Add($"Bar {expectedBar.BarNumber}, Beat {exp.Beat}: Provenance mismatch: expected {exp.Provenance}, actual {act.Provenance}");
            }
        }

        private static string MapMidiNoteToRole(int midiNote)
        {
            return midiNote switch
            {
                36 => "Kick",
                38 => "Snare",
                40 => "Snare",
                42 => "ClosedHat",
                44 => "HiHatPedal",
                46 => "OpenHat",
                49 => "Crash",
                51 => "Ride",
                53 => "RideBell",
                41 => "FloorTom",
                43 => "FloorTom",
                45 => "LowTom",
                47 => "MidTom",
                48 => "HighTom",
                50 => "HighTom",
                _ => $"Unknown:{midiNote}"
            };
        }
    }
}
