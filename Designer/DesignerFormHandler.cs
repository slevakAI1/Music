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
            if (Globals.Designer != null) return true;

            MessageBox.Show(owner,
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
                MessageBox.Show(form,
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

            using var dlg = new SectionEditorForm(Globals.Designer!.SectionTimeline);
            if (dlg.ShowDialog(form) == DialogResult.OK)
            {
                // Copy back into the existing Sections instance to preserve references
                var target = Globals.Designer!.SectionTimeline;
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
            using var voiceForm = new VoiceSelectorForm();
            
            // Initialize with existing voices from the designer
            var existingVoices = Globals.Designer?.Voices?.Voices?
                .Where(v => !string.IsNullOrWhiteSpace(v.VoiceName))
                .ToDictionary(
                    v => v.VoiceName, 
                    v => string.IsNullOrWhiteSpace(v.GrooveRole) ? "Select..." : v.GrooveRole,
                    StringComparer.OrdinalIgnoreCase);
            
            if (existingVoices?.Count > 0)
            {
                voiceForm.SetExistingVoices(existingVoices);
            }

            if (voiceForm.ShowDialog(form) == DialogResult.OK)
            {
                var selected = voiceForm.SelectedVoicesWithRoles;
                if (selected?.Count > 0)
                {
                    if (Globals.Designer == null)
                        Globals.Designer = new Designer.Designer();

                    Globals.Designer.Voices ??= new VoiceSet();
                    Globals.Designer.Voices.Reset();

                    foreach (var kvp in selected)
                    {
                        var role = kvp.Value == "Select..." ? "" : kvp.Value;
                        Globals.Designer.Voices.AddVoice(kvp.Key, role);
                    }

                    UpdateDesignerReport(form);
                }
            }
        }

        public static void EditHarmony(DesignerForm form)
        {
            if (!EnsureDesignOrNotify(form)) return;

            var existing = Globals.Designer!.HarmonyTimeline;
            using var dlg = new HarmonyEditorForm(existing);
            if (dlg.ShowDialog(form) == DialogResult.OK)
            {
                Globals.Designer!.HarmonyTimeline = dlg.ResultTimeline;
                UpdateDesignerReport(form);
            }
        }

        public static void EditTimeSignature(DesignerForm form)
        {
            if (!EnsureDesignOrNotify(form)) return;

            var existing = Globals.Designer!.TimeSignatureTimeline;
            using var dlg = new TimeSignatureEditorForm(existing);
            if (dlg.ShowDialog(form) == DialogResult.OK)
            {
                Globals.Designer!.TimeSignatureTimeline = dlg.ResultTimeline;
                UpdateDesignerReport(form);
            }
        }

        public static void EditTempo(DesignerForm form)
        {
            if (!EnsureDesignOrNotify(form)) return;

            var existing = Globals.Designer!.TempoTimeline;
            using var dlg = new TempoEditorForm(existing);
            if (dlg.ShowDialog(form) == DialogResult.OK)
            {
                Globals.Designer!.TempoTimeline = dlg.ResultTimeline;
                UpdateDesignerReport(form);
            }
        }
    }
}