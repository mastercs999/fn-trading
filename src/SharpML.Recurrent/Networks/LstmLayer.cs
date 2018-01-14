using System;
using System.Collections.Generic;
using SharpML.Recurrent.Activations;
using SharpML.Recurrent.Models;

namespace SharpML.Recurrent.Networks
{
    [Serializable]
    public class LstmLayer : DropoutLayer, ILayer
    {
        int _inputDimension;
        readonly int _outputDimension;

        private Matrix _wix;
        private Matrix _wih;
        private Matrix _inputBias;
        private Matrix _wfx;
        private Matrix _wfh;
        private Matrix _forgetBias;
        private Matrix _wox;
        private Matrix _woh;
        private Matrix _outputBias;
        private Matrix _wcx;
        private Matrix _wch;
        private Matrix _cellWriteBias;

        private Matrix _hiddenContext;
        private Matrix _cellContext;



        private Matrix _wixB;
        private Matrix _wihB;
        private Matrix _inputBiasB;
        private Matrix _wfxB;
        private Matrix _wfhB;
        private Matrix _forgetBiasB;
        private Matrix _woxB;
        private Matrix _wohB;
        private Matrix _outputBiasB;
        private Matrix _wcxB;
        private Matrix _wchB;
        private Matrix _cellWriteBiasB;

        readonly INonlinearity _inputGateActivation = new SigmoidUnit();
        readonly INonlinearity _forgetGateActivation = new SigmoidUnit();
        readonly INonlinearity _outputGateActivation = new SigmoidUnit();
        readonly INonlinearity _cellInputActivation = new TanhUnit();
        readonly INonlinearity _cellOutputActivation = new TanhUnit();

        public LstmLayer(int inputDimension, int outputDimension, double initParamsStdDev, Random rng, double dropout) : base(dropout, inputDimension, outputDimension, rng)
        {
            this._inputDimension = inputDimension;
            this._outputDimension = outputDimension;
            _wix = Matrix.Random(outputDimension, inputDimension, 1 / Math.Sqrt(inputDimension), rng);
            _wih = Matrix.Random(outputDimension, outputDimension, 1 / Math.Sqrt(outputDimension), rng);
            _inputBias = new Matrix(outputDimension);
            _wfx = Matrix.Random(outputDimension, inputDimension, 1 / Math.Sqrt(inputDimension), rng);
            _wfh = Matrix.Random(outputDimension, outputDimension, 1 / Math.Sqrt(outputDimension), rng);
            //set forget bias to 1.0, as described here: http://jmlr.org/proceedings/papers/v37/jozefowicz15.pdf
            _forgetBias = Matrix.Ones(outputDimension, 1);
            _wox = Matrix.Random(outputDimension, inputDimension, 1 / Math.Sqrt(inputDimension), rng);
            _woh = Matrix.Random(outputDimension, outputDimension, 1 / Math.Sqrt(outputDimension), rng);
            _outputBias = new Matrix(outputDimension);
            _wcx = Matrix.Random(outputDimension, inputDimension, 1 / Math.Sqrt(inputDimension), rng);
            _wch = Matrix.Random(outputDimension, outputDimension, 1 / Math.Sqrt(outputDimension), rng);
            _cellWriteBias = new Matrix(outputDimension);

            SaveWeights();
        }

