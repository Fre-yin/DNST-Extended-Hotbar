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

internal static class NativeChecks
{
    internal static void CheckContainerBeforeFix()
    {
        // Called before installing container hooks, after the older data hooks.
        // Reproduces their coverage gap on a disposable native container only.
        var container = new UnitQuickSlotContainer();
        var guid = Il2CppSystem.Guid.NewGuid();
        container.OnSpawned(guid);
        var length = container._unitQuickSlot[guid]._quickSlots.Length;
        var viewLength = container.GetAll(guid).Length;
        Require(length == 4 && viewLength == 4, "baseline native container bypasses data-method hooks");
        DungeonSettlers10SlotsMod.Log.Msg($"Regression reproduced BEFORE container fix: backing slots={length}, native GetAll={viewLength}; data-method hooks alone do not expand either.");
    }

    internal static void Run()
    {
        // Disposable native objects only; no campaign or user settings are loaded or saved.
        CheckLegacyDisplayBoundary();
        CheckKeyLabels();
        if (DungeonSettlers10SlotsMod.RunUiAudits) NativeKeyboardChecks.Run();
        CheckContainer();
        var slots = new QuickSlotData();
        var last = DungeonSettlers10SlotsMod.Capacity - 1;
        slots.AddAt(last, "mod_smoke_test");
        Require(slots.GetSlot(last) == "mod_smoke_test", "last slot write/read");
        var reloaded = new QuickSlotData();
        reloaded.Deserialize(slots.Serialize());
        Require(reloaded.GetSlot(last) == "mod_smoke_test", "native slot serialization round trip");
        reloaded.RemoveAt(last);
        Require(string.IsNullOrEmpty(reloaded.GetSlot(last)), "last slot clear");
        var settings = new KeySetting();
        CombatBindingPreset.VerifyDefaults(settings.Cast<IKeySettingReader>());
        var originalKeys = new[] { KeyInputType.UseSkill_1, KeyInputType.UseSkill_2, KeyInputType.UseSkill_3, KeyInputType.UseSkill_4 }
            .Select(settings.GetKeyCodeByType).ToArray();
        Require(DungeonSettlers10SlotsMod.Capacity == 10 && DungeonSettlers10SlotsMod.ExtraCount == 6, "ten active slots, six extra bindings");
        Require(!ExtraBindings.IsExtra(ExtraBindings.TypeAt(6)), "skill 11 not exposed");
        Require(!ExtraBindings.IsExtra(ExtraBindings.TypeAt(7)), "skill 12 not exposed");
        var keyFactory = NativeKeyboardChecks.IsolatedFactory();
        for (var i = 0; i < DungeonSettlers10SlotsMod.ExtraCount; i++)
        {
            var type = ExtraBindings.TypeAt(i);
            var evt = keyFactory.ConvertKeyInputToEventData(type).Cast<UseQuickSlotSkillRequested>();
            Require(evt.Index == DungeonSettlers10SlotsMod.FirstExtra + 1 + i, "extra use-event UI index (includes basic attack)");
        }
        var changed = settings.GetKeySettingData();
        var changedType = ExtraBindings.TypeAt(DungeonSettlers10SlotsMod.ExtraCount - 1);
        var replaced = false;
        for (var i = 0; i < changed.Bindings.Count; i++)
            if (changed.Bindings[i].InputType == changedType && changed.Bindings[i].SlotIndex == 1)
            {
                changed.Bindings[i] = new KeyBindingData(changedType, 1, KeyCode.F12);
                replaced = true;
            }
        if (!replaced) changed.Bindings.Add(new KeyBindingData(changedType, 1, KeyCode.F12));
        settings.SetKeySettingData(changed);
        Require(settings.TryGetBinding(changedType, 1, out var assigned) && assigned.KeyCode == KeyCode.F12, "native rebinding of slot 10, second column");
        var reloadedSettings = new KeySetting(settings.GetKeySettingData());
        Require(reloadedSettings.TryGetBinding(changedType, 1, out var restored) && restored.KeyCode == KeyCode.F12, "native binding persistence");
        reloadedSettings.ResetDefaultSetting();
        Require(!reloadedSettings.TryGetBinding(changedType, 1, out var reset) || reset.KeyCode == KeyCode.None, "native reset defaults");
        for (var i = 0; i < 4; i++) Require(settings.GetKeyCodeByType((KeyInputType)((int)KeyInputType.UseSkill_1 + i)) == originalKeys[i], "first four bindings unchanged by secondary rebind");
        DungeonSettlers10SlotsMod.Log.Msg("Native smoke checks PASS: slot 10 write/read, save/load, clear; six extra bindings; slots 11/12 excluded; both-column defaults; native rebinding, persistence, reset; original bindings; event indices.");
    }

    static void CheckLegacyDisplayBoundary()
    {
        foreach (var count in new[] { 0, 4, 10, 12 })
        {
            var saved = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray(Enumerable.Range(0, count).Select(i => "skill_" + i).ToArray());
            var view = RefreshExtraSkills.DisplaySlots(saved);
            Require(view.Length == 10 && saved.Length == count, "display-only capacity " + count);
            for (var i = 0; i < 10; i++) Require(view[i] == (i < count ? "skill_" + i : ""), "display slot " + i);
            if (count == 10) Require(view.Pointer == saved.Pointer, "normal display path makes no copy");
            var data = new QuickSlotData();
            data.Deserialize(new QuickSlotSaveData { QuickSlots = saved, ItemQuickSlotKey = "native_item" });
            var roundtrip = data.Serialize();
            Require(roundtrip.QuickSlots.Length == Math.Max(10, count), "legacy save capacity preserved");
            for (var i = 0; i < count; i++) Require(roundtrip.QuickSlots[i] == "skill_" + i, "legacy hidden keys preserved " + i);
        }
        Require(RefreshExtraSkills.DisplaySlots(null).Length == 10, "null display input safe");
        DungeonSettlers10SlotsMod.Log.Msg("Legacy display boundary PASS: null/0/4/10/12 keys, ten visible fields, hidden keys saved intact, normal path without array copy.");
    }

    static void CheckKeyLabels()
    {
        for (var digit = 0; digit <= 9; digit++)
        {
            var code = KeyCode.Alpha0 + digit;
            Require(code.ToString() == "Alpha" + digit, "reproduce raw hotbar enum text");
            Require(HotbarKeyLabels.Display(code) == digit.ToString(), "compact digit " + digit);
        }
        var settings = new KeySetting();
        var type = ExtraBindings.TypeAt(5);
        settings.BindKey(type, 0, KeyCode.None);
        Require(HotbarKeyLabels.Read(settings.Cast<IKeySettingReader>(), type) == "", "unbound label empty");
        settings.BindKey(type, 1, KeyCode.F12);
        Require(HotbarKeyLabels.Read(settings.Cast<IKeySettingReader>(), type) == "F12", "secondary-only label");
        settings.BindKey(type, 0, KeyCode.Alpha1);
        Require(HotbarKeyLabels.Read(settings.Cast<IKeySettingReader>(), type) == "1", "primary label preferred");
        settings.ResetDefaultSetting();
        Require(HotbarKeyLabels.Read(settings.Cast<IKeySettingReader>(), type) == "", "reset leaves skill ten unbound");
        DungeonSettlers10SlotsMod.Log.Msg("Native key-label checks PASS: Alpha0-9 -> 0-9; original formatter; unbound, secondary-only, primary and reset labels; no campaign settings changed.");
    }

    static void CheckContainer()
    {
        var container = new UnitQuickSlotContainer();
        var first = Il2CppSystem.Guid.NewGuid();
        var second = Il2CppSystem.Guid.NewGuid();
        container.OnSpawned(first);
        container.OnSpawned(second);
        Require(container._unitQuickSlot[first]._quickSlots.Length == 10, "native spawn capacity");
        for (var i = 0; i < 10; i++) container.AddQuickSlotAt(first, i, "probe_" + i);
        Require(container.GetAll(first).Length == 10, "native container view capacity");
        for (var i = 0; i < 10; i++)
        {
            Require(container.GetSkillAt(first, i) == "probe_" + i, "native container read/write " + i);
            Require(string.IsNullOrEmpty(container.GetSkillAt(second, i)), "GUID separation " + i);
        }
        container.SetItemQuickSlot(first, "probe_item");
        var saved = container.Serialize();
        Require(saved[first].QuickSlots.Length == 10, "native container saves ten slots");
        var loaded = new UnitQuickSlotContainer();
        loaded.Deserialize(saved);
        for (var i = 0; i < 10; i++) Require(loaded.GetSkillAt(first, i) == "probe_" + i, "native container load " + i);
        Require(loaded.GetItemQuickSlotKey(first) == "probe_item", "item slot preserved");
        // Use the same native remove/add/read operations as MoveQuickSlotRequested.
        var left = loaded.GetSkillAt(first, 0);
        var right = loaded.GetSkillAt(first, 9);
        loaded.RemoveQuickSlotAt(first, 0);
        loaded.RemoveQuickSlotAt(first, 9);
        loaded.AddQuickSlotAt(first, 9, left);
        loaded.AddQuickSlotAt(first, 0, right);
        Require(loaded.GetSkillAt(first, 9) == "probe_0" && loaded.GetSkillAt(first, 0) == "probe_9", "native first/last swap operations");
        loaded.RemoveQuickSlotAt(first, 9);
        Require(string.IsNullOrEmpty(loaded.GetSkillAt(first, 9)), "native extra-slot removal");
        loaded.Reset(first);
        Require(loaded._unitQuickSlot[first]._quickSlots.Length == 10, "native reset capacity");
        // Exercise a legacy four-slot save independently of the data deserializer hooks.
        var legacy = new UnitQuickSlotContainer();
        var legacyData = new Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Guid, QuickSlotSaveData>();
        NativeSaveDictionary.Add(legacyData, first, new QuickSlotSaveData { QuickSlots = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray(new[] { "old", "", "", "" }), ItemQuickSlotKey = "old_item" });
        legacy.Deserialize(legacyData);
        Require(legacy.GetAll(first).Length == 10 && legacy.GetSkillAt(first, 0) == "old" && legacy.GetItemQuickSlotKey(first) == "old_item", "legacy container load preserves data");
        DungeonSettlers10SlotsMod.Log.Msg("Native container regression PASS: ten slots, GUID separation, save/load, first/last swap operations, removal, reset, legacy four-slot load.");
    }

    private static void Require(bool value, string check)
    {
        if (!value) throw new InvalidOperationException("Native smoke check FAILED: " + check);
    }
}
