using HarmonyLib;
using Il2CppInterop.Runtime;
#if BEPINEX
using global::Refactor.Control;
using global::Refactor.Main;
using global::Refactor.Main.Event;
using global::Refactor.Main.InputModule;
#else
using Il2CppRefactor.Control;
using Il2CppRefactor.Main;
using Il2CppRefactor.Main.Event;
using Il2CppRefactor.Main.InputModule;
#endif

namespace DungeonSettlers10Slots;

internal static class ExtraInteractions
{
    internal const UIInputType Remove = (UIInputType)22001;
    internal const UIInputType ToggleAuto = (UIInputType)22002;
    internal static bool IsExtra(int index) => index >= DungeonSettlers10SlotsMod.FirstExtra && index < DungeonSettlers10SlotsMod.Capacity;

    internal static void UseNativeLabel(ref int index, out int actualIndex)
    {
        actualIndex = IsExtra(index) ? index : -1;
        if (actualIndex >= 0) index = 0;
    }

    internal static void SetAction(Interaction interaction, int index, UIInputType action)
    {
        if (index < 0) return;
        // InteractionInputType remains the native slot-1 action, used only for
        // the original translated menu label. OnSelect sends InteractionUIInput.
        // Never add indices beyond 3 to the four-entry native action enum ranges.
        var input = interaction.InteractionUIInput;
        input.UIInputType = action;
        input.ParamIndex1 = index;
        interaction.InteractionUIInput = input;
    }

    internal static InteractionFactory IsolatedFactory() =>
        Il2CppSystem.Runtime.Serialization.FormatterServices.GetUninitializedObject(Il2CppType.Of<InteractionFactory>()).Cast<InteractionFactory>();
    // Native bodies of the two factory methods never read instance fields.
    // Avoid their unrelated world-dependent constructor in isolated tests.

    internal static void ReproduceBeforeFix()
    {
        var factory = IsolatedFactory();
        var guid = Il2CppSystem.Guid.NewGuid();
        var remove = factory.CreateUnsetQuickslot(4, guid);
        var auto = factory.CreateAutoSkillSet(8, guid, true);
        Require(remove.InteractionInputType == UIInputType.InteractionOption_SetAutoSkill1, "original remove-slot-5 aliases auto-skill");
        Require(auto.InteractionInputType == UIInputType.Inventory_ItemSendAmount, "original auto-slot-9 aliases inventory send amount");
        DungeonSettlers10SlotsMod.Log.Msg($"Context regression reproduced BEFORE fix: remove slot 5={remove.InteractionInputType}; auto slot 9={auto.InteractionInputType}.");
    }

    internal static void Check(bool checkText)
    {
        var factory = IsolatedFactory();
        var guid = Il2CppSystem.Guid.NewGuid();
        var events = new InputEventFactory();
        for (var index = 0; index < DungeonSettlers10SlotsMod.Capacity; index++)
        {
            var remove = factory.CreateUnsetQuickslot(index, guid);
            var evt = events.ConvertUIEventToEventData(remove.InteractionUIInput)?.TryCast<UnsetQuickSlotRequested>();
            Require(evt != null && evt.Index == index && evt.UnitGuid.Equals(guid), "remove event slot " + (index + 1));
            Require(remove.InteractionInputType == UIInputType.InteractionOption_UnsetQuickSlot1 + (IsExtra(index) ? 0 : index), "remove label slot " + (index + 1));
            if (checkText) CheckText(remove, index, "remove");
            foreach (var enabled in new[] { false, true })
            {
                var auto = factory.CreateAutoSkillSet(index, guid, enabled);
                var toggle = events.ConvertUIEventToEventData(auto.InteractionUIInput)?.TryCast<ToggleAutoSkillRequested>();
                Require(toggle != null && toggle.Index == index && toggle.UnitGuid.Equals(guid), "auto event slot " + (index + 1));
                var baseLabel = enabled ? UIInputType.InteractionOption_UnsetAutoSkill1 : UIInputType.InteractionOption_SetAutoSkill1;
                Require(auto.InteractionInputType == baseLabel + (IsExtra(index) ? 0 : index), "auto label slot " + (index + 1));
                if (checkText) CheckText(auto, index, enabled ? "auto off" : "auto on");
            }
        }
        DungeonSettlers10SlotsMod.Log.Msg("Native context checks PASS: slots 1-10 remove + auto on/off retain original labels and correct GUID/index events; localized text checked=" + checkText);
    }

    static void CheckText(Interaction interaction, int index, string action)
    {
        var text = InteractionUtil.GetInteractionText(interaction);
        Require(!string.IsNullOrWhiteSpace(text) && !text.Contains("TextKey", StringComparison.OrdinalIgnoreCase) && !text.Contains("TEXTKEY_"), "resolved native context text");
        if (IsExtra(index)) DungeonSettlers10SlotsMod.Log.Msg($"Native context text skill={index + 1}, action={action}: {text}");
    }

    static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Context check failed: " + message);
    }
}

[HarmonyPatch(typeof(InteractionFactory), nameof(InteractionFactory.CreateUnsetQuickslot))]
internal static class ExtraRemoveInteraction
{
    static void Prefix(ref int __0, out int __state) => ExtraInteractions.UseNativeLabel(ref __0, out __state);
    static void Postfix(int __state, ref Interaction __result) => ExtraInteractions.SetAction(__result, __state, ExtraInteractions.Remove);
}

[HarmonyPatch(typeof(InteractionFactory), nameof(InteractionFactory.CreateAutoSkillSet))]
internal static class ExtraAutoInteraction
{
    static void Prefix(ref int __0, out int __state) => ExtraInteractions.UseNativeLabel(ref __0, out __state);
    static void Postfix(int __state, ref Interaction __result) => ExtraInteractions.SetAction(__result, __state, ExtraInteractions.ToggleAuto);
}

[HarmonyPatch(typeof(InputEventFactory), nameof(InputEventFactory.ConvertUIEventToEventData))]
internal static class ExtraInteractionEvents
{
    static bool Prefix(InputEventFactory __instance, UIInput __0, ref IEventData __result)
    {
        var type = __0.UIInputType;
        if (type != ExtraInteractions.Remove && type != ExtraInteractions.ToggleAuto) return true;
        __result = null;
        if (!ExtraInteractions.IsExtra(__0.ParamIndex1)) return false;
        __result = type == ExtraInteractions.Remove
            ? new UnsetQuickSlotRequested(__instance._currentMap, __0.ParamGuid1, __0.ParamIndex1).Cast<IEventData>()
            : new ToggleAutoSkillRequested(__instance._currentMap, __0.ParamGuid1, __0.ParamIndex1).Cast<IEventData>();
        return false;
    }
}

[HarmonyPatch(typeof(InputFilter), nameof(InputFilter.InitializeInputConditions))]
internal static class ExtraInteractionInputGate
{
    static void Postfix(InputFilter __instance)
    {
        var conditions = __instance.UIInputFilter._inputConditionMapping;
        foreach (var pair in new[]
        {
            (ExtraInteractions.Remove, UIInputType.InteractionOption_UnsetQuickSlot1),
            (ExtraInteractions.ToggleAuto, UIInputType.InteractionOption_SetAutoSkill1)
        })
            if (conditions.TryGetValue(pair.Item2, out var native)) conditions[pair.Item1] = native;
    }
}
