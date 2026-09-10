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
            string name = Path.GetFileName(packagePath), tag = "helper-v" + fields.Get("helperVersion").String;
            string url = "https://github.com/" + Updates.Repository + "/releases/download/" + tag + "/" + name;
            var offer = new UpdateOffer(Updates.ParseVersion(fields.Get("helperVersion").String), name, Files.Hash(package), url, tag, package.Length,
                fields.Get("prerelease").Text == "true", url + ".update.json", Files.Hash(signature), signature.Length) { SignedMetadata = signature };
            UpdateSignature.Verify(offer, signature, UpdateSignature.PublicKey(), DateTime.UtcNow);
            string staged = Updates.Stage(package, offer, Files.Root(store), CancellationToken.None);
            bool verifiedLaunch = false; Updates.Launch(staged, package, offer, exe => verifiedLaunch = File.Exists(exe));
            Check(verifiedLaunch);
            Console.WriteLine("PASS actual publisher signature, package, identity, staging and launch verification. Execution deliberately suppressed.");
            Console.WriteLine("Isolated stage: " + staged);
        }
        internal static void Run(Action<string, Action> test, string root, string executable)
        {
            test("helper and tests target Framework 4.8 with modern TLS defaults", () => {
                foreach (var assembly in new[] { System.Reflection.Assembly.GetExecutingAssembly(), System.Reflection.Assembly.ReflectionOnlyLoadFrom(executable) })
                {
                    var target = System.Reflection.CustomAttributeData.GetCustomAttributes(assembly).SingleOrDefault(x => x.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute");
                    Check(target != null && (string)target.ConstructorArguments[0].Value == ".NETFramework,Version=v4.8");
                }
                Check(AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName == ".NETFramework,Version=v4.8");
                bool disabled;
                AppContext.TryGetSwitch("Switch.System.Net.DontEnableSystemDefaultTlsVersions", out disabled); Check(!disabled);
                AppContext.TryGetSwitch("Switch.System.Net.DontEnableSchUseStrongCrypto", out disabled); Check(!disabled);
            });
            root = Files.Under(root, "updates-" + Guid.NewGuid().ToString("N"));
            signer = new RSACryptoServiceProvider(4096, new CspParameters(24)); signer.PersistKeyInCsp = false;
            publicKey = signer.ToXmlString(false); signatureTime = DateTime.UtcNow;
            byte[] package = Package(File.ReadAllBytes(executable));
            string feed = Feed(package);
            var current = Updates.ParseVersion("0.1.4");
            var offer = Updates.Select(feed, current, true); offer.SignedMetadata = Signed(package);
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
                foreach (var state in new[] { null, "localNone", "localFound", "localHelperNone", "localChecking", "updateCancelled", "updateUnavailable", "updateDownloading" })
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
                var unsigned = Files.Under(localFolder, "Extended-Hotbar-Helper-0.1.13-test.zip"); File.WriteAllBytes(unsigned, package);
                Throws(() => LocalHelpers.Scan(localFolder, CancellationToken.None, publicKey, current));
                Throws(() => LocalUpdates.Scan(localFolder, "", true, CancellationToken.None, publicKey, current));
                using (var cancel = new CancellationTokenSource()) { cancel.Cancel(); Throws(() => LocalHelpers.Read(localPath, cancel.Token, publicKey)); }
            });
            test("update selects newer helper from the pinned public release", () => { Check(offer.Version == Updates.ParseVersion("0.1.12")); Check(offer.Prerelease && offer.Hash == Files.Hash(package)); });
            test("update ignores current older and draft versions", () => { Check(Updates.Select(feed, offer.Version, true) == null); Check(Updates.Select(feed.Replace("\"draft\":false", "\"draft\":true"), current, true) == null); Check(Updates.Select(feed, Updates.ParseVersion("0.2.0"), true) == null); });
            test("stable channel excludes test names and prereleases", () => { Check(Updates.Select(feed, current, false) == null); Check(Updates.Select(feed.Replace("\"prerelease\":true", "\"prerelease\":false"), current, false) == null); Check(Updates.Select(feed.Replace("-test.zip", ".zip").Replace("\"prerelease\":true", "\"prerelease\":false"), current, false) != null); });
            test("source and test-save archives are not update packages", () => { Check(Updates.Select(feed.Replace("-test.zip", "-test-mit-Testspielstand.zip"), current, true) == null); Check(Updates.Select(feed.Replace("Extended-Hotbar-Helper-0.1.12-test.zip", "Source-code.zip"), current, true) == null); });
            test("numeric version ordering is independent of release order", () => {
                var newer = feed.Substring(1, feed.Length - 2); var older = newer.Replace("0.1.12", "0.1.9");
                foreach (var releases in new[] { "[" + newer + "," + older + "]", "[" + older + "," + newer + "]" })
                    Check(Updates.Select(releases, current, true).Version == Updates.ParseVersion("0.1.12"));
            });
            test("duplicate version with different digest is refused", () => { var release = feed.Substring(1, feed.Length - 2); Throws(() => Updates.Select("[" + release + "," + release.Replace(Files.Hash(package), new string('A', 64)) + "]", current, true)); });
            test("metadata requires GitHub digest and completed asset size", () => { Throws(() => Updates.Select(feed.Replace("sha256:", "md5:"), current, true)); Throws(() => Updates.Select(feed.Replace("\"uploaded\"", "\"new\""), current, true)); Throws(() => Updates.Select(feed.Replace("\"size\":" + package.Length, "\"size\":0"), current, true)); Throws(() => Updates.Select(feed.Replace("\"size\":" + package.Length, "\"size\":33554433"), current, true)); });
            test("metadata rejects other repository HTTP and malformed structures", () => { Throws(() => Updates.Select(feed.Replace("https://github.com/", "http://github.com/"), current, true)); Throws(() => Updates.Select(feed.Replace(Updates.Repository, "other/repository"), current, true)); Throws(() => Updates.Select("{}", current, true)); Throws(() => Updates.Select(feed.Replace("\"draft\":false", "\"draft\":\"false\""), current, true)); Throws(() => Updates.Select(feed.Replace("helper-v0.1.12", "../main"), current, true)); });
            test("update versions reject ambiguous or unsupported formats", () => { foreach (var version in new[] { "1.2", "1.2.3.4", "1.2.3-beta", "01.2.3", "65536.1.1", "../1.2.3" }) Throws(() => Updates.ParseVersion(version)); });
            test("download origins allow only fixed HTTPS and GitHub asset redirects", () => {
                Check(GitHubTransport.Allowed(new Uri(Updates.Feed), false)); Check(GitHubTransport.Allowed(new Uri(offer.Url), false));
                Check(GitHubTransport.Allowed(new Uri("https://release-assets.githubusercontent.com/github-production-release-asset/asset?sig=test"), true));
                foreach (var url in new[] { "http://github.com/" + Updates.Repository + "/releases/download/tag/file.zip", "https://evil.test/file", "https://github.com.evil.test/file", "https://github.com/other/repo/releases/download/tag/file.zip", "https://user@github.com/" + Updates.Repository + "/releases/download/tag/file.zip", "https://github.com:444/" + Updates.Repository + "/releases/download/tag/file.zip", "https://release-assets.githubusercontent.com.evil.test/a", "file:///C:/fake.zip" }) Check(!GitHubTransport.Allowed(new Uri(url), true));
                Check(!GitHubTransport.Allowed(new Uri("https://release-assets.githubusercontent.com/a"), false));
            });
            test("transport check downloads no package and no local files", () => { var transport = new FakeTransport(feed, package); var client = new UpdateClient(transport, publicKey); Check(client.Check(current, true, CancellationToken.None) != null); Check(transport.Calls.SequenceEqual(new[] { Updates.Feed, offer.SignatureUrl })); });
            test("transport package download verifies digest before staging", () => { var transport = new FakeTransport(feed, package); Check(new UpdateClient(transport, publicKey).Download(offer, CancellationToken.None).SequenceEqual(package)); Check(transport.Calls.Single() == offer.Url); transport.Package = new byte[package.Length]; Throws(() => new UpdateClient(transport, publicKey).Download(offer, CancellationToken.None)); });
            test("bounded response and cancellation stop oversized partial data", () => { Throws(() => Updates.ReadBounded(new MemoryStream(new byte[5]), 4, CancellationToken.None)); Check(Updates.ReadBounded(new MemoryStream(new byte[4]), 4, CancellationToken.None).Length == 4); using (var c = new CancellationTokenSource()) { c.Cancel(); Throws(() => Updates.ReadBounded(new MemoryStream(new byte[4]), 4, c.Token)); Throws(() => new UpdateClient(new FakeTransport(feed, package), publicKey).Check(current, true, c.Token)); } });
            test("package hash and declared size mismatch are refused", () => { var corrupt = (byte[])package.Clone(); corrupt[0] ^= 1; Throws(() => Updates.Unpack(corrupt, offer, CancellationToken.None)); Throws(() => Updates.Unpack(package.Take(package.Length - 1).ToArray(), offer, CancellationToken.None)); });
            test("verified package contains exactly helper and notices", () => Check(Updates.Unpack(package, offer, CancellationToken.None).Keys.OrderBy(x => x).SequenceEqual(Updates.PackageFiles.OrderBy(x => x))));
            test("zip traversal duplicate linked and extra files are refused", () => {
                foreach (var badName in new[] { "../escape.exe", "C:/escape.exe", "Instructions\\de.txt", "Instructions/de.txt:payload", Updates.ExeName, "Helper.Tests.exe" })
                { var bad = Package(File.ReadAllBytes(executable), badName); var badOffer = Updates.Select(Feed(bad), current, true); Throws(() => Updates.Unpack(bad, badOffer, CancellationToken.None)); }
                var linked = Package(File.ReadAllBytes(executable), null, true); Throws(() => Updates.Unpack(linked, Updates.Select(Feed(linked), current, true), CancellationToken.None));
            });
            test("zip entry and total expansion are bounded", () => { var large = Package(File.ReadAllBytes(executable), null, false, new byte[2 * 1024 * 1024 + 1]); Throws(() => Updates.Unpack(large, Updates.Select(Feed(large), current, true), CancellationToken.None)); });
            string stage = null;
            test("stage is side by side and old helper remains unchanged", () => { string store = Files.Under(root, "update-stage"); Directory.CreateDirectory(store); string old = Files.Under(store, Updates.ExeName); File.WriteAllText(old, "old-helper"); stage = Updates.Stage(package, offer, store, CancellationToken.None, publicKey); Check(File.ReadAllText(old) == "old-helper"); Check(File.ReadAllBytes(Files.Under(stage, Updates.ExeName)).SequenceEqual(File.ReadAllBytes(executable))); Check(Directory.GetFiles(stage, "*", SearchOption.AllDirectories).Length == 21); });
            test("internet origin is retained for Windows security checks", () => { using (var stream = Updates.InternetZone(stage, false)) Check(Files.Text(Updates.ReadBounded(stream, 4096, CancellationToken.None)).Contains("ZoneId=3")); });
            test("launch verifies files again and uses a non-shell-command target", () => { string started = null; Updates.Launch(stage, package, offer, exe => started = exe, publicKey); Check(started == Files.Under(stage, Updates.ExeName)); File.AppendAllText(Files.Under(stage, "LICENSE"), "changed"); bool launched = false; Throws(() => Updates.Launch(stage, package, offer, exe => launched = true, publicKey)); Check(!launched); });
            test("wrong helper identity or version cannot be staged for execution", () => { var fake = Package(Files.Utf8.GetBytes("not an executable")); Throws(() => Updates.Stage(fake, SignedOffer(fake, current), Files.Under(root, "bad-stage"), CancellationToken.None, publicKey)); var wrongVersion = Updates.Select(feed.Replace("0.1.12", "0.1.13"), current, true); wrongVersion.SignedMetadata = offer.SignedMetadata; Throws(() => Updates.Stage(package, wrongVersion, Files.Under(root, "wrong-version"), CancellationToken.None, publicKey)); });
            test("cancelled stage creates no launchable destination", () => { string destination = Files.Under(root, "cancel-stage"); using (var c = new CancellationTokenSource()) { c.Cancel(); Throws(() => Updates.Stage(package, offer, destination, c.Token, publicKey)); } Check(!Directory.Exists(destination)); });
            test("preferences preserve opt-out and corrupt preferences disable checks", () => { var path = Files.Under(root, "preferences/options.json"); Check(UpdatePreferences.Load(path).Automatic); new UpdatePreferences { Automatic = false, IncludeTests = false }.Save(path); Check(!UpdatePreferences.Load(path).Automatic && !UpdatePreferences.Load(path).IncludeTests); File.WriteAllText(path, "bad json"); Check(!UpdatePreferences.Load(path).Automatic); });
            test("update handoff is data only and rejects broad or network paths", () => { string game = Files.Under(root, "Game with spaces ä"), profile = Files.Under(root, "Profile"); Check(Updates.ParseHandoff(Updates.Handoff(game, profile)).SequenceEqual(new[] { game, profile })); Throws(() => Updates.Handoff("C:\\", profile)); Throws(() => Updates.Handoff("\\\\host\\share", profile)); Throws(() => Updates.ParseHandoff(new string('A', 8193))); });
            test("signed update validates with pinned key and is rechecked at use", () => UpdateSignature.Verify(offer, offer.SignedMetadata, publicKey, DateTime.UtcNow));
            test("missing signature cannot create files or launch code", () => {
                var unsigned = Updates.Select(feed, current, true); string destination = Files.Under(root, "unsigned-stage"); bool launched = false;
                Throws(() => Updates.Stage(package, unsigned, destination, CancellationToken.None, publicKey)); Check(!Directory.Exists(destination));
                Throws(() => Updates.Launch(stage, package, unsigned, exe => launched = true, publicKey)); Check(!launched);
            });
            test("network attacker cannot replace signed payload even with new metadata hashes", () => {
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
                var replacements = new Dictionary<string,string> { { Updates.Repository, "evil/repo" }, { "ExtendedHotbarHelper.Update.v1", "OtherProduct" }, { "\"helperVersion\":\"0.1.12\"", "\"helperVersion\":\"0.1.13\"" }, { "Extended-Hotbar-Helper-0.1.12-test.zip", "Other.zip" }, { "\"size\":" + package.Length, "\"size\":1" }, { Files.Hash(package), new string('B', 64) }, { "\"prerelease\":true", "\"prerelease\":false" } };
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
        { return new UpdateOffer(offer.Version, offer.FileName, offer.Hash, offer.Url, offer.Tag, offer.Size, offer.Prerelease, offer.SignatureUrl, Files.Hash(envelope), envelope.Length) { SignedMetadata = envelope }; }
        private static string Feed(byte[] package)
        {
            string name = "Extended-Hotbar-Helper-0.1.12-test.zip", tag = "helper-v0.1.12";
            return "[{\"draft\":false,\"prerelease\":true,\"tag_name\":" + LosslessJson.Quote(tag) + ",\"assets\":[{\"name\":" + LosslessJson.Quote(name)
                + ",\"state\":\"uploaded\",\"size\":" + package.Length + ",\"digest\":\"sha256:" + Files.Hash(package) + "\",\"browser_download_url\":"
                + LosslessJson.Quote("https://github.com/" + Updates.Repository + "/releases/download/" + tag + "/" + name) + "}," + SignatureAsset(package, name, tag) + "]}]";
        }
        private static UpdateOffer SignedOffer(byte[] package, Version current)
        { var offer = Updates.Select(Feed(package), current, true); offer.SignedMetadata = Signed(package); return offer; }
        private static string SignatureAsset(byte[] package, string name, string tag)
        {
            var signature = Signed(package);
            return "{\"name\":" + LosslessJson.Quote(name + ".update.json") + ",\"state\":\"uploaded\",\"size\":" + signature.Length + ",\"digest\":\"sha256:" + Files.Hash(signature)
                + "\",\"browser_download_url\":" + LosslessJson.Quote("https://github.com/" + Updates.Repository + "/releases/download/" + tag + "/" + name + ".update.json") + "}";
        }
        private static byte[] Signed(byte[] package, string changeFrom = null, string changeTo = null, RSACryptoServiceProvider key = null)
        {
            var fields = "{\"purpose\":\"ExtendedHotbarHelper.Update.v1\",\"repository\":" + LosslessJson.Quote(Updates.Repository)
                + ",\"helperVersion\":\"0.1.12\",\"fileName\":\"Extended-Hotbar-Helper-0.1.12-test.zip\",\"size\":" + package.Length
                + ",\"sha256\":\"" + Files.Hash(package) + "\",\"prerelease\":true,\"issuedUtc\":" + LosslessJson.Quote(signatureTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))
                + ",\"expiresUtc\":" + LosslessJson.Quote(signatureTime.AddDays(180).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)) + "}";
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
        private sealed class FakeTransport : IUpdateTransport
        {
            internal readonly List<string> Calls = new List<string>();
            internal byte[] Package;
            private readonly string feed;
            internal FakeTransport(string feed, byte[] package) { this.feed = feed; Package = package; }
            public byte[] Get(string url, int limit, CancellationToken token) { token.ThrowIfCancellationRequested(); Calls.Add(url); var bytes = url == Updates.Feed ? Files.Utf8.GetBytes(feed) : url.EndsWith(".update.json", StringComparison.Ordinal) ? Signed(Package) : Package; if (bytes.Length > limit) throw Updates.Invalid("fake response too large"); return bytes; }
        }
    }
}
