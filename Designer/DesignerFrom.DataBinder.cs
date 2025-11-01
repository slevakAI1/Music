using System;
using System.Windows.Forms;
using Music.Designer;

namespace Music
{
    public partial class DesignerForm
    {
        public DesignerData CaptureFormData()
        {
            return Globals.Design ?? new DesignerData();
        }

        // Apply DesignerData into form controls.
        public void ApplyFormData(DesignerData? data)
        {
            if (data == null) return;
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(data);
        }
    }
}