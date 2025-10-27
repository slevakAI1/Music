using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MusicXml.Domain;
using Music.Design;

namespace Music.Generate
{
    public partial class GenerateForm : Form
    {

        // CORRECT
        private Score? _score;
        private DesignClass? _design;

        public GenerateForm()
        {
            InitializeComponent();

            // Window behavior similar to other forms
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
           
            // CORRECT
            // Load current global score and design into form-local fields for later use
            // Constructor is the only place that reads Globals per requirement.
            _score = Globals.Score;
            _design = Globals.Design;
        }

        // CORRECT
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        // CONFIRMED CORRECT
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // DATA INITIALIZATION - POPULATION OF PARTS DATA FROM (LOCAL) DESIGN
            GenerateFormHelper.PopulatePartsFromDesign(cbPart, _design);
            GenerateFormHelper.LoadEndBarTotalFromDesign(lblEndBarTotal, _design);

            // FORM CONTROL INITIALIZATION
            GenerateFormHelper.LoadNoteValues(cbNoteValue);
        }

        // TODO REVISIT
        private void btnApply_Click(object sender, EventArgs e)
        {
            GenerateFormHelper.Apply(this, 
                _score, 
                cbPart, 
                numStaff, 
                numStartBar, 
                numEndBar, 
                cbStep, 
                cbAccidental, 
                numOctaveAbs, 
                cbNoteValue, 
                numNumberOfNotes);
        }

        // SET DESIGN AND GENERATE DEFAULTS
        private void btnSetDefault_Click(object? sender, EventArgs e)
        {
            _design = GenerateFormHelper.SetDefaults(cbPart, numEndBar, numNumberOfNotes, rbPitchAbsolute, cbStep, cbAccidental, cbPattern, lblEndBarTotal);
            // TODO - Must refresh UI - only those that use _design in the signature!
        }

        private void btnNewScore_Click(object sender, EventArgs e)
        {
            _score = GenerateFormHelper.NewScore(this, _design, cbPart, lblEndBarTotal);
        }
    }
}