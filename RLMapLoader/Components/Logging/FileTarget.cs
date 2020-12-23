using System;
using System.IO;
using System.Text;

namespace RLMapLoader.Components.Logging
{
    public class FileTarget : ILogTarget
    { 
        public string BackingFilePath { get; }

        public FileTarget(Environment.SpecialFolder inRoot)
        {
            BackingFilePath = Environment.GetFolderPath(inRoot) + "\\rl-loader.log";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content">Must be UTF8</param>
        public (bool ok, string Message) PerformLog(string content)
        {
            using var logFile = File.Open(BackingFilePath, FileMode.OpenOrCreate);
            try
            {
                var contentBytes = Encoding.UTF8.GetBytes(content);
                logFile.Write(contentBytes);
                logFile.Flush();
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }

            return (true,null);

        }
    }
}