using System.Diagnostics;

// AI: purpose=centralized app exception handling; registers UI, AppDomain and TaskScheduler handlers; call Configure once at startup
// AI: invariants=Configure idempotent; must be called before showing forms; handlers depend on WinForms Application.OpenForms
// AI: threading=handlers run on different threads; HandleException marshals to UI thread via main form Invoke when required
// AI: behavior=UnobservedTaskException is SetObserved to avoid process termination; UnhandledException may run on non-UI thread
// AI: security=messages may reveal file/stack info; avoid exposing to end users in production builds or sanitize before logging

namespace Music
{
    // AI: GlobalExceptionHandler: static helper; keep registration and message behavior stable to avoid changing app crash semantics
    public static class GlobalExceptionHandler
    {
        private static bool _isConfigured = false;

        // AI: Configure: idempotent registration of ThreadException, AppDomain.UnhandledException, TaskScheduler.UnobservedTaskException
        // AI: DO NOT call more than once; calling later than app start may miss earlier exceptions
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

        // AI: OnThreadException: UI thread exceptions forwarded to HandleException
        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        // AI: OnUnhandledException: AppDomain-level handler; ExceptionObject may not be an Exception in some hosts
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                HandleException(exception);
            }
        }

        // AI: OnUnobservedTaskException: marks task exception observed to prevent process termination
        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.SetObserved(); // Prevent the application from terminating
        }

        // AI: HandleException: compose user-friendly message, marshal to UI thread using mainForm if available, show MessageBox
        // AI: fallback: if no forms, call MessageBox.Show; keep Activate() call to restore focus to owner after dialog
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

        // AI: GetActiveMdiChild: attempts to find active MDI child; may return null if not determinable or on error
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

        // AI: ShowErrorDialog: primary UI for error display; keep owner Activate to restore focus; fallback to ownerless MessageBox on failure
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

        // AI: BuildErrorMessage: composes message for AggregateException or single exceptions; includes inner exception details
        private static string BuildErrorMessage(Exception exception)
        {
            if (exception == null)
                return "An unknown error occurred.";

            var message = new System.Text.StringBuilder();

            // For aggregate exceptions (from tasks), show all inner exceptions
            if (exception is AggregateException aggEx)
            {
                message.AppendLine("Multiple errors occurred:");
                message.AppendLine();

                foreach (var innerEx in aggEx.InnerExceptions)
                {
                    message.AppendLine($"• {innerEx.Message}");
                    AppendExceptionSource(message, innerEx);
                }
            }
            else
            {
                // Single exception
                message.AppendLine(exception.Message);
                AppendExceptionSource(message, exception);

                // Include inner exception if present
                if (exception.InnerException != null)
                {
                    message.AppendLine();
                    message.AppendLine("Details:");
                    message.AppendLine(exception.InnerException.Message);
                    AppendExceptionSource(message, exception.InnerException);
                }
            }

            return message.ToString();
        }

        // AI: AppendExceptionSource: best-effort include file/class/method/line; may be empty in release builds without PDBs
        private static void AppendExceptionSource(System.Text.StringBuilder message, Exception exception)
        {
            try
            {
                var sourceInfo = GetExceptionSourceInfo(exception);
                if (sourceInfo != null)
                {
                    message.AppendLine();

                    if (!string.IsNullOrEmpty(sourceInfo.FileName))
                    {
                        message.AppendLine($"File: {sourceInfo.FileName}");
                    }

                    if (!string.IsNullOrEmpty(sourceInfo.ClassName))
                    {
                        message.AppendLine($"Class: {sourceInfo.ClassName}");
                    }

                    if (!string.IsNullOrEmpty(sourceInfo.MethodName))
                    {
                        message.AppendLine($"Method: {sourceInfo.MethodName}");
                    }

                    if (sourceInfo.LineNumber > 0)
                    {
                        message.AppendLine($"Line: {sourceInfo.LineNumber}");
                    }
                }
            }
            catch
            {
                // If we can't get source info, just skip it rather than failing
            }
        }

        // AI: GetExceptionSourceInfo: uses StackTrace(exception, true) to read file/line info; requires PDBs to return file/line
        private static ExceptionSourceInfo? GetExceptionSourceInfo(Exception exception)
        {
            try
            {
                var stackTrace = new StackTrace(exception, true);
                
                // Get the first frame that has file information (this is where the exception was thrown)
                var frame = stackTrace.GetFrames()?.FirstOrDefault(f => 
                    f.GetFileName() != null || f.GetMethod() != null);

                if (frame == null)
                    return null;

                var method = frame.GetMethod();
                var fileName = frame.GetFileName();
                var lineNumber = frame.GetFileLineNumber();

                return new ExceptionSourceInfo
                {
                    FileName = !string.IsNullOrEmpty(fileName) ? System.IO.Path.GetFileName(fileName) : null,
                    ClassName = method?.DeclaringType?.Name,
                    MethodName = method?.Name,
                    LineNumber = lineNumber
                };
            }
            catch
            {
                return null;
            }
        }

        // AI: ExceptionSourceInfo: container for best-effort source metadata; keep fields nullable for missing info
        private sealed class ExceptionSourceInfo
        {
            public string? FileName { get; set; }
            public string? ClassName { get; set; }
            public string? MethodName { get; set; }
            public int LineNumber { get; set; }
        }
    }
}