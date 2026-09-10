using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace ExtendedHotbar.Helper
{
    internal static class ReleaseInfo
    {
        internal const string ModVersion = "0.3.8";
        internal const string GameHash = "B0CD8B641D551019B82C0AF3DDE1532D6FF7BA155B20B2D3936742924B42DB2A";
        internal const string MetadataHash = "CE84EC266501C8DF4A23B31D50D8B413C82DAE49A062BC623B3440E8A3F5A23F";
        internal const string PackageHash = "D7824BD6B31E3760EBC803111261B75EC5ADF60FAE87B7AAC444DF9B2993F544";
        internal const string Dll = "Mods/DungeonSettlers10Slots.dll";
        internal const string OldDll = "Mods/DungeonSettlers12Slots.dll";
        internal const string Asset = "Mods/DungeonSettlers10SlotsAssets/SkillFrame__sharedassets0_mod_4898.png";
        internal const string Notice = "Mods/DungeonSettlers10SlotsAssets/NOTICE.txt";
        internal static readonly string[] Owned = { Dll, OldDll, Asset, Notice };
        internal static Dictionary<string, byte[]> Package(ModLoaderProfile loader = null)
        {
            return ReadPackage(PackageBytes(loader), loader);
        }
        internal static byte[] PackageBytes(ModLoaderProfile loader = null)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream((loader ?? ModLoaders.Melon).Resource))
            {
                if (stream == null) throw new HelperFailure("errorPackage", "Eingebautes Mod-Paket fehlt.");
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory); var bytes = memory.ToArray();
                    if (Files.Hash(bytes) != (loader ?? ModLoaders.Melon).PackageHash) throw new HelperFailure("errorPackage", "Prüfsumme des Mod-Pakets stimmt nicht.");
                    return bytes;
                }
            }
        }
        // Explicit export only: never extract or import saves, overwrite a file,
        // or modify game/profile paths as a side effect of installation/startup.
        internal static void ExportPackage(string path, ModLoaderProfile loader = null)
        {
            var folder = Files.Root(Path.GetDirectoryName(path));
            var target = Files.Under(folder, Path.GetFileName(path));
            if (!target.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) throw new HelperFailure("errorPath", "ZIP-Datei erwartet.");
            if (File.Exists(target) || Directory.Exists(target)) throw new HelperFailure("errorExportExists", "Bitte einen neuen Dateinamen wählen.");
            var bytes = PackageBytes(loader);
            var temporary = Files.Under(folder, ".eh-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                Files.SafeAncestors(target);
                File.Move(temporary, target); // Fails safely if the destination appeared meanwhile.
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        internal static Dictionary<string, byte[]> ReadPackage(byte[] bytes, ModLoaderProfile loader = null)
        {
            if (Files.Hash(bytes) != (loader ?? ModLoaders.Melon).PackageHash) throw new HelperFailure("errorPackage", "Prüfsumme des Mod-Pakets stimmt nicht.");
            using (var memory = new MemoryStream(bytes))
            using (var zip = new ZipArchive(memory, ZipArchiveMode.Read))
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
                foreach (var entry in zip.Entries)
                {
                    if (!names.Add(entry.FullName) || entry.FullName.Contains("..") || entry.Length > 4 * 1024 * 1024) throw new HelperFailure("errorPackage", "Ungültiges ZIP.");
                    if (!(loader ?? ModLoaders.Melon).Required.Contains(entry.FullName)) continue;
                    using (var input = entry.Open()) using (var target = new MemoryStream()) { input.CopyTo(target); result.Add(entry.FullName, target.ToArray()); }
                }
                if (result.Count != 3 || (loader ?? ModLoaders.Melon).Required.Any(x => !result.ContainsKey(x))) throw new HelperFailure("errorPackage", "Installierbare Hotbar-Dateien fehlen.");
                return result;
            }
        }
    }

}
