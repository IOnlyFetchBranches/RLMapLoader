using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RLMapLoader.Components.Helpers.Extensions
{
    public static class VdfParser
    {
        public static string ToJson(string vdfString)
        {
            var sb = new StringBuilder();
            var bracketedVdfString = "{ " + vdfString + " }";


            bool inInner = false;
            var quoteCount = 0;
            var stage = 0;
            //Vdf parsing strat. Works in pairs of quotes O(n)
            foreach (var c in bracketedVdfString)
            {
                sb.Append(c);
                if (c.Equals('"'))
                {
                    quoteCount++;
                }
                
                
                if (quoteCount == 2)
                {
                    stage++;
                    if(c.Equals('{') && stage >1)
                    {
                        //flip from odd to even or viceversa
                        stage--;
                        inInner = true;
                    }

                    if (stage == 1 && !inInner)
                    {
                        sb.Append(':');
                    }
                    else if (stage % 2 == 0)
                    {
                        //on every even set of quotes, also add :
                        sb.Append(':');
                    }
                    else
                    {
                        sb.Append(',');
                    }

                    if (c.Equals('}') && inInner)
                    {
                        //flip from odd to even or viceversa
                        stage++;
                        inInner = false;
                    }

                    quoteCount = 0;
                }

                
            }
            //final cleaning
            var dirtyJson = sb.ToString();
            var cleanJson = dirtyJson.Remove(dirtyJson.LastIndexOf(','), 1)
                .Replace("\\\\","\\").Replace("\\","\\\\");
            return cleanJson;
        }
    }
    public class AcfParser
    {

        public AcfStruct ACFToStruct(string acfText)
        {
            return ACFFileToStruct(acfText);
        }

        private AcfStruct ACFFileToStruct(string RegionToReadIn)
        {
            AcfStruct ACF = new AcfStruct();
            int LengthOfRegion = RegionToReadIn.Length;
            int CurrentPos = 0;
            while (LengthOfRegion > CurrentPos)
            {
                int FirstItemStart = RegionToReadIn.IndexOf('"', CurrentPos);
                if (FirstItemStart == -1)
                    break;
                int FirstItemEnd = RegionToReadIn.IndexOf('"', FirstItemStart + 1);
                CurrentPos = FirstItemEnd + 1;
                string FirstItem = RegionToReadIn.Substring(FirstItemStart + 1, FirstItemEnd - FirstItemStart - 1);

                int SecondItemStartQuote = RegionToReadIn.IndexOf('"', CurrentPos);
                int SecondItemStartBraceleft = RegionToReadIn.IndexOf('{', CurrentPos);
                if (SecondItemStartBraceleft == -1 || SecondItemStartQuote < SecondItemStartBraceleft)
                {
                    int SecondItemEndQuote = RegionToReadIn.IndexOf('"', SecondItemStartQuote + 1);
                    string SecondItem = RegionToReadIn.Substring(SecondItemStartQuote + 1, SecondItemEndQuote - SecondItemStartQuote - 1);
                    CurrentPos = SecondItemEndQuote + 1;
                    ACF.SubItems.Add(FirstItem, SecondItem);
                }
                else
                {
                    int SecondItemEndBraceright = RegionToReadIn.NextEndOf('{', '}', SecondItemStartBraceleft + 1);
                    AcfStruct ACFS = ACFFileToStruct(RegionToReadIn.Substring(SecondItemStartBraceleft + 1, SecondItemEndBraceright - SecondItemStartBraceleft - 1));
                    CurrentPos = SecondItemEndBraceright + 1;
                    ACF.SubACF.Add(FirstItem, ACFS);
                }
            }

            return ACF;
        }

    }

    public class AcfStruct
    {
        public Dictionary<string, AcfStruct> SubACF { get; private set; }
        public Dictionary<string, string> SubItems { get; private set; }

        public AcfStruct()
        {
            SubACF = new Dictionary<string, AcfStruct>();
            SubItems = new Dictionary<string, string>();
        }

        public override string ToString()
        {
            return ToString(0);
        }

        private string ToString(int Depth)
        {
            StringBuilder SB = new StringBuilder();
            foreach (KeyValuePair<string, string> item in SubItems)
            {
                SB.Append('\t', Depth);
                SB.AppendFormat("\"{0}\"\t\t\"{1}\"\r\n", item.Key, item.Value);
            }
            foreach (KeyValuePair<string, AcfStruct> item in SubACF)
            {
                SB.Append('\t', Depth);
                SB.AppendFormat("\"{0}\"\n", item.Key);
                SB.Append('\t', Depth);
                SB.AppendLine("{");
                SB.Append(item.Value.ToString(Depth + 1));
                SB.Append('\t', Depth);
                SB.AppendLine("}");
            }
            return SB.ToString();
        }
    }

    static class Extension
    {
        public static int NextEndOf(this string str, char Open, char Close, int startIndex)
        {
            if (Open == Close)
                throw new Exception("\"Open\" and \"Close\" char are equivalent!");

            int OpenItem = 0;
            int CloseItem = 0;
            for (int i = startIndex; i < str.Length; i++)
            {
                if (str[i] == Open)
                {
                    OpenItem++;
                }
                if (str[i] == Close)
                {
                    CloseItem++;
                    if (CloseItem > OpenItem)
                        return i;
                }
            }
            throw new Exception("Not enough closing characters!");
        }
    }
}