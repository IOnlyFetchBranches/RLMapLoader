using System;
using System.Globalization;
using System.IO;
using RLMapLoader.Components.Helpers;
using RLMapLoader.Components.Logging;
using RLMapLoader.Components.Models;

namespace RLMapLoader.Components.Core
{
    public class MapInstaller:Component
    {
        private string _mode; //the operation mode of the installer
        private int _forMapWorkShopId;
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
        /// Sets installer state variables. TODO: Seperate out state object?
        /// </summary>
        /// <param name="args"></param>
        private void ConfigureRunParameters(string[] args)
        {
            if (args.Length != 2)
            {
                throw new Exception("Invalid argument count! Expected 2.");
            }

            _mode = args[0];

            var ok = Int32.TryParse(args[1], out _forMapWorkShopId);
            if (!ok)
            {
                throw new Exception("Invalid second parameter, pass integer ID for workshop map. Hint: call 'list workshop' first.");
            }

        }

        public int PerformLoad()
        {
            try
            {
                _logger.LogInfo($"Begin install to {_modFolderPath}");

                //get handle to source map file
                //get handle to mod directory
                //check if map is already staged
                //if map is already staged, do we warn? yes by default, but user doesnt have choice rn
                //then, if map is staged, rename to stored name. We may need a configmodule to handle saving the previous name....

               
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to LoadMap.", e);
                return 1;
            }
            return 0;
        }
    
    }
}