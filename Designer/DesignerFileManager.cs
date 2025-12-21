using Music.Writer;
using System.Text.Json;

namespace Music.Designer
{
    internal static class DesignerFileManager
    {
        public static void SaveDesign(Form owner)
        {
            if (Globals.Designer == null) return;

            try
            {
                var design = Globals.Designer!;

                // Explicit snapshot to ensure TempoTrack and TimeSignatureTrack (and their Events) are serialized.
                var snapshot = new
                {
                    design.DesignId,
                    design.Voices,
                    design.SectionTrack,
                    design.HarmonyTrack,
                    design.TempoTrack,
                    design.TimeSignatureTrack
                };

                var json = JsonSerializer.Serialize(
                    snapshot,
                    new JsonSerializerOptions { WriteIndented = true });

                // Ensure the target folder exists under the project root: Design/Designs
                var baseDir = AppContext.BaseDirectory;
                var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
                var targetDir = Path.Combine(projectRoot, "Design", "Designs");
                Directory.CreateDirectory(targetDir);

                var fileName = $"Design-{design.DesignId}.json";
                var fullPath = Path.Combine(targetDir, fileName);

                File.WriteAllText(fullPath, json);

                MessageBoxHelper.Show(owner, $"Design saved to:\n{fullPath}", "Saved",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show(owner, $"Failed to save design.\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Overload: Save a specific Designer instance
        public static void SaveDesign(Form owner, Designer designer)
        {
            if (designer == null) return;

            try
            {
                // Explicit snapshot to ensure TempoTrack and TimeSignatureTrack (and their Events) are serialized.
                var snapshot = new
                {
                    designer.DesignId,
                    designer.Voices,
                    designer.SectionTrack,
                    designer.HarmonyTrack,
                    designer.GrooveTrack,
                    designer.TempoTrack,
                    designer.TimeSignatureTrack
                };

                var json = JsonSerializer.Serialize(
                    snapshot,
                    new JsonSerializerOptions { WriteIndented = true });

                // Ensure the target folder exists under the project root: Design/Designs
                var baseDir = AppContext.BaseDirectory;
                var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
                var targetDir = Path.Combine(projectRoot, "Design", "Designs");
                Directory.CreateDirectory(targetDir);

                var fileName = $"Design-{designer.DesignId}.json";
                var fullPath = Path.Combine(targetDir, fileName);

                File.WriteAllText(fullPath, json);

                MessageBoxHelper.Show(owner, $"Design saved to:\n{fullPath}", "Saved",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show(owner, $"Failed to save design.\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static bool LoadDesign(Form owner)
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
                var defaultDir = Path.Combine(projectRoot, "Design", "Designs");

                using var ofd = new OpenFileDialog
                {
                    Filter = "Design Files (*.json)|*.json|All Files (*.*)|*.*",
                    Title = "Open Design",
                    InitialDirectory = defaultDir,
                    RestoreDirectory = true
                };

                if (ofd.ShowDialog(owner) != DialogResult.OK) return false;

                var json = File.ReadAllText(ofd.FileName);

                // Robust manual deserialization to rebuild computed fields
                var loaded = DesignerSerialization.DeserializeDesign(json);

                Globals.Designer = loaded;

                MessageBoxHelper.Show(owner, $"Design loaded from:\n{ofd.FileName}", "Loaded",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                return true;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show(owner, $"Failed to load design.\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Overload: Load into a specific Designer instance (returns the loaded designer)
        public static Designer? LoadDesign(Form owner, out bool success)
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
                var defaultDir = Path.Combine(projectRoot, "Design", "Designs");

                using var ofd = new OpenFileDialog
                {
                    Filter = "Design Files (*.json)|*.json|All Files (*.*)|*.*",
                    Title = "Open Design",
                    InitialDirectory = defaultDir,
                    RestoreDirectory = true
                };

                if (ofd.ShowDialog(owner) != DialogResult.OK)
                {
                    success = false;
                    return null;
                }

                var json = File.ReadAllText(ofd.FileName);

                // Robust manual deserialization to rebuild computed fields
                var loaded = DesignerSerialization.DeserializeDesign(json);

                MessageBoxHelper.Show(owner, $"Design loaded from:\n{ofd.FileName}", "Loaded",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                success = true;
                return loaded;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show(owner, $"Failed to load design.\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                success = false;
                return null;
            }
        }
    }
}