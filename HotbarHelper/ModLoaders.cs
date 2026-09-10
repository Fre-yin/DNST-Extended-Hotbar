using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ExtendedHotbar.Helper
{
    // One reviewed layout per loader. No arbitrary paths, DLLs, or loader downloads.
    internal sealed class ModLoaderProfile
    {
        internal readonly string Id, Label, Assembly, Dll, Asset, Notice, Core, PackageHash, Resource, FileName, Purpose, LogPath;
        internal readonly string[] Owned, Required;
        internal readonly Regex PackageName;
        internal ModLoaderProfile(string id, string label, string assembly, string root, string oldDll,
            string core, string hash, string resource, string fileName, string purpose, string logPath, string pattern)
        {
            Id = id; Label = label; Assembly = assembly; Dll = root + "/" + assembly + ".dll";
            Asset = root + "/DungeonSettlers10SlotsAssets/SkillFrame__sharedassets0_mod_4898.png";
            Notice = root + "/DungeonSettlers10SlotsAssets/NOTICE.txt";
            Core = core; PackageHash = hash; Resource = resource; FileName = fileName; Purpose = purpose; LogPath = logPath;
            Required = new[] { Dll, Asset, Notice };
            Owned = oldDll == null ? Required : new[] { Dll, oldDll, Asset, Notice };
            PackageName = new Regex(pattern, RegexOptions.CultureInvariant);
        }
        public override string ToString() { return Label; }
        internal void ValidateSelection(string game, bool requireLoader)
        {
            var other = this == ModLoaders.Melon ? ModLoaders.BepInEx : ModLoaders.Melon;
            // Reject mixed/incorrect installations, including disabled remnants. Never switch loaders by deleting them.
            if (ModLoaders.HasFootprint(game, other))
                throw new HelperFailure("errorLoaderMix", "The selected loader does not match this folder, or both loaders are present. Use a separate game copy.");
            if (requireLoader && !File.Exists(Files.Under(game, Core)))
                throw new HelperFailure("errorLoader", "Required loader is missing: " + Label);
        }
    }

    internal static class ModLoaders
    {
        internal static readonly ModLoaderProfile Melon = new ModLoaderProfile("melon", "MelonLoader 0.7.3", "DungeonSettlers10Slots", "Mods",
            "Mods/DungeonSettlers12Slots.dll", "MelonLoader/net6/MelonLoader.dll", ReleaseInfo.PackageHash, "HotbarPackage.zip",
            "Extended-Hotbar-" + ReleaseInfo.ModVersion + ".zip", "ExtendedHotbar.Mod.v1", "MelonLoader/Latest.log",
            @"\AExtended-Hotbar-([0-9]+\.[0-9]+\.[0-9]+)(?:-mit-Testspielstand)?(?: \([1-9][0-9]{0,2}\))?\.zip\z");
        internal static readonly ModLoaderProfile BepInEx = new ModLoaderProfile("bepinex", "BepInEx 6 · IL2CPP (preview)", "DungeonSettlersHotbar.BepInEx", "BepInEx/plugins/ExtendedHotbar",
            null, "BepInEx/core/BepInEx.Unity.IL2CPP.dll", "77193451CD045D03A50E24A80F2D0582E178F335D90926EDF9AAF9E6B3880547", "BepInExPackage.zip",
            "Extended-Hotbar-BepInEx-" + ReleaseInfo.ModVersion + "-bepinex.1.zip", "ExtendedHotbar.Mod.BepInEx.v1", "BepInEx/LogOutput.log",
            @"\AExtended-Hotbar-BepInEx-([0-9]+\.[0-9]+\.[0-9]+)-bepinex\.[1-9][0-9]*(?: \([1-9][0-9]{0,2}\))?\.zip\z");
        internal static readonly ModLoaderProfile[] All = { Melon, BepInEx };
        internal static bool Owns(string relative) { return All.Any(x => x.Owned.Contains(relative)); }
        internal static bool HasFootprint(string game, ModLoaderProfile loader)
        {
            return Directory.Exists(Files.Under(game, loader == Melon ? "MelonLoader" : "BepInEx"))
                || loader.Owned.Any(x => File.Exists(Files.Under(game, x)))
                || File.Exists(Files.Under(game, loader == Melon ? "version.dll" : "winhttp.dll"));
        }
        internal static ModLoaderProfile Detect(string game)
        {
            var found = All.Where(x => HasFootprint(game, x)).ToArray();
            return found.Length == 1 ? found[0] : null; // Missing or mixed: never guess.
        }
    }
}
