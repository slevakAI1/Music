using Music.Generator;

// AI: purpose=App entrypoint: configure global handlers, preload singletons, init RNG, start WinForms MainForm.
// AI: invariants=GlobalExceptionHandler.Configure must run before any UI init; Rng.Initialize called once app-wide.
// AI: deps=WordParser.EnsureLoaded; ApplicationConfiguration.Initialize; MainForm; STAThread required for WinForms.
namespace Music
{
    internal static class Program
    {
        // AI: entry=Main: initialize global services in this order then call Application.Run(MainForm).
        [STAThread]
        static void Main()
        {
            // Configure global exception handling BEFORE any UI initialization
            GlobalExceptionHandler.Configure();

            // AI: EnsureLoaded: pre-load singletons (WordParser) at startup to avoid first-use delays.
            WordParser.EnsureLoaded();

            // Initialize RNG manager (once per app)
            Rng.Initialize();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
