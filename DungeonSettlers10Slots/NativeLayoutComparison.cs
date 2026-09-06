using Il2CppRefactor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSettlers10Slots;

// An opt-in visual comparison, not a four-slot fallback or a save migration.
// Keep all eleven UI objects: native RefreshSkillUI indexes them from the
// ten-element data array even when only the original frame is displayed.
internal static class NativeLayoutComparison
{
    internal const string Argument = "--ds-test-native-hotbar-layout";
    internal static readonly bool Enabled = Environment.GetCommandLineArgs().Contains(Argument);

    // Called only from QuickSlotUI.Init's postfix. Read-only: no clones, test
    // skills, disk polling, additional selection hooks or rendering changes.
    internal static void Check(QuickSlotUI bar)
    {
        try
        {
            Require(bar._skillSlots.Count == 11, "basic attack and all ten skill objects retained");
            var panel = bar._skillSlots[0].transform.parent.TryCast<RectTransform>();
            Require(panel && panel.name == "Panel_QuickSlot" && panel.parent == bar.transform, "native panel hierarchy");
            var row = bar._skillSlots[1].transform.parent;
            Require(row && row.name == "Layout_Skills" && row.parent == panel, "native skill row");
            for (var i = 1; i < bar._skillSlots.Count; i++)
                Require(bar._skillSlots[i] && bar._skillSlots[i].transform.parent == row
                    && bar._skillSlots[i].gameObject.activeSelf, "skill object retained and not hidden: " + i);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            Require(layout && layout.enabled, "native horizontal layout remains enabled");
            var background = panel.GetComponent<Image>();
            Require(background && background.sprite && background.sprite.name == "SkillFrame", "original frame sprite");
            Require(background.overrideSprite == background.sprite, "no custom frame override");
            Require(!HotbarFrameLayout.Frame, "custom texture/sprite was not loaded");
            Require(Vector2.Distance(panel.rect.size, new Vector2(362, 82)) < 0.1f, "original panel dimensions");
            Require(Vector2.Distance(panel.anchoredPosition, new Vector2(-1, 0)) < 0.1f, "original panel offset");
            DungeonSettlers10SlotsMod.Log.Msg("Native-layout A/B checks PASS: original SkillFrame 362x82 at (-1,0); native layout enabled; all eleven UI objects retained; custom frame not loaded. No slot, binding or save data changed by comparison.");
        }
        catch (Exception ex)
        {
            // A failed check invalidates this comparison, not the storage
            // patches. Do not repair the UI or change gameplay during a probe.
            DungeonSettlers10SlotsMod.Log.Warning("Native-layout A/B comparison INVALID: " + ex.Message);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
