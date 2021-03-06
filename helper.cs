using System;
using System.Reflection;

namespace cs_nn_fm
{
    public class Helper
    {
        public static void SplitInputOutput(double[][] allData, out double[][] inputData, out double[][] outputData)
        {
            inputData = new double[allData.Length][];
            outputData = new double[allData.Length][];
            for (int i = 0; i < allData.Length; i++)
            {
                inputData[i] = new[] {allData[i][0]};
                outputData[i] = new[] {allData[i][1]};
            }
        }

        //split TrainSet and OutputSet using reflection
        public static void SplitTrainTest(Dataset allData, double trainPct,
            int seed, out Dataset trainData, out Dataset testData, bool shuffle_flag = false)
        {
            Random rnd = new Random(seed);
            var allDataItems = allData.DataSet;
            var totRows = allDataItems.Length;
            var trainRows = (int) (totRows * trainPct); // usually 0.80
            var testRows = totRows - trainRows;
            var trainDataItems = new double[trainRows][];
            var testDataItems = new double[testRows][];
//            dynamic trainData = (allData)trainData;
//            testData = new double[testRows][];

            double[][] copy = new double[allDataItems.Length][]; // ref copy of data
            for (int i = 0; i < copy.Length; ++i)
                copy[i] = allDataItems[i];

            if (shuffle_flag)
            {
                for (int i = 0; i < copy.Length; ++i) // scramble order
                {
                    int r = rnd.Next(i, copy.Length); // use Fisher-Yates
                    double[] tmp = copy[r];
                    copy[r] = copy[i];
                    copy[i] = tmp;
                }
            }

            for (int i = 0; i < trainRows; ++i)
                trainDataItems[i] = copy[i];

            for (int i = 0; i < testRows; ++i)
                testDataItems[i] = copy[i + trainRows];
            var assembly = Assembly.GetExecutingAssembly(); // 获取当前程序集 
//            var type = allData.GetType();

            //parameters
            var trainSetParameters = new object[] {trainRows, trainDataItems, 1, true};
            var testSetParameters = new object[] {testRows, testDataItems, 1, true};
            trainData = assembly.CreateInstance((allData.GetType()).ToString(), true,
                BindingFlags.Default, null, trainSetParameters, null, null) as Dataset; // reflection
            testData = assembly.CreateInstance(allData.GetType().ToString(), true,
                BindingFlags.Default, null, testSetParameters, null, null) as Dataset;
        } // SplitTrainTest

        public static void InitializeWeights(ref double[,] weights, double lo = -0.001, double hi = 0.001,
            int rnd_seed = 1)
        {
            var rnd = new Random(rnd_seed);
            var row = weights.GetLength(0);
            var col = weights.GetLength(1);
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    weights[i, j] = lo + (hi - lo) * rnd.NextDouble();
                }
            }
        }

        public static double[,] MakeMatrix(int rows, int cols, double init_val = 0.0) //helper method
        {
            var res = new double[rows, cols];
//            for (int r = 0; r < res.Length; r++)
//            {
//                res[r] = new double[cols];
//            }

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < cols; j++)
                {
                    res[i, j] = init_val;
                }
            }

            return res;
        }

        public static void ShowVector(double[] vector, int decimals = 9, int line_len = 6, bool new_line = true)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                if (i % line_len == 0 && i > 0) // avoid the state 
                    Console.WriteLine("");
                Console.Write(vector[i].ToString("F" + decimals) + " ");
            }

            if (new_line)
                Console.WriteLine("");
        }

        public static void ShowMatrix(double[][] matrix, int numRows, int decimals = 10, bool indices = true)
        {
            int len = matrix.Length; // refractor?
            if (len < numRows)
            {
                numRows = len;
            }

            for (int i = 0; i < numRows; i++)
            {
                if (indices)
                    Console.Write("[" + i.ToString().PadLeft(len) + "] ");
                for (int j = 0; j < matrix[i].Length; j++)
                {
                    var v = matrix[i][j];
                    if (v >= 0.0) //refractor?
                        Console.Write(" "); //'+'
                    Console.Write(v.ToString("F" + decimals) + "  ");
                }

                Console.WriteLine("");
            }

            if (numRows < matrix.Length)
            {
                Console.WriteLine(". . .");
                int lastRow = matrix.Length - 1;
                if (indices)
                    Console.Write("[" + lastRow.ToString().PadLeft(len) + "]");
                for (int j = 0; j < matrix[lastRow].Length; j++)
                {
                    var v = matrix[lastRow][j];
                    if (v >= 0.0)
                        Console.Write("  ");
                    Console.Write(v.ToString("F" + decimals) + "  ");
                    ;
                }
            }

            Console.WriteLine("\n");
        }

        public static void ShowMatrix(double[,] matrix, int numRows = 12, int decimals = 10, bool indices = true)
        {
            int len = matrix.GetLength(0); // refractor?
            if (len < numRows)
            {
                numRows = len;
            }

            for (int i = 0; i < numRows; i++)
            {
                if (indices)
                    Console.Write("[" + i.ToString().PadLeft(len) + "] ");
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    var v = matrix[i, j];
                    if (v >= 0.0) //refractor?
                        Console.Write(" "); //'+'
                    Console.Write(v.ToString("F" + decimals) + "  ");
                }

                Console.WriteLine("");
            }

            if (numRows < matrix.GetLength(0))
            {
                Console.WriteLine(". . .");
                int lastRow = matrix.Length - 1;
                if (indices)
                    Console.Write("[" + lastRow.ToString().PadLeft(len) + "]");
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    var v = matrix[lastRow, j];
                    if (v >= 0.0)
                        Console.Write("  ");
                    Console.Write(v.ToString("F" + decimals) + "  ");
                    ;
                }
            }

            Console.WriteLine("\n");
        }

        public static double[][] GenerateClassificationModelData(Model model, int numRows, int seed = 1,
            double inputLow = -10, double inputHigh = 10.0)
        {
            Random rnd = new Random(seed);
            int weightsNum = model.WeightsNum;

            double[][] result = new double[numRows][]; // allocate return-result
            for (int i = 0; i < numRows; ++i)
                result[i] = new double[model.InputNum + model.OutputNum]; // 1-of-N in last column

            for (int r = 0; r < numRows; ++r) // for each row
            {
                // generate random inputs
                double[] inputs = new double[model.InputNum];
                for (int i = 0; i < inputs.Length; ++i)
                    inputs[i] = (inputHigh-inputLow)* rnd.NextDouble() +inputLow; // [-10.0 to -10.0] by default

                // compute outputs
                double[] outputs = model.Forward(inputs).PredictedValues;

                // translate outputs to 1-of-N. Especially for classification
                double[] oneOfN = new double[model.OutputNum]; // all 0.0

                var maxIndex = 0;
                var maxValue = outputs[0];
                for (var i = 0; i < model.OutputNum; ++i)
                {
                    if (!(outputs[i] > maxValue)) continue;
                    maxIndex = i;
                    maxValue = outputs[i];
                }

                oneOfN[maxIndex] = 1.0;

                // place inputs and 1-of-N output values into curr row
                int c = 0; // column into result[][]
                for (int i = 0; i < model.InputNum; ++i) // inputs
                    result[r][c++] = inputs[i];
                for (int i = 0; i < model.OutputNum; ++i) // outputs
                    result[r][c++] = oneOfN[i];
            } // each row

            return result;
        }
    }
}