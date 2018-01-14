using System.Collections.Generic;
using SharpML.Recurrent.Models;
using System;

namespace SharpML.Recurrent.Networks
{
    public interface ILayer 
    {
        Matrix Activate(Matrix input, Graph g);
        void ResetState();
        List<Matrix> GetParameters();
        void GenerateDropout(bool training);
        void SaveWeights();
        void RestoreWeights();
    }
}
