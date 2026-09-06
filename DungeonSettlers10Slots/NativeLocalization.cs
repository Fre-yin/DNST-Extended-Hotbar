using System.Globalization;
using HarmonyLib;
using Il2CppRefactor.Main.InputModule;
using Il2CppRefactor.Setting;
using Il2CppRefactor.UI;
using Il2CppRefactor.Util;

namespace DungeonSettlers10Slots;

internal static class ExtraBindingText
{
    internal const string SkillTemplate = "TEXTKEY_KeyInputType_UseSkill_1";
    internal const string ItemTemplate = "TEXTKEY_KeyInputType_UseItemQuickSlot_1";

    // Native SetRowData and BuildConflictInfo both format this key from the enum.
    // Our out-of-range enum values stringify as their stable numeric IDs.
    internal static string Key(int bindingId) => "TEXTKEY_KeyInputType_" + bindingId.ToString(CultureInfo.InvariantCulture);

    internal static void EnsureCurrent()
    {
        var manager = DataSheetManager.Instance;
        if (manager) Register(manager._text);
    }

    internal static void Register(TextSheet sheet)
    {
        if (sheet?._textTable == null) return;
        var table = sheet._textTable;
        string Read(string key) => table.TryGetValue(key, out var text) ? text : null;
        // Use the game's own English fallback when a translation is incomplete.
        string English(string key) => sheet._tableData != null && sheet._tableData.ContainsKey(key)
            ? sheet.GetTextByLanguage(key, LanguageType.English) : null;
        var skill = Read(SkillTemplate);
        var item = Read(ItemTemplate);
        var englishSkill = SlotText.IsResolved(skill) ? null : English(SkillTemplate);
        var englishItem = SlotText.IsResolved(item) ? null : English(ItemTemplate);
        void Set(string key, string value)
        {
            if (!table.TryGetValue(key, out var old) || old != value) table[key] = value;
        }
        for (var i = 0; i < DungeonSettlers10SlotsMod.ExtraCount; i++)
            Set(Key((int)ExtraBindings.TypeAt(i)), SlotText.Skill(skill, englishSkill, i + 5));
        for (var i = 1; i < 3; i++)
            Set(Key((int)ItemSlotInput.Key(i)), SlotText.Item(item, englishItem, i + 1));
    }
}

[HarmonyPatch(typeof(TextSheet), nameof(TextSheet.ParseMatchingLanguage))]
internal static class RegisterExtraBindingLanguage
{
    // Native parsing clears the active dictionary on every language change.
    // Register afterwards, before the game refreshes its translated UI.
    static void Postfix(TextSheet __instance) => ExtraBindingText.Register(__instance);
}

[HarmonyPatch(typeof(SubUI_KeySetting), nameof(SubUI_KeySetting.SetRowData))]
internal static class RefreshExtraBindingFonts
{
    internal static void Postfix(KeyInputType __0, SubUI_KeySettingParts __1)
    {
        if (!ExtraBindings.IsExtra(__0) && !ItemSlotInput.IsExtra(__0)) return;
        // GameOptionView.RefreshLanguage scans active children only. Binding
        // rows may be hidden in another tab; SetTextKey updates text, not fonts.
        Refresh(__1._titleText);
        Refresh(__1._firstButtonText);
        Refresh(__1._secondButtonText);
    }

    private static void Refresh(DASText text)
    {
        // Inactive cloned rows have not run Awake. RefreshOnRunTime restores
        // cached wrapping/overflow/max-lines even when those defaults are still
        // zero. Capture the prefab's settings using the native idempotent guard
        // before that first refresh; do not force Awake or invent layout values.
        text.StoreInitialDefaults();
        text.RefreshOnRunTime();
    }
}
