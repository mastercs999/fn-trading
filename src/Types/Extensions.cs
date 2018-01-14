using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Types
{
    public static class Extensions
    {
        public static string[] Split(this string str, string splitter)
        {
            // Split string by string
            str = str.Replace(splitter, "_|_");
            return str.Split(new[] { "_|_" }, StringSplitOptions.None);
        }

        public static string Remove(this string str, params string[] toRemove)
        {
            string result = (string)str.Clone();

            foreach (string tr in toRemove)
                result = result.Replace(tr, "").Trim();

            return result;
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        public static string RemoveIllegalChars(this string str)
        {
            string result = (string)str.Clone();

            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalid)
                result = result.Replace(c.ToString(), "");

            return result;
        }

        public static IEnumerable<T> GetLastN<T>(this IEnumerable<T> list, int lastN)
        {
            return list.Reverse().Take(lastN);
        }
    }
}
