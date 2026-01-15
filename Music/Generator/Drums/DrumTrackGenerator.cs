// AI: purpose=Generate drum track: kick, snare, hi-hat, ride using DrumVariationEngine for living performance.
/// AI: invariants=Calls DrumVariationEngine per bar; integrates DrumFillEngine at section transitions; converts to MIDI PartTrackEvent list; returns sorted by AbsoluteTimeTicks.
/// AI: deps=Uses DrumVariationEngine, DrumFillEngine, RandomHelpers, PitchRandomizer.SelectDrumVelocity for velocity shaping.
/// AI: uses fixed variation parameters; no tension/energy dependency.

using Music.MyMidi;
using Music.Song.Material;

namespace Music.Generator
{
    internal static class DrumTrackGenerator
    {
        /// <summary>
        /// Generates drum track: kick, snare, hi-hat, ride with deterministic variations and fills.
        /// Uses fixed variation parameters (no tension/energy dependency).
        /// Accepts motifPresence for ducking awareness.
        /// </summary>
        public static PartTrack Generate(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            SectionTrack sectionTrack,
            MotifPlacementPlan? motifPlan,
            MotifPresenceMap? motifPresence,
            int totalBars,
            RandomizationSettings settings,
            int midiProgramNumber)
        {

            var notes = new List<PartTrackEvent>();
            var randomizer = new PitchRandomizer(settings);

            // MIDI drum note numbers (General MIDI)
            const int kickNote = 36;
            const int snareNote = 38;
            const int closedHiHatNote = 42;
            const int openHiHatNote = 46;
            const int rideCymbalNote = 51;
            const int crashCymbal1Note = 49;  // Crash Cymbal 1
            const int crashCymbal2Note = 57;  // Crash Cymbal 2
            const int tomHighNote = 50;       // High tom
            const int tomMidNote = 47;        // Mid tom
            const int tomLowNote = 45;        // Low tom

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var grooveEvent = grooveTrack.GetActiveGrooveEvent(bar);

                // Get section type and index for variation engine and fill engine
                MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse; // Default
                int sectionIndex = 0;
                int absoluteSectionIndex = 0;
                Section? section = null;
                if (sectionTrack.GetActiveSection(bar, out section) && section != null)
                {
                    sectionType = section.SectionType;
                    sectionIndex = section.SectionId;
                    absoluteSectionIndex = sectionTrack.Sections.IndexOf(section);
                    if (absoluteSectionIndex < 0)
                        absoluteSectionIndex = 0;
                }

                // Use fixed approach for tension hooks (no tension/energy variation)
                int barIndexWithinSection = section != null ? (bar - section.StartBar) : 0;
                // Use fixed pull probability bias (no tension/energy variation)
                double pullProbabilityBias = 0.0;

                // Use default drum parameters
                var drumParameters = settings.DrumParameters ?? new DrumRoleParameters();

                // Story 9.3: Check if lead motif is active for ducking
                bool hasLeadMotif = motifPresence?.HasLeadMotif(absoluteSectionIndex, barIndexWithinSection) ?? false;
                double duckingMultiplier = hasLeadMotif ? 0.7 : 1.0; // Thin optional hits by 30% when lead motif active

                // Create deterministic per-bar RNG so fill decisions remain deterministic when knobs change.
                var barRng = RandomHelpers.CreateLocalRng(settings.Seed, $"{grooveEvent.SourcePresetName ?? "groove"}_{sectionType}", bar, 0m);

                // Phrase-end pull biases optional fill chance (with null tension query, bias is 0.0); this must not override transition fills.
                double tensionFillExtraProbability = Math.Clamp(pullProbabilityBias, 0.0, 0.20);

                // Check if this bar should have a fill (Story 6.3) or if Stage 7 requests extra fills via parameter.
                bool shouldFill = DrumFillEngine.ShouldGenerateFill(bar, totalBars, sectionTrack)
                    || barRng.NextDouble() < (drumParameters?.FillProbability ?? 0.0) + tensionFillExtraProbability;

                List<DrumVariationEngine.DrumHit> allHits;

                if (shouldFill)
                {
                    // Generate fill instead of normal variation
                    allHits = DrumFillEngine.GenerateFill(
                        bar,
                        grooveEvent.SourcePresetName ?? "default",
                        sectionType,
                        sectionIndex,
                        settings.Seed,
                        totalBars,
                        drumParameters);

                    // Safety: a fill should maintain pulse/timekeeping unless intentionally designed as a "stop-time" break.
                    // Real drummers keep at least hat/ride going during fills to anchor the band.
                    // Count timekeeping hits (hat, ride, kick on downbeats) to determine if groove needs merging.
                    int timekeepingHits = allHits.Count(h => 
                        h.Role == "hat" || 
                        h.Role == "ride" || 
                        (h.Role == "kick" && (h.OnsetBeat == 1m || h.OnsetBeat == 3m)));

                    // If fill has no timekeeping, merge in the base groove so the band doesn't lose pulse.
                    if (timekeepingHits == 0)
                    {
                        var grooveVariation = DrumVariationEngine.Generate(
                            grooveEvent,
                            sectionType,
                            bar,
                            settings.Seed,
                            drumParameters);

                        // Merge, keeping deterministic uniqueness by (Role, OnsetBeat, TimingOffsetTicks).
                        // Fills take priority (added first), groove fills in the gaps.
                        allHits = allHits
                            .Concat(grooveVariation.Hits)
                            .GroupBy(h => (h.Role, h.OnsetBeat, h.TimingOffsetTicks))
                            .Select(g => g.First())
                            .ToList();
                    }
                }
                else
                {
                    // Generate per-bar variation plan using DrumVariationEngine
                    var variation = DrumVariationEngine.Generate(grooveEvent, sectionType, bar, settings.Seed, drumParameters);
                    allHits = variation.Hits;

                    // Story 7.5.5: Phrase-end dropout option (bias-only): thin timekeeping late in bar when tension high.
                    // Guardrails: never remove anchor/main hits; never remove kick/snare; only remove non-main hat hits on late subdivisions.
                    // No tension/energy bias: always false
                    if (false)
                    {
                        double dropProb = 0.0;
                        if (barRng.NextDouble() < dropProb)
                        {
                            allHits = allHits
                                .Where(h =>
                                    h.Role != "hat"
                                    || h.IsMain
                                    || h.OnsetBeat < 4m)
                                .ToList();
                        }
                    }
                }

                // Add cymbal orchestration
                var cymbalHits = CymbalOrchestrationEngine.GenerateCymbalHits(
                    bar,
                    totalBars,
                    sectionTrack,
                    sectionType,
                    grooveEvent.SourcePresetName ?? "default",
                    settings.Seed);

                // Convert cymbal hits to DrumHit format and add to allHits
                foreach (var cymbalHit in cymbalHits)
                {
                    string role = cymbalHit.Type switch
                    {
                        "crash1" => "crash_cymbal_1",
                        "crash2" => "crash_cymbal_2",
                        "choke" => "crash_cymbal_1",  // Use crash1 for choke (short duration)
                        _ => "crash_cymbal_1"
                    };

                    bool isChoke = cymbalHit.Type == "choke";

                    allHits.Add(new DrumVariationEngine.DrumHit
                    {
                        Role = role,
                        OnsetBeat = cymbalHit.OnsetBeat,
                        IsMain = true,
                        TimingOffsetTicks = cymbalHit.TimingOffsetTicks,
                        IsInFill = false,
                        FillProgress = 0.0,
                        IsChoke = isChoke
                    });
                }

                // Story 9.3: Apply ducking when lead motif active (thin optional hat hits)
                if (hasLeadMotif)
                {
                    var localRng = RandomHelpers.CreateLocalRng(settings.Seed, $"duck_{bar}", bar, 0m);
                    allHits = allHits.Where(h =>
                    {
                        // Never remove groove anchors (main hits, kick, snare)
                        if (h.IsMain || h.Role == "kick" || h.Role == "snare")
                            return true;

                        // Thin optional hat hits deterministically
                        if (h.Role == "hat")
                            return localRng.NextDouble() < duckingMultiplier;

                        // Keep all other hits
                        return true;
                    }).ToList();
                }

                // Convert hits to MIDI events
                foreach (var hit in allHits)
                {
                    // Build onset grid to resolve beat position to ticks
                    var singleOnsetList = new List<decimal> { hit.OnsetBeat };
                    var slots = OnsetGrid.Build(bar, singleOnsetList, barTrack);
                    if (slots == null || slots.Count == 0)
                        continue;

                    var slot = slots[0];
                    int baseTick = (int)slot.StartTick + hit.TimingOffsetTicks;

                    // Clamp to zero to avoid negative absolute ticks
                    baseTick = Math.Max(0, baseTick);

                    switch (hit.Role)
                    {
                        case "kick":
                            {
                                bool isStrongBeat = RandomHelpers.IsStrongBeat(slot.OnsetBeat);
                                int baseVel = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "kick", baseVelocity: 100);
                                
                                // Apply velocity shaping
                                int vel = DrumVelocityShaper.ShapeVelocity(
                                    role: "kick",
                                    baseVelocity: baseVel,
                                    bar: slot.Bar,
                                    onsetBeat: slot.OnsetBeat,
                                    seed: settings.Seed,
                                    sectionType: sectionType,
                                    isStrongBeat: isStrongBeat,
                                    isGhost: false,
                                    isInFill: hit.IsInFill,
                                    fillProgress: hit.FillProgress);
                                

                                // Apply DrumRoleParameters velocity bias (Story 6.5 hook). Keep deterministic and simple.
                                if (drumParameters != null && Math.Abs(drumParameters.VelocityBias) > 0.0001)
                                {
                                    vel = Math.Clamp(vel + (int)Math.Round(drumParameters.VelocityBias), 1, 127);
                                }

                                // Reduce velocity for non-main kicks
                                if (!hit.IsMain)
                                    vel = Math.Max(25, (int)(vel * 0.82));
                                
                                var noteDuration = MusicConstants.TicksPerQuarterNote;

                                // Prevent overlap: trim previous notes of the same pitch that would extend past this note-on
                                NoteOverlapHelper.PreventOverlap(notes, kickNote, baseTick);

                                notes.Add(new PartTrackEvent(
                                    noteNumber: kickNote,
                                    absoluteTimeTicks: baseTick,
                                    noteDurationTicks: noteDuration,
                                    noteOnVelocity: vel));
                                break;
                            }

                        case "snare":
                            {
                                bool isStrongBeat = RandomHelpers.IsStrongBeat(slot.OnsetBeat);

                                if (hit.IsFlam)
                                {
                                    // Flam pre-hit: use dedicated flam velocity method
                                    int mainVel = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "snare", baseVelocity: 90);
                                    int preVel = DrumVelocityShaper.FlamPreHitVelocity(mainVel, settings.Seed, slot.Bar, slot.OnsetBeat);
                                    
                                    // Apply velocity bias to flam pre-hit
                                    if (drumParameters != null && Math.Abs(drumParameters.VelocityBias) > 0.0001)
                                    {
                                        preVel = Math.Clamp(preVel + (int)Math.Round(drumParameters.VelocityBias), 1, 127);
                                    }
                                    
                                    var noteDuration = MusicConstants.TicksPerQuarterNote;

                                    // Prevent overlap: trim previous notes of the same pitch
                                    NoteOverlapHelper.PreventOverlap(notes, snareNote, baseTick);

                                    notes.Add(new PartTrackEvent(
                                        noteNumber: snareNote,
                                        absoluteTimeTicks: baseTick,
                                        noteDurationTicks: noteDuration,
                                        noteOnVelocity: preVel));
                                }
                                else if (hit.IsGhost)
                                {
                                    // Ghost note: use dedicated ghost velocity method
                                    int baseVel = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "snare", baseVelocity: 40);
                                    int vel = DrumVelocityShaper.GhostNoteVelocity(baseVel, settings.Seed, slot.Bar, slot.OnsetBeat);
                                    
                                    // Apply velocity bias to ghost notes
                                    if (drumParameters != null && Math.Abs(drumParameters.VelocityBias) > 0.0001)
                                    {
                                        vel = Math.Clamp(vel + (int)Math.Round(drumParameters.VelocityBias), 1, 127);
                                    }
                                    
                                    var noteDuration = MusicConstants.TicksPerQuarterNote;

                                    // Prevent overlap: trim previous notes of the same pitch
                                    NoteOverlapHelper.PreventOverlap(notes, snareNote, baseTick);

                                    notes.Add(new PartTrackEvent(
                                        noteNumber: snareNote,
                                        absoluteTimeTicks: baseTick,
                                        noteDurationTicks: noteDuration,
                                        noteOnVelocity: vel));
                                }
                                else
                                {
                                    // Main snare hit
                                    int baseVel = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "snare", baseVelocity: 90);
                                    
                                    // Apply velocity shaping
                                    int vel = DrumVelocityShaper.ShapeVelocity(
                                        role: "snare",
                                        baseVelocity: baseVel,
                                        bar: slot.Bar,
                                        onsetBeat: slot.OnsetBeat,
                                        seed: settings.Seed,
                                        sectionType: sectionType,
                                        isStrongBeat: isStrongBeat,
                                        isGhost: false,
                                        isInFill: hit.IsInFill,
                                        fillProgress: hit.FillProgress);
                                    
                                    // Apply velocity bias
                                    if (drumParameters != null && Math.Abs(drumParameters.VelocityBias) > 0.0001)
                                    {
                                        vel = Math.Clamp(vel + (int)Math.Round(drumParameters.VelocityBias), 1, 127);
                                    }
                                    
                                    if (!hit.IsMain)
                                        vel = Math.Max(30, (int)(vel * 0.85));
                                    
                                    var noteDuration = MusicConstants.TicksPerQuarterNote;

                                    // Prevent overlap: trim previous notes of the same pitch
                                    NoteOverlapHelper.PreventOverlap(notes, snareNote, baseTick);

                                    notes.Add(new PartTrackEvent(
                                        noteNumber: snareNote,
                                        absoluteTimeTicks: baseTick,
                                        noteDurationTicks: noteDuration,
                                        noteOnVelocity: vel));
                                }
                                break;
                            }

