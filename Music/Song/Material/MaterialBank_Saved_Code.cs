using Music.Song.Material;

namespace Music.Generator
{
    public static class MaterialBank_Saved_Code
    {
        
        // AI: creates MaterialBank with example motifs used by motif planner; safe to return non-empty test bank
        private static MaterialBank InitializeMaterialBank()
        {
            var bank = new MaterialBank();

            // Add test motifs from MotifLibrary (Story 8.3)
            var chorusHook = MotifLibrary.ClassicRockHookA().ToPartTrack();
            bank.Add(chorusHook);

            var verseRiff = MotifLibrary.SteadyVerseRiffA().ToPartTrack();
            bank.Add(verseRiff);

            var synthHook = MotifLibrary.BrightSynthHookA().ToPartTrack();
            bank.Add(synthHook);

            var bassFill = MotifLibrary.BassTransitionFillA().ToPartTrack();
            bank.Add(bassFill);

            return bank;
        }

        // AI: creates MotifPlacementPlan; returns Empty when materialBank empty; logs placements
        private static MotifPlacementPlan CreateMotifPlacementPlan(
            MaterialBank materialBank,
            SectionTrack sectionTrack,
            int seed)
        {
            Tracer.DebugTrace($"CreateMotifPlacementPlan: MaterialBank has {materialBank.Count} items");
            
            if (materialBank.Count == 0)
            {
                Tracer.DebugTrace("CreateMotifPlacementPlan: Returning empty plan (no motifs in bank)");
                return MotifPlacementPlan.Empty(seed);
            }

            // Use static CreatePlan method
            var plan = MotifPlacementPlanner.CreatePlan(
                sectionTrack,
                materialBank,
                seed);
            
            Tracer.DebugTrace($"CreateMotifPlacementPlan: Created plan with {plan.Placements.Count} placements");
            foreach (var p in plan.Placements)
            {
                Tracer.DebugTrace($"  Planned: Role={p.MotifSpec.IntendedRole}, Section={p.AbsoluteSectionIndex}, Bar={p.StartBarWithinSection}-{p.StartBarWithinSection + p.DurationBars - 1}");
            }
            
            return plan;
        }
    }
}