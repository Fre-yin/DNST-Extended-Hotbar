using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ExtendedHotbar.Helper
{
    // Stable codes, never translated text, identify guarded failures.
    internal sealed class HelperFailure : IOException
    {
        internal readonly string Code;
        internal HelperFailure(string code, string diagnostic, Exception inner = null) : base(diagnostic, inner) { Code = code; }
    }

    internal sealed class UiText
    {
        // Matches the ten LanguageType values verified in the game's native language audit.
        internal static readonly string[] Codes = { "en", "ko", "fr", "de", "ru", "zh-Hans", "zh-Hant", "ja", "es", "pt-BR" };
        internal static readonly string[] Names = { "English", "한국어", "Français", "Deutsch", "Русский", "简体中文", "繁體中文", "日本語", "Español", "Português (Brasil)" };
        private readonly Dictionary<string, string> values;
        internal readonly string Code;
        internal UiText(string code)
        {
            Code = Codes.Contains(code) ? code : "en";
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Language." + Code + ".json"))
            {
                if (stream == null) throw new InvalidOperationException("Missing embedded language: " + Code);
                using (var reader = new StreamReader(stream)) values = Parse(reader.ReadToEnd());
            }
        }
        internal static Dictionary<string, string> Parse(string json)
        {
            var doc = new LosslessJson(json);
            if (doc.Root.Kind != "object") throw new FormatException("Language object required.");
            return doc.Root.Members.ToDictionary(x => x.Name, x => x.Value.String);
        }
        internal IEnumerable<string> Keys { get { return values.Keys; } }
        internal string this[string key] { get { return values[key]; } }
        internal string Format(string key, params object[] args) { return string.Format(CultureInfo.InvariantCulture, this[key], args); }
        internal static string FromCulture(string name)
        {
            name = (name ?? "").Replace('_', '-').ToLowerInvariant();
            if (name == "zh" || name.StartsWith("zh-"))
                return name.Contains("hant") || name.EndsWith("-tw") || name.EndsWith("-hk") || name.EndsWith("-mo") ? "zh-Hant" : "zh-Hans";
            var prefix = name.Split('-')[0];
            return prefix == "pt" ? "pt-BR" : Codes.Contains(prefix) ? prefix : "en";
        }
        internal static string FromSettings(string settings, string fallback)
        {
            try
            {
                var value = new LosslessJson(settings).Root.Get("GeneralSettingData").Get("SaveCurrentLanguageType").Integer;
                return value >= 0 && value < Codes.Length ? Codes[value] : fallback;
            }
            catch { return fallback; }
        }
        internal static string Detect(string profile)
        {
            var fallback = FromCulture(CultureInfo.CurrentUICulture.Name);
            try { var bytes = Files.Read(Files.Under(profile, "UserSetting.json")); return bytes == null ? fallback : FromSettings(Files.Text(bytes), fallback); }
            catch { return fallback; } // Read-only detection; an inaccessible profile is not an error.
        }
        internal string Error(Exception ex)
        {
            var failure = ex as HelperFailure;
            var key = failure != null ? failure.Code : ex is FormatException ? "errorData" : ex is UnauthorizedAccessException || ex is System.Security.SecurityException ? "errorAccess" : "errorIO";
            return this[key];
        }
        internal string HistoryAction(string action)
        {
            const string restore = "Wiederherstellen: ";
            if (action.StartsWith(restore, StringComparison.Ordinal)) return this["undoShort"] + ": " + HistoryAction(action.Substring(restore.Length));
            switch (action)
            {
                case "Installieren / Aktualisieren": return this["install"];
                case "Ohne Hotbar vorbereiten": return this["export"].Replace("\n", " ");
                case "Originaltasten vorbereiten (ohne Spielstandänderung)": return this["native"];
                default: return this["change"];
            }
        }
    }
}
