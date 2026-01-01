using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Music.Writer.OptionForm
{
    public partial class OptionForm : Form
    {
        public OptionForm()
        {
            InitializeComponent();

            // Initialize comboboxes
            cbChordBase.SelectedIndex = 0; // C
            cbChordQuality.SelectedIndex = 0; // Major
            cbChordKey.SelectedIndex = 0; // C

            // Initialize staff selection - default to staff 1 checked
            if (clbStaffs != null && clbStaffs.Items.Count > 0)
                clbStaffs.SetItemChecked(0, true); // Check staff "1"
        }

        // Apply WriterFormData to the option form controls
        public void ApplyWriterFormData(WriterFormData? data)
        {
            if (data == null) return;

            var transform = new WriterFormTransform();
            transform.ApplyFormData(
                data,
                null,  // cbCommand - not in OptionForm
                clbParts,
                clbStaffs,
                rbChord,
                cbStep,
                rbPitchAbsolute,
                rbPitchKeyRelative,
                cbAccidental,
                numOctaveAbs,
                numDegree,
                cbChordKey,
                numChordDegree,
                cbChordQuality,
                cbChordBase,
                cbNoteValue,
                numDots,
                txtTupletNumber,
                numTupletCount,
                numTupletOf,
                numNumberOfNotes);
        }

        // Capture WriterFormData from the option form controls
        public WriterFormData CaptureWriterFormData()
        {
            var transform = new WriterFormTransform();
            return transform.CaptureFormData(
                null,  // cbCommand - not in OptionForm
                clbParts,
                clbStaffs,
                rbChord,
                cbStep,
                rbPitchAbsolute,
                rbPitchKeyRelative,
                cbAccidental,
                numOctaveAbs,
                numDegree,
                cbChordKey,
                numChordDegree,
                cbChordQuality,
                cbChordBase,
                cbNoteValue,
                numDots,
                txtTupletNumber,
                numTupletCount,
                numTupletOf,
                numNumberOfNotes);
        }
    }
}
