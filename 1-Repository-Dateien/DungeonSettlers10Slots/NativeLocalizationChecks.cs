using Il2CppRefactor.Main.InputModule;
using Il2CppRefactor.Setting;
using Il2CppRefactor.UI;
using Il2CppRefactor.UI.Util;
using Il2CppRefactor.Util;
using Il2CppTMPro;
using UnityEngine;

namespace DungeonSettlers10Slots;

// Explicit one-shot test only. Separate native text sheets and inactive UI
// objects; never select a language, save settings, or dispatch campaign input.
internal static class NativeLocalizationChecks
{
    private static readonly bool requested = Environment.GetCommandLineArgs().Contains("--ds-run-localization-audits");
    private static bool pending = requested;

    internal static void RunIfRequested()
    {
        if (!pending) return;
        var manager = DataSheetManager.Instance;
        if (!manager || !manager.IsReady || manager._text?._table == null) return;
        pending = false;
        try { Run(manager._text); }
        catch (Exception ex) { DungeonSettlers10SlotsMod.Log.Error("Native localization audit FAILED: " + ex); }
    }

    private static void Run(TextSheet source)
    {
        var originalLanguage = LanguageSetting.GetCurrentLanguage();
        var originalText = source.GetText(ExtraBindingText.SkillTemplate);
        var originalCount = source._textTable.Count;
        var types = Enumerable.Range(0, DungeonSettlers10SlotsMod.ExtraCount).Select(ExtraBindings.TypeAt)
            .Concat(new[] { ItemSlotInput.Key(1), ItemSlotInput.Key(2) }).ToArray();
        var sheet = new TextSheet();
        sheet.Init(source._table);
        var count = 0;
        foreach (var language in Enum.GetValues<LanguageType>())
        {
            sheet.ParseMatchingLanguage(language); // actual Harmony registration must run
            foreach (var type in types)
            {
                var key = ExtraBindingText.Key((int)type);
                var slot = ExtraBindings.IsExtra(type) ? (int)type - ExtraBindings.FirstId + 5 : (int)type - 12100 + 1;
                var expected = ExtraBindings.IsExtra(type)
                    ? SlotText.Skill(sheet.GetText(ExtraBindingText.SkillTemplate), null, slot)
                    : SlotText.Item(sheet.GetText(ExtraBindingText.ItemTemplate), null, slot);
                Require(sheet.GetTextExists(key) && sheet.GetText(key) == expected, language + " / " + key);
                count++;
            }
            DungeonSettlers10SlotsMod.Log.Msg("Native localization language PASS: " + language
                + "; " + sheet.GetText(ExtraBindingText.Key(12010)) + "; " + sheet.GetText(ExtraBindingText.Key(12102)));
        }
        Require(LanguageSetting.GetCurrentLanguage() == originalLanguage
            && source.GetText(ExtraBindingText.SkillTemplate) == originalText
            && source._textTable.Count == originalCount, "live language/text table unchanged");

        var root = new GameObject("HotbarLocalizationAudit");
        root.SetActive(false);
        try
        {
            T Child<T>(string name) where T : Component
            {
                var go = new GameObject(name);
                go.transform.SetParent(root.transform, false);
                return go.AddComponent<T>();
            }
            // Reproduce the former failure on an isolated, never-awakened label:
            // translated text/font assertions alone miss an invisible TMP mesh.
            var baseline = Child<DASText>("BeforeAwakeBaseline");
            ConfigureLayout(baseline);
            Require(!baseline._hasInitDefaults, "baseline has not run Awake");
            baseline.RefreshOnRunTime();
            Require(baseline.maxVisibleLines == 0, "unprepared native refresh reproduces zero visible lines");
            DungeonSettlers10SlotsMod.Log.Msg("Native localization regression reproduced: inactive label refresh before Awake sets maxVisibleLines=0.");
            var view = root.AddComponent<SubUI_KeySetting>();
            var settings = new KeySetting();
            view._keySettingReader = settings.Cast<IKeySettingReader>();
            view._canChangeKeyBinding = true;
            view._awaitInputPopup = Child<RectTransform>("AwaitInput");
            view._awaitConflictPopup = Child<RectTransform>("AwaitConflict");
            view._awaitText = Child<DASText>("AwaitText");
            var row = Child<SubUI_KeySettingParts>("Row");
            row._titleText = Child<DASText>("Title");
            row._titleText.SetUseTextKey(true);
            row._firstButton = Child<DASButton>("Primary");
            row._secondButton = Child<DASButton>("Secondary");
            row._firstButtonText = Child<DASText>("PrimaryLabel");
            row._secondButtonText = Child<DASText>("SecondaryLabel");
            var labels = new[] { row._titleText, row._firstButtonText, row._secondButtonText };
            foreach (var label in labels) ConfigureLayout(label);
            row._firstButtonTooltip = Child<InteractableUI>("PrimaryTooltip");
            row._secondButtonTooltip = Child<InteractableUI>("SecondaryTooltip");
            var expectedFont = LanguageSetting.GetFont(row._titleText.GetStyle());
            Require(expectedFont, "current native font exists");
            var foreignFont = Enum.GetValues<LanguageType>()
                .Select(language => LanguageSetting.GetFont(row._titleText.GetStyle(), language))
                .FirstOrDefault(font => font && font.Pointer != expectedFont.Pointer);
            Require(foreignFont, "alternate native language font exists");
            foreach (var type in types)
            {
                var expected = source.GetText(ExtraBindingText.Key((int)type));
                Require(SlotText.IsResolved(expected), "active-language key resolved");
                // A null font makes TMP load its fallback immediately; use an
                // actual foreign-language font to reproduce a stale tab font.
                row._titleText.font = foreignFont;
                row._titleText.SetTextKey(ExtraBindingText.Key((int)type));
                Require(row._titleText.font?.Pointer == foreignFont.Pointer, "baseline SetTextKey retains stale font");
                view.SetRowData(type, row); // completely native, no ExtraRow override
                Require(row._titleText.text == expected && row._titleText.GetUseTextKey(), "native title " + type);
                Require(expectedFont && row._titleText.font?.Pointer == expectedFont.Pointer, "inactive row font refreshed");
                foreach (var label in labels)
                    Require(label._hasInitDefaults && label.maxVisibleLines == 1
                        && label.overflowMode == TextOverflowModes.Truncate && !label.enableWordWrapping,
                        "pre-Awake and repeated refresh preserve native label layout " + type);
                row._firstButton.onClick.Invoke();
                Require(view._isAwaitingInput && view._awaitingInputType == type && view._awaitingSlotIndex == 0, "primary callback");
                view.StopAwaitInput();
                row._secondButton.onClick.Invoke();
                Require(view._isAwaitingInput && view._awaitingInputType == type && view._awaitingSlotIndex == 1, "secondary callback");
                view.StopAwaitInput();
                var binding = settings.GetKeyCodeByType(type);
                var conflict = view.BuildConflictInfo(binding, KeyInputType.UseSkill_1, 0);
                Require(conflict.Contains(expected) && !conflict.Contains("TextKey not found"), "native conflict name " + type);
            }
        }
        finally { UnityEngine.Object.Destroy(root); }
        DungeonSettlers10SlotsMod.Log.Msg($"Native localization PASS: {count} names across {Enum.GetValues<LanguageType>().Length} languages; 8 native row titles + inactive font/layout refreshes + 16 binding callbacks + 8 conflict names in {originalLanguage}; inactive UI only, user language/settings/campaign untouched.");
    }

