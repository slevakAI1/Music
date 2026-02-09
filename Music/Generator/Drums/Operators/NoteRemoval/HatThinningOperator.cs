// AI: purpose=NoteRemoval operator that removes every other hi-hat hit for breathing room.
// AI: invariants=Targets ClosedHat only; skips protected/must-hit onsets (enforced by applicator).
// AI: deps=DrumOperatorBase, IDrumRemovalOperator, DrummerContext, RemovalCandidate, GrooveRoles.

using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.NoteRemoval
{
    // AI: purpose=Thin hi-hat pattern by removing offbeat hat hits; creates open feel for verses/bridges.
    // AI: note=Real drummers often play quarter-note hats in verses and eighth-note hats in choruses.
    public sealed class HatThinningOperator : DrumOperatorBase, IDrumRemovalOperator
    {
        public override string OperatorId => "DrumHatThinning";

        public override OperatorFamily OperatorFamily => OperatorFamily.NoteRemoval;

        // Removal operators do not add onsets.
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
            => [];

        // Prefer verse/bridge sections where sparser feel is musical.
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Most effective outside fill windows; fills have their own density logic.
            if (context.Bar.IsFillWindow)
                return false;

            return true;
        }

        // Remove offbeat hat hits (fractional beats like 1.5, 2.5, 3.5, 4.5) from current bar.
        public IEnumerable<RemovalCandidate> GenerateRemovals(DrummerContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!CanApply(context))
                yield break;

            int barNumber = context.Bar.BarNumber;
            int beatsPerBar = context.Bar.BeatsPerBar;

            // Target offbeat hat positions (the "and" of each beat)
            for (int beat = 1; beat <= beatsPerBar; beat++)
            {
                decimal offbeat = beat + 0.5m;
                yield return new RemovalCandidate
                {
                    BarNumber = barNumber,
                    Beat = offbeat,
                    Role = GrooveRoles.ClosedHat,
                    OperatorId = OperatorId,
                    Reason = "Thin hats to quarter-note pattern for breathing room"
                };
            }
        }
    }
}
