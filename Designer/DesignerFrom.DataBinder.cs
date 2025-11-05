using System;
using System.Windows.Forms;
using Music.Designer;

namespace Music
{
    public partial class DesignerForm
    {
        public Designer.Designer CaptureFormData()
        {
            return Globals.Designer ?? new Designer.Designer();
        }

        // Apply DesignerData into form controls.
        public void ApplyFormData(Designer.Designer? data)
        {
            if (data == null) return;
            txtDesignerReport.Text = DesignerReport.CreateDesignerReport(data);
        }
    }
}