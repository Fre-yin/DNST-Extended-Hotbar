using System.Globalization;

namespace DungeonSettlers10Slots;

// Extend the game's translated singular slot label, preserving word order and
// punctuation. No independent translation catalogue to drift from the game.
internal static class SlotText
{
    internal static string Skill(string translated, string english, int slot) =>
        Numbered(translated, english, "Use Skill 1", slot);

    internal static string Item(string translated, string english, int slot) =>
        Numbered(translated, english, "Use Item Quick Slot 1", slot);

    internal static bool IsResolved(string text) => !string.IsNullOrWhiteSpace(text)
        && !text.Trim().Equals("TODO", StringComparison.OrdinalIgnoreCase)
        && !text.Contains("TEXTKEY_", StringComparison.OrdinalIgnoreCase)
        && !text.Contains("TextKey not found", StringComparison.OrdinalIgnoreCase);

    private static string Numbered(string translated, string english, string fallback, int slot)
    {
        if (slot < 1) throw new ArgumentOutOfRangeException(nameof(slot));
        var text = IsResolved(translated) ? translated : IsResolved(english) ? english : fallback;
        var number = slot.ToString(CultureInfo.InvariantCulture);
        var inTag = false;
        for (var i = 0; i < text.Length; i++)
        {
            // TMP formatting can itself contain digits (e.g. <color=#111111>).
            if (text[i] == '<') { inTag = true; continue; }
            if (text[i] == '>') { inTag = false; continue; }
            if (inTag || text[i] != '1'
                || (i > 0 && char.IsDigit(text[i - 1]))
                || (i + 1 < text.Length && char.IsDigit(text[i + 1]))) continue;
            return text[..i] + number + text[(i + 1)..];
        }
        // The shipped Korean item label has no number at all.
        return text + " " + number;
    }
}
