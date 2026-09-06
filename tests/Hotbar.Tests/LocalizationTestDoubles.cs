// Minimal in-memory substitutes for the native language boundary. They test the
// linked production registration code, not Harmony dispatch or Unity rendering.
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class HarmonyPatch : Attribute
    {
        public HarmonyPatch(Type type, string method) { }
    }
}

namespace Il2CppRefactor.Setting
{
    internal enum LanguageType { English }
}

namespace Il2CppRefactor.Main.InputModule
{
    internal enum KeyInputType { }
}

namespace Il2CppRefactor.UI
{
    internal sealed class DASText
    {
        public int Refreshes;
        public bool HasDefaults;
        public int MaxVisibleLines = 1;
        private int initialMaxLines;
        // Model the native pre-Awake default restoration, not TMP rendering.
        public void StoreInitialDefaults()
        {
            if (HasDefaults) return;
            initialMaxLines = MaxVisibleLines;
            HasDefaults = true;
        }
        public void RefreshOnRunTime()
        {
            Refreshes++;
            MaxVisibleLines = initialMaxLines;
        }
    }
    internal sealed class SubUI_KeySetting
    {
        public void SetRowData() { }
    }
    internal sealed class SubUI_KeySettingParts
    {
        public DASText _titleText = new();
        public DASText _firstButtonText = new();
        public DASText _secondButtonText = new();
    }
}

namespace Il2CppRefactor.Util
{
    internal sealed class TextSheet
    {
        public Dictionary<string, string> _textTable = new();
        public Dictionary<string, string> _tableData = new();
        public string GetTextByLanguage(string key, Setting.LanguageType language) => _tableData[key];
        public void ParseMatchingLanguage() { }
    }

    internal sealed class DataSheetManager
    {
        public static DataSheetManager Instance;
        public TextSheet _text;
        public static implicit operator bool(DataSheetManager manager) => manager != null;
    }
}

namespace DungeonSettlers10Slots
{
    internal static class DungeonSettlers10SlotsMod { internal static int ExtraCount => 6; }
    internal static class ExtraBindings
    {
        internal static int TypeAt(int index) => 12005 + index;
        internal static bool IsExtra(Il2CppRefactor.Main.InputModule.KeyInputType type) => (int)type >= 12005 && (int)type <= 12010;
    }
    internal static class ItemSlotInput
    {
        internal static int Key(int slot) => 12100 + slot;
        internal static bool IsExtra(Il2CppRefactor.Main.InputModule.KeyInputType type) => (int)type is 12101 or 12102;
    }
}
