using RLMapLoader.Components.Core;
using RLMapLoader.Components.Logging;
using RLMapLoader.Components.Models;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace RLMapLoader
{
    class Program
    {
        private const string TAG = "Program";
        private static string PROGRAM_STATE_PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"\\.rlLoader";

        private static Logger _programLogger = new Logger(TAG);
        static int Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            if(args.Length == 0)
            {
                ShowHelp();
                return 1;
            }
            else
            {
                //Resolve state
                var state = GetProgramState();
                try
                {
                    return ProcessArgs(args, state);
                }
                finally
                {
                    WriteState(state);
                }
            }
        }

        private static MapLoaderState GetProgramState()
        {
            if (DoesStateExist())
            {
                return ReadState(File.OpenText(PROGRAM_STATE_PATH));
            }
            else
            {
                var newState = new MapLoaderState();
                //write out
                WriteState(newState);
                return newState;

            }
        }

        private static MapLoaderState ReadState(StreamReader fromFile)
        {
            _programLogger.LogDebug("Reading state.");
            using (fromFile)
            {
                var b64State = fromFile.ReadToEnd();
                var stateBytes = Convert.FromBase64String(b64State);
                var stateJsonStr = Encoding.UTF8.GetString(stateBytes);
                return JsonSerializer.Deserialize<MapLoaderState>(stateJsonStr);
            }
        }

        private static void WriteState(MapLoaderState newState)
        {
            _programLogger.LogDebug("Writing state.");

            var serState = JsonSerializer.Serialize(newState);
            var serStateBytes = Encoding.UTF8.GetBytes(serState);
            var b64SerState = Convert.ToBase64String(serStateBytes);

            using var outFile = File.Create(PROGRAM_STATE_PATH);
            using var writer = new StreamWriter(outFile) {AutoFlush = true};

            writer.WriteLine(b64SerState);

            _programLogger.LogInfo("State writing successful.");
        }

        private static bool DoesStateExist() => File.Exists(PROGRAM_STATE_PATH);

        private static void ShowHelp()
        {
            Console.WriteLine("Currently supports the following usages: Load");
        }

        private static int ProcessArgs(string[] args, MapLoaderState withState)
        {
            var action = args[0];

            switch (action.ToLower())
            {
                case "load":
                    return new MapInstaller(args, ref withState).PerformLoad();
                case "list":
                    return new ListMaster(withState).List(args);
                case "sync":
                    /*
                     * TODO: for fun?
                     * sync from <userCode>
                     * sync 
                     * Investigate implementing google drive API to store and share map files.
                     * You can generate a unique code then share this code with your friend they can use it to sync map files
                     * The sync module can run the MapInstaller to install the map from a temporary location after downloading.
                     * For security should check file size, update name to the hash of the file, and file extension.
                     *
                     */
                default:
                    ShowHelp();
                    return 1;
            }
        }
    } 
}
