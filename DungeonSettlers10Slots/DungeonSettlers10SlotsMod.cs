using System.Security.Cryptography;
using HarmonyLib;
#if BEPINEX
using global::Refactor.Main;
using global::Refactor.Main.Event;
using global::Refactor.Main.InputModule;
using global::Refactor.UI;
#else
using Il2CppRefactor.Main;
using Il2CppRefactor.Main.Event;
using Il2CppRefactor.Main.InputModule;
using Il2CppRefactor.UI;
#endif
using UnityEngine;

namespace DungeonSettlers10Slots;

// Shared gameplay runtime. Loader entry points only supply logging, Harmony
// ownership and lifecycle callbacks; slot IDs and save formats stay identical.
public sealed partial class DungeonSettlers10SlotsMod
{
    internal static HotbarLog Log;
    private HarmonyLib.Harmony runtimeHarmony;
    private bool stopped;
    internal bool IsReady => ready;
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
    // DS_B.0.4.19, Steam build 25154317. The native binary and its metadata
    // belong together; a partially copied patch must not enable these hooks.
    private const string SupportedHash = "B0CD8B641D551019B82C0AF3DDE1532D6FF7BA155B20B2D3936742924B42DB2A";
    private const string SupportedMetadataHash = "CE84EC266501C8DF4A23B31D50D8B413C82DAE49A062BC623B3440E8A3F5A23F";

    internal void StartRuntime(HotbarLog logger, HarmonyLib.Harmony harmony)
    {
        Log = logger;
        runtimeHarmony = harmony;
        stopped = false;
        try { Initialize(); }
        catch (Exception ex)
        {
            ready = false;
            runtimeHarmony.UnpatchSelf();
            Log.Error("10-Slot-Erweiterung sicher deaktiviert: Initialisierung/Selbsttest fehlgeschlagen. " + ex);
        }
    }

    private void Initialize()
    {
        var binary = Path.Combine(Path.GetDirectoryName(Application.dataPath), "GameAssembly.dll");
        var metadata = Path.Combine(Application.dataPath, "il2cpp_data", "Metadata", "global-metadata.dat");
        if (FileHash(binary) != SupportedHash || FileHash(metadata) != SupportedMetadataHash)
        {
            Log.Warning("Unbekannte oder nicht zusammengehörige Spieldateien: 10-Slot-Erweiterung deaktiviert; erwartet DS_B.0.4.19 / Build 25154317 (Spielcode und Metadaten).");
            return;
        }
        Log.Msg("Compatibility fingerprints PASS: DS_B.0.4.19 / Steam 25154317; native binary and metadata match.");
#if BEPINEX
        NativeSaveDictionary.VerifyInterop();
        Log.Msg("BepInEx save dictionary interop PASS: boxed native save record roundtrip before patching.");
#endif
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
            runtimeHarmony.CreateClassProcessor(type).Patch();
        }
        if (RunAudits)
        {
            NativeChecks.CheckContainerBeforeFix();
            ExtraInteractions.ReproduceBeforeFix();
        }
        foreach (var type in new[] { typeof(ContainerSlotMutation), typeof(ContainerSlotCreated), typeof(ContainerSlotsLoaded), typeof(ContainerSlotsSaved) })
        {
            Log.Msg("Installing native patch: " + type.Name);
            runtimeHarmony.CreateClassProcessor(type).Patch();
        }
        foreach (var type in new[] { typeof(ExtraRemoveInteraction), typeof(ExtraAutoInteraction), typeof(ExtraInteractionEvents), typeof(ExtraInteractionInputGate) })
        {
            Log.Msg("Installing native patch: " + type.Name);
            runtimeHarmony.CreateClassProcessor(type).Patch();
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
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        ready = true;
        Log.Msg("10-slot release patches loaded; native bindings preserved, extra bindings initially unbound. Hotbar layout and labels update only on relevant events; no periodic Unity object scans or read-path storage guards.");
        if (NativeLayoutComparison.Enabled)
            Log.Warning("PRESENTATION A/B TEST B: original frame/layout; all ten slots, bindings and save handling retained. Extra fields may overlap other HUD controls. Restart without " + NativeLayoutComparison.Argument + " to restore the custom presentation.");
    }

    internal void Tick()
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

    internal void SceneChanged()
    {
        if (!ready) return;
        // Additive scenes may leave the registered HUD alive. Keep its reader so
        // later rebinding continues to update labels on that same hotbar.
        HotbarKeyLabels.Prune();
        HotbarLabelChecks.Clear();
        HotbarFrameLayout.Prune();
        HotbarGapAlignment.Prune();
        ItemSlotUI.Prune();
    }

    internal void StopRuntime()
    {
        if (stopped) return;
        stopped = true;
        ready = false;
        try
        {
            ItemSlotStorage.ClearAll();
            HotbarKeyLabels.Clear();
            HotbarGapAlignment.Clear();
            HotbarFrameLayout.Dispose();
        }
        finally { runtimeHarmony?.UnpatchSelf(); }
    }

    private static string FileHash(string path)
    {
        using var file = File.OpenRead(path);
        using var hash = SHA256.Create();
        return Convert.ToHexString(hash.ComputeHash(file));
    }

}
