using System;
using System.Collections.Generic;
using System.Linq;
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

                form.UpdateDesignerReport();
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

                form.UpdateDesignerReport();
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
                form.UpdateDesignerReport();
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
                form.UpdateDesignerReport();
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
                form.UpdateDesignerReport();
            }
        }
    }
}