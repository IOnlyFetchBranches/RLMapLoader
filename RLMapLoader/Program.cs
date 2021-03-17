using RLMapLoader.Components.Core;
using RLMapLoader.Components.Logging;
using RLMapLoader.Components.Models;
using System;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using RLMapLoader.Components;
using RLMapLoader.Components.Core.Constants;

namespace RLMapLoader
{
    class Program
    {
        private const string TAG = "Program";
        private static string PROGRAM_STATE_PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"\\.rlLoader";

        private static Logger _programLogger = new Logger(TAG);
        private static UserModule _user;

        static int Main(string[] args)
        {
            //Resolve state, only used for settings/previous run status.
            var state = GetProgramState();
            
            //Check for previous login. If active, instantiate user module here
            if (state.IsLoggedOn)
            {
                CreateUserModule(ref state);
            }

            if (args.Length == 0)
            {
                ShowHelp();
                return BeginCommandLoop(state);
            }
            else
            {
                try
                {
                    return ProcessArgs(args, state);
                }
                finally
                {
                    state.IsFirstTime = false;
                    WriteState(state);
                }
            }
        }


        private static int BeginCommandLoop(MapLoaderState withState)
        {
            var loopCount = 0;
            var invalid = true;
            while (true)
            {
                loopCount++;
                Console.WriteLine("\nPlease enter a command...");
                var input = Console.ReadLine();
                //Prevent console spam
                if (loopCount == 5)
                {
                    Console.Clear();
                    loopCount = 0;
                }

                var args = input.Split(null);

                if (input.ToLower() == "exit")
                {
                    _programLogger.LogInfo("Closing...");
                    Thread.Sleep(5000);
                    return 0;
                }
                else
                {
                    try
                    {
                        invalid = ProcessArgs(args, withState) == 1;
                    }
                    catch(Exception e)
                    {
                        _programLogger.LogError($"Caught program exception, while running command: {args.Aggregate( (i,j) => i + ' ' + j)}", e);
                    }
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

            _programLogger.LogDebug("State writing successful.");
        }

        private static bool DoesStateExist() => File.Exists(PROGRAM_STATE_PATH);

        private static void ShowHelp()
        {
            Console.WriteLine("Currently supports the following usages: 'status', 'load <workshopId>', 'unload', 'list workshop', 'exit'");
        }

        private static void ShowAppStatus(MapLoaderState withState)
        {
            Console.WriteLine($"|{"Authenticated",15} | {withState.IsLoggedOn}");
            Console.WriteLine($"|{"Current User",15} | {withState.PrivateUserEmail}");
            Console.WriteLine($"|{"User ID",15} | {withState.UserId}");
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine($"\n|{"Is Map Loaded",15} | {withState.IsMapLoaded}");
            Console.WriteLine($"|{"Loaded Map Name",15} | {withState.LoadedMapName}");
            Console.WriteLine($"|{"Last Known Map Name",15} | {withState.LastKnownMapName}");

        }

        private static int ProcessArgs(string[] args, MapLoaderState withState)
        {
            var action = args[0];
            int exitCode;

            switch (action.ToLower())
                {
                    case "load":
                        exitCode= new MapInstaller(args, ref withState).PerformLoad();
                        WriteState(withState);
                        break;
                    case "unload":
                        exitCode = new MapInstaller(args, ref withState).PerformUnLoad();
                        WriteState(withState);
                    break;
                    case "list":
                        exitCode= new ListMaster().List(args);
                        break;
                    case "status":
                        ShowAppStatus(withState);
                        return 0;
                    case "login":
                        //TODO: run this method async? Right now, now really a big deal imo. Will need to be converted in UI tho
                        exitCode = CreateUserModule(ref withState);
                        WriteState(withState);
                    break;
                    case "logout":
                        //TODO: run this method async? Right now, now really a big deal imo. Will need to be converted in UI tho
                        exitCode = DestroyUserModule(ref withState);
                        WriteState(withState);
                    break;

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

            return exitCode;
        }

        //TODO:Convert user login logic to async for UI
        private static int CreateUserModule(ref MapLoaderState withState)
        {
            if (withState.IsLoggedOn && _user != null)
            {
                _programLogger.LogError("User already logged on. Call logout first.");
                return 1;
            }
            _user = new UserModule();
            var ok = _user.InitializeAsync().Result;
            if (!ok)
            {
                return 1;
            }
            withState.IsLoggedOn = _user.IsActive;
            withState.PrivateUserEmail = _user.AuthProfile.User.Email;
            withState.UserId = _user.AuthProfile.User.LocalId;
            return 0;
        }
        private static int DestroyUserModule(ref MapLoaderState withState)
        {
            if (!withState.IsLoggedOn)
            {
                _programLogger.LogError("User not logged on. Call login first.");
                return 1;
            }
            var exitCode =   _user.LogoutAsync().Result ? 0 : 1;
            if (exitCode == 0)
            {
                withState.UserId = "";
                withState.PrivateUserEmail = "";
                withState.IsLoggedOn = false;
            }

            return exitCode;
        }

    } 
}
