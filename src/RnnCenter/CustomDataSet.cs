using SharpML.Recurrent.DataStructs;
using SharpML.Recurrent.Loss;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Types;

namespace RnnCenter
{
    internal class CustomDataSet : DataSet
    {
        public CustomDataSet(Config config)
        {
            Training = CreateSequences(config.RnnTrainXFile, config.RnnTrainYFile);
            Validation = CreateSequences(config.RnnValidXFile, config.RnnValidYFile);
            Testing = CreateSequences(config.RnnTestXFile, config.RnnTestYFile);
            InputDimension = Training[0].Steps[0].Input.Rows;
            OutputDimension = Training[0].Steps[0].TargetOutput.Rows;
            LossTraining = new LossSumOfSquares();
            LossReporting = new LossSumOfSquares();
        }

        private List<DataSequence> CreateSequences(string xFilename, string yFilename)
        {
            double[][] x = LoadCsv(xFilename);
            double[][] y = LoadCsv(yFilename);

            // Create sequences
            List<DataSequence> sequences = new List<DataSequence>();
            DataSequence ds = new DataSequence();
            sequences.Add(ds);
            ds.Steps = new List<DataStep>();

            for (int i = 0; i < x.Length; ++i)
                ds.Steps.Add(new DataStep(x[i], y[i]));

            return sequences; 
        }

        private double[][] LoadCsv(string filename)
        {
            return File.ReadAllLines(filename).Select(x => x.Split(new char[] { ';' }).Select(y => double.Parse(y)).ToArray()).ToArray();
        }
    }
}
