#if BEPINEX
using global::Refactor.Main.InputModule;
using global::Refactor.Main.Event;
using global::Refactor.UI;
#else
using Il2CppRefactor.Main.InputModule;
using Il2CppRefactor.Main.Event;
using Il2CppRefactor.UI;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonSettlers10Slots;

internal static class HotbarDiagnostics
{
    static readonly HashSet<string> checkedOwners = new();
    static bool checkedContextText;
    internal static void CheckOnce(QuickSlotUI bar)
    {
        var slots = bar._skillSlots;
        if (slots == null || slots.Count != DungeonSettlers10SlotsMod.Capacity + 1) return;
        SkillSlotUI source = null;
        for (var i = 1; i < slots.Count; i++)
            if (slots[i]._owner != null && !string.IsNullOrEmpty(slots[i]._key)) { source = slots[i]; break; }
        if (!source) return;
        var id = bar.GetInstanceID() + ":" + source._owner.Pointer;
        if (!checkedOwners.Add(id)) return;
        if (!checkedContextText)
        {
            try { ExtraInteractions.Check(true); checkedContextText = true; }
            catch (Exception ex) { DungeonSettlers10SlotsMod.Log.Error("Native context text audit failed: " + ex); }
        }
        try { Check(bar, source); }
        catch (Exception ex) { DungeonSettlers10SlotsMod.Log.Warning("Hotbar audit failed: " + ex); }
    }

    static void Check(QuickSlotUI bar, SkillSlotUI source)
    {
        var nativePayload = source.GetPayload();
        if (nativePayload == null) throw new InvalidOperationException("Known occupied slot produced no payload.");
        // Use the real skill/owner, with the native skill-tree sentinel (-1).
        var payload = new DragData { Type = nativePayload.Type, Owner = nativePayload.Owner, Key = nativePayload.Key,
            SlotIndex = -1, Icon = nativePayload.Icon };
        GameObject scratch = null;
        try
        {
            // Only this hidden clone emits events, into an isolated InputStream.
            // No event is dispatched to the campaign and no slot data is written.
            scratch = UnityEngine.Object.Instantiate(source.gameObject);
            scratch.name = "SkillDropIsolatedProbe";
            scratch.SetActive(false);
            var probe = scratch.GetComponent<SkillSlotUI>();
            probe.InitOnInteract(null, "Alpha1");
            if (probe._txtInputKey.GetText() != "1" || probe._txtInputKey.textWrappingMode != Il2CppTMPro.TextWrappingModes.NoWrap)
                throw new InvalidOperationException("Native label regression: Alpha1 must be single-line 1.");
            probe.InitOnInteract(null, "0");
            if (probe._txtInputKey.GetText() != "0") throw new InvalidOperationException("Preformatted zero label was lost.");
            DungeonSettlers10SlotsMod.Log.Msg("Native hotbar label PASS: InitOnInteract Alpha1 -> 1, zero preserved, wrapping disabled; isolated cloned UI only.");
            var buffer = new InputStream();
            probe._inputStream = new UIInputStream(buffer);
            for (var i = 1; i < bar._skillSlots.Count; i++)
            {
                var slot = bar._skillSlots[i];
                var parent = slot.transform.parent;
                DungeonSettlers10SlotsMod.Log.Msg($"Hotbar audit: ui={i}, nativeIndex={slot._index}, ownerMatch={slot._owner?.Pointer == payload.Owner?.Pointer}, sameStream={slot._inputStream?.Pointer == source._inputStream?.Pointer}, sameBuffer={slot._inputStream?._inputStream?.Pointer == source._inputStream?._inputStream?.Pointer}, active={slot.gameObject.activeInHierarchy}, interactable={slot.interactable}, canDrop={slot.CanDrop(payload)}, parent={parent?.name}/{parent?.parent?.name}");
                buffer.ClearInputs();
                probe._owner = slot._owner;
                probe._index = slot._index;
                probe.OnDrop(payload, source.gameObject);
                var emitted = buffer.UIInputs.GetEnumerator();
                if (!emitted.MoveNext())
                    DungeonSettlers10SlotsMod.Log.Warning($"Isolated native drop ui={i}: no event emitted.");
                else
                {
                    var evt = emitted.Current;
                    DungeonSettlers10SlotsMod.Log.Msg($"Isolated native drop ui={i}: event={evt.UIInputType}, index={evt.ParamIndex1}, key={evt.ParamString1}, guid={evt.ParamGuid1}, correctIndex={evt.ParamIndex1 == i - 1}");
                }
                buffer.ClearInputs();
                probe.OnDrop(nativePayload, source.gameObject);
                emitted = buffer.UIInputs.GetEnumerator();
                if (!emitted.MoveNext()) throw new InvalidOperationException("Native MOVE produced no input at " + i);
                var moveInput = emitted.Current;
                var move = new InputEventFactory().ConvertUIEventToEventData(moveInput)?.TryCast<MoveQuickSlotRequested>();
                var correct = move != null && move.Index == nativePayload.SlotIndex - 1 && move.TargetIndex == i - 1;
                DungeonSettlers10SlotsMod.Log.Msg($"Isolated native MOVE ui={i}: source={move?.Index}, target={move?.TargetIndex}, guid={move?.UnitGuid}, correct={correct}");
                if (!correct) throw new InvalidOperationException("Native MOVE conversion failed at " + i);
            }
        }
        finally { if (scratch) UnityEngine.Object.Destroy(scratch); }
    }

}
