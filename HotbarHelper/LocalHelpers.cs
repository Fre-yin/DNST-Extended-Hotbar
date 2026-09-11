using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ExtendedHotbar.Helper
{
    internal sealed class LocalHelperPackage
    {
        internal string Path;
        internal UpdateOffer Offer;
    }
    internal sealed class LocalUpdateResult
    {
        internal LocalHelperPackage Helper;
        internal LocalModPackage Mod;
        internal Version Baseline;
    }
    internal static class LocalUpdates
    {
        internal static LocalUpdateResult Scan(string folder, string game, bool helperOnly, CancellationToken token, string publicKey = null, Version current = null, ModLoaderProfile loader = null)
        {
            var result = new LocalUpdateResult { Helper = LocalHelpers.Scan(folder, token, publicKey, current) };
            // A helper-only request must not depend on a valid game, its installed
            // DLL or any mod archive (including damaged or incompatible archives).
            if (helperOnly || result.Helper != null) return result;
            var installed = string.IsNullOrWhiteSpace(game) ? Updates.ParseVersion("0.0.0") : LocalMods.Installed(game, loader);
            result.Baseline = Updates.ParseVersion(ReleaseInfo.ModVersion);
            if (installed > result.Baseline) result.Baseline = installed;
            result.Mod = LocalMods.Scan(folder, installed, token, publicKey, loader);
            return result;
        }
        internal static string StartArguments(bool helperOnly, string game, string profile)
        {
            // No install handoff for helper-only updates. Even absent/invalid game
            // settings must not turn this request into a mod operation or block it.
            return helperOnly || string.IsNullOrWhiteSpace(game) ? "" : "--update-install " + Updates.Handoff(game, profile);
        }
        internal static string NoticeKey(string state)
        { return state == "updateAvailable" || state == "localHelperFound" ? state : null; }
    }
    internal static class LocalHelpers
    {
        private static readonly Regex Name = new Regex(@"\AExtended-Hotbar-Helper-([0-9]+\.[0-9]+\.[0-9]+)(-test)?\.zip\z", RegexOptions.CultureInvariant);
        internal static LocalHelperPackage Scan(string folder, CancellationToken token, string publicKey = null, Version current = null)
        {
            folder = Files.Root(folder); if (!Directory.Exists(folder)) return null;
            current = current ?? Updates.ParseVersion(Updates.HelperVersion);
            var candidates = new System.Collections.Generic.List<string>(); int count = 0;
            foreach (var path in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly))
            {
                token.ThrowIfCancellationRequested();
                if (++count > 10000) throw new HelperFailure("errorLocalHelperPackage", "Downloads search limit exceeded.");
                var match = Name.Match(System.IO.Path.GetFileName(path));
                if (match.Success && Updates.ParseVersion(match.Groups[1].Value) > current) candidates.Add(path);
            }
            LocalHelperPackage best = null;
            foreach (var path in candidates.OrderByDescending(x => Updates.ParseVersion(Name.Match(System.IO.Path.GetFileName(x)).Groups[1].Value)))
            {
                if (best != null && Updates.ParseVersion(Name.Match(System.IO.Path.GetFileName(path)).Groups[1].Value) < best.Offer.Version) break;
                var item = Read(path, token, publicKey);
                if (best != null && (best.Offer.Hash != item.Offer.Hash || best.Offer.Prerelease != item.Offer.Prerelease))
                    throw new HelperFailure("errorLocalHelperPackage", "Conflicting local helper packages share the highest version.");
                best = best ?? item;
            }
            return best;
        }
        internal static LocalHelperPackage Read(string path, CancellationToken token, string publicKey = null)
        {
            try
            {
                path = System.IO.Path.GetFullPath(path); Files.SafeAncestors(path); Files.SafeAncestors(path + ".update.json");
                var match = Name.Match(System.IO.Path.GetFileName(path));
                if (!match.Success) throw new FormatException("Unsupported helper filename.");
                byte[] signature;
                using (var input = File.OpenRead(path + ".update.json")) signature = Updates.ReadBounded(input, 16384, token);
                var fields = new LosslessJson(Files.Text(UpdateSignature.VerifyPayload(signature, publicKey ?? UpdateSignature.PublicKey()))).Root;
                var version = Updates.ParseVersion(match.Groups[1].Value);
                var filename = System.IO.Path.GetFileName(path);
                if (fields.Get("fileName").String != filename || fields.Get("helperVersion").String != version.ToString(3)
                    || fields.Get("prerelease").Kind != "bool" || (fields.Get("prerelease").Text == "true") != match.Groups[2].Success
                    || fields.Get("size").Integer <= 0 || fields.Get("size").Integer > Updates.MaxPackage)
                    throw new FormatException("Local helper filename/channel/size does not match its signed metadata.");
                var offer = new UpdateOffer(version, filename, fields.Get("sha256").String, fields.Get("size").Integer,
                    match.Groups[2].Success, Files.Hash(signature), signature.Length) { SignedMetadata = signature };
                UpdateSignature.Verify(offer, signature, publicKey ?? UpdateSignature.PublicKey(), DateTime.UtcNow);
                var result = new LocalHelperPackage { Path = path, Offer = offer };
                ReadBytes(result, token, publicKey); return result;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is IOException || ex is FormatException || ex is ArgumentException || ex is System.Security.Cryptography.CryptographicException || ex is System.Xml.XmlException)
            { throw new HelperFailure("errorLocalHelperPackage", "Local helper or matching signature refused: " + System.IO.Path.GetFileName(path), ex); }
        }
        internal static byte[] ReadBytes(LocalHelperPackage package, CancellationToken token, string publicKey = null)
        {
            UpdateSignature.Verify(package.Offer, package.Offer.SignedMetadata, publicKey ?? UpdateSignature.PublicKey(), DateTime.UtcNow);
            Files.SafeAncestors(package.Path); byte[] bytes;
            using (var input = new FileStream(package.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
                bytes = Updates.ReadBounded(input, Updates.MaxPackage, token);
            Updates.Unpack(bytes, package.Offer, token); return bytes;
        }
    }
}
