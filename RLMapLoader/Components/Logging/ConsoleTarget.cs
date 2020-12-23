using System;

namespace RLMapLoader.Components.Logging
{
    public class ConsoleTarget : ILogTarget
    {
        public (bool ok, string Message) PerformLog(string content)
        {
            Console.WriteLine(content);
            return (true, null);
        }
    }
}