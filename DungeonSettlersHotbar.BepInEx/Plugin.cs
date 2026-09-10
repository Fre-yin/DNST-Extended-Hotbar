using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace DungeonSettlers10Slots;

[BepInPlugin(Id, "Extended Hotbar (BepInEx preview)", "0.3.8-bepinex.1")]
[BepInProcess("DungeonSettlers.exe")]
public sealed class Plugin : BasePlugin
{
    internal const string Id = "fre-yin.dnst.extended-hotbar";

    public override void Load()
    {
        using (var process = System.Diagnostics.Process.GetCurrentProcess())
            Log.LogInfo($"Hotbar session: {process.Id}|{process.StartTime.ToUniversalTime().Ticks}|{Path.GetDirectoryName(process.MainModule.FileName)}");
        var runtime = new DungeonSettlers10SlotsMod();
        HotbarDriver driver = null;
        try
        {
            runtime.StartRuntime(new HotbarLog(message => Log.LogInfo(message),
                message => Log.LogWarning(message), message => Log.LogError(message)), new Harmony(Id));
            if (!runtime.IsReady) throw new InvalidOperationException("Hotbar compatibility or initialization check failed. No hotbar hooks remain active.");
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
