using System;
using System.Collections.Generic;
using SharpML.Recurrent.Models;
using System.Linq;

namespace SharpML.Recurrent.Networks
{
    [Serializable]
    public class NeuralNetwork : INetwork
    {
        readonly List<ILayer> _layers;

        private int _inputSize;
        private Random _rng;
        private double _inDropout;
        private bool[] _inDropped;

        public NeuralNetwork(List<ILayer> layers, int inputSize, double inDropout, Random rng)
        {
            this._layers = layers;
            this._inputSize = inputSize;
            this._rng = rng;
            this._inDropout = inDropout;
        }

        public Matrix Activate(Matrix input, Graph g, bool[] dropped = null)
        {
            dropped = null;

            // Input dropout
            Matrix prev = input.Clone();
            if (dropped == null)
            {
                prev.Dropped = _inDropped;
                if (!g.ApplyBackprop)
                    for (int i = 0; i < prev.W.Length; ++i)
                        prev.W[i] *= _inDropout;
            }
            else
            {
                if (g.ApplyBackprop)
                {
                    double stayCount = dropped.Where(x => !x).Count();

                    for (int i = 0; i < dropped.Length; ++i)
                        if (!dropped[i])
                            dropped[i] = _rng.NextDouble() > _inDropout;

                    prev.Dropped = dropped;
                    for (int i = 0; i < prev.W.Length; ++i)
                        prev.W[i] *= prev.W.Length / stayCount;
                }
                else
                {
                    double stayCount = dropped.Where(x => !x).Count();
                    prev.Dropped = dropped;
                    for (int i = 0; i < prev.W.Length; ++i)
                        prev.W[i] *= prev.W.Length / stayCount * _inDropout;
                }
            }

            foreach (ILayer layer in _layers)
            {
                prev = layer.Activate(prev, g);
            }
            return prev;
        }

        public void ResetState()
        {
            foreach (ILayer layer in _layers)
            {
                layer.ResetState();
            }
        }

        public List<Matrix> GetParameters()
        {
            List<Matrix> result = new List<Matrix>();
            foreach (ILayer layer in _layers)
            {
                result.AddRange(layer.GetParameters());
            }
            return result;
        }

        public void GenerateDropout(bool training)
        {
            foreach (ILayer layer in _layers)
                layer.GenerateDropout(training);

            // Generate dropped
            bool[] dropped = new bool[_inputSize];
            if (training)
                for (int i = 0; i < dropped.Length; ++i)
                    dropped[i] = _rng.NextDouble() > _inDropout;
            _inDropped = dropped;
        }

        public void SaveWeights()
        {
            foreach (ILayer layer in _layers)
                layer.SaveWeights();
        }

        public void RestoreWeights()
        {
            foreach (ILayer layer in _layers)
                layer.RestoreWeights();
        }
    }
}
