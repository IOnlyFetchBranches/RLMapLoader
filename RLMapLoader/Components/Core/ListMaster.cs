using RLMapLoader.Components.Core.Strategies;
using RLMapLoader.Components.Interfaces;
using RLMapLoader.Components.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RLMapLoader.Components.Core
{
    public class ListMaster : Component
    {
       

        public int List(string[] args)
        {
            if (args.Length != 2)
            {
                _logger.LogWarning("Incorrect argument count for list. Try list workshop");
                return 1;
            }
            //what are we listing.
            var listTarget = args[1];

            switch (listTarget.ToLower())
            {
                case ("workshop"):
                    var res = new ListWorkshopStrategy(_logger).Execute();
                    if (res.msg != null)
                    {
                        _logger.LogError($"Encountered problem while performing list operation. \nMsg: {res.msg}");
                        //error logic
                        return 1;
                    }

                    try
                    {
                        PrintInfos(res.infos);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Encountered problem while performing list operation.", e);
                        return 1;
                    }
                    return 0;
                default:
                    _logger.LogError($"Parameter incorrect, '{listTarget.ToLower()}'");
                    return 1;

            }
        }

   
        private void PrintInfos(List<MapInfo> infos)
        {
            const int idColLength = -12;
            const int nameColLength = -50;
            const int descColWidth = -100;
            
            //sort errors
            var errInfos = infos.Where(info => info.ID == "N/A").ToList();
            var goodInfos = infos.Where(info => info.ID != "N/A").ToList();
            //print pretty
            var sb = new StringBuilder();
            //I know this looks like wtf... but its really String.Format notation using interpolation. Thanks resharper :P
            var headerRow = $"|{"ID",idColLength}|{"NAME",nameColLength}|{"DESCRIPTION",descColWidth}|";
            sb.AppendLine(headerRow);
            var spacerRow = $"|{" ",idColLength}|{" ",nameColLength}|{" ",descColWidth}|";
            sb.AppendLine(spacerRow);
            goodInfos.ForEach(info =>
            {
                var workshopItemListRow = $"|{info.ID,idColLength}|{info.Name,nameColLength}|{info.Description,descColWidth}|";
                sb.AppendLine(workshopItemListRow);
            });
            sb.AppendLine(spacerRow);
            Console.WriteLine(sb.ToString());
        }
    }
}