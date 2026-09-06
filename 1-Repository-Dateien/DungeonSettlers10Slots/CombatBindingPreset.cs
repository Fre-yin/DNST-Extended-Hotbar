using HarmonyLib;
using Il2CppRefactor;
using Il2CppRefactor.Main.InputModule;
using Il2CppRefactor.Setting;
using Il2CppRefactor.UI;
using UnityEngine;

namespace DungeonSettlers10Slots;

internal static class CombatBindingPreset
{
    internal const string ApplyArgument = "--ds-apply-number-bindings";
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
        Apply(bindings);
        ApplyItems(bindings);
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

    // Explicit, one-launch opt-in only. Never overwrite a later manual rebind at
    // normal startup. The game owns the serialization and the options file.
    internal static void ApplyRequestedProfile()
    {
        var numbers = Environment.GetCommandLineArgs().Contains(ApplyArgument);
        var items = Environment.GetCommandLineArgs().Contains("--ds-apply-item-bindings");
        if (!numbers && !items) return;
        var path = Path.Combine(Application.persistentDataPath, "UserSetting.json");
        if (!File.Exists(path)) throw new InvalidOperationException("Existing user settings required for one-time preset application.");
        var backup = path + ".before-number-bindings-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + ".bak";
        File.Copy(path, backup, false);
        try
        {
            var data = SaveLoadHelper.GetUserSettingSaveData();
            var settings = new KeySetting(data.KeySettingData);
            var keys = settings.GetKeySettingData();
            if (numbers) Apply(keys.Bindings);
            if (items) ApplyItems(keys.Bindings);
            settings.SetKeySettingData(keys);
            if (numbers) Verify(settings.Cast<IKeySettingReader>());
            data.KeySettingData = settings.GetKeySettingData();
            SaveLoadHelper.SaveUserSettingFile(data);
            if (numbers) Verify(new KeySetting(SaveLoadHelper.GetUserSettingSaveData().KeySettingData).Cast<IKeySettingReader>());
            DungeonSettlers10SlotsMod.Log.Msg($"Requested binding preset saved once through native settings (numbers={numbers}, items Q/E/R={items}). Backup: " + backup);
        }
        catch
        {
            File.Copy(backup, path, true);
            throw;
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
