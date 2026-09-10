namespace DungeonSettlers10Slots;

internal sealed class HotbarLog
{
    private readonly Action<string> info, warning, error;
    internal HotbarLog(Action<string> info, Action<string> warning, Action<string> error)
    {
        this.info = info;
        this.warning = warning;
        this.error = error;
    }
    internal void Msg(string message) => info(message);
    internal void Warning(string message) => warning(message);
    internal void Error(string message) => error(message);
}
