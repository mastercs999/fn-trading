using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter.Helpers
{
    internal static class LocalUtils
    {
        public static int Round(double number, int order)
        {
            return ((int)Math.Round(number / order)) * order;
        }


        public static string[] AllSubstrings(this String str, string startsWith, int length)
        {
            List<string> substrings = new List<string>();

            int index = 0;
            while (true)
            {
                index = str.IndexOf(startsWith, index);
                if (index < 0)
                    break;

                substrings.Add(str.Substring(index, length));

                ++index;
            }

            return substrings.ToArray();
        }

        public static string Capitalize(this String str)
        {
            return char.ToUpper(str[0]) + str.Substring(1);
        }
    }
}
