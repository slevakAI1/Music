namespace Music.Generate
{
    public sealed class VoiceManagerClass
    {
        // Persist in VoiceSet; text box is display only
        public void AddDefaultVoices(IWin32Window owner, SectionSetClass? structure, VoiceSetClass voiceSet, TextBox txtVoiceSet)
        {
            if (structure == null)
            {
                MessageBox.Show(owner, "Create the song structure first.", "No Structure", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var voices = voiceSet.AddDefaultVoices();

            // Display on form
            var lines = new List<string>();
            foreach (var v in voices)
                lines.Add(v.VoiceName);
            txtVoiceSet.Lines = lines.ToArray();
        }
    }
}