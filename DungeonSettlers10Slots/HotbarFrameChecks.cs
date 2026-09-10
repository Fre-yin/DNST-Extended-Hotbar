using Il2CppInterop.Runtime.InteropTypes.Arrays;
#if BEPINEX
using global::Refactor.UI;
#else
using Il2CppRefactor.UI;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSettlers10Slots;

internal static class HotbarFrameChecks
{
    internal static void Check(QuickSlotUI source)
    {
        var sourceRoot = Stamp(source.GetComponent<RectTransform>());
        var sourceRects = source._skillSlots.ToArray().Select(slot => Stamp(slot.GetComponent<RectTransform>())).ToArray();
        var host = new GameObject("InactiveCustomHotbarLayoutProbe");
        host.SetActive(false);
        try
        {
            // Asymmetric side panels: center the entire frame+Y group, not the
            // frame alone and not the monitor. Check several available gaps.
            const float frameWidth = 724;
            const float rightExtension = 72; // eight-unit gap plus the 64-unit Y slot
            foreach (var edges in new[] { (468f, 1460f), (350f, 1300f), (200f, 1800f) })
            {
                var offset = HotbarGapAlignment.CenterOffset(edges.Item1, edges.Item2, 960, rightExtension);
                var groupLeft = 960 + offset - frameWidth / 2f;
                var groupRight = 960 + offset + frameWidth / 2f + rightExtension;
                Require(Mathf.Abs(groupLeft - edges.Item1 - (edges.Item2 - groupRight)) < 0.01f,
                    "equal profile/control margins for complete frame+Y");
            }
            var bar = UnityEngine.Object.Instantiate(source.gameObject, host.transform).GetComponent<QuickSlotUI>();
            Require(!bar.gameObject.activeInHierarchy, "inactive clone");
            var panel = bar._skillSlots[0].transform.parent.Cast<RectTransform>();
            var references = bar._skillSlots.ToArray().Select(slot => (slot.Pointer, slot.transform.parent.Pointer,
                Owner: slot._owner?.Pointer, Stream: slot._inputStream?.Pointer, slot._index, slot._key)).ToArray();
            var all = bar.GetComponentsInChildren<RectTransform>(true).ToArray();
            var before = all.Select(Stamp).ToArray();
            var frameBefore = panel.GetComponent<Image>().sprite;
            var layout = new HotbarFrameLayout.LayoutState(bar);
            layout.Apply();
            var once = all.Select(Stamp).ToArray();
            var applyCount = layout.ApplyCount;
            layout.Apply();
            Require(all.Select(Stamp).SequenceEqual(once), "idempotent refresh");
            Require(layout.ApplyCount == applyCount, "unchanged layout performs no UI writes");
            Require(panel.anchorMin == new Vector2(0.5f, 0) && panel.anchorMax == panel.anchorMin
                && Mathf.Abs(panel.anchoredPosition.x) < 0.01f, "panel centered in original HUD");
            Require(panel.GetComponent<Image>().sprite == HotbarFrameLayout.Frame, "user frame applied");
            Require(Mathf.Abs(panel.rect.width / panel.rect.height - HotbarFrameLayout.Frame.rect.width / HotbarFrameLayout.Frame.rect.height) < 0.001f,
                "frame aspect ratio retained");
            var previousRight = float.NegativeInfinity;
            var firstY = 0f;
            for (var i = 0; i < bar._skillSlots.Count; i++)
            {
                var slot = bar._skillSlots[i];
                var bounds = BoundsIn(panel, slot.GetComponent<RectTransform>());
                Require(bounds.xMin >= panel.rect.xMin + 9.9f && bounds.xMax <= panel.rect.xMax - 9.9f, "slot within frame " + i);
                Require(bounds.yMin >= panel.rect.yMin && bounds.yMax <= panel.rect.yMax, "slot height within frame " + i);
                Require(bounds.xMin >= previousRight - 0.01f, "slot hit regions do not overlap " + i);
                previousRight = bounds.xMax;
                if (i == 0) firstY = bounds.center.y;
                else Require(Mathf.Abs(bounds.center.y - firstY) < 0.01f, "common skill baseline");
                Require(references[i] == (slot.Pointer, slot.transform.parent.Pointer, slot._owner?.Pointer,
                    slot._inputStream?.Pointer, slot._index, slot._key), "native slot identity/owner/input unchanged " + i);
            }
            if (bar._itemQuickSlot)
            {
                var itemSlot = bar._itemQuickSlot.GetComponent<RectTransform>();
                Require(BoundsIn(panel, itemSlot).xMin >= panel.rect.xMax, "Y item slot outside frame");
                var originalPosition = itemSlot.anchoredPosition;
                var originalAnchors = (itemSlot.anchorMin, itemSlot.anchorMax);
                // Exercise both native phases explicitly on this inactive clone:
                // prefab child centered at 0, then layout child with left padding.
                foreach (var centerX in new[] { 0f, itemSlot.rect.width / 2 + 4 })
                {
                    itemSlot.anchorMin = itemSlot.anchorMax = new Vector2(0, 0);
                    itemSlot.anchoredPosition = new Vector2(centerX, originalPosition.y);
                    layout.Apply(force: true);
                    Require(Mathf.Abs(BoundsIn(panel, itemSlot).xMin - panel.rect.xMax - ItemSlotUI.FrameGap) < 0.05f,
                        "item left edge keeps the compact gap in both native layout phases");
                }
                itemSlot.anchorMin = originalAnchors.Item1; itemSlot.anchorMax = originalAnchors.Item2;
                itemSlot.anchoredPosition = originalPosition;
            }
            // Nonzero offsets exercise the actual RectTransforms, not only
            // centering arithmetic. The Y edge must follow the whole group.
            foreach (var offset in new[] { 137f, -125f })
            {
                layout.Apply(offset);
                Require(Mathf.Abs(panel.anchoredPosition.x - offset) < 0.01f, "gap offset applied");
                if (bar._itemQuickSlot)
                    Require(Mathf.Abs(BoundsIn(panel, bar._itemQuickSlot.GetComponent<RectTransform>()).xMin
                        - panel.rect.xMax - 8) < 0.05f, "Y follows nonzero gap offset");
                var positioned = all.Select(Stamp).ToArray();
                layout.Apply(offset);
                Require(all.Select(Stamp).SequenceEqual(positioned), "gap offset idempotent");
            }
            layout.Restore();
            Require(all.Select(Stamp).SequenceEqual(before), "all touched rects restored");
            Require(panel.GetComponent<Image>().sprite == frameBefore, "frame restored");
            // Reject an unknown hierarchy before writing any presentation fields.
            var last = bar._skillSlots[10].transform;
            var parent = last.parent;
            last.SetParent(panel, false);
            var guardBefore = all.Select(Stamp).ToArray();
            var rejected = false;
            try { _ = new HotbarFrameLayout.LayoutState(bar); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected && all.Select(Stamp).SequenceEqual(guardBefore), "unknown hierarchy rejected without mutation");
            last.SetParent(parent, false);
            Require(Stamp(source.GetComponent<RectTransform>()) == sourceRoot
                && source._skillSlots.ToArray().Select(slot => Stamp(slot.GetComponent<RectTransform>())).SequenceEqual(sourceRects), "live UI untouched by tests");
            DungeonSettlers10SlotsMod.Log.Msg("Custom frame layout PASS: inactive native clone; 11 unchanged slots fit frame, no hitbox overlap, equal frame+Y gap margins, nonzero offsets, aspect ratio, Y outside in both layout phases, idempotence, native identity/owner/input retained, rollback and unknown-hierarchy guard. Campaign and other HUD panels untouched.");
        }
        finally { UnityEngine.Object.Destroy(host); }
    }

    static (Vector2, Vector2, Vector2, Vector2, Vector2, Vector3) Stamp(RectTransform rect) =>
        (rect.anchorMin, rect.anchorMax, rect.pivot, rect.anchoredPosition, rect.sizeDelta, rect.localScale);

    static Rect BoundsIn(RectTransform relative, RectTransform rect)
    {
        var corners = new Il2CppStructArray<Vector3>(4);
        rect.GetWorldCorners(corners);
        var a = relative.InverseTransformPoint(corners[0]);
        var b = relative.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
    }

    static void Require(bool condition, string check)
    {
        if (!condition) throw new InvalidOperationException("Custom frame layout check FAILED: " + check);
    }
}
