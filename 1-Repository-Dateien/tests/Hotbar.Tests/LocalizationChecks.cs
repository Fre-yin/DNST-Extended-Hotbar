using DungeonSettlers10Slots;
using Il2CppRefactor.Util;

internal static class LocalizationChecks
{
    // Two native rows only, read from build 25124554 TextKeyTable, path ID
    // -1601464976024264656. All ten selectable LanguageType values; untranslated
    // TODO columns for non-selectable languages are not advertised as support.
    internal static readonly (string Language, string Skill, string Item, string SkillPattern, string ItemPattern)[] Rows =
    {
        ("English", "Use Skill 1", "Use Item Quick Slot 1", "Use Skill {0}", "Use Item Quick Slot {0}"),
        ("Korean", "스킬 1 사용", "아이템 퀵슬롯 단축키", "스킬 {0} 사용", "아이템 퀵슬롯 단축키 {0}"),
        ("French", "Utiliser la compétence 1", "Utiliser l'emplacement rapide d'objet 1", "Utiliser la compétence {0}", "Utiliser l'emplacement rapide d'objet {0}"),
        ("German", "Skill 1 benutzen", "Quickslot 1 nutzen", "Skill {0} benutzen", "Quickslot {0} nutzen"),
        ("Russian", "Использовать навык 1", "Использовать быстрый слот предмета 1", "Использовать навык {0}", "Использовать быстрый слот предмета {0}"),
        ("ChineseSimplified", "使用技能 1", "使用物品快捷栏 1", "使用技能 {0}", "使用物品快捷栏 {0}"),
        ("ChineseTraditional", "使用技能 1", "使用物品快捷欄 1", "使用技能 {0}", "使用物品快捷欄 {0}"),
        ("Japanese", "スキル1を使用", "アイテムクイックスロット1を使用", "スキル{0}を使用", "アイテムクイックスロット{0}を使用"),
        ("Spanish", "Usar habilidad 1", "Usar ranura rápida de objeto 1", "Usar habilidad {0}", "Usar ranura rápida de objeto {0}"),
        ("PortugueseBrazil", "Usar Habilidade 1", "Usar Item do slot rápido 1", "Usar Habilidade {0}", "Usar Item do slot rápido {0}"),
    };

    internal static void Run(Action<string, Action> test)
    {
        void Check(bool value) { if (!value) throw new Exception("localization assertion failed"); }
        foreach (var row in Rows)
            test("native language " + row.Language + ": all eight additional names", () =>
            {
                var sheet = new TextSheet();
                sheet._textTable.Add(ExtraBindingText.SkillTemplate, row.Skill);
                sheet._textTable.Add(ExtraBindingText.ItemTemplate, row.Item);
                sheet._textTable.Add("unrelated", "unchanged");
                ExtraBindingText.Register(sheet);
                for (var slot = 5; slot <= 10; slot++)
                    Check(sheet._textTable[ExtraBindingText.Key(12000 + slot)] == string.Format(row.SkillPattern, slot));
                for (var slot = 2; slot <= 3; slot++)
                    Check(sheet._textTable[ExtraBindingText.Key(12099 + slot)] == string.Format(row.ItemPattern, slot));
                Check(sheet._textTable.Count == 11);
                Check(sheet._textTable[ExtraBindingText.SkillTemplate] == row.Skill);
                Check(sheet._textTable[ExtraBindingText.ItemTemplate] == row.Item);
                Check(sheet._textTable["unrelated"] == "unchanged");
                Check(!sheet._textTable.ContainsKey(ExtraBindingText.Key(12011)));
                var snapshot = sheet._textTable.ToArray();
                ExtraBindingText.Register(sheet);
                Check(snapshot.SequenceEqual(sheet._textTable));
            });

        test("language switch replaces every extra key without stale German or cached English", () =>
        {
            var sheet = new TextSheet();
            DataSheetManager.Instance = new DataSheetManager { _text = sheet };
            try
            {
                foreach (var row in Rows.Concat(Rows.Reverse()))
                {
                    sheet._textTable.Clear(); // native ParseMatchingLanguage behavior
                    sheet._textTable[ExtraBindingText.SkillTemplate] = row.Skill;
                    sheet._textTable[ExtraBindingText.ItemTemplate] = row.Item;
                    ExtraBindingText.EnsureCurrent();
                    Check(sheet._textTable[ExtraBindingText.Key(12010)] == string.Format(row.SkillPattern, 10));
                    Check(sheet._textTable[ExtraBindingText.Key(12102)] == string.Format(row.ItemPattern, 3));
                    Check(sheet._textTable.Count == 10);
                }
            }
            finally { DataSheetManager.Instance = null; }
        });
        test("missing/TODO/error translation uses native English, then safe literal fallback", () =>
        {
            foreach (var missing in new[] { null, "", " ", "TODO", " todo ", "TEXTKEY_KeyInputType_UseSkill_1", "TextKey not found : bad" })
            {
                var sheet = new TextSheet();
                sheet._textTable[ExtraBindingText.SkillTemplate] = missing;
                sheet._textTable[ExtraBindingText.ItemTemplate] = missing;
                sheet._tableData[ExtraBindingText.SkillTemplate] = "Native English skill 1";
                sheet._tableData[ExtraBindingText.ItemTemplate] = "Native English item 1";
                ExtraBindingText.Register(sheet);
                Check(sheet._textTable[ExtraBindingText.Key(12005)] == "Native English skill 5");
                Check(sheet._textTable[ExtraBindingText.Key(12102)] == "Native English item 3");
                Check(SlotText.Skill(missing, missing, 10) == "Use Skill 10");
                Check(SlotText.Item(missing, missing, 2) == "Use Item Quick Slot 2");
            }
        });
        test("numbering preserves TMP tags, neighboring digits and culture", () =>
        {
            Check(SlotText.Skill("<color=#111111>Skill 1</color>", null, 10) == "<color=#111111>Skill 10</color>");
            Check(SlotText.Item("<size=11>Slot 1</size>", null, 3) == "<size=11>Slot 3</size>");
            Check(SlotText.Skill("12 / Skill 1", null, 5) == "12 / Skill 5");
            var previous = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("ar-SA");
                Check(ExtraBindingText.Key(12005) == "TEXTKEY_KeyInputType_12005");
                Check(SlotText.Skill("Skill 1", null, 10) == "Skill 10");
            }
            finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
        });
        test("uninitialized language services are harmless", () =>
        {
            ExtraBindingText.Register(null);
            ExtraBindingText.Register(new TextSheet { _textTable = null });
            DataSheetManager.Instance = null;
            ExtraBindingText.EnsureCurrent();
            var sheet = new TextSheet { _tableData = null };
            ExtraBindingText.Register(sheet);
            Check(sheet._textTable[ExtraBindingText.Key(12102)] == "Use Item Quick Slot 3");
        });
        test("font refresh is restricted to the eight additional rows", () =>
        {
            foreach (var id in new[] { 0, 1, 12004, 12005, 12006, 12007, 12008, 12009, 12010, 12011, 12100, 12101, 12102, 12103 })
            {
                var row = new Il2CppRefactor.UI.SubUI_KeySettingParts();
                RefreshExtraBindingFonts.Postfix((Il2CppRefactor.Main.InputModule.KeyInputType)id, row);
                var expected = (id >= 12005 && id <= 12010) || id == 12101 || id == 12102 ? 1 : 0;
                Check(row._titleText.Refreshes == expected && row._firstButtonText.Refreshes == expected
                    && row._secondButtonText.Refreshes == expected);
                Check(row._titleText.HasDefaults == (expected == 1));
            }
        });
        test("extra labels retain prefab line limits before Awake and on repeated refresh", () =>
        {
            foreach (var id in new[] { 12005, 12006, 12007, 12008, 12009, 12010, 12101, 12102 })
            {
                var row = new Il2CppRefactor.UI.SubUI_KeySettingParts();
                var labels = new[] { row._titleText, row._firstButtonText, row._secondButtonText };
                for (var i = 0; i < labels.Length; i++) labels[i].MaxVisibleLines = i + 1;
                RefreshExtraBindingFonts.Postfix((Il2CppRefactor.Main.InputModule.KeyInputType)id, row);
                for (var i = 0; i < labels.Length; i++)
                {
                    Check(labels[i].HasDefaults && labels[i].MaxVisibleLines == i + 1);
                    labels[i].MaxVisibleLines = 7; // a temporary layout change must not replace stored defaults
                }
                RefreshExtraBindingFonts.Postfix((Il2CppRefactor.Main.InputModule.KeyInputType)id, row);
                for (var i = 0; i < labels.Length; i++) Check(labels[i].MaxVisibleLines == i + 1);
            }
        });
    }
}
