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

// Test the same Apply path as the live presenter, on an inactive deep clone.
// No real settings, callback or campaign input is used by these checks.
internal static class HotbarLabelChecks
{
    static readonly HashSet<int> checkedBars = new();
    internal static void Clear() => checkedBars.Clear();

    internal static void CheckOnce(QuickSlotUI source)
    {
        if (!source || source._skillSlots == null || source._skillSlots.Count != DungeonSettlers10SlotsMod.Capacity + 1) return;
        if (!checkedBars.Add(source.GetInstanceID())) return;
        try { Check(source); }
        catch (Exception ex) { DungeonSettlers10SlotsMod.Log.Error("Extra hotbar key-label audit FAILED: " + ex); }
    }

    static void Check(QuickSlotUI source)
    {
        var originalTexts = new string[source._skillSlots.Count];
        for (var i = 0; i < originalTexts.Length; i++) originalTexts[i] = TextOf(source._skillSlots[i]);
        var host = new GameObject("InactiveExtraKeyLabelProbe");
        host.SetActive(false);
        try
        {
            // Instantiate beneath an already inactive parent: no transient OnEnable,
            // no presenter, and no copied button callback is ever invoked.
            var clone = UnityEngine.Object.Instantiate(source.gameObject, host.transform).GetComponent<QuickSlotUI>();
            Require(!clone.gameObject.activeInHierarchy, "clone must stay inactive");
            Require(clone._skillSlots.Count == originalTexts.Length, "cloned slot count");
            for (var i = 1; i < clone._skillSlots.Count; i++)
            {
                Require(clone._skillSlots[i]._txtInputKey && source._skillSlots[i]._txtInputKey, "skill label exists " + i);
                Require(clone._skillSlots[i]._txtInputKey.Pointer != source._skillSlots[i]._txtInputKey.Pointer, "isolated label " + i);
            }

            var settings = new KeySetting();
            var reader = settings.Cast<IKeySettingReader>();
            var cases = Enumerable.Range(0, 10).Select(i => (KeyCode)((int)KeyCode.Alpha0 + i))
                .Concat(new[] { KeyCode.Q, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.L, KeyCode.Minus, KeyCode.Plus, KeyCode.F12 }).ToArray();
            var checks = 0;
            foreach (var code in cases)
            {
                // First only the secondary column, then primary takes precedence.
                foreach (var primary in new[] { false, true })
                {
                    settings.ResetDefaultSetting();
                    for (var extra = 0; extra < DungeonSettlers10SlotsMod.ExtraCount; extra++)
                    {
                        var type = ExtraBindings.TypeAt(extra);
                        settings.BindKey(type, 0, KeyCode.None);
                        settings.BindKey(type, 1, primary ? KeyCode.F11 : code);
                        if (primary) settings.BindKey(type, 0, code);
                    }
                    HotbarKeyLabels.Apply(clone, reader);
                    var expected = code >= KeyCode.Alpha0 && code <= KeyCode.Alpha9
                        ? ((int)code - (int)KeyCode.Alpha0).ToString() : SubUI_KeySetting.ToDisplayKey(code);
                    for (var extra = 0; extra < DungeonSettlers10SlotsMod.ExtraCount; extra++)
                    {
                        var uiIndex = DungeonSettlers10SlotsMod.FirstExtra + extra + 1;
                        var label = clone._skillSlots[uiIndex]._txtInputKey;
                        Require(label.GetText() == expected, $"skill {uiIndex}, {code}, primary={primary}");
                        Require(!label.GetUseTextKey(), "literal label, not localization key");
                        Require(label.textWrappingMode == TextWrappingModes.NoWrap, "no wrapped key names");
                        checks++;
                        if (primary && (code == KeyCode.Alpha1 || code == KeyCode.F12))
                            DungeonSettlers10SlotsMod.Log.Msg($"Extra label geometry: skill={uiIndex}, key={expected}, rectWidth={label.rectTransform.rect.width:0.##}, preferredWidth={label.GetPreferredValues(expected).x:0.##}, slotWidth={clone._skillSlots[uiIndex].GetComponent<RectTransform>().rect.width:0.##}");
                    }
                }
            }

            // Distinct values catch wrong index routing that equal-key tests cannot.
            var distinct = new[] { KeyCode.Alpha1, KeyCode.Alpha0, KeyCode.Q, KeyCode.Minus, KeyCode.Plus, KeyCode.F12 };
            settings.ResetDefaultSetting();
            for (var extra = 0; extra < distinct.Length; extra++) settings.BindKey(ExtraBindings.TypeAt(extra), 0, distinct[extra]);
            HotbarKeyLabels.Apply(clone, reader);
            for (var extra = 0; extra < distinct.Length; extra++)
                Require(clone._skillSlots[DungeonSettlers10SlotsMod.FirstExtra + extra + 1]._txtInputKey.GetText() == HotbarKeyLabels.Display(distinct[extra]), "distinct extra-slot routing " + extra);

            settings.ResetDefaultSetting();
            HotbarKeyLabels.Apply(clone, reader);
            for (var extra = 0; extra < DungeonSettlers10SlotsMod.ExtraCount; extra++)
                Require(clone._skillSlots[DungeonSettlers10SlotsMod.FirstExtra + extra + 1]._txtInputKey.GetText()
                    == "", "reset leaves extra unbound " + extra);
            var defaults = new[] { "Q", "E", "R", "T" };
            for (var i = 0; i < defaults.Length; i++) Require(clone._skillSlots[i + 1]._txtInputKey.GetText() == defaults[i], "original skill default " + i);
            Require(TextOf(clone._skillSlots[0]) == originalTexts[0], "basic attack untouched");
            for (var i = 0; i < originalTexts.Length; i++) Require(TextOf(source._skillSlots[i]) == originalTexts[i], "live UI untouched " + i);
            DungeonSettlers10SlotsMod.Log.Msg($"Extra hotbar key-label PASS: all six actual slot clones; {checks} key/column checks; digits 0-9, Q/E/R/T/L, minus, plus, F12; distinct routing; reset; original four and live UI unchanged. Inactive clones and disposable settings only; pixel appearance still requires visual playtest.");
        }
        finally { UnityEngine.Object.Destroy(host); }
    }

    static string TextOf(SkillSlotUI slot) => slot._txtInputKey ? slot._txtInputKey.GetText() : null;

    static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
    }
}
