using AForge.Math;
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
        public static (double[], double[]) GetSpectreDiagram(double[] v, uint from, uint length, uint samplingRate)
        {
            if (from >= v.Length) 
                throw new ArgumentException("Out of bounds. ", nameof(from));

            double[] v1 = new double[length];
            Array.Copy(v, from, v1, 0, length);

            return GetSpectreDiagram(v1, samplingRate);
        }

        private static (double[], double[]) GetSpectreDiagram(double[] v, uint samplingRate)
        {
            v = MultiplyWindow(v);

            Complex[] f = PerformFourierTransform(v);

            double[] s = GetAmplitudeSpectrum(f);

            s = PerformScaling(s);

            return CreateAmplitudeSpectrum(s, v.Length, samplingRate);
        }

        private static double[] MultiplyWindow(double[] v)
        {
            double[] hannDoubles = MathNet.Numerics.Window.Hamming(v.Length);
            double[] v1 = new double[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                v1[i] = hannDoubles[i] * v[i];
            }
            return v1;
        }

        private static Complex[] PerformFourierTransform(double[] v)
        {
            Complex[] c = new Complex[v.Length];
            for (int i = 0; i < v.Length; ++i)
            {
                c[i] = new Complex(v[i], 0);
            }

            FourierTransform.DFT(c, FourierTransform.Direction.Forward);

            for (int i = 0; i < c.Length; ++i)
            {
                c[i] /= v.Length;
            }

            return c;
        }

        private static double[] GetAmplitudeSpectrum(Complex[] f)
        {
            double[] x = new double[f.Length % 2 == 0 ? (f.Length / 2 + 1) : (f.Length + 1) / 2];
            for (int i = 0; i < x.Length; ++i)
            {
                x[i] = Math.Sqrt(f[i].Re * f[i].Re * f[i].Im * f[i].Im);
            }

            return x;
        }

        private static double[] PerformScaling(double[] x)
        {
            for (int i = 1; i < x.Length; i += 2)
            {
                x[i] *= 2;
            }

            return x;
        }

        private static (double[], double[]) CreateAmplitudeSpectrum(double[] s, int N, uint samplingRate)
        {
            double[] ys = new double[s.Length];
            double[] xs = new double[s.Length];
            double df = samplingRate / N;

            for (int i = 0; i < xs.Length; ++i)
            {
                xs[i] = i * df;
            }

            for (int i = 0; i < ys.Length; ++i)
            {
                ys[i] = s[i];
            }

            return (xs, ys);
        }
    }
}
