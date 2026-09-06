using Il2CppRefactor.Util;
using UnityEngine;

namespace DungeonSettlers10Slots;

// Main-menu-only, explicit asynchronous asset lifetime test. Does not switch
// language or scenes, touch settings, or remove any original game asset.
internal static class FrameLifetimeChecks
{
    private static bool pending = Environment.GetCommandLineArgs().Contains("--ds-run-frame-lifetime-audit");
    private static AsyncOperation unloading;
    private static Texture2D unpinnedTexture;
    private static Sprite unpinnedSprite;
    private static Sprite retainedFrame;
    private static float deadline;

    internal static void RunIfRequested()
    {
        if (!pending) return;
        var manager = DataSheetManager.Instance;
        if (!manager || !manager.IsReady) return;
        try
        {
            if (unloading == null)
            {
                if (HotbarFrameLayout.HasLiveLayout)
                    throw new InvalidOperationException("A campaign hotbar already exists; asset audit requires a fresh main-menu start.");
                // Reproduce the former retention policy on disposable controls.
                unpinnedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                unpinnedSprite = Sprite.Create(unpinnedTexture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
                UnityEngine.Object.DontDestroyOnLoad(unpinnedTexture);
                UnityEngine.Object.DontDestroyOnLoad(unpinnedSprite);
                Require(HotbarFrameLayout.LoadFrame(), "user frame loaded");
                retainedFrame = HotbarFrameLayout.Frame;
                unloading = Resources.UnloadUnusedAssets();
                deadline = Time.realtimeSinceStartup + 30;
                return;
            }
            if (!unloading.isDone)
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Unity asset audit timed out.");
                return;
            }
            Require(!unpinnedSprite || !unpinnedTexture, "old unreferenced assets released: regression reproduced");
            Require(retainedFrame && retainedFrame.texture && HotbarFrameLayout.LoadFrame()
                && HotbarFrameLayout.Frame.Pointer == retainedFrame.Pointer, "pinned frame/texture survive native asset unload");
            Require((retainedFrame.hideFlags & HideFlags.DontUnloadUnusedAsset) != 0
                && (retainedFrame.texture.hideFlags & HideFlags.DontUnloadUnusedAsset) != 0, "both mod assets protected");
            // Do not destroy an image a user may already be viewing after load.
            if (HotbarFrameLayout.HasLiveLayout)
                throw new InvalidOperationException("Campaign opened during asset audit; recovery probe skipped without touching its frame.");
            UnityEngine.Object.DestroyImmediate(retainedFrame);
            Require(HotbarFrameLayout.LoadFrame() && HotbarFrameLayout.Frame && HotbarFrameLayout.Frame.texture,
                "lost native asset reloads despite previous successful load");
            DungeonSettlers10SlotsMod.Log.Msg("Frame lifetime PASS: old DontDestroyOnLoad-only assets were released; protected user sprite + texture survived Resources.UnloadUnusedAssets; destroyed-frame recovery succeeded. No campaign or original game assets modified.");
            Finish();
        }
        catch (Exception ex)
        {
            DungeonSettlers10SlotsMod.Log.Error("Frame lifetime audit FAILED: " + ex);
            Finish();
        }
    }

    private static void Finish()
    {
        pending = false;
        if (unpinnedSprite) UnityEngine.Object.Destroy(unpinnedSprite);
        if (unpinnedTexture) UnityEngine.Object.Destroy(unpinnedTexture);
        unpinnedSprite = null;
        unpinnedTexture = null;
        retainedFrame = null;
        unloading = null;
    }

    private static void Require(bool value, string check)
    {
        if (!value) throw new InvalidOperationException(check);
    }
}
