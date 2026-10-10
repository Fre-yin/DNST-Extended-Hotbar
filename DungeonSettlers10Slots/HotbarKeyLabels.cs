using HarmonyLib;
#if BEPINEX
using global::Refactor.Main.InputModule;
using global::Refactor.Setting;
using global::Refactor.UI;
using global::TMPro;
#else
using Il2CppRefactor.Main.InputModule;
using Il2CppRefactor.Setting;
using Il2CppRefactor.UI;
using Il2CppTMPro;
#endif
using UnityEngine;

namespace DungeonSettlers10Slots;

internal static class HotbarKeyLabels
{
    static readonly Dictionary<int, (QuickSlotUI Bar, IKeySettingReader Settings)> targets = new();
    // DS_B.0.4.29 names keys per keyboard layout (QWERTZ, AZERTY, JIS) through the
    // settings reader that the HUD and the options menu share.
    static IKeySettingReader current;
    static KeyboardKeyLabels fallback;

    internal static void Use(IKeySettingReader settings)
    {
        if (settings != null) current = settings;
    }

    internal static string Display(KeyCode key) => Display(current, key);

    internal static string Display(IKeySettingReader settings, KeyCode key)
    {
        if (key == KeyCode.None) return "";
        if (settings != null) return settings.GetDisplayKey(key);
        // No reader yet: the game's own QWERTY names (Alpha1 -> 1).
        fallback ??= new KeyboardKeyLabels();
        return fallback.GetLabel(key, KeyboardLayoutType.QWERTY);
    }

    internal static string Read(IKeySettingReader settings, KeyInputType type)
    {
        for (var column = 0; column < 2; column++)
        {
            if (!settings.TryGetBinding(type, column, out var binding) || binding.KeyCode == KeyCode.None) continue;
            var key = Display(settings, binding.KeyCode);
            return binding.ModifierKey == KeyCode.None ? key : Display(settings, binding.ModifierKey) + "+" + key;
        }
        return "";
    }

    internal static void Apply(QuickSlotUI bar, IKeySettingReader settings)
    {
        if (!bar || settings == null || bar._skillSlots == null) return;
        for (var dataIndex = 0; dataIndex < DungeonSettlers10SlotsMod.Capacity && dataIndex + 1 < bar._skillSlots.Count; dataIndex++)
        {
            var type = dataIndex < DungeonSettlers10SlotsMod.FirstExtra
                ? KeyInputType.UseSkill_1 + dataIndex
                : ExtraBindings.TypeAt(dataIndex - DungeonSettlers10SlotsMod.FirstExtra);
            Set(bar._skillSlots[dataIndex + 1]._txtInputKey, Read(settings, type));
        }
        ItemSlotUI.Labels(bar, settings);
    }

    internal static void Register(QuickSlotUI bar, IKeySettingReader settings)
    {
        if (!bar || settings == null) return;
        Use(settings);
        targets[bar.GetInstanceID()] = (bar, settings);
        Apply(bar, settings);
    }

    internal static void RefreshRegistered()
    {
        if (targets.Count == 0) return;
        foreach (var target in targets.ToArray())
        {
            if (target.Value.Bar) Apply(target.Value.Bar, target.Value.Settings);
            else targets.Remove(target.Key);
        }
    }

    internal static void Clear()
    {
        targets.Clear();
        current = null;
    }
    internal static void Prune()
    {
        foreach (var id in targets.Where(pair => !pair.Value.Bar).Select(pair => pair.Key).ToArray()) targets.Remove(id);
    }

    internal static void Set(DASText label, string text)
    {
        if (!label) return;
        if (label.textWrappingMode != TextWrappingModes.NoWrap) label.textWrappingMode = TextWrappingModes.NoWrap;
        if (label.GetUseTextKey()) label.SetUseTextKey(false);
        if (label.GetText() != text) label.SetText(text);
    }
}

[HarmonyPatch(typeof(SkillSlotUI), nameof(SkillSlotUI.InitOnInteract))]
internal static class CompactInitialSkillKey
{
    static void Prefix(ref string __1)
    {
        if (Enum.TryParse<KeyCode>(__1, out var key) && key.ToString() == __1) __1 = HotbarKeyLabels.Display(key);
    }
    static void Postfix(SkillSlotUI __instance, string __1) => HotbarKeyLabels.Set(__instance._txtInputKey, __1 ?? "");
}

[HarmonyPatch(typeof(QuickSlotUIPresenter), nameof(QuickSlotUIPresenter.OnInitializeView))]
internal static class InitialSkillKeyLabels
{
    // The view names its initial keys during this call; give them the HUD's layout.
    static void Prefix(QuickSlotUIPresenter __instance) => HotbarKeyLabels.Use(__instance._keySettingReader);

    static void Postfix(QuickSlotUIPresenter __instance)
    {
        HotbarKeyLabels.Register(__instance.View, __instance._keySettingReader);
        if (DungeonSettlers10SlotsMod.RunUiAudits) HotbarLabelChecks.CheckOnce(__instance.View);
    }
}

[HarmonyPatch]
internal static class RefreshSkillLabelsOnBindingChange
{
    static IEnumerable<System.Reflection.MethodBase> TargetMethods() => AccessTools.GetDeclaredMethods(typeof(KeySetting))
        .Where(method => method.Name is nameof(KeySetting.BindKey) or nameof(KeySetting.SetKeySettingData) or nameof(KeySetting.ResetDefaultSetting)
            or nameof(KeySetting.SetKeyboardLayout));
    static void Postfix() => HotbarKeyLabels.RefreshRegistered();
}
