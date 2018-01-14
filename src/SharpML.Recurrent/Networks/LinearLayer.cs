using System;
using System.Collections.Generic;
using SharpML.Recurrent.Models;

namespace SharpML.Recurrent.Networks
{
     [Serializable]
    public class LinearLayer : DropoutLayer, ILayer
    {
         readonly Matrix _w;
        //no biases

         public LinearLayer(int inputDimension, int outputDimension, double initParamsStdDev, Random rng, double dropout) : base(dropout, inputDimension, outputDimension, rng)
        {
            _w = Matrix.Random(outputDimension, inputDimension, initParamsStdDev, rng);
        }

        public Matrix Activate(Matrix input, Graph g)
        {
            Matrix returnObj = g.Mul(_w, input);
            return returnObj;
        }

        public void ResetState()
        {

        }

        public List<Matrix> GetParameters()
        {
            List<Matrix> result = new List<Matrix>();
            result.Add(_w);
            return result;
        }

        public void SaveWeights()
        {
            throw new Exception("Not implemented");
        }

        public void RestoreWeights()
        {
            throw new Exception("Not implemented");
        }
    }
}
