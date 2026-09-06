using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ExtendedHotbar.Helper
{
    internal enum HotbarStatus { Stopped, Pending, Active, Inactive, Unknown }
    internal static class LoadStatus
    {
        internal static HotbarStatus Read(string game)
        {
            var running = Process.GetProcessesByName("DungeonSettlers");
            try
            {
                if (running.Length == 0) return HotbarStatus.Stopped;
                if (running.Length != 1) return HotbarStatus.Unknown;
                if (!string.Equals(running[0].MainModule.FileName, Files.Under(game, "DungeonSettlers.exe"), StringComparison.OrdinalIgnoreCase)) return HotbarStatus.Unknown;
                var path = Files.Under(game, "MelonLoader/Latest.log");
                if (!File.Exists(path)) return HotbarStatus.Pending;
                // Read-only and shared: never lock MelonLoader's writer.
                if (new FileInfo(path).Length > 4 * 1024 * 1024) return HotbarStatus.Unknown;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream)) return Assess(reader.ReadToEnd(), game, running[0].StartTime.ToUniversalTime(), File.GetLastWriteTimeUtc(path));
            }
            catch { return HotbarStatus.Unknown; }
            finally { foreach (var process in running) process.Dispose(); }
        }
        internal static HotbarStatus Assess(string log, string game, DateTime startedUtc, DateTime writtenUtc)
        {
            if (writtenUtc < startedUtc || !log.Contains("Game::BasePath = " + game + "\r\n") && !log.Contains("Game::BasePath = " + game + "\n")) return HotbarStatus.Unknown;
            var firstTime = Regex.Match(log, @"\A\[([0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3})\]");
            TimeSpan time;
            if (!firstTime.Success || !TimeSpan.TryParse(firstTime.Groups[1].Value, out time)) return HotbarStatus.Unknown;
            var localStart = startedUtc.ToLocalTime();
            var distance = new[] { -1, 0, 1 }.Min(day => Math.Abs((localStart.Date.AddDays(day).Add(time) - localStart).TotalSeconds));
            if (distance > 3) return HotbarStatus.Unknown;
            var hotbarLines = log.Split('\n').Where(line => line.Contains("[Extended_Hotbar]")).ToArray();
            bool ready = hotbarLines.Any(line => line.Contains("10-slot release patches loaded;"));
            bool failed = hotbarLines.Any(line => line.Contains("10-Slot-Erweiterung sicher deaktiviert:") || line.Contains("10-Slot-Erweiterung deaktiviert;") || line.Contains("Unbekannte Skill-Indizierung: Erweiterung deaktiviert."));
            if (ready && failed) return HotbarStatus.Unknown;
            if (failed) return HotbarStatus.Inactive;
            if (ready) return HotbarStatus.Active;
            if (!Regex.IsMatch(log, @"\b[0-9]+ Mods? loaded\.")) return HotbarStatus.Pending;
            bool discovered = log.Contains("DungeonSettlers10Slots.dll") || log.Contains("DungeonSettlers12Slots.dll") || log.Contains("Extended Hotbar v");
            return discovered ? HotbarStatus.Pending : HotbarStatus.Inactive;
        }
    }
}
