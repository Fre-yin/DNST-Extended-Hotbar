using HarmonyLib;
#if BEPINEX
using global::Refactor.Main.InputModule;
using global::Refactor.Setting;
using global::Refactor.UI;
#else
using Il2CppRefactor.Main.InputModule;
using Il2CppRefactor.Setting;
using Il2CppRefactor.UI;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSettlers10Slots;

internal static class ItemSlotUI
{
    internal const float FrameGap = 4;
    static readonly Dictionary<int, (QuickSlotUI Bar, ItemQuickSlotUI[] Slots)> bars = new();
    [ThreadStatic] internal static bool Refreshing;
    internal static ItemQuickSlotUI[] Slots(QuickSlotUI bar) => bars.TryGetValue(bar.GetInstanceID(), out var entry) ? entry.Slots : null;
    internal static float Width(QuickSlotUI bar)
    {
        var slots = Slots(bar);
        if (slots == null) return bar._itemQuickSlot.GetComponent<RectTransform>().rect.width;
        var first = slots[0].GetComponent<RectTransform>();
        var last = slots[2].GetComponent<RectTransform>();
        float X(RectTransform rect, float x) => bar.transform.InverseTransformPoint(rect.TransformPoint(new Vector3(x, 0, 0))).x;
        var firstWidth = X(first, first.rect.xMax) - X(first, first.rect.xMin);
        var settledWidth = X(last, last.rect.xMax) - X(first, first.rect.xMin);
        // Before the hidden native LayoutGroup has run, the clones still overlap.
        // Afterwards use actual edges, including native child/group scales.
        return settledWidth > firstWidth * 2 ? settledWidth : firstWidth * 3;
    }

    internal static void Ensure(QuickSlotUI bar)
    {
        Prune();
        if (Slots(bar) != null) return;
        var original = bar._itemQuickSlot;
        if (!original || original.transform.parent.name != "Layout_Item") throw new InvalidOperationException("Native Item-Hierarchie unbekannt.");
        var group = original.transform.parent.GetComponent<HorizontalLayoutGroup>();
        if (!group) throw new InvalidOperationException("Native horizontale Item-Anordnung fehlt.");
        var slots = new ItemQuickSlotUI[3];
        slots[0] = original;
        for (var i = 1; i < slots.Length; i++)
        {
            var index = i;
            var slot = UnityEngine.Object.Instantiate(original.gameObject, original.transform.parent).GetComponent<ItemQuickSlotUI>();
            slots[i] = slot;
            slot.name = "ItemQuickSlot_" + (i + 1);
            // InitUI writes delegates, including the native drag validation.
            slot.InitUI((Il2CppSystem.Action)(() => Send(bar, ItemSlotInput.Use, index)),
                (Il2CppSystem.Action)(() => Send(bar, ItemSlotInput.Show, index)), "",
                (Il2CppSystem.Func<DragData, bool>)(payload => bar.CanDropItemQuickSlot(payload)),
                (Il2CppSystem.Action<DragData, GameObject>)((payload, source) =>
                {
                    if (bar.TryGetDroppableItemQuickSlotKey(payload, out var key)) Send(bar, ItemSlotInput.Set, index, key);
                }));
            slot.SetItem("", 0, new Il2CppSystem.Nullable<Il2CppSystem.Guid>());
        }
        bars.Add(bar.GetInstanceID(), (bar, slots));
        group.spacing = 0;
        LayoutRebuilder.MarkLayoutForRebuild(group.GetComponent<RectTransform>());
        DungeonSettlers10SlotsMod.Log.Msg("Three native item fields created; original item controls/tooltips/drop validation retained.");
    }

    static void Send(QuickSlotUI bar, UIInputType action, int slot, string key = "")
    {
        if (!bar || !bar.isActiveAndEnabled || bar._inputStream == null) return;
        var input = new UIInput(action, bar._selected, key, slot);
        bar._inputStream.AddInput(input);
    }

    internal static void Labels(QuickSlotUI bar, IKeySettingReader settings)
    {
        var slots = Slots(bar);
        if (slots == null) return;
        for (var i = 0; i < slots.Length; i++) HotbarKeyLabels.Set(slots[i]._inputKeyText, HotbarKeyLabels.Read(settings, ItemSlotInput.Key(i)));
    }

    internal static void Refresh(QuickSlotUIPresenter presenter)
    {
        var bar = presenter.View;
        if (!bar || Refreshing) return;
        var slots = Slots(bar);
        if (slots == null) return;
        var original = bar._itemQuickSlot;
        Refreshing = true;
        try
        {
            for (var i = 1; i < slots.Length; i++)
            {
                using var context = new ItemSlotContext(i);
                bar._itemQuickSlot = slots[i];
                // Native presenter computes amount and item GUID, then SetItem
                // owns icon, tooltip, enabled state. No inventory/consumption copy.
                presenter.RefreshItemQuickSlotUI();
            }
        }
        finally { bar._itemQuickSlot = original; Refreshing = false; }
    }

    internal static void Prune()
    {
        foreach (var id in bars.Where(pair => !pair.Value.Bar).Select(pair => pair.Key).ToArray()) bars.Remove(id);
    }
}

[HarmonyPatch(typeof(QuickSlotUIPresenter), nameof(QuickSlotUIPresenter.RefreshOnSelect))]
internal static class SettleItemsWhenShown
{
    static void Prefix(QuickSlotUIPresenter __instance, out bool __state) => __state = __instance.View && __instance.View.gameObject.activeInHierarchy;
    static void Postfix(QuickSlotUIPresenter __instance, bool __state)
    {
        // Hidden layout groups defer their child arrangement until activation.
        // No repeated layout passes when switching between visible characters.
        if (!__state && __instance.View && __instance.View.gameObject.activeInHierarchy) HotbarFrameLayout.RequestSettle();
    }
}

[HarmonyPatch(typeof(QuickSlotUIPresenter), nameof(QuickSlotUIPresenter.RefreshItemQuickSlotUI))]
internal static class RefreshExtraItems
{
    static void Prefix(out ItemSlotContext __state) => __state = ItemSlotUI.Refreshing ? null : new ItemSlotContext(0);
    static void Postfix(QuickSlotUIPresenter __instance) => ItemSlotUI.Refresh(__instance);
    static void Finalizer(ItemSlotContext __state) => __state?.Dispose();
}
