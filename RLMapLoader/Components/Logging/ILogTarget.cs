namespace RLMapLoader.Components.Logging
{
    public interface ILogTarget
    {
        (bool ok, string Message) PerformLog(string content);
    }
}