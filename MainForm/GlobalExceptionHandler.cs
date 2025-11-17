using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Music
{
    /// <summary>
    /// Provides centralized exception handling for the entire application.
    /// Intercepts unhandled exceptions from UI thread, background threads, and async operations,
    /// displays them to the user, and returns control to the active form.
    /// </summary>
    public static class GlobalExceptionHandler
    {
        private static bool _isConfigured = false;

        /// <summary>
        /// Configures global exception handlers for the application.
        /// Should be called once at application startup, before any forms are shown.
        /// </summary>
        public static void Configure()
        {
            if (_isConfigured)
                return;

            // Handle exceptions on the UI thread (synchronous operations)
            Application.ThreadException += OnThreadException;

            // Handle exceptions on background threads and unobserved task exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // Handle unobserved task exceptions (async/await operations)
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Ensure Windows Forms uses our exception handler instead of crashing
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            _isConfigured = true;
        }

        /// <summary>
        /// Handles exceptions thrown on the UI thread (Windows Forms controls, event handlers, etc.)
        /// </summary>
        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        /// <summary>
        /// Handles exceptions thrown on background threads or from the AppDomain.
        /// </summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                HandleException(exception);
            }
        }

        /// <summary>
        /// Handles exceptions from unobserved Task operations (async/await that don't get awaited properly).
        /// </summary>
        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.SetObserved(); // Prevent the application from terminating
        }

        /// <summary>
        /// Core exception handling logic. Displays error to user on the UI thread
        /// and returns control to the active MDI child form.
        /// </summary>
        private static void HandleException(Exception exception)
        {
            try
            {
                // Get the active MDI child form, if any
                Form? activeForm = GetActiveMdiChild();

                // Build a user-friendly error message
                string errorMessage = BuildErrorMessage(exception);
                string caption = "Error";

                // Display the error dialog on the UI thread
                if (Application.OpenForms.Count > 0)
                {
                    // Get the main form (first opened form, typically the MDI parent)
                    Form mainForm = Application.OpenForms[0];

                    if (mainForm.InvokeRequired)
                    {
                        // Marshal to UI thread if called from background thread
                        mainForm.Invoke(new Action(() =>
                        {
                            ShowErrorDialog(activeForm ?? mainForm, errorMessage, caption);
                        }));
                    }
                    else
                    {
                        // Already on UI thread
                        ShowErrorDialog(activeForm ?? mainForm, errorMessage, caption);
                    }
                }
                else
                {
                    // Fallback if no forms are open (shouldn't happen in normal operation)
                    MessageBox.Show(errorMessage, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch
            {
                // Last-resort error handling if our error handler itself fails
                try
                {
                    MessageBox.Show(
                        $"An error occurred, and the error handler encountered an additional problem.\n\nOriginal error: {exception.Message}",
                        "Critical Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                catch
                {
                    // If even the fallback fails, there's nothing more we can do
                }
            }
        }

        /// <summary>
        /// Gets the currently active MDI child form, if any.
        /// </summary>
        private static Form? GetActiveMdiChild()
        {
            try
            {
                // Find the MDI parent form
                foreach (Form form in Application.OpenForms)
                {
                    if (form.IsMdiContainer && form.ActiveMdiChild != null)
                    {
                        return form.ActiveMdiChild;
                    }
                }
            }
            catch
            {
                // If we can't determine the active child, return null
            }

            return null;
        }

        /// <summary>
        /// Displays the error dialog to the user.
        /// </summary>
        private static void ShowErrorDialog(Form owner, string message, string caption)
        {
            try
            {
                MessageBox.Show(owner, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // After the user closes the dialog, focus returns to the owner form automatically.
                // Ensure the form is in a ready state to accept user input.
                if (owner != null && !owner.IsDisposed)
                {
                    owner.Activate();
                }
            }
            catch
            {
                // Fallback if showing with owner fails
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Builds a user-friendly error message from an exception.
        /// Can be enhanced to provide more context, logging, etc.
        /// </summary>
        private static string BuildErrorMessage(Exception exception)
        {
            if (exception == null)
                return "An unknown error occurred.";

            // For aggregate exceptions (from tasks), show all inner exceptions
            if (exception is AggregateException aggEx)
            {
                var messages = new System.Text.StringBuilder();
                messages.AppendLine("Multiple errors occurred:");
                messages.AppendLine();

                foreach (var innerEx in aggEx.InnerExceptions)
                {
                    messages.AppendLine($"• {innerEx.Message}");
                }

                return messages.ToString();
            }

            // For single exceptions, show the message and inner exception if present
            string message = exception.Message;

            if (exception.InnerException != null)
            {
                message += $"\n\nDetails: {exception.InnerException.Message}";
            }

            return message;
        }
    }
}