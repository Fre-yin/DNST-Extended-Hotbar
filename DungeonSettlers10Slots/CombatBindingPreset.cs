using HarmonyLib;
#if BEPINEX
using global::Refactor;
using global::Refactor.Main.InputModule;
using global::Refactor.Setting;
using global::Refactor.UI;
#else
using Il2CppRefactor;
using Il2CppRefactor.Main.InputModule;
using Il2CppRefactor.Setting;
using Il2CppRefactor.UI;
#endif
using UnityEngine;

namespace DungeonSettlers10Slots;

internal static class CombatBindingPreset
{
    internal static KeyBindingData[] OriginalDefaults { get; private set; }
    internal static KeyCode Digit(int index) => index == 9 ? KeyCode.Alpha0 : KeyCode.Alpha1 + index;
    internal static KeyInputType Skill(int index) => index < 4 ? KeyInputType.UseSkill_1 + index : ExtraBindings.TypeAt(index - 4);
    internal static bool IsUnit(KeyInputType type) => type >= KeyInputType.SelectUnit_1 && type <= KeyInputType.SelectUnit_10;
    internal static bool IsTarget(KeyInputType type) => IsUnit(type)
        || (type >= KeyInputType.AddSelectUnit_1 && type <= KeyInputType.AddSelectUnit_10)
        || (type >= KeyInputType.UseSkill_1 && type <= KeyInputType.UseSkill_4) || ExtraBindings.IsExtra(type)
        || type == ItemSlotInput.Key(0) || ItemSlotInput.IsExtra(type);

    internal static void ApplyDefaults(Il2CppSystem.Collections.Generic.List<KeyBindingData> bindings)
    {
        OriginalDefaults ??= bindings.ToArray();
        // Extend the native schema, never replace a native or user-selected key.
        // Binding profiles belong to the Helper's explicit, backed-up operation.
        var extra = Enumerable.Range(0, DungeonSettlers10SlotsMod.ExtraCount).Select(ExtraBindings.TypeAt)
            .Concat(Enumerable.Range(1, 2).Select(ItemSlotInput.Key));
        foreach (var type in extra)
            for (var column = 0; column < 2; column++)
                if (!bindings.ToArray().Any(b => b.InputType == type && b.SlotIndex == column))
                    bindings.Add(new KeyBindingData(type, column, KeyCode.None));
    }

    internal static void VerifyDefaults(IKeySettingReader reader)
    {
        foreach (var type in Enumerable.Range(0, DungeonSettlers10SlotsMod.ExtraCount).Select(ExtraBindings.TypeAt)
            .Concat(Enumerable.Range(1, 2).Select(ItemSlotInput.Key)))
            for (var column = 0; column < 2; column++)
                Require(reader.TryGetBinding(type, column, out var binding) && binding.KeyCode == KeyCode.None,
                    "extra default unbound: " + type + "/" + column);
        foreach (var original in OriginalDefaults)
        {
            // The game itself filters its internal developer actions.
            if ((int)original.InputType >= 65 && (int)original.InputType <= 70) continue;
            Require(reader.TryGetBinding(original.InputType, original.SlotIndex, out var kept)
                && kept.KeyCode == original.KeyCode && kept.ModifierKey == original.ModifierKey
                && kept.IsKeyDown == original.IsKeyDown, "native default retained: " + original.InputType);
        }
    }

    internal static void Apply(Il2CppSystem.Collections.Generic.List<KeyBindingData> bindings)
    {
        for (var index = 0; index < 10; index++)
        {
            Set(bindings, KeyInputType.SelectUnit_1 + index, 0, Digit(index), KeyCode.LeftShift);
            Set(bindings, Skill(index), 0, Digit(index));
            Set(bindings, KeyInputType.SelectUnit_1 + index, 1, KeyCode.None);
            Set(bindings, Skill(index), 1, KeyCode.None);
            // Native hidden additive selection uses exactly Shift+digit as well.
            // Leave it unbound for this preset, rather than emitting both actions.
            for (var column = 0; column < 2; column++)
                Set(bindings, KeyInputType.AddSelectUnit_1 + index, column, KeyCode.None);
        }
    }

    internal static void ApplyItems(Il2CppSystem.Collections.Generic.List<KeyBindingData> bindings)
    {
        var keys = new[] { KeyCode.Q, KeyCode.E, KeyCode.R };
        for (var i = 0; i < 3; i++)
        {
            Set(bindings, ItemSlotInput.Key(i), 0, keys[i]);
            Set(bindings, ItemSlotInput.Key(i), 1, KeyCode.None);
        }
    }

    static void Set(Il2CppSystem.Collections.Generic.List<KeyBindingData> bindings, KeyInputType type, int column,
        KeyCode key, KeyCode modifier = KeyCode.None)
    {
        var replacement = new KeyBindingData(type, column, key, true, modifier);
        for (var i = 0; i < bindings.Count; i++)
            if (bindings[i].InputType == type && bindings[i].SlotIndex == column)
            {
                bindings[i] = replacement;
                return;
            }
        bindings.Add(replacement);
    }

    internal static void Verify(IKeySettingReader reader)
    {
        for (var index = 0; index < 10; index++)
        {
            Require(reader.TryGetBinding(Skill(index), 0, out var skill) && skill.KeyCode == Digit(index)
                && skill.ModifierKey == KeyCode.None && skill.IsKeyDown, "skill " + (index + 1));
            Require(reader.TryGetBinding(KeyInputType.SelectUnit_1 + index, 0, out var unit) && unit.KeyCode == Digit(index)
                && unit.ModifierKey == KeyCode.LeftShift && unit.IsKeyDown, "Shift+unit " + (index + 1));
            foreach (var type in new[] { Skill(index), KeyInputType.SelectUnit_1 + index })
                Require(!reader.TryGetBinding(type, 1, out var secondary) || secondary.KeyCode == KeyCode.None, "empty secondary " + type);
            for (var column = 0; column < 2; column++)
                Require(!reader.TryGetBinding(KeyInputType.AddSelectUnit_1 + index, column, out var add) || add.KeyCode == KeyCode.None,
                    "no hidden additive-selection collision " + index);
        }
    }

    static void Require(bool value, string check)
    {
        if (!value) throw new InvalidOperationException("Combat binding preset FAILED: " + check);
    }
}

[HarmonyPatch(typeof(KeySetting), nameof(KeySetting.SyncShiftSelectBindingIfNeeded))]
internal static class AvoidShiftSelectionCollision
{
    static void Prefix(KeySetting __instance, KeyInputType __0, int __1, ref KeyCode __2)
    {
        // BindKey has already stored the primary binding before calling this.
        // Native sync ignores its modifier and would recreate Shift+digit even
        // for a primary that is itself Shift+digit. Preserve normal sync when
        // the user changes the primary back to an unmodified key.
        if (__1 == 0 && CombatBindingPreset.IsUnit(__0)
            && __instance.TryGetBinding(__0, 0, out var primary) && primary.ModifierKey != KeyCode.None)
            __2 = KeyCode.None;
    }
}

[HarmonyPatch(typeof(SubUI_KeySetting), nameof(SubUI_KeySetting.SetRowData))]
internal static class ShowCombatBindingModifiers
{
    internal static string Label(IKeySettingReader reader, KeyInputType type, int column)
    {
        if (!reader.TryGetBinding(type, column, out var binding) || binding.KeyCode == KeyCode.None) return "-";
        var key = HotbarKeyLabels.Display(binding.KeyCode);
        if (binding.ModifierKey == KeyCode.None) return key;
        var modifier = binding.ModifierKey == KeyCode.LeftShift ? "Shift" : HotbarKeyLabels.Display(binding.ModifierKey);
        return modifier + "+" + key;
    }

    static void Postfix(SubUI_KeySetting __instance, KeyInputType __0, SubUI_KeySettingParts __1)
    {
        if (!CombatBindingPreset.IsTarget(__0) || __instance._keySettingReader == null) return;
        HotbarKeyLabels.Set(__1._firstButtonText, Label(__instance._keySettingReader, __0, 0));
        HotbarKeyLabels.Set(__1._secondButtonText, Label(__instance._keySettingReader, __0, 1));
    }
}
