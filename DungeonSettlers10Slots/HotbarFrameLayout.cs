using UnityEngine;
using UnityEngine.UI;
using Il2CppRefactor.UI;

namespace DungeonSettlers10Slots;

// Presentation only. Keep every native slot in its existing parent with its
// callbacks, owner, input stream, index, icon and cooldown objects unchanged.
internal static class HotbarFrameLayout
{
    internal const string AssetName = "SkillFrame__sharedassets0_mod_4898.png";
    static readonly Dictionary<int, LayoutState> states = new();
    static readonly HashSet<int> rejected = new();
    static Texture2D texture;
    internal static Sprite Frame { get; private set; }
    static bool loadFailed;
    static int pendingSettleFrames;

    internal static bool LoadFrame()
    {
        if (Frame && texture) return true;
        if (loadFailed) return false;
        // A collected Unity object is not a failed file load. Recreate the pair
        // if either native object was released between UI/scene lifetimes.
        ReleaseFrame();
        Texture2D candidate = null;
        try
        {
            var path = Path.Combine(Path.GetDirectoryName(typeof(HotbarFrameLayout).Assembly.Location),
                "DungeonSettlers10SlotsAssets", AssetName);
            var bytes = File.ReadAllBytes(path);
            // Bound decoding before passing an external PNG to Unity.
            if (bytes.Length < 24 || bytes.Length > 4 * 1024 * 1024
                || !bytes.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
                throw new InvalidDataException("Expected a bounded PNG frame.");
            var width = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
            var height = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
            if (width < 1 || width > 4096 || height < 1 || height > 512)
                throw new InvalidDataException("Frame dimensions outside supported range.");
            candidate = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(candidate, bytes, false)) throw new InvalidDataException("PNG decode failed.");
            candidate.filterMode = FilterMode.Point;
            candidate.wrapMode = TextureWrapMode.Clamp;
            candidate.name = "DungeonSettlers10SlotsFrameTexture";
            candidate.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Frame = Sprite.Create(candidate, new Rect(0, 0, candidate.width, candidate.height), new Vector2(0.5f, 0.5f),
                16, 0, SpriteMeshType.FullRect);
            if (!Frame) throw new InvalidOperationException("Sprite creation failed.");
            Frame.name = "DungeonSettlers10SlotsFrame";
            Frame.hideFlags = HideFlags.DontUnloadUnusedAsset;
            texture = candidate;
            // These assets have no persistent scene owner. IL2CPP does not see
            // our managed static fields as Unity asset references. Pin only the
            // two mod-owned assets; Dispose still destroys both explicitly.
            DungeonSettlers10SlotsMod.Log.Msg($"Custom hotbar frame loaded: {candidate.width}x{candidate.height}; original pixels, point filtering.");
            return true;
        }
        catch (Exception ex)
        {
            loadFailed = true;
            if (candidate) UnityEngine.Object.Destroy(candidate);
            if (Frame) UnityEngine.Object.Destroy(Frame);
            Frame = null;
            DungeonSettlers10SlotsMod.Log.Warning("Custom frame unavailable; previous hotbar presentation kept. " + ex.Message);
            return false;
        }
    }

    internal static void Apply(QuickSlotUI bar, bool force = false)
    {
        if (!bar || !LoadFrame()) return;
        var id = bar.GetInstanceID();
        if (rejected.Contains(id)) return;
        try
        {
            if (states.TryGetValue(id, out var previous) && (!previous.Bar || previous.Bar.Pointer != bar.Pointer))
            {
                previous.Restore();
                states.Remove(id);
            }
            if (!states.TryGetValue(id, out var state))
            {
                state = new LayoutState(bar);
                states.Add(id, state);
                state.Apply(HotbarGapAlignment.Measure(bar, state.Width, state.RightExtension), force);
                RequestSettle();
                if (DungeonSettlers10SlotsMod.RunUiAudits) HotbarFrameChecks.Check(bar);
                DungeonSettlers10SlotsMod.Log.Msg($"Custom hotbar layout active: panel={state.Panel.rect.width}x{state.Panel.rect.height}, 10 skills + basic attack, centered; native hierarchy retained.");
            }
            else state.Apply(HotbarGapAlignment.Measure(bar, state.Width, state.RightExtension), force);
        }
        catch (Exception ex)
        {
            if (states.Remove(id, out var state)) state.Restore();
            rejected.Add(id);
            DungeonSettlers10SlotsMod.Log.Warning("Custom frame/layout skipped safely: " + ex);
        }
    }

    internal static void ReapplyAll()
    {
        foreach (var state in states.Values.ToArray())
            if (state.Bar) Apply(state.Bar, true);
    }

    internal static void ReapplyScene(int scene)
    {
        foreach (var state in states.Values.Where(value => value.Scene == scene).ToArray())
            if (state.Bar) Apply(state.Bar, true);
        RequestSettle(1);
    }

