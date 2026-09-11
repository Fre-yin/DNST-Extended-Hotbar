using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Web.Script.Serialization;

namespace ExtendedHotbar.Helper
{
    internal static class LocalModTests
    {
        private static void Check(bool ok) { if (!ok) throw new Exception("Local update assertion failed."); }
        private static void Refused(Action action) { try { action(); } catch (Exception) { return; } throw new Exception("Expected refusal."); }
        internal static void Run(Action<string, Action> test, string root)
        {
            string store = Files.Under(root, "local-mods-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(store);
            using (var signer = new RSACryptoServiceProvider(4096, new CspParameters(24)))
            {
                signer.PersistKeyInCsp = false; var publicKey = signer.ToXmlString(false);
                var zero = Updates.ParseVersion("0.0.0"); var version = Updates.ParseVersion("0.3.9");
                Func<string, byte[]> archive = v => Package(signer, v);
                test("local filenames ignore helpers source archives and partial downloads", () => {
                    foreach (var file in new[] { "DNST-Extended-Hotbar-main.zip", "Extended-Hotbar-Helper-0.1.6-test.zip", "Extended-Hotbar-0.3.9.zip.part", "Extended-Hotbar-0.3.9.zip.crdownload" }) Check(LocalMods.FileVersion(file) == null);
                    Check(LocalMods.FileVersion("Extended-Hotbar-0.3.9 (1).zip") == version);
                    Check(LocalMods.FileVersion("Extended-Hotbar-0.3.9-mit-Testspielstand.zip") == version);
                });
                test("local scan is read only and does not search nested folders", () => {
                    string folder = Files.Under(store, "empty"); Directory.CreateDirectory(Files.Under(folder, "nested"));
                    File.WriteAllBytes(Files.Under(folder, "nested/Extended-Hotbar-0.3.9.zip"), archive("0.3.9"));
                    Check(LocalMods.Scan(folder, zero, CancellationToken.None, publicKey) == null);
                    Check(Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Length == 1);
                });
                test("original unsigned mod variants work only with pinned install bytes", () => {
                    var path = Put(store, "0.3.8", Package(null, "0.3.8"));
                    var result = LocalMods.Read(path, CancellationToken.None, publicKey); Check(result.Payload.Count == 3);
                    var demo = Files.Under(store, "Extended-Hotbar-0.3.8-mit-Testspielstand.zip"); File.WriteAllBytes(demo, Package(null, "0.3.8", extra: "Saves/demo.json"));
                    Check(LocalMods.Read(demo, CancellationToken.None, publicKey).Payload.Count == 3);
                });
                test("future local mod requires the pinned publisher signature", () => {
                    var path = Put(store, "0.3.9", archive("0.3.9")); Check(LocalMods.Read(path, CancellationToken.None, publicKey).Version == version);
                    using (var other = new RSACryptoServiceProvider(4096, new CspParameters(24))) {
                        other.PersistKeyInCsp = false; Refused(() => LocalMods.Read(path, CancellationToken.None, other.ToXmlString(false)));
                    }
                    File.WriteAllBytes(path, Package(null, "0.3.9")); Refused(() => LocalMods.Read(path, CancellationToken.None, publicKey));
                });
                test("local scan sorts versions numerically and never offers a downgrade", () => {
                    var folder = Files.Under(store, "ordered"); Directory.CreateDirectory(folder);
                    Put(folder, "0.3.10", archive("0.3.10")); Put(folder, "0.3.11", archive("0.3.11"));
                    Check(LocalMods.Scan(folder, zero, CancellationToken.None, publicKey).Version == Updates.ParseVersion("0.3.11"));
                    Check(LocalMods.Scan(folder, Updates.ParseVersion("0.3.12"), CancellationToken.None, publicKey) == null);
                });
                test("equivalent browser duplicates are accepted but conflicting bytes refused", () => {
                    var folder = Files.Under(store, "duplicates"); Directory.CreateDirectory(folder);
                    var path = Put(folder, "0.3.9", archive("0.3.9")); var duplicate = Files.Under(folder, "Extended-Hotbar-0.3.9 (1).zip");
                    File.Copy(path, duplicate); Check(LocalMods.Scan(folder, zero, CancellationToken.None, publicKey) != null);
                    File.WriteAllBytes(duplicate, Package(signer, "0.3.9", modified: true)); Refused(() => LocalMods.Scan(folder, zero, CancellationToken.None, publicKey));
                });
                test("local update binds version helper minimum and supported game fingerprints", () => {
                    var path = Put(store, "0.3.9", archive("0.3.10")); Refused(() => LocalMods.Read(path, CancellationToken.None, publicKey));
                    foreach (var field in new[] { "purpose", "repository", "minimumHelper", "gameHash", "metadataHash" }) {
                        File.WriteAllBytes(path, Package(signer, "0.3.9", changedField: field)); Refused(() => LocalMods.Read(path, CancellationToken.None, publicKey));
                    }
                });
                test("local update refuses content corruption unsafe paths duplicate entries and symlinks", () => {
                    var path = Files.Under(store, "Extended-Hotbar-0.3.9.zip");
                    foreach (var extra in new[] { "../outside.dll", "Mods\\evil.dll", "Mods/file:ads", ReleaseInfo.Dll, "/root.dll", "folder./file" }) {
                        File.WriteAllBytes(path, Package(signer, "0.3.9", extra: extra)); Refused(() => LocalMods.Read(path, CancellationToken.None, publicKey));
                    }
                    File.WriteAllBytes(path, Package(signer, "0.3.9", corrupt: true)); Refused(() => LocalMods.Read(path, CancellationToken.None, publicKey));
                    File.WriteAllBytes(path, Package(signer, "0.3.9", extra: "link", linked: true)); Refused(() => LocalMods.Read(path, CancellationToken.None, publicKey));
                });
                test("local update rejects oversized ZIP input without extracting it", () => {
                    var path = Files.Under(store, "Extended-Hotbar-0.3.9.zip");
                    using (var file = File.Create(path)) file.SetLength(16 * 1024 * 1024 + 1);
                    Refused(() => LocalMods.Read(path, CancellationToken.None, publicKey));
                });
                test("local confirmation rechecks the exact ZIP and cancellation stops reads", () => {
                    var path = Put(store, "0.3.9", archive("0.3.9")); var offer = LocalMods.Read(path, CancellationToken.None, publicKey);
                    Check(LocalMods.Recheck(offer, CancellationToken.None, publicKey).Count == 3);
                    File.WriteAllBytes(path, Package(signer, "0.3.9", extra: "extra.txt")); Refused(() => LocalMods.Recheck(offer, CancellationToken.None, publicKey));
                    using (var cancel = new CancellationTokenSource()) { cancel.Cancel(); Refused(() => LocalMods.Read(path, cancel.Token, publicKey)); Refused(() => LocalMods.Scan(store, zero, cancel.Token, publicKey)); }
                });
            }
        }
        private static string Put(string folder, string version, byte[] bytes)
        { var path = Files.Under(folder, "Extended-Hotbar-" + version + ".zip"); File.WriteAllBytes(path, bytes); return path; }
        // A synthetic future-version envelope, not a real playable release.
        private static byte[] Package(RSACryptoServiceProvider signer, string version, string extra = null, string changedField = null, bool corrupt = false, bool modified = false, bool linked = false)
        {
            var files = ReleaseInfo.Package(); if (modified) files[ReleaseInfo.Notice] = Files.Utf8.GetBytes("different signed notice");
            var fields = new Dictionary<string, object> {
                {"purpose", "ExtendedHotbar.Mod.v1"}, {"repository", Updates.Repository}, {"modVersion", version}, {"minimumHelper", Updates.HelperVersion},
                {"gameHash", ReleaseInfo.GameHash}, {"metadataHash", ReleaseInfo.MetadataHash}, {"files", files.ToDictionary(x => x.Key, x => Files.Hash(x.Value))}
            };
            if (changedField != null) fields[changedField] = changedField == "minimumHelper" ? "99.0.0" : "wrong";
            if (signer != null) {
                var payload = Files.Utf8.GetBytes(new JavaScriptSerializer().Serialize(fields));
                var envelope = new { format = 1, payload = Convert.ToBase64String(payload), signature = Convert.ToBase64String(signer.SignData(payload, CryptoConfig.MapNameToOID("SHA256"))) };
                files.Add(LocalMods.Manifest, Files.Utf8.GetBytes(new JavaScriptSerializer().Serialize(envelope)));
            }
            if (corrupt) files[ReleaseInfo.Dll][0] ^= 1;
            using (var memory = new MemoryStream()) {
                using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, true)) {
                    foreach (var file in files) using (var stream = zip.CreateEntry(file.Key).Open()) stream.Write(file.Value, 0, file.Value.Length);
                    if (extra != null) { var entry = zip.CreateEntry(extra); if (linked) entry.ExternalAttributes = unchecked((int)0xA0000000); using (var stream = entry.Open()) stream.WriteByte(1); }
                }
                return memory.ToArray();
            }
        }
    }
}
