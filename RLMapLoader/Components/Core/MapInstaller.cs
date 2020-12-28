using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using RLMapLoader.Components.Core.Constants;
using RLMapLoader.Components.Core.Strategies;
using RLMapLoader.Components.Helpers;
using RLMapLoader.Components.Helpers.Extensions;
using RLMapLoader.Components.Interfaces;
using RLMapLoader.Components.Logging;
using RLMapLoader.Components.Models;

namespace RLMapLoader.Components.Core
{
    public class MapInstaller:Component
    {
        private string _mode; //the operation mode of the installer
        private  long _forMapWorkShopId;
        //You can assume a RL context on the following variables
        private string _modFolderPath, _cookedFolderPath, _workshopFolderPath;

        private readonly MapLoaderState _state;
        private readonly SteamHelper _steamHelper;

        public MapInstaller(string[] args, ref MapLoaderState withState)
        {
            _state = withState;
            _steamHelper = new SteamHelper();
            if (!_steamHelper.IsHealthy)
            {
                throw new Exception($"SteamHelper could not resolve an important path. Base Steam Dir: {_steamHelper?.SteamInstallPath}");
            }

            //maploader load <workshopId>
            ConfigureRunParameters(args);
            ResolveDirectories();
        }

        private void ResolveDirectories()
        {
            var rlPath = _steamHelper.GetRLInstallPath();
            _cookedFolderPath = $"{rlPath}\\TAGame\\CookedPCConsole";

            _modFolderPath = $"{_cookedFolderPath}\\mods";
            if (!Directory.Exists(_modFolderPath))
            {
                _logger.LogWarning($"Mod path not found {_modFolderPath}");
            }
            //
            _workshopFolderPath = _steamHelper.GetRLWorkshopPath();
        }

        /// <summary>
        /// Sets installer state variables. TODO: Separate out state object?
        /// </summary>
        /// <param name="args"></param>
        private void ConfigureRunParameters(string[] args)
        {

            _mode = args[0].ToLower();

            if (_mode =="load"){
                
                if(args.Length != 2)
                {
                    throw new Exception("Invalid argument count! Expected at least (2)");
                }

                var ok = long.TryParse(args[1], out _forMapWorkShopId);
                if (!ok)
                {
                    throw new Exception("Invalid second parameter, pass integer ID for workshop map. Hint: call 'list workshop' first.");
                }

            }
            else if(_mode == "unload" && args.Length != 1){
                throw new Exception("Invalid argument count! Expected only 1");
            }



        }


        private IInstallStrategy DetermineStrategy(MapLoaderState withState)
        {
            if (withState.IsMapLoaded)
                _logger.LogDebug($"Earlier map load detected. {withState.LoadedMapName}");
            
            //Only one strategy for now, util cloud sync  TODO: Create CloudSyncStrategy
            return new InstallMapOverwrite();
        }

        public int PerformLoad()
        {
            try
            {
                _logger.LogInfo($"Begin install to {_modFolderPath}");
                var res = DetermineStrategy(_state).Execute(GenerateInstallData());

                if (!res.ok)
                {
                    _logger.LogError($"Unsuccessful install execution detected. \nMsg: {res.msg}");
                    return 1;
                }

                _state.IsMapLoaded = true;
                _state.LoadedMapName = res.msg;

                if(_state.LastKnownMapName == null)
                {
                    _state.LastKnownMapName = _state.LoadedMapName;
                }
                return 0;

            }
            catch (Exception e)
            {
                _logger.LogError("Failed to LoadMap.", e);
                return 1;
            }
        }

        public  int PerformUnLoad()
        {
            if (!_state.IsMapLoaded)
            {
                Console.WriteLine("You don't currently have any maps loaded! Call load <workshopId>, see help for more info.");
                return 1;
            }
            var baseInstallLoc = $"{_modFolderPath}\\{InstallConstants.StagingFileName}";
            try
            {
                File.Move(baseInstallLoc, baseInstallLoc + ".off");
                //state updates
                _state.IsMapLoaded = false;
                _state.LastKnownMapName = _state.LoadedMapName;
                _state.LoadedMapName = null;
                
                _logger.LogInfo("Done.");
            }catch(Exception e)
            {
                _logger.LogError("Could not unload map!", e);
                return 1;
            }
            return 0;
        }

        private Dictionary<string, string> GenerateInstallData()
        {
            switch (_mode)
            {
                case "load":
                    string sourceMapPath = null;
                    var startingSourcePath = $"{_workshopFolderPath}\\{_forMapWorkShopId}";
                    if (!Directory.Exists(startingSourcePath)) throw new Exception($"Could not find workshop data for id: {_forMapWorkShopId}");
                    //search for the map file.
                    foreach (var file in Directory.EnumerateFiles(startingSourcePath))
                    {
                        
                        if (file.EndsWith(".udk"))
                        {
                            sourceMapPath = file;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(sourceMapPath))
                    {
                        var ex = new Exception($"Found no valid udk file in path {startingSourcePath}");
                        _logger.LogError("Failed install.", ex);
                        throw ex;
                    }
                   
                    //Build basic load params
                    return new Dictionary<string,string>
                    {
                        {Constants.InstallConstants.InstallDataMembers.SourceLocation, sourceMapPath},
                        {Constants.InstallConstants.InstallDataMembers.TargetLocation, _modFolderPath}
                    }.WithNullCheck();
                default:
                    throw new Exception("Invalid mode.");
            }
        }
    }
}