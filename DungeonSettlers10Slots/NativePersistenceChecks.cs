using HarmonyLib;
using Il2CppRefactor;
using Il2CppRefactor.Main;

namespace DungeonSettlers10Slots;

// Explicit audit only. Writes a fresh temporary directory, never campaign files
// or the shared header cache. No audit hooks remain installed afterwards.
internal static class NativePersistenceChecks
{
    const string FixtureArgument = "--ds-audit-save-fixture=";
    static string targetPath;
    static string saveName;
    static bool corruptWrite;
    static int headerCalls;

    internal static void RunIfRequested()
    {
        var argument = Environment.GetCommandLineArgs().SingleOrDefault(a => a.StartsWith(FixtureArgument, StringComparison.Ordinal));
        if (argument == null) return;
        var fixture = File.ReadAllText(argument[FixtureArgument.Length..]);
        var directory = Path.Combine(Path.GetTempPath(), "DSHotbarAudit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        targetPath = Path.Combine(directory, "campaign.json");
        saveName = "DSHotbarAudit-" + Guid.NewGuid().ToString("N");
        var harmony = new HarmonyLib.Harmony("Danny.DungeonSettlers10Slots.PersistenceAudit");
        try
        {
            harmony.Patch(AccessTools.Method(typeof(SaveLoadHelper), nameof(SaveLoadHelper.GetCampaignSavePath)), prefix: Method(nameof(RedirectPath)));
            harmony.Patch(AccessTools.Method(typeof(SaveLoadHelper), nameof(SaveLoadHelper.UpsertSaveHeaderCache)), prefix: Method(nameof(SkipSharedHeaderCache)));
            var corrupt = Method(nameof(InjectInvalidJson)); corrupt.priority = Priority.First;
            harmony.Patch(AccessTools.Method(typeof(SaveLoadHelper), nameof(SaveLoadHelper.WriteJsonFileSafely)), prefix: corrupt);
            var settings = SaveLoadHelper.CreateCampaignSaveLoadSerializerSettings();
            var save = SaveLoadHelper.DeserializeCampaignSaveDataSafely(fixture, settings, targetPath);
            var header = save.CampaignSaveHeader; header.SaveName = saveName; save.CampaignSaveHeader = header;
            var expected = ItemSlotSaveCodec.Decode(fixture);
            Require(expected.Units.Count > 0 && expected.SaveBlockReason == null, "fixture includes extras");
            Require(SaveLoadHelper.SaveFile(save), "native SaveFile returns success");
            Require(File.Exists(targetPath) && headerCalls == 1 && SaveItemExtension.Current == null, "campaign written, shared cache untouched, scope restored");
            var written = File.ReadAllText(targetPath);
            Require(ItemSlotSaveCodec.Encode("{}", ItemSlotSaveCodec.Decode(written)) == ItemSlotSaveCodec.Encode("{}", expected), "disk JSON preserves all extra assignments");
            var restored = SaveLoadHelper.DeserializeCampaignSaveDataSafely(written, settings, targetPath);
            var container = new UnitQuickSlotContainer();
            container.Deserialize(restored.PlayerUnitsSaveData.QuickSlots);
            foreach (var unit in expected.Units)
            {
                var guid = Il2CppSystem.Guid.Parse(unit.Key);
                for (var i = 1; i <= 2; i++)
                {
                    using var scope = new ItemSlotContext(i);
                    Require(container.GetItemQuickSlotKey(guid) == unit.Value[i - 1], "disk -> native deserialize -> GUID item " + i);
                }
            }
            // Compare native skill/item-one records independently of the addon.
            var before = save.PlayerUnitsSaveData.QuickSlots.GetEnumerator();
            while (before.MoveNext())
            {
                var entry = before.Current;
                var after = restored.PlayerUnitsSaveData.QuickSlots[entry.Key];
                Require(after.ItemQuickSlotKey == entry.Value.ItemQuickSlotKey, "native item-one preserved");
                Require(after.QuickSlots.Length == entry.Value.QuickSlots.Length, "native skill length preserved");
                for (var i = 0; i < after.QuickSlots.Length; i++) Require(after.QuickSlots[i] == entry.Value.QuickSlots[i], "native skill value preserved");
            }
            corruptWrite = true;
            Require(!SaveLoadHelper.SaveFile(save), "encoding failure reports save failure");
            Require(File.ReadAllText(targetPath) == written && headerCalls == 1 && SaveItemExtension.Current == null, "encoding failure preserves previous file and suppresses header update");
            corruptWrite = false;
            ItemSlotStorage.ClearAll();
            Require(!SaveLoadHelper.SaveFile(save) && File.ReadAllText(targetPath) == written, "missing snapshot cannot overwrite previous file");
            container.Deserialize(save.PlayerUnitsSaveData.QuickSlots);
            Require(ItemSlotStorage.State(container).SaveBlockReason != null, "missing load association remains save-blocked");
            // A new decode must recover normally after failures/eviction.
            restored = SaveLoadHelper.DeserializeCampaignSaveDataSafely(written, settings, targetPath);
            Require(SaveLoadHelper.SaveFile(restored) && SaveItemExtension.Current == null, "retry after reload succeeds");
            DungeonSettlers10SlotsMod.Log.Msg("Native persistence audit PASS: actual SaveFile/atomic writer/disk/native reload/GUID items, original skills/item retained, encoding failure/missing snapshot leave file unchanged, retry and scope cleanup. Evidence: " + directory);
        }
        finally
        {
            harmony.UnpatchSelf(); targetPath = null; saveName = null; corruptWrite = false; headerCalls = 0;
            ItemSlotStorage.ClearAll();
        }
    }

    static HarmonyMethod Method(string name) => new(AccessTools.Method(typeof(NativePersistenceChecks), name));
    static bool RedirectPath(string __0, ref string __result)
    {
        if (__0 != saveName) return true;
        __result = targetPath; return false;
    }
    static bool SkipSharedHeaderCache(bool __runOriginal, ref bool __result)
    {
        if (__runOriginal) { headerCalls++; __result = true; }
        return false;
    }
    static void InjectInvalidJson(string __0, ref string __1)
    {
        if (corruptWrite && __0 == targetPath) __1 = "{invalid-native-json";
    }
    static void Require(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("Native persistence audit FAILED: " + name);
    }
}
