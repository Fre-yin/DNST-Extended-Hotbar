using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

namespace ExtendedHotbar.Helper
{
    internal static class DualLoaderTests
    {
        static void Check(bool ok) { if (!ok) throw new Exception("Dual-loader assertion failed."); }
        static void Refused(Action action) { try { action(); } catch { return; } throw new Exception("Expected refusal."); }
        internal static void Run(Action<string, Action> test, string root, string fixture, string settings)
        {
            foreach (var loader in ModLoaders.All)
            {
                var selected = loader;
                test(selected.Id + ": install / no-op / undo preserve settings, saves and other mods", () => {
                    var e = new Env(root, selected, fixture, settings); var payload = ReleaseInfo.Package(selected);
                    e.Put("OtherMod.txt", new byte[] { 9 }); var original = File.ReadAllBytes(e.Settings);
                    var backup = e.Op.Install(e.Game, e.Profile, payload, selected);
                    Check(payload.All(x => Files.FileHash(Files.Under(e.Game, x.Key)) == Files.Hash(x.Value)));
                    Check(File.ReadAllBytes(e.Settings).SequenceEqual(original) && File.ReadAllText(e.Save) == fixture);
                    Check(e.Op.Install(e.Game, e.Profile, payload, selected) == null);
                    e.Op.Restore(e.Game, e.Profile, Path.GetFileName(backup));
                    Check(selected.Required.All(x => !File.Exists(Files.Under(e.Game, x))) && File.Exists(Files.Under(e.Game, "OtherMod.txt")));
                });
                test(selected.Id + ": wrong payload / wrong loader / mixed loader rejected before backup", () => {
                    var other = ModLoaders.All.Single(x => x != selected); var e = new Env(root, selected, fixture, settings);
                    Refused(() => e.Op.Install(e.Game, e.Profile, ReleaseInfo.Package(other), selected));
                    Refused(() => e.Op.Install(e.Game, e.Profile, ReleaseInfo.Package(other), other));
                    Check(!Directory.Exists(e.Store));
                    e.Put(other.Core, new byte[] { 1 });
                    Check(ModLoaders.Detect(e.Game) == null);
                    Refused(() => e.Op.Install(e.Game, e.Profile, ReleaseInfo.Package(selected), selected));
                    Check(!Directory.Exists(e.Store) && File.ReadAllText(e.Settings) == settings);
                });
                test(selected.Id + ": failed write rolls back selected loader only", () => {
                    var e = new Env(root, selected, fixture, settings);
                    e.Op.AfterWrite = i => { if (i == 1) throw new IOException("injected failure"); };
                    Refused(() => e.Op.Install(e.Game, e.Profile, ReleaseInfo.Package(selected), selected));
                    Check(selected.Required.All(x => !File.Exists(Files.Under(e.Game, x))));
                    Check(File.ReadAllText(e.Settings) == settings && e.Op.History(e.Game, e.Profile).Single().Status == "recovered");
                });
                test(selected.Id + ": removal and undo retain campaign and foreign mod", () => {
                    var e = new Env(root, selected, fixture, settings); e.Put("OtherMod.txt", new byte[] { 9 });
                    e.Op.Install(e.Game, e.Profile, ReleaseInfo.Package(selected), selected); string exported;
                    var backup = e.Op.Disable(e.Game, e.Profile, e.Save, Profiles.VanillaDefaults(), out exported, loader: selected);
                    Check(File.Exists(exported) && File.ReadAllText(e.Save) == fixture && File.Exists(Files.Under(e.Game, "OtherMod.txt")));
                    Check(selected.Required.All(x => !File.Exists(Files.Under(e.Game, x))));
                    e.Op.Restore(e.Game, e.Profile, Path.GetFileName(backup));
                    Check(selected.Required.All(x => File.Exists(Files.Under(e.Game, x))) && File.Exists(exported));
                });
                test(selected.Id + ": exact bundled package discovery, identity and loader-scoped export", () => {
                    var e = new Env(root, selected, fixture, settings); var other = ModLoaders.All.Single(x => x != selected);
                    Check(ModLoaders.Detect(e.Game) == selected);
                    var folder = Files.Under(e.Profile, "Downloads"); Directory.CreateDirectory(folder);
                    var path = Files.Under(folder, selected.FileName); ReleaseInfo.ExportPackage(path, selected);
                    var offer = LocalMods.Scan(folder, Updates.ParseVersion("0.0.0"), CancellationToken.None, loader: selected);
                    Check(offer != null && offer.Loader == selected && offer.Hash == selected.PackageHash);
                    Check(LocalMods.Scan(folder, Updates.ParseVersion("99.0.0"), CancellationToken.None, loader: selected) == null);
                    Check(LocalMods.Scan(folder, Updates.ParseVersion("0.0.0"), CancellationToken.None, loader: other) == null);
                    Refused(() => ReleaseInfo.ReadPackage(ReleaseInfo.PackageBytes(selected), other));
                    e.Op.Install(e.Game, e.Profile, offer.Payload, selected);
                    new GamePolicy().OwnedFile(Files.Under(e.Game, selected.Dll));
                    Check(LocalMods.Installed(e.Game, selected) == Updates.ParseVersion(ReleaseInfo.ModVersion));
                });
                test(selected.Id + ": game running and wrong build block install and key changes", () => {
                    var e = new Env(root, selected, fixture, settings);
                    foreach (var running in new[] { true, false }) {
                        e.Policy.Running = running; e.Policy.Compatible = running;
                        Refused(() => e.Op.Install(e.Game, e.Profile, ReleaseInfo.Package(selected), selected));
                        Refused(() => { string hash; e.Op.PreviewHotbarKeys(e.Game, e.Profile, out hash); });
                    }
                    Check(!Directory.Exists(e.Store));
                });
            }
            test("optional full profile has exact 66 unique rows and no internal key collisions", () => {
                var rows = new LosslessJson(HotbarKeyProfile.Bindings()).Root.Items;
                Check(rows.Count == 66 && rows.Select(x => x.Get("InputType").Integer + ":" + x.Get("SlotIndex").Integer).Distinct().Count() == 66);
                var used = rows.Where(x => x.Get("KeyCode").Integer != 0).ToArray();
                Check(used.Length == 23 && used.Select(x => x.Get("KeyCode").Integer + ":" + x.Get("ModifierKey").Integer).Distinct().Count() == 23);
                Check(rows.Count(x => x.Get("InputType").Integer == 12005 && x.Get("KeyCode").Integer == 53) == 1);
                Check(rows.All(x => x.Get("InputType").Integer != 12011 && x.Get("InputType").Integer != 12012));
            });
            test("profile preview rejects unconfirmed conflicts and stale consent; undo / redo are scoped", () => {
                var e = new Env(root, ModLoaders.BepInEx, fixture, settings);
                var doc = new LosslessJson(settings); var rows = Profiles.Bindings(doc);
                var foreign = Profiles.Row(999, 1, 113, 0);
                var input = doc.Apply(new[] { doc.Replace(rows, "[" + string.Join(",", rows.Items.Select(doc.Raw).Concat(new[] { foreign })) + "]") });
                File.WriteAllText(e.Settings, input); string hash;
                var conflicts = e.Op.PreviewHotbarKeys(e.Game, e.Profile, out hash);
                Check(conflicts.Any(x => x.Get("InputType").Integer == 999));
                Refused(() => e.Op.ConfigureHotbarKeys(e.Game, e.Profile, false, hash));
                Check(!Directory.Exists(e.Store) && File.ReadAllText(e.Settings) == input);
                File.AppendAllText(e.Settings, " "); Refused(() => e.Op.ConfigureHotbarKeys(e.Game, e.Profile, true, hash));
                File.WriteAllText(e.Settings, input);
                var backup = e.Op.ConfigureHotbarKeys(e.Game, e.Profile, true, hash);
                var applied = File.ReadAllText(e.Settings);
                Check(Profiles.Same(Profiles.SelectedCharacters(applied), Profiles.SelectedCharacters(Profiles.Configure(applied, HotbarKeyProfile.Bindings(), HotbarKeyProfile.Affected, false))));
                Check(Profiles.Bindings(new LosslessJson(applied)).Items.Single(x => x.Get("InputType").Integer == 999 && x.Get("SlotIndex").Integer == 1).Get("KeyCode").Integer == 0);
                string currentHash; e.Op.PreviewHotbarKeys(e.Game, e.Profile, out currentHash);
                Check(e.Op.ConfigureHotbarKeys(e.Game, e.Profile, false, currentHash) == null);
                var undo = e.Op.Restore(e.Game, e.Profile, Path.GetFileName(backup));
                Check(Profiles.Same(Profiles.Selected(File.ReadAllText(e.Settings)), Profiles.Selected(input)));
                Check(Profiles.Bindings(new LosslessJson(File.ReadAllText(e.Settings))).Items.Single(x => x.Get("InputType").Integer == 999 && x.Get("SlotIndex").Integer == 1).Get("KeyCode").Integer == 113);
                e.Op.Restore(e.Game, e.Profile, Path.GetFileName(undo));
                Check(Profiles.Same(Profiles.Selected(File.ReadAllText(e.Settings)), Profiles.Selected(applied)) && File.ReadAllText(e.Save) == fixture);
            });
            test("BepInEx status requires exact process, start, path and completed initialization", () => {
                var start = DateTime.UtcNow; const string game = @"J:\Example\Game";
                var prefix = "[Info   :Extended Hotbar (BepInEx preview)] ";
                var marker = prefix + "Hotbar session: 12|" + start.Ticks + "|" + game + "\r\n";
                var ready = prefix + "BepInEx hotbar lifecycle attached.\r\n";
                Check(LoadStatus.AssessBepInEx(marker + ready, game, 12, start, start) == HotbarStatus.Active);
                Check(LoadStatus.AssessBepInEx(marker, game, 12, start, start) == HotbarStatus.Pending);
                Check(LoadStatus.AssessBepInEx(marker + ready, game, 13, start, start) == HotbarStatus.Unknown);
                Check(LoadStatus.AssessBepInEx(marker + ready, game + "Other", 12, start, start) == HotbarStatus.Unknown);
                Check(LoadStatus.AssessBepInEx(marker + ready, game, 12, start.AddSeconds(1), start) == HotbarStatus.Unknown);
                Check(LoadStatus.AssessBepInEx(ready, game, 12, start, start) == HotbarStatus.Unknown);
                Check(LoadStatus.AssessBepInEx(marker + prefix + "Hotbar initialization failed.", game, 12, start, start) == HotbarStatus.Inactive);
            });
            test("future BepInEx updates require a loader-bound publisher signature and unchanged payload", () => {
                var e = new Env(root, ModLoaders.BepInEx, fixture, settings);
                var path = Files.Under(e.Profile, "Extended-Hotbar-BepInEx-0.3.9-bepinex.1.zip");
                using (var signer = new RSACryptoServiceProvider(4096, new CspParameters(24))) {
                    signer.PersistKeyInCsp = false; var publicKey = signer.ToXmlString(false);
                    File.WriteAllBytes(path, SignedBepPackage(signer, ModLoaders.BepInEx.Purpose));
                    var offer = LocalMods.Read(path, CancellationToken.None, publicKey, ModLoaders.BepInEx);
                    Check(offer.Loader == ModLoaders.BepInEx && offer.Version == Updates.ParseVersion("0.3.9"));
                    Check(LocalMods.Recheck(offer, CancellationToken.None, publicKey).Count == 3);
                    File.WriteAllBytes(path, SignedBepPackage(signer, ModLoaders.Melon.Purpose));
                    Refused(() => LocalMods.Read(path, CancellationToken.None, publicKey, ModLoaders.BepInEx));
                    File.WriteAllBytes(path, SignedBepPackage(signer, ModLoaders.BepInEx.Purpose, true));
                    Refused(() => LocalMods.Read(path, CancellationToken.None, publicKey, ModLoaders.BepInEx));
                    Refused(() => LocalMods.Recheck(offer, CancellationToken.None, publicKey));
                }
            });
            test("optional profile undo preserves unrelated edits and refuses later affected edits", () => {
                var e = new Env(root, ModLoaders.Melon, fixture, settings); string hash;
                e.Op.PreviewHotbarKeys(e.Game, e.Profile, out hash);
                var backup = e.Op.ConfigureHotbarKeys(e.Game, e.Profile, true, hash);
                var current = File.ReadAllText(e.Settings); var doc = new LosslessJson(current);
                var unrelated = Profiles.Bindings(doc).Items.First(x => x.Get("InputType").Integer == 999);
                File.WriteAllText(e.Settings, doc.Apply(new[] { doc.Replace(unrelated.Get("KeyCode"), "122") }));
                e.Op.Restore(e.Game, e.Profile, Path.GetFileName(backup));
                Check(Profiles.Bindings(new LosslessJson(File.ReadAllText(e.Settings))).Items.First(x => x.Get("InputType").Integer == 999).Get("KeyCode").Integer == 122);
                e.Op.PreviewHotbarKeys(e.Game, e.Profile, out hash);
                backup = e.Op.ConfigureHotbarKeys(e.Game, e.Profile, true, hash);
                doc = new LosslessJson(File.ReadAllText(e.Settings));
                var affected = Profiles.Bindings(doc).Items.First(x => x.Get("InputType").Integer == 12005);
                File.WriteAllText(e.Settings, doc.Apply(new[] { doc.Replace(affected.Get("KeyCode"), "108") }));
                Refused(() => e.Op.Restore(e.Game, e.Profile, Path.GetFileName(backup)));
            });
        }
        // Synthetic future version, signed with an ephemeral test key, never a publisher key.
        static byte[] SignedBepPackage(RSACryptoServiceProvider signer, string purpose, bool corrupt = false)
        {
            var files = ReleaseInfo.Package(ModLoaders.BepInEx);
            var manifest = new Dictionary<string, object> {
                { "purpose", purpose }, { "repository", Updates.Repository }, { "modVersion", "0.3.9" },
                { "minimumHelper", Updates.HelperVersion }, { "gameHash", ReleaseInfo.GameHash },
                { "metadataHash", ReleaseInfo.MetadataHash }, { "files", files.ToDictionary(x => x.Key, x => Files.Hash(x.Value)) }
            };
            var payload = Files.Utf8.GetBytes(new JavaScriptSerializer().Serialize(manifest));
            files.Add(LocalMods.Manifest, Files.Utf8.GetBytes(new JavaScriptSerializer().Serialize(new {
                format = 1, payload = Convert.ToBase64String(payload),
                signature = Convert.ToBase64String(signer.SignData(payload, CryptoConfig.MapNameToOID("SHA256")))
            })));
            if (corrupt) files[ModLoaders.BepInEx.Dll][0] ^= 1;
            using (var memory = new MemoryStream()) {
                using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, true))
                    foreach (var file in files) using (var stream = zip.CreateEntry(file.Key).Open()) stream.Write(file.Value, 0, file.Value.Length);
                return memory.ToArray();
            }
        }
        sealed class Policy : IGamePolicy
        {
            internal bool Running; internal bool Compatible = true;
            public void Validate(string game, bool compatible) { if (compatible && !Compatible) throw new IOException("unsupported build"); }
            public void Stopped() { if (Running) throw new IOException("game running"); }
            public void OwnedFile(string path) { new GamePolicy().OwnedFile(path); }
        }
        sealed class Env
        {
            internal readonly string Game, Profile, Store, Settings, Save;
            internal readonly Policy Policy = new Policy(); internal readonly Operations Op;
            internal Env(string root, ModLoaderProfile loader, string fixture, string settings)
            {
                var folder = Files.Under(root, "dual-" + Guid.NewGuid().ToString("N"));
                Game = Files.Under(folder, "Game"); Profile = Files.Under(folder, "Profile"); Store = Files.Under(folder, "Backups");
                Put("DungeonSettlers.exe", new byte[] { 1 }); Put(loader.Core, new byte[] { 1 });
                Directory.CreateDirectory(Files.Under(Profile, "Saves")); Settings = Files.Under(Profile, "UserSetting.json");
                Save = Files.Under(Profile, "Saves/source.json"); File.WriteAllText(Save, fixture); File.WriteAllText(Settings, settings);
                Op = new Operations(Store, Policy);
            }
            internal void Put(string path, byte[] bytes) { Files.Atomic(Files.Under(Game, path), bytes); }
        }
    }
}
