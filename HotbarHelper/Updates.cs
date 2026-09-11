using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
        internal readonly string FileName, Hash, SignatureHash;
        internal readonly int Size, SignatureSize;
        internal byte[] SignedMetadata;
        internal readonly bool Prerelease;
        internal UpdateOffer(Version version, string fileName, string hash, int size, bool prerelease, string signatureHash, int signatureSize)
        { Version = version; FileName = fileName; Hash = hash; Size = size; Prerelease = prerelease; SignatureHash = signatureHash; SignatureSize = signatureSize; }
    }

    // Local package validation/staging only. Repository is a signed product ID,
    // not a feed or endpoint. No network transport is compiled into the helper.
    internal static class Updates
    {
        internal const string HelperVersion = "0.1.13";
        internal const string Repository = "Fre-yin/DNST-Extended-Hotbar";
        internal const string ExeName = "Extended-Hotbar-Helper.exe";
        internal const int MaxPackage = 32 * 1024 * 1024;
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
        internal static Dictionary<string, byte[]> Unpack(byte[] bytes, UpdateOffer offer, CancellationToken token)
        {
            if (bytes == null || bytes.Length != offer.Size || bytes.Length > MaxPackage || Files.Hash(bytes) != offer.Hash) throw Invalid("Local package hash/size mismatch.");
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
                    if (target.Length + count > limit) throw Invalid("Input exceeded the size limit.");
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
            var zone = Files.Utf8.GetBytes("[ZoneTransfer]\r\nZoneId=3\r\n");
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

}
