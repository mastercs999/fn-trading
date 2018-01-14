using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpML.Recurrent.Activations;
using SharpML.Recurrent.Networks;
using Types;

//Copyright (c) <year> <copyright holders>
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace SharpML.Recurrent.Util
{
    public static class NetworkBuilder
    {
        public static Config Config { get; set; }

        public static NeuralNetwork MakeLstm(int inputDimension, int hiddenDimension, int hiddenLayers, int outputDimension, INonlinearity decoderUnit, double initParamsStdDev, Random rng, double dropout, double inDropout, Config config)
        {
            Config = config;

            List<ILayer> layers = new List<ILayer>();
            for (int h = 0; h < hiddenLayers; h++)
            {
                if (h == 0)
                {
                    layers.Add(new LstmLayer(inputDimension, hiddenDimension, initParamsStdDev, rng, dropout));
                }
                else
                {
                    layers.Add(new LstmLayer(hiddenDimension, hiddenDimension, initParamsStdDev, rng, dropout));
                }
            }
            layers.Add(new FeedForwardLayer(hiddenDimension, outputDimension, decoderUnit, initParamsStdDev, rng, 1));
            return new NeuralNetwork(layers, inputDimension, inDropout, rng);
        }

        public static NeuralNetwork MakeLstmWithInputBottleneck(int inputDimension, int bottleneckDimension, int hiddenDimension, int hiddenLayers, int outputDimension, INonlinearity decoderUnit, double initParamsStdDev, Random rng, double dropout, double inDropout)
        {
            List<ILayer> layers = new List<ILayer>();
            layers.Add(new LinearLayer(inputDimension, bottleneckDimension, initParamsStdDev, rng, dropout));
            for (int h = 0; h < hiddenLayers; h++)
            {
                if (h == 0)
                {
                    layers.Add(new LstmLayer(bottleneckDimension, hiddenDimension, initParamsStdDev, rng, dropout));
                }
                else
                {
                    layers.Add(new LstmLayer(hiddenDimension, hiddenDimension, initParamsStdDev, rng, dropout));
                }
            }
            layers.Add(new FeedForwardLayer(hiddenDimension, outputDimension, decoderUnit, initParamsStdDev, rng, 1));
            return new NeuralNetwork(layers, inputDimension, inDropout, rng);
        }

        public static NeuralNetwork MakeFeedForward(int inputDimension, int hiddenDimension, int hiddenLayers, int outputDimension, INonlinearity hiddenUnit, INonlinearity decoderUnit, double initParamsStdDev, Random rng, double dropout, double inDropout, Config config)
        {
            Config = config;

            List<ILayer> layers = new List<ILayer>();
            for (int h = 0; h < hiddenLayers; h++)
            {
                if (h == 0)
                {
                    layers.Add(new FeedForwardLayer(inputDimension, hiddenDimension, hiddenUnit, initParamsStdDev, rng, dropout));
                }
                else
                {
                    layers.Add(new FeedForwardLayer(hiddenDimension, hiddenDimension, hiddenUnit, initParamsStdDev, rng, dropout));
                }
            }
            layers.Add(new FeedForwardLayer(hiddenDimension, outputDimension, decoderUnit, initParamsStdDev, rng, 1));
            return new NeuralNetwork (layers, inputDimension, inDropout, rng);
        }

        public static NeuralNetwork MakeGru(int inputDimension, int hiddenDimension, int hiddenLayers, int outputDimension, INonlinearity decoderUnit, double initParamsStdDev, Random rng, double dropout, double inDropout)
        {
            List<ILayer> layers = new List<ILayer>();
            for (int h = 0; h < hiddenLayers; h++)
            {
                if (h == 0)
                {
                    layers.Add(new GruLayer(inputDimension, hiddenDimension, initParamsStdDev, rng, dropout));
                }
                else
                {
                    layers.Add(new GruLayer(hiddenDimension, hiddenDimension, initParamsStdDev, rng, dropout));
                }
            }
            layers.Add(new FeedForwardLayer(hiddenDimension, outputDimension, decoderUnit, initParamsStdDev, rng, 1));
            return new NeuralNetwork(layers, inputDimension, inDropout, rng);
        }

        public static NeuralNetwork MakeRnn(int inputDimension, int hiddenDimension, int hiddenLayers, int outputDimension, INonlinearity hiddenUnit, INonlinearity decoderUnit, double initParamsStdDev, Random rng, double dropout, double inDropout)
        {
            List<ILayer> layers = new List<ILayer>();
            for (int h = 0; h < hiddenLayers; h++)
            {
                if (h == 0)
                {
                    layers.Add(new RnnLayer(inputDimension, hiddenDimension, hiddenUnit, initParamsStdDev, rng, dropout));
                }
                else
                {
                    layers.Add(new RnnLayer(hiddenDimension, hiddenDimension, hiddenUnit, initParamsStdDev, rng, dropout));
                }
            }
            layers.Add(new FeedForwardLayer(hiddenDimension, outputDimension, decoderUnit, initParamsStdDev, rng, 1));
            return new NeuralNetwork(layers, inputDimension, inDropout, rng);
        }
    }
}
