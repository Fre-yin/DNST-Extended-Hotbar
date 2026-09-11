using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace ExtendedHotbar.Helper
{
    // Optional integration probe, compiled only into Helper.Tests.exe. It never
    // launches the game, uses the real Downloads folder, or changes live profiles.
    internal static class LocalUpdateSmokeTests
    {
        private static void Check(bool ok, string message)
        { if (!ok) throw new Exception(message); Console.WriteLine("PASS " + message); }

        internal static void Run(string packageFolder, string sourceGame, string outputRoot, string saveFixture, string settings)
        {
            sourceGame = Files.Root(sourceGame);
            outputRoot = Files.Root(outputRoot);
            if (outputRoot.Equals(sourceGame, StringComparison.OrdinalIgnoreCase)
                || outputRoot.StartsWith(sourceGame + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The output must be outside the source game.");
            new GamePolicy().Validate(sourceGame, true);
            new GamePolicy().Stopped();
            var run = Files.Under(outputRoot, "local-update-smoke-" + Guid.NewGuid().ToString("N"));
            if (Directory.Exists(run)) throw new IOException("Test directory already exists.");
            Directory.CreateDirectory(run);
            Console.WriteLine("Isolated integration files: " + run);

            var downloads = Files.Under(run, "Downloads"); Directory.CreateDirectory(downloads);
            var selected = LocalHelpers.Scan(packageFolder, CancellationToken.None, current: Updates.ParseVersion("0.0.0"));
            if (selected == null) throw new Exception("No publisher-signed helper package found.");
            var helperPath = Files.Under(downloads, selected.Offer.FileName);
            File.Copy(selected.Path, helperPath);
            File.Copy(selected.Path + ".update.json", helperPath + ".update.json");
            var modPath = Files.Under(downloads, "Extended-Hotbar-" + ReleaseInfo.ModVersion + ".zip");
            using (var input = Assembly.GetExecutingAssembly().GetManifestResourceStream("HotbarPackage.zip"))
            using (var output = new FileStream(modPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) input.CopyTo(output);
            var sourcePackageHash = Files.FileHash(selected.Path);
            var signatureHash = Files.FileHash(selected.Path + ".update.json");
            var modHash = Files.FileHash(modPath);

            // Simulate an older controller without relabeling or rebuilding the
            // shipped executable. All package trust checks use the real public key.
            var helper = LocalHelpers.Scan(downloads, CancellationToken.None, current: Updates.ParseVersion("0.0.0"));
            Check(helper != null && helper.Offer.Hash == sourcePackageHash, "signed helper discovered in the isolated download folder");
            Check(LocalHelpers.Scan(downloads, CancellationToken.None, current: helper.Offer.Version) == null,
                "the same helper version is not offered again after handoff");
            var mod = LocalMods.Scan(downloads, Updates.ParseVersion("0.0.0"), CancellationToken.None);
            Check(mod != null && mod.Hash == modHash, "mod package discovered alongside the helper and signature");

            var bytes = LocalHelpers.ReadBytes(helper, CancellationToken.None);
            var stage = Updates.Stage(bytes, helper.Offer, Files.Under(run, "Staged"), CancellationToken.None);
            Console.WriteLine("PASS package staged and launch identity verified: " + stage);
            var previews = Files.Under(run, "Preview"); Directory.CreateDirectory(previews);
            var preview = Files.Under(previews, "de.png");
            Updates.Launch(stage, bytes, helper.Offer, exe =>
            {
                Console.WriteLine("Starting signed helper through Windows in read-only preview mode.");
                // Exercise the actual shipped entry point in its existing read-only
                // preview mode. Shell launch preserves Windows attachment checks.
                using (var child = Process.Start(new ProcessStartInfo {
                    FileName = exe, WorkingDirectory = stage, Arguments = "--preview \"" + preview + "\" de",
                    UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden
                }))
                {
                    if (child == null) throw new Exception("Windows did not return the helper process.");
                    Console.WriteLine("Preview process: " + child.Id);
                    if (!child.WaitForExit(30000))
                        throw new Exception("Helper preview did not finish; inspect Windows prompts. No security prompt was bypassed. Process: " + child.Id);
                    Check(child.ExitCode == 0, "the staged signed helper starts as a real process and exits successfully");
                }
            });
            foreach (var suffix in new[] { "", "-game-multiple", "-game-selected", "-advanced", "-characters", "-overwrite", "-update", "-update-confirm", "-helper-confirm", "-helper-only-confirm", "-update-choice", "-remove" })
            {
                using (var bitmap = new System.Drawing.Bitmap(Files.Under(previews, "de" + suffix + ".png")))
                    Check(bitmap.Width > 100 && bitmap.Height > 100, "shipped helper rendered " + (suffix.Length == 0 ? "main window" : suffix));
            }
            using (var zone = Updates.InternetZone(stage, false))
            using (var reader = new StreamReader(zone))
                Check(reader.ReadToEnd().Contains("ZoneId=3"), "Windows Internet-origin marking retained after execution");

            // Minimum real-build fixture for the production installation checks,
            // deliberately not a playable copy of the whole game.
            var game = Files.Under(run, "Game"); var profile = Files.Under(run, "Profile");
            var sourceFiles = new[] { "DungeonSettlers.exe", "GameAssembly.dll", "DungeonSettlers_Data/il2cpp_data/Metadata/global-metadata.dat", "MelonLoader/net6/MelonLoader.dll" };
            var sourceHashes = sourceFiles.ToDictionary(x => x, x => Files.FileHash(Files.Under(sourceGame, x)));
            foreach (var name in sourceFiles)
            {
                var target = Files.Under(game, name); Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(Files.Under(sourceGame, name), target);
            }
            Directory.CreateDirectory(Files.Under(profile, "Saves"));
            var save = Files.Under(profile, "Saves/Test.json"); File.Copy(Path.GetFullPath(saveFixture), save);
            var setting = Files.Under(profile, "UserSetting.json"); File.WriteAllText(setting, settings);
            Directory.CreateDirectory(Files.Under(game, "Mods"));
            var foreign = Files.Under(game, "Mods/OtherMod.keep"); File.WriteAllText(foreign, "unrelated test sentinel");
            var saveHash = Files.FileHash(save); var settingsHash = Files.FileHash(setting); var foreignHash = Files.FileHash(foreign);
            var handed = Updates.ParseHandoff(Updates.Handoff(game, profile));
            Check(handed.SequenceEqual(new[] { game, profile }), "handoff preserves both isolated paths, including spaces");
            var op = new Operations(Files.Under(run, "Backups"), new GamePolicy());
            var payload = LocalMods.Recheck(mod, CancellationToken.None);
            var backup = op.Install(handed[0], handed[1], payload);
            Check(backup != null, "local mod installs with real build checks and a completed backup transaction");
            foreach (var entry in payload)
                Check(Files.FileHash(Files.Under(game, entry.Key)) == Files.Hash(entry.Value), "installed bytes match: " + entry.Key);
            Check(LocalMods.Installed(game) == mod.Version, "installed DLL version matches the selected mod");
            Check(op.Install(game, profile, LocalMods.Recheck(mod, CancellationToken.None)) == null,
                "repeating installation creates no additional change or undo entry");
            Check(op.History(game, profile).Count == 1, "exactly one installation transaction exists");
            Check(Files.FileHash(save) == saveHash && Files.FileHash(setting) == settingsHash && Files.FileHash(foreign) == foreignHash,
                "isolated save, settings and unrelated mod remain byte-identical");
            Check(Directory.GetFiles(Files.Under(profile, "Saves")).Length == 1 && !Directory.Exists(Files.Under(game, "Testspielstand")),
                "bundled demo is not imported or extracted during mod installation");
            foreach (var name in sourceFiles)
                Check(Files.FileHash(Files.Under(sourceGame, name)) == sourceHashes[name], "source game file unchanged: " + name);
            Check(Files.FileHash(selected.Path) == sourcePackageHash && Files.FileHash(selected.Path + ".update.json") == signatureHash
                && Files.FileHash(modPath) == modHash, "input packages and signature remain unchanged");
            LocalOnlyChecks.Verify(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Updates.ExeName));
            Console.WriteLine("PASS local integration smoke. Actual helper process: read-only preview. Installation: isolated fixture only. Interactive consent, restart handoff and in-game play remain separate acceptance tests.");
        }
    }
}
