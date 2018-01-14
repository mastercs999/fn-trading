using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Types
{
    public class Utils
    {
        public const int PrefixLength = 27;
        public const int ProgressBarLength = 52;

        public static string CreateProgressBar(int length, double percent)
        {
            // Minus place for number of percent
            length -= 7;

            int left = (int)(length * percent / 100);
            int right = length - left;

            string progressBar = "";
            progressBar += "[";
            progressBar += new String('=', left);
            progressBar += new String(' ', right);
            progressBar += "]";
            progressBar += " " + String.Format("{0,3}", Math.Round(percent)) + "%";

            return progressBar;
        }

        public static void DrawMessage(string prefix, string message, ConsoleColor color)
        {
            Console.CursorLeft = 0;
            Console.ForegroundColor = color;

            Console.Write((prefix.PadRight(Utils.PrefixLength) + message).PadRight(Console.WindowWidth - 1));

            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static int PercentIntervalByLength(int length)
        {
            return Math.Max(1, (int)Math.Round(length / 100.0));
        }

        public static double NormalizeValue(double value, double newMean, double newStd, double oldMean, double oldStd)
        {
            return (value - oldMean) * (newStd / oldStd) + newMean;
        }

        public static double ToInterval(double value, double newMin, double newMax, double oldMin, double oldMax)
        {
            if (oldMax - oldMin == 0)
                return (newMax - newMin) / 2;

            return (value - oldMin) * (newMax - newMin) / (oldMax - oldMin) + newMin;
        }

        public static double[][] LoadCsv(string filename)
        {
            return File.ReadAllLines(filename).Select(x => x.Split(new char[] { ';' }).Select(y => double.Parse(y)).ToArray()).ToArray();
        }
    }
}
