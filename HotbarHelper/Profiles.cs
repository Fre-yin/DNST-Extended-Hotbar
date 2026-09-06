using System;
using System.Collections.Generic;
using System.Linq;

namespace ExtendedHotbar.Helper
{
    internal static class Profiles
    {
        internal static bool Extra(int type) { return type >= 12005 && type <= 12012 || type == 12101 || type == 12102; }
        internal static bool Affected(int type) { return type >= 4 && type <= 23 || type >= 34 && type <= 37 || type == 60 || Extra(type); }
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
        {
            var doc = new LosslessJson(json);
            return "[" + string.Join(",", Bindings(doc).Items.Where(x => Affected(x.Get("InputType").Integer)).Select(doc.Raw)) + "]";
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
        {
            var doc = new LosslessJson(current);
            var original = Bindings(doc);
            var replacement = new LosslessJson(selected).Root;
            if (replacement.Kind != "array" || replacement.Items.Any(x => !Affected(x.Get("InputType").Integer))) throw new FormatException("Fremde Tasten im Hotbar-Profil.");
            Func<JsonNode, string> identity = x => x.Get("InputType").Integer + ":" + x.Get("SlotIndex").Integer;
            var replacements = replacement.Items.ToDictionary(identity, x => selected.Substring(x.Start, x.End - x.Start));
            var values = new List<string>();
            foreach (var entry in original.Items)
            {
                if (!Affected(entry.Get("InputType").Integer)) { values.Add(doc.Raw(entry)); continue; }
                string value; var key = identity(entry);
                if (replacements.TryGetValue(key, out value)) { values.Add(value); replacements.Remove(key); }
            }
            // Preserve existing row order; append only missing actions/columns.
            values.AddRange(replacement.Items.Where(x => replacements.ContainsKey(identity(x))).Select(x => replacements[identity(x)]));
            var result = doc.Apply(new[] { doc.Replace(original, "[" + string.Join(",", values) + "]") });
            Bindings(new LosslessJson(result));
            return result;
        }
        // Explicitly chosen fallback for users without a pre-mod binding backup.
        // Only this build's 25 native hotbar/selection actions are reset, not all settings.
        internal static string VanillaDefaults()
        {
            var rows = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                int digit = i == 9 ? 48 : 49 + i;
                for (int col = 0; col < 2; col++)
                {
                    rows.Add(Row(4 + i, col, col == 0 ? digit : 0, 0));
                    rows.Add(Row(14 + i, col, col == 0 ? digit : 0, col == 0 ? 304 : 0));
                }
            }
            var keys = new[] { 113, 101, 114, 116 };
            for (int i = 0; i < 4; i++) for (int col = 0; col < 2; col++) rows.Add(Row(34 + i, col, col == 0 ? keys[i] : 0, 0));
            rows.Add(Row(60, 0, 121, 0)); rows.Add(Row(60, 1, 0, 0));
            return "[" + string.Join(",", rows) + "]";
        }
        private static string Row(int type, int slot, int key, int modifier)
        {
            return "{\"InputType\":" + type + ",\"SlotIndex\":" + slot + ",\"KeyCode\":" + key + ",\"IsKeyDown\":true,\"ModifierKey\":" + modifier + "}";
        }
    }

    internal static class VanillaSave
    {
        internal static string Export(string source, string saveName, Guid campaignGuid, string displaySuffix = "(ohne Hotbar)")
        {
            if (campaignGuid == Guid.Empty || !System.Text.RegularExpressions.Regex.IsMatch(saveName, @"\A[A-Za-z0-9_-]{1,64}\z")) throw new FormatException("Ungültiger Exportname.");
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
            if (!Guid.TryParse(oldGuid, out parsed) || parsed == Guid.Empty || parsed == campaignGuid) throw new FormatException("Ungültige Kampagnenkennung.");
            var clanGuid = doc.Root.Get("ClanSaveData").Get("CampaignGuid");
            if (clanGuid.String != oldGuid) throw new FormatException("Widersprüchliche Kampagnenkennungen.");
            if (CountGuid(doc.Root, oldGuid) != 2) throw new FormatException("Zusätzliche Kampagnenverweise; Export wird nicht geraten.");
            var edits = new List<JsonEdit> {
                doc.Replace(header.Get("CampaignGuid"), LosslessJson.Quote(campaignGuid.ToString())),
                doc.Replace(clanGuid, LosslessJson.Quote(campaignGuid.ToString())),
                doc.Replace(header.Get("SaveName"), LosslessJson.Quote(saveName)),
                doc.Replace(header.Get("DisplayName"), LosslessJson.Quote(header.Get("DisplayName").String + " " + displaySuffix)),
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
