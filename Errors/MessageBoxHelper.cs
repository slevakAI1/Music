// AI: purpose=thread-safe UI message helper; safe to call from any thread; use BeginInvoke to avoid deadlocks from worker threads
// AI: invariants=ShowInternal is fire-and-forget for ownered messages; synchronous Show(...) returns DialogResult and may block UI
// AI: threading=Show(Form,..) uses Invoke if required to ensure modal owner dialog; BeginInvoke used in ShowInternal to avoid deadlock
// AI: behavior=ShowInternal swallows secondary exceptions to avoid crashing during error display; keep that behavior
// AI: change=if altering threading or ownership rules update GlobalExceptionHandler and callers that rely on non-blocking calls

namespace Music.Writer

    // CLEAN - This should be used everywhere

{
    internal static class MessageBoxHelper
    {
        // Fire-and-forget show; safe to call from any thread.
        public static void ShowMessage(Form? owner, string text, string caption)
        {
            ShowInternal(owner, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Convenience overload used across the codebase where no owner was supplied.
        public static void ShowMessage(string text, string caption)
        {
            ShowInternal(null, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void ShowError(string text, string caption)
        {
            ShowInternal(null, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void ShowError(Form? owner, string text, string caption)
        {
            ShowInternal(owner, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // Synchronous "Show" helpers kept for compatibility with existing callers.
        // These return the DialogResult and will execute on the correct thread.
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            // No owner provided: use default synchronous MessageBox on current thread.
            return MessageBox.Show(text, caption, buttons, icon);
        }

        public static DialogResult Show(Form? owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (owner == null || !owner.IsHandleCreated)
            {
                return MessageBox.Show(text, caption, buttons, icon);
            }

            // If caller is not on UI thread for the owner, marshal synchronously to ensure a true modal dialog.
            if (owner.InvokeRequired)
            {
                try
                {
                    return (DialogResult)owner.Invoke(new Func<DialogResult>(() => MessageBox.Show(owner, text, caption, buttons, icon)));
                }
                catch
                {
                    // Fall back to non-owned message box if invoke fails.
                    return MessageBox.Show(text, caption, buttons, icon);
                }
            }
            else
            {
                return MessageBox.Show(owner, text, caption, buttons, icon);
            }
        }

        // Centralized: always execute the UI show on the UI thread, asynchronously.
        // Used by ShowMessage/ShowError to avoid deadlocks when called from background threads.
        // AI: ShowInternal is fire-and-forget for ownered dialogs; it intentionally does not return DialogResult.
        // AI: It guards against owner disposal between scheduling and execution and swallows secondary exceptions.
        private static void ShowInternal(Form? owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            try
            {
                if (owner != null && owner.IsHandleCreated)
                {
                    // Use BeginInvoke to avoid deadlock if caller is a worker thread and UI is waiting.
                    // We intentionally do not capture and return the DialogResult here — for error/info messages
                    // we simply display them and return immediately.
                    owner.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            // If the owner is disposed between scheduling and execution, guard against exceptions.
                            if (!owner.IsHandleCreated || owner.Disposing || owner.IsDisposed)
                            {
                                MessageBox.Show(text, caption, buttons, icon);
                            }
                            else
                            {
                                MessageBox.Show(owner, text, caption, buttons, icon);
                            }
                        }
                        catch
                        {
                            // Swallow secondary exceptions to avoid crashing the app from an error dialog.
                            try { MessageBox.Show(text, caption, buttons, icon); } catch { }
                        }
                    }));
                }
                else
                {
                    // No valid owner, show on default UI thread (safe to call from UI thread or background)
                    // This is synchronous but only used when owner null/invalid; background callers should
                    // prefer providing the owner or using the other overloads.
                    MessageBox.Show(text, caption, buttons, icon);
                }
            }
            catch
            {
                // As a last resort, try a plain (no-owner) MessageBox on the calling thread.
                try { MessageBox.Show(text, caption, buttons, icon); } catch { }
            }
        }
    }
}
