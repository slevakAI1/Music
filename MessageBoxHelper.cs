namespace Music
{
    /// <summary>
    /// Application-scoped MessageBox helper that does not require a Form to be passed in.
    /// It creates a tiny invisible owner form so the MessageBox is modal to this application
    /// only (no Desktop-only flag) and restores activation to the previously active form
    /// when the user dismisses the dialog.
    /// </summary>
    public static class MessageBoxHelper
    {
        /// <summary>
        /// Show a message box modal to this application. Safe to call from any class.
        /// If called from a non-UI thread this will marshal the call to an existing UI form.
        /// </summary>
        public static DialogResult Show(string text, string caption = "Error", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            // Find a form that can be used to marshal to the UI thread if required.
            var dispatcher = Form.ActiveForm ?? Application.OpenForms.Cast<Form>().FirstOrDefault();

            if (dispatcher != null && dispatcher.InvokeRequired)
            {
                // Marshal to UI thread of an existing form
                return (DialogResult)dispatcher.Invoke(new Func<DialogResult>(() => ShowInternal(text, caption, buttons, icon)));
            }

            return ShowInternal(text, caption, buttons, icon);
        }

        private static DialogResult ShowInternal(
            string text, 
            string caption, 
            MessageBoxButtons buttons, 
            MessageBoxIcon icon)
        {
            // Save currently active form (if any) so we can restore activation later.
            Form? previouslyActive = Form.ActiveForm ?? Application.OpenForms.Cast<Form>().FirstOrDefault();

            // Create a tiny invisible form to act as the owner for the MessageBox. This ensures
            // the MessageBox is modal to this application only and won't affect other applications.
            using var owner = new Form
            {
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Size = new Size(1, 1),
                Location = new Point(-32000, -32000),
                FormBorderStyle = FormBorderStyle.None,
                Opacity = 0
            };

            // Show the owner so it has a valid window handle.
            owner.Show();
            // Force creation of the handle
            var _ = owner.Handle;

            // Display the MessageBox with the owner. Do not use DefaultDesktopOnly.
            var result = MessageBox.Show(owner, text, caption, buttons, icon);

            // After the user dismisses the box, attempt to restore activation to the previously active form.
            try
            {
                if (previouslyActive != null && !previouslyActive.IsDisposed && previouslyActive != owner)
                {
                    // Ensure the previously active form is visible and has a handle, then activate.
                    if (!previouslyActive.Visible)
                    {
                        previouslyActive.Show();
                    }

                    if (!previouslyActive.IsHandleCreated)
                    {
                        var h = previouslyActive.Handle; // force handle create
                    }

                    previouslyActive.Activate();
                }
            }
            catch
            {
                // Swallow any exceptions here; we don't want to crash the caller because of activation restore.
            }

            // Close the invisible owner.
            try { owner.Close(); } catch { }

            return result;
        }

        /// <summary>
        /// Convenience shorthand for showing an error message with an OK button.
        /// </summary>
        public static DialogResult ShowError(string text, string caption = "Error") => 
                Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
