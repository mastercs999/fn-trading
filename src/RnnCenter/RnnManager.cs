using SharpML.Recurrent.Activations;
using SharpML.Recurrent.DataStructs;
using SharpML.Recurrent.Models;
using SharpML.Recurrent.Networks;
using SharpML.Recurrent.Trainer;
using SharpML.Recurrent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Types;

namespace RnnCenter
{
    public class RnnManager
    {
        public static Config Config { get; set; }

        public RnnManager(Config config)
        {
            Config = config;
        }

        public void Predict()
        {
            if (!Config.Reload &&
                File.Exists(Config.RnnPredictedXFile) &&
                File.Exists(Config.RnnPredictedYFile))
                return;

            Random rng = new Random(Config.Random.Next());

            CustomDataSet data = new CustomDataSet(Config);

            RnnConfig rnnConfig = (RnnConfig)Serializer.Deserialize(Config.RnnConfigFile);

            int inputDimension = data.Training[0].Steps[0].Input.Rows;
            int hiddenDimension = 30;
            int outputDimension = data.Training[0].Steps[0].TargetOutput.Rows;
            int hiddenLayers = 1;
            double learningRate = 0.01;
            double initParamsStdDev = 0.08;
            double dropout = 0.5;
            double inDropout = 0.8;

            INetwork nn = NetworkBuilder.MakeLstm(inputDimension,
                hiddenDimension,
                hiddenLayers,
                outputDimension,
                new LinearUnit(),
                initParamsStdDev, rng, dropout, inDropout, Config);
            //nn = NetworkBuilder.MakeFeedForward(inputDimension,
            //    hiddenDimension,
            //    hiddenLayers,
            //    outputDimension,
            //    new SigmoidUnit(),
            //    new LinearUnit(),
            //    initParamsStdDev, rng, dropout, inDropout, Config);

            int reportEveryNthEpoch = 10;
            int trainingEpochs = 100;

            Trainer.train<NeuralNetwork>(trainingEpochs, learningRate, nn, data, reportEveryNthEpoch, rng);

            StreamWriter predictedXFile = new StreamWriter(Config.RnnPredictedXFile);
            StreamWriter predictedYFile = new StreamWriter(Config.RnnPredictedYFile);
            for (int i = 0; i < data.Testing.First().Steps.Count; ++i)
            {
                DataStep ds = data.Testing.First().Steps[i];

                Graph g = new Graph(false);

                // Generate in dropout
                bool[] dropped = new bool[ds.Input.W.Length];
                for (int col = 0; col < dropped.Length; ++col)
                    dropped[col] = Math.Abs(rnnConfig.GetTransformed(0, i, col, ds.Input.W[col])) < 0.0000001;

                Matrix input = new Matrix(ds.Input.W);
                Matrix output = nn.Activate(input, g, dropped);

                // Write into file
                string line1 = "";
                string line2 = "";
                foreach (double d in output.W)
                    line1 += d + ";";
                foreach (double d in ds.TargetOutput.W)
                    line2 += d + ";";

                predictedXFile.WriteLine(line1.Substring(0, line1.Length - 1));
                predictedYFile.WriteLine(line2.Substring(0, line2.Length - 1));
            }
            predictedXFile.Close();
            predictedYFile.Close();
        }
    }
}