    private static void ConfigureLayout(DASText label)
    {
        label.maxVisibleLines = 1;
        label.overflowMode = TextOverflowModes.Truncate;
        label.enableWordWrapping = false;
    }

    // Opt-in integration check on the real cloned settings rows, after native
    // SetRowData. No popup, binding, language, or save changes are performed.
    internal static void CheckOptionsIfRequested(SubUI_KeySetting view)
    {
        if (!requested || view._rows == null || !view._rows.ContainsKey(ExtraBindings.TypeAt(0))) return;
        try
        {
            var types = Enumerable.Range(0, DungeonSettlers10SlotsMod.ExtraCount).Select(ExtraBindings.TypeAt)
                .Concat(new[] { ItemSlotInput.Key(1), ItemSlotInput.Key(2) });
            foreach (var type in types)
            {
                Require(view._rows.TryGetValue(type, out var row) && row, "real options row exists " + type);
                var expected = DataSheetManager.Instance._text.GetText(ExtraBindingText.Key((int)type));
                Require(SlotText.IsResolved(expected) && row._titleText.text == expected, "real options title " + type);
                foreach (var label in new[] { row._titleText, row._firstButtonText, row._secondButtonText })
                    Require(!string.IsNullOrEmpty(label.text) && label._hasInitDefaults && label.maxVisibleLines > 0,
                        "real options label text and visible lines " + type);
            }
            DungeonSettlers10SlotsMod.Log.Msg("Native options integration PASS: 8 real cloned rows, 24 nonempty labels with positive visible-line limits.");
        }
        catch (Exception ex) { DungeonSettlers10SlotsMod.Log.Error("Native options integration FAILED: " + ex); }
    }

    private static void Require(bool condition, string check)
    {
        if (!condition) throw new InvalidOperationException(check);
    }
}
