using System.Text.Json;
using DungeonSettlers10Slots;

var failed = 0;
var passed = 0;
var property = ItemSlotSaveCodec.Property;
var id = "b480d9fa-af1e-473f-b4ba-82017384857a";
void Check(bool value) { if (!value) throw new Exception("assertion failed"); }
void Reject(Action action)
{
    try { action(); }
    catch (FormatException) { return; }
    throw new Exception("unsafe write was accepted");
}
void Test(string name, Action action)
{
    try { action(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
}
string Wrap(string payload) => "{\"" + property + "\":" + payload + "}";

Test("vanilla save", () => Check(ItemSlotSaveCodec.Decode("{}").Units.Count == 0));
Test("keys, escaping, numeric precision and native bytes", () =>
{
    var state = new ItemSlotState();
    state.Units.Add(id, new[] { "ITEM_\"quoted\\key", "Ä\nfood" });
    var native = "{\"Native\":9007199254740993,\"Nested\":{\"x\":1e-50}} \r\n";
    var json = ItemSlotSaveCodec.Encode(native, state);
    Check(json.StartsWith(native[..native.LastIndexOf('}')]) && json.EndsWith("} \r\n"));
    var restored = ItemSlotSaveCodec.Decode(json);
    Check(restored.Units[id].SequenceEqual(state.Units[id]));
});
Test("snapshot copies do not alias live keys", () =>
{
    var state = new ItemSlotState(); state.Units.Add(id, new[] { "a", "b" });
    var snapshot = state.Copy(); state.Units[id][0] = "changed";
    Check(snapshot.Units[id][0] == "a");
});
foreach (var payload in new[] { "null", "[]", "{\"Version\":2,\"Future\":true}",
    "{\"Version\":1,\"Units\":{},\"Future\":9007199254740993}",
    "{\"Version\":1,\"Units\":{\"bad-guid\":[\"a\",\"b\"]}}" })
    Test("opaque extension preserved: " + payload, () =>
    {
        var state = ItemSlotSaveCodec.Decode(Wrap(payload));
        Check(state.OpaqueExtension == payload);
        Check(ItemSlotSaveCodec.Encode("{}", state) == Wrap(payload));
    });
Test("reject invalid native JSON on write", () => Reject(() => ItemSlotSaveCodec.Encode("this is not JSON}", new ItemSlotState())));
Test("reject duplicate extension on write", () => Reject(() => ItemSlotSaveCodec.Encode(Wrap("{}"), new ItemSlotState())));
Test("native-compatible comments/trailing commas on load", () =>
    Check(ItemSlotSaveCodec.Decode("{/*comment*/\"Native\":1,}").SaveBlockReason == null));
foreach (var json in new[] { "", "[1,2]", "{bad", "{\"" + property + "\":{},\"" + property + "\":{}}" })
    Test("unreadable/ambiguous input blocks subsequent save: " + json, () =>
    {
        var state = ItemSlotSaveCodec.Decode(json);
        Check(state.SaveBlockReason != null && state.Copy().SaveBlockReason == state.SaveBlockReason);
        Reject(() => ItemSlotSaveCodec.Encode("{}", state));
    });
Test("FIFO stays bounded and retains the newest 64 through slot reuse", () =>
{
    var cache = new BoundedSnapshotCache<int, string>(64);
    for (var i = 0; i < 1000; i++) cache.Put(i, i.ToString());
    for (var i = 0; i < 1000; i++) Check(cache.TryGetValue(i, out var text) == (i >= 936) && (i < 936 || text == i.ToString()));
    cache.Put(936, "updated"); cache.Put(1000, "new");
    Check(!cache.TryGetValue(936, out _) && cache.TryGetValue(937, out _));
    cache.Clear(); Check(!cache.TryGetValue(1000, out _));
    cache.Put(1, "after-clear"); Check(cache.TryGetValue(1, out var value) && value == "after-clear");
});
if (args.Length == 1)
    Test("real sandbox autosave read-only roundtrip", () =>
    {
        var json = File.ReadAllText(args[0]);
        var state = ItemSlotSaveCodec.Decode(json);
        Check(state.Units.Count > 0 && state.OpaqueExtension == null);
        Check(ItemSlotSaveCodec.Decode(ItemSlotSaveCodec.Encode("{}", state)).Units[id].SequenceEqual(state.Units[id]));
    });
LocalizationChecks.Run(Test);
Console.WriteLine($"RESULT {passed} passed, {failed} failed");
return failed == 0 ? 0 : 1;
