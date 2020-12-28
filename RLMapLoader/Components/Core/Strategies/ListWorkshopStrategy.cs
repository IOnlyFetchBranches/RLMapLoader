using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualBasic;
using RLMapLoader.Components.Helpers;
using RLMapLoader.Components.Helpers.Extensions;
using RLMapLoader.Components.Interfaces;
using RLMapLoader.Components.Logging;

namespace RLMapLoader.Components.Core.Strategies
{
    public class ListWorkshopStrategy : IListStrategy
    {
        private List<string> _metadataFileExt = new List<string>{".json",".vdf"}; 
        private SteamHelper _steamHelper;
        private Logger _logger;

        public ListWorkshopStrategy(Logger _withLogger)
        {
            _steamHelper = new SteamHelper();
            _logger = _withLogger;
        }
        public (List<MapInfo> infos, string msg) Execute()
        {
            //check component health
            if (!_steamHelper.IsHealthy)
            {
                return (null, "Steam Helper is not healthy. BaseSteamPath: " + _steamHelper.SteamInstallPath);
            }
            //return object init
            var retInfos = new List<MapInfo>();
            //get workshop path.
            var wsPath = _steamHelper.GetRLWorkshopPath();
            //iterate through all the folders
            var wsFolderNames = Directory.EnumerateDirectories(wsPath);
            foreach (var wsFolderName in wsFolderNames)
            {
                try
                {
                    var info = GetMapData(wsFolderName);
                    retInfos.Add(info);
                }
                catch 
                {
                 _logger.LogWarning($"Failed to load metadata for wsPath: {wsFolderName}");   
                }
            }
            return (retInfos, null);
        }

        private MapInfo GetMapData(string wsPath)
        {
           //open folder
           var innerContents = Directory.EnumerateFiles(wsPath);
           //find metadata file (if exists), you need to filter through enumeration.
           try
           {
               var metadataFile = innerContents.First(name => _metadataFileExt.Contains(Path.GetExtension(name.ToLower())));
               return ParseMetadata(metadataFile);
           }
           catch (Exception e)
           {
               return GenerateMetadata(wsPath);
           }
        }

        private MapInfo GenerateMetadata(string wsPath)
        {
            try
            {
                var innerContents = Directory.EnumerateFiles(wsPath);
                var mapFile = (innerContents.First(t =>
                    Path.GetExtension(t).Equals(".udk", StringComparison.CurrentCultureIgnoreCase)));

                var name = Path.GetFileName(mapFile);
                var id = wsPath.Substring(wsPath.LastIndexOf(Path.DirectorySeparatorChar) + 1);

                return new MapInfo {ID = id, Name = name, Description = "N/A"};
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to generate any metadata for wsPath: {wsPath}");
            }
        }

        private  MapInfo ParseMetadata(string metadataFilePath)
        {
            string id, name, description;
            JsonElement selected = new JsonElement();

            //determine type. VDF? JSON?
            var type = Path.GetExtension(metadataFilePath);
            //read
            var fileContents = File.ReadAllText(metadataFilePath);
            //validate
            if (!fileContents.StartsWith("{") && fileContents.TrimStart().StartsWith('"')) 
                fileContents = fileContents.VdfToJson();
            //parse TODO:fix jsons never laoding            
            var jDoc = JsonDocument.Parse(fileContents);
            if (type.Equals(".json", StringComparison.CurrentCultureIgnoreCase))
            {
                var ok = jDoc.RootElement.TryGetProperty("ItemID", out selected);
                id = ok ? selected.GetRawText() : "none";
                ok = jDoc.RootElement.TryGetProperty("Title", out selected);
                name = ok ? selected.GetRawText() : Path.GetFileName(metadataFilePath);
                ok = jDoc.RootElement.TryGetProperty("Description", out selected);
                description = ok ? selected.GetRawText() : "none";

            }
            else if (type.Equals(".vdf", StringComparison.CurrentCultureIgnoreCase))
            {
                var ok = jDoc.RootElement.GetProperty("workshopitem").TryGetProperty("publishedfileid", out selected);
                id = ok ? selected.GetString() : "none";
                ok = jDoc.RootElement.TryGetProperty("title", out selected);
                name = ok ? selected.GetString() : Path.GetFileName(metadataFilePath);
                ok = jDoc.RootElement.TryGetProperty("description", out selected);
                description = ok ? selected.GetString() : "none";
            }

            else
            {
                //invalid
                throw new Exception($"Failed trying to parse the following metadata:\n{metadataFilePath}");
            }

            //return
            return new MapInfo { ID = id, Description = description, Name = name };
        }
    }
}