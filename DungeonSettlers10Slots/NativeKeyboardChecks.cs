using Il2CppInterop.Runtime;
#if BEPINEX
using global::Refactor.Main;
using global::Refactor.Main.Event;
using global::Refactor.Main.InputModule;
using global::Refactor.Map;
using global::Refactor.Setting;
using global::Refactor.UI;
using global::Refactor.View;
#else
using Il2CppRefactor.Main;
using Il2CppRefactor.Main.Event;
using Il2CppRefactor.Main.InputModule;
using Il2CppRefactor.Map;
using Il2CppRefactor.Setting;
using Il2CppRefactor.UI;
using Il2CppRefactor.View;
#endif
using UnityEngine;

namespace DungeonSettlers10Slots;

internal static class NativeKeyboardChecks
{
    internal static bool IsRunning { get; private set; }
    internal static InputEventFactory IsolatedFactory()
    {
        // Only IsActivated and HasKeyLockedUI are used for skill-key conversion.
        // Initialize their own empty collections, without constructing/registering
        // a real UI factory or presenter in the game's dependency system.
        var popups = Il2CppSystem.Runtime.Serialization.FormatterServices.GetUninitializedObject(Il2CppType.Of<PopUpViewContainer>()).Cast<PopUpViewContainer>();
        popups._ActiveUIs_k__BackingField = new Il2CppSystem.Collections.Generic.Dictionary<PopUpType, UI_Base>();
        popups._KeyLockUIs_k__BackingField = new Il2CppSystem.Collections.Generic.HashSet<PopUpType>();
        return new InputEventFactory { _currentMap = (MapType)0, _popUpUIs = popups.Cast<IViewContainer<PopUpType>>() };
    }

    internal static void Run()
    {
        IsRunning = true;
        try { RunChecks(); }
        finally { IsRunning = false; }
    }

    static void RunChecks()
    {
        var factory = IsolatedFactory();
        for (var uiIndex = 0; uiIndex <= 4; uiIndex++)
        {
            var native = factory.ConvertKeyInputToEventData(KeyInputType.UseSkill_0 + uiIndex)?.TryCast<UseQuickSlotSkillRequested>();
            Require(native != null && native.Index == uiIndex, "native basic/skill key index " + uiIndex);
        }
        var previousFirstExtra = new UseQuickSlotSkillRequested(DungeonSettlers10SlotsMod.FirstExtra);
        Require(previousFirstExtra.Index == factory.ConvertKeyInputToEventData(KeyInputType.UseSkill_4).Cast<UseQuickSlotSkillRequested>().Index,
            "prior extra mapping incorrectly addresses native skill 4");
        DungeonSettlers10SlotsMod.Log.Msg("Keyboard regression reproduced: old first-extra event index 4 equals native skill 4 (storage 3); skill 5 requires event index 5 (storage 4).");

        var keysAllowed = true;
        Il2CppSystem.Func<bool> yes = (Il2CppSystem.Func<bool>)(() => true);
        Il2CppSystem.Func<bool> no = (Il2CppSystem.Func<bool>)(() => false);
        var condition = new InputCondition
        {
            IsPopUpActiveFunc = no, IsPlayerUnitSelectedFunc = yes, IsCampaignStartedFunc = yes,
            IsNoTooltipHeldFunc = yes, IsCheatConsoleClosedFunc = yes, IsResearchUIKeyInputAllowedFunc = yes,
            KeyAllowedFunc = (Il2CppSystem.Func<bool>)(() => keysAllowed), HasNoticeFunc = no, LoadAllowedFunc = yes
        };
        var filter = new InputFilter(condition);
        var settings = new KeySetting();
        // A deliberately minimal, disposable keymap isolates this route from
        // unrelated commands that might share L in the original defaults.
        settings._keyMapping.Clear();
        settings.RebuildInputIndex();
        settings.SyncSnapshot();
        var reader = settings.Cast<IKeySettingReader>();
        var stream = new InputStream();
        var dispatcher = new KeyInputDispatcher(reader, stream);
        // Its normal constructor injects real world dependencies. Populate only
        // the fields read by AddKeyInputAsEventData; never dispatch its output.
        var binding = Il2CppSystem.Runtime.Serialization.FormatterServices.GetUninitializedObject(Il2CppType.Of<InputBinding>()).Cast<InputBinding>();
        binding._keySetting = reader;
        binding._inputStream = stream;
        binding._inputFilter = filter;
        binding._eventDataFactory = factory;
        binding._observerMapper = new InputObserverMapper();
        binding._viewEventFactory = new InputViewEventFactory();
        binding._keyInputs = new Il2CppSystem.Collections.Generic.List<KeyInputType>();
        var events = new Il2CppSystem.Collections.Generic.HashSet<IEventData>();
        var views = new Il2CppSystem.Collections.Generic.HashSet<IViewUpdateData>();

        void ConvertKey(KeyCode key, KeyCode modifier = KeyCode.None)
        {
            events.Clear(); views.Clear(); stream.ClearInputs(); binding._keyInputs.Clear();
            dispatcher.ClearInputStream();
            dispatcher._currentModifierKeyCode = modifier;
            dispatcher._inputKeyDownStream.Add(key);
            dispatcher.AddKeyInputToInputStream();
            Require(stream.KeyInputs.Count == 1, "native dispatcher emitted key-down: " + key);
            binding.AddKeyInputAsEventData(events, views);
        }

        void ConvertL() => ConvertKey(KeyCode.L);

        void ExpectSkill(int uiIndex, string check)
        {
            Require(events.Count == 1, "one event: " + check);
            var iterator = events.GetEnumerator();
            Require(iterator.MoveNext(), "event exists: " + check);
            var skill = iterator.Current.TryCast<UseQuickSlotSkillRequested>();
            Require(skill != null && skill.Index == uiIndex, "correct skill index: " + check);
        }

        for (var extra = 0; extra < DungeonSettlers10SlotsMod.ExtraCount; extra++)
        {
            if (extra > 0) settings.BindKey(ExtraBindings.TypeAt(extra - 1), 0, KeyCode.None);
            settings.BindKey(ExtraBindings.TypeAt(extra), 0, KeyCode.L);
            Require(dispatcher._keyDowns.Any(code => code == KeyCode.L), "native dispatcher refreshed after rebinding");
            ConvertL();
            Require(events.Count == 1, "one native event for L skill " + (extra + 5));
            var enumerator = events.GetEnumerator();
            Require(enumerator.MoveNext(), "event exists");
            var skill = enumerator.Current.TryCast<UseQuickSlotSkillRequested>();
            Require(skill != null && skill.Index == extra + 5, "L routes to UI skill " + (extra + 5));
            keysAllowed = false;
            ConvertL();
            Require(events.Count == 0, "native key gate blocks extra " + (extra + 5));
            keysAllowed = true;
        }

        filter.BlockPhysicalInputs();
        ConvertL();
        Require(events.Count == 0, "global physical-input block respected");
        filter.UnblockPhysicalInputs();
        var popups = factory._popUpUIs.Cast<PopUpViewContainer>();
        popups.KeyLockUIs.Add((PopUpType)0);
        ConvertL();
        Require(events.Count == 0, "native popup key lock respected");
        popups.KeyLockUIs.Clear();
        factory._currentMap = (MapType)(-1);
        ConvertL();
        Require(events.Count == 0, "native no-map guard respected");
        factory._currentMap = (MapType)0;
        settings.BindKey(ExtraBindings.TypeAt(5), 0, KeyCode.None);
        ConvertL();
        Require(events.Count == 0, "unbound key emits no skill event");
        DungeonSettlers10SlotsMod.Log.Msg("Native keyboard pipeline PASS: L key-down buffer -> dispatcher -> native binding/index lookup -> input filter -> factory; all six extra skills have event indices 5-10. Key gate, global block, popup lock, no map and unbound reject input. Isolated buffers/settings only; actual OS keypress and campaign combat still require playtest.");

        // Sweep every distinct Unity keyboard enum value; mouse/controller values
        // start at Mouse0 and are outside this keyboard test. This injects only
        // into our disposable dispatcher, never the OS or a campaign input stream.
        var codes = Enum.GetValues<KeyCode>().Distinct()
            .Where(code => code > KeyCode.None && code < KeyCode.Mouse0).ToArray();
        var defaults = new KeySetting();
        var defaultKeys = defaults.GetAllKeys((Il2CppSystem.Func<KeyInput, bool>)(_ => true)).ToArray();
        var spareCodes = codes.Where(code => !defaultKeys.Contains(code)
            && ((code >= KeyCode.A && code <= KeyCode.Z) || (code >= KeyCode.F1 && code <= KeyCode.F15))).Take(2).ToArray();
        Require(spareCodes.Length == 2, "two originally unbound keys for save/reset probes");
        var attempts = 0;
        for (var extra = 0; extra < DungeonSettlers10SlotsMod.ExtraCount; extra++)
        {
            var type = ExtraBindings.TypeAt(extra);
            var uiIndex = extra + 5;
            for (var column = 0; column < 2; column++)
            {
                foreach (var code in codes)
                {
                    settings.BindKey(type, column, code);
                    Require(dispatcher._keyDowns.Any(key => key == code), "new key registered with dispatcher");
                    ConvertKey(code);
                    ExpectSkill(uiIndex, $"skill {uiIndex}, column {column}, key {code}");
                    settings.BindKey(type, column, KeyCode.None);
                    ConvertKey(code);
                    Require(events.Count == 0, $"cleared skill {uiIndex}, column {column}, key {code}");
                    attempts += 2;
                }
                foreach (var modifier in new[] { KeyCode.LeftShift, KeyCode.LeftControl, KeyCode.LeftAlt })
                {
                    settings.BindKey(type, column, KeyCode.L, true, modifier);
                    ConvertKey(KeyCode.L, modifier);
                    ExpectSkill(uiIndex, $"skill {uiIndex}, column {column}, modifier {modifier}");
                    ConvertKey(KeyCode.L);
                    Require(events.Count == 0, "modifier required");
                    ConvertKey(KeyCode.L, modifier == KeyCode.LeftAlt ? KeyCode.LeftShift : KeyCode.LeftAlt);
                    Require(events.Count == 0, "wrong modifier rejected");
                    settings.BindKey(type, column, KeyCode.None);
                }
            }

            // Both columns must work, even though the hotbar displays only one.
            settings.BindKey(type, 0, KeyCode.L);
            settings.BindKey(type, 1, KeyCode.F12);
            ConvertL(); ExpectSkill(uiIndex, "primary with secondary present");
            ConvertKey(KeyCode.F12); ExpectSkill(uiIndex, "secondary with primary present");
            settings.BindKey(type, 0, KeyCode.F11);
            ConvertL(); Require(events.Count == 0, "old key removed after rebinding");
            ConvertKey(KeyCode.F11); ExpectSkill(uiIndex, "new primary after rebinding");
            ConvertKey(KeyCode.F12); ExpectSkill(uiIndex, "secondary preserved after rebinding primary");
            settings.BindKey(type, 0, KeyCode.L);
            settings.BindKey(type, 1, KeyCode.L);
            ConvertL(); ExpectSkill(uiIndex, "same action in both columns emits only once");

            settings.BindKey(type, 0, spareCodes[0]);
            settings.BindKey(type, 1, spareCodes[1]);
            var reloaded = new KeySetting(settings.GetKeySettingData());
            binding._keySetting = reloaded.Cast<IKeySettingReader>();
            dispatcher.UpdateKeySetting(binding._keySetting);
            foreach (var key in spareCodes) { ConvertKey(key); ExpectSkill(uiIndex, "binding round trip " + key); }
            reloaded.ResetDefaultSetting();
            dispatcher.UpdateKeySetting(binding._keySetting);
            CombatBindingPreset.VerifyDefaults(reloaded.Cast<IKeySettingReader>());
            foreach (var key in spareCodes) { ConvertKey(key); Require(events.Count == 0, "reset key inactive " + key); }
            binding._keySetting = reader;
            dispatcher.UpdateKeySetting(reader);
            settings.BindKey(type, 0, KeyCode.None);
            settings.BindKey(type, 1, KeyCode.None);
            DungeonSettlers10SlotsMod.Log.Msg($"Keyboard matrix skill {uiIndex} PASS: {codes.Length} keyboard codes in both columns, unbind, rebind, modifiers, duplicate-column deduplication, native settings reload/reset.");
        }

        // All six different bindings active at once, to detect crossed indices.
        for (var extra = 0; extra < DungeonSettlers10SlotsMod.ExtraCount; extra++)
            settings.BindKey(ExtraBindings.TypeAt(extra), 0, KeyCode.A + extra);
        for (var extra = 0; extra < DungeonSettlers10SlotsMod.ExtraCount; extra++)
        {
            ConvertKey(KeyCode.A + extra);
            ExpectSkill(extra + 5, "simultaneous distinct slot bindings");
        }
        DungeonSettlers10SlotsMod.Log.Msg($"Native keyboard matrix PASS: {codes.Length} distinct Unity keyboard codes x 6 extra skills x 2 columns; {attempts} bound/unbound event assertions; both-column, rebind, 36 modifier combinations, duplicate-column, native reload/reset and simultaneous distinct binding checks. No real settings, OS keypresses or campaign actions changed.");

        // Full default keymap, not a hand-picked subset: Shift must select exactly
        // one unit, while the same bare digit invokes exactly one matching skill.
        var combat = new KeySetting();
        CombatBindingPreset.VerifyDefaults(combat.Cast<IKeySettingReader>());
        var explicitProfile = combat.GetKeySettingData();
        CombatBindingPreset.Apply(explicitProfile.Bindings);
        CombatBindingPreset.ApplyItems(explicitProfile.Bindings);
        combat.SetKeySettingData(explicitProfile);
        CombatBindingPreset.Verify(combat.Cast<IKeySettingReader>());
        foreach (var original in CombatBindingPreset.OriginalDefaults.Where(value => !CombatBindingPreset.IsTarget(value.InputType)))
        {
            // DS_B.0.4.12 ApplyBindings (RVA 5F2300, branch 5F271B)
            // deliberately discards input IDs 0x41..0x46, although the raw
            // factory still emits three of those developer bindings. Assert
            // their removal instead of mistaking native filtering for damage.
            if (NativeFiltersDeveloperBinding(original.InputType))
            {
                Require(!combat.TryGetBinding(original.InputType, original.SlotIndex, out _),
                    "native developer binding remains filtered: " + original.InputType);
                continue;
            }
            Require(combat.TryGetBinding(original.InputType, original.SlotIndex, out var kept)
                && kept.KeyCode == original.KeyCode && kept.ModifierKey == original.ModifierKey && kept.IsKeyDown == original.IsKeyDown,
                "unrelated native default preserved: " + original.InputType);
        }
        CheckNativeDeveloperFiltering(combat);
        // Reapply and round-trip to catch collisions introduced by native load/reset.
        var combatData = combat.GetKeySettingData();
        var count = combatData.Bindings.Count;
        CombatBindingPreset.Apply(combatData.Bindings);
        Require(combatData.Bindings.Count == count, "preset idempotent without duplicate entries");
        combat = new KeySetting(combatData);
        binding._keySetting = combat.Cast<IKeySettingReader>();
        dispatcher.UpdateKeySetting(binding._keySetting);
        void ExpectItem(int slot, string check, int expectedCount = 1)
        {
            var itemCount = 0;
            var iterator = events.GetEnumerator();
            while (iterator.MoveNext())
            {
                var evt = iterator.Current;
                if (evt.TryCast<UseItemQuickSlotRequested>() != null)
                {
                    itemCount++;
                    Require(ItemSlotInput.Take(evt.Cast<Il2CppSystem.Object>()) == slot, "item target: " + check);
                }
                else
                {
                    // Q/E also emit the native construction-rotation request.
                    // Its construction-only handler owns applicability; emitting
                    // it is not a second item use or a character-selection event.
                    Require(evt.TryCast<SelectConstructionRotateRequested>() != null, "only native construction context shares Q/E: " + check);
                }
            }
            Require(itemCount == expectedCount, "exact item event count: " + check);
        }
        for (var slot = 0; slot < 3; slot++)
        {
            var key = new[] { KeyCode.Q, KeyCode.E, KeyCode.R }[slot];
            ConvertKey(key); ExpectItem(slot, "full default map QER");
            keysAllowed = false;
            ConvertKey(key); ExpectItem(slot, "item key gate", 0);
            keysAllowed = true;
            popups.KeyLockUIs.Add((PopUpType)0);
            ConvertKey(key); ExpectItem(slot, "item popup lock", 0);
            popups.KeyLockUIs.Clear();
            combat.BindKey(ItemSlotInput.Key(slot), 1, KeyCode.F12);
            ConvertKey(KeyCode.F12); ExpectItem(slot, "item secondary binding");
            combat.BindKey(ItemSlotInput.Key(slot), 1, KeyCode.None);
        }
        DungeonSettlers10SlotsMod.Log.Msg("Item keyboard pipeline PASS: native dispatcher/filter/observer/factory; QER in complete keymap, all three slots, secondary F12, key gate and popup locks.");
        void CheckCombatRoutes()
        {
            CombatBindingPreset.Verify(binding._keySetting);
            for (var value = KeyInputType.TEST_SpawnLittleItems; value <= KeyInputType.TEST_TurnOnOffUI; value++)
                for (var column = 0; column < 2; column++)
                    Require(!binding._keySetting.TryGetBinding(value, column, out _), "developer filter retained after reload/reset: " + value);
            for (var index = 0; index < 10; index++)
            {
                ConvertKey(CombatBindingPreset.Digit(index));
                ExpectSkill(index + 1, "number preset bare digit");
                ConvertKey(CombatBindingPreset.Digit(index), KeyCode.LeftShift);
                Require(events.Count == 1, "Shift+digit produces only one event");
                var iterator = events.GetEnumerator();
                Require(iterator.MoveNext(), "Shift selection event exists");
                var select = iterator.Current.TryCast<SelectPlayerUnitRequested>();
                Require(select != null && select.Index == index, "Shift selects expected unit, not skill/additive selection");
                Require(ShowCombatBindingModifiers.Label(binding._keySetting, KeyInputType.SelectUnit_1 + index, 0)
                    == "Shift+" + ((index + 1) % 10), "modifier visible in options");
                Require(ShowCombatBindingModifiers.Label(binding._keySetting, CombatBindingPreset.Skill(index), 0)
                    == ((index + 1) % 10).ToString(), "compact skill digit in options");
            }
        }
        CheckCombatRoutes();
        combat.ResetDefaultSetting();
        CombatBindingPreset.VerifyDefaults(combat.Cast<IKeySettingReader>());
        // Reset must NOT reapply a preset. Apply it only to this disposable audit object.
        var requested = combat.GetKeySettingData();
        CombatBindingPreset.Apply(requested.Bindings);
        CombatBindingPreset.ApplyItems(requested.Bindings);
        combat.SetKeySettingData(requested);
        dispatcher.UpdateKeySetting(binding._keySetting);
        CheckCombatRoutes();
        // Rebinding a modified unit key must not resurrect the hidden Shift key.
        combat.BindKey(KeyInputType.SelectUnit_1, 0, KeyCode.F11, true, KeyCode.LeftShift);
        Require(combat.TryGetBinding(KeyInputType.AddSelectUnit_1, 0, out var additive) && additive.KeyCode == KeyCode.None,
            "modified rebind does not recreate hidden collision");
        combat.BindKey(KeyInputType.SelectUnit_1, 0, KeyCode.F11);
        Require(combat.TryGetBinding(KeyInputType.AddSelectUnit_1, 0, out additive) && additive.KeyCode == KeyCode.F11
            && additive.ModifierKey == KeyCode.LeftShift, "unmodified rebind preserves original additive behavior");
        // Normal startup/loading must respect subsequent manual customization.
        combat.BindKey(CombatBindingPreset.Skill(4), 0, KeyCode.L);
        var custom = new KeySetting(combat.GetKeySettingData());
        Require(custom.GetKeyCodeByType(CombatBindingPreset.Skill(4)) == KeyCode.L, "later customization survives native reload");
        DungeonSettlers10SlotsMod.Log.Msg("Combat number preset PASS: full native defaults and reload/reset; 40 exclusive digit/Shift-digit event checks; all 10 skills and unit indices; visible modifier labels; no hidden additive collisions; modified/unmodified rebind sync; later customization retained; unrelated defaults (including Tab/F10, attack and building controls) preserved. Item keys use requested QER. No campaign events dispatched.");
    }

    static bool NativeFiltersDeveloperBinding(KeyInputType type) =>
        type >= KeyInputType.TEST_SpawnLittleItems && type <= KeyInputType.TEST_TurnOnOffUI;

    static void CheckNativeDeveloperFiltering(KeySetting combat)
    {
        var raw = CombatBindingPreset.OriginalDefaults;
        Require(raw.Any(value => value.InputType == KeyInputType.TEST_SpawnLittleItems)
            && !combat.TryGetBinding(KeyInputType.TEST_SpawnLittleItems, 0, out _),
            "reproduce raw-default/effective-default difference introduced by 0.4.12");

        // Independently require the mod's preset to leave the complete raw
        // non-target list unchanged, including the filtered developer entries.
        var copy = new Il2CppSystem.Collections.Generic.List<KeyBindingData>();
        foreach (var value in raw) copy.Add(value);
        static (KeyInputType, int, KeyCode, bool, KeyCode) Stamp(KeyBindingData value) =>
            (value.InputType, value.SlotIndex, value.KeyCode, value.IsKeyDown, value.ModifierKey);
        var before = raw.Where(value => !CombatBindingPreset.IsTarget(value.InputType)).Select(Stamp).ToArray();
        CombatBindingPreset.Apply(copy);
        Require(copy.ToArray().Where(value => !CombatBindingPreset.IsTarget(value.InputType)).Select(Stamp).SequenceEqual(before),
            "all raw non-target defaults preserved exactly by preset");

        // Even explicitly saved developer bindings must still be rejected by
        // the native loader. Probe both columns without touching real settings.
        var data = combat.GetKeySettingData();
        for (var type = KeyInputType.TEST_SpawnLittleItems; type <= KeyInputType.TEST_TurnOnOffUI; type++)
            for (var column = 0; column < 2; column++)
                data.Bindings.Add(new KeyBindingData(type, column, column == 0 ? KeyCode.F11 : KeyCode.F12));
        var loaded = new KeySetting(data);
        for (var type = KeyInputType.TEST_SpawnLittleItems; type <= KeyInputType.TEST_TurnOnOffUI; type++)
            for (var column = 0; column < 2; column++)
                Require(!loaded.TryGetBinding(type, column, out _), "native loader discards saved developer binding: " + type);
        CombatBindingPreset.Verify(loaded.Cast<IKeySettingReader>());
        DungeonSettlers10SlotsMod.Log.Msg("Native 0.4.12 developer-filter PASS: old raw-default assumption reproduced; six internal developer actions remain absent in both columns after native load; all raw non-target defaults unchanged. No developer keys re-enabled, no real settings written.");
    }

    static void Require(bool value, string description)
    {
        if (!value) throw new InvalidOperationException("Native keyboard check FAILED: " + description);
    }
}
