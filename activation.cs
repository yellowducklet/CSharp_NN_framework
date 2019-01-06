using System;

namespace cs_nn_fm {
    internal class Activation {
        public static double HyperTan (double x) // hyperbolic tan
        {
            if (x < -20)
                return -1.0;
            return x > 20 ? 1.0 : Math.Tanh (x);
        }
        public static double Sigmoid (double x) { // sigmoid activation
            return 1 / (1 + Math.Exp (-x));
        }
    }
}