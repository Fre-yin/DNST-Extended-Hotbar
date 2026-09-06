namespace DungeonSettlers10Slots;

// Explicit insertion order: Dictionary enumeration order after removal/reuse is
// not a FIFO. Owners in TValue keep native pointers alive while they are cached.
internal sealed class BoundedSnapshotCache<TKey, TValue>
{
    readonly int capacity;
    readonly Dictionary<TKey, TValue> entries = new();
    readonly Queue<TKey> insertionOrder = new();
    internal BoundedSnapshotCache(int capacity)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        this.capacity = capacity;
    }
    internal void Put(TKey key, TValue value)
    {
        if (!entries.ContainsKey(key))
        {
            if (entries.Count == capacity) entries.Remove(insertionOrder.Dequeue());
            insertionOrder.Enqueue(key);
        }
        entries[key] = value;
    }
    internal bool TryGetValue(TKey key, out TValue value) => entries.TryGetValue(key, out value);
    internal void Clear() { entries.Clear(); insertionOrder.Clear(); }
}
