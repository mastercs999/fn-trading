using DataCenter.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Types;

namespace DataCenter.Interface
{
    internal class Exporter
    {
        public static void ExportRnnConfig(RnnConfig rnnConfig, string filename)
        {
            Serializer.Serialize(filename, rnnConfig);
        }

        public static void ExportCSV(DataTable table, bool printHeader, string filename, string actionName)
        {
            string prefix = actionName + "...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // For content
            using (StreamWriter sw = new StreamWriter(filename))
            {
                // Headers
                if (printHeader)
                {
                    IEnumerable<string> columnNames = table.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
                    sw.WriteLine(string.Join(";", columnNames));
                }

                // Rows
                int drawEvery = Utils.PercentIntervalByLength(table.Rows.Count);
                for (int i = 0; i < table.Rows.Count; ++i)
                {
                    DataRow row = table.Rows[i];

                    List<string> fields = new List<string>(row.ItemArray.Length);
                    foreach (object o in row.ItemArray)
                    {
                        Type tt = o.GetType();
                        if (o is double)
                        {
                            if (double.IsNaN((double)o))
                                fields.Add("NaN");
                            else
                                fields.Add(((double)o).ToString());
                        }
                        else
                            fields.Add(o.ToString());
                    }
                    sw.WriteLine(string.Join(";", fields));

                    // Update progress bar
                    if (i % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / table.Rows.Count * 100.0), ConsoleColor.Gray);
                }
            }

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();
        }
        public static void ExportCSV(double[][] data, string filename)
        {
            File.WriteAllLines(filename, data.Select(x => string.Join(";", x)));
        }
    }
}
