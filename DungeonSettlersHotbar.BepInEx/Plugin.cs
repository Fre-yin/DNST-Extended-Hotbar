using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace DungeonSettlers10Slots;

[BepInPlugin(Id, "Extended Hotbar (BepInEx)", "1.0.2")]
[BepInProcess("DungeonSettlers.exe")]
public sealed class Plugin : BasePlugin
{
    internal const string Id = "fre-yin.dnst.extended-hotbar";

    public override void Load()
    {
        var runtime = new DungeonSettlers10SlotsMod();
        HotbarDriver driver = null;
        try
        {
            runtime.StartRuntime(new HotbarLog(message => Log.LogInfo(message),
                message => Log.LogWarning(message), message => Log.LogError(message)), new Harmony(Id));
            if (runtime.CompatibilityRejected)
            {
                // Same outcome as MelonLoader: the runtime already warned and installed
                // no hooks. An unsupported game build after a patch is not a crash.
                runtime.StopRuntime();
                if (Environment.GetCommandLineArgs().Contains("--ds-bepinex-smoke-test")) Application.Quit(1);
                return;
            }
            if (!runtime.IsReady) throw new InvalidOperationException("Hotbar initialization check failed. No hotbar hooks remain active.");
            driver = AddComponent<HotbarDriver>();
            driver.Attach(runtime);
            Log.LogInfo("BepInEx hotbar lifecycle attached. MelonLoader is not required; restart to remove this plugin.");
        }
        catch
        {
            Log.LogError("Hotbar initialization failed.");
            runtime.StopRuntime();
            if (driver) UnityEngine.Object.Destroy(driver);
            if (Environment.GetCommandLineArgs().Contains("--ds-bepinex-smoke-test")) Application.Quit(1);
            throw;
        }
    }

    // Removing native slot/save patches mid-campaign is not a safe hot reload.
    public override bool Unload() => false;
}

public sealed class HotbarDriver : MonoBehaviour
{
    private DungeonSettlers10SlotsMod runtime;
    private UnityAction<Scene, LoadSceneMode> sceneLoaded;
    private bool smokeTest;
    private int smokeFrames;
    private int smokeScenes;
    private float smokeQuitAfter;
    public HotbarDriver(IntPtr pointer) : base(pointer) { }

    [HideFromIl2Cpp]
    internal void Attach(DungeonSettlers10SlotsMod value)
    {
        runtime = value;
        smokeTest = Environment.GetCommandLineArgs().Contains("--ds-bepinex-smoke-test");
        if (smokeTest) smokeQuitAfter = Time.realtimeSinceStartup + 10;
        sceneLoaded = (Action<Scene, LoadSceneMode>)OnSceneLoaded;
        SceneManager.sceneLoaded += sceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        runtime?.SceneChanged();
        if (smokeTest) smokeScenes++;
    }
    public void Update()
    {
        runtime?.Tick();
        if (smokeTest && ++smokeFrames >= 120 && Time.realtimeSinceStartup >= smokeQuitAfter)
        {
            smokeQuitAfter = float.PositiveInfinity;
            DungeonSettlers10SlotsMod.Log.Msg($"BepInEx lifecycle smoke PASS: {smokeFrames} Update callbacks, {smokeScenes} scene callbacks; requesting clean application quit. No campaign was loaded by the test.");
            Application.Quit();
        }
    }
    public void OnApplicationQuit() => Detach();
    public void OnDestroy() => Detach();

    [HideFromIl2Cpp]
    private void Detach()
    {
        var owned = runtime;
        runtime = null;
        try
        {
            if (sceneLoaded != null) SceneManager.sceneLoaded -= sceneLoaded;
            sceneLoaded = null;
        }
        finally
        {
            owned?.StopRuntime();
            if (owned != null && smokeTest) DungeonSettlers10SlotsMod.Log.Msg("BepInEx lifecycle smoke PASS: runtime cleanup completed.");
        }
    }
}
