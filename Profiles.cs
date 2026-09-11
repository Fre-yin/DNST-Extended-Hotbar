using System;
using System.Collections.Generic;
using System.Linq;

namespace ExtendedHotbar.Helper
{
    internal static class Profiles
    {
        internal const int CharacterStart = 4;
        internal const int CharacterEnd = 23;
        internal const int SkillStart = 34;
        internal const int SkillEnd = 37;
        internal const int CharacterRowCount = 10;
        internal const int SkillRowCount = 4;
        internal const int CharacterAndSkillRowSplit = 14;
        internal const int ZeroModifier = 0;
        internal const int ShiftModifier = 304;
        internal const int ItemActionType = 60;
        internal const int ProfileItemUseType = 12101;
        internal const int ProfileItemUnsetType = 12102;
        internal const int ProfileSkillStart = 12001;
        internal const int ExtraProfileStart = 12005;
        internal const int ExtraProfileEnd = 12012;
        internal const int HotbarProfileItemStart = 12005;
        internal const int HotbarProfileItemEnd = 12010;

        internal static bool Extra(int type) { return type >= ExtraProfileStart && type <= ExtraProfileEnd || type == ProfileItemUseType || type == ProfileItemUnsetType; }
        internal static bool Affected(int type) { return Character(type) || Skill(type) || type == ItemActionType || Extra(type); }
        // SelectUnit_1..10 and the game's hidden AddSelectUnit_1..10 actions.
        internal static bool Character(int type) { return type >= CharacterStart && type <= CharacterEnd; }
        internal static bool Skill(int type) { return type >= SkillStart && type <= SkillEnd; }
        internal static JsonNode Bindings(LosslessJson document)
        {
            var bindings = document.Root.Get("KeySettingData").Get("Bindings");
            if (bindings.Kind != "array" || bindings.Items.Count > 1000) throw new FormatException("Unbekannte Tastenbelegungen.");
            var unique = new HashSet<string>();
            foreach (var entry in bindings.Items)
            {
                int type = entry.Get("InputType").Integer, column = entry.Get("SlotIndex").Integer;
                if (column < 0 || column > 1 || !unique.Add(type + ":" + column)) throw new FormatException("Doppelte oder unbekannte Tastenzeile.");
                int key = entry.Get("KeyCode").Integer, modifier = entry.Get("ModifierKey").Integer;
                if (key < 0 || modifier < 0 || entry.Get("IsKeyDown").Kind != "bool") throw new FormatException("Ungültige Tastenwerte.");
            }
            return bindings;
        }
        internal static string Selected(string json)
        { return Selected(json, Affected); }
        internal static string SelectedCharacters(string json)
        { return Selected(json, Character); }
        private static string Selected(string json, Func<int, bool> affected)
        {
            var doc = new LosslessJson(json);
            return "[" + string.Join(",", Bindings(doc).Items.Where(x => affected(x.Get("InputType").Integer)).Select(doc.Raw)) + "]";
        }
        internal static bool Same(string left, string right)
        {
            return CanonicalBindings(new LosslessJson(left).Root) == CanonicalBindings(new LosslessJson(right).Root);
        }
        private static string CanonicalBindings(JsonNode node)
        {
            if (node.Kind != "array") throw new FormatException("Tastenliste erwartet.");
            return "[" + string.Join(",", node.Items.OrderBy(x => x.Get("InputType").Integer).ThenBy(x => x.Get("SlotIndex").Integer).Select(Canonical)) + "]";
        }
        private static string Canonical(JsonNode node)
        {
            if (node.Kind == "object") return "{" + string.Join(",", node.Members.OrderBy(x => x.Name, StringComparer.Ordinal).Select(x => LosslessJson.Quote(x.Name) + ":" + Canonical(x.Value))) + "}";
            if (node.Kind == "array") return "[" + string.Join(",", node.Items.Select(Canonical)) + "]";
            return node.Kind == "string" ? LosslessJson.Quote(node.Text) : node.Text ?? "null";
        }
        internal static string Merge(string current, string selected)
        { return Merge(current, selected, Affected); }
        internal static string MergeCharacters(string current, string selected)
        { return Merge(current, selected, Character); }
        private static string Merge(string current, string selected, Func<int, bool> affected)
        {
            var doc = new LosslessJson(current);
            var original = Bindings(doc);
            var replacement = new LosslessJson(selected).Root;
            if (replacement.Kind != "array" || replacement.Items.Any(x => !affected(x.Get("InputType").Integer))) throw new FormatException("Fremde Tasten im gewählten Profil.");
            Func<JsonNode, string> identity = x => x.Get("InputType").Integer + ":" + x.Get("SlotIndex").Integer;
            var replacements = replacement.Items.ToDictionary(identity, x => selected.Substring(x.Start, x.End - x.Start));
            var values = new List<string>();
            foreach (var entry in original.Items)
            {
                if (!affected(entry.Get("InputType").Integer)) { values.Add(doc.Raw(entry)); continue; }
                string value; var key = identity(entry);
                if (replacements.TryGetValue(key, out value)) { values.Add(value); replacements.Remove(key); }
            }
            // Preserve existing row order; append only missing actions/columns.
            values.AddRange(replacement.Items.Where(x => replacements.ContainsKey(identity(x))).Select(x => replacements[identity(x)]));
            var result = doc.Apply(new[] { doc.Replace(original, "[" + string.Join(",", values) + "]") });
            Bindings(new LosslessJson(result));
            return result;
        }
        private static string CharacterDefaults(bool shift)
        {
            var rows = new List<string>();
            for (int i = 0; i < CharacterRowCount; i++)
            {
                int digit = i == 9 ? 48 : 49 + i;
                rows.Add(Row(CharacterStart + i, 0, digit, shift ? ShiftModifier : ZeroModifier));
                rows.Add(Row(CharacterStart + i, 1, ZeroModifier, ZeroModifier));
                // A Shift-modified selection must not also emit additive selection.
                rows.Add(Row(CharacterAndSkillRowSplit + i, 0, shift ? 0 : digit, shift ? ZeroModifier : ShiftModifier));
                rows.Add(Row(CharacterAndSkillRowSplit + i, 1, ZeroModifier, ZeroModifier));
            }
            return "[" + string.Join(",", rows) + "]";
        }
        internal static List<JsonNode> CharacterConflicts(string current, bool shift)
        { return Conflicts(current, CharacterDefaults(shift), Character); }
        internal static List<JsonNode> Conflicts(string json, string desired, Func<int, bool> scope)
        {
            var current = new LosslessJson(json);
            var requested = new LosslessJson(desired).Root.Items.Where(x => x.Get("KeyCode").Integer != 0).ToArray();
            return Bindings(current).Items.Where(other => !scope(other.Get("InputType").Integer)
                && requested.Any(x => x.Get("KeyCode").Integer == other.Get("KeyCode").Integer && x.Get("ModifierKey").Integer == other.Get("ModifierKey").Integer)).ToList();
        }
        internal static string ConfigureCharacters(string current, bool shift, bool overwriteConflicts = false)
        {
            return Configure(current, CharacterDefaults(shift), Character, overwriteConflicts);
        }
        internal static string Configure(string current, string desired, Func<int, bool> scope, bool overwriteConflicts)
        {
            var doc = new LosslessJson(current);
            var conflicts = Conflicts(current, desired, scope);
            if (conflicts.Count != 0 && !overwriteConflicts)
                throw new HelperFailure("errorCharacterConflict", "Gewünschte Charaktertasten sind bereits durch andere Aktionen belegt.");
            if (conflicts.Count != 0)
                current = doc.Apply(conflicts.SelectMany(row => new[] {
                    doc.Replace(row.Get("KeyCode"), "0"), doc.Replace(row.Get("ModifierKey"), "0")
                }));
            return Merge(current, desired, scope);
        }
        internal static string RestoreCharacterBindings(string current, string before, string after)
        { return RestoreBindingChanges(current, before, after, Character); }
        internal static string RestoreBindingChanges(string current, string before, string after, Func<int, bool> affected)
        {
            var doc = new LosslessJson(current); var old = new LosslessJson(before); var applied = new LosslessJson(after);
            Func<JsonNode, string> identity = row => row.Get("InputType").Integer + ":" + row.Get("SlotIndex").Integer;
            var originals = Bindings(old).Items.ToDictionary(identity);
            var expected = Bindings(applied).Items.ToDictionary(identity);
            var present = Bindings(doc).Items.ToDictionary(identity);
            Func<Dictionary<string, JsonNode>, string, string> value = (rows, key) => rows.ContainsKey(key) ? Canonical(rows[key]) : null;
            // Restore character rows plus exactly the foreign rows explicitly
            // cleared by the confirmed overwrite. Later unrelated edits survive.
            var scope = new HashSet<string>(originals.Keys.Concat(expected.Keys).Where(key =>
                affected((originals.ContainsKey(key) ? originals[key] : expected[key]).Get("InputType").Integer)
                || value(originals, key) != value(expected, key)));
            if (scope.Any(key => value(present, key) != value(expected, key)))
                throw new HelperFailure("errorConflict", "Betroffene Charaktertasten oder freigegebene Belegungen wurden inzwischen geändert.");
            var restored = new List<string>();
            foreach (var row in Bindings(doc).Items)
            {
                var key = identity(row);
                if (!scope.Contains(key)) restored.Add(doc.Raw(row));
                else if (originals.ContainsKey(key)) restored.Add(old.Raw(originals[key]));
            }
            restored.AddRange(Bindings(old).Items.Where(row => scope.Contains(identity(row)) && !present.ContainsKey(identity(row))).Select(old.Raw));
            var result = doc.Apply(new[] { doc.Replace(Bindings(doc), "[" + string.Join(",", restored) + "]") });
            Bindings(new LosslessJson(result)); return result;
        }
        // Explicitly chosen fallback for users without a pre-mod binding backup.
        // Only this build's 25 native hotbar/selection actions are reset, not all settings.
        internal static string VanillaDefaults()
        {
            var rows = new List<string>();
            for (int i = 0; i < CharacterRowCount; i++)
            {
                int digit = i == 9 ? 48 : 49 + i;
                for (int col = 0; col < 2; col++)
                {
                    rows.Add(Row(CharacterStart + i, col, col == 0 ? digit : 0, 0));
                    rows.Add(Row(CharacterAndSkillRowSplit + i, col, col == 0 ? digit : 0, col == 0 ? ShiftModifier : ZeroModifier));
                }
            }
            var keys = new[] { 113, 101, 114, 116 };
            for (int i = 0; i < SkillRowCount; i++) for (int col = 0; col < 2; col++) rows.Add(Row(SkillStart + i, col, col == 0 ? keys[i] : 0, 0));
            rows.Add(Row(ItemActionType, 0, 121, 0)); rows.Add(Row(ItemActionType, 1, 0, 0));
            return "[" + string.Join(",", rows) + "]";
        }
        internal static string Row(int type, int slot, int key, int modifier)
        {
            return "{\"InputType\":" + type + ",\"SlotIndex\":" + slot + ",\"KeyCode\":" + key + ",\"IsKeyDown\":true,\"ModifierKey\":" + modifier + "}";
        }
    }

    internal static class VanillaSave
    {
        internal static string Export(string source, string saveName, string displaySuffix = "(ohne Hotbar)")
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(saveName, @"\A[A-Za-z0-9_-]{1,64}\z")) throw new FormatException("Ungültiger Exportname.");
            if (string.IsNullOrWhiteSpace(displaySuffix) || displaySuffix.Length > 64 || displaySuffix.Any(char.IsControl)) throw new FormatException("Invalid export display suffix.");
            var doc = new LosslessJson(source);
            var header = doc.Root.Get("CampaignSaveHeader");
            var version = header.Get("Version").String;
            // Save/key field layouts were compared across the two verified builds.
            if (version != "DS_B.0.4.17" && version != "DS_B.0.4.19") throw new HelperFailure("errorSaveVersion", "Spielstandversion noch nicht für den Export geprüft.");
            if (header.Get("IronModeEnabled").Kind != "bool" || header.Get("IronModeEnabled").Text != "false") throw new HelperFailure("errorIron", "Ironmode-Spielstände werden nicht exportiert.");
            if (header.Get("IsAutoSave").Kind != "bool") throw new FormatException("Unbekanntes Speicherformat.");
            var oldGuid = header.Get("CampaignGuid").String;
            Guid parsed;
            if (!Guid.TryParse(oldGuid, out parsed) || parsed == Guid.Empty) throw new FormatException("Ungültige Kampagnenkennung.");
            var clanGuid = doc.Root.Get("ClanSaveData").Get("CampaignGuid");
            if (clanGuid.String != oldGuid) throw new FormatException("Widersprüchliche Kampagnenkennungen.");
            if (CountGuid(doc.Root, oldGuid) != 2) throw new FormatException("Zusätzliche Kampagnenverweise; Export wird nicht geraten.");
            // A new manual save belongs to the existing campaign. Keep both GUID
            // fields verbatim; changing them would create another campaign group.
            // Consequently autosaves are shared, as disclosed before confirmation.
            var displayName = header.Get("DisplayName").String;
            if (!displayName.EndsWith(" " + displaySuffix, StringComparison.Ordinal)) displayName += " " + displaySuffix;
            var edits = new List<JsonEdit> {
                doc.Replace(header.Get("SaveName"), LosslessJson.Quote(saveName)),
                doc.Replace(header.Get("DisplayName"), LosslessJson.Quote(displayName)),
                doc.Replace(header.Get("IsAutoSave"), "false")
            };
            var slots = doc.Root.Get("PlayerUnitsSaveData").Get("QuickSlots");
            if (slots.Kind != "object") throw new FormatException("Unbekannte Skill-Slots.");
            foreach (var unit in slots.Members)
            {
                Guid id;
                if (!Guid.TryParse(unit.Name, out id)) throw new FormatException("Ungültige Charakterkennung.");
                var keys = unit.Value.Get("QuickSlots");
                if (keys.Kind != "array" || keys.Items.Count < 4 || keys.Items.Count > 12 || keys.Items.Any(x => x.Kind != "null" && x.Kind != "string")) throw new FormatException("Unbekanntes Skill-Slot-Format.");
                edits.Add(doc.Replace(keys, "[" + string.Join(",", keys.Items.Take(4).Select(doc.Raw)) + "]"));
            }
            var extra = doc.Root.Optional("DungeonSettlers10Slots_Items");
            if (extra != null)
            {
                if (extra.Get("Version").Integer != 1 || extra.Members.Count != 2 || extra.Get("Units").Kind != "object") throw new FormatException("Unbekannte Itemslot-Erweiterung.");
                foreach (var unit in extra.Get("Units").Members)
                {
                    Guid id;
                    if (!Guid.TryParse(unit.Name, out id) || unit.Value.Kind != "array" || unit.Value.Items.Count != 2 || unit.Value.Items.Any(x => x.Kind != "string" && x.Kind != "null")) throw new FormatException("Unbekannte Itemslot-Daten.");
                }
                edits.Add(doc.Remove(doc.Root, "DungeonSettlers10Slots_Items"));
            }
            return doc.Apply(edits);
        }
        private static int CountGuid(JsonNode node, string guid)
        {
            return (node.Kind == "string" && string.Equals(node.Text, guid, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                + node.Items.Sum(x => CountGuid(x, guid)) + node.Members.Sum(x => (string.Equals(x.Name, guid, StringComparison.OrdinalIgnoreCase) ? 1 : 0) + CountGuid(x.Value, guid));
        }
    }
}
