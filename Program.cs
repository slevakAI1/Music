using System;
using System.Windows.Forms;
using Music.Generator;

namespace Music
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Configure global exception handling BEFORE any UI initialization
            GlobalExceptionHandler.Configure();

            // AI: EnsureLoaded: pre-load singletons (WordParser) at startup to avoid first-use delays.
            WordParser.EnsureLoaded();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}