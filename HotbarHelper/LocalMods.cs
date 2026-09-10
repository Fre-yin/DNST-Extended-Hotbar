using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace ExtendedHotbar.Helper
{
    internal sealed class LocalModPackage
    {
        internal string Path, Hash;
        internal Version Version;
        internal ModLoaderProfile Loader;
        internal Dictionary<string, byte[]> Payload;
    }

    // Local input only: no HTTP, automatic execution, recursive search or ZIP extraction.
    internal static class LocalMods
    {
        internal const string Manifest = "ExtendedHotbar.update.json";
        private const int MaxZip = 16 * 1024 * 1024;
        [DllImport("shell32.dll")]
        private static extern int SHGetKnownFolderPath(ref Guid id, uint flags, IntPtr token, out IntPtr path);
        internal static string Downloads()
        {
            var id = new Guid("374DE290-123F-4565-9164-39C4925E467B"); IntPtr pointer;
            int result = SHGetKnownFolderPath(ref id, 0, IntPtr.Zero, out pointer);
            try { Marshal.ThrowExceptionForHR(result); return Files.Root(Marshal.PtrToStringUni(pointer)); }
            finally { if (pointer != IntPtr.Zero) Marshal.FreeCoTaskMem(pointer); }
        }
        internal static Version FileVersion(string path, ModLoaderProfile loader = null)
        {
            var match = (loader ?? ModLoaders.Melon).PackageName.Match(System.IO.Path.GetFileName(path));
            return match.Success ? Updates.ParseVersion(match.Groups[1].Value) : null;
        }
        internal static Version Installed(string game, ModLoaderProfile loader = null)
        {
            loader = loader ?? ModLoaders.Melon;
            var path = Files.Under(game, loader.Dll);
            if (!File.Exists(path)) return Updates.ParseVersion("0.0.0");
            var identity = AssemblyName.GetAssemblyName(path); // Metadata only, never load DLL code.
            if (identity.Name != loader.Assembly) throw new HelperFailure("errorForeign", "Foreign DLL in the hotbar slot.");
            return Updates.ParseVersion(identity.Version.ToString(3));
        }
        internal static LocalModPackage Scan(string downloads, Version installed, CancellationToken token, string publicKey = null, ModLoaderProfile loader = null)
        {
            loader = loader ?? ModLoaders.Melon;
            downloads = Files.Root(downloads);
            if (!Directory.Exists(downloads)) return null;
            var minimum = Updates.ParseVersion(ReleaseInfo.ModVersion);
            if (installed > minimum) minimum = installed;
            var candidates = new List<KeyValuePair<string, Version>>(); int count = 0;
            foreach (var path in Directory.EnumerateFiles(downloads, "*", SearchOption.TopDirectoryOnly))
            {
                token.ThrowIfCancellationRequested();
                if (++count > 10000) throw new HelperFailure("errorLocalPackage", "Downloads exceeds the 10000-file search limit.");
                var version = FileVersion(path, loader);
                if (version != null && version >= minimum) candidates.Add(new KeyValuePair<string, Version>(path, version));
            }
            LocalModPackage best = null;
            foreach (var candidate in candidates.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.Ordinal))
            {
                if (best != null && candidate.Value < best.Version) break;
                var value = Read(candidate.Key, token, publicKey, loader);
                // Two equivalent downloads are harmless; conflicting release contents are not.
                if (best != null && best.Payload.Any(x => Files.Hash(x.Value) != Files.Hash(value.Payload[x.Key])))
                    throw new HelperFailure("errorLocalPackage", "Conflicting local packages have the same mod version.");
                best = best ?? value;
            }
            return best;
        }
        internal static LocalModPackage Read(string path, CancellationToken token, string publicKey = null, ModLoaderProfile loader = null)
        {
            try
            {
                loader = loader ?? ModLoaders.Melon;
                path = System.IO.Path.GetFullPath(path); Files.SafeAncestors(path);
                var version = FileVersion(path, loader);
                if (version == null) throw new FormatException("Not a supported mod ZIP filename.");
                byte[] bytes;
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    bytes = Updates.ReadBounded(file, MaxZip, token);
                var hash = Files.Hash(bytes);
                if (hash == loader.PackageHash && version == Updates.ParseVersion(ReleaseInfo.ModVersion))
                    return new LocalModPackage { Loader = loader, Path = path, Version = version, Hash = hash, Payload = ReleaseInfo.ReadPackage(bytes, loader) };
                var wanted = loader.Required;
                var payload = new Dictionary<string, byte[]>(StringComparer.Ordinal);
                byte[] envelope = null;
                using (var memory = new MemoryStream(bytes))
                using (var zip = new ZipArchive(memory, ZipArchiveMode.Read))
                {
                    if (zip.Entries.Count > 128) throw new FormatException("Too many ZIP entries.");
                    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase); long expanded = 0;
                    foreach (var entry in zip.Entries)
                    {
                        token.ThrowIfCancellationRequested();
                        var parts = entry.FullName.Split('/');
                        if (!names.Add(entry.FullName) || parts.Any(x => x.Length == 0 || x == "." || x == ".." || x.EndsWith(".") || x.EndsWith(" "))
                            || entry.FullName.IndexOfAny(new[] { '\\', ':', '\0' }) >= 0
                            || ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000 || (entry.ExternalAttributes & 0x400) != 0
                            || entry.Length > 4 * 1024 * 1024 || (expanded += entry.Length) > 32 * 1024 * 1024)
                            throw new FormatException("Unsafe ZIP layout or expansion.");
                        if (!wanted.Contains(entry.FullName) && entry.FullName != Manifest) continue;
                        using (var input = entry.Open())
                        {
                            var content = Updates.ReadBounded(input, entry.FullName == Manifest ? 16384 : 4 * 1024 * 1024, token);
                            if (content.Length != entry.Length) throw new FormatException("Truncated ZIP entry.");
                            if (entry.FullName == Manifest) envelope = content; else payload.Add(entry.FullName, content);
                        }
                    }
                }
                if (version == Updates.ParseVersion(ReleaseInfo.ModVersion) && payload.Count == 3)
                {
                    var bundled = ReleaseInfo.Package(loader);
                    if (wanted.All(x => payload.ContainsKey(x) && Files.Hash(payload[x]) == Files.Hash(bundled[x])))
                        return new LocalModPackage { Loader = loader, Path = path, Version = version, Hash = hash, Payload = payload };
                }
                var manifest = new LosslessJson(Files.Text(UpdateSignature.VerifyPayload(envelope, publicKey ?? UpdateSignature.PublicKey()))).Root;
                if (manifest.Members.Count != 7 || manifest.Get("purpose").String != loader.Purpose || manifest.Get("repository").String != Updates.Repository
                    || manifest.Get("modVersion").String != version.ToString(3) || payload.Count != 3)
                    throw new FormatException("Signed mod identity is not bound to these files.");
                // A new game build or save format still needs a reviewed helper. A local
                // package never overrides the helper's build-safety policy.
                if (Updates.ParseVersion(manifest.Get("minimumHelper").String) > Updates.ParseVersion(Updates.HelperVersion)
                    || manifest.Get("gameHash").String != ReleaseInfo.GameHash || manifest.Get("metadataHash").String != ReleaseInfo.MetadataHash)
                    throw new HelperFailure("errorLocalHelper", "Package requires a newer helper or another supported game build.");
                var hashes = manifest.Get("files");
                if (hashes.Kind != "object" || hashes.Members.Count != 3 || wanted.Any(x => hashes.Get(x).String != Files.Hash(payload[x])))
                    throw new FormatException("Installed file hashes do not match the publisher signature.");
                return new LocalModPackage { Loader = loader, Path = path, Version = version, Hash = hash, Payload = payload };
            }
            catch (OperationCanceledException) { throw; }
            catch (HelperFailure ex) when (ex.Code == "errorLocalHelper") { throw; }
            catch (Exception ex) when (ex is IOException || ex is FormatException || ex is ArgumentException || ex is System.Security.Cryptography.CryptographicException || ex is System.Xml.XmlException)
            { throw new HelperFailure("errorLocalPackage", "Local mod package refused: " + System.IO.Path.GetFileName(path), ex); }
        }
        internal static Dictionary<string, byte[]> Recheck(LocalModPackage offer, CancellationToken token, string publicKey = null)
        {
            var current = Read(offer.Path, token, publicKey, offer.Loader);
            if (current.Hash != offer.Hash) throw new HelperFailure("errorLocalPackage", "Package changed after selection.");
            return current.Payload;
        }
    }
}
