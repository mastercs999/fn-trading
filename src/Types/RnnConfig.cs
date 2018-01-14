using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Types
{
    [Serializable]
    public class RnnConfig
    {
        public double[][,] PcaCoefficients { get; set; }

        private Dictionary<int, Stat[,]> OldStats;
        private Dictionary<int, int> Windows;

        private Dictionary<int, double> ClippedValues;

        public RnnConfig()
        {
            OldStats = new Dictionary<int, Stat[,]>();
            Windows = new Dictionary<int, int>();

            PcaCoefficients = new double[2][,];

            ClippedValues = new Dictionary<int, double>();
        }

        public void BeginStat(int order, int rows, int columns, int window)
        {
            OldStats.Add(order, new Stat[rows / window + 1, columns]);
            Windows.Add(order, window);
        }

        public void SetStat(int order, int row, int column, Stat stat)
        {
            OldStats[order][row / Windows[order], column] = stat;
        }

        public Stat GetStat(int order, int row, int column)
        {
            return OldStats[order][row / Windows[order], column];
        }

        public double GetTransformed(int order, int row, int column, double value)
        {
            Stat st = GetStat(order, row, column);
            return Utils.NormalizeValue(value, st.Mean, st.Deviance, 0, 1);
        }

        public void SetClippedValue(int row, int col, double value)
        {
            ClippedValues[row * 100000 + col] = value;
        }
        public double? GetClippedValue(int row, int col)
        {
            if (ClippedValues.ContainsKey(row * 100000 + col))
                return ClippedValues[row * 100000 + col];
            else
                return null;
        }
    }

    [Serializable]
    public class Stat
    {
        public double Mean { get; set; }
        public double Deviance { get; set; }
    }
}
