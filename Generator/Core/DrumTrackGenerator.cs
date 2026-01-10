// AI: purpose=Generate drum track: kick, snare, hi-hat, ride using DrumVariationEngine for living performance (Story 6.1+6.3).
// AI: invariants=Calls DrumVariationEngine per bar; integrates DrumFillEngine at section transitions; converts to MIDI PartTrackEvent list; returns sorted by AbsoluteTimeTicks.
// AI: deps=Uses DrumVariationEngine, DrumFillEngine, RandomHelpers, PitchRandomizer.SelectDrumVelocity for velocity shaping.

using Music.MyMidi;

namespace Music.Generator
{
    internal static class DrumTrackGenerator
    {
        /// <summary>
        /// Generates drum track: kick, snare, hi-hat, ride with deterministic variations and fills.
        /// Updated for Story 6.1 and Story 6.3 (section transition fills).
        /// </summary>
        public static PartTrack Generate(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            SectionTrack sectionTrack,
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
            const int tomHighNote = 50;     // High tom
            const int tomMidNote = 47;      // Mid tom
            const int tomLowNote = 45;      // Low tom

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var grooveEvent = grooveTrack.GetActiveGrooveEvent(bar);

                // Get section type and index for variation engine and fill engine
                MusicConstants.eSectionType sectionType = MusicConstants.eSectionType.Verse; // Default
                int sectionIndex = 0;
                if (sectionTrack.GetActiveSection(bar, out var section) && section != null)
                {
                    sectionType = section.SectionType;
                    sectionIndex = section.SectionId;
                }

                // Check if this bar should have a fill (Story 6.3)
                bool shouldFill = DrumFillEngine.ShouldGenerateFill(bar, totalBars, sectionTrack);

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
                        totalBars);
                }
                else
                {
                    // Generate per-bar variation plan using DrumVariationEngine
                    var variation = DrumVariationEngine.Generate(grooveEvent, sectionType, bar, settings.Seed);
                    allHits = variation.Hits;
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
                                
                                // Reduce velocity for non-main kicks
                                if (!hit.IsMain)
                                    vel = Math.Max(25, (int)(vel * 0.82));
                                
                                notes.Add(new PartTrackEvent(
                                    noteNumber: kickNote,
                                    absoluteTimeTicks: baseTick,
                                    noteDurationTicks: MusicConstants.TicksPerQuarterNote,
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
                                    
                                    notes.Add(new PartTrackEvent(
                                        noteNumber: snareNote,
                                        absoluteTimeTicks: baseTick,
                                        noteDurationTicks: MusicConstants.TicksPerQuarterNote,
                                        noteOnVelocity: preVel));
                                }
                                else if (hit.IsGhost)
                                {
                                    // Ghost note: use dedicated ghost velocity method
                                    int baseVel = randomizer.SelectDrumVelocity(slot.Bar, slot.OnsetBeat, "snare", baseVelocity: 40);
                                    int vel = DrumVelocityShaper.GhostNoteVelocity(baseVel, settings.Seed, slot.Bar, slot.OnsetBeat);
                                    
                                    notes.Add(new PartTrackEvent(
                                        noteNumber: snareNote,
                                        absoluteTimeTicks: baseTick,
                                        noteDurationTicks: MusicConstants.TicksPerQuarterNote,
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
                                    
                                    if (!hit.IsMain)
                                        vel = Math.Max(30, (int)(vel * 0.85));
                                    
                                    notes.Add(new PartTrackEvent(
                                        noteNumber: snareNote,
                                        absoluteTimeTicks: baseTick,
                                        noteDurationTicks: MusicConstants.TicksPerQuarterNote,
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

                                // Soften non-main hats
                                if (!hit.IsMain) 
                                    vel = Math.Max(20, (int)(vel * 0.85));

                                // Open vs closed articulation
                                int noteNumber = hit.IsOpenHat ? openHiHatNote : closedHiHatNote;

                                notes.Add(new PartTrackEvent(
                                    noteNumber: noteNumber,
                                    absoluteTimeTicks: baseTick,
                                    noteDurationTicks: MusicConstants.TicksPerQuarterNote / 2,
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

                                // Soften non-main ride hits
                                if (!hit.IsMain) 
                                    vel = Math.Max(25, (int)(vel * 0.85));

                                notes.Add(new PartTrackEvent(
                                    noteNumber: rideCymbalNote,
                                    absoluteTimeTicks: baseTick,
                                    noteDurationTicks: MusicConstants.TicksPerQuarterNote,
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

                                notes.Add(new PartTrackEvent(
                                    noteNumber: tomNote,
                                    absoluteTimeTicks: baseTick,
                                    noteDurationTicks: MusicConstants.TicksPerQuarterNote,
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
