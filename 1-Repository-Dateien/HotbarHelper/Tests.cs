using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExtendedHotbar.Helper
{
    internal static class Tests
    {
        private static int passed, failed;
        private static string root, fixture;
        private static Dictionary<string, byte[]> package;
        private static void Main(string[] args)
        {
            fixture = File.ReadAllText(args[0]); root = Files.Root(args[1]); Directory.CreateDirectory(root);
            package = ReleaseInfo.Package();
            var english = new UiText("en");
            foreach (var code in UiText.Codes)
            {
                var language = code;
                Test("complete language and format placeholders: " + language, () =>
                {
                    var text = new UiText(language); Check(text.Keys.OrderBy(x => x).SequenceEqual(english.Keys.OrderBy(x => x)));
                    foreach (var key in english.Keys)
                    {
                        Check(!string.IsNullOrWhiteSpace(text[key]));
                        var pattern = @"\{[0-9]+\}";
                        var wanted = System.Text.RegularExpressions.Regex.Matches(english[key], pattern).Cast<System.Text.RegularExpressions.Match>().Select(x => x.Value).OrderBy(x => x);
                        var found = System.Text.RegularExpressions.Regex.Matches(text[key], pattern).Cast<System.Text.RegularExpressions.Match>().Select(x => x.Value).OrderBy(x => x);
                        Check(wanted.SequenceEqual(found));
                        Check(text.Format(key, "PATH", "SUFFIX").Length > 0);
                        if (key.StartsWith("error")) Check(text.Error(new HelperFailure(key, "diagnostic")) == text[key]);
                    }
                    Check(text["saveFilter"].Split('|').Length == 2 && text["backupFilter"].Split('|').Length == 4);
                    Check(text["helpText"].Contains("Ironmode") && text["helpText"].Contains("MIT") && text["nativeAsk"].Contains("Q/E/R/T"));
                    Check(text.HistoryAction("Wiederherstellen: Installieren / Aktualisieren").Contains(text["install"]));
                    Check(!text.HistoryAction("Ohne Hotbar vorbereiten").Contains("\n"));
                    Check(text.Format("removalSummary", text["saveSuffix"]).Contains(text["saveSuffix"]));
                });
                Test("localized export only changes intended fields: " + language, () =>
                {
                    var id = Guid.NewGuid(); var text = new UiText(language);
                    var output = new LosslessJson(VanillaSave.Export(fixture, "TestCopy", id, text["saveSuffix"]));
                    var original = new LosslessJson(fixture);
                    Check(output.Root.Get("CampaignSaveHeader").Get("DisplayName").String == original.Root.Get("CampaignSaveHeader").Get("DisplayName").String + " " + text["saveSuffix"]);
                    foreach (var member in original.Root.Members.Where(x => !new[] { "CampaignSaveHeader", "ClanSaveData", "PlayerUnitsSaveData", "DungeonSettlers10Slots_Items" }.Contains(x.Name))) Check(original.Raw(member.Value) == output.Raw(output.Root.Get(member.Name)));
                    Check(UiText.FromSettings("{\"GeneralSettingData\":{\"SaveCurrentLanguageType\":" + Array.IndexOf(UiText.Codes, language) + "}}", "en") == language);
                });
            }
            Test("culture detection and safe language fallback", () =>
            {
                foreach (var pair in new[] { "de-DE=de", "ko-KR=ko", "ja-JP=ja", "fr-CA=fr", "ru-RU=ru", "es-MX=es", "pt-PT=pt-BR", "pt-BR=pt-BR", "zh-TW=zh-Hant", "zh-HK=zh-Hant", "zh-Hant=zh-Hant", "zh-CN=zh-Hans", "zh-SG=zh-Hans", "en-US=en", "it-IT=en" })
                { var parts = pair.Split('='); Check(UiText.FromCulture(parts[0]) == parts[1]); }
                Check(UiText.FromSettings("{}", "fr") == "fr"); Check(UiText.FromSettings("invalid", "de") == "de");
                Check(UiText.FromSettings("{\"GeneralSettingData\":{\"SaveCurrentLanguageType\":99}}", "ko") == "ko");
                Check(new UiText("unknown").Code == "en"); Throws(() => UiText.Parse("{\"a\":\"1\",\"a\":\"2\"}"));
                Throws(() => VanillaSave.Export(fixture, "Test", Guid.NewGuid(), "bad\nsuffix"));
            });
            Test("embedded release contains only the three install files", () => Check(package.Count == 3 && package.ContainsKey(ReleaseInfo.Dll)));
            Test("modified release is refused", () => Throws(() => ReleaseInfo.ReadPackage(new byte[] { 1, 2, 3 })));
            Test("large numeric spelling retained", () => { var d = new LosslessJson("{\"n\":9007199254740993,\"f\":1.234567890123456789e+30,\"x\":1}"); Check(d.Apply(new[] { d.Replace(d.Root.Get("x"), "2") }) == "{\"n\":9007199254740993,\"f\":1.234567890123456789e+30,\"x\":2}"); });
            foreach (var bad in new[] { "", "{\"a\":1,\"a\":2}", "{\"a\":1,\"\\u0061\":2}", "[1,]", "{\"a\":1,}", "01", "1e", "true false", "\"a\n\"", "\"\\q\"", "{", "/*x*/{}", new string('[', 140) + new string(']', 140) })
            { var input = bad; Test("invalid JSON blocked " + (passed + failed), () => Throws(() => new LosslessJson(input))); }
            Test("escaped property names resolve correctly", () => Check(new LosslessJson("{\"\\u0061\":\"x\\\"y\"}").Root.Get("a").String == "x\"y"));
            foreach (var json in new[] { "{\"a\":1,\"b\":2,\"c\":3}", "{\"b\":2,\"a\":1,\"c\":3}", "{\"b\":2,\"c\":3,\"a\":1}", "{\"a\":1}" })
            { var input = json; Test("property removal keeps valid JSON", () => { var d = new LosslessJson(input); Check(new LosslessJson(d.Apply(new[] { d.Remove(d.Root, "a") })).Root.Optional("a") == null); }); }
            Test("overlapping edits refused", () => { var d = new LosslessJson("[1,2]"); Throws(() => d.Apply(new[] { new JsonEdit(0, 4, "[]"), new JsonEdit(1, 2, "3") })); });
            Test("settings preserve language, other controls and numeric bytes", () =>
            {
                var settings = Settings(); var d = new LosslessJson(settings); var converted = Profiles.Merge(settings, Profiles.VanillaDefaults()); var result = new LosslessJson(converted);
                Check(d.Raw(d.Root.Get("GeneralSettingData")) == result.Raw(result.Root.Get("GeneralSettingData")));
                Check(converted.Contains("9007199254740993") && converted.Contains("1.234567890123456789e+30"));
                Check(Profiles.Bindings(result).Items.Any(x => x.Get("InputType").Integer == 999 && x.Get("KeyCode").Integer == 304));
                Check(!Profiles.Bindings(result).Items.Any(x => Profiles.Extra(x.Get("InputType").Integer)));
                Check(Profiles.Same(Profiles.Selected(Profiles.Merge(converted, Profiles.Selected(settings))), Profiles.Selected(settings)));
            });
            Test("foreign bindings cannot be restored through hotbar profile", () => Throws(() => Profiles.Merge(Settings(), "[{\"InputType\":999,\"SlotIndex\":0}]")));
            Test("binding comparison ignores harmless list reordering", () =>
            {
                var selected = Profiles.Selected(Settings()); var doc = new LosslessJson(selected);
                var reordered = "[" + string.Join(",", doc.Root.Items.AsEnumerable().Reverse().Select(doc.Raw)) + "]";
                Check(Profiles.Same(selected, reordered));
            });
            Test("merging preserves relative order of existing native rows", () =>
            {
                var current = Settings(); var before = Profiles.Bindings(new LosslessJson(current)).Items.Where(x => !Profiles.Extra(x.Get("InputType").Integer)).Select(x => x.Get("InputType").Integer).ToArray();
                var after = Profiles.Bindings(new LosslessJson(Profiles.Merge(current, Profiles.VanillaDefaults()))).Items.Where(x => x.Get("SlotIndex").Integer == 0).Take(before.Length).Select(x => x.Get("InputType").Integer).ToArray();
                Check(before.SequenceEqual(after));
            });
            Test("duplicate bindings refused", () => Throws(() => Profiles.Bindings(new LosslessJson("{\"KeySettingData\":{\"Bindings\":[" + Row(34, 0, 1, 0) + "," + Row(34, 0, 2, 0) + "]}}"))));
            Test("provided save fixture exports with independent campaign identity", () =>
            {
                var id = Guid.NewGuid(); var result = new LosslessJson(VanillaSave.Export(fixture, "Test_ohneHotbar", id));
                Check(result.Root.Get("CampaignSaveHeader").Get("CampaignGuid").String == id.ToString());
                Check(result.Root.Get("ClanSaveData").Get("CampaignGuid").String == id.ToString());
                Check(result.Root.Get("CampaignSaveHeader").Get("SaveName").String == "Test_ohneHotbar");
                Check(result.Root.Optional("DungeonSettlers10Slots_Items") == null);
                Check(result.Root.Get("PlayerUnitsSaveData").Get("QuickSlots").Members.All(x => x.Value.Get("QuickSlots").Items.Count == 4));
            });
            Test("provided save fixture inventory, units, map and progress stay verbatim", () =>
            {
                var d = new LosslessJson(fixture); var e = new LosslessJson(VanillaSave.Export(fixture, "Vanilla", Guid.NewGuid()));
                foreach (var member in d.Root.Members.Where(x => !new[] { "CampaignSaveHeader", "ClanSaveData", "PlayerUnitsSaveData", "DungeonSettlers10Slots_Items" }.Contains(x.Name))) Check(d.Raw(member.Value) == e.Raw(e.Root.Get(member.Name)));
                foreach (var key in new[] { "ClanSaveData", "PlayerUnitsSaveData", "CampaignSaveHeader" })
                    foreach (var member in d.Root.Get(key).Members.Where(x => !new[] { "CampaignGuid", "QuickSlots", "SaveName", "DisplayName", "IsAutoSave" }.Contains(x.Name))) Check(d.Raw(member.Value) == e.Raw(e.Root.Get(key).Get(member.Name)));
                foreach (var member in d.Root.Get("PlayerUnitsSaveData").Get("QuickSlots").Members)
                {
                    var native = e.Root.Get("PlayerUnitsSaveData").Get("QuickSlots").Get(member.Name);
                    Check(d.Raw(member.Value.Get("ItemQuickSlotKey")) == e.Raw(native.Get("ItemQuickSlotKey")));
                    for (int i = 0; i < 4; i++) Check(d.Raw(member.Value.Get("QuickSlots").Items[i]) == e.Raw(native.Get("QuickSlots").Items[i]));
                }
            });
            Test("unknown game save version refused", () => Throws(() => VanillaSave.Export(fixture.Replace("DS_B.0.4.17", "DS_B.0.4.18"), "Test", Guid.NewGuid())));
            Test("new 0.4.19 save header preserves version and unrelated bytes", () =>
            {
                var source = fixture.Replace("DS_B.0.4.17", "DS_B.0.4.19");
                var original = new LosslessJson(source);
                var output = new LosslessJson(VanillaSave.Export(source, "NewBuildCopy", Guid.NewGuid()));
                Check(output.Root.Get("CampaignSaveHeader").Get("Version").String == "DS_B.0.4.19");
                foreach (var member in original.Root.Members.Where(x => !new[] { "CampaignSaveHeader", "ClanSaveData", "PlayerUnitsSaveData", "DungeonSettlers10Slots_Items" }.Contains(x.Name)))
                    Check(original.Raw(member.Value) == output.Raw(output.Root.Get(member.Name)));
                Throws(() => VanillaSave.Export(source.Replace("DS_B.0.4.19", "DS_B.0.4.20"), "Future", Guid.NewGuid()));
            });
            Test("unknown item extension refused", () => { var d = new LosslessJson(fixture); var v = d.Root.Get("DungeonSettlers10Slots_Items").Get("Version"); Throws(() => VanillaSave.Export(d.Apply(new[] { d.Replace(v, "2") }), "Test", Guid.NewGuid())); });
            Test("duplicate campaign reference refused", () => { var d = new LosslessJson(fixture); var id = d.Root.Get("CampaignSaveHeader").Get("CampaignGuid").String; Throws(() => VanillaSave.Export(fixture.Insert(1, "\"FutureReference\":" + LosslessJson.Quote(id) + ","), "Test", Guid.NewGuid())); });
            Test("iron mode refused", () => { var d = new LosslessJson(fixture); Throws(() => VanillaSave.Export(d.Apply(new[] { d.Replace(d.Root.Get("CampaignSaveHeader").Get("IronModeEnabled"), "true") }), "Test", Guid.NewGuid())); });
            foreach (var unsafeName in new[] { "../x", "C:/x", "x:y", "", "x.json" }) { var name = unsafeName; Test("unsafe save name refused", () => Throws(() => VanillaSave.Export(fixture, name, Guid.NewGuid()))); }
            foreach (var unsafePath in new[] { "../x", "a/../b", "C:/x", "a:b", "a//b", "a./b", "a /b" }) { var name = unsafePath; Test("unsafe relative target refused", () => Throws(() => Files.Under(root, name))); }
            foreach (var badRoot in new[] { "relative", "C:relative", "C:\\", @"\\server\share", "" }) { var path = badRoot; Test("ambiguous or broad root refused", () => Throws(() => Files.Root(path))); }
            Test("real directory junction is rejected", () =>
            {
                var place = Files.Under(root, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(place);
                var target = Files.Under(place, "target"); Directory.CreateDirectory(target); var link = Files.Under(place, "link");
                var command = "New-Item -ItemType Junction -Path '" + link.Replace("'", "''") + "' -Target '" + target.Replace("'", "''") + "' -ErrorAction Stop | Out-Null";
                var start = new System.Diagnostics.ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -Command \"" + command + "\"") { UseShellExecute = false, CreateNoWindow = true, WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden };
                using (var process = System.Diagnostics.Process.Start(start)) { if (!process.WaitForExit(15000)) throw new Exception("Junction fixture creation timed out"); Check(process.ExitCode == 0); }
                Check((File.GetAttributes(link) & FileAttributes.ReparsePoint) != 0);
                Throws(() => Files.Under(place, "link/file.txt")); Check(!File.Exists(System.IO.Path.Combine(target, "file.txt")));
            });
            Test("install and rollback preserve foreign mods and game", () =>
            {
                var env = new Env(); var old = Bytes("legacy"); env.PutGame(ReleaseInfo.OldDll, old); env.PutGame("Mods/Other.dll", Bytes("foreign"));
                var settings = File.ReadAllBytes(env.Setting); var backup = env.Op.Install(env.Game, env.Profile, package);
                Check(!File.Exists(env.Path(ReleaseInfo.OldDll))); Check(Files.FileHash(env.Path(ReleaseInfo.Dll)) == Files.Hash(package[ReleaseInfo.Dll]));
                Check(Files.Hash(File.ReadAllBytes(env.Setting)) == Files.Hash(settings));
                env.Op.Restore(env.Game, env.Profile, Path.GetFileName(backup));
                Check(!File.Exists(env.Path(ReleaseInfo.Dll)) && Files.FileHash(env.Path(ReleaseInfo.OldDll)) == Files.Hash(old));
                Check(File.ReadAllText(env.Path("Mods/Other.dll")) == "foreign" && File.ReadAllText(env.Path("DungeonSettlers.exe")) == "game");
            });
            Test("reinstalling identical files is a no-op without extra backup", () =>
            {
                var env = new Env(); env.Op.Install(env.Game, env.Profile, package);
                var count = Directory.GetDirectories(env.Store).Length; var settings = Files.FileHash(env.Setting);
                Check(env.Op.Install(env.Game, env.Profile, package) == null);
                Check(Directory.GetDirectories(env.Store).Length == count && Files.FileHash(env.Setting) == settings);
                Check(env.Op.History(env.Game, env.Profile).Count == 1);
            });
            Test("unchanged native keys do not create redundant backups", () =>
            {
                var env = new Env(); env.Op.PrepareNativeKeys(env.Game, env.Profile);
                var settings = Files.FileHash(env.Setting);
                Check(env.Op.PrepareNativeKeys(env.Game, env.Profile) == null);
                Check(env.Op.History(env.Game, env.Profile).Count == 1 && Files.FileHash(env.Setting) == settings);
            });
            Test("legacy no-change backup restores as harmless no-op", () =>
            {
                var env = new Env(); env.Op.Install(env.Game, env.Profile, package); var id = LegacyNoOp(env);
                var count = Directory.GetDirectories(env.Store).Length; var settings = Files.FileHash(env.Setting);
                Check(env.Op.Restore(env.Game, env.Profile, id) == null);
                Check(Directory.GetDirectories(env.Store).Length == count && Files.FileHash(env.Setting) == settings);
                foreach (var entry in package) Check(Files.FileHash(env.Path(entry.Key)) == Files.Hash(entry.Value));
            });
            Test("legacy no-change record does not hide the prior native key backup", () =>
            {
                var env = new Env(); env.Op.Install(env.Game, env.Profile, package); var settings = Profiles.Selected(File.ReadAllText(env.Setting));
                env.Op.PrepareNativeKeys(env.Game, env.Profile); LegacyNoOp(env);
                env.Op.Install(env.Game, env.Profile, package);
                Check(Profiles.Same(Profiles.Selected(File.ReadAllText(env.Setting)), settings));
            });
            Test("undo menu omits unchanged entries but keeps interrupted recovery", () =>
            {
                var env = new Env(); env.Op.Install(env.Game, env.Profile, package); var id = LegacyNoOp(env);
                var records = env.Op.History(env.Game, env.Profile);
                Check(records.Count == 2 && records.Where(Operations.CanUndo).Count() == 1);
                var unchanged = records.Single(x => x.Id == id); Check(!Operations.CanUndo(unchanged));
                unchanged.Status = "prepared"; Check(Operations.CanUndo(unchanged));
                unchanged.Status = "recovery-needed"; Check(Operations.CanUndo(unchanged));
                unchanged.Status = "recovered"; Check(!Operations.CanUndo(unchanged));
            });
            Test("no-op install still enforces running-game and build guards", () =>
            {
                var env = new Env(); env.Op.Install(env.Game, env.Profile, package);
                env.Policy.Running = true; Throws(() => env.Op.Install(env.Game, env.Profile, package));
                env.Policy.Running = false; env.Policy.Compatible = false; Throws(() => env.Op.Install(env.Game, env.Profile, package));
                Check(env.Op.History(env.Game, env.Profile).Count == 1);
            });
            Test("failed install automatically restores old files", () =>
            {
                var env = new Env(); env.PutGame(ReleaseInfo.OldDll, Bytes("legacy")); env.Op.AfterWrite = i => { if (i == 2) throw new IOException("injected failure"); };
                Throws(() => env.Op.Install(env.Game, env.Profile, package));
                Check(!File.Exists(env.Path(ReleaseInfo.Dll)) && File.ReadAllText(env.Path(ReleaseInfo.OldDll)) == "legacy"); Check(env.Op.History(env.Game, env.Profile).Single().Status == "recovered");
            });
            Test("corrupt pre-commit backup prevents any file mutation", () =>
            {
                var env = new Env(); env.PutGame(ReleaseInfo.Dll, Bytes("old"));
                env.Op.BeforeCommit = () => { var folder = Directory.GetDirectories(env.Store).Single(); File.WriteAllText(System.IO.Path.Combine(folder, "0.before"), "corrupt"); };
                Throws(() => env.Op.Install(env.Game, env.Profile, package)); Check(File.ReadAllText(env.Path(ReleaseInfo.Dll)) == "old");
            });
            Test("concurrent settings edit is not overwritten", () =>
            {
                var env = new Env(); env.Op.BeforeCommit = () => File.WriteAllText(env.Setting, Settings().Replace("Korean", "English"));
                Throws(() => env.Op.PrepareNativeKeys(env.Game, env.Profile)); Check(File.ReadAllText(env.Setting).Contains("English"));
            });
            Test("running game refuses mutations", () => { var env = new Env(); env.Policy.Running = true; Throws(() => env.Op.Install(env.Game, env.Profile, package)); Check(!Directory.Exists(env.Store)); });
            Test("wrong game build refused", () => { var env = new Env(); env.Policy.Compatible = false; Throws(() => env.Op.Install(env.Game, env.Profile, package)); Check(!File.Exists(env.Path(ReleaseInfo.Dll))); });
            Test("missing loader refused", () => { var env = new Env(); File.Delete(env.Path("MelonLoader/net6/MelonLoader.dll")); Throws(() => env.Op.Install(env.Game, env.Profile, package)); });
            Test("modified mod blocks historical rollback", () => { var env = new Env(); var backup = env.Op.Install(env.Game, env.Profile, package); env.PutGame(ReleaseInfo.Dll, Bytes("changed")); Throws(() => env.Op.Restore(env.Game, env.Profile, Path.GetFileName(backup))); Check(File.ReadAllText(env.Path(ReleaseInfo.Dll)) == "changed"); });
            Test("corrupt backup blocks rollback", () => { var env = new Env(); env.PutGame(ReleaseInfo.Dll, Bytes("old")); var backup = env.Op.Install(env.Game, env.Profile, package); File.WriteAllText(Path.Combine(backup, "0.before"), "corrupt"); Throws(() => env.Op.Restore(env.Game, env.Profile, Path.GetFileName(backup))); Check(Files.FileHash(env.Path(ReleaseInfo.Dll)) == Files.Hash(package[ReleaseInfo.Dll])); });
            Test("backup cannot target another installation", () => { var env = new Env(); var backup = env.Op.Install(env.Game, env.Profile, package); var other = new Env(); Throws(() => env.Op.Restore(other.Game, env.Profile, Path.GetFileName(backup))); });
            Test("process starting mid-install leaves recoverable transaction", () =>
            {
                var env = new Env(); env.Op.AfterWrite = i => env.Policy.Running = true;
                Throws(() => env.Op.Install(env.Game, env.Profile, package)); var tx = env.Op.History(env.Game, env.Profile).Single(); Check(tx.Status == "recovery-needed");
                env.Policy.Running = false; env.Op.AfterWrite = null;
                Throws(() => env.Op.Install(env.Game, env.Profile, package)); env.Op.Restore(env.Game, env.Profile, tx.Id);
                Check(!File.Exists(env.Path(ReleaseInfo.Dll)) && env.Op.History(env.Game, env.Profile).Single().Status == "recovered");
            });
            Test("disable exports copy and restore never rolls back campaign progress", () =>
            {
                var env = new Env(); env.Op.Install(env.Game, env.Profile, package); var oldSettings = File.ReadAllText(env.Setting); var sourceHash = Files.FileHash(env.Save);
                string exported; var backup = env.Op.Disable(env.Game, env.Profile, env.Save, Profiles.VanillaDefaults(), out exported);
                Check(!File.Exists(env.Path(ReleaseInfo.Dll)) && File.Exists(exported) && Files.FileHash(env.Save) == sourceHash);
                File.WriteAllText(exported, File.ReadAllText(exported) + "\n "); var exportHash = Files.FileHash(exported);
                var changedLanguage = File.ReadAllText(env.Setting).Replace("Korean", "English"); File.WriteAllText(env.Setting, changedLanguage);
                env.Op.Restore(env.Game, env.Profile, Path.GetFileName(backup));
                Check(File.Exists(env.Path(ReleaseInfo.Dll)) && File.ReadAllText(env.Setting).Contains("English"));
                Check(Profiles.Same(Profiles.Selected(File.ReadAllText(env.Setting)), Profiles.Selected(oldSettings)));
                Check(Files.FileHash(env.Save) == sourceHash && Files.FileHash(exported) == exportHash);
            });
            for (int failure = 0; failure < 7; failure++)
            {
                int step = failure;
                Test("disable rollback after step " + step, () =>
                {
                    var env = new Env(); env.PutGame("Mods/Other.dll", Bytes("foreign")); env.Op.Install(env.Game, env.Profile, package);
                    var saveHash = Files.FileHash(env.Save); var settingsHash = Files.FileHash(env.Setting);
                    env.Op.AfterWrite = index => { if (index == step) throw new IOException("injected"); };
                    string exported; Throws(() => env.Op.Disable(env.Game, env.Profile, env.Save, Profiles.VanillaDefaults(), out exported));
                    Check(Files.FileHash(env.Save) == saveHash && Files.FileHash(env.Setting) == settingsHash);
                    Check(Directory.GetFiles(System.IO.Path.GetDirectoryName(env.Save)).Length == 1);
                    foreach (var entry in package) Check(Files.FileHash(env.Path(entry.Key)) == Files.Hash(entry.Value));
                    Check(File.ReadAllText(env.Path("Mods/Other.dll")) == "foreign");
                });
            }
            Test("changed relevant keys block automatic rebind rollback", () =>
            {
                var env = new Env(); var backup = env.Op.PrepareNativeKeys(env.Game, env.Profile); File.WriteAllText(env.Setting, File.ReadAllText(env.Setting).Replace("\"KeyCode\":113", "\"KeyCode\":108"));
                Throws(() => env.Op.Restore(env.Game, env.Profile, Path.GetFileName(backup)));
            });
            Test("native key preparation does not touch save or mod", () => { var env = new Env(); env.Op.Install(env.Game, env.Profile, package); var hash = Files.FileHash(env.Save); env.Op.PrepareNativeKeys(env.Game, env.Profile); Check(Files.FileHash(env.Save) == hash && File.Exists(env.Path(ReleaseInfo.Dll))); });
            Test("install reactivates previously saved hotbar keys", () => { var env = new Env(); var original = Profiles.Selected(File.ReadAllText(env.Setting)); env.Op.PrepareNativeKeys(env.Game, env.Profile); File.WriteAllText(env.Setting, File.ReadAllText(env.Setting).Replace("Korean", "English")); env.Op.Install(env.Game, env.Profile, package); Check(Profiles.Same(Profiles.Selected(File.ReadAllText(env.Setting)), original)); Check(File.ReadAllText(env.Setting).Contains("English")); });
            Test("install preserves manually changed vanilla hotkeys", () => { var env = new Env(); env.Op.PrepareNativeKeys(env.Game, env.Profile); File.WriteAllText(env.Setting, File.ReadAllText(env.Setting).Replace("\"KeyCode\":113", "\"KeyCode\":108")); var changed = File.ReadAllText(env.Setting); env.Op.Install(env.Game, env.Profile, package); Check(File.ReadAllText(env.Setting) == changed); });
            Test("invalid save prevents disable before any mutation", () => { var env = new Env(); env.Op.Install(env.Game, env.Profile, package); File.WriteAllText(env.Save, "bad"); string exported; Throws(() => env.Op.Disable(env.Game, env.Profile, env.Save, Profiles.VanillaDefaults(), out exported)); Check(File.Exists(env.Path(ReleaseInfo.Dll))); });
            Test("localized save filenames are supported", () => { var env = new Env(); var name = Files.Under(env.Profile, "Saves/Mein Prüfstand 1.json"); File.Copy(env.Save, name); string exported; env.Op.Disable(env.Game, env.Profile, name, Profiles.VanillaDefaults(), out exported); Check(File.Exists(exported) && Files.FileHash(name) == Files.FileHash(env.Save)); });
            Test("settings with mod extras are not accepted as vanilla profile", () => { var env = new Env(); string exported; Throws(() => env.Op.Disable(env.Game, env.Profile, env.Save, Profiles.Selected(Settings()), out exported)); });
            Test("incomplete vanilla profile is refused", () => { var env = new Env(); string exported; Throws(() => env.Op.Disable(env.Game, env.Profile, env.Save, "[]", out exported)); });
            Test("status ready evidence", () => Check(Status("[Extended_Hotbar] 10-slot release patches loaded;") == HotbarStatus.Active));
            Test("status explicit disable evidence", () => Check(Status("[Extended_Hotbar] 10-Slot-Erweiterung deaktiviert;") == HotbarStatus.Inactive));
            Test("status absence only after loader enumeration", () => Check(Status("[x] 1 Mod loaded.") == HotbarStatus.Inactive));
            Test("status loading is not a failure", () => Check(Status("[x] Loading Mods...") == HotbarStatus.Pending));
            Test("status DLL present is not ready evidence", () => Check(Status("[x] DungeonSettlers10Slots.dll\n[x] 2 Mods loaded.") == HotbarStatus.Pending));
            Test("contradictory ready and disabled evidence is unknown", () => Check(Status("[Extended_Hotbar] 10-slot release patches loaded;\n[Extended_Hotbar] 10-Slot-Erweiterung deaktiviert;") == HotbarStatus.Unknown));
            Test("unrelated log text does not prove hotbar activation", () => Check(Status("[Other_Mod] 10-slot release patches loaded;") == HotbarStatus.Pending));
            Test("old log cannot describe new process", () => { var start = DateTime.Now; Check(LoadStatus.Assess(Log(start.AddMinutes(-1), "1 Mod loaded."), root, start.ToUniversalTime(), start.ToUniversalTime()) == HotbarStatus.Unknown); });
            Test("wrong installation log is ignored", () => { var start = DateTime.Now; Check(LoadStatus.Assess(Log(start, "1 Mod loaded."), root + "-other", start.ToUniversalTime(), start.ToUniversalTime()) == HotbarStatus.Unknown); });
            Test("production policy checks actual bundled DLL without loading it", () => { var env = new Env(); env.PutGame(ReleaseInfo.Dll, package[ReleaseInfo.Dll]); new GamePolicy().OwnedFile(env.Path(ReleaseInfo.Dll)); });
            Test("production policy refuses invalid DLL under owned filename", () => { var env = new Env(); env.PutGame(ReleaseInfo.Dll, Bytes("not a hotbar")); Throws(() => new GamePolicy().OwnedFile(env.Path(ReleaseInfo.Dll))); });
            if (args.Length > 2 && !string.IsNullOrWhiteSpace(args[2]))
            {
                Test("READ ONLY installed game fingerprints", () => new GamePolicy().Validate(args[2], true));
                Console.WriteLine("READ ONLY running-game status: " + LoadStatus.Read(args[2]));
            }
            Console.WriteLine("PASS " + passed + "; FAIL " + failed + ". Isolated files: " + root);
            Environment.ExitCode = failed == 0 ? 0 : 1;
        }
        private static HotbarStatus Status(string text) { var start = DateTime.Now; return LoadStatus.Assess(Log(start, text), root, start.ToUniversalTime(), start.ToUniversalTime()); }
        private static string Log(DateTime start, string text) { return "[" + start.ToString("HH:mm:ss.fff") + "] start\nGame::BasePath = " + root + "\n" + text; }
        private static string Row(int type, int col, int key, int modifier) { return "{\"InputType\":" + type + ",\"SlotIndex\":" + col + ",\"KeyCode\":" + key + ",\"IsKeyDown\":true,\"ModifierKey\":" + modifier + "}"; }
        private static string Settings()
        {
            var rows = new List<string> { Row(999, 0, 304, 0) };
            for (int i = 0; i < 10; i++) { rows.Add(Row(4 + i, 0, i == 9 ? 48 : 49 + i, 304)); rows.Add(Row(14 + i, 0, 0, 0)); }
            for (int i = 0; i < 4; i++) rows.Add(Row(34 + i, 0, 49 + i, 0));
            rows.Add(Row(60, 0, 113, 0));
            for (int i = 0; i < 6; i++) rows.Add(Row(12005 + i, 0, i == 5 ? 48 : 53 + i, 0));
            rows.Add(Row(12101, 0, 101, 0)); rows.Add(Row(12102, 0, 114, 0));
            return "{\"GeneralSettingData\":{\"Language\":\"Korean\",\"Big\":9007199254740993,\"Precise\":1.234567890123456789e+30},\"KeySettingData\":{\"Bindings\":[" + string.Join(",", rows) + "]}}";
        }
        private static string LegacyNoOp(Env env)
        {
            // Matches the valid but unchanged transactions created by helper 0.1.0.
            var record = new TransactionRecord { Id = Guid.NewGuid().ToString("N"), Game = env.Game, Profile = env.Profile, Action = "Installieren / Aktualisieren", Status = "complete", Time = DateTime.UtcNow.AddMinutes(1).ToString("o") };
            var folder = Files.Under(env.Store, record.Id); Directory.CreateDirectory(folder);
            foreach (var name in ReleaseInfo.Owned.Concat(new[] { "UserSetting.json" }))
            {
                var settings = name == "UserSetting.json"; var bytes = Files.Read(settings ? env.Setting : env.Path(name));
                int index = record.Entries.Count;
                if (bytes != null) { File.WriteAllBytes(Files.Under(folder, index + ".before"), bytes); File.WriteAllBytes(Files.Under(folder, index + ".after"), bytes); }
                record.Entries.Add(new ChangeRecord { Area = settings ? "settings" : "game", Name = name, BeforeHash = Files.Hash(bytes), AfterHash = Files.Hash(bytes) });
            }
            File.WriteAllText(Files.Under(folder, "transaction.json"), new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(record));
            return record.Id;
        }
        private static byte[] Bytes(string text) { return Files.Utf8.GetBytes(text); }
        private static void Check(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
        private static void Throws(Action action) { try { action(); } catch { return; } throw new Exception("Expected failure"); }
        private static void Test(string title, Action action) { try { action(); passed++; Console.WriteLine("PASS " + title); } catch (Exception ex) { failed++; Console.WriteLine("FAIL " + title + ": " + ex); } }
        private sealed class FakePolicy : IGamePolicy
        {
            internal bool Running, Compatible = true;
            public void Validate(string game, bool compatible) { if (compatible && !Compatible) throw new IOException("wrong build"); }
            public void Stopped() { if (Running) throw new IOException("game running"); }
            public void OwnedFile(string path) { }
        }
        private sealed class Env
        {
            internal readonly string Game, Profile, Store, Save, Setting;
            internal readonly FakePolicy Policy = new FakePolicy();
            internal readonly Operations Op;
            internal Env()
            {
                var place = Files.Under(root, Guid.NewGuid().ToString("N"));
                Game = Files.Under(place, "Game"); Profile = Files.Under(place, "Profile"); Store = Files.Under(place, "Backups");
                PutGame("DungeonSettlers.exe", Bytes("game")); PutGame("MelonLoader/net6/MelonLoader.dll", Bytes("loader"));
                Directory.CreateDirectory(Files.Under(Profile, "Saves")); Save = Files.Under(Profile, "Saves/Test.json"); File.WriteAllText(Save, fixture);
                Setting = Files.Under(Profile, "UserSetting.json"); File.WriteAllText(Setting, Settings()); Op = new Operations(Store, Policy);
            }
            internal string Path(string relative) { return Files.Under(Game, relative); }
            internal void PutGame(string relative, byte[] bytes) { var path = Path(relative); Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)); File.WriteAllBytes(path, bytes); }
        }
    }
}
