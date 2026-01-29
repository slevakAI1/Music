// AI: purpose=Model of drummer limbs and role→limb mapping for physicality validation.
// AI: invariants=Limbs enum is stable (4 values); default mapping covers all GrooveRoles drum roles; GetRequiredLimb returns null for unknown roles.
// AI: deps=GrooveRoles for role constants; consumed by LimbConflictDetector and PhysicalityFilter.
// AI: change=Story 4.1 defines model; Story 4.3 integrates into PhysicalityFilter.


using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Physicality
{
    /// <summary>
    /// Physical limbs available to a drummer.
    /// </summary>
    public enum Limb
    {
        /// <summary>Right hand - typically plays hi-hat or ride cymbal.</summary>
        RightHand = 0,

        /// <summary>Left hand - typically plays snare and toms.</summary>
        LeftHand = 1,

        /// <summary>Right foot - typically plays kick drum.</summary>
        RightFoot = 2,

        /// <summary>Left foot - typically operates hi-hat pedal.</summary>
        LeftFoot = 3
    }

    /// <summary>
    /// Models a drummer's physical capabilities: which limb plays which drum role.
    /// Provides role→limb mapping for conflict detection.
    /// Story 4.1: Define Limb Model.
    /// </summary>
    public sealed class LimbModel
    {
        private readonly IReadOnlyDictionary<string, Limb> _roleLimbMapping;

        /// <summary>
        /// Creates a LimbModel with the specified role→limb mapping.
        /// </summary>
        /// <param name="roleLimbMapping">Custom mapping from role names to limbs. Null uses defaults.</param>
        public LimbModel(IReadOnlyDictionary<string, Limb>? roleLimbMapping = null)
        {
            _roleLimbMapping = roleLimbMapping ?? CreateDefaultMapping();
        }

        /// <summary>
        /// Gets the limb required to play the specified role.
        /// </summary>
        /// <param name="role">Drum role (e.g., "Kick", "Snare", "ClosedHat").</param>
        /// <returns>The limb that plays this role, or null if role is unknown.</returns>
        public Limb? GetRequiredLimb(string role)
        {
            if (string.IsNullOrEmpty(role))
                return null;

            return _roleLimbMapping.TryGetValue(role, out var limb) ? limb : null;
        }

        /// <summary>
        /// Gets the current role→limb mapping (read-only).
        /// </summary>
        public IReadOnlyDictionary<string, Limb> RoleLimbMapping => _roleLimbMapping;

        /// <summary>
        /// Creates a new LimbModel with an additional or updated role mapping.
        /// </summary>
        /// <param name="role">Role to add/update.</param>
        /// <param name="limb">Limb that plays this role.</param>
        /// <returns>New LimbModel with updated mapping.</returns>
        public LimbModel WithRoleMapping(string role, Limb limb)
        {
            ArgumentNullException.ThrowIfNull(role);

            var newMapping = new Dictionary<string, Limb>(_roleLimbMapping)
            {
                [role] = limb
            };

            return new LimbModel(newMapping);
        }

        /// <summary>
        /// Creates the default role→limb mapping for standard drum kit setup.
        /// Right-handed drummer: right hand on hat/ride, left hand on snare/toms.
        /// </summary>
        private static Dictionary<string, Limb> CreateDefaultMapping()
        {
            return new Dictionary<string, Limb>
            {
                // Right hand - timekeeping cymbals
                [GrooveRoles.ClosedHat] = Limb.RightHand,
                [GrooveRoles.OpenHat] = Limb.RightHand,
                [GrooveRoles.Ride] = Limb.RightHand,
                [GrooveRoles.Crash] = Limb.RightHand,

                // Left hand - snare and toms
                [GrooveRoles.Snare] = Limb.LeftHand,
                [GrooveRoles.Tom1] = Limb.LeftHand,
                [GrooveRoles.Tom2] = Limb.LeftHand,
                [GrooveRoles.FloorTom] = Limb.LeftHand,

                // Right foot - kick drum
                [GrooveRoles.Kick] = Limb.RightFoot,

                // Left foot - hi-hat pedal (for open/close control)
                // Note: "HiHatPedal" is not in GrooveRoles but may be added
            };
        }

        /// <summary>
        /// Default LimbModel instance with standard right-handed drummer mapping.
        /// </summary>
        public static LimbModel Default { get; } = new();

        /// <summary>
        /// Left-handed drummer mapping (reversed hands).
        /// </summary>
        public static LimbModel LeftHanded { get; } = new(new Dictionary<string, Limb>
        {
            // Left hand - timekeeping cymbals (reversed)
            [GrooveRoles.ClosedHat] = Limb.LeftHand,
            [GrooveRoles.OpenHat] = Limb.LeftHand,
            [GrooveRoles.Ride] = Limb.LeftHand,
            [GrooveRoles.Crash] = Limb.LeftHand,

            // Right hand - snare and toms (reversed)
            [GrooveRoles.Snare] = Limb.RightHand,
            [GrooveRoles.Tom1] = Limb.RightHand,
            [GrooveRoles.Tom2] = Limb.RightHand,
            [GrooveRoles.FloorTom] = Limb.RightHand,

            // Right foot - kick drum (unchanged)
            [GrooveRoles.Kick] = Limb.RightFoot,
        });
    }
}
