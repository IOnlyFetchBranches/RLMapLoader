using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32;
using RLMapLoader.Components.Core;
using RLMapLoader.Components.Logging;
using static System.Char;

namespace RLMapLoader.Components.Helpers
{
    public class SteamHelper : Component
    {
        private const string STEAM_REG_64 = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam";
        private const int RL_STEAM_ID = 252950; 
        public  string SteamInstallPath => _sInstallPath ?? LoadSteamRegistryKey("InstallPath");
        
        public bool IsHealthy;
        private Logger _logger;

        private static string _sInstallPath;
        //VDF library data file path, acf Game metadata file path, steamapps folder location, RL Install Path,
        private string _vdfPath, _acfPathPrefix, _libPath, _rlDir, _wsPath;

        /// <summary>
        /// Check IsHealthy and print SteamInstallPath if false.
        /// </summary>
        public SteamHelper()
        {
            _logger = new Logger(TAG);
            _vdfPath = SteamInstallPath + "\\steamapps\\libraryfolders.vdf";
            _acfPathPrefix = $"\\steamapps\\appmanifest_{RL_STEAM_ID}.acf";
            _libPath = GetLibraryFolders().Find(folderPath => File.Exists(folderPath + _acfPathPrefix));
            IsHealthy = SteamInstallPath != null && _vdfPath != null;
        }

        public string GetRLInstallPath()
        {
            if (_rlDir != null) return _rlDir;
            if (!IsHealthy) return null;
            try
            {
                using var rlAcfFile = File.OpenText(_libPath + _acfPathPrefix);
                var acfFields = JsonDocument.Parse(rlAcfFile.ReadToEnd());
                var rlDirName = acfFields.RootElement.GetProperty("installDir").GetString();
                _rlDir = $"{_libPath}\\steamapps\\common\\{rlDirName}";

                if (_rlDir == null) _logger.LogWarning("Was unable to determine RL install path.");
                return _rlDir;
            }
            catch (Exception e)
            {
                _logger.LogError("Encountered exception while determining RL install path", e);
                return null;
            }
        }   
        public string GetRLWorkshopPath()
        {
            //start with steam lib base path
            _wsPath ??= $"{_libPath}\\steamapps\\workshop";
            //add id parameter
            return $"{_wsPath}\\{RL_STEAM_ID}";

        }

        private  string LoadSteamRegistryKey(string key)
        {
            try
            {
                using var steamTopLevelKey = Registry.LocalMachine.OpenSubKey(STEAM_REG_64);
                _sInstallPath =  steamTopLevelKey?.GetValue(key) as string;
                return _sInstallPath;
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to load Steam Registry Key {key}", e);
                return null;
            }
        }

        private List<string> GetLibraryFolders()
        {
            if (IsHealthy) return null;

            //parse VDF
            try
            {
                using var libVdf = File.OpenText(_vdfPath);
                var vdfDoc = JsonDocument.Parse(libVdf.ReadToEnd());

                var folderPaths = new List<string>();
                foreach (var property in vdfDoc.RootElement.EnumerateObject())
                {
                    if (IsDigit(property.Name, 0))
                    {
                        folderPaths.Add(property.Value.GetString());
                    }
                }

                return folderPaths;
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to get library folders", e);
                return null;
            }
        }

     
    }
}