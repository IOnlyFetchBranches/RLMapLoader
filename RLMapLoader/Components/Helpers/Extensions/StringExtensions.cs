using System.Text;

namespace RLMapLoader.Components.Helpers.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="vdfString"></param>
        /// <returns></returns>
        public static string VdfToJson(this string vdfString)
        {
            return VdfParser.ToJson(vdfString);
        }

        /// <summary>
        /// thanks @silent3241 https://stackoverflow.com/questions/39065573/reading-values-from-an-acf-manifest-file
        /// </summary>
        /// <param name="acfString"></param>
        /// <returns></returns>
        public static AcfStruct AcfToStruct(this string acfString)
        {
            return new AcfParser().ACFToStruct(acfString);
        }
    }
}