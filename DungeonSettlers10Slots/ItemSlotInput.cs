using System.Reflection;
using HarmonyLib;
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

internal static class ItemSlotInput
{
    internal const UIInputType Use = (UIInputType)22101;
    internal const UIInputType Set = (UIInputType)22102;
    internal const UIInputType Unset = (UIInputType)22103;
    internal const UIInputType Show = (UIInputType)22104;
    internal static KeyInputType Key(int slot) => slot == 0 ? KeyInputType.UseItemQuickSlot_1 : (KeyInputType)(12100 + slot);
    internal static bool IsExtra(KeyInputType type) => type == Key(1) || type == Key(2);
    internal static readonly (UIInputType Extra, UIInputType Native)[] Actions =
    {
        (Use, UIInputType.QuickSlot_UseItem), (Set, UIInputType.InteractionOption_SetItemQuickSlot),
        (Unset, UIInputType.InteractionOption_UnsetItemQuickSlot), (Show, UIInputType.ShowInteraction_ItemQuickSlot)
    };

    // Native UseItemQuickSlotRequested has no index. Retain slot metadata until
    // the normal event dispatcher delivers that exact native object, not just
    // until the input factory returns. Strong references prevent pointer reuse.
    static readonly Dictionary<IntPtr, (IEventData Event, int Slot)> pending = new();
    internal static void Tag(ref IEventData evt, int slot)
    {
        if (evt == null || !ItemSlotContext.IsExtra(slot)) return;
        if (pending.Count >= 256)
        {
            evt = null; // Never fall back to executing item 1 when metadata is lost.
            DungeonSettlers10SlotsMod.Log.Warning("Zusatz-Itemeingabe verworfen: Ereigniswarteschlange voll.");
            return;
        }
        pending[evt.Pointer] = (evt, slot);
    }
    internal static int Take(Il2CppSystem.Object evt) => pending.Remove(evt.Pointer, out var item) ? item.Slot : 0;
}

[HarmonyPatch(typeof(InputEventFactory), nameof(InputEventFactory.ConvertKeyInputToEventData))]
internal static class ExtraItemKeyEvent
{
    static void Prefix(ref KeyInputType __0, out int __state)
    {
        __state = ItemSlotInput.IsExtra(__0) ? (int)__0 - 12100 : 0;
        if (__state != 0) __0 = KeyInputType.UseItemQuickSlot_1;
    }
    static void Postfix(int __state, ref IEventData __result)
    {
        if (__state == 0 || __result == null) return;
        if (__result.TryCast<UseItemQuickSlotRequested>() == null) { __result = null; return; }
        ItemSlotInput.Tag(ref __result, __state);
    }
}

[HarmonyPatch(typeof(InputEventFactory), nameof(InputEventFactory.ConvertUIEventToEventData))]
internal static class ExtraItemUIEvent
{
    static bool Prefix(ref UIInput __0, out int __state, ref IEventData __result)
    {
        __state = 0;
        foreach (var action in ItemSlotInput.Actions)
        {
            if (__0.UIInputType != action.Extra) continue;
            if (!ItemSlotContext.IsExtra(__0.ParamIndex1)) { __result = null; return false; }
            __state = __0.ParamIndex1;
            __0.UIInputType = action.Native;
            return true;
        }
        return true;
    }
    static void Postfix(int __state, ref IEventData __result) => ItemSlotInput.Tag(ref __result, __state);
}

[HarmonyPatch]
internal static class ExtraItemDispatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(PlayerControlEventHandler), nameof(PlayerControlEventHandler.OnEvent), new[] { typeof(UseItemQuickSlotRequested) });
        foreach (var type in new[] { typeof(SetItemQuickSlotRequested), typeof(UnsetItemQuickSlotRequested), typeof(ShowInteractionRequested_ItemQuickSlot) })
            yield return AccessTools.Method(typeof(InputEventHandler), nameof(InputEventHandler.OnEvent), new[] { type });
    }
    static void Prefix(Il2CppSystem.Object __0, out ItemSlotContext __state) => __state = new ItemSlotContext(ItemSlotInput.Take(__0));
    static void Finalizer(ItemSlotContext __state) => __state?.Dispose();
}

[HarmonyPatch(typeof(InteractionFactory), nameof(InteractionFactory.CreateUnsetItemQuickSlot))]
internal static class ExtraItemRemoveMenu
{
    static void Postfix(ref Interaction __result)
    {
        if (ItemSlotContext.IsExtra(ItemSlotContext.Current))
            ExtraInteractions.SetAction(__result, ItemSlotContext.Current, ItemSlotInput.Unset);
    }
}

[HarmonyPatch(typeof(InputFilter), nameof(InputFilter.InitializeInputConditions))]
internal static class ExtraItemInputGate
{
    static void Postfix(InputFilter __instance)
    {
        var keys = __instance.KeyInputFilter._inputConditionMapping;
        for (var slot = 1; slot < 3; slot++) keys[ItemSlotInput.Key(slot)] = keys[KeyInputType.UseItemQuickSlot_1];
        var ui = __instance.UIInputFilter._inputConditionMapping;
        foreach (var action in ItemSlotInput.Actions)
            if (ui.TryGetValue(action.Native, out var condition)) ui[action.Extra] = condition;
    }
}

[HarmonyPatch(typeof(InputObserverMapper), nameof(InputObserverMapper.Map), new[] { typeof(KeyInputType) })]
internal static class ExtraItemObserver
{
    static void Prefix(ref KeyInputType __0)
    {
        if (ItemSlotInput.IsExtra(__0)) __0 = KeyInputType.UseItemQuickSlot_1;
    }
}
