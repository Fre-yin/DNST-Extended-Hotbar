using System;
using System.IO;

namespace ExtendedHotbar.Helper
{
    // Only the local Downloads scan has a preference. MainForm supplies the
    // existing local-update-preferences.json path; old online settings are unused.
    internal sealed class LocalUpdatePreferences
    {
        internal bool Automatic = true;
        internal static LocalUpdatePreferences Load(string path)
        {
            var value = new LocalUpdatePreferences();
            if (!File.Exists(path)) return value;
            try
            {
                if (new FileInfo(path).Length > 2048) throw new FormatException("Preferences exceed size limit.");
                var node = new LosslessJson(Files.Text(Files.Read(path))).Root;
                if (node.Get("automatic").Kind != "bool") throw new FormatException();
                value.Automatic = node.Get("automatic").Text == "true";
                // Ignore the historical includeTests field. It never changes
                // local package selection or re-enables an online operation.
            }
            catch { value.Automatic = false; } // Damaged local preferences fail closed.
            return value;
        }
        internal void Save(string path)
        { Files.Atomic(path, Files.Utf8.GetBytes("{\"automatic\":" + (Automatic ? "true" : "false") + "}")); }
    }
}
