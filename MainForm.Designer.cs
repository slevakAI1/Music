namespace Music
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem MenuFile;
        private System.Windows.Forms.ToolStripMenuItem MenuExportMusicXml;
        private System.Windows.Forms.ToolStripMenuItem MenuForm;
        private System.Windows.Forms.ToolStripMenuItem MenuMusic;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            menuStrip1 = new MenuStrip();
            MenuFile = new ToolStripMenuItem();
            MenuImportMusicXml = new ToolStripMenuItem();
            MenuExportMusicXml = new ToolStripMenuItem();
            MenuImportMidi = new ToolStripMenuItem();
            MenuForm = new ToolStripMenuItem();
            MenuMusic = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            menuStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { MenuFile, MenuForm });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(613, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // MenuFile
            // 
            MenuFile.DropDownItems.AddRange(new ToolStripItem[] { MenuImportMusicXml, MenuExportMusicXml, MenuImportMidi });
            MenuFile.Name = "MenuFile";
            MenuFile.Size = new Size(37, 20);
            MenuFile.Text = "File";
            // 
            // MenuImportMusicXml
            // 
            MenuImportMusicXml.Name = "MenuImportMusicXml";
            MenuImportMusicXml.Size = new Size(180, 22);
            MenuImportMusicXml.Text = "Import MusicXml";
            MenuImportMusicXml.Click += MenuImportMusicXml_Click;
            // 
            // MenuExportMusicXml
            // 
            MenuExportMusicXml.Name = "MenuExportMusicXml";
            MenuExportMusicXml.Size = new Size(180, 22);
            MenuExportMusicXml.Text = "Export MusicXML";
            MenuExportMusicXml.Click += MenuExportMusicXml_Click;
            // 
            // MenuImportMidi
            // 
            MenuImportMidi.Name = "MenuImportMidi";
            MenuImportMidi.Size = new Size(180, 22);
            MenuImportMidi.Text = "Import MIDI";
            MenuImportMidi.Click += MenuImportMidi_Click;
            // 
            // MenuForm
            // 
            MenuForm.DropDownItems.AddRange(new ToolStripItem[] { MenuMusic });
            MenuForm.Name = "MenuForm";
            MenuForm.Size = new Size(47, 20);
            MenuForm.Text = "Form";
            // 
            // MenuMusic
            // 
            MenuMusic.Name = "MenuMusic";
            MenuMusic.Size = new Size(106, 22);
            MenuMusic.Text = "Music";
            MenuMusic.Click += MenuFormMusic_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1 });
            statusStrip1.Location = new Point(0, 324);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(613, 22);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(63, 17);
            toolStripStatusLabel1.Text = "Status Info";
            // 
            // MainForm
            // 
            ClientSize = new Size(613, 346);
            Controls.Add(menuStrip1);
            Controls.Add(statusStrip1);
            IsMdiContainer = true;
            MainMenuStrip = menuStrip1;
            Name = "MainForm";
            Text = "MDI Parent";
            WindowState = FormWindowState.Maximized;
            Load += Form1_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStripMenuItem MenuImportMusicXml;
        private ToolStripMenuItem MenuImportMidi;
    }
}
