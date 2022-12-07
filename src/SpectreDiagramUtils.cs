using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lab4
{
    internal static class SpectreDiagramUtils
    {
        public static double[] GetSpectreDiagram(double[] v, uint from, uint length)
        {
            if (from >= v.Length) 
                throw new ArgumentException("Out of bounds. ", nameof(from));

            double[] v1 = new double[length];
            Array.Copy(v, from, v1, 0, length);

            return GetSpectreDiagram(v1);
        }

        private static double[] GetSpectreDiagram(double[] v1)
        {
            double[] hannDoubles = MathNet.Numerics.Window.Hamming(v1.Length);
            
            double[] v2 = new double[v1.Length];
            for (int i = 0; i < v1.Length; i++)
            {
                v2[i] = hannDoubles[i] * v1[i];
            }

            return v2;
        }
    }
}
