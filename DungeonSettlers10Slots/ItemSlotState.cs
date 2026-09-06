namespace DungeonSettlers10Slots;

// Extra item keys never enter the native skill array or replace the first item key.
internal sealed class ItemSlotState
{
    internal readonly Dictionary<string, string[]> Units = new(StringComparer.OrdinalIgnoreCase);
    internal string OpaqueExtension;
    // A missing/ambiguous association must never become an empty, writable save.
    internal string SaveBlockReason;
    internal ItemSlotState Copy()
    {
        var copy = new ItemSlotState { OpaqueExtension = OpaqueExtension, SaveBlockReason = SaveBlockReason };
        foreach (var pair in Units) copy.Units.Add(pair.Key, (string[])pair.Value.Clone());
        return copy;
    }
}
