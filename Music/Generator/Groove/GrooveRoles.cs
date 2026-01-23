namespace Music.Generator
{
    // AI: purpose=Stable role identifiers for groove policies/candidates/profiles; string-based for extensibility.
    // AI: invariants=Role names MUST match across policies/candidates; case-sensitive; do not rename without migrating data.
    // AI: change=Add new roles here; update all policies/candidates that reference roles (ProtectionPolicy, VariationCatalog, etc.).
    public static class GrooveRoles
    {
        public const string Kick = "Kick";
        public const string Snare = "Snare";
        public const string ClosedHat = "ClosedHat";
        public const string OpenHat = "OpenHat";
        public const string Crash = "Crash";
        public const string Ride = "Ride";
        public const string Tom1 = "Tom1";
        public const string Tom2 = "Tom2";
        public const string FloorTom = "FloorTom";
        public const string DrumKit = "DrumKit";
        public const string Bass = "Bass";
        public const string Comp = "Comp";
        public const string Pads = "Pads";
        public const string Keys = "Keys";
        public const string Lead = "Lead";
    }
}