        public Matrix Activate(Matrix input, Graph g)
        {

            //input gate
            _wix.Dropped = base.Dropped_OI;
            //_wih.Dropped = base.Dropped_OO;
            Matrix sum0 = g.Mul(_wix, input);
            Matrix sum1 = g.Mul(_wih, _hiddenContext);
            //_inputBias.Dropped = base.Dropped_O1;
            Matrix inputGate = g.Nonlin(_inputGateActivation, g.Add(g.Add(sum0, sum1), _inputBias));

            //forget gate
            _wfx.Dropped = base.Dropped_OI;
            //_wfh.Dropped = base.Dropped_OO;
            Matrix sum2 = g.Mul(_wfx, input);
            Matrix sum3 = g.Mul(_wfh, _hiddenContext);
            //_forgetBias.Dropped = base.Dropped_O1;
            Matrix forgetGate = g.Nonlin(_forgetGateActivation, g.Add(g.Add(sum2, sum3), _forgetBias));

            //output gate
            _wox.Dropped = base.Dropped_OI;
            //_woh.Dropped = base.Dropped_OO;
            Matrix sum4 = g.Mul(_wox, input);
            Matrix sum5 = g.Mul(_woh, _hiddenContext);
            //_outputBias.Dropped = base.Dropped_O1;
            Matrix outputGate = g.Nonlin(_outputGateActivation, g.Add(g.Add(sum4, sum5), _outputBias));

            //write operation on cells
            _wcx.Dropped = base.Dropped_OI;
            //_wch.Dropped = base.Dropped_OO;
            Matrix sum6 = g.Mul(_wcx, input);
            Matrix sum7 = g.Mul(_wch, _hiddenContext);
            //_cellWriteBias.Dropped = base.Dropped_O1;
            Matrix cellInput = g.Nonlin(_cellInputActivation, g.Add(g.Add(sum6, sum7), _cellWriteBias));

            //compute new cell activation
            //forgetGate.Dropped = base.Dropped_O1;
            //inputGate.Dropped = base.Dropped_O1;
            Matrix retainCell = g.Elmul(forgetGate, _cellContext);
            Matrix writeCell = g.Elmul(inputGate, cellInput);
            //retainCell.Dropped = base.Dropped_O1;
            //writeCell.Dropped = base.Dropped_O1;
            Matrix cellAct = g.Add(retainCell, writeCell);

            //compute hidden state as gated, saturated cell activations
            //cellAct.Dropped = base.Dropped_O1;
            //outputGate.Dropped = base.Dropped_O1;
            Matrix output = g.Elmul(outputGate, g.Nonlin(_cellOutputActivation, cellAct));

            //rollover activations for next iteration
            //output.Dropped = base.Dropped_O1;
            //cellAct.Dropped = base.Dropped_O1;
            _hiddenContext = output;
            _cellContext = cellAct;

            output = output.Clone();
            if (!g.ApplyBackprop)
                ScaleWeightsByDropout(output);
            return output;
        }

        public void ResetState()
        {
            _hiddenContext = new Matrix(_outputDimension);
            _cellContext = new Matrix(_outputDimension);
        }

        public List<Matrix> GetParameters()
        {
            List<Matrix> result = new List<Matrix>();
            result.Add(_wix);
            result.Add(_wih);
            result.Add(_inputBias);
            result.Add(_wfx);
            result.Add(_wfh);
            result.Add(_forgetBias);
            result.Add(_wox);
            result.Add(_woh);
            result.Add(_outputBias);
            result.Add(_wcx);
            result.Add(_wch);
            result.Add(_cellWriteBias);
            return result;
        }

        public void SaveWeights()
        {
            _wixB = _wix.Clone();
            _wihB = _wih.Clone();
            _inputBiasB = _inputBias.Clone();
            _wfxB = _wfx.Clone();
            _wfhB = _wfh.Clone();
            _forgetBiasB = _forgetBias.Clone();
            _woxB = _wox.Clone();
            _wohB = _woh.Clone();
            _outputBiasB = _outputBias.Clone();
            _wcxB = _wcx.Clone();
            _wchB = _wch.Clone();
            _cellWriteBiasB = _cellWriteBias.Clone();
        }

        public void RestoreWeights()
        {
            _wix = _wixB.Clone();
            _wih = _wihB.Clone();
            _inputBias = _inputBiasB.Clone();
            _wfx = _wfxB.Clone();
            _wfh = _wfhB.Clone();
            _forgetBias = _forgetBiasB.Clone();
            _wox = _woxB.Clone();
            _woh = _wohB.Clone();
            _outputBias = _outputBiasB.Clone();
            _wcx = _wcxB.Clone();
            _wch = _wchB.Clone();
            _cellWriteBias = _cellWriteBiasB.Clone();
        }
    }
}
