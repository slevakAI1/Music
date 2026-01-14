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
        ///  Required method for SongContext support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            menuStrip1 = new MenuStrip();
            MenuFile = new ToolStripMenuItem();
            MenuImportMusicXml = new ToolStripMenuItem();
            MenuExportMusicXml = new ToolStripMenuItem();
            viewMusicXmlToolStripMenuItem = new ToolStripMenuItem();
            importMidiToolStripMenuItem = new ToolStripMenuItem();
            playMidiFileToolStripMenuItem = new ToolStripMenuItem();
            generateToolStripMenuItem = new ToolStripMenuItem();
            arrangerToolStripMenuItem = new ToolStripMenuItem();
            writerToolStripMenuItem = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            menuStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { MenuFile, generateToolStripMenuItem, arrangerToolStripMenuItem, writerToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(796, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // MenuFile
            // 
            MenuFile.DropDownItems.AddRange(new ToolStripItem[] { MenuImportMusicXml, MenuExportMusicXml, viewMusicXmlToolStripMenuItem, importMidiToolStripMenuItem, playMidiFileToolStripMenuItem });
            MenuFile.Name = "MenuFile";
            MenuFile.Size = new Size(37, 20);
            MenuFile.Text = "File";
            // 
            // MenuImportMusicXml
            // 
            MenuImportMusicXml.Name = "MenuImportMusicXml";
            MenuImportMusicXml.Size = new Size(166, 22);
            MenuImportMusicXml.Text = "Import MusicXml";
            MenuImportMusicXml.Click += MenuImportMusicXml_Click;
            // 
            // MenuExportMusicXml
            // 
            MenuExportMusicXml.Name = "MenuExportMusicXml";
            MenuExportMusicXml.Size = new Size(166, 22);
            MenuExportMusicXml.Text = "Export MusicXML";
            MenuExportMusicXml.Click += MenuExportMusicXml_Click;
            // 
            // viewMusicXmlToolStripMenuItem
            // 
            viewMusicXmlToolStripMenuItem.Name = "viewMusicXmlToolStripMenuItem";
            viewMusicXmlToolStripMenuItem.Size = new Size(166, 22);
            viewMusicXmlToolStripMenuItem.Text = "View MusicXml";
            viewMusicXmlToolStripMenuItem.Click += viewMusicXmlToolStripMenuItem_Click;
            // 
            // importMidiToolStripMenuItem
            // 
            importMidiToolStripMenuItem.Name = "importMidiToolStripMenuItem";
            importMidiToolStripMenuItem.Size = new Size(166, 22);
            importMidiToolStripMenuItem.Text = "Import Midi File";
            importMidiToolStripMenuItem.Click += importMidiToolStripMenuItem_Click;
            // 
            // playMidiFileToolStripMenuItem
            // 
            playMidiFileToolStripMenuItem.Name = "playMidiFileToolStripMenuItem";
            playMidiFileToolStripMenuItem.Size = new Size(166, 22);
            playMidiFileToolStripMenuItem.Text = "Play Midi File";
            playMidiFileToolStripMenuItem.Click += playMidiFileToolStripMenuItem_Click;
            // 
            // generateToolStripMenuItem
            // 
            generateToolStripMenuItem.Name = "generateToolStripMenuItem";
            generateToolStripMenuItem.Size = new Size(51, 20);
            generateToolStripMenuItem.Text = "Writer";
            generateToolStripMenuItem.Click += generateToolStripMenuItem_Click;
            // 
            // arrangerToolStripMenuItem
            // 
            arrangerToolStripMenuItem.Name = "arrangerToolStripMenuItem";
            arrangerToolStripMenuItem.Size = new Size(65, 20);
            arrangerToolStripMenuItem.Text = "Arranger";
            arrangerToolStripMenuItem.Click += arrangerToolStripMenuItem_Click;
            // 
            // writerToolStripMenuItem
            // 
            writerToolStripMenuItem.Name = "writerToolStripMenuItem";
            writerToolStripMenuItem.Size = new Size(40, 20);
            writerToolStripMenuItem.Text = "Test";
            writerToolStripMenuItem.Click += testToolStripMenuItem_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1 });
            statusStrip1.Location = new Point(0, 352);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(796, 22);
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
            ClientSize = new Size(796, 374);
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
        private ToolStripMenuItem generateToolStripMenuItem;
        private ToolStripMenuItem writerToolStripMenuItem;
        private ToolStripMenuItem viewMusicXmlToolStripMenuItem;
        private ToolStripMenuItem arrangerToolStripMenuItem;
        private ToolStripMenuItem importMidiToolStripMenuItem;
        private ToolStripMenuItem playMidiFileToolStripMenuItem;
    }
}
