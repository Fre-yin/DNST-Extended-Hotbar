using MelonLoader;

[assembly: MelonInfo(typeof(DungeonSettlers10Slots.DungeonSettlers10SlotsMod), "Extended Hotbar", "0.3.8", "Danny/Codex")]
[assembly: MelonGame(null, "DungeonSettlers")]
[assembly: HarmonyDontPatchAll]

namespace DungeonSettlers10Slots;

public sealed partial class DungeonSettlers10SlotsMod : MelonMod
{
    public override void OnInitializeMelon() => StartRuntime(
        new HotbarLog(message => LoggerInstance.Msg(message),
            message => LoggerInstance.Warning(message), message => LoggerInstance.Error(message)),
        HarmonyInstance);
    public override void OnUpdate() => Tick();
    public override void OnSceneWasLoaded(int buildIndex, string sceneName) => SceneChanged();
    public override void OnDeinitializeMelon() => StopRuntime();
}
