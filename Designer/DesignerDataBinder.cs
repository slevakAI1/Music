using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using Music.Designer;

namespace Music.Designer.DesignerForm
{
    internal class DesignerDataBinder
    {
        // Capture current DesignerForm state into a DesignerData object.
        // Minimal: returns the existing Globals.Design if present, otherwise a new DesignerData.
        public DesignerData CaptureFormData(Form form)
        {
            // No private control-to-data mapping required for now because the form edits Globals.Design
            // directly for containers (Sections, Parts, etc). Keep this minimal so it can be extended later.
            return Globals.Design ?? new DesignerData();
        }

        // Apply DesignerData into form controls.
        // Minimal implementation: update txtDesignView (if present) with the textual view of the design.
        public void ApplyFormData(Form form, DesignerData? data)
        {
            if (form == null || data == null) return;

            // Helper to fetch private fields by name on the form (searching base types as needed)
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

            // Update the design-view textbox if it exists
            var txtDesignView = GetField<TextBox>("txtDesignView");
            if (txtDesignView != null)
            {
                // Use the same helper as the existing form to build the view string
                txtDesignView.Text = DesignerReport.CreateDesignerReport(data);
            }

            // Future control mappings (e.g., part lists, timelines) can be added here using GetField<T>
        }
    }
}