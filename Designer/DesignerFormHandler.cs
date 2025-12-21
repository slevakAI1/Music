using Music.Designer;
using Music.Writer;
using System.ComponentModel.Design;
using System.Reflection;

namespace Music
{
    public static class DesignerFormHandler
    {
        // Centralized check used by all edit actions.
        private static bool EnsureDesignOrNotify(Form owner)
        {
            if (Globals.Designer != null) return true;

            MessageBoxHelper.Show(
                "Create a new design first.",
                "No Design",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        public static void NewDesign(DesignerForm form)
        {
            Globals.Designer = new Designer.Designer();
            UpdateDesignerReport(form);
        }

        public static void SaveDesign(DesignerForm form)
        {
            if (Globals.Designer == null)
            {
                MessageBoxHelper.Show(
                    "Create a new design first.",
                    "No Design",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            DesignerFileManager.SaveDesign(form);
        }

        public static void LoadDesign(DesignerForm form)
        {
            var loaded = DesignerFileManager.LoadDesign(form);
            if (loaded)
            {
                UpdateDesignerReport(form);
            }
        }

        // Update the txtDesignerReport TextBox (if present) with the current Globals.Design snapshot.
        public static void UpdateDesignerReport(Form form)
        {
            if (form == null) return;

            T? GetField<T>(string name) where T : class
            {
                var t = form.GetType();
                while (t != null)
                {
                    var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (fi != null)
                        return fi.GetValue(form) as T;
                    t = t.BaseType;
                }
                return null;
            }

            var txtDesignerReport = GetField<TextBox>("txtDesignerReport");
            if (txtDesignerReport != null)
            {
                txtDesignerReport.Text = DesignerReport.CreateDesignerReport(Globals.Designer);
            }
        }

        public static void EditSections(DesignerForm form)
        {
            if (!EnsureDesignOrNotify(form)) return;

            using var dlg = new SectionEditorForm(Globals.Designer!.SectionTrack);
            if (dlg.ShowDialog(form) == DialogResult.OK)
            {
                // Copy back into the existing Sections instance to preserve references
                var target = Globals.Designer!.SectionTrack;
                target.Reset();
                foreach (var s in dlg.ResultSections.Sections)
                {
                    target.Add(s.SectionType, s.BarCount, s.Name);
                }

                UpdateDesignerReport(form);
            }
        }

        public static void EditHarmony(DesignerForm form)
        {
            if (!EnsureDesignOrNotify(form)) return;

            var existing = Globals.Designer!.HarmonyTrack;
            using var dlg = new HarmonyEditorForm(existing);
            if (dlg.ShowDialog(form) == DialogResult.OK)
            {
                Globals.Designer!.HarmonyTrack = dlg.ResultTimeline;
                UpdateDesignerReport(form);
            }
        }

        public static void EditGroove(DesignerForm form)
        {
            if (!EnsureDesignOrNotify(form)) return;

            var existing = Globals.Designer!.GrooveTrack;
            using var dlg = new GrooveEditorForm(existing);
            if (dlg.ShowDialog(form) == DialogResult.OK)
            {
                Globals.Designer!.GrooveTrack = dlg.ResultTimeline;
                Globals.Designer!.GrooveTrack?.EnsureIndexed();
                // UpdateDesignerReport(form); -- may be deprecated soon
            }
        }




        public static void EditTimeSignature(DesignerForm form)
        {
            if (!EnsureDesignOrNotify(form)) return;

            var existing = Globals.Designer!.TimeSignatureTrack;
            using var dlg = new TimeSignatureEditorForm(existing);
            if (dlg.ShowDialog(form) == DialogResult.OK)
            {
                Globals.Designer!.TimeSignatureTrack = dlg.ResultTimeline;
                UpdateDesignerReport(form);
            }
        }

        public static void EditTempo(DesignerForm form)
        {
            if (!EnsureDesignOrNotify(form)) return;

            var existing = Globals.Designer!.TempoTrack;
            using var dlg = new TempoEditorForm(existing);
            if (dlg.ShowDialog(form) == DialogResult.OK)
            {
                Globals.Designer!.TempoTrack = dlg.ResultTimeline;
                UpdateDesignerReport(form);
            }
        }
    }
}