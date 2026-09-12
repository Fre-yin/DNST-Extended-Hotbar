using HarmonyLib;
#if BEPINEX
using global::Refactor;
#else
using Il2CppRefactor;
#endif

namespace DungeonSettlers10Slots;

[HarmonyPatch(typeof(SaveLoadHelper), nameof(SaveLoadHelper.DeserializeCampaignSaveDataSafely))]
internal static class LoadItemExtension
{
    static void Postfix(string __0, CampaignSaveData __result)
    {
        var state = ItemSlotSaveCodec.Decode(__0);
        ItemSlotStorage.Remember(__result.PlayerUnitsSaveData?.QuickSlots, state);
        if (state.OpaqueExtension != null)
            DungeonSettlers10SlotsMod.Log.Warning("Unbekannte Itemslot-Zusatzdaten: unverändert erhalten; Zusatzslots in diesem Spielstand gesperrt.");
        if (state.SaveBlockReason != null) DungeonSettlers10SlotsMod.Log.Error(state.SaveBlockReason + " Weiterspeichern gesperrt; Originaldatei bleibt erhalten.");
    }
}

[HarmonyPatch(typeof(SaveLoadHelper), nameof(SaveLoadHelper.SaveFile))]
internal static class SaveItemExtension
{
    internal sealed class WriteContext
    {
        internal readonly WriteContext Previous;
        internal string Path;
        internal ItemSlotState State;
        internal bool CampaignPrepared;
        internal bool Failed;
        internal WriteContext(WriteContext previous) { Previous = previous; }
    }
    [ThreadStatic] internal static WriteContext Current;

    static bool Prefix(CampaignSaveData __0, out WriteContext __state, ref bool __result)
    {
        __state = Current = new WriteContext(Current);
        try
        {
            if (!ItemSlotStorage.TrySnapshot(__0.PlayerUnitsSaveData?.QuickSlots, out var state))
                throw new InvalidOperationException("Zuordnung der Itemslot-Zusatzdaten fehlt.");
            if (state.SaveBlockReason != null) throw new InvalidOperationException(state.SaveBlockReason);
            Current.State = state;
            Current.Path = SaveLoadHelper.GetCampaignSavePath(__0.CampaignSaveHeader.SaveName);
            if (string.IsNullOrEmpty(Current.Path)) throw new InvalidOperationException("Kampagnenpfad fehlt.");
            return true;
        }
        catch (Exception ex)
        {
            Current.Failed = true;
            __result = false;
            DungeonSettlers10SlotsMod.Log.Error("Speichern abgebrochen; vorhandene Datei bleibt unverändert: " + ex.Message);
            return false;
        }
    }

    static void Postfix(WriteContext __state, ref bool __result)
    {
        if (__state == null || __state.Failed || !__state.CampaignPrepared) __result = false;
    }

    static void Finalizer(WriteContext __state) => Current = __state?.Previous;
}

    [HarmonyPatch(typeof(SaveLoadHelper), nameof(SaveLoadHelper.WriteJsonFileSafely))]
    internal static class WriteItemExtension
    {
        static bool Prefix(string __0, ref string __1)
        {
            var context = SaveItemExtension.Current;
            if (context == null) return true;
            if (context.Failed) return false; // Also suppress the subsequent save-header write.
            if (!string.Equals(__0, context.Path, StringComparison.OrdinalIgnoreCase)) return true;
            if (context.CampaignPrepared) return true;
            try
            {
                __1 = ItemSlotSaveCodec.Encode(__1, context.State?.Copy());
                context.CampaignPrepared = true;
                return true;
            }
        catch (Exception ex)
        {
            // Managed exceptions must not escape into the IL2CPP trampoline:
            // explicitly skip the native write instead of risking plain JSON.
            context.Failed = true;
            DungeonSettlers10SlotsMod.Log.Error("Itemslot-Speicherung abgebrochen; Datei bleibt unverändert: " + ex.Message);
            return false;
        }
    }

    static void Finalizer(Exception __exception, string __0)
    {
        var context = SaveItemExtension.Current;
        if (__exception != null && context != null && string.Equals(__0, context.Path, StringComparison.OrdinalIgnoreCase))
            context.Failed = true;
    }
}

[HarmonyPatch(typeof(SaveLoadHelper), nameof(SaveLoadHelper.UpsertSaveHeaderCache))]
internal static class GuardItemSaveHeader
{
    [HarmonyPriority(Priority.First)]
    static bool Prefix(ref bool __result)
    {
        var context = SaveItemExtension.Current;
        if (context == null || context.CampaignPrepared && !context.Failed) return true;
        __result = false;
        return false;
    }
}
