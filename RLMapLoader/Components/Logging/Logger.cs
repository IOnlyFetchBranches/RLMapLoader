using System;
using System.Collections.Generic;

namespace RLMapLoader.Components.Logging
{
    public class Logger
    {
        private const bool DEBUG_MODE = false;

        public string ComponentName { get; }

        private List<ILogTarget> _targets = new List<ILogTarget>();
       
        public Logger(string componentName, List<ILogTarget> withTargets = null)
        {
            ComponentName = componentName;
            ConfigureTargets(withTargets);
        }

        public void LogDebug(string message)
        {
            if (!DEBUG_MODE) return;
            var content = $"{GetHeaderString("Debug")} {message}";
            PerformLog(content);
        }
        public void LogError(string message, Exception e = null)
        {
            var content = $"{GetHeaderString("Error")} {message} \nEx.msg: {e?.Message} \n\n {e?.StackTrace}";
            PerformLog(content);
        }
        public void LogWarning(string message)
        {
            var content = $"{GetHeaderString("Warning")} {message}";
            PerformLog(content);
        }
        public void LogInfo(string message)
        {
            var content = $"{GetHeaderString("Info")} {message}";
            PerformLog(content);
        }

        private void LoadDefaultTargets()
        {
            _targets.Add(new ConsoleTarget());
            _targets.Add(new FileTarget(Environment.SpecialFolder.MyDocuments));
        }
        private void ConfigureTargets(List<ILogTarget> withTargets)
        {
            if (withTargets == null)
            {
                LoadDefaultTargets();
            }
            else
            {
                _targets = withTargets;
            }
        }
        private void PerformLog(string content)
        {
            _targets.ForEach(target =>
            {
                var res = target.PerformLog(content);
                if (!res.ok)
                {
                    Console.WriteLine($"[{DateTime.Now}] [Error] failed to performLog on target!");
                }
            });
        }
        private string GetHeaderString(string level)
        {
            return $"[{DateTime.Now:g}]-[{level.ToUpper()}]-";
        }

       
    }
}