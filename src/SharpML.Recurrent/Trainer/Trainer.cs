using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SharpML.Recurrent;
using SharpML.Recurrent.DataStructs;
using SharpML.Recurrent.Loss;
using SharpML.Recurrent.Models;
using SharpML.Recurrent.Networks;
using SharpML.Recurrent.Util;
using Types;

namespace SharpML.Recurrent.Trainer
{
    public class Trainer
    {

        public static double DecayRate = 0.999;
        public static double SmoothEpsilon = 1e-8;
        public static double GradientClipValue = 5;
        public static double Regularization = 0.000001; // L2 regularization strength

        public static double train<T>(int trainingEpochs, double learningRate, INetwork network, DataSet data, int reportEveryNthEpoch, Random rng) where T : INetwork
        {
            return train<T>(trainingEpochs, learningRate, network, data, reportEveryNthEpoch, false, false, null, rng);
        }

        public static double train<T>(int trainingEpochs, double learningRate, INetwork network, DataSet data, int reportEveryNthEpoch, bool initFromSaved, bool overwriteSaved, String savePath, Random rng) where T : INetwork
        {
            Console.WriteLine("--------------------------------------------------------------");
            if (initFromSaved)
            {
                Console.WriteLine("initializing network from saved state...");
                try
                {
                    network = (INetwork)Binary.ReadFromBinary<T>(savePath);
                    data.DisplayReport(network, rng);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Oops. Unable to load from a saved state.");
                    Console.WriteLine("WARNING: " + e.Message);
                    Console.WriteLine("Continuing from freshly initialized network instead.");
                }
            }
            double result = 1.0;
            double bestLoss = double.PositiveInfinity;
            int pokus = 0;
            for (int epoch = 0; epoch < trainingEpochs; epoch++)
            {

                String show = "epoch[" + (epoch + 1) + "/" + trainingEpochs + "]";

                double reportedLossTrain = Pass(learningRate, network, data.Training, true, data.LossTraining, data.LossReporting);
                result = reportedLossTrain;
                if (Double.IsNaN(reportedLossTrain) || Double.IsInfinity(reportedLossTrain))
                {
                    throw new Exception("WARNING: invalid value for training loss. Try lowering learning rate.");
                }
                double reportedLossValidation = 0;
                double reportedLossTesting = 0;
                if (data.Validation != null)
                {
                    reportedLossValidation = Pass(learningRate, network, data.Validation, false, data.LossTraining, data.LossReporting);
                    result = reportedLossValidation;
                }
                if (data.Testing != null)
                {
                    reportedLossTesting = Pass(learningRate, network, data.Testing, false, data.LossTraining, data.LossReporting);
                    result = reportedLossTesting;
                }
                show += "\ttrain = " + String.Format("{0:N5}", reportedLossTrain);
                if (data.Validation != null)
                {
                    show += "\tvalid = " + String.Format("{0:N5}", reportedLossValidation);
                }
                if (data.Testing != null)
                {
                    show += "\ttest  = " + String.Format("{0:N5}", reportedLossTesting);
                }
                show += "\t" + learningRate;
                Console.WriteLine(show);

                if (epoch % reportEveryNthEpoch == reportEveryNthEpoch - 1)
                {
                    data.DisplayReport(network, rng);
                }

                if (overwriteSaved)
                {
                    Binary.WriteToBinary<T>(network, savePath);
                }

                if (reportedLossTrain == 0 && reportedLossValidation == 0)
                {
                    Console.WriteLine("--------------------------------------------------------------");
                    Console.WriteLine("\nDONE.");
                    break;
                }

                // Save best error
                if (reportedLossValidation < bestLoss)
                {
                    bestLoss = reportedLossValidation;
                    pokus = 0;

                    network.SaveWeights();
                }
                else
                {
                    ++pokus;

                    if (pokus == 3)
                    {
                        learningRate /= 2;

                        network.RestoreWeights();
                        pokus = 0;
                    }
                }

                if (learningRate < 0.0000001 || epoch + 1 == trainingEpochs)
                {
                    network.RestoreWeights();
                    break;
                }
            }

            network.GenerateDropout(false);

            Console.WriteLine();

            return result;
        }

        public static double Pass(double learningRate, INetwork network, List<DataSequence> sequences,
            bool applyTraining, ILoss lossTraining, ILoss lossReporting)
        {
            double numerLoss = 0;
            double denomLoss = 0;

            RnnConfig rnnConfig = (RnnConfig)Serializer.Deserialize(NetworkBuilder.Config.RnnConfigFile);

            foreach (DataSequence seq in sequences)
            {
                network.ResetState();
                Graph g = new Graph(applyTraining);
                network.GenerateDropout(applyTraining);

                for (int i = 0; i < seq.Steps.Count; ++i)
                {
                    DataStep step = seq.Steps[i];

                    // Generate in dropout
                    bool[] dropped = new bool[step.Input.W.Length];
                    for (int col = 0; col < dropped.Length; ++col)
                        dropped[col] = Math.Abs(rnnConfig.GetTransformed(0, i, col, step.Input.W[col])) < 0.0000001;

                    Matrix output = network.Activate(step.Input, g, dropped);

                    if (step.TargetOutput != null)
                    {
                        double loss = lossReporting.Measure(output, step.TargetOutput);
                        if (Double.IsNaN(loss) || Double.IsInfinity(loss))
                        {
                            return loss;
                        }
                        numerLoss += loss;
                        denomLoss++;
                        if (applyTraining)
                        {
                            lossTraining.Backward(output, step.TargetOutput);
                        }
                    }

                    if (i % 10 == 0 && applyTraining)
                    {
                        g.Backward(); //backprop dw values
                        UpdateModelParams(network, learningRate); //update params

                        g = new Graph(applyTraining);
                        network.GenerateDropout(applyTraining);
                    }
                }
            }
            return numerLoss / denomLoss;
        }

        //public static Tuple<double, Matrix> PassSingleWithResult(double learningRate, INetwork network, DataStep step, bool applyTraining, ILoss lossTraining, ILoss lossReporting)
        //{
        //    var returnObj = new Tuple<double, Matrix>(0, null);
        //    double numerLoss = 0;
        //    double denomLoss = 0;


        //    network.ResetState();
        //    Graph g = new Graph(applyTraining);
        //    foreach (DataStep step in sequence.Steps)
        //    {
        //        Matrix output = network.Activate(step.Input, g);
        //        if (step.TargetOutput != null)
        //        {
        //            double loss = lossReporting.Measure(output, step.TargetOutput);
        //            if (Double.IsNaN(loss) || Double.IsInfinity(loss))
        //            {
        //                //return loss;
        //                return new Tuple<double, Matrix>(loss, output);
        //            }
        //            numerLoss += loss;
        //            denomLoss++;
        //            if (applyTraining)
        //            {
        //                lossTraining.Backward(output, step.TargetOutput);
        //            }
        //        }
        //    }
        //    List<DataSequence> thisSequence = new List<DataSequence>();
        //    thisSequence.Add(sequence);
        //    if (applyTraining)
        //    {
        //        g.Backward(); //backprop dw values
        //        UpdateModelParams(network, learningRate); //update params
        //    }

        //    return numerLoss / denomLoss;
        //    return new Tuple<double, Matrix>(loss, output);
        //}

        public static void UpdateModelParams(INetwork network, double stepSize)
        {
            foreach (Matrix m in network.GetParameters())
            {
                for (int i = 0; i < m.W.Length; i++)
                {
                    if (!m.Dropped[i])
                    {
                        // rmsprop adaptive learning rate
                        double mdwi = m.Dw[i];
                        m.StepCache[i] = m.StepCache[i] * DecayRate + (1 - DecayRate) * mdwi * mdwi;

                        // gradient clip
                        if (mdwi > GradientClipValue)
                        {
                            mdwi = GradientClipValue;
                        }
                        if (mdwi < -GradientClipValue)
                        {
                            mdwi = -GradientClipValue;
                        }

                        // update (and regularize)
                        m.W[i] += -stepSize * mdwi / Math.Sqrt(m.StepCache[i] + SmoothEpsilon) - Regularization * m.W[i];
                    }
                    m.Dw[i] = 0;
                }
            }
        }
    }
}
