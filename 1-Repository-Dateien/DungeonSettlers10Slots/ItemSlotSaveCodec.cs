using System.Text.Json;

namespace DungeonSettlers10Slots;

internal static class ItemSlotSaveCodec
{
    internal const string Property = "DungeonSettlers10Slots_Items";
    static readonly JsonDocumentOptions ReadOptions = new()
    {
        AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip, MaxDepth = 256
    };

    internal static ItemSlotState Decode(string json)
    {
        try { return DecodeDocument(json); }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or FormatException)
        {
            return new ItemSlotState { SaveBlockReason = "Itemslot-Zusatzdaten konnten nicht eindeutig gelesen werden." };
        }
    }

    static ItemSlotState DecodeDocument(string json)
    {
        using var document = JsonDocument.Parse(json, ReadOptions);
        var state = new ItemSlotState();
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new FormatException();
        if (document.RootElement.EnumerateObject().Count(p => p.NameEquals(Property)) > 1) throw new FormatException();
        if (!document.RootElement.TryGetProperty(Property, out var extension)) return state;
        var raw = extension.GetRawText();
        try
        {
            if (extension.GetProperty("Version").GetInt32() != 1) throw new FormatException();
            foreach (var unit in extension.GetProperty("Units").EnumerateObject())
            {
                if (!Guid.TryParse(unit.Name, out var guid) || guid == Guid.Empty || unit.Value.GetArrayLength() != 2 || state.Units.Count >= 10000)
                    throw new FormatException();
                var keys = unit.Value.EnumerateArray().Select(item => item.GetString() ?? "").ToArray();
                if (keys.Any(key => key.Length > 1024)) throw new FormatException();
                state.Units.Add(guid.ToString(), keys);
            }
            // Unknown fields may belong to a newer writer; don't silently drop them.
            if (extension.EnumerateObject().Count() != 2) throw new FormatException();
            return state;
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException or KeyNotFoundException or ArgumentException)
        {
            return new ItemSlotState { OpaqueExtension = raw };
        }
    }

    internal static string Encode(string nativeJson, ItemSlotState state)
    {
        if (state == null || state.SaveBlockReason != null)
            throw new FormatException(state?.SaveBlockReason ?? "Itemslot-Zustand fehlt.");
        // Validate rather than guessing from the last brace. Do not emit a second
        // extension if another writer has already supplied one.
        bool nonEmpty;
        try
        {
            using var document = JsonDocument.Parse(nativeJson, new JsonDocumentOptions { MaxDepth = 256 });
            if (document.RootElement.ValueKind != JsonValueKind.Object || document.RootElement.TryGetProperty(Property, out _))
                throw new FormatException("Native campaign must be an object without an existing item extension.");
            nonEmpty = document.RootElement.EnumerateObject().Any();
        }
        catch (JsonException ex) { throw new FormatException("Native campaign is not valid JSON.", ex); }
        // Keep all native JSON bytes intact, including numeric precision and type
        // metadata. This runs only inside SaveFile, before its original atomic write.
        var end = nativeJson.Length - 1;
        while (end >= 0 && char.IsWhiteSpace(nativeJson[end])) end--;
        if (end < 0 || nativeJson[end] != '}') throw new FormatException("Native campaign is not a JSON object.");
        var extra = state.OpaqueExtension ?? JsonSerializer.Serialize(new { Version = 1, Units = state.Units });
        return nativeJson.Insert(end, (nonEmpty ? "," : "") + "\"" + Property + "\":" + extra);
    }
}
