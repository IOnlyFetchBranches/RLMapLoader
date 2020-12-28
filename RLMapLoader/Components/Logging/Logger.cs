using System;
using System.Collections.Generic;

namespace RLMapLoader.Components.Logging
{
    public class Logger
    {
        //TODO: Set this to false
        private readonly bool DEBUG_MODE;

        public string ComponentName { get; }

        private List<ILogTarget> _targets = new List<ILogTarget>();
       
        public Logger(string componentName, List<ILogTarget> withTargets = null)
        {
#if DEBUG
            DEBUG_MODE = true;
#else
            DEBUG_MODE = false;
#endif
            ComponentName = componentName;
            ConfigureTargets(withTargets);
        }

        public void LogDebug(string message)
        {
            var content = $"{GetHeaderString("Debug")} {message}";
            PerformLog(content);
        }
        public void LogError(string message, Exception e = null)
        {
            if(!DEBUG_MODE) Console.WriteLine("Error has been logged. Operation may not have been successful.");

            var content = e != null
                ? $"{GetHeaderString("Error")} {message} \nEx.msg: {e?.Message} \n\n {e?.StackTrace} "
                : $"{GetHeaderString("Error")} {message}";
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

        /// <summary>
        /// We don't console log outside of DEBUG_MODE
        /// Maybe not the best fix, but LogError writes a user friendly notification that something fucked up when not in DEBUG_MODE
        /// </summary>
        private void LoadDefaultTargets()
        {
            if(DEBUG_MODE)
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