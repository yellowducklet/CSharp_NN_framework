using System;
using System.Text;

namespace cs_nn_fm
{
    public class nn
    {
        private int _numInput;
        private int _numHidden;
        private int _numOutput;
        private double[] _inputs; // input nodes
        private double[] _hiddens;
        private double[] _outputs;
        private double[,] ih_weights; // Input-hidden weights
        private double[] _hBiases; // Input-hidden weights
        private double[,] _hoWeights; // hidden-output weights
        private double[] _oBiases;
        private Random _rnd;

        public nn(int numInput, int numHidden, int numOutput, int rndSeed)
        {
            _numInput = numInput;
            _numHidden = numHidden;
            _numOutput = numOutput;
            // init predictor
            _inputs = new double[numInput];
            _hiddens = new double[numHidden];
            _outputs = new double[numOutput];
            // init weights
            ih_weights = Helper.MakeMatrix(numInput, numHidden, 0.0);
            _hoWeights = Helper.MakeMatrix(numHidden, numOutput, 0.0);
            _hBiases = new double[numHidden];
            _oBiases = new double[numOutput];

            _rnd = new Random(rndSeed); // init rnd
            InitializeWeights(); // all weights and biases
        }

        // helper methods
        public double[] InitializeWeights()
        {
            var numOfWeights = (_numInput * _numHidden) + _numHidden + (_numHidden * _numOutput) + _numOutput;
            var initialWeights = new double[numOfWeights];
            var lo = -0.001;
            var hi = 0.001;
            for (var i = 0; i < initialWeights.Length; i++)
            {
                initialWeights[i] = lo + (hi - lo) * _rnd.NextDouble(); // set weihts -0.001-0.001(lo-hi)
            }

            SetWeight(initialWeights);
            return initialWeights;
        }

        public void SetWeight(double[] weights)
        {
            // copy serialized weights and biases into separate weights[] array
            var numOfWeights = (_numInput * _numHidden) + _numHidden + (_numHidden * _numOutput) + _numOutput;
            if (numOfWeights != weights.Length)
            {
                throw new Exception("Bad weights array in SetWeights");
            }

            var w = 0;
            for (int i = 0; i < _numInput; ++i)
            for (int j = 0; j < _numHidden; ++j)
                ih_weights[i, j] = weights[w++];

            for (int j = 0; j < _numHidden; ++j)
                _hBiases[j] = weights[w++];

            for (int j = 0; j < _numHidden; ++j)
            for (int k = 0; k < _numOutput; ++k)
                _hoWeights[j, k] = weights[w++];

            for (int k = 0; k < _numOutput; ++k)
                _oBiases[k] = weights[w++]; // 
        }

        public double[] GetWeights()
        {
            var numOfWeights = (_numInput * _numHidden) + _numHidden + (_numHidden * _numOutput) + _numOutput;
            var res = new double[numOfWeights];
            //i-h weighs + h biases + h-o weights + o biases (order)
            var w = 0;
            for (int i = 0; i < _numInput; ++i)
            for (int j = 0; j < _numHidden; ++j)
                res[w++] = ih_weights[i, j];

            for (int j = 0; j < _numHidden; ++j)
                res[w++] = _hBiases[j];

            for (int j = 0; j < _numHidden; ++j)
            for (int k = 0; k < _numOutput; ++k)
                res[w++] = _hoWeights[j, k];

            for (int k = 0; k < _numOutput; ++k)
                res[w++] = _oBiases[k];
            return res; // tmp
        }

        public double[] Forward(double[] x_values, bool debug = false)
        {
            if (debug)
            {
                Console.WriteLine("input:" + x_values[0]);
            }

            // preliminary values
            var hSums = new double[_numHidden]; // scratch array
            var oSums = new double[_numOutput];

            for (int i = 0; i < _numInput; i++)
            {
                _inputs[i] = x_values[i]; // copy independent vars into input nodes
            }

            for (int j = 0; j < _numHidden; j++)
            for (int i = 0; i < _numInput; i++)
            {
                hSums[j] += _inputs[i] * ih_weights[i, j]; // full-connect network
            }


            // add the bias
            for (int j = 0; j < _numHidden; j++)
                hSums[j] += _hBiases[j];
            if (debug)
            {
                Console.WriteLine("hSum");
                Helper.ShowVector(hSums, 8, 12, true);
            }

            //activation
            for (int j = 0; j < _numHidden; j++)
            {
                _hiddens[j] = Activation.HyperTan(hSums[j]);
            }

            if (debug)
            {
                Console.WriteLine("after activation hsum");
                Helper.ShowVector(hSums, 8, 12, true);
            }


            for (int k = 0; k < _numOutput; k++)
            for (int j = 0; j < _numHidden; j++)
            {
                oSums[k] += _hiddens[j] * _hoWeights[j, k];
            }

            for (int k = 0; k < _numOutput; k++)
            {
                oSums[k] += _oBiases[k];
            }

            if (debug)
            {
                Console.WriteLine("after add biases oSum");
                Helper.ShowVector(oSums, 8, 12, true);
            }


            // no softmax activation in regression applied. Just copy
            Array.Copy(oSums, _outputs, _outputs.Length);
            double[] resRes = new double[_numOutput];
            Array.Copy(_outputs, resRes, resRes.Length); // copy res_res to output[]
            if (debug)
            {
                Console.WriteLine("output:" + _outputs[0]);
            }

            return resRes;
        }

        public double[] Train(Dataset data_set, int max_epochs, double lr, double momentum, bool shuffle_flag,
            ref double[] weights, bool debug = false)
        {
            Console.WriteLine("stochastic back propogation training start:");
            // back-prop specific arrays
            var hoGrads = Helper.MakeMatrix(_numHidden, _numOutput, 0.0); // hidden-output grad
            var obGrads = new double[_numOutput]; // output bias grad

            var ihGrad = Helper.MakeMatrix(_numInput, _numHidden, 0.0); // input-hidden grad
            var hbGrad = new double[_numHidden]; // hidden-bias grad

            // signal
            var oSignals = new double[_numOutput]; // signals == gradients w/o associated input terms
            var hSignals = new double[_numHidden];

            //backprop-momentum specific array
            var ihPrevWeightsDelta = Helper.MakeMatrix(_numInput, _numHidden, 0.0);
            var hPrevBiasesDelta = new double[_numHidden];
            var hoPrevWeightsDelta = Helper.MakeMatrix(_numHidden, _numOutput, 0.0);
            var oPrevBiasesDelta = new double[_numOutput];
            // train NN using lr and momentum
            var epoch = 0;
            var xValues = new double[_numInput]; // input vals
            var tValues = new double[_numOutput]; // target vals

            var sequence = new int[data_set.GetLen()];
            for (int i = 0; i < sequence.Length; i++)
            {
                sequence[i] = i;
            }

            var errInterval = 1 + max_epochs / 40; // interval to check validation data
            var trainData = data_set.DataSet;
            while (epoch < max_epochs)
            {
                // every epoch
                epoch++;
                if (epoch % errInterval == 0 && epoch < max_epochs)
                {
                    // check err
                    var trainErr = Error(trainData);
                    Console.WriteLine("epoch= " + epoch + " acc = " + (1 - trainErr).ToString("F4"));
                }

                if (shuffle_flag)
                {
                    Shuffle(sequence); // shuffle the order
                }

                for (int ii = 0; ii < trainData.Length; ii++)
                {
                    int idx = sequence[ii];
                    Array.Copy(trainData[idx], xValues, _numInput);
                    Array.Copy(trainData[idx], _numInput, tValues, 0, _numOutput);
                    Forward(xValues,debug); // res_outupt has been copied to output[]

                    // i=inputs j=hidden(s) k=outputs
                    // 1. compute output nodes signals
                    for (int k = 0; k < _numOutput; k++)
                    {
                        var derivatives = 1.0; // dummy
                        oSignals[k] = (_outputs[k] - tValues[k]) * derivatives;
                    }

                    if (debug)
                    {
                        Console.WriteLine("oSignals:");
                        Helper.ShowVector(oSignals);
                    }

                    // 2. compute h-to-o weights gradients using output signals
                    for (int j = 0; j < _numHidden; j++)
                    {
                        for (int k = 0; k < _numOutput; k++)
                        {
                            hoGrads[j, k] = oSignals[k] * _hiddens[j];
                        }
                    }

                    if (debug)
                    {
                        Console.WriteLine("hoGrads");
                        Helper.ShowMatrix(hoGrads);
                    }

                    // 2'. compute the output biases grads using output signals
                    for (int k = 0; k < _numOutput; k++)
                    {
                        obGrads[k] = oSignals[k] * 1.0; // dummy
                    }

                    if (debug)
                    {
                        Console.WriteLine("obgrads");
                        Helper.ShowVector(obGrads);
                    }

                    // 3.compute hidden nodes signals
                    for (int j = 0; j < _numHidden; j++)
                    {
                        var sum = 0.0;
                        for (int k = 0; k < _numOutput; k++)
                        {
                            sum += oSignals[k] * _hoWeights[j, k];
                        }

                        var derivatives = (1 + _hiddens[j]) * (1 - _hiddens[j]); // for tanh
                        hSignals[j] = sum * derivatives;
                    }

                    if (debug)
                    {
                        Console.WriteLine("hSignals");
                        Helper.ShowVector(hSignals);
                    }

                    // 4. compute input-hidden weights grads
                    for (int i = 0; i < _numInput; i++)
                    {
                        for (int j = 0; j < _numHidden; j++)
                        {
                            ihGrad[i, j] = hSignals[j] * _inputs[i];
                        }
                    }

                    if (debug)
                    {
                        Console.WriteLine("ihGrads");
                        Helper.ShowMatrix(ihGrad);
                    }

                    // 4.b compute input-hidden biases grads
                    for (int j = 0; j < _numHidden; j++)
                    {
                        hbGrad[j] = hSignals[j] * 1.0; // dummy 1.0 input
                    }

                    if (debug)
                    {
                        Console.WriteLine("ihBiasGrads");
                        Helper.ShowVector(hbGrad);
                    }

                    // ========begin update here==========
                    //1. update input-hidden weights
                    for (int i = 0; i < _numInput; i++)
                    {
                        for (int j = 0; j < _numHidden; j++)
                        {
                            var delta = lr * ihGrad[i, j];
                            ih_weights[i, j] -= delta;
                            // momentum involved
                            ih_weights[i, j] -= ihPrevWeightsDelta[i, j] * momentum;
                            ihPrevWeightsDelta[i, j] = delta;
                        }
                    }

                    if (debug)
                    {
                        Console.WriteLine("ih_weights after update");
                        Helper.ShowMatrix(ih_weights);
                        Console.WriteLine("ih_p_W_delta after update");
                        Helper.ShowMatrix(ihPrevWeightsDelta);
                    }

                    //2. update hidden biases
                    for (int j = 0; j < _numHidden; j++)
                    {
                        var delta = hbGrad[j] * lr;
                        _hBiases[j] -= delta;
                        _hBiases[j] -= hPrevBiasesDelta[j] * momentum;
                        hPrevBiasesDelta[j] = delta;
                    }

                    if (debug)
                    {
                        Console.WriteLine("_hBias after update");
                        Helper.ShowVector(_hBiases);
                        Console.WriteLine("h_p_W_delta after update");
                        Helper.ShowVector(hPrevBiasesDelta);
                    }

                    //3.update hidden-output weights
                    for (int j = 0; j < _numHidden; j++)
                    {
                        for (int k = 0; k < _numOutput; k++)
                        {
                            var delta = hoGrads[j, k] * lr;
                            _hoWeights[j, k] -= delta;
                            _hoWeights[j, k] -= hoPrevWeightsDelta[j, k] * momentum;
                            hoPrevWeightsDelta[j, k] = delta;
                        }
                    }

                    if (debug)
                    {
                        Console.WriteLine("ho_weights after update");
                        Helper.ShowMatrix(_hoWeights);
                        Console.WriteLine("ho_p_W_delta after update");
                        Helper.ShowMatrix(hoPrevWeightsDelta);
                    }

                    //4.update output biases
                    for (int k = 0; k < _numOutput; k++)
                    {
                        var delta = obGrads[k] * lr;
                        _oBiases[k] -= delta;
                        _oBiases[k] -= oPrevBiasesDelta[k] * momentum;
                        oPrevBiasesDelta[k] = delta;
                    }

                    if (debug)
                    {
                        Console.WriteLine("_oBias after update");
                        Helper.ShowVector(_oBiases);
                        Console.WriteLine("h_p_W_delta after update");
                        Helper.ShowVector(oPrevBiasesDelta);
                    }
                } //each training item
            } // end while(each epoch)

            var bestWeights = GetWeights();
            Console.WriteLine("Finished training");
            return bestWeights;
        }

        private void Shuffle(int[] sequence)
        {
            for (int i = 0; i < sequence.Length; i++)
            {
                var r = _rnd.Next(i, sequence.Length);
                var tmp = sequence[r];
                sequence[r] = sequence[i];
                sequence[i] = tmp;
            }
        }

        private double Error(double[][] data)
        {
            // MSE : average squared error per training item
            var sumSquaredErr = 0.0;
            var xValues = new double[_numInput]; // intput x(first input_num vals in train_data)
            var tValues = new double[_numOutput]; // output y(last num_output vals in train_data)
            for (int i = 0; i < data.Length; i++)
            {
                Array.Copy(data[i], xValues, _numInput);
                Array.Copy(data[i], _numInput, tValues, 0, _numOutput);
                var yValues = Forward(xValues);
                for (int j = 0; j < _numOutput; j++)
                {
                    var err = tValues[j] - yValues[j]; // calc
                    sumSquaredErr += Math.Pow(err, 2);
                }
            }

            return sumSquaredErr / data.Length;
        } //Error
    } // class NeuralNetwork
} //ns