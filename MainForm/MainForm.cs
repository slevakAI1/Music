namespace Music
{
    public partial class MainForm : Form
    {
        private readonly FileManager _fileManager;

        public MainForm()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            this.IsMdiContainer = true;

            _fileManager = new FileManager(ShowStatus);

            // Show MusicForm on startup, filling the MDI parent
            ShowChildForm(typeof(GenerateForm));
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        // Show or activate a child form. Let the child manage its own size/state.
        private void ShowChildForm(Type childType)
        {
            var existing = this.MdiChildren.FirstOrDefault(f => f.GetType() == childType);
            if (existing != null)
            {
                if (existing.WindowState != FormWindowState.Maximized)
                    existing.WindowState = FormWindowState.Maximized;
                existing.Activate();
                return;
            }

            Form child = (Form)Activator.CreateInstance(childType)!;
            child.MdiParent = this;
            // Do not force WindowState/FormBorderStyle/Minimize/Maximize here.
            child.Show();
        }

        private void ShowStatus(String message)
        {
            if (statusStrip1 != null && statusStrip1.Items.Count > 0)
                statusStrip1.Items[0].Text = message;
        }

        //                   Tool Strip Methods

        private void MenuImportMusicXml_Click(object sender, EventArgs e)
        {
            _fileManager.ImportMusicXml(this);
        }

        private void MenuExportMusicXml_Click(object sender, EventArgs e)
        {
            _fileManager.ExportMusicXml(this);
        }

        private void MenuGenerateForm_Click(object sender, EventArgs e)
        {
            ShowChildForm(typeof(GenerateForm));
        }

        private void MenuTestForm_Click(object sender, EventArgs e)
        {
            ShowChildForm(typeof(TestForm));
        }
    }
}
