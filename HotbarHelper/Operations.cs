using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace ExtendedHotbar.Helper
{
    internal static class ReleaseInfo
    {
        internal const string ModVersion = "0.3.7";
        internal const string GameHash = "B0CD8B641D551019B82C0AF3DDE1532D6FF7BA155B20B2D3936742924B42DB2A";
        internal const string MetadataHash = "CE84EC266501C8DF4A23B31D50D8B413C82DAE49A062BC623B3440E8A3F5A23F";
        internal const string PackageHash = "DA9C92F107298C211799DF678B5EFB267843F8755E84073DA6AB43CDAF6E30B1";
        internal const string Dll = "Mods/DungeonSettlers10Slots.dll";
        internal const string OldDll = "Mods/DungeonSettlers12Slots.dll";
        internal const string Asset = "Mods/DungeonSettlers10SlotsAssets/SkillFrame__sharedassets0_mod_4898.png";
        internal const string Notice = "Mods/DungeonSettlers10SlotsAssets/NOTICE.txt";
        internal static readonly string[] Owned = { Dll, OldDll, Asset, Notice };
        internal static Dictionary<string, byte[]> Package()
        {
            return ReadPackage(PackageBytes());
        }
        internal static byte[] PackageBytes()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HotbarPackage.zip"))
            {
                if (stream == null) throw new HelperFailure("errorPackage", "Eingebautes Mod-Paket fehlt.");
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory); var bytes = memory.ToArray();
                    if (Files.Hash(bytes) != PackageHash) throw new HelperFailure("errorPackage", "Prüfsumme des Mod-Pakets stimmt nicht.");
                    return bytes;
                }
            }
        }
        // Explicit export only: never extract or import saves, overwrite a file,
        // or modify game/profile paths as a side effect of installation/startup.
        internal static void ExportPackage(string path)
        {
            var folder = Files.Root(Path.GetDirectoryName(path));
            var target = Files.Under(folder, Path.GetFileName(path));
            if (!target.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) throw new HelperFailure("errorPath", "ZIP-Datei erwartet.");
            if (File.Exists(target) || Directory.Exists(target)) throw new HelperFailure("errorExportExists", "Bitte einen neuen Dateinamen wählen.");
            var bytes = PackageBytes();
            var temporary = Files.Under(folder, ".eh-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                Files.SafeAncestors(target);
                File.Move(temporary, target); // Fails safely if the destination appeared meanwhile.
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        internal static Dictionary<string, byte[]> ReadPackage(byte[] bytes)
        {
            if (Files.Hash(bytes) != PackageHash) throw new HelperFailure("errorPackage", "Prüfsumme des Mod-Pakets stimmt nicht.");
            using (var memory = new MemoryStream(bytes))
            using (var zip = new ZipArchive(memory, ZipArchiveMode.Read))
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
                foreach (var entry in zip.Entries)
                {
                    if (!names.Add(entry.FullName) || entry.FullName.Contains("..") || entry.Length > 4 * 1024 * 1024) throw new HelperFailure("errorPackage", "Ungültiges ZIP.");
                    if (!Owned.Contains(entry.FullName)) continue;
                    using (var input = entry.Open()) using (var target = new MemoryStream()) { input.CopyTo(target); result.Add(entry.FullName, target.ToArray()); }
                }
                if (result.Count != 3 || !result.ContainsKey(Dll) || !result.ContainsKey(Asset) || !result.ContainsKey(Notice)) throw new HelperFailure("errorPackage", "Installierbare Hotbar-Dateien fehlen.");
                return result;
            }
        }
    }

    internal static class Files
    {
        internal static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        internal static string Hash(byte[] bytes) { if (bytes == null) return null; using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", ""); }
        internal static string FileHash(string path) { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
        internal static string Text(byte[] bytes) { var text = Utf8.GetString(bytes); return text.Length > 0 && text[0] == '\uFEFF' ? text.Substring(1) : text; }
        internal static byte[] Read(string path)
        {
            SafeAncestors(path);
            if (Directory.Exists(path)) throw new HelperFailure("errorPath", "Datei erwartet, Ordner gefunden: " + path);
            if (!File.Exists(path)) return null;
            if (new FileInfo(path).Length > 64 * 1024 * 1024) throw new HelperFailure("errorIO", "Datei unerwartet groß: " + path);
            return File.ReadAllBytes(path);
        }
        internal static string Root(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !System.Text.RegularExpressions.Regex.IsMatch(path, @"\A[A-Za-z]:[\\/]")) throw new HelperFailure("errorPath", "Bitte einen vollständigen lokalen Ordnerpfad auswählen.");
            var root = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (root.Length < 4 || root.StartsWith(@"\\", StringComparison.Ordinal) || !Path.IsPathRooted(root) || root.IndexOf(':', 2) >= 0) throw new HelperFailure("errorPath", "Bitte einen lokalen Unterordner auswählen.");
            SafeAncestors(root); return root;
        }
        internal static string Under(string root, string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || relative.IndexOf(':') >= 0 || Path.IsPathRooted(relative) || relative.Split('/', '\\').Any(x => x == "." || x == ".." || x.Length == 0 || x.EndsWith(".") || x.EndsWith(" "))) throw new HelperFailure("errorPath", "Unsicherer relativer Pfad.");
            var path = Path.GetFullPath(Path.Combine(Root(root), relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(Root(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new HelperFailure("errorPath", "Pfad verlässt den Zielordner.");
            SafeAncestors(path); return path;
        }
        internal static void SafeAncestors(string path)
        {
            for (var current = Path.GetFullPath(path); !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
                if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new HelperFailure("errorPath", "Verknüpfte Ordner/Dateien werden aus Sicherheitsgründen nicht verändert: " + current);
        }
        internal static void Atomic(string path, byte[] bytes)
        {
            SafeAncestors(path);
            if (bytes == null) { if (File.Exists(path)) File.Delete(path); return; }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // Stay on the same filesystem for atomic replacement without appending
            // the full target name and pushing an otherwise valid path over MAX_PATH.
            var temp = Path.Combine(Path.GetDirectoryName(path), ".eh-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }

    internal interface IGamePolicy { void Validate(string game, bool compatible); void Stopped(); void OwnedFile(string path); }
    internal sealed class GamePolicy : IGamePolicy
    {
        public void Validate(string game, bool compatible)
        {
            Files.Root(game);
            if (!File.Exists(Files.Under(game, "DungeonSettlers.exe"))) throw new HelperFailure("errorGame", "DungeonSettlers.exe fehlt im ausgewählten Spielordner.");
            if (compatible && (Files.FileHash(Files.Under(game, "GameAssembly.dll")) != ReleaseInfo.GameHash || Files.FileHash(Files.Under(game, "DungeonSettlers_Data/il2cpp_data/Metadata/global-metadata.dat")) != ReleaseInfo.MetadataHash))
                throw new HelperFailure("errorBuild", "Dieser Spielbuild ist noch nicht geprüft. Erwartet: DS_B.0.4.19 / 25154317. Keine Änderung vorgenommen.");
        }
        public void Stopped()
        {
            var processes = Process.GetProcessesByName("DungeonSettlers");
            try { if (processes.Length != 0) throw new HelperFailure("errorRunning", "Bitte Dungeon Settlers und die Testkopie zuerst vollständig schließen."); }
            finally { foreach (var process in processes) process.Dispose(); }
        }
        public void OwnedFile(string path)
        {
            if (!File.Exists(path) || !path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) return;
            var name = AssemblyName.GetAssemblyName(path).Name;
            if (name != "DungeonSettlers10Slots" && name != "DungeonSettlers12Slots") throw new HelperFailure("errorForeign", "Unter einem Hotbar-Dateinamen liegt eine fremde DLL. Abbruch: " + path);
        }
    }

    public sealed class ChangeRecord
    {
        public string Area, Name, BeforeHash, AfterHash;
    }
    public sealed class TransactionRecord
    {
        public int Schema = 1;
        public string Id, Game, Profile, Action, Status, Time;
        public List<ChangeRecord> Entries = new List<ChangeRecord>();
    }
    internal sealed class PlannedFile
    {
        internal string Area, Name;
        internal byte[] After;
        internal bool CheckExpected;
        internal string ExpectedHash;
        internal PlannedFile(string area, string name, byte[] after) { Area = area; Name = name; After = after; }
        internal PlannedFile Expect(byte[] before) { CheckExpected = true; ExpectedHash = Files.Hash(before); return this; }
    }

    internal sealed class Operations
    {
        internal const string CharacterKeysAction = "Charaktertasten einrichten";
        private readonly IGamePolicy policy;
        internal readonly string Store;
        // Tests inject an isolated fake game policy; the GUI always uses GamePolicy.
        internal Operations(string store, IGamePolicy policy) { Store = Files.Root(store); this.policy = policy; }
        internal Action<int> AfterWrite { get; set; }
        internal Action BeforeCommit { get; set; }
        private static string Resolve(string game, string profile, string area, string name)
        {
            if (area == "game" && ReleaseInfo.Owned.Contains(name)) return Files.Under(game, name);
            if (area == "settings" && name == "UserSetting.json") return Files.Under(profile, name);
            if (area == "save" && !string.IsNullOrWhiteSpace(name) && name.Length <= 200 && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && Path.GetFileName(name) == name
                && !System.Text.RegularExpressions.Regex.IsMatch(name, @"\A(?:CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])(?:\.|\z)", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return Files.Under(profile, "Saves/" + name);
            throw new HelperFailure("errorPath", "Unzulässiges Wiederherstellungsziel.");
        }
        private IDisposable Lock()
        {
            Files.SafeAncestors(Store); Directory.CreateDirectory(Store);
            return new FileStream(Files.Under(Store, "operation.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        internal string Install(string game, string profile, Dictionary<string, byte[]> payload)
        {
            policy.Stopped(); policy.Validate(game, true);
            if (!File.Exists(Files.Under(game, "MelonLoader/net6/MelonLoader.dll"))) throw new HelperFailure("errorLoader", "Bitte zuerst MelonLoader 0.7.3 für dieses Spiel installieren.");
            var files = ReleaseInfo.Owned.Select(x => new PlannedFile("game", x, payload.ContainsKey(x) ? payload[x] : null)).ToList();
            if (payload.Count != 3 || !payload.ContainsKey(ReleaseInfo.Dll) || !payload.ContainsKey(ReleaseInfo.Asset) || !payload.ContainsKey(ReleaseInfo.Notice)) throw new HelperFailure("errorPackage", "Unvollständiges Mod-Paket.");
            var settings = Files.Read(Files.Under(profile, "UserSetting.json"));
            if (settings != null)
            {
                Profiles.Bindings(new LosslessJson(Files.Text(settings)));
                var restored = settings;
                // 0.1.0 also recorded installs that changed no files. Those must not
                // obscure the last actual change when restoring the Hotbar key profile.
                var previous = History(game, profile).FirstOrDefault(x => x.Entries.Any(e => e.BeforeHash != e.AfterHash));
                if (previous != null && previous.Status == "complete" && (previous.Action == "Ohne Hotbar vorbereiten" || previous.Action == "Originaltasten vorbereiten (ohne Spielstandänderung)"))
                {
                    int index = previous.Entries.FindIndex(x => x.Area == "settings");
                    if (index >= 0)
                    {
                        var old = Payload(previous, index, true); var native = Payload(previous, index, false);
                        if (old != null && native != null && Profiles.Same(Profiles.Selected(Files.Text(settings)), Profiles.Selected(Files.Text(native))))
                            restored = Files.Utf8.GetBytes(Profiles.Merge(Files.Text(settings), Profiles.Selected(Files.Text(old))));
                    }
                }
                files.Add(new PlannedFile("settings", "UserSetting.json", restored).Expect(settings));
            }
            return Execute(game, profile, "Installieren / Aktualisieren", files);
        }
        internal string Disable(string game, string profile, string sourceSave, string vanillaBindings, out string exported, string displaySuffix = "(ohne Hotbar)")
        {
            policy.Stopped(); policy.Validate(game, true);
            var source = Path.GetFullPath(sourceSave);
            if (!string.Equals(Path.GetDirectoryName(source), Files.Under(profile, "Saves"), StringComparison.OrdinalIgnoreCase)) throw new HelperFailure("errorSave", "Bitte einen Spielstand aus dem gewählten Saves-Ordner auswählen.");
            var sourceName = Path.GetFileName(source);
            Resolve(game, profile, "save", sourceName);
            var original = Files.Read(source);
            if (original == null) throw new HelperFailure("errorSave", "Spielstand fehlt.");
            // Unique file identity, not a new campaign identity. Existing saves
            // are never selected as an output target or overwritten on retries.
            var name = "OhneHotbar_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N");
            var converted = VanillaSave.Export(Files.Text(original), name, displaySuffix);
            var settingsPath = Files.Under(profile, "UserSetting.json");
            var settings = Files.Read(settingsPath);
            if (settings == null) throw new HelperFailure("errorSettings", "Benutzereinstellungen fehlen. Bitte das Spiel einmal normal einrichten.");
            var selected = new LosslessJson(vanillaBindings).Root;
            if (selected.Kind != "array" || selected.Items.Any(x => Profiles.Extra(x.Get("InputType").Integer))) throw new HelperFailure("errorKeys", "Das gewählte Tastenprofil enthält noch Hotbar-Zusatztasten.");
            var required = Enumerable.Range(4, 20).Concat(Enumerable.Range(34, 4)).Concat(new[] { 60 });
            if (required.Any(type => !selected.Items.Any(x => x.Get("InputType").Integer == type && x.Get("SlotIndex").Integer == 0))) throw new HelperFailure("errorKeys", "Die Original-Sicherung enthält nicht alle benötigten Tasten. Bitte eine vollständige Sicherung oder den Originalstandard wählen.");
            var restored = Profiles.Merge(Files.Text(settings), vanillaBindings);
            exported = Files.Under(profile, "Saves/" + name + ".json");
            if (File.Exists(exported)) throw new HelperFailure("errorSave", "Exportdatei existiert schon.");
            var files = new List<PlannedFile> {
                new PlannedFile("save", sourceName, original).Expect(original), // backed up, never rewritten
                new PlannedFile("save", name + ".json", Files.Utf8.GetBytes(converted)).Expect(null),
                new PlannedFile("settings", "UserSetting.json", Files.Utf8.GetBytes(restored)).Expect(settings)
            };
            files.AddRange(ReleaseInfo.Owned.Select(x => new PlannedFile("game", x, null)));
            return Execute(game, profile, "Ohne Hotbar vorbereiten", files);
        }
        internal string PrepareNativeKeys(string game, string profile)
        {
            policy.Stopped(); policy.Validate(game, true);
            var current = Files.Read(Files.Under(profile, "UserSetting.json"));
            if (current == null) throw new HelperFailure("errorSettings", "Benutzereinstellungen fehlen.");
            var native = Files.Utf8.GetBytes(Profiles.Merge(Files.Text(current), Profiles.VanillaDefaults()));
            return Execute(game, profile, "Originaltasten vorbereiten (ohne Spielstandänderung)", new List<PlannedFile> { new PlannedFile("settings", "UserSetting.json", native).Expect(current) });
        }
        internal List<JsonNode> PreviewCharacterKeys(string game, string profile, bool shift, out string settingsHash)
        {
            policy.Stopped(); policy.Validate(game, true);
            var current = Files.Read(Files.Under(profile, "UserSetting.json"));
            if (current == null) throw new HelperFailure("errorSettings", "Benutzereinstellungen fehlen.");
            settingsHash = Files.Hash(current);
            return Profiles.CharacterConflicts(Files.Text(current), shift);
        }
        internal string ConfigureCharacterKeys(string game, string profile, bool shift, bool overwriteConflicts = false, string expectedSettingsHash = null)
        {
            policy.Stopped(); policy.Validate(game, true);
            var current = Files.Read(Files.Under(profile, "UserSetting.json"));
            if (current == null) throw new HelperFailure("errorSettings", "Benutzereinstellungen fehlen.");
            if (expectedSettingsHash != null && Files.Hash(current) != expectedSettingsHash)
                throw new HelperFailure("errorConflict", "Die Tastenbelegung hat sich seit der angezeigten Zusammenfassung geändert. Bitte erneut prüfen.");
            var configured = Files.Utf8.GetBytes(Profiles.ConfigureCharacters(Files.Text(current), shift, overwriteConflicts));
            return Execute(game, profile, CharacterKeysAction, new List<PlannedFile> {
                new PlannedFile("settings", "UserSetting.json", configured).Expect(current)
            });
        }
        private static bool CharacterKeysOnly(string action)
        {
            const string undo = "Wiederherstellen: ";
            while (action != null && action.StartsWith(undo, StringComparison.Ordinal)) action = action.Substring(undo.Length);
            return action == CharacterKeysAction;
        }
        // A null result means successful verification with no file changes, not failure.
        private string Execute(string game, string profile, string action, List<PlannedFile> plan)
        {
            game = Files.Root(game); profile = Files.Root(profile);
            using (Lock())
            {
                policy.Stopped();
                if (History(game, profile).Any(x => x.Status == "prepared" || x.Status == "recovery-needed")) throw new HelperFailure("errorRecovery", "Eine unterbrochene Änderung muss zuerst über Wiederherstellen abgeschlossen werden.");
                var snapshots = new List<byte[]>();
                foreach (var item in plan)
                {
                    var path = Resolve(game, profile, item.Area, item.Name);
                    if (item.Area == "game") policy.OwnedFile(path);
                    var before = Files.Read(path);
                    if (item.CheckExpected && Files.Hash(before) != item.ExpectedHash) throw new HelperFailure("errorConflict", "Datei seit der Auswahl geändert; bitte erneut versuchen: " + path);
                    snapshots.Add(before);
                }
                policy.Stopped();
                if (plan.Select((item, index) => Files.Hash(snapshots[index]) == Files.Hash(item.After)).All(same => same)) return null;
                var record = new TransactionRecord { Id = Guid.NewGuid().ToString("N"), Game = game, Profile = profile, Action = action, Status = "preparing", Time = DateTime.UtcNow.ToString("o") };
                var folder = Files.Under(Store, record.Id); Directory.CreateDirectory(folder);
                for (int i = 0; i < plan.Count; i++)
                {
                    var item = plan[i]; var before = snapshots[i];
                    if (before != null) File.WriteAllBytes(Files.Under(folder, i + ".before"), before);
                    if (item.After != null) File.WriteAllBytes(Files.Under(folder, i + ".after"), item.After);
                    record.Entries.Add(new ChangeRecord { Area = item.Area, Name = item.Name, BeforeHash = Files.Hash(before), AfterHash = Files.Hash(item.After) });
                }
                if (BeforeCommit != null) BeforeCommit();
                // A successful copy alone is not proof that the recoverable backup is sound.
                for (int i = 0; i < record.Entries.Count; i++) { Payload(record, i, true); Payload(record, i, false); }
                record.Status = "prepared"; Save(record);
                try
                {
                    for (int i = 0; i < record.Entries.Count; i++)
                    {
                        policy.Stopped(); var entry = record.Entries[i]; var path = Resolve(game, profile, entry.Area, entry.Name);
                        if (Files.Hash(Files.Read(path)) != entry.BeforeHash) throw new HelperFailure("errorConflict", "Datei wurde während der Vorbereitung verändert: " + path);
                        if (entry.BeforeHash != entry.AfterHash) Files.Atomic(path, Payload(record, i, false));
                        if (Files.Hash(Files.Read(path)) != entry.AfterHash) throw new HelperFailure("errorConflict", "Dateiprüfung fehlgeschlagen: " + path);
                        if (AfterWrite != null) AfterWrite(i);
                    }
                    record.Status = "complete"; Save(record); return folder;
                }
                catch (Exception error)
                {
                    try { Recover(record); }
                    catch (Exception recovery) { record.Status = "recovery-needed"; Save(record); throw new HelperFailure("errorInterrupted", "Änderung unterbrochen. Sicherung: " + folder + "\n" + recovery.Message, error); }
                    throw new HelperFailure("errorRecovered", "Änderung abgebrochen und zurückgenommen. Sicherung: " + folder + "\n" + error.Message, error);
                }
            }
        }
        internal List<TransactionRecord> History(string game, string profile)
        {
            var result = new List<TransactionRecord>();
            if (!Directory.Exists(Store)) return result;
            foreach (var folder in Directory.GetDirectories(Store))
            {
                Guid id;
                if (!Guid.TryParseExact(Path.GetFileName(folder), "N", out id)) continue;
                var path = Files.Under(folder, "transaction.json");
                if (!File.Exists(path)) continue; // preparation failed before any mutation
                var record = Load(path);
                if (string.Equals(record.Game, Files.Root(game), StringComparison.OrdinalIgnoreCase) && string.Equals(record.Profile, Files.Root(profile), StringComparison.OrdinalIgnoreCase)) result.Add(record);
            }
            return result.OrderByDescending(x => x.Time, StringComparer.Ordinal).ToList();
        }
        private TransactionRecord Load(string path)
        {
            var bytes = Files.Read(path);
            if (bytes == null || bytes.Length > 1024 * 1024) throw new HelperFailure("errorBackup", "Ungültiges Sicherungsprotokoll.");
            new LosslessJson(Files.Text(bytes));
            var record = new JavaScriptSerializer().Deserialize<TransactionRecord>(Files.Text(bytes));
            Guid id;
            if (record == null || record.Schema != 1 || !Guid.TryParseExact(record.Id, "N", out id) || Path.GetFileName(Path.GetDirectoryName(path)) != record.Id || record.Entries == null || record.Entries.Count > 10) throw new HelperFailure("errorBackup", "Unbekanntes Sicherungsformat.");
            if (record.Entries.Select(x => x.Area + ":" + x.Name).Distinct().Count() != record.Entries.Count) throw new HelperFailure("errorBackup", "Doppelte Sicherungsziele.");
            foreach (var entry in record.Entries) Resolve(record.Game, record.Profile, entry.Area, entry.Name);
            return record;
        }
        private void Save(TransactionRecord record)
        {
            Files.Atomic(Files.Under(Files.Under(Store, record.Id), "transaction.json"), Files.Utf8.GetBytes(new JavaScriptSerializer().Serialize(record)));
        }
        private byte[] Payload(TransactionRecord record, int index, bool before)
        {
            var entry = record.Entries[index]; var expected = before ? entry.BeforeHash : entry.AfterHash;
            var bytes = Files.Read(Files.Under(Files.Under(Store, record.Id), index + (before ? ".before" : ".after")));
            if (Files.Hash(bytes) != expected) throw new HelperFailure("errorBackup", "Beschädigte Sicherungsdatei; keine Wiederherstellung.");
            return bytes;
        }
        private void Recover(TransactionRecord record)
        {
            policy.Stopped();
            // Check every target and backup before restoring any file.
            for (int i = 0; i < record.Entries.Count; i++)
            {
                var entry = record.Entries[i]; Payload(record, i, true);
                var hash = Files.Hash(Files.Read(Resolve(record.Game, record.Profile, entry.Area, entry.Name)));
                if (hash != entry.BeforeHash && hash != entry.AfterHash) throw new HelperFailure("errorConflict", "Datei inzwischen anderweitig geändert; automatische Wiederherstellung abgebrochen: " + entry.Name);
            }
            for (int i = record.Entries.Count - 1; i >= 0; i--)
            {
                policy.Stopped(); var entry = record.Entries[i];
                var path = Resolve(record.Game, record.Profile, entry.Area, entry.Name);
                var currentHash = Files.Hash(Files.Read(path));
                if (currentHash != entry.BeforeHash && currentHash != entry.AfterHash) throw new HelperFailure("errorConflict", "Datei während der Wiederherstellung geändert: " + path);
                if (currentHash != entry.BeforeHash) Files.Atomic(path, Payload(record, i, true));
            }
            record.Status = "recovered"; Save(record);
        }
        internal string Restore(string game, string profile, string id)
        {
            Guid parsed;
            if (!Guid.TryParseExact(id, "N", out parsed)) throw new HelperFailure("errorBackup", "Ungültige Sicherungsauswahl.");
            var record = Load(Files.Under(Files.Under(Store, id), "transaction.json"));
            if (!string.Equals(Files.Root(game), record.Game, StringComparison.OrdinalIgnoreCase) || !string.Equals(Files.Root(profile), record.Profile, StringComparison.OrdinalIgnoreCase)) throw new HelperFailure("errorBackup", "Sicherung gehört zu einem anderen Spiel-/Profilordner.");
            policy.Stopped(); policy.Validate(game, false);
            if (record.Status == "prepared" || record.Status == "recovery-needed") { using (Lock()) { Recover(record); } return Files.Under(Store, id); }
            if (record.Status != "complete") throw new HelperFailure("errorBackup", "Diese Sicherung ist bereits zurückgenommen.");
            var plan = new List<PlannedFile>();
            for (int i = 0; i < record.Entries.Count; i++)
            {
                var entry = record.Entries[i];
                if (entry.Area == "save" || entry.BeforeHash == entry.AfterHash) continue; // never roll campaign progress back
                var current = Files.Read(Resolve(game, profile, entry.Area, entry.Name));
                var before = Payload(record, i, true); var after = Payload(record, i, false);
                if (entry.Area == "settings")
                {
                    if (current == null || before == null || after == null) throw new HelperFailure("errorConflict", "Tastensicherung fehlt.");
                    if (CharacterKeysOnly(record.Action))
                        before = Files.Utf8.GetBytes(Profiles.RestoreCharacterBindings(Files.Text(current), Files.Text(before), Files.Text(after)));
                    else
                    {
                        if (!Profiles.Same(Profiles.Selected(Files.Text(current)), Profiles.Selected(Files.Text(after)))) throw new HelperFailure("errorConflict", "Die betroffenen Tasten wurden inzwischen geändert. Sie werden nicht ungefragt überschrieben.");
                        before = Files.Utf8.GetBytes(Profiles.Merge(Files.Text(current), Profiles.Selected(Files.Text(before))));
                    }
                }
                else if (Files.Hash(current) != entry.AfterHash) throw new HelperFailure("errorConflict", "Moddatei wurde seit dieser Sicherung geändert: " + entry.Name);
                plan.Add(new PlannedFile(entry.Area, entry.Name, before).Expect(current));
            }
            // Old no-change transactions are valid; do not mislabel them as corrupt.
            if (plan.Count == 0) return null;
            return Execute(game, profile, "Wiederherstellen: " + record.Action, plan);
        }
        internal static bool CanUndo(TransactionRecord record)
        {
            return record.Status == "prepared" || record.Status == "recovery-needed"
                || record.Status == "complete" && record.Entries.Any(e => e.Area != "save" && e.BeforeHash != e.AfterHash);
        }
    }
}
