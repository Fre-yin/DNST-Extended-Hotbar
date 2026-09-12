#if BEPINEX
using global::Refactor.Main;
using global::Refactor.Main.Event;
using global::Refactor.Main.InputModule;
using global::Refactor.Setting;
#else
using Il2CppRefactor.Main;
using Il2CppRefactor.Main.Event;
using Il2CppRefactor.Main.InputModule;
using Il2CppRefactor.Setting;
#endif
using UnityEngine;

namespace DungeonSettlers10Slots;

internal static class ItemSlotChecks
{
    internal static void Run()
    {
        var owner = new UnitQuickSlotContainer();
        var first = Il2CppSystem.Guid.NewGuid();
        var second = Il2CppSystem.Guid.NewGuid();
        owner.OnSpawned(first); owner.OnSpawned(second);
        owner.SetItemQuickSlot(first, "native_item");
        owner.AddQuickSlotAt(first, 9, "skill_ten");
        for (var slot = 1; slot < 3; slot++)
        {
            using var context = new ItemSlotContext(slot);
            owner.SetItemQuickSlot(first, "item_" + slot);
            Require(owner.GetItemQuickSlotKey(first) == "item_" + slot && owner.HasItemQuickSlot(first), "read/write/has slot " + slot);
            Require(string.IsNullOrEmpty(owner.GetItemQuickSlotKey(second)) && !owner.HasItemQuickSlot(second), "GUID separation");
            owner.SetItemQuickSlot(Il2CppSystem.Guid.NewGuid(), "unknown");
            Require(owner._unitQuickSlot.Count == 2, "unknown GUID cannot create record");
        }
        Require(ItemSlotContext.Current == 0 && owner.GetItemQuickSlotKey(first) == "native_item", "scope restored, native slot untouched");
        Require(owner.GetAll(first).Length == 10 && owner.GetSkillAt(first, 9) == "skill_ten", "skill slots unchanged");
        var save = owner.Serialize();
        var loaded = new UnitQuickSlotContainer();
        loaded.Deserialize(save);
        for (var slot = 1; slot < 3; slot++)
        {
            using var context = new ItemSlotContext(slot);
            Require(loaded.GetItemQuickSlotKey(first) == "item_" + slot, "snapshot round trip " + slot);
        }
        using (new ItemSlotContext(1)) loaded.ClearItemQuickSlot(first);
        using (new ItemSlotContext(1)) Require(!loaded.HasItemQuickSlot(first), "clear first extra");
        using (new ItemSlotContext(2)) Require(loaded.GetItemQuickSlotKey(first) == "item_2", "other extra retained");
        Require(loaded.GetItemQuickSlotKey(first) == "native_item", "native item retained after extra removal");
        var json = ItemSlotSaveCodec.Encode("{\"Native\":9007199254740993}", ItemSlotStorage.State(owner));
        var decoded = ItemSlotSaveCodec.Decode(json);
        Require(json.Contains("9007199254740993") && decoded.Units[first.ToString()][1] == "item_2", "JSON precision and extra data");
        Require(ItemSlotSaveCodec.Decode("{}").Units.Count == 0, "vanilla save has empty extras");
        var future = ItemSlotSaveCodec.Decode("{\"" + ItemSlotSaveCodec.Property + "\":{\"Version\":2,\"Future\":true}}");
        Require(future.OpaqueExtension != null && ItemSlotSaveCodec.Encode("{}", future).Contains("\"Future\":true"), "unknown version preserved read-only");
        ItemSlotStorage.Remember(save, decoded);
        loaded.Deserialize(save);
        using (new ItemSlotContext(2)) Require(loaded.GetItemQuickSlotKey(first) == "item_2", "JSON -> native dictionary -> live container");
        loaded.Reset(first);
        using (new ItemSlotContext(2)) Require(!loaded.HasItemQuickSlot(first), "unit reset clears extra assignment");

        var settings = new KeySetting();
        var factory = NativeKeyboardChecks.IsolatedFactory();
        for (var slot = 0; slot < 3; slot++)
        {
            var type = ItemSlotInput.Key(slot);
            Require(settings.TryGetBinding(type, 0, out var binding) && binding.KeyCode == new[] { KeyCode.Y, KeyCode.None, KeyCode.None }[slot], "native/unbound defaults " + slot);
            var evt = factory.ConvertKeyInputToEventData(type);
            Require(evt?.TryCast<UseItemQuickSlotRequested>() != null && ItemSlotInput.Take(evt.Cast<Il2CppSystem.Object>()) == slot, "native key event and routing " + slot);
            settings.BindKey(type, 0, KeyCode.L);
            var reloaded = new KeySetting(settings.GetKeySettingData());
            Require(reloaded.TryGetBinding(type, 0, out var changed) && changed.KeyCode == KeyCode.L, "native arbitrary rebind persistence " + slot);
            settings.ResetDefaultSetting();
        }
        var menuFactory = ExtraInteractions.IsolatedFactory();
        for (var slot = 1; slot < 3; slot++)
        {
            using var context = new ItemSlotContext(slot);
            var menu = menuFactory.CreateUnsetItemQuickSlot(first);
            Require(menu.InteractionInputType == UIInputType.InteractionOption_UnsetItemQuickSlot, "native remove label");
            foreach (var modifier in new[] { ModifierKey.None, ModifierKey.LeftShift, ModifierKey.Ctrl, ModifierKey.Alt })
            {
                var nativeInput = new UIInput(UIInputType.InteractionOption_UnsetItemQuickSlot, first);
                var native = factory.ConvertUIEventToEventData(nativeInput, modifier);
                Require(native?.TryCast<UnsetItemQuickSlotRequested>()?.UnitGuid.Equals(first) == true,
                    "native remove contract " + modifier);
                var evt = factory.ConvertUIEventToEventData(menu.InteractionUIInput, modifier);
                Require(evt?.TryCast<UnsetItemQuickSlotRequested>()?.UnitGuid.Equals(first) == true
                    && ItemSlotInput.Take(evt.Cast<Il2CppSystem.Object>()) == slot,
                    "native GUID-specific remove event " + slot + " / " + modifier);
            }
        }
        DungeonSettlers10SlotsMod.Log.Msg("UI modifier contract PASS: native and extra item removal preserve GUID/slot for None, Shift, Ctrl and Alt on the two-argument input factory.");
        DungeonSettlers10SlotsMod.Log.Msg("Item-slot isolated checks PASS: three assignments, GUID isolation, skill preservation, scoped read/write/remove/reset, JSON + native storage round trip, unknown-version preservation, native/unbound defaults + arbitrary rebinds, key and menu routing.");
    }

    static void Require(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("Item-slot check FAILED: " + name);
    }
}
