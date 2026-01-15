namespace Music
{
    partial class SerializerTestForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnTestSerializer = new Button();
            btnTestParser = new Button();
            btnCreatTestMusicXmlFile = new Button();
            SuspendLayout();
            // 
            // btnTestSerializer
            // 
            btnTestSerializer.Location = new Point(301, 213);
            btnTestSerializer.Name = "btnTestSerializer";
            btnTestSerializer.Size = new Size(200, 23);
            btnTestSerializer.TabIndex = 18;
            btnTestSerializer.Text = "Test Export/Serializer";
            btnTestSerializer.UseVisualStyleBackColor = true;
            btnTestSerializer.Click += btnTestSerializer_Click;
            // 
            // btnTestParser
            // 
            btnTestParser.Location = new Point(301, 175);
            btnTestParser.Name = "btnTestParser";
            btnTestParser.Size = new Size(200, 23);
            btnTestParser.TabIndex = 17;
            btnTestParser.Text = "Test Import/Parser";
            btnTestParser.UseVisualStyleBackColor = true;
            btnTestParser.Click += btnTestParser_Click;
            // 
            // btnCreatTestMusicXmlFile
            // 
            btnCreatTestMusicXmlFile.Location = new Point(308, 298);
            btnCreatTestMusicXmlFile.Name = "btnCreatTestMusicXmlFile";
            btnCreatTestMusicXmlFile.Size = new Size(193, 23);
            btnCreatTestMusicXmlFile.TabIndex = 19;
            btnCreatTestMusicXmlFile.Text = "Create Text MusicXml File";
            btnCreatTestMusicXmlFile.UseVisualStyleBackColor = true;
            btnCreatTestMusicXmlFile.Click += btnCreateTestMusicXmlFile_Click;
            // 
            // TestForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnCreatTestMusicXmlFile);
            Controls.Add(btnTestSerializer);
            Controls.Add(btnTestParser);
            Name = "TestForm";
            Text = "TestForm";
            Load += TestForm_Load;
            ResumeLayout(false);
        }

        #endregion

        private Button btnTestSerializer;
        private Button btnTestParser;
        private Button btnCreatTestMusicXmlFile;
    }
}