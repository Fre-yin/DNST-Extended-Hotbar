using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#if BEPINEX
using global::Refactor;
using global::Refactor.UI;
#else
using Il2CppRefactor;
using Il2CppRefactor.UI;
#endif
using UnityEngine;

namespace DungeonSettlers10Slots;

internal static class HotbarGapAlignment
{
    static readonly string[] controlNames = { "Button_Gather", "Button_Cancel", "Button_PickUp", "Button_Demolition" };
    static readonly Dictionary<int, EntitySummaryUIView> summaries = new();
    static readonly Dictionary<int, ControlModeUIView> controls = new();
    static string lastReport;

    internal static void Register(EntitySummaryUIView view)
    {
        if (!view) return;
        var scene = view.gameObject.scene.handle;
        summaries[scene] = view;
        HotbarFrameLayout.ReapplyScene(scene);
    }

    internal static void Register(ControlModeUIView view)
    {
        if (!view) return;
        var scene = view.gameObject.scene.handle;
        controls[scene] = view;
        HotbarFrameLayout.ReapplyScene(scene);
    }

    internal static void Prune()
    {
        foreach (var scene in summaries.Where(pair => !pair.Value).Select(pair => pair.Key).ToArray()) summaries.Remove(scene);
        foreach (var scene in controls.Where(pair => !pair.Value).Select(pair => pair.Key).ToArray()) controls.Remove(scene);
    }

    internal static void Clear()
    {
        summaries.Clear();
        controls.Clear();
        lastReport = null;
    }

    internal static float CenterOffset(float left, float right, float rootCenter, float rightExtension) =>
        (left + right) / 2 - rootCenter - rightExtension / 2;

    internal static float Measure(QuickSlotUI bar, float frameWidth, float rightExtension)
    {
        var root = bar.GetComponent<RectTransform>();
        var scene = bar.gameObject.scene.handle;
        var left = root.rect.xMin;
        var right = root.rect.xMax;
        var hasSummary = summaries.TryGetValue(scene, out var summary);
        var hasControl = controls.TryGetValue(scene, out var control);
        var found = hasSummary && summary && hasControl && control;
        if (found)
        {
            left = BoundsIn(root, summary.GetComponent<RectTransform>()).xMax;
            var buttons = controlNames.Select(name => control.transform.Find(name)?.TryCast<RectTransform>()).ToArray();
            found = buttons.All(button => button);
            if (found) right = buttons.Min(button => BoundsIn(root, button).xMin);
        }
        if (!found || right <= left)
        {
            found = false;
            left = root.rect.xMin; right = root.rect.xMax;
        }
        var offset = CenterOffset(left, right, root.rect.center.x, rightExtension);
        var spare = right - left - frameWidth - rightExtension;
        var report = $"Hotbar gap alignment: nativeBounds={found}, left={left:0.##}, right={right:0.##}, fullWidth={frameWidth + rightExtension:0.##}, panelOffset={offset:0.##}, equalMargin={spare / 2:0.##}; profile/control transforms read-only.";
        if (report != lastReport)
        {
            lastReport = report;
            DungeonSettlers10SlotsMod.Log.Msg(report);
            if (spare < 0) DungeonSettlers10SlotsMod.Log.Warning("Hotbar exceeds the available HUD gap at this resolution/UI scale; a smaller UI scale or later compact layout is needed.");
        }
        return offset;
    }

    internal static Rect BoundsIn(RectTransform relative, RectTransform target)
    {
        var corners = new Il2CppStructArray<Vector3>(4);
        target.GetWorldCorners(corners);
        var points = corners.Select(point => relative.InverseTransformPoint(point)).ToArray();
        return Rect.MinMaxRect(points.Min(point => point.x), points.Min(point => point.y),
            points.Max(point => point.x), points.Max(point => point.y));
    }
}

[HarmonyPatch(typeof(EntitySummaryUIView), nameof(EntitySummaryUIView.Init))]
internal static class RegisterEntitySummary
{
    static void Postfix(EntitySummaryUIView __instance) => HotbarGapAlignment.Register(__instance);
}

[HarmonyPatch(typeof(ControlModeUIView), nameof(ControlModeUIView.Init))]
internal static class RegisterControlMode
{
    static void Postfix(ControlModeUIView __instance) => HotbarGapAlignment.Register(__instance);
}

[HarmonyPatch(typeof(UIScaleManager), nameof(UIScaleManager.ApplyScaleToAllRoots))]
internal static class RefreshLayoutOnUiScale
{
    static void Postfix() => HotbarFrameLayout.RequestSettle();
}
