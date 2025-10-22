using System.Text.Json;

namespace Music.Design
{
    internal static class DesignSerialization
    {
        internal static DesignClass DeserializeDesign(string json)
        {
            var dto = JsonSerializer.Deserialize<DesignDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (dto == null)
                throw new InvalidOperationException("Invalid design JSON.");

            var design = new DesignClass(dto.DesignId);

            // Voices
            design.VoiceSet.Reset();
            if (dto.VoiceSet?.Voices != null)
            {
                foreach (var v in dto.VoiceSet.Voices)
                {
                    if (!string.IsNullOrWhiteSpace(v?.VoiceName))
                        design.VoiceSet.AddVoice(v.VoiceName);
                }
            }

            // Sections (compute StartBar via Add)
            design.SectionSet.Reset();
            if (dto.SectionSet?.Sections != null)
            {
                foreach (var s in dto.SectionSet.Sections)
                {
                    design.SectionSet.Add((MusicEnums.eSectionType)s.SectionType, s.BarCount > 0 ? s.BarCount : 1, s.Name);
                }
            }

            // Harmonic timeline
            if (dto.HarmonicTimeline != null)
            {
                var tl = new HarmonicTimeline();
                tl.ConfigureGlobal($"{dto.HarmonicTimeline.BeatsPerBar}/4", dto.HarmonicTimeline.TempoBpm);
                if (dto.HarmonicTimeline.Events != null)
                {
                    foreach (var e in dto.HarmonicTimeline.Events)
                    {
                        // Ensure each event is inserted to build index
                        tl.Add(new HarmonicEvent
                        {
                            StartBar = e.StartBar,
                            StartBeat = e.StartBeat,
                            DurationBeats = e.DurationBeats,
                            Key = e.Key,
                            Degree = e.Degree,
                            Quality = e.Quality,
                            Bass = e.Bass
                        });
                    }
                }
                design.HarmonicTimeline = tl;
            }

            return design;
        }

        // DTOs matching the JSON shape
        private sealed class DesignDto
        {
            public string? DesignId { get; set; }
            public VoiceSetDto? VoiceSet { get; set; }
            public SectionSetDto? SectionSet { get; set; }
            public HarmonicTimelineDto? HarmonicTimeline { get; set; }
        }

        private sealed class VoiceSetDto
        {
            public List<VoiceClass>? Voices { get; set; }
        }

        private sealed class SectionSetDto
        {
            public List<SectionDto>? Sections { get; set; }
            public int TotalBars { get; set; }
        }

        private sealed class SectionDto
        {
            public int SectionType { get; set; }
            public int StartBar { get; set; }
            public int BarCount { get; set; }
            public string? Name { get; set; }
            public Guid Id { get; set; }
        }

        private sealed class HarmonicTimelineDto
        {
            public int BeatsPerBar { get; set; }
            public int TempoBpm { get; set; }
            public List<HarmonicEvent>? Events { get; set; }
        }
    }
}
