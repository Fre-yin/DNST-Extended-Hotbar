using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace ExtendedHotbar.Helper
{
    internal static class GameDiscoveryTests
    {
        internal static void Run(Action<string, Action> test, string root)
        {
            Func<string> fresh = () => { var path = Files.Under(root, "discovery-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(path); return path; };
            test("game discovery reads nonstandard Steam libraries and manifest install directory", () =>
            {
                var folder = fresh(); var steam = Sub(folder, "Steam"); var library = Sub(folder, "Custom library ä");
                var game = Game(Sub(library, "steamapps/common/Custom DNST install"));
                Write(steam, "steamapps/libraryfolders.vdf", "// comment with a fake \"path\" \"Z:\\\\ignored\"\n\"libraryfolders\" { \"7\" { \"path\" " + Q(library) + " \"apps\" { \"2798330\" \"1\" } } }");
                Write(library, "steamapps/appmanifest_2798330.acf", Manifest("Custom DNST install"));
                Check(Find(new[] { steam }).SequenceEqual(new[] { game }));
            });
            test("legacy Steam libraries and case duplicate roots are supported", () =>
            {
                var folder = fresh(); var steam = Sub(folder, "Steam"); var library = Sub(folder, "Legacy");
                var game = Game(Sub(library, "steamapps/common/Dungeon Settlers"));
                Write(steam, "config/libraryfolders.vdf", "\"LibraryFolders\" { \"1\" " + Q(library) + " }");
                Check(Find(new[] { steam, library, library.ToUpperInvariant() }).SequenceEqual(new[] { game }));
            });
            test("main game and test copies are returned as distinct sorted choices", () =>
            {
                var folder = fresh(); var steam = Sub(folder, "Steam"); var sandbox = Sub(folder, "DNST-Test");
                var a = Game(Sub(steam, "steamapps/common/Dungeon Settlers"));
                var b = Game(Sub(steam, "steamapps/common/Dungeon Settlers Test"));
                var c = Game(Sub(sandbox, "Game"));
                Check(GameDiscovery.Find(new[] { steam }, new[] { a }, new[] { sandbox }, CancellationToken.None)
                    .SequenceEqual(new[] { a, b, c }.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)));
            });
            test("only a sole discovery may prefill an empty unchanged game field", () =>
            {
                Check(GameDiscovery.InitialChoice(new[] { "one" }, "", true) == "one");
                Check(GameDiscovery.InitialChoice(new[] { "one", "two" }, "", true) == "");
                Check(GameDiscovery.InitialChoice(new string[0], "", true) == "");
                Check(GameDiscovery.InitialChoice(new[] { "one" }, "my test folder", true) == "my test folder");
                Check(GameDiscovery.InitialChoice(new[] { "one" }, "", false) == "");
                Check(GameDiscovery.InitialChoice(new[] { "one", "two" }, "manual", false) == "manual");
            });
            test("automatic copy discovery excludes backups but accepts a directly chosen copy", () =>
            {
                var folder = fresh(); var game = Game(Sub(folder, "Game")); var backup = Game(Sub(folder, "Backups/old/Game"));
                var archive = Game(Sub(folder, "Archive/Game"));
                Check(GameDiscovery.Find(new string[0], new string[0], new[] { folder }, CancellationToken.None).SequenceEqual(new[] { game }));
                Check(GameDiscovery.Find(new string[0], new[] { backup, archive }, new string[0], CancellationToken.None).Length == 2);
            });
            test("incomplete and uninstalled leftovers are not offered as games", () =>
            {
                var folder = fresh(); var game = Sub(folder, "steamapps/common/Dungeon Settlers");
                Write(game, "DungeonSettlers.exe", "not a game by itself");
                Check(Find(new[] { folder }).Length == 0);
                Write(game, "GameAssembly.dll", "still incomplete");
                Check(Find(new[] { folder }).Length == 0);
            });
            test("broken Steam metadata does not hide other valid installations", () =>
            {
                var folder = fresh(); var broken = Sub(folder, "broken"); var valid = Sub(folder, "valid");
                var game = Game(Sub(valid, "steamapps/common/Dungeon Settlers"));
                Write(broken, "steamapps/libraryfolders.vdf", "\"libraryfolders\" { \"0\" {");
                Check(Find(new[] { broken, valid }).SequenceEqual(new[] { game }));
            });
            test("foreign app ids and traversal install folders are ignored", () =>
            {
                var folder = fresh(); var steam = Sub(folder, "Steam"); Game(Sub(steam, "steamapps/common/Other"));
                Write(steam, "steamapps/appmanifest_2798330.acf", Manifest("Other").Replace("2798330", "999"));
                Check(Find(new[] { steam }).Length == 0);
                Game(Sub(steam, "steamapps/Escape"));
                foreach (var name in new[] { "../Escape", "..\\Escape", "C:\\Escape", "//host/share", "Other.", "Other " })
                { Write(steam, "steamapps/appmanifest_2798330.acf", Manifest(name)); Check(Find(new[] { steam }).Length == 0); }
            });
            test("network device relative and missing discovery roots are rejected", () =>
            {
                Check(Find(new[] { @"\\invalid-host\share", @"\\?\C:\test", "relative", "C:", "", null }).Length == 0);
            });
            test("metadata size duplicate key and nesting limits fail closed", () =>
            {
                var folder = fresh(); var other = Sub(folder, "unlisted"); Game(Sub(other, "steamapps/common/Dungeon Settlers"));
                var path = "\"path\" " + Q(other);
                foreach (var text in new[] { new string(' ', 1024 * 1024 + 1), "\"libraryfolders\" { \"0\" { " + path + " " + path + " } }",
                    string.Concat(Enumerable.Repeat("\"a\" {", 18)) + new string('}', 18) })
                { Write(folder, "steamapps/libraryfolders.vdf", text); Check(Find(new[] { folder }).Length == 0); }
            });
            test("copy search is depth bounded and direct manual paths remain usable", () =>
            {
                var folder = fresh(); var game = Game(Sub(folder, "a/b/c/d/Game"));
                Check(GameDiscovery.Find(new string[0], new string[0], new[] { folder }, CancellationToken.None).Length == 0);
                Check(GameDiscovery.Find(new string[0], new[] { game }, new string[0], CancellationToken.None).Single() == game);
            });
            test("cancelled game discovery returns no selection and performs no writes", () =>
            {
                var folder = fresh(); var game = Game(Sub(folder, "steamapps/common/Dungeon Settlers"));
                var before = Directory.GetFiles(folder, "*", SearchOption.AllDirectories).ToDictionary(x => x, Files.FileHash);
                using (var cancellation = new CancellationTokenSource())
                {
                    cancellation.Cancel(); bool cancelled = false;
                    try { GameDiscovery.Find(new[] { folder }, new string[0], new string[0], cancellation.Token); }
                    catch (OperationCanceledException) { cancelled = true; }
                    Check(cancelled);
                }
                Check(Find(new[] { folder }).Single() == game);
                Check(Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Length == before.Count && before.All(x => Files.FileHash(x.Key) == x.Value));
            });
        }
        private static string[] Find(string[] roots) { return GameDiscovery.Find(roots, new string[0], new string[0], CancellationToken.None); }
        private static void Check(bool result) { if (!result) throw new Exception("Game discovery assertion failed."); }
        private static string Q(string text) { return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""; }
        private static string Manifest(string folder) { return "\"AppState\" { \"appid\" \"2798330\" \"installdir\" " + Q(folder) + " }"; }
        private static string Sub(string root, string name) { var folder = Files.Under(root, name); Directory.CreateDirectory(folder); return folder; }
        private static void Write(string root, string name, string text) { var path = Files.Under(root, name); Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, text); }
        private static string Game(string path)
        {
            foreach (var name in new[] { "DungeonSettlers.exe", "GameAssembly.dll", "DungeonSettlers_Data/il2cpp_data/Metadata/global-metadata.dat" }) Write(path, name, "inert discovery fixture");
            return path;
        }
    }
}
