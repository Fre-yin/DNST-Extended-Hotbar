using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Security.Cryptography;
using System.Globalization;

namespace ExtendedHotbar.Helper
{
    internal static class UpdateTests
    {
        private static RSACryptoServiceProvider signer;
        private static string publicKey;
        private static DateTime signatureTime;
        internal static void VerifyLocalPackage(string packagePath, string signaturePath, string store)
        {
            var package = Files.Read(Path.GetFullPath(packagePath)); var signature = Files.Read(Path.GetFullPath(signaturePath));
            var fields = new LosslessJson(Files.Text(Convert.FromBase64String(new LosslessJson(Files.Text(signature)).Root.Get("payload").String))).Root;
            string name = Path.GetFileName(packagePath);
            var offer = new UpdateOffer(Updates.ParseVersion(fields.Get("helperVersion").String), name, Files.Hash(package), package.Length,
                fields.Get("prerelease").Text == "true", Files.Hash(signature), signature.Length) { SignedMetadata = signature };
            UpdateSignature.Verify(offer, signature, UpdateSignature.PublicKey(), DateTime.UtcNow);
            string staged = Updates.Stage(package, offer, Files.Root(store), CancellationToken.None);
            bool verifiedLaunch = false; Updates.Launch(staged, package, offer, exe => verifiedLaunch = File.Exists(exe));
            Check(verifiedLaunch);
            Console.WriteLine("PASS actual publisher signature, package, identity, staging and launch verification. Execution deliberately suppressed.");
            Console.WriteLine("Isolated stage: " + staged);
        }
        internal static void Run(Action<string, Action> test, string root, string executable)
        {
            test("helper and tests target Framework 4.8", () => {
                foreach (var assembly in new[] { System.Reflection.Assembly.GetExecutingAssembly(), System.Reflection.Assembly.ReflectionOnlyLoadFrom(executable) })
                {
                    var target = System.Reflection.CustomAttributeData.GetCustomAttributes(assembly).SingleOrDefault(x => x.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute");
                    Check(target != null && (string)target.ConstructorArguments[0].Value == ".NETFramework,Version=v4.8");
                }
                Check(AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName == ".NETFramework,Version=v4.8");
            });
            test("built helper contains no online components or network API references", () => LocalOnlyChecks.Verify(executable));
            test("online-only UI strings are absent in every language", () => {
                var removed = new[] { "updateAutomatic", "updateTests", "updateCheck", "updateChecking", "updateNone", "updateWait", "updateAsk", "updateDownload", "updateTestLabel", "updateStableLabel", "errorUpdatePrivate", "errorUpdateRate", "errorUpdateNetwork", "updateHelp" };
                foreach (var code in UiText.Codes) {
                    var text = new UiText(code); Check(!text.Keys.Intersect(removed).Any()); Check(text.Keys.Contains("localPreparing"));
                    Check(text["title"].Contains(Updates.HelperVersion));
                }
            });
            root = Files.Under(root, "updates-" + Guid.NewGuid().ToString("N"));
            signer = new RSACryptoServiceProvider(4096, new CspParameters(24)); signer.PersistKeyInCsp = false;
            publicKey = signer.ToXmlString(false); signatureTime = DateTime.UtcNow;
            byte[] package = Package(File.ReadAllBytes(executable));
            var current = Updates.ParseVersion("0.1.4");
            var offer = SignedOffer(package);
            var localFolder = Files.Under(root, "local-helper"); Directory.CreateDirectory(localFolder);
            var localPath = Files.Under(localFolder, offer.FileName);
            File.WriteAllBytes(localPath, package); File.WriteAllBytes(localPath + ".update.json", offer.SignedMetadata);
            test("helper-only update discovery works without a game folder", () => {
                var selected = LocalUpdates.Scan(localFolder, "invalid game path", true, CancellationToken.None, publicKey, current);
                Check(selected.Helper != null && selected.Helper.Offer.Hash == offer.Hash && selected.Mod == null && selected.Baseline == null);
            });
            test("helper-only no-update ignores invalid game paths and broken mod ZIPs", () => {
                var folder = Files.Under(root, "helper-only-no-update"); Directory.CreateDirectory(folder);
                File.WriteAllText(Files.Under(folder, "Extended-Hotbar-0.3.9.zip"), "broken mod archive");
                var selected = LocalUpdates.Scan(folder, "invalid game path", true, CancellationToken.None, publicKey, current);
                Check(selected.Helper == null && selected.Mod == null && selected.Baseline == null);
                Throws(() => LocalUpdates.Scan(folder, "", false, CancellationToken.None, publicKey, current));
            });
            test("helper-only launch never supplies a mod-install handoff", () => {
                Check(LocalUpdates.StartArguments(true, "invalid", "invalid") == "");
                Check(LocalUpdates.StartArguments(true, null, null) == "");
                Check(LocalUpdates.StartArguments(true, Files.Under(root, "game"), Files.Under(root, "profile")) == "");
                var args = LocalUpdates.StartArguments(false, Files.Under(root, "game"), Files.Under(root, "profile"));
                Check(args.StartsWith("--update-install ", StringComparison.Ordinal));
                Check(Updates.ParseHandoff(args.Substring("--update-install ".Length)).SequenceEqual(new[] { Files.Under(root, "game"), Files.Under(root, "profile") }));
            });
            test("quiet package line only announces a newer mod or helper", () => {
                Check(LocalUpdates.NoticeKey("updateAvailable") == "updateAvailable");
                Check(LocalUpdates.NoticeKey("localHelperFound") == "localHelperFound");
                foreach (var state in new[] { null, "localNone", "localFound", "localHelperNone", "localChecking", "updateCancelled", "updateUnavailable", "localPreparing" })
                    Check(LocalUpdates.NoticeKey(state) == null);
            });
            test("mod update keeps helper priority without touching game metadata", () => {
                var selected = LocalUpdates.Scan(localFolder, "invalid game path", false, CancellationToken.None, publicKey, current);
                Check(selected.Helper != null && selected.Mod == null);
            });
            test("local helper detects signed newer package and ignores current version", () => {
                var selected = LocalHelpers.Scan(localFolder, CancellationToken.None, publicKey, current);
                Check(selected != null && selected.Offer.Version == offer.Version);
                Check(LocalHelpers.Scan(localFolder, CancellationToken.None, publicKey, offer.Version) == null);
                Check(LocalHelpers.ReadBytes(selected, CancellationToken.None, publicKey).SequenceEqual(package));
            });
            test("local helper requires matching sidecar and rechecks changed ZIP", () => {
                var selected = LocalHelpers.Read(localPath, CancellationToken.None, publicKey);
                File.WriteAllBytes(localPath, new byte[package.Length]); Throws(() => LocalHelpers.ReadBytes(selected, CancellationToken.None, publicKey));
                File.WriteAllBytes(localPath, package); File.WriteAllText(localPath + ".update.json", "{}"); Throws(() => LocalHelpers.Read(localPath, CancellationToken.None, publicKey));
                File.WriteAllBytes(localPath + ".update.json", offer.SignedMetadata);
            });
            test("local helper stages and verifies launch without a network call", () => {
                var selected = LocalHelpers.Read(localPath, CancellationToken.None, publicKey); var bytes = LocalHelpers.ReadBytes(selected, CancellationToken.None, publicKey);
                var folder = Updates.Stage(bytes, selected.Offer, Files.Under(root, "local-helper-stage"), CancellationToken.None, publicKey);
                bool called = false; Updates.Launch(folder, bytes, selected.Offer, exe => called = File.Exists(exe), publicKey); Check(called);
            });
            test("local helper rejects unsigned higher version and cancellation", () => {
                var unsigned = Files.Under(localFolder, "Extended-Hotbar-Helper-0.1.14-test.zip"); File.WriteAllBytes(unsigned, package);
                Throws(() => LocalHelpers.Scan(localFolder, CancellationToken.None, publicKey, current));
                Throws(() => LocalUpdates.Scan(localFolder, "", true, CancellationToken.None, publicKey, current));
                using (var cancel = new CancellationTokenSource()) { cancel.Cancel(); Throws(() => LocalHelpers.Read(localPath, cancel.Token, publicKey)); }
            });
            test("local discovery ignores source archives partial downloads and nested packages", () => {
                var folder = Files.Under(root, "ignored-local"); Directory.CreateDirectory(Files.Under(folder, "nested"));
                foreach (var name in new[] { "Source-code.zip", "Extended-Hotbar-Helper-0.1.14-test.zip.part", "Extended-Hotbar-Helper-0.1.14-test.zip.crdownload", "Extended-Hotbar-Helper-0.1.14-test-mit-Testspielstand.zip" })
                    File.WriteAllText(Files.Under(folder, name), "not a package");
                PutLocal(Files.Under(folder, "nested"), package);
                Check(LocalHelpers.Scan(folder, CancellationToken.None, publicKey, current) == null);
            });
            test("local selection uses numeric versions and excludes current or older helpers", () => {
                var folder = Files.Under(root, "ordered-local"); Directory.CreateDirectory(folder);
                PutLocal(folder, package, "0.1.9"); PutLocal(folder, package);
                Check(LocalHelpers.Scan(folder, CancellationToken.None, publicKey, current).Offer.Version == offer.Version);
                Check(LocalHelpers.Scan(folder, CancellationToken.None, publicKey, offer.Version) == null);
                Check(LocalHelpers.Scan(folder, CancellationToken.None, publicKey, Updates.ParseVersion("0.2.0")) == null);
            });
            test("same-version stable and preview local packages are ambiguous", () => {
                var folder = Files.Under(root, "ambiguous-local"); Directory.CreateDirectory(folder);
                PutLocal(folder, package); PutLocal(folder, package, preview: false);
                Throws(() => LocalHelpers.Scan(folder, CancellationToken.None, publicKey, current));
            });
            test("local reader supports stable packages and binds filename to signed channel", () => {
                var folder = Files.Under(root, "stable-local"); Directory.CreateDirectory(folder);
                var path = PutLocal(folder, package, preview: false);
                Check(!LocalHelpers.Read(path, CancellationToken.None, publicKey).Offer.Prerelease);
                File.WriteAllBytes(path + ".update.json", Signed(package));
                Throws(() => LocalHelpers.Read(path, CancellationToken.None, publicKey));
            });
            test("local reader rejects missing oversized and malformed signature files", () => {
                var folder = Files.Under(root, "invalid-signatures"); Directory.CreateDirectory(folder);
                var path = Files.Under(folder, offer.FileName); File.WriteAllBytes(path, package);
                Throws(() => LocalHelpers.Read(path, CancellationToken.None, publicKey));
                foreach (var signature in new[] { new byte[16385], Files.Utf8.GetBytes("{}") }) {
                    File.WriteAllBytes(path + ".update.json", signature); Throws(() => LocalHelpers.Read(path, CancellationToken.None, publicKey));
                }
            });
            test("signed local package sizes remain bounded before extraction", () => {
                var folder = Files.Under(root, "invalid-sizes"); Directory.CreateDirectory(folder);
                var path = PutLocal(folder, package);
                foreach (var size in new[] { 0, -1, Updates.MaxPackage + 1 }) {
                    File.WriteAllBytes(path + ".update.json", Signed(package, "\"size\":" + package.Length, "\"size\":" + size));
                    Throws(() => LocalHelpers.Read(path, CancellationToken.None, publicKey));
                }
            });
            test("update versions reject ambiguous or unsupported formats", () => { foreach (var version in new[] { "1.2", "1.2.3.4", "1.2.3-beta", "01.2.3", "65536.1.1", "../1.2.3" }) Throws(() => Updates.ParseVersion(version)); });
            test("bounded local input and cancellation stop oversized partial data", () => { Throws(() => Updates.ReadBounded(new MemoryStream(new byte[5]), 4, CancellationToken.None)); Check(Updates.ReadBounded(new MemoryStream(new byte[4]), 4, CancellationToken.None).Length == 4); using (var c = new CancellationTokenSource()) { c.Cancel(); Throws(() => Updates.ReadBounded(new MemoryStream(new byte[4]), 4, c.Token));  } });
            test("package hash and declared size mismatch are refused", () => { var corrupt = (byte[])package.Clone(); corrupt[0] ^= 1; Throws(() => Updates.Unpack(corrupt, offer, CancellationToken.None)); Throws(() => Updates.Unpack(package.Take(package.Length - 1).ToArray(), offer, CancellationToken.None)); });
            test("verified package contains exactly helper and notices", () => Check(Updates.Unpack(package, offer, CancellationToken.None).Keys.OrderBy(x => x).SequenceEqual(Updates.PackageFiles.OrderBy(x => x))));
            test("zip traversal duplicate linked and extra files are refused", () => {
                foreach (var badName in new[] { "../escape.exe", "C:/escape.exe", "Instructions\\de.txt", "Instructions/de.txt:payload", Updates.ExeName, "Helper.Tests.exe" })
                { var bad = Package(File.ReadAllBytes(executable), badName); var badOffer = Offer(bad); Throws(() => Updates.Unpack(bad, badOffer, CancellationToken.None)); }
                var linked = Package(File.ReadAllBytes(executable), null, true); Throws(() => Updates.Unpack(linked, Offer(linked), CancellationToken.None));
            });
            test("zip entry and total expansion are bounded", () => { var large = Package(File.ReadAllBytes(executable), null, false, new byte[2 * 1024 * 1024 + 1]); Throws(() => Updates.Unpack(large, Offer(large), CancellationToken.None)); });
            string stage = null;
            test("stage is side by side and old helper remains unchanged", () => { string store = Files.Under(root, "update-stage"); Directory.CreateDirectory(store); string old = Files.Under(store, Updates.ExeName); File.WriteAllText(old, "old-helper"); stage = Updates.Stage(package, offer, store, CancellationToken.None, publicKey); Check(File.ReadAllText(old) == "old-helper"); Check(File.ReadAllBytes(Files.Under(stage, Updates.ExeName)).SequenceEqual(File.ReadAllBytes(executable))); Check(Directory.GetFiles(stage, "*", SearchOption.AllDirectories).Length == 21); });
            test("Windows security zone remains without an invented download URL", () => {
                using (var stream = Updates.InternetZone(stage, false)) {
                    var zone = Files.Text(Updates.ReadBounded(stream, 4096, CancellationToken.None));
                    Check(zone.Contains("ZoneId=3") && !zone.Contains("HostUrl="));
                }
            });
            test("launch verifies files again and uses a non-shell-command target", () => { string started = null; Updates.Launch(stage, package, offer, exe => started = exe, publicKey); Check(started == Files.Under(stage, Updates.ExeName)); File.AppendAllText(Files.Under(stage, "LICENSE"), "changed"); bool launched = false; Throws(() => Updates.Launch(stage, package, offer, exe => launched = true, publicKey)); Check(!launched); });
            test("wrong helper identity or version cannot be staged for execution", () => { var fake = Package(Files.Utf8.GetBytes("not an executable")); Throws(() => Updates.Stage(fake, SignedOffer(fake), Files.Under(root, "bad-stage"), CancellationToken.None, publicKey)); var wrongVersion = Offer(package, Signed(package, "0.1.13", "0.1.14"), "0.1.14"); Throws(() => Updates.Stage(package, wrongVersion, Files.Under(root, "wrong-version"), CancellationToken.None, publicKey)); });
            test("cancelled stage creates no launchable destination", () => { string destination = Files.Under(root, "cancel-stage"); using (var c = new CancellationTokenSource()) { c.Cancel(); Throws(() => Updates.Stage(package, offer, destination, c.Token, publicKey)); } Check(!Directory.Exists(destination)); });
            test("local preferences preserve opt-out and corrupt preferences disable checks", () => {
                var path = Files.Under(root, "preferences/options.json"); Check(LocalUpdatePreferences.Load(path).Automatic);
                new LocalUpdatePreferences { Automatic = false }.Save(path); Check(!LocalUpdatePreferences.Load(path).Automatic);
                File.WriteAllText(path, "bad json"); Check(!LocalUpdatePreferences.Load(path).Automatic);
                File.WriteAllText(path, new string(' ', 2049)); Check(!LocalUpdatePreferences.Load(path).Automatic);
            });
            test("legacy local preference keeps opt-out but removes unused release-channel field", () => {
                var path = Files.Under(root, "preferences/legacy.json"); File.WriteAllText(path, "{\"automatic\":false,\"includeTests\":true}");
                var prefs = LocalUpdatePreferences.Load(path); Check(!prefs.Automatic); prefs.Save(path);
                Check(File.ReadAllText(path) == "{\"automatic\":false}");
                File.WriteAllText(path, "{\"automatic\":true,\"includeTests\":false}"); Check(LocalUpdatePreferences.Load(path).Automatic);
            });
            test("update handoff is data only and rejects broad or network paths", () => { string game = Files.Under(root, "Game with spaces ä"), profile = Files.Under(root, "Profile"); Check(Updates.ParseHandoff(Updates.Handoff(game, profile)).SequenceEqual(new[] { game, profile })); Throws(() => Updates.Handoff("C:\\", profile)); Throws(() => Updates.Handoff("\\\\host\\share", profile)); Throws(() => Updates.ParseHandoff(new string('A', 8193))); });
            test("signed update validates with pinned key and is rechecked at use", () => UpdateSignature.Verify(offer, offer.SignedMetadata, publicKey, DateTime.UtcNow));
            test("missing signature cannot create files or launch code", () => {
                var unsigned = Offer(package); string destination = Files.Under(root, "unsigned-stage"); bool launched = false;
                Throws(() => Updates.Stage(package, unsigned, destination, CancellationToken.None, publicKey)); Check(!Directory.Exists(destination));
                Throws(() => Updates.Launch(stage, package, unsigned, exe => launched = true, publicKey)); Check(!launched);
            });
            test("modified local payload is rejected even with replaced envelope hashes", () => {
                var envelope = new LosslessJson(Files.Text(offer.SignedMetadata));
                var payload = Convert.FromBase64String(envelope.Root.Get("payload").String); payload[8] ^= 1;
                var forged = Files.Utf8.GetBytes(envelope.Apply(new[] { envelope.Replace(envelope.Root.Get("payload"), LosslessJson.Quote(Convert.ToBase64String(payload))) }));
                var altered = WithSignature(offer, forged); Throws(() => UpdateSignature.Verify(altered, forged, publicKey, DateTime.UtcNow));
            });
            test("a different publisher key cannot authorize an update", () => {
                using (var other = new RSACryptoServiceProvider(4096, new CspParameters(24))) {
                    other.PersistKeyInCsp = false; var forged = Signed(package, null, null, other);
                    Throws(() => UpdateSignature.Verify(WithSignature(offer, forged), forged, publicKey, DateTime.UtcNow));
                }
            });
            test("signed scope binds repository version name size hash and release channel", () => {
                var replacements = new Dictionary<string,string> { { Updates.Repository, "evil/repo" }, { "ExtendedHotbarHelper.Update.v1", "OtherProduct" }, { "\"helperVersion\":\"0.1.13\"", "\"helperVersion\":\"0.1.14\"" }, { "Extended-Hotbar-Helper-0.1.13-test.zip", "Other.zip" }, { "\"size\":" + package.Length, "\"size\":1" }, { Files.Hash(package), new string('B', 64) }, { "\"prerelease\":true", "\"prerelease\":false" } };
                foreach (var pair in replacements) { var signed = Signed(package, pair.Key, pair.Value); Throws(() => UpdateSignature.Verify(WithSignature(offer, signed), signed, publicKey, DateTime.UtcNow)); }
            });
            test("expired future-dated and excessive-lifetime signatures fail closed", () => {
                Throws(() => UpdateSignature.Verify(offer, offer.SignedMetadata, publicKey, signatureTime.AddDays(181)));
                Throws(() => UpdateSignature.Verify(offer, offer.SignedMetadata, publicKey, signatureTime.AddDays(-1)));
                var signed = Signed(package, signatureTime.AddDays(180).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture), signatureTime.AddDays(181).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
                Throws(() => UpdateSignature.Verify(WithSignature(offer, signed), signed, publicKey, DateTime.UtcNow));
            });
            test("private keys and external XML entities cannot be accepted as public trust", () => { Throws(() => UpdateSignature.ValidatePublicKey(signer.ToXmlString(true))); Throws(() => UpdateSignature.ValidatePublicKey("<!DOCTYPE x [<!ENTITY ext SYSTEM 'https://evil.test/key'>]><RSAKeyValue>&ext;</RSAKeyValue>")); UpdateSignature.ValidatePublicKey(UpdateSignature.PublicKey()); });
            signer.Dispose();
        }
        private static UpdateOffer WithSignature(UpdateOffer offer, byte[] envelope)
        { return new UpdateOffer(offer.Version, offer.FileName, offer.Hash, offer.Size, offer.Prerelease, Files.Hash(envelope), envelope.Length) { SignedMetadata = envelope }; }
        private static UpdateOffer Offer(byte[] package, byte[] envelope = null, string version = "0.1.13")
        { return new UpdateOffer(Updates.ParseVersion(version), "Extended-Hotbar-Helper-" + version + "-test.zip", Files.Hash(package), package.Length, true, envelope == null ? "" : Files.Hash(envelope), envelope == null ? 0 : envelope.Length) { SignedMetadata = envelope }; }
        private static UpdateOffer SignedOffer(byte[] package)
        { return Offer(package, Signed(package)); }
        private static string PutLocal(string folder, byte[] package, string version = "0.1.13", bool preview = true)
        {
            var path = Files.Under(folder, "Extended-Hotbar-Helper-" + version + (preview ? "-test" : "") + ".zip");
            File.WriteAllBytes(path, package); File.WriteAllBytes(path + ".update.json", Signed(package, version: version, preview: preview));
            return path;
        }
        private static byte[] Signed(byte[] package, string changeFrom = null, string changeTo = null, RSACryptoServiceProvider key = null, string version = "0.1.13", bool preview = true)
        {
            var fields = "{\"purpose\":\"ExtendedHotbarHelper.Update.v1\",\"repository\":" + LosslessJson.Quote(Updates.Repository)
                + ",\"helperVersion\":\"0.1.13\",\"fileName\":\"Extended-Hotbar-Helper-0.1.13-test.zip\",\"size\":" + package.Length
                + ",\"sha256\":\"" + Files.Hash(package) + "\",\"prerelease\":true,\"issuedUtc\":" + LosslessJson.Quote(signatureTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))
                + ",\"expiresUtc\":" + LosslessJson.Quote(signatureTime.AddDays(180).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)) + "}";
            if (version != "0.1.13") fields = fields.Replace("0.1.13", version);
            if (!preview) fields = fields.Replace("-test.zip", ".zip").Replace("\"prerelease\":true", "\"prerelease\":false");
            if (changeFrom != null) fields = fields.Replace(changeFrom, changeTo);
            var bytes = Files.Utf8.GetBytes(fields);
            return Files.Utf8.GetBytes("{\"format\":1,\"payload\":" + LosslessJson.Quote(Convert.ToBase64String(bytes)) + ",\"signature\":"
                + LosslessJson.Quote(Convert.ToBase64String((key ?? signer).SignData(bytes, CryptoConfig.MapNameToOID("SHA256")))) + "}");
        }
        private static byte[] Package(byte[] exe, string badName = null, bool linked = false, byte[] large = null)
        {
            using (var memory = new MemoryStream())
            {
                using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
                {
                    foreach (var name in Updates.PackageFiles)
                    {
                        var entry = archive.CreateEntry(name == "LICENSE" && badName != null ? badName : name);
                        if (linked && name == "LICENSE") entry.ExternalAttributes = unchecked((int)0xA0000000);
                        using (var target = entry.Open()) { var bytes = name == Updates.ExeName ? exe : name == "LICENSE" && large != null ? large : Files.Utf8.GetBytes("test notice"); target.Write(bytes, 0, bytes.Length); }
                    }
                }
                return memory.ToArray();
            }
        }
        private static void Check(bool condition) { if (!condition) throw new Exception("Update assertion failed."); }
        private static void Throws(Action action) { try { action(); } catch { return; } throw new Exception("Expected update rejection."); }
    }
}
