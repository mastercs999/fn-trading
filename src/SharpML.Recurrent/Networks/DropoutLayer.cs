using SharpML.Recurrent.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpML.Recurrent.Networks
{
    public class DropoutLayer
    {
        private double _dropout;
        private Random _rng;
        private int _inputSize;
        private int _outputSize;

        protected bool[] Dropped_OI { get; set; }
        protected bool[] Dropped_OO { get; set; }
        protected bool[] Dropped_O1 { get; set; }

        public DropoutLayer(double dropout, int inputSize, int outputSize, Random rng)
        {
            _dropout = dropout;
            _rng = rng;
            _inputSize = inputSize;
            _outputSize = outputSize;
        }

        public void GenerateDropout(bool training)
        {
            // Generate dropped
            bool[] dropped = new bool[_outputSize];
            if (training)
                for (int i = 0; i < dropped.Length; ++i)
                    dropped[i] = _rng.NextDouble() > _dropout;

            // Generate dropped array
            Matrix oi = new Matrix(_outputSize, _inputSize);
            Matrix oo = new Matrix(_outputSize, _outputSize);
            Matrix o1 = new Matrix(_outputSize, 1);
            for (int i = 0; i < dropped.Length; ++i)
                if (dropped[i])
                {
                    for (int col = 0; col < oi.Cols; ++col)
                        oi.SetDropped(i, col, true);
                    for (int col = 0; col < oo.Cols; ++col)
                        oo.SetDropped(i, col, true);
                    for (int col = 0; col < o1.Cols; ++col)
                        o1.SetDropped(i, col, true);
                }

            // Save dropped arrays
            Dropped_OI = oi.Dropped;
            Dropped_OO = oo.Dropped;
            Dropped_O1 = o1.Dropped;
        }

        public void ScaleWeightsByDropout(Matrix m)
        {
            for (int i = 0; i < m.W.Length; ++i)
                m.W[i] *= _dropout;
        }
    }
}
