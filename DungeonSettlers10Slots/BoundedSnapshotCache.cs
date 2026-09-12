namespace DungeonSettlers10Slots;

// Explicit insertion order: Dictionary enumeration order after removal/reuse is
// not a FIFO. Owners in TValue keep native pointers alive while they are cached.
internal sealed class BoundedSnapshotCache<TKey, TValue>
{
    readonly int capacity;
    readonly Dictionary<TKey, TValue> entries = new();
    readonly Queue<TKey> insertionOrder = new();
    readonly object sync = new();
    internal BoundedSnapshotCache(int capacity)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        this.capacity = capacity;
    }
    internal void Put(TKey key, TValue value)
    {
        lock (sync)
        {
            if (!entries.ContainsKey(key))
            {
                if (entries.Count == capacity)
                {
                    while (insertionOrder.Count != 0 && !entries.Remove(insertionOrder.Dequeue())) { }
                }
                insertionOrder.Enqueue(key);
            }
            entries[key] = value;
        }
    }
    internal bool TryGetValue(TKey key, out TValue value)
    {
        lock (sync) return entries.TryGetValue(key, out value);
    }
    internal void Clear()
    {
        lock (sync) { entries.Clear(); insertionOrder.Clear(); }
    }
}
