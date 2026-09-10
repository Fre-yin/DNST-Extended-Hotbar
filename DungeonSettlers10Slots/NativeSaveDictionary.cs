#if BEPINEX
using global::Refactor.Main;
#else
using Il2CppRefactor.Main;
#endif

namespace DungeonSettlers10Slots;

internal static class NativeSaveDictionary
{
    internal static void VerifyInterop()
    {
        // Disposable objects only: catch loader marshalling changes before
        // installing any native slot/save hooks, even on normal launches.
        var dictionary = new Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Guid, QuickSlotSaveData>();
        var key = Il2CppSystem.Guid.NewGuid();
        var slots = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray(new[] { "interop_probe" });
        Add(dictionary, key, new QuickSlotSaveData { QuickSlots = slots, ItemQuickSlotKey = "interop_item" });
        var stored = dictionary[key];
        if (stored.QuickSlots?.Pointer != slots.Pointer || stored.ItemQuickSlotKey != "interop_item")
            throw new InvalidOperationException("Native save dictionary interop failed; hotbar must remain disabled.");
    }

    internal static void Add(Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Guid, QuickSlotSaveData> dictionary,
        Il2CppSystem.Guid key, QuickSlotSaveData value)
    {
#if BEPINEX
        // BE.788 generates Dictionary<TKey,TValue>.Add without unboxing wrapped
        // native value types. QuickSlotSaveData is such a non-blittable struct:
        // passing its object header as struct data corrupts the stored fields.
        // The native IDictionary entry point explicitly accepts boxed objects
        // and performs its own checked unboxing. Do not patch the loader or
        // hand-copy native memory; preserve the original dictionary and schema.
        dictionary.Cast<Il2CppSystem.Collections.IDictionary>().Add(key.BoxIl2CppObject(), value);
#else
        dictionary.Add(key, value);
#endif
    }
}
