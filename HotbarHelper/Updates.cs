using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace ExtendedHotbar.Helper
{
    internal sealed class UpdateOffer
    {
        internal readonly Version Version;
        internal readonly string FileName, Hash, Url, Tag, SignatureUrl, SignatureHash;
        internal readonly int Size, SignatureSize;
        internal byte[] SignedMetadata;
        internal readonly bool Prerelease;
        internal UpdateOffer(Version version, string fileName, string hash, string url, string tag, int size, bool prerelease, string signatureUrl, string signatureHash, int signatureSize)
        { Version = version; FileName = fileName; Hash = hash; Url = url; Tag = tag; Size = size; Prerelease = prerelease; SignatureUrl = signatureUrl; SignatureHash = signatureHash; SignatureSize = signatureSize; }
    }

    // Fixed repository, anonymous HTTPS only. Release assets, never source archives
    // or scripts from release notes. Executable content also requires a signature
    // from the embedded release key, separate from the GitHub account and digest.
    internal static class Updates
    {
        internal const string HelperVersion = "0.1.12";
        // Future activation requires a reviewed build, key recovery and live testing.
        // This is deliberately not configurable through downloaded files or settings.
        internal static readonly bool OnlineEnabled = false;
        internal const string Repository = "Fre-yin/DNST-Extended-Hotbar";
        internal const string Feed = "https://api.github.com/repos/" + Repository + "/releases?per_page=100";
        internal const string ExeName = "Extended-Hotbar-Helper.exe";
        internal const int MaxPackage = 32 * 1024 * 1024;
        private static readonly Regex PackageName = new Regex(@"\AExtended-Hotbar-Helper-((?:0|[1-9][0-9]{0,4})\.(?:0|[1-9][0-9]{0,4})\.(?:0|[1-9][0-9]{0,4}))(-test)?\.zip\z", RegexOptions.CultureInvariant);
        internal static readonly string[] PackageFiles = new[] {
            ExeName, "BITTE ZUERST LESEN.txt", "READ FIRST - ENGLISH.txt", "LICENSE", "LICENSING.txt", "THIRD_PARTY_NOTICES.txt", "Assets/NOTICE.txt",
            "Licenses/HarmonyX-MIT.txt", "Licenses/Il2CppInterop-LGPL-3.0.txt", "Licenses/MelonLoader-Apache-2.0.txt", "Licenses/Newtonsoft.Json-MIT.txt"
        }.Concat(UiText.Codes.Select(x => "Instructions/" + x + ".txt")).ToArray();

        internal static Version ParseVersion(string value)
        {
            Version version;
            if (!Regex.IsMatch(value ?? "", @"\A(?:0|[1-9][0-9]{0,4})\.(?:0|[1-9][0-9]{0,4})\.(?:0|[1-9][0-9]{0,4})\z") || !Version.TryParse(value + ".0", out version)
                || version.Major > 65535 || version.Minor > 65535 || version.Build > 65535) throw Invalid("Invalid helper version.");
            return version;
        }
        internal static HelperFailure Invalid(string message) { return new HelperFailure("errorUpdateData", message); }
        internal static string Handoff(string game, string profile)
        {
            return Convert.ToBase64String(Files.Utf8.GetBytes("{\"game\":" + LosslessJson.Quote(Files.Root(game)) + ",\"profile\":" + LosslessJson.Quote(Files.Root(profile)) + "}"));
        }
        internal static string[] ParseHandoff(string encoded)
        {
            if (encoded.Length > 8192) throw Invalid("Update handoff too large.");
            var root = new LosslessJson(Files.Text(Convert.FromBase64String(encoded))).Root;
            return new[] { Files.Root(root.Get("game").String), Files.Root(root.Get("profile").String) };
        }
        private static bool Boolean(JsonNode node)
        { if (node.Kind != "bool") throw Invalid("Boolean release flag required."); return node.Text == "true"; }

        internal static UpdateOffer Select(string json, Version current, bool includeTests)
        {
            try
            {
                var root = new LosslessJson(json).Root;
                if (root.Kind != "array" || root.Items.Count > 100) throw Invalid("Bounded release list required.");
                UpdateOffer best = null;
                foreach (var release in root.Items)
                {
                    if (Boolean(release.Get("draft"))) continue;
                    bool prerelease = Boolean(release.Get("prerelease"));
                    if (prerelease && !includeTests) continue;
                    var assets = release.Get("assets");
                    if (assets.Kind != "array" || assets.Items.Count > 1000) throw Invalid("Invalid release assets.");
                    foreach (var asset in assets.Items)
                    {
                        string name = asset.Get("name").String; var match = PackageName.Match(name);
                        if (!match.Success || (!includeTests && match.Groups[2].Success)) continue;
                        var version = ParseVersion(match.Groups[1].Value);
                        if (version <= current) continue;
                        string tag = release.Get("tag_name").String;
                        if (!Regex.IsMatch(tag, @"\A[A-Za-z0-9][A-Za-z0-9._-]{0,99}\z")) throw Invalid("Unsupported release tag.");
                        string expected = "https://github.com/" + Repository + "/releases/download/" + tag + "/" + name;
                        if (asset.Get("browser_download_url").String != expected || asset.Get("state").String != "uploaded") throw Invalid("Asset is not a completed download in the pinned repository.");
                        string digest = asset.Get("digest").String;
                        if (!Regex.IsMatch(digest, @"\Asha256:[0-9a-fA-F]{64}\z")) throw Invalid("GitHub SHA256 digest missing or invalid.");
                        int size = asset.Get("size").Integer;
                        if (size <= 0 || size > MaxPackage) throw Invalid("Package size outside supported bounds.");
                        var signed = assets.Items.SingleOrDefault(x => x.Get("name").String == name + ".update.json");
                        if (signed == null || signed.Get("browser_download_url").String != expected + ".update.json" || signed.Get("state").String != "uploaded") throw Invalid("Signed update metadata missing.");
                        string signedDigest = signed.Get("digest").String; int signedSize = signed.Get("size").Integer;
                        if (!Regex.IsMatch(signedDigest, @"\Asha256:[0-9a-fA-F]{64}\z") || signedSize <= 0 || signedSize > 16384) throw Invalid("Invalid signed metadata asset.");
                        var offer = new UpdateOffer(version, name, digest.Substring(7).ToUpperInvariant(), expected, tag, size, prerelease || match.Groups[2].Success, expected + ".update.json", signedDigest.Substring(7).ToUpperInvariant(), signedSize);
                        if (best != null && best.Version == offer.Version && best.Hash != offer.Hash) throw Invalid("Ambiguous packages for the same helper version.");
                        if (best == null || best.Version < offer.Version) best = offer;
                    }
                }
                return best;
            }
            catch (HelperFailure) { throw; }
            catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is InvalidOperationException)
            { throw new HelperFailure("errorUpdateData", "Release metadata could not be validated.", ex); }
        }

        internal static Dictionary<string, byte[]> Unpack(byte[] bytes, UpdateOffer offer, CancellationToken token)
        {
            if (bytes == null || bytes.Length != offer.Size || bytes.Length > MaxPackage || Files.Hash(bytes) != offer.Hash) throw Invalid("Downloaded package hash/size mismatch.");
            var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            using (var memory = new MemoryStream(bytes))
            using (var zip = new ZipArchive(memory, ZipArchiveMode.Read))
            {
                if (zip.Entries.Count != PackageFiles.Length) throw Invalid("Unexpected package entry count.");
                long total = 0;
                foreach (var entry in zip.Entries)
                {
                    token.ThrowIfCancellationRequested();
                    if (!PackageFiles.Contains(entry.FullName) || result.ContainsKey(entry.FullName) || entry.Length <= 0 || entry.Length > (entry.FullName == ExeName ? MaxPackage : 2 * 1024 * 1024)
                        || (entry.ExternalAttributes & 0x400) != 0 || ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000 || (total += entry.Length) > 64 * 1024 * 1024)
                        throw Invalid("Unexpected, duplicate, linked or oversized archive entry.");
                    using (var input = entry.Open()) result.Add(entry.FullName, ReadBounded(input, (int)entry.Length, token));
                    if (result[entry.FullName].Length != entry.Length) throw Invalid("Truncated archive entry.");
                }
            }
            if (PackageFiles.Any(name => !result.ContainsKey(name))) throw Invalid("Required package file missing.");
            return result;
        }
        internal static byte[] ReadBounded(Stream input, int limit, CancellationToken token)
        {
            using (var target = new MemoryStream())
            {
                var buffer = new byte[16384]; int count;
                while (true)
                {
                    token.ThrowIfCancellationRequested(); count = input.Read(buffer, 0, buffer.Length);
                    if (count == 0) break;
                    if (target.Length + count > limit) throw Invalid("Response exceeded the size limit.");
                    target.Write(buffer, 0, count);
                }
                token.ThrowIfCancellationRequested(); return target.ToArray();
            }
        }

        // New version directory only. The active helper and all game/profile files
        // are untouched. Partial/cancelled stages are retained but never launched.
        internal static string Stage(byte[] bytes, UpdateOffer offer, string store, CancellationToken token, string publicKey = null)
        {
            UpdateSignature.Verify(offer, offer.SignedMetadata, publicKey ?? UpdateSignature.PublicKey(), DateTime.UtcNow);
            var files = Unpack(bytes, offer, token);
            string folder = Files.Under(store, offer.Version.ToString(3) + "-" + Guid.NewGuid().ToString("N"));
            if (Directory.Exists(folder)) throw Invalid("Stage already exists.");
            foreach (var pair in files)
            {
                token.ThrowIfCancellationRequested(); string target = Files.Under(folder, pair.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(target)); Files.SafeAncestors(target);
                using (var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { output.Write(pair.Value, 0, pair.Value.Length); output.Flush(true); }
            }
            // Preserve the Internet origin for Windows attachment/SmartScreen checks.
            // Never remove MOTW or change system security settings for an update.
            var zone = Files.Utf8.GetBytes("[ZoneTransfer]\r\nZoneId=3\r\nHostUrl=" + offer.Url + "\r\n");
            using (var output = InternetZone(folder, true))
                output.Write(zone, 0, zone.Length);
            VerifyStage(folder, files, offer); token.ThrowIfCancellationRequested(); return folder;
        }
        internal static void VerifyStage(string folder, Dictionary<string, byte[]> files, UpdateOffer offer)
        {
            foreach (var pair in files)
                if (Files.FileHash(Files.Under(folder, pair.Key)) != Files.Hash(pair.Value)) throw Invalid("Staged file changed.");
            // Reads PE/CLR metadata only; never loads or executes downloaded code.
            var assembly = AssemblyName.GetAssemblyName(Files.Under(folder, ExeName));
            if (assembly.Name != "Extended-Hotbar-Helper" || assembly.Version != offer.Version || assembly.ProcessorArchitecture != ProcessorArchitecture.Amd64)
                throw Invalid("Helper identity, architecture or version differs from the release.");
        }
        internal static void Launch(string folder, byte[] bytes, UpdateOffer offer, Action<string> start, string publicKey = null)
        {
            UpdateSignature.Verify(offer, offer.SignedMetadata, publicKey ?? UpdateSignature.PublicKey(), DateTime.UtcNow);
            VerifyStage(folder, Unpack(bytes, offer, CancellationToken.None), offer);
            start(Files.Under(folder, ExeName));
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
        internal static FileStream InternetZone(string folder, bool write)
        {
            // .NET Framework's string-path FileStream rejects ADS syntax. Open only
            // this fixed NTFS metadata stream through Windows, not arbitrary paths.
            string exe = Files.Under(folder, ExeName);
            if (!File.Exists(exe)) throw Invalid("Cannot mark a missing helper.");
            var handle = CreateFile(exe + ":Zone.Identifier", write ? 0x40000000u : 0x80000000u, 1, IntPtr.Zero, write ? 1u : 3u, 0x80, IntPtr.Zero);
            if (handle.IsInvalid) { int error = Marshal.GetLastWin32Error(); handle.Dispose(); throw new IOException("Internet origin metadata could not be accessed.", new System.ComponentModel.Win32Exception(error)); }
            try { return new FileStream(handle, write ? FileAccess.Write : FileAccess.Read); } catch { handle.Dispose(); throw; }
        }
    }

    internal interface IUpdateTransport { byte[] Get(string url, int limit, CancellationToken token); }
    internal sealed class GitHubTransport : IUpdateTransport
    {
        internal static bool Allowed(Uri uri, bool assetRedirect)
        {
            if (uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0) return false;
            if (uri.AbsoluteUri == Updates.Feed) return true;
            if (uri.Host == "github.com" && uri.AbsolutePath.StartsWith("/" + Updates.Repository + "/releases/download/", StringComparison.Ordinal) && uri.Query.Length == 0) return true;
            return assetRedirect && uri.Host == "release-assets.githubusercontent.com";
        }
        public byte[] Get(string url, int limit, CancellationToken token)
        {
            if (!Updates.OnlineEnabled) throw new HelperFailure("errorUpdateNetwork", "Online updates are disabled in this helper build. No connection attempted.");
            try
            {
                // FrameworkTarget.cs enables modern runtime defaults for both entry
                // assemblies. Keep OS-managed TLS and certificate revocation checks.
                ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
                ServicePointManager.CheckCertificateRevocationList = true;
                var uri = new Uri(url); bool asset = uri.Host == "github.com";
                for (int hop = 0; hop < 4; hop++)
                {
                    token.ThrowIfCancellationRequested();
                    if (!Allowed(uri, asset && hop > 0)) throw Updates.Invalid("Redirect left the allowed HTTPS release hosts.");
                    var request = (HttpWebRequest)WebRequest.Create(uri);
                    request.AllowAutoRedirect = false; request.Timeout = 15000; request.ReadWriteTimeout = 15000;
                    request.UserAgent = "Extended-Hotbar-Helper/" + Updates.HelperVersion;
                    request.UseDefaultCredentials = false; request.Credentials = null; request.CookieContainer = null;
                    request.AutomaticDecompression = DecompressionMethods.None;
                    if (uri.Host == "api.github.com") { request.Accept = "application/vnd.github+json"; request.Headers["X-GitHub-Api-Version"] = "2026-03-10"; }
                    // OS certificate validation stays enabled; no token or profile data is sent.
                    using (token.Register(request.Abort))
                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        int status = (int)response.StatusCode;
                        if (status == 301 || status == 302 || status == 303 || status == 307 || status == 308)
                        { uri = new Uri(uri, response.Headers["Location"] ?? throw Updates.Invalid("Missing redirect location.")); continue; }
                        if (status != 200 || response.ContentLength > limit) throw Updates.Invalid("Unexpected HTTP response or size.");
                        using (var input = response.GetResponseStream()) return Updates.ReadBounded(input, limit, token);
                    }
                }
                throw Updates.Invalid("Too many redirects.");
            }
            catch (WebException ex)
            {
                if (token.IsCancellationRequested) throw new OperationCanceledException(token);
                using (var response = ex.Response as HttpWebResponse)
                {
                    int status = response == null ? 0 : (int)response.StatusCode;
                    throw new HelperFailure(status == 404 ? "errorUpdatePrivate" : status == 403 || status == 429 ? "errorUpdateRate" : "errorUpdateNetwork", "GitHub request failed (HTTP " + status + ").", ex);
                }
            }
            catch (IOException ex) when (!(ex is HelperFailure))
            { if (token.IsCancellationRequested) throw new OperationCanceledException(token); throw new HelperFailure("errorUpdateNetwork", "Update response interrupted.", ex); }
        }
    }

    internal sealed class UpdateClient
    {
        private readonly IUpdateTransport transport;
        private readonly string publicKey;
        internal UpdateClient(IUpdateTransport transport, string publicKey = null) { this.transport = transport; this.publicKey = publicKey ?? UpdateSignature.PublicKey(); }
        internal UpdateOffer Check(Version current, bool tests, CancellationToken token)
        {
            var offer = Updates.Select(Files.Text(transport.Get(Updates.Feed, 8 * 1024 * 1024, token)), current, tests);
            if (offer == null) return null;
            var signed = transport.Get(offer.SignatureUrl, offer.SignatureSize, token);
            UpdateSignature.Verify(offer, signed, publicKey, DateTime.UtcNow); offer.SignedMetadata = signed; return offer;
        }
        internal byte[] Download(UpdateOffer offer, CancellationToken token)
        {
            UpdateSignature.Verify(offer, offer.SignedMetadata, publicKey, DateTime.UtcNow);
            var bytes = transport.Get(offer.Url, offer.Size, token);
            Updates.Unpack(bytes, offer, token); return bytes;
        }
    }

    internal sealed class UpdatePreferences
    {
        internal bool Automatic = true, IncludeTests = true;
        internal static UpdatePreferences Load(string path)
        {
            var value = new UpdatePreferences();
            if (!File.Exists(path)) return value;
            try
            {
                if (new FileInfo(path).Length > 2048) throw new FormatException("Preferences exceed size limit.");
                var node = new LosslessJson(Files.Text(Files.Read(path))).Root;
                if (node.Get("automatic").Kind != "bool" || node.Get("includeTests").Kind != "bool") throw new FormatException();
                value.Automatic = node.Get("automatic").Text == "true"; value.IncludeTests = node.Get("includeTests").Text == "true";
            }
            catch { value.Automatic = false; value.IncludeTests = false; } // Fail closed on damaged preferences.
            return value;
        }
        internal void Save(string path)
        { Files.Atomic(path, Files.Utf8.GetBytes("{\"automatic\":" + (Automatic ? "true" : "false") + ",\"includeTests\":" + (IncludeTests ? "true" : "false") + "}")); }
    }
}