                        case "hat":
                            {
                                bool isStrongBeat = RandomHelpers.IsStrongBeat(slot.OnsetBeat);
                                int baseVel = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "hat", baseVelocity: 70);

                                // Apply velocity shaping with hand pattern accents
                                int vel = DrumVelocityShaper.ShapeVelocity(
                                    role: "hat",
                                    baseVelocity: baseVel,
                                    bar: slot.Bar,
                                    onsetBeat: slot.OnsetBeat,
                                    seed: settings.Seed,
                                    sectionType: sectionType,
                                    isStrongBeat: isStrongBeat,
                                    isGhost: false,
                                    isInFill: hit.IsInFill,
                                    fillProgress: hit.FillProgress);

                                // Apply velocity bias
                                if (drumParameters != null && Math.Abs(drumParameters.VelocityBias) > 0.0001)
                                {
                                    vel = Math.Clamp(vel + (int)Math.Round(drumParameters.VelocityBias), 1, 127);
                                }
                                
                                // Soften non-main hats
                                if (!hit.IsMain) 
                                    vel = Math.Max(20, (int)(vel * 0.85));

                                // Open vs closed articulation
                                int noteNumber = hit.IsOpenHat ? openHiHatNote : closedHiHatNote;
                                var noteDuration = MusicConstants.TicksPerQuarterNote / 2;

                                // Prevent overlap: trim previous notes of the same pitch (open/closed hats are different pitches)
                                NoteOverlapHelper.PreventOverlap(notes, noteNumber, baseTick);

                                notes.Add(new PartTrackEvent(
                                    noteNumber: noteNumber,
                                    absoluteTimeTicks: baseTick,
                                    noteDurationTicks: noteDuration,
                                    noteOnVelocity: vel));
                                break;
                            }

                        case "ride":
                            {
                                bool isStrongBeat = RandomHelpers.IsStrongBeat(slot.OnsetBeat);
                                int baseVel = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "ride", baseVelocity: 75);

                                // Apply velocity shaping with hand pattern accents
                                int vel = DrumVelocityShaper.ShapeVelocity(
                                    role: "ride",
                                    baseVelocity: baseVel,
                                    bar: slot.Bar,
                                    onsetBeat: slot.OnsetBeat,
                                    seed: settings.Seed,
                                    sectionType: sectionType,
                                    isStrongBeat: isStrongBeat,
                                    isGhost: false,
                                    isInFill: hit.IsInFill,
                                    fillProgress: hit.FillProgress);

                                // Apply velocity bias
                                if (drumParameters != null && Math.Abs(drumParameters.VelocityBias) > 0.0001)
                                {
                                    vel = Math.Clamp(vel + (int)Math.Round(drumParameters.VelocityBias), 1, 127);
                                }
                                
                                // Soften non-main ride hits
                                if (!hit.IsMain) 
                                    vel = Math.Max(25, (int)(vel * 0.85));

                                var noteDuration = MusicConstants.TicksPerQuarterNote;

                                // Prevent overlap: trim previous notes of the same pitch
                                NoteOverlapHelper.PreventOverlap(notes, rideCymbalNote, baseTick);

                                notes.Add(new PartTrackEvent(
                                    noteNumber: rideCymbalNote,
                                    absoluteTimeTicks: baseTick,
                                    noteDurationTicks: noteDuration,
                                    noteOnVelocity: vel));
                                break;
                            }

                        case "tom_high":
                        case "tom_mid":
                        case "tom_low":
                            {
                                // Tom hits (used in fills)
                                int tomNote = hit.Role switch
                                {
                                    "tom_high" => tomHighNote,
                                    "tom_low" => tomLowNote,
                                    _ => tomMidNote
                                };

                                bool isStrongBeat = RandomHelpers.IsStrongBeat(slot.OnsetBeat);
                                int baseVel = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "snare", baseVelocity: 85);

                                // Apply velocity shaping (use snare-like shaping for toms)
                                int vel = DrumVelocityShaper.ShapeVelocity(
                                    role: "snare",
                                    baseVelocity: baseVel,
                                    bar: slot.Bar,
                                    onsetBeat: slot.OnsetBeat,
                                    seed: settings.Seed,
                                    sectionType: sectionType,
                                    isStrongBeat: isStrongBeat,
                                    isGhost: false,
                                    isInFill: hit.IsInFill,
                                    fillProgress: hit.FillProgress);

                                // Apply velocity bias to toms
                                if (drumParameters != null && Math.Abs(drumParameters.VelocityBias) > 0.0001)
                                {
                                    vel = Math.Clamp(vel + (int)Math.Round(drumParameters.VelocityBias), 1, 127);
                                }

                                var noteDuration = MusicConstants.TicksPerQuarterNote;

                                // Prevent overlap: trim previous notes of the same pitch (each tom has unique note number)
                                NoteOverlapHelper.PreventOverlap(notes, tomNote, baseTick);

                                notes.Add(new PartTrackEvent(
                                    noteNumber: tomNote,
                                    absoluteTimeTicks: baseTick,
                                    noteDurationTicks: noteDuration,
                                    noteOnVelocity: vel));
                                break;
                            }

                        case "crash_cymbal_1":
                            {
                                bool isStrongBeat = RandomHelpers.IsStrongBeat(slot.OnsetBeat);
                                int baseVel = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "crash", baseVelocity: 80);

                                // Apply velocity shaping
                                int vel = DrumVelocityShaper.ShapeVelocity(
                                    role: "crash",
                                    baseVelocity: baseVel,
                                    bar: slot.Bar,
                                    onsetBeat: slot.OnsetBeat,
                                    seed: settings.Seed,
                                    sectionType: sectionType,
                                    isStrongBeat: isStrongBeat,
                                    isGhost: false,
                                    isInFill: hit.IsInFill,
                                    fillProgress: hit.FillProgress);

                                // Apply velocity bias to crashes
                                if (drumParameters != null && Math.Abs(drumParameters.VelocityBias) > 0.0001)
                                {
                                    vel = Math.Clamp(vel + (int)Math.Round(drumParameters.VelocityBias), 1, 127);
                                }

                                // Boost velocity for accent hits (but not chokes)
                                if (hit.IsMain && !hit.IsChoke)
                                    vel = Math.Min(127, (int)(vel * 1.15));

                                // Use very short duration for choke, long sustain for crash
                                int duration = hit.IsChoke
                                    ? MusicConstants.TicksPerQuarterNote / 16  // Very short for choke
                                    : MusicConstants.TicksPerQuarterNote * 2;   // Long sustain for crash

                                // Prevent overlap: trim previous notes of the same pitch
                                NoteOverlapHelper.PreventOverlap(notes, crashCymbal1Note, baseTick);

                                notes.Add(new PartTrackEvent(
                                    noteNumber: crashCymbal1Note,
                                    absoluteTimeTicks: baseTick,
                                    noteDurationTicks: duration,
                                    noteOnVelocity: vel));
                                break;
                            }

                        case "crash_cymbal_2":
                            {
                                bool isStrongBeat = RandomHelpers.IsStrongBeat(slot.OnsetBeat);
                                int baseVel = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "crash", baseVelocity: 80);

                                // Apply velocity shaping
                                int vel = DrumVelocityShaper.ShapeVelocity(
                                    role: "crash",
                                    baseVelocity: baseVel,
                                    bar: slot.Bar,
                                    onsetBeat: slot.OnsetBeat,
                                    seed: settings.Seed,
                                    sectionType: sectionType,
                                    isStrongBeat: isStrongBeat,
                                    isGhost: false,
                                    isInFill: hit.IsInFill,
                                    fillProgress: hit.FillProgress);

                                // Apply velocity bias to crashes
                                if (drumParameters != null && Math.Abs(drumParameters.VelocityBias) > 0.0001)
                                {
                                    vel = Math.Clamp(vel + (int)Math.Round(drumParameters.VelocityBias), 1, 127);
                                }

                                // Boost velocity for accent hits
                                if (hit.IsMain)
                                    vel = Math.Min(127, (int)(vel * 1.15));

                                var noteDuration = MusicConstants.TicksPerQuarterNote * 2;

                                // Prevent overlap: trim previous notes of the same pitch
                                NoteOverlapHelper.PreventOverlap(notes, crashCymbal2Note, baseTick);

                                notes.Add(new PartTrackEvent(
                                    noteNumber: crashCymbal2Note,
                                    absoluteTimeTicks: baseTick,
                                    noteDurationTicks: noteDuration,
                                    noteOnVelocity: vel));
                                break;
                            }
                    }
                }
            }

            // Ensure events are sorted by AbsoluteTimeTicks before returning
            notes = notes.OrderBy(e => e.AbsoluteTimeTicks).ToList();

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }
    }
}
