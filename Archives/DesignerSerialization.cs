//using System.Text.Json;

//namespace Music.Designer
//{
//    internal static class DesignerSerialization
//    {
//        internal static SongContext DeserializeDesign(string json)
//        {
//            var dto = JsonSerializer.Deserialize<DesignDto>(json, new JsonSerializerOptions
//            {
//                PropertyNameCaseInsensitive = true
//            });
//            if (dto == null)
//                throw new InvalidOperationException("Invalid design JSON.");

//            var design = new SongContext(dto.DesignId);

//            // Voices (preserve order as given in JSON)
//            design.Voices.Reset();
//            if (dto.VoiceSet?.Voices != null)
//            {
//                foreach (var v in dto.VoiceSet.Voices)
//                {
//                    if (!string.IsNullOrWhiteSpace(v?.VoiceName))
//                        design.Voices.AddVoice(v.VoiceName);
//                }
//            }

//            // Sections: preserve StartBar and order from JSON (no recalculation)
//            design.SectionTrack.Reset();
//            if (dto.SectionSet?.Sections != null)
//            {
//                foreach (var s in dto.SectionSet.Sections)
//                {
//                    design.SectionTrack.Sections.Add(new Section
//                    {
//                        SectionType = (MusicConstants.eSectionType)s.SectionType,
//                        StartBar = s.StartBar > 0 ? s.StartBar : 1,
//                        BarCount = s.BarCount > 0 ? s.BarCount : 1,
//                        Name = s.Name,
//                        // Id is runtime-only; keep a new Guid or copy if needed
//                    });
//                }
//                // Sync internal state based on existing StartBar/BarCount without changing them
//                design.SectionTrack.SyncAfterExternalLoad();
//            }

//            // Harmony timeline: preserve order and values
//            if (dto.HarmonyTimeline != null)
//            {
//                var tl = new HarmonyTrack
//                {
//                    BeatsPerBar = dto.HarmonyTimeline.BeatsPerBar > 0 ? dto.HarmonyTimeline.BeatsPerBar : 4
//                    // TempoBpm removed from HarmonyTrack; dto.HarmonyTrack.TempoBpm is ignored (migrated to TempoTrack separately)
//                };
//                if (dto.HarmonyTimeline.Events != null)
//                {
//                    foreach (var e in dto.HarmonyTimeline.Events)
//                    {
//                        tl.Events.Add(new HarmonyEvent
//                        {
//                            StartBar = e.StartBar,
//                            StartBeat = e.StartBeat,
//                            DurationBeats = e.DurationBeats,
//                            Key = e.Key,
//                            Degree = e.Degree,
//                            Quality = e.Quality,
//                            Bass = e.Bass
//                        });
//                    }
//                }
//                // Build index without changing the list or StartBar values
//                tl.EnsureIndexed();
//                design.HarmonyTrack = tl;
//            }

//            return design;
//        }

//        // DTOs matching the JSON shape
//        private sealed class DesignDto
//        {
//            public string? DesignId { get; set; }
//            public VoiceSetDto? VoiceSet { get; set; }
//            public SectionSetDto? SectionSet { get; set; }
//            public HarmonyTimelineDto? HarmonyTimeline { get; set; }
//        }

//        private sealed class VoiceSetDto
//        {
//            public List<Voice>? Voices { get; set; }
//        }

//        private sealed class SectionSetDto
//        {
//            public List<SectionDto>? Sections { get; set; }
//            public int TotalBars { get; set; }
//        }

//        private sealed class SectionDto
//        {
//            public int SectionType { get; set; }
//            public int StartBar { get; set; }
//            public int BarCount { get; set; }
//            public string? Name { get; set; }
//            public Guid Id { get; set; }
//        }

//        private sealed class HarmonyTimelineDto
//        {
//            public int BeatsPerBar { get; set; }
//            // Deprecated: was stored alongside Harmony; retained for back-compat but ignored here.
//            public List<HarmonyEvent>? Events { get; set; }
//        }
//    }
//}
