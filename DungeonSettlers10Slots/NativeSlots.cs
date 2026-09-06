using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppRefactor;
using Il2CppRefactor.Main;
using Il2CppRefactor.Main.InputModule;
using Il2CppRefactor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSettlers10Slots;

internal static class SlotStorage
{
    internal static void Ensure(UnitQuickSlotContainer container, Il2CppSystem.Guid guid)
    {
        // Do not create records for unknown GUIDs: preserve native ownership/lifecycle rules.
        if (container._unitQuickSlot.TryGetValue(guid, out var data)) Ensure(data);
    }

    internal static void Ensure(QuickSlotData data)
    {
        var old = data._quickSlots;
        if (old != null && old.Length >= DungeonSettlers10SlotsMod.Capacity) return;
        var expanded = new Il2CppStringArray(DungeonSettlers10SlotsMod.Capacity);
        for (var i = 0; i < expanded.Length; i++) expanded[i] = old != null && i < old.Length ? old[i] : "";
        data._quickSlots = expanded;
    }
}

[HarmonyPatch]
internal static class ContainerSlotMutation
{
    static IEnumerable<MethodBase> TargetMethods() => new[]
    {
        "AddQuickSlotAt", "RemoveQuickSlotAt", "TryRegisterQuickSlot", "ResetSkillSlots"
    }.Select(name => AccessTools.Method(typeof(UnitQuickSlotContainer), name));

    // IL2CPP inlines QuickSlotData methods into this container. Patching only the
    // small data methods cannot cover the native event-handler -> container path.
    static void Prefix(UnitQuickSlotContainer __instance, Il2CppSystem.Guid __0) => SlotStorage.Ensure(__instance, __0);
}

[HarmonyPatch]
internal static class ContainerSlotCreated
{
    static IEnumerable<MethodBase> TargetMethods() => new[] { "OnSpawned", "Reset" }
        .Select(name => AccessTools.Method(typeof(UnitQuickSlotContainer), name));
    static void Postfix(UnitQuickSlotContainer __instance, Il2CppSystem.Guid __0) => SlotStorage.Ensure(__instance, __0);
}

[HarmonyPatch(typeof(UnitQuickSlotContainer), nameof(UnitQuickSlotContainer.Deserialize))]
internal static class ContainerSlotsLoaded
{
    static void Postfix(UnitQuickSlotContainer __instance, Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Guid, QuickSlotSaveData> __0)
    {
        // Native Deserialize constructs four slots and copies min(saved, 4),
        // inlining QuickSlotData.Deserialize. Restore the complete native record
        // after it has created the GUID entries; merely resizing would lose 5+.
        if (__0 == null) { ItemSlotStorage.Loaded(__instance, null); return; }
        var entries = __0.GetEnumerator();
        while (entries.MoveNext())
        {
            var entry = entries.Current;
            if (__instance._unitQuickSlot.TryGetValue(entry.Key, out var data)) data.Deserialize(entry.Value);
        }
        ItemSlotStorage.Loaded(__instance, __0);
    }
}

[HarmonyPatch(typeof(UnitQuickSlotContainer), nameof(UnitQuickSlotContainer.Serialize))]
internal static class ContainerSlotsSaved
{
    static bool Prefix(UnitQuickSlotContainer __instance, ref Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Guid, QuickSlotSaveData> __result)
    {
        // Native container Serialize also inlines the fixed-size four-element
        // allocation. Keep its dictionary/schema but use our full-length copies.
        var result = new Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Guid, QuickSlotSaveData>();
        var entries = __instance._unitQuickSlot.GetEnumerator();
        while (entries.MoveNext())
        {
            var entry = entries.Current;
            result.Add(entry.Key, entry.Value.Serialize());
        }
        __result = result;
        ItemSlotStorage.Remember(result, ItemSlotStorage.State(__instance));
        return false;
    }
}

[HarmonyPatch]
internal static class MutateSlots
{
    static IEnumerable<MethodBase> TargetMethods() => new[] { "AddAt", "TryAdd", "RemoveAt" }
        .Select(name => AccessTools.Method(typeof(QuickSlotData), name));
    static void Prefix(QuickSlotData __instance) => SlotStorage.Ensure(__instance);
}

[HarmonyPatch(typeof(QuickSlotData), nameof(QuickSlotData.Deserialize))]
internal static class LoadSlots
{
    static bool Prefix(QuickSlotData __instance, QuickSlotSaveData __0)
    {
        var saved = __0.QuickSlots;
        __instance._quickSlots = new Il2CppStringArray(Math.Max(DungeonSettlers10SlotsMod.Capacity, saved?.Length ?? 0));
        for (var i = 0; i < __instance._quickSlots.Length; i++) __instance._quickSlots[i] = saved != null && i < saved.Length ? saved[i] : "";
        __instance._itemQuickSlotKey = __0.ItemQuickSlotKey;
        return false;
    }
}

[HarmonyPatch(typeof(QuickSlotData), nameof(QuickSlotData.Serialize))]
internal static class SaveSlots
{
    static bool Prefix(QuickSlotData __instance, ref QuickSlotSaveData __result)
    {
        SlotStorage.Ensure(__instance);
        var copy = new Il2CppStringArray(__instance._quickSlots.Length);
        for (var i = 0; i < copy.Length; i++) copy[i] = __instance._quickSlots[i];
        __result = new QuickSlotSaveData { QuickSlots = copy, ItemQuickSlotKey = __instance._itemQuickSlotKey };
        return false;
    }
}

[HarmonyPatch(typeof(QuickSlotData), nameof(QuickSlotData.ResetSkillSlots))]
internal static class ResetSlots
{
    static void Postfix(QuickSlotData __instance) => SlotStorage.Ensure(__instance);
}

[HarmonyPatch(typeof(QuickSlotUI), nameof(QuickSlotUI.Init))]
internal static class ExpandHotbar
{
    static void Prefix(QuickSlotUI __instance, Il2CppSystem.Collections.Generic.Dictionary<int, string> __1)
    {
        var slots = __instance._skillSlots;
        var original = slots.Count;
        if (original < 1) throw new InvalidOperationException("Originale Skill-Slots fehlen.");
        var template = slots[original - 1];
        var extraParent = template.transform.parent;
        // UI slot zero is the separate basic attack; QuickSlotData contains only the four active slots.
        while (slots.Count < DungeonSettlers10SlotsMod.Capacity + 1)
        {
            var clone = UnityEngine.Object.Instantiate(template.gameObject, extraParent).GetComponent<SkillSlotUI>();
            clone.name = "SkillSlot_" + slots.Count;
            // InitUI and InitSkillUI set the native icon/owner state after cloning.
            slots.Add(clone);
        }
        // Init binds the original callbacks (selection, context menu and auto-use) for every slot.
        for (var i = DungeonSettlers10SlotsMod.FirstExtra; i < DungeonSettlers10SlotsMod.Capacity; i++)
            if (!__1.ContainsKey(i)) __1[i] = "";
        if (original != slots.Count)
            DungeonSettlers10SlotsMod.Log.Msg($"Native hotbar expanded: {original} -> {slots.Count}; parent={template.transform.parent.name}");
    }
    static void Postfix(QuickSlotUI __instance)
    {
        ItemSlotUI.Ensure(__instance);
        ApplyLayout(__instance);
    }

    internal static void ApplyLayout(QuickSlotUI bar)
    {
        if (bar._skillSlots == null || bar._skillSlots.Count != DungeonSettlers10SlotsMod.Capacity + 1) return;
        if (NativeLayoutComparison.Enabled)
        {
            NativeLayoutComparison.Check(bar);
            return;
        }
        HotbarFrameLayout.Apply(bar);
    }
}

[HarmonyPatch]
internal static class RefreshExtraSkills
{
    static IEnumerable<MethodBase> TargetMethods() => new[] { "InitSkillUI", "RefreshSkillUI" }
        .Select(name => AccessTools.Method(typeof(QuickSlotUI), name));

    static void Prefix(ref Il2CppStringArray __1) => __1 = DisplaySlots(__1);

    internal static Il2CppStringArray DisplaySlots(Il2CppStringArray source)
    {
        if (source != null && source.Length == DungeonSettlers10SlotsMod.Capacity) return source;
        var expanded = new Il2CppStringArray(DungeonSettlers10SlotsMod.Capacity);
        for (var i = 0; i < expanded.Length; i++)
            expanded[i] = source != null && i < source.Length ? source[i] : "";
        // Presentation only: preserve hidden legacy slots 11/12 in the backing
        // record/save; native RefreshSkillUI indexes a field for every input key.
        return expanded;
    }

    static void Postfix(QuickSlotUI __instance, IEntity __0, Il2CppStringArray __1, IInMapDataSupplier __2)
    {
        // Verified in native code: InitSkillUI iterates _skillSlots.Count,
        // RefreshSkillUI iterates quickSlots.Length. Both already handle every
        // extra slot, and InitSkillUI sets empty icons and owner/index itself.
        if (DungeonSettlers10SlotsMod.RunUiAudits) HotbarDiagnostics.CheckOnce(__instance);
    }
}
