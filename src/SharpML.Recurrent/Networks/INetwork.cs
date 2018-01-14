using System.Collections.Generic;
using SharpML.Recurrent.Models;

namespace SharpML.Recurrent.Networks
{
    public interface INetwork 
    {
        Matrix Activate(Matrix input, Graph g, bool[] dropped = null);
        void ResetState();
        List<Matrix> GetParameters();
        void GenerateDropout(bool training);
        void SaveWeights();
        void RestoreWeights();
    }
}
