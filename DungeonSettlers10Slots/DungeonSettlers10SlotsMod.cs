using System.Security.Cryptography;
using HarmonyLib;
using Il2CppRefactor.Main;
using Il2CppRefactor.Main.Event;
using Il2CppRefactor.Main.InputModule;
using Il2CppRefactor.UI;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(DungeonSettlers10Slots.DungeonSettlers10SlotsMod), "Extended Hotbar", "0.3.6", "Danny/Codex")]
[assembly: MelonGame(null, "DungeonSettlers")]
[assembly: HarmonyDontPatchAll]

namespace DungeonSettlers10Slots;

public sealed class DungeonSettlers10SlotsMod : MelonMod
{
    internal static MelonLogger.Instance Log;
    internal static int FirstExtra;
    internal const int Capacity = 10;
    internal static int ExtraCount => Capacity - FirstExtra;
    private bool ready;
    private int screenWidth;
    private int screenHeight;
    internal const string UiAuditArgument = "--ds-run-ui-audits";
    internal static bool RunUiAudits { get; } = Environment.GetCommandLineArgs().Contains(UiAuditArgument);
    internal static bool RunAudits { get; } = RunUiAudits || Environment.GetCommandLineArgs().Contains("--ds-run-item-audits")
        || Environment.GetCommandLineArgs().Contains("--ds-run-audits");
    // DS_B.0.4.17, Steam build 25143510. The native binary and its metadata
    // belong together; a partially copied patch must not enable these hooks.
    private const string SupportedHash = "07F5CEA4B73F0747A71328EE0670837B53A894856AD511039C2611991DCDFA82";
    private const string SupportedMetadataHash = "4BBA8FF88B8B0B9A4F42E96F17D6602F84AF2B3EDFF362C34B5EB51FA0B0C971";

    public override void OnInitializeMelon()
    {
        Log = LoggerInstance;
        try { Initialize(); }
        catch (Exception ex)
        {
            ready = false;
            HarmonyInstance.UnpatchSelf();
            Log.Error("10-Slot-Erweiterung sicher deaktiviert: Initialisierung/Selbsttest fehlgeschlagen. " + ex);
        }
    }

    private void Initialize()
    {
        var binary = Path.Combine(Path.GetDirectoryName(Application.dataPath), "GameAssembly.dll");
        var metadata = Path.Combine(Application.dataPath, "il2cpp_data", "Metadata", "global-metadata.dat");
        if (FileHash(binary) != SupportedHash || FileHash(metadata) != SupportedMetadataHash)
        {
            Log.Warning("Unbekannte oder nicht zusammengehörige Spieldateien: 10-Slot-Erweiterung deaktiviert; erwartet DS_B.0.4.17 / Build 25143510 (Spielcode und Metadaten).");
            return;
        }
        Log.Msg("Compatibility fingerprints PASS: DS_B.0.4.17 / Steam 25143510; native binary and metadata match.");
        // Resolve the native offset: basic attack must not consume an extra active-skill slot.
        FirstExtra = QuickSlotData.MAX_SLOT;
        Log.Msg($"Native slot layout: MAX_SLOT={FirstExtra}, target={Capacity}");
        if (FirstExtra != 4)
        {
            Log.Warning("Unbekannte Skill-Indizierung: Erweiterung deaktiviert.");
            return;
        }
        Log.Msg("Preparing native patches.");
        var patchTypes = new List<Type> { typeof(MutateSlots), typeof(LoadSlots), typeof(SaveSlots), typeof(ResetSlots), typeof(ExpandHotbar),
            typeof(ExtraDefaults), typeof(AvoidShiftSelectionCollision), typeof(ShowCombatBindingModifiers),
            typeof(RegisterExtraBindingLanguage), typeof(RefreshExtraBindingFonts), typeof(ExpandOptions),
            typeof(ExtraSkillEvents), typeof(ExtraSkillInputGate), typeof(ExtraSkillObserver),
            typeof(CompactInitialSkillKey), typeof(InitialSkillKeyLabels), typeof(RefreshSkillLabelsOnBindingChange),
            typeof(RegisterEntitySummary), typeof(RegisterControlMode), typeof(RefreshLayoutOnUiScale),
            typeof(ReadExtraItem), typeof(HasExtraItem), typeof(SetExtraItem), typeof(ClearExtraItem), typeof(ResetExtraItems), typeof(ResetCampaignItems), typeof(ClearAllExtraItems),
            typeof(ExtraItemKeyEvent), typeof(ExtraItemUIEvent), typeof(ExtraItemDispatch), typeof(ExtraItemRemoveMenu),
            typeof(ExtraItemInputGate), typeof(ExtraItemObserver), typeof(RefreshExtraItems), typeof(SettleItemsWhenShown),
            typeof(LoadItemExtension), typeof(SaveItemExtension), typeof(WriteItemExtension), typeof(GuardItemSaveHeader) };
        // Legacy 12-slot saves retain their data, but the native view must only
        // receive its ten visible keys. The normal ten-key path makes no copy.
        patchTypes.Add(typeof(RefreshExtraSkills));
        foreach (var type in patchTypes)
        {
            Log.Msg("Installing native patch: " + type.Name);
            HarmonyInstance.CreateClassProcessor(type).Patch();
        }
        if (RunAudits)
        {
            NativeChecks.CheckContainerBeforeFix();
            ExtraInteractions.ReproduceBeforeFix();
        }
        foreach (var type in new[] { typeof(ContainerSlotMutation), typeof(ContainerSlotCreated), typeof(ContainerSlotsLoaded), typeof(ContainerSlotsSaved) })
        {
            Log.Msg("Installing native patch: " + type.Name);
            HarmonyInstance.CreateClassProcessor(type).Patch();
        }
        foreach (var type in new[] { typeof(ExtraRemoveInteraction), typeof(ExtraAutoInteraction), typeof(ExtraInteractionEvents), typeof(ExtraInteractionInputGate) })
        {
            Log.Msg("Installing native patch: " + type.Name);
            HarmonyInstance.CreateClassProcessor(type).Patch();
        }
        if (RunAudits)
        {
            Log.Msg("Explicit audit launch: starting isolated native checks.");
            try
            {
                ExtraInteractions.Check(false);
                NativeChecks.Run();
                if (!RunUiAudits) NativeKeyboardChecks.Run();
                ItemSlotChecks.Run();
                NativePersistenceChecks.RunIfRequested();
            }
            finally { ItemSlotStorage.ClearAll(); }
        }
        CombatBindingPreset.ApplyRequestedProfile();
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        ready = true;
        Log.Msg("10-slot release patches loaded; skills 1-0, units Shift+1-0. Hotbar layout and labels update only on relevant events; no periodic Unity object scans or read-path storage guards.");
        if (NativeLayoutComparison.Enabled)
            Log.Warning("PRESENTATION A/B TEST B: original frame/layout; all ten slots, bindings and save handling retained. Extra fields may overlap other HUD controls. Restart without " + NativeLayoutComparison.Argument + " to restore the custom presentation.");
    }

    public override void OnUpdate()
    {
        if (!ready) return;
        NativeLocalizationChecks.RunIfRequested();
        FrameLifetimeChecks.RunIfRequested();
        if (screenWidth != Screen.width || screenHeight != Screen.height)
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            HotbarFrameLayout.RequestSettle();
        }
        HotbarFrameLayout.UpdatePending();
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        // Additive scenes may leave the registered HUD alive. Keep its reader so
        // later rebinding continues to update labels on that same hotbar.
        HotbarKeyLabels.Prune();
        HotbarLabelChecks.Clear();
        HotbarFrameLayout.Prune();
        HotbarGapAlignment.Prune();
        ItemSlotUI.Prune();
    }

    public override void OnDeinitializeMelon()
    {
        ready = false;
        ItemSlotStorage.ClearAll();
        HotbarKeyLabels.Clear();
        HotbarGapAlignment.Clear();
        HotbarFrameLayout.Dispose();
    }

    private static string FileHash(string path)
    {
        using var file = File.OpenRead(path);
        using var hash = SHA256.Create();
        return Convert.ToHexString(hash.ComputeHash(file));
    }

}