    // Unity's layout groups settle after QuickSlotUI.Init. Two bounded follow-up
    // passes keep the Y slot aligned in both native layout phases. Once this
    // counter reaches zero, OnUpdate performs no hotbar lookup or UI write.
    internal static void RequestSettle(int frames = 2) => pendingSettleFrames = Math.Max(pendingSettleFrames, frames);

    internal static void UpdatePending()
    {
        if (pendingSettleFrames <= 0) return;
        pendingSettleFrames--;
        ReapplyAll();
    }

    internal static void Prune()
    {
        foreach (var id in states.Where(entry => !entry.Value.Bar || !entry.Value.Panel).Select(entry => entry.Key).ToArray()) states.Remove(id);
        rejected.Clear();
    }

    internal static void Dispose()
    {
        foreach (var state in states.Values) state.Restore();
        states.Clear(); rejected.Clear();
        pendingSettleFrames = 0;
        ReleaseFrame();
        loadFailed = false;
    }

    private static void ReleaseFrame()
    {
        if (Frame) UnityEngine.Object.Destroy(Frame);
        if (texture) UnityEngine.Object.Destroy(texture);
        Frame = null;
        texture = null;
    }

    internal static bool HasLiveLayout => states.Values.Any(state => state.Bar);

    internal sealed class LayoutState
    {
        internal readonly QuickSlotUI Bar;
        internal readonly int Scene;
        internal readonly RectTransform Panel;
        readonly RectTransform row, basic, item, itemSlot, ammo, getDown;
        readonly RectTransform[] active;
        readonly Image background, divider;
        readonly Sprite originalSprite, originalOverride;
        readonly Image.Type originalType;
        readonly bool originalAspect, dividerEnabled;
        readonly RectSnapshot[] rectangles;
        readonly Behaviour[] layouts;
        readonly bool[] enabledLayouts;
        readonly float panelHeight, width, gap, margin, centerY, itemY, ammoY, getDownY;
        readonly Vector2 cell;
        bool initialized;
        float lastCenterOffset;
        internal int ApplyCount { get; private set; }
        internal float Width => width;
        internal float RightExtension => itemSlot ? ItemSlotUI.FrameGap + ItemSlotUI.Width(Bar) : 0;

        internal LayoutState(QuickSlotUI bar)
        {
            Bar = bar;
            Scene = bar.gameObject.scene.handle;
            if (bar._skillSlots == null || bar._skillSlots.Count != 11) throw new InvalidOperationException("Expected native 11-field skill list.");
            basic = bar._skillSlots[0].GetComponent<RectTransform>();
            Panel = basic.parent.TryCast<RectTransform>();
            row = bar._skillSlots[1].transform.parent.TryCast<RectTransform>();
            if (!Panel || !row || Panel.name != "Panel_QuickSlot" || row.name != "Layout_Skills"
                || row.parent != Panel || Panel.parent != bar.transform)
                throw new InvalidOperationException("Unknown native hotbar hierarchy.");
            active = Enumerable.Range(1, 10).Select(i => bar._skillSlots[i].GetComponent<RectTransform>()).ToArray();
            if (active.Any(rect => !rect || rect.parent != row)) throw new InvalidOperationException("Unexpected skill parent.");
            background = Panel.GetComponent<Image>();
            var sprite = background ? background.sprite : null;
            if (!sprite || (sprite.name != "SkillFrame" && sprite != Frame)) throw new InvalidOperationException("Original SkillFrame image not found.");
            cell = basic.rect.size;
            if (cell.x <= 0 || cell.y <= 0 || active.Any(rect => Vector2.Distance(rect.rect.size, cell) > 0.1f))
                throw new InvalidOperationException("Unexpected native slot sizes.");
            panelHeight = Panel.rect.height;
            width = panelHeight * Frame.rect.width / Frame.rect.height;
            // At 724x82 there is room for all eleven original 64x64 controls,
            // plus ten pixels of padding on each side, with no scaling of icons.
            if (width < 11 * cell.x + 20 || panelHeight < cell.y)
                throw new InvalidOperationException("Frame is too narrow for unchanged native controls.");
            gap = Mathf.Min(4, (width - 20 - 11 * cell.x) / 10);
            margin = (width - 11 * cell.x - 10 * gap) / 2;
            centerY = basic.anchoredPosition.y;
            divider = Panel.Find("Img_Divider")?.GetComponent<Image>();
            item = bar._itemQuickSlot ? bar._itemQuickSlot.transform.parent.TryCast<RectTransform>() : null;
            itemSlot = bar._itemQuickSlot ? bar._itemQuickSlot.GetComponent<RectTransform>() : null;
            ammo = bar._ammoSlots != null && bar._ammoSlots.Count > 0 ? bar._ammoSlots[0].transform.parent.TryCast<RectTransform>() : null;
            getDown = bar._getDownButton ? bar._getDownButton.GetComponent<RectTransform>() : null;
            foreach (var owned in new[] { item, ammo, getDown })
                if (owned && owned.parent != bar.transform) throw new InvalidOperationException("Unknown auxiliary hotbar parent.");
            if (item && item.name != "Layout_Item" || ammo && ammo.name != "Layout_Ammo")
                throw new InvalidOperationException("Unknown auxiliary hotbar layout.");
            itemY = item ? item.anchoredPosition.y : 0;
            ammoY = panelHeight + 8;
            getDownY = getDown ? getDown.anchoredPosition.y : 0;
            rectangles = new[] { Panel, row, basic, item, ammo, getDown }.Concat(active).Where(rect => rect).Select(rect => new RectSnapshot(rect)).ToArray();
            layouts = new Behaviour[] { row.GetComponent<LayoutGroup>(), row.GetComponent<ContentSizeFitter>() }.Where(component => component).ToArray();
            enabledLayouts = layouts.Select(component => component.enabled).ToArray();
            originalSprite = background.sprite; originalOverride = background.overrideSprite;
            originalType = background.type; originalAspect = background.preserveAspect;
            dividerEnabled = divider && divider.enabled;
        }

        internal void Apply(float centerOffset = 0, bool force = false)
        {
            if (!force && initialized && Mathf.Abs(centerOffset - lastCenterOffset) < 0.01f) return;
            ApplyCount++;
            // Also repair a deliberately recreated runtime asset on an existing
            // layout during its next requested settle, without rebuilding slots.
            if (background.sprite != Frame) background.sprite = Frame;
            if (background.overrideSprite != Frame) background.overrideSprite = Frame;
            if (!initialized)
            {
                foreach (var layout in layouts) layout.enabled = false;
                background.type = Image.Type.Simple; background.preserveAspect = true;
                // The shared HUD and its character panel are never moved/resized.
                Panel.anchorMin = Panel.anchorMax = new Vector2(0.5f, 0);
                Panel.pivot = new Vector2(0.5f, 0);
                Panel.sizeDelta = new Vector2(width, panelHeight);
                Position(basic, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(margin + cell.x / 2, centerY), cell);
                Position(row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(margin + cell.x + gap, centerY),
                    new Vector2(10 * cell.x + 9 * gap, cell.y));
                for (var i = 0; i < active.Length; i++)
                    Position(active[i], new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(cell.x / 2 + i * (cell.x + gap), 0), cell);
                // No space is needed for the old decorative basic/skill separator.
                if (divider) divider.enabled = false;
                if (ammo)
                {
                    ammo.anchorMin = ammo.anchorMax = new Vector2(0.5f, 0);
                    ammo.pivot = new Vector2(1, 0);
                }
                initialized = true;
            }
            lastCenterOffset = centerOffset;
            SetAnchoredPosition(Panel, new Vector2(centerOffset, rectangles[0].Position.y));
            if (item)
            {
                // Layout_Item's origin is not the visible slot's left edge.
                // Before Unity settles its layout the centered child starts at
                // -28; afterwards native padding places it at +4. Account for
                // its actual edge in either phase, without changing the child.
                var left = Bar.transform.InverseTransformPoint(itemSlot.TransformPoint(new Vector3(itemSlot.rect.xMin, 0, 0))).x;
                var desiredLeft = Bar.GetComponent<RectTransform>().rect.center.x + centerOffset + width / 2 + ItemSlotUI.FrameGap;
                SetAnchoredPosition(item, new Vector2(item.anchoredPosition.x + desiredLeft - left, itemY));
            }
            if (ammo) SetAnchoredPosition(ammo, new Vector2(centerOffset + width / 2, ammoY));
            if (getDown) SetAnchoredPosition(getDown, new Vector2(centerOffset + width / 2 - 23, getDownY));
        }

        internal void Restore()
        {
            if (background)
            {
                background.sprite = originalSprite; background.overrideSprite = originalOverride;
                background.type = originalType; background.preserveAspect = originalAspect;
            }
            foreach (var rect in rectangles) rect.Restore();
            for (var i = 0; i < layouts.Length; i++) if (layouts[i]) layouts[i].enabled = enabledLayouts[i];
            if (divider) divider.enabled = dividerEnabled;
        }

        static void Position(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot;
            rect.sizeDelta = size; rect.anchoredPosition = position;
        }

        static void SetAnchoredPosition(RectTransform rect, Vector2 position)
        {
            if ((rect.anchoredPosition - position).sqrMagnitude > 0.0001f) rect.anchoredPosition = position;
        }
    }

    sealed class RectSnapshot
    {
        readonly RectTransform rect;
        readonly Vector2 min, max, pivot, size;
        internal readonly Vector2 Position;
        internal RectSnapshot(RectTransform target)
        {
            rect = target; min = rect.anchorMin; max = rect.anchorMax; pivot = rect.pivot;
            size = rect.sizeDelta; Position = rect.anchoredPosition;
        }
        internal void Restore()
        {
            if (!rect) return;
            rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
            rect.sizeDelta = size; rect.anchoredPosition = Position;
        }
    }
}
