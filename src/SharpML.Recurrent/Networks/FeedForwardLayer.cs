using System;
using System.Collections.Generic;
using SharpML.Recurrent.Activations;
using SharpML.Recurrent.Models;

namespace SharpML.Recurrent.Networks
{
    [Serializable]
    public class FeedForwardLayer : DropoutLayer, ILayer
    {
        private Matrix _w;
        private Matrix _b;
        readonly INonlinearity _f;

        private Matrix _wB;
        private Matrix _bB;

        public FeedForwardLayer(int inputDimension, int outputDimension, INonlinearity f, double initParamsStdDev, Random rng, double dropout) : base(dropout, inputDimension, outputDimension, rng)
        {
            _w = Matrix.Random(outputDimension, inputDimension, 1 / Math.Sqrt(inputDimension), rng);
            _b = new Matrix(outputDimension);
            this._f = f;

            SaveWeights();
        }

        public Matrix Activate(Matrix input, Graph g)
        {
            _w.Dropped = base.Dropped_OI;
            //_b.Dropped = base.Dropped_O1;
            Matrix sum = g.Add(g.Mul(_w, input), _b);

            //sum.Dropped = base.Dropped_O1;
            Matrix returnObj = g.Nonlin(_f, sum);

            returnObj.Dropped = base.Dropped_O1;
            if (!g.ApplyBackprop)
                ScaleWeightsByDropout(returnObj);
            return returnObj;
        }

        public void ResetState()
        {

        }

        public List<Matrix> GetParameters()
        {
            List<Matrix> result = new List<Matrix>();
            result.Add(_w);
            result.Add(_b);
            return result;
        }

        public void SaveWeights()
        {
            _wB = _w.Clone();
            _bB = _b.Clone();
        }

        public void RestoreWeights()
        {
            _w = _wB.Clone();
            _b = _bB.Clone();
        }
    }
}
