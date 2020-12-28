using System;
using System.Collections.Generic;
using System.Linq;
using RLMapLoader.Components.Logging;

namespace RLMapLoader.Components.Helpers.Extensions
{
    public static class DictExtensions
    {
        /// <summary>
        /// Checks dictionary fields, can take in logger and will auto write failing fields.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static Dictionary<string, string> WithNullCheck(this Dictionary<string,string> dict, Logger logger = null)
        {
            var delimiter = ",";
            try
            {
                var failingKeys = dict.Where(pair => pair.Value == null).Select(pair => pair.Key).ToList(); //Find every element that has null value then project into list of keys
                if(failingKeys.Any()) 
                    logger?.LogWarning( $"The following items are null in the dictionary: {failingKeys.Aggregate((i,j) => i + delimiter + j)}" );
            }
            catch (Exception e)
            {
                logger?.LogError($"Failed to null check.",e);
                return dict; //Still return dictionary.
            }

            return dict;

        }
    }
}