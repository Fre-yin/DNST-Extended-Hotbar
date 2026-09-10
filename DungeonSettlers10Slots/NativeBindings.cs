using HarmonyLib;
#if BEPINEX
using global::Refactor.Main;
using global::Refactor.Main.Event;
using global::Refactor.Main.InputModule;
using global::Refactor.Setting;
using global::Refactor.UI;
#else
using Il2CppRefactor.Main;
using Il2CppRefactor.Main.Event;
using Il2CppRefactor.Main.InputModule;
using Il2CppRefactor.Setting;
using Il2CppRefactor.UI;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSettlers10Slots;

internal static class ExtraBindings
{
    // Keep existing IDs stable; only skills 5-10 are exposed.
    internal const int FirstId = 12005;
    internal static bool IsExtra(KeyInputType type) => (int)type >= FirstId && (int)type < FirstId + DungeonSettlers10SlotsMod.ExtraCount;
    internal static KeyInputType TypeAt(int extra) => (KeyInputType)(FirstId + extra);
}

[HarmonyPatch(typeof(KeySetting), nameof(KeySetting.CreateDefaultBindings))]
internal static class ExtraDefaults
{
    static void Postfix(Il2CppSystem.Collections.Generic.List<KeyBindingData> __result)
    {
        CombatBindingPreset.ApplyDefaults(__result);
    }
}

[HarmonyPatch(typeof(SubUI_KeySetting), nameof(SubUI_KeySetting.RefreshRows))]
internal static class ExpandOptions
{
    // Builds 25124554 / 25143510 inline Init in the parent settings view; those
    // callers use RefreshRows. Extend before its native enumeration so
    // native SetRowData initializes every clone's binding callbacks immediately.
    static void Prefix(SubUI_KeySetting __instance)
    {
        ExtraBindingText.EnsureCurrent();
        EnsureRows(__instance);
    }

    static void Postfix(SubUI_KeySetting __instance) => NativeLocalizationChecks.CheckOptionsIfRequested(__instance);

    internal static bool EnsureRows(SubUI_KeySetting __instance)
    {
        if (__instance._rows == null || !__instance._rows.TryGetValue(KeyInputType.UseSkill_4, out var template)
            || !__instance._rows.TryGetValue(KeyInputType.UseItemQuickSlot_1, out var itemTemplate)
            || !template || !itemTemplate) return false;
        var changed = false;
        for (var i = 0; i < DungeonSettlers10SlotsMod.ExtraCount; i++)
        {
            if (__instance._rows.ContainsKey(ExtraBindings.TypeAt(i))) continue;
            var clone = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent).GetComponent<SubUI_KeySettingParts>();
            clone.name = "SkillBinding_" + (i + 5);
            clone.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + i + 1);
            __instance._rows.Add(ExtraBindings.TypeAt(i), clone);
            changed = true;
        }
        for (var i = 1; i < 3; i++)
        {
            if (__instance._rows.ContainsKey(ItemSlotInput.Key(i))) continue;
            var clone = UnityEngine.Object.Instantiate(itemTemplate.gameObject, itemTemplate.transform.parent).GetComponent<SubUI_KeySettingParts>();
            clone.name = "ItemBinding_" + (i + 1);
            clone.transform.SetSiblingIndex(itemTemplate.transform.GetSiblingIndex() + i);
            __instance._rows.Add(ItemSlotInput.Key(i), clone);
            changed = true;
        }
        if (!changed) return false;
        LayoutRebuilder.MarkLayoutForRebuild(template.transform.parent.Cast<RectTransform>());
        DungeonSettlers10SlotsMod.Log.Msg($"Options: {DungeonSettlers10SlotsMod.ExtraCount} skill + 2 item rows added, two binding columns each.");
        return true;
    }
}

[HarmonyPatch(typeof(InputEventFactory), nameof(InputEventFactory.ConvertKeyInputToEventData))]
internal static class ExtraSkillEvents
{
    static void Prefix(ref KeyInputType __0, out int __state)
    {
        __state = 0;
        if (!ExtraBindings.IsExtra(__0)) return;
        // Use events count basic attack as 0 and active skills as 1..10.
        // Storage/drag events instead address active skills as 0..9.
        __state = DungeonSettlers10SlotsMod.FirstExtra + 1 + (int)__0 - ExtraBindings.FirstId;
        // Preserve the native map/popup/key-lock checks and event construction.
        __0 = KeyInputType.UseSkill_4;
    }

    static void Postfix(int __state, ref IEventData __result)
    {
        if (__state == 0 || __result == null) return;
        var skill = __result.TryCast<UseQuickSlotSkillRequested>();
        if (skill == null || skill.Index != 4)
        {
            __result = null;
            DungeonSettlers10SlotsMod.Log.Error("Unexpected native skill-key event; extra input discarded.");
            return;
        }
        skill.Index = __state;
    }
}

[HarmonyPatch(typeof(InputFilter), nameof(InputFilter.InitializeInputConditions))]
internal static class ExtraSkillInputGate
{
    static void Postfix(InputFilter __instance)
    {
        var conditions = __instance.KeyInputFilter._inputConditionMapping;
        if (!conditions.ContainsKey(KeyInputType.UseSkill_4)) throw new InvalidOperationException("Native Skill-Eingabebedingungen fehlen.");
        for (var i = 0; i < DungeonSettlers10SlotsMod.ExtraCount; i++) conditions[ExtraBindings.TypeAt(i)] = conditions[KeyInputType.UseSkill_4];
    }
}

[HarmonyPatch(typeof(InputObserverMapper), nameof(InputObserverMapper.Map), new[] { typeof(KeyInputType) })]
internal static class ExtraSkillObserver
{
    static void Prefix(ref KeyInputType __0)
    {
        if (ExtraBindings.IsExtra(__0)) __0 = KeyInputType.UseSkill_4;
    }
}
