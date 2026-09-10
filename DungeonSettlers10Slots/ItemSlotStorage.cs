using HarmonyLib;
#if BEPINEX
using global::Refactor.Main;
#else
using Il2CppRefactor.Main;
#endif
using Guid = Il2CppSystem.Guid;
#if BEPINEX
using SaveDictionary = Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Guid, global::Refactor.Main.QuickSlotSaveData>;
#else
using SaveDictionary = Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Guid, Il2CppRefactor.Main.QuickSlotSaveData>;
#endif

namespace DungeonSettlers10Slots;

internal static class ItemSlotStorage
{
    static readonly Dictionary<IntPtr, (UnitQuickSlotContainer Owner, ItemSlotState State)> containers = new();
    static readonly BoundedSnapshotCache<IntPtr, (SaveDictionary Owner, ItemSlotState State)> snapshots = new(64);

    internal static ItemSlotState State(UnitQuickSlotContainer container)
    {
        if (!containers.TryGetValue(container.Pointer, out var entry))
            containers.Add(container.Pointer, entry = (container, new ItemSlotState()));
        return entry.State;
    }

    internal static string Read(UnitQuickSlotContainer container, Guid guid, int slot)
    {
        if (!ItemSlotContext.IsExtra(slot) || !container._unitQuickSlot.ContainsKey(guid)) return "";
        return State(container).Units.TryGetValue(guid.ToString(), out var keys) ? keys[slot - 1] : "";
    }

    internal static void Write(UnitQuickSlotContainer container, Guid guid, int slot, string key)
    {
        if (!ItemSlotContext.IsExtra(slot) || !container._unitQuickSlot.ContainsKey(guid)) return;
        var state = State(container);
        if (state.OpaqueExtension != null || state.SaveBlockReason != null) return; // Preserve unsupported mod data, read-only.
        var id = guid.ToString();
        if (!state.Units.TryGetValue(id, out var keys)) state.Units.Add(id, keys = new[] { "", "" });
        keys[slot - 1] = key ?? "";
        if (keys.All(string.IsNullOrEmpty)) state.Units.Remove(id);
    }

    internal static void Remember(SaveDictionary dictionary, ItemSlotState state)
    {
        if (dictionary == null) return;
        // Strong references prevent native pointer reuse. Save/load is synchronous;
        // keep a bounded set also covering the menu's recent preview deserializations.
        snapshots.Put(dictionary.Pointer, (dictionary, state.Copy()));
    }

    internal static bool TrySnapshot(SaveDictionary dictionary, out ItemSlotState state)
    {
        state = null;
        if (dictionary == null || !snapshots.TryGetValue(dictionary.Pointer, out var entry)) return false;
        state = entry.State.Copy();
        return true;
    }

    internal static void Loaded(UnitQuickSlotContainer container, SaveDictionary dictionary)
    {
        TrySnapshot(dictionary, out var loaded);
        containers[container.Pointer] = (container, loaded ?? new ItemSlotState
        {
            SaveBlockReason = "Zuordnung der geladenen Itemslot-Zusatzdaten fehlt; Spielstand bitte erneut laden."
        });
    }

    internal static void ClearContainers() => containers.Clear();
    internal static void ClearAll() { containers.Clear(); snapshots.Clear(); }
    internal static void Forget(UnitQuickSlotContainer container) => containers.Remove(container.Pointer);
}

internal sealed class ItemSlotContext : IDisposable
{
    [ThreadStatic] internal static int Current;
    readonly int previous;
    bool disposed;
    internal static bool IsExtra(int slot) => slot is 1 or 2;
    internal ItemSlotContext(int slot) { previous = Current; Current = slot; }
    public void Dispose() { if (disposed) return; Current = previous; disposed = true; }
}

[HarmonyPatch(typeof(UnitQuickSlotContainer), nameof(UnitQuickSlotContainer.GetItemQuickSlotKey))]
internal static class ReadExtraItem
{
    static bool Prefix(UnitQuickSlotContainer __instance, Guid __0, ref string __result)
    {
        if (!ItemSlotContext.IsExtra(ItemSlotContext.Current)) return true;
        __result = ItemSlotStorage.Read(__instance, __0, ItemSlotContext.Current);
        return false;
    }
}

[HarmonyPatch(typeof(UnitQuickSlotContainer), nameof(UnitQuickSlotContainer.HasItemQuickSlot))]
internal static class HasExtraItem
{
    static bool Prefix(UnitQuickSlotContainer __instance, Guid __0, ref bool __result)
    {
        if (!ItemSlotContext.IsExtra(ItemSlotContext.Current)) return true;
        __result = !string.IsNullOrEmpty(ItemSlotStorage.Read(__instance, __0, ItemSlotContext.Current));
        return false;
    }
}

[HarmonyPatch(typeof(UnitQuickSlotContainer), nameof(UnitQuickSlotContainer.SetItemQuickSlot))]
internal static class SetExtraItem
{
    static bool Prefix(UnitQuickSlotContainer __instance, Guid __0, string __1)
    {
        if (!ItemSlotContext.IsExtra(ItemSlotContext.Current)) return true;
        ItemSlotStorage.Write(__instance, __0, ItemSlotContext.Current, __1);
        return false;
    }
}

[HarmonyPatch(typeof(UnitQuickSlotContainer), nameof(UnitQuickSlotContainer.ClearItemQuickSlot))]
internal static class ClearExtraItem
{
    static bool Prefix(UnitQuickSlotContainer __instance, Guid __0)
    {
        if (!ItemSlotContext.IsExtra(ItemSlotContext.Current)) return true;
        ItemSlotStorage.Write(__instance, __0, ItemSlotContext.Current, "");
        return false;
    }
}

[HarmonyPatch]
internal static class ResetExtraItems
{
    static IEnumerable<System.Reflection.MethodBase> TargetMethods() => new[] { nameof(UnitQuickSlotContainer.Reset), nameof(UnitQuickSlotContainer.OnDespawned) }
        .Select(name => AccessTools.Method(typeof(UnitQuickSlotContainer), name));
    static void Postfix(UnitQuickSlotContainer __instance, Guid __0) => ItemSlotStorage.State(__instance).Units.Remove(__0.ToString());
}

[HarmonyPatch(typeof(UnitQuickSlotContainer), nameof(UnitQuickSlotContainer.Clear))]
internal static class ClearAllExtraItems
{
    static void Postfix(UnitQuickSlotContainer __instance) => ItemSlotStorage.Forget(__instance);
}

[HarmonyPatch(typeof(CampaignDataContainer), nameof(CampaignDataContainer.Reset))]
internal static class ResetCampaignItems
{
    static void Prefix() => ItemSlotStorage.ClearContainers();
}
