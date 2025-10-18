using System.Collections.Generic;
using System.Windows.Forms;

namespace Music.Generate
{
    public sealed class VoiceManager
    {
        public void AddDefaultVoicesAndRender(IWin32Window owner, ScoreDesign? structure, TextBox txtVoiceSet)
        {
            if (structure == null)
            {
                MessageBox.Show(owner, "Create the song structure first.", "No Structure", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            structure.AddVoices();

            var lines = new List<string>();
            foreach (var v in structure.Voices)
                lines.Add(v.Value);

            // One voice per line in the voiceset textbox
            txtVoiceSet.Lines = lines.ToArray();
        }
    }
}