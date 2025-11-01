using Music.Designer;
using MusicXml.Domain;

namespace Music.Generator
{
    public partial class GeneratorForm : Form
    {
        private Score? _score;
        private DesignerData? _design;

        //===========================   I N I T I A L I Z A T I O N   ===========================
        public GeneratorForm()
        {
            InitializeComponent();

            // Window behavior similar to other forms
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;

            // Load current global score and design into form-local fields for later use
            // Constructor is the only place that reads Globals per requirement.
            _score = Globals.Score;
            _design = Globals.Design;

            // Capture form control values manually set in the form designer
            // This will only be done once, at form construction time.
            Globals.GenerationData ??= CaptureFormData();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (this.MdiParent != null && this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
        }

        // The Generator form is activated each time it gains focus.
        // The initialization of controls is controlled entirely by the current Design and persisted GenerationData.
        // It does not depend on the prior state of the controls.
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // Merge in any design changes that may have happened while outside this form
            Globals.GenerationData?.ApplyDesignDefaults(_design);

            // Update the form to take into account any design changes
            ApplyFormData(Globals.GenerationData);
        }

        // Persist current control state whenever the form loses activation (user switches to another MDI child)
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Globals.GenerationData?.ApplyDesignDefaults(_design);
            ApplyFormData(Globals.GenerationData);
        }

        //===============================   E V E N T S   ==============================

        private void btnApplySetNotes_Click(object sender, EventArgs e)
        {
            // Persist current control state and pass the captured DTO to PatternSetNotes.
            // All control-to-primitive mapping/logic is handled inside PatternSetNotes.Apply(Score, GenerationData).
            Globals.GenerationData = CaptureFormData();
            if (Globals.GenerationData != null)
            {
                PatternSetNotes.Apply1(_score!, Globals.GenerationData);
                Globals.Score = _score;
            }
        }

        private void btnNewScore_Click(object sender, EventArgs e)
        {
            _score = ScoreHelper.NewScore(this, _design, clbParts, lblEndBarTotal);
        }


        //===========================================================================================
        //                      T E S T   S C E N A R I O   B U T T O N S
        //  

        // This sets design test scenario D1
        private void btnSetDesignTestScenarioD1_Click(object sender, EventArgs e)
        {
            // Ensure design exists and apply design defaults
            Globals.Design ??= new DesignerData();
            DesignerTests.SetTestDesignD1(Globals.Design);
        }

        // This sets generator test scenario G1
        // Description: Set generator test values using the current design (in Globals)
        private void btnSetGeneratorTestScenarioG1_Click(object sender, EventArgs e)
        {
            // Merge in any design changes
            Globals.GenerationData?.ApplyDesignDefaults(_design);

            // Get GenerationData defaults from helper (no UI controls passed)
            Globals.GenerationData = GeneratorTestHelpers.SetTestGeneratorG1(Globals.Design);

            // Apply the generated defaults into the form controls via the instance method
            ApplyFormData(Globals.GenerationData);
        }
    }
}