using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RLMapLoader.Components.Core.Constants;
using RLMapLoader.Components.Helpers.Extensions;
using RLMapLoader.Components.Interfaces;
using RLMapLoader.Components.Logging;
using Timer = System.Timers.Timer;

namespace RLMapLoader.Components.Core.Strategies
{
    public class InstallMapOverwrite : IInstallStrategy
    {
        private const string TAG = "InstallMapOverwrite";

        private readonly Stopwatch _stopwatch;
        private readonly Logger _logger;

        public InstallMapOverwrite()
        {
            _stopwatch = new Stopwatch();
            _logger = new Logger(TAG);
        }
        public (bool ok, string msg) Execute(Dictionary<string, string> installData)
        {
            string sourceMapPath, targetPath;
            //Parse install data, provided by installer. This class need to be independently executable
            try
            {
                sourceMapPath = installData[InstallConstants.InstallDataMembers.SourceLocation];
                targetPath = installData[InstallConstants.InstallDataMembers.TargetLocation];
            }
            catch (Exception e)
            {
                return (false, $"Failed to parse install data. \nMsg:{e.Message}");
            }
            //Strategy logic
            //get handle to source map file
            if (!File.Exists(sourceMapPath)) return (false, $"File does not exist '{sourceMapPath}'");
            using var inMapFile = File.OpenRead(sourceMapPath);
            // handle creation of target directory if needed
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            //load map bytes, may need to be multithreaded? Add speed check.
            _logger.LogDebug("Reading map...");
             _stopwatch.Start();
             var mapBytes = inMapFile.GetAllBytes();
             _stopwatch.Stop(); 
             _logger.LogDebug($"Read complete in {_stopwatch.ElapsedMilliseconds}ms.");
             _stopwatch.Reset();
            //once you have the bytes, write them out.
            _logger.LogDebug("Writing map...");
            _stopwatch.Start();
            File.WriteAllBytes($"{targetPath}\\{InstallConstants.StagingFileName}", mapBytes);
            _stopwatch.Stop();
            _logger.LogDebug($"Write complete in {_stopwatch.ElapsedMilliseconds}ms.");
            _stopwatch.Reset();
            //Done??
            //return old name for the installer to store in state. So that it can be retrieved on next run to revert.
            return (true, Path.GetFileName(sourceMapPath));
        }
    }

}