using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Music.Designer;
using Music;

namespace Music
{
    public static class DesignerFormHandler
    {
        // Centralized check used by all edit actions.
        private static bool EnsureDesignOrNotify(Form owner)
        {
            if (Globals.Design != null) return true;

            MessageBox.Show(owner,
                "Create a new score design first.",
                "No Design",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        public static void NewDesign(DesignerForm form)
        {
            Globals.Design = new DesignerData();
            UpdateDesignerReport(form);
        }

        public static void SaveDesign(DesignerForm form)
        {
            if (Globals.Design == null)
            {
                MessageBox.Show(form,
                    "Create a new score design first.",
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
                txtDesignerReport.Text = DesignerReport.CreateDesignerReport(Globals.Design);
            }
        }

        public static void EditSections(DesignerForm form)
        {
            if (!EnsureDesignOrNotify(form)) return;

            using var dlg = new SectionEditorForm(Globals.Design!.SectionSet);
            if (dlg.ShowDialog(form) == DialogResult.OK)
            {
                // Copy back into the existing Sections instance to preserve references
                var target = Globals.Design!.SectionSet;
                target.Reset();
                foreach (var s in dlg.ResultSections.Sections)
                {
                    target.Add(s.SectionType, s.BarCount, s.Name);
                }

                UpdateDesignerReport(form);
            }
        }

        public static void SelectVoices(DesignerForm form)
        {
            if (!EnsureDesignOrNotify(form)) return;

            using var dlg = new VoiceSelectorForm();
            if (dlg.ShowDialog(form) == DialogResult.OK)
            {
                var score = Globals.Design!;
                var existing = new HashSet<string>(score.PartSet.Parts.Select(v => v.PartName),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var name in dlg.SelectedVoices)
                {
                    if (!existing.Contains(name))
                    {
                        score.PartSet.AddVoice(name);
                        existing.Add(name);
                    }
                }

                UpdateDesignerReport(form);
            }
        }

        public static void EditHarmony(DesignerForm form)
        {
            if (!EnsureDesignOrNotify(form)) return;

            var existing = Globals.Design!.HarmonicTimeline;
            using var dlg = new HarmonicEditorForm(existing);
            if (dlg.ShowDialog(form) == DialogResult.OK)
            {
                Globals.Design!.HarmonicTimeline = dlg.ResultTimeline;
                UpdateDesignerReport(form);
            }
        }

        public static void EditTimeSignature(DesignerForm form)
        {
            if (!EnsureDesignOrNotify(form)) return;

            var existing = Globals.Design!.TimeSignatureTimeline;
            using var dlg = new TimeSignatureEditorForm(existing);
            if (dlg.ShowDialog(form) == DialogResult.OK)
            {
                Globals.Design!.TimeSignatureTimeline = dlg.ResultTimeline;
                UpdateDesignerReport(form);
            }
        }

        public static void EditTempo(DesignerForm form)
        {
            if (!EnsureDesignOrNotify(form)) return;

            var existing = Globals.Design!.TempoTimeline;
            using var dlg = new TempoEditorForm(existing);
            if (dlg.ShowDialog(form) == DialogResult.OK)
            {
                Globals.Design!.TempoTimeline = dlg.ResultTimeline;
                UpdateDesignerReport(form);
            }
        }
    }
}