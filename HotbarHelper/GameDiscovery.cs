using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace ExtendedHotbar.Helper
{
    // Discovery is advisory and read-only. Installation still verifies the game
    // fingerprints independently; no discovered executable is ever launched.
    internal static class GameDiscovery
    {
        internal const string AppId = "2798330";
        private const int MaxFolders = 512, MaxLibraries = 64;
        private static readonly string[] GameFiles = { "DungeonSettlers.exe", "GameAssembly.dll", "DungeonSettlers_Data/il2cpp_data/Metadata/global-metadata.dat" };

        internal static string[] FindSystem(CancellationToken token)
        {
            var steam = new List<string>(); var direct = new List<string>(); var copies = new List<string>();
            foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    using (var root = RegistryKey.OpenBaseKey(hive, view))
                    {
                        using (var key = root.OpenSubKey(@"Software\Valve\Steam"))
                            if (key != null) { steam.Add(key.GetValue("SteamPath") as string); steam.Add(key.GetValue("InstallPath") as string); }
                        using (var key = root.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Steam App " + AppId))
                            if (key != null) direct.Add(key.GetValue("InstallLocation") as string);
                    }
                }
                catch (Exception ex) when (Unavailable(ex)) { }
            }
            foreach (var folder in new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86 })
            {
                var path = Environment.GetFolderPath(folder);
                if (!string.IsNullOrWhiteSpace(path)) steam.Add(Path.Combine(path, "Steam"));
            }
            foreach (var drive in DriveInfo.GetDrives())
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    if ((drive.DriveType != DriveType.Fixed && drive.DriveType != DriveType.Removable) || !drive.IsReady) continue;
                    foreach (var relative in new[] { "Steam", "SteamLibrary", "Games/Steam", "Games/SteamLibrary", "Spiele/Steam", "Spiele/SteamLibrary" })
                        steam.Add(Path.Combine(drive.Name, relative.Replace('/', Path.DirectorySeparatorChar)));
                    // Only named game/test containers, never a recursive whole-disk scan.
                    foreach (var folder in Directory.EnumerateDirectories(drive.Name).Take(256))
                        if (GameContainer(Path.GetFileName(folder))) copies.Add(folder);
                }
                catch (Exception ex) when (Unavailable(ex)) { }
            }
            return Find(steam, direct, copies, token);
        }

        internal static string[] Find(IEnumerable<string> steamRoots, IEnumerable<string> directRoots, IEnumerable<string> copyRoots, CancellationToken token)
        {
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pending = new Queue<string>(); var timer = Stopwatch.StartNew();
            foreach (var candidate in steamRoots.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxLibraries))
            {
                token.ThrowIfCancellationRequested(); var path = LocalFolder(candidate);
                if (path != null && libraries.Add(path)) pending.Enqueue(path);
            }
            foreach (var candidate in directRoots.Take(MaxLibraries)) AddGame(found, candidate, token);
            while (pending.Count != 0 && timer.Elapsed < TimeSpan.FromSeconds(5))
            {
                token.ThrowIfCancellationRequested(); var library = pending.Dequeue();
                foreach (var file in new[] { "steamapps/libraryfolders.vdf", "config/libraryfolders.vdf" })
                {
                    var document = ReadVdf(library, file, token);
                    var folders = Child(document, "libraryfolders");
                    if (folders == null) continue;
                    foreach (var item in folders)
                    {
                        token.ThrowIfCancellationRequested(); int index;
                        if (!int.TryParse(item.Key, NumberStyles.None, CultureInfo.InvariantCulture, out index)) continue;
                        var entry = item.Value as Dictionary<string, object>;
                        var path = LocalFolder(entry == null ? item.Value as string : Value(entry, "path"));
                        if (path != null && libraries.Count < MaxLibraries && libraries.Add(path)) pending.Enqueue(path);
                    }
                }
                AddGame(found, Path.Combine(library, "steamapps", "common", "Dungeon Settlers"), token);
                var state = Child(ReadVdf(library, "steamapps/appmanifest_" + AppId + ".acf", token), "AppState");
                var install = Value(state, "installdir");
                if (Value(state, "appid") == AppId && Leaf(install))
                    AddGame(found, Path.Combine(library, "steamapps", "common", install), token);
                // Additional named test copies beside the Steam installation.
                foreach (var folder in Subfolders(Path.Combine(library, "steamapps", "common"), token))
                    if (GameContainer(Path.GetFileName(folder))) AddGame(found, folder, token);
            }
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<KeyValuePair<string, int>>();
            foreach (var path in copyRoots.Take(MaxFolders)) queue.Enqueue(new KeyValuePair<string, int>(path, 0));
            while (queue.Count != 0 && visited.Count < MaxFolders && timer.Elapsed < TimeSpan.FromSeconds(5))
            {
                token.ThrowIfCancellationRequested(); var item = queue.Dequeue(); var folder = LocalFolder(item.Key);
                if (folder == null || Archived(Path.GetFileName(folder)) || !visited.Add(folder)) continue;
                AddGame(found, folder, token);
                if (item.Value >= 3 || found.Contains(folder)) continue;
                foreach (var child in Subfolders(folder, token))
                {
                    if (queue.Count + visited.Count >= MaxFolders) break;
                    queue.Enqueue(new KeyValuePair<string, int>(child, item.Value + 1));
                }
            }
            token.ThrowIfCancellationRequested();
            return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        internal static string InitialChoice(string[] results, string current, bool unchanged)
        { return unchanged && string.IsNullOrWhiteSpace(current) && results.Length == 1 ? results[0] : current; }

        private static bool GameContainer(string name)
        {
            return !Archived(name) && (name.IndexOf("dungeon", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("dnst", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("test", StringComparison.OrdinalIgnoreCase) >= 0 || name.Equals("Games", StringComparison.OrdinalIgnoreCase) || name.Equals("Spiele", StringComparison.OrdinalIgnoreCase));
        }
        private static bool Archived(string name)
        { return name.IndexOf("backup", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("archiv", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("sicherung", StringComparison.OrdinalIgnoreCase) >= 0; }
        private static bool Unavailable(Exception ex)
        { return ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException || ex is ArgumentException || ex is NotSupportedException; }
        private static string LocalFolder(string path)
        {
            var local = LocalPath(path);
            return local != null && Directory.Exists(local) ? local : null;
        }
        private static string LocalPath(string path)
        {
            try
            {
                // Reject UNC/device/relative/mapped-network paths before any file access.
                if (string.IsNullOrWhiteSpace(path) || path.Length < 4 || !char.IsLetter(path[0]) || path[1] != ':' || (path[2] != '\\' && path[2] != '/')) return null;
                var drive = new DriveInfo(path.Substring(0, 3));
                if ((drive.DriveType != DriveType.Fixed && drive.DriveType != DriveType.Removable) || !drive.IsReady) return null;
                var local = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (local.Length < 4 || local.IndexOf(':', 2) >= 0) return null;
                var root = Path.GetPathRoot(local); var ancestor = root;
                // Inspect links from the drive towards the leaf. Checking a child
                // first could traverse a junction (including a network target).
                foreach (var part in local.Substring(root.Length).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                {
                    ancestor = Path.Combine(ancestor, part);
                    if ((File.GetAttributes(ancestor) & FileAttributes.ReparsePoint) != 0) return null;
                }
                return local;
            }
            catch (Exception ex) when (Unavailable(ex)) { return null; }
        }
        private static void AddGame(HashSet<string> found, string candidate, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); var folder = LocalFolder(candidate); if (folder == null) return;
            try { if (GameFiles.All(x => File.Exists(LocalPath(Path.Combine(folder, x.Replace('/', Path.DirectorySeparatorChar)))))) found.Add(folder); }
            catch (Exception ex) when (Unavailable(ex)) { }
        }
        private static string[] Subfolders(string candidate, CancellationToken token)
        {
            var folder = LocalFolder(candidate); if (folder == null) return new string[0];
            try
            {
                var result = new List<string>();
                foreach (var child in Directory.EnumerateDirectories(folder).Take(MaxFolders))
                { token.ThrowIfCancellationRequested(); result.Add(child); }
                return result.ToArray();
            }
            catch (Exception ex) when (Unavailable(ex)) { return new string[0]; }
        }
        private static bool Leaf(string value)
        { return !string.IsNullOrWhiteSpace(value) && value != "." && value != ".." && value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !value.EndsWith(".") && !value.EndsWith(" "); }
        private static Dictionary<string, object> Child(Dictionary<string, object> map, string key)
        { object value; return map != null && map.TryGetValue(key, out value) ? value as Dictionary<string, object> : null; }
        private static string Value(Dictionary<string, object> map, string key)
        { object value; return map != null && map.TryGetValue(key, out value) ? value as string : null; }
        private static Dictionary<string, object> ReadVdf(string folder, string relative, CancellationToken token)
        {
            try
            {
                var path = LocalPath(Path.Combine(folder, relative.Replace('/', Path.DirectorySeparatorChar))); if (path == null || !File.Exists(path)) return null;
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    return new Vdf(Files.Text(Updates.ReadBounded(file, 1024 * 1024, token)), token).Read();
            }
            catch (Exception ex) when (Unavailable(ex) || ex is FormatException) { return null; }
        }

        // A small bounded reader for Steam's quoted KeyValues data. Comments,
        // braces and escaping are parsed rather than searching arbitrary text.
        private sealed class Vdf
        {
            private readonly string source; private readonly CancellationToken token; private int index, pairs;
            internal Vdf(string source, CancellationToken token) { this.source = source; this.token = token; }
            internal Dictionary<string, object> Read() { return Map(0); }
            private Dictionary<string, object> Map(int depth)
            {
                if (depth > 16) throw new FormatException("VDF nesting limit.");
                var map = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                while (true)
                {
                    token.ThrowIfCancellationRequested(); Space();
                    if (index == source.Length) { if (depth != 0) throw new FormatException("Incomplete VDF."); return map; }
                    if (source[index] == '}') { if (depth == 0) throw new FormatException("Unexpected VDF close."); index++; return map; }
                    if (++pairs > 8192) throw new FormatException("VDF entry limit.");
                    var key = Quoted(); Space();
                    if (index == source.Length || map.ContainsKey(key)) throw new FormatException("Invalid VDF entry.");
                    object value;
                    if (source[index] == '{') { index++; value = Map(depth + 1); } else value = Quoted();
                    map.Add(key, value);
                }
            }
            private void Space()
            {
                while (index < source.Length)
                {
                    if (char.IsWhiteSpace(source[index])) { index++; continue; }
                    if (source[index] == '/' && index + 1 < source.Length && source[index + 1] == '/')
                    { while (index < source.Length && source[index] != '\n') index++; continue; }
                    break;
                }
            }
            private string Quoted()
            {
                if (index >= source.Length || source[index++] != '"') throw new FormatException("Quoted VDF string expected.");
                var result = new StringBuilder();
                while (index < source.Length)
                {
                    char c = source[index++]; if (c == '"') return result.ToString();
                    if (c == '\\' && index < source.Length && (source[index] == '\\' || source[index] == '"')) c = source[index++];
                    result.Append(c);
                }
                throw new FormatException("Unclosed VDF string.");
            }
        }
    }
}
