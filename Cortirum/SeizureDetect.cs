using MathNet.Numerics.Statistics;
using MathNet.Numerics.LinearRegression;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CortriumBLE.SignalProcessing
{
    public static class SeizureDetect
    {
        /// <summary>
        /// Calculates ModCSI and CSI based on filtered and unfiltered RR intervals.
        /// </summary>
        /// <param name="rrIntervals">The RR intervals as a list of integers.</param>
        /// <param name="movingWindowSize">The moving window size (mws) for calculations.</param>
        /// <returns>An array of two double values: ModCSI and CSI with slope adjustment.</returns>
        public static double[] CalcModCSI_and_CSI(List<int> rrIntervals, int movingWindowSize)
        {
            double[] rrFiltered = ApplyMedianFilter(rrIntervals, movingWindowSize);
            double slope = CalculateSlope(rrFiltered, movingWindowSize);

            double SD1Filt = CalculateSD1(rrFiltered, movingWindowSize);
            double SD2Filt = CalculateSD2(rrFiltered, movingWindowSize);
            double SD1Unfilt = CalculateSD1Unfiltered(rrIntervals, movingWindowSize);
            double SD2Unfilt = CalculateSD2Unfiltered(rrIntervals, movingWindowSize);

            double T_filt = 4 * SD1Filt;
            double L_filt = 4 * SD2Filt;
            double T_unfilt = 4 * SD1Unfilt;
            double L_unfilt = 4 * SD2Unfilt;

            double modCSI = (L_filt * L_filt) / T_filt;
            double modCSI_slope = modCSI * slope;
            double CSI = L_unfilt / T_unfilt;
            double CSI_slope = CSI * slope;

            return new double[] { modCSI_slope, CSI_slope };
        }

        public static double[] ApplyMedianFilter(List<int> rrIntervals, int movingWindowSize)
        {
            double[] rrFiltered = new double[movingWindowSize];
            for (int j = 0; j < movingWindowSize; j++)
            {
                double[] filterBuffer = rrIntervals.Skip(j).Take(7).Select(Convert.ToDouble).ToArray();
                Array.Sort(filterBuffer);
                rrFiltered[j] = filterBuffer.Length % 2 == 0
                    ? (filterBuffer[filterBuffer.Length / 2] + filterBuffer[filterBuffer.Length / 2 - 1]) / 2.0
                    : filterBuffer[filterBuffer.Length / 2];
            }
            return rrFiltered;
        }

        public static double CalculateSlope(double[] rrFiltered, int movingWindowSize)
        {
            double[] xAxis = new double[movingWindowSize];
            double[] yAxis = new double[movingWindowSize];

            for (int j = 0; j < movingWindowSize; ++j)
            {
                xAxis[j] = j > 0 ? xAxis[j - 1] + rrFiltered[j] : rrFiltered[j];
                yAxis[j] = 60.0 / rrFiltered[j];
            }

            var regression = SimpleRegression.Fit(xAxis, yAxis);
            return Math.Abs(regression.Item2);
        }

        public static double CalculateSD1(double[] rrFiltered, int movingWindowSize)
        {
            double[] rrDiffFilt = rrFiltered.Zip(rrFiltered.Skip(1), (a, b) => (b - a) * (Math.Sqrt(2) / 2)).ToArray();
            return 1000 * Statistics.StandardDeviation(rrDiffFilt);
        }

        public static double CalculateSD2(double[] rrFiltered, int movingWindowSize)
        {
            double[] rrSumFilt = rrFiltered.Zip(rrFiltered.Skip(1), (a, b) => (a + b) * (Math.Sqrt(2) / 2)).ToArray();
            return 1000 * Statistics.StandardDeviation(rrSumFilt);
        }

        public static double CalculateSD1Unfiltered(List<int> rrIntervals, int movingWindowSize)
        {
            double[] rrDiffUnfilt = rrIntervals.Skip(6).Zip(rrIntervals.Skip(7), (a, b) => (b - a) * (Math.Sqrt(2) / 2)).ToArray();
            return 1000 * Statistics.StandardDeviation(rrDiffUnfilt);
        }

        public static double CalculateSD2Unfiltered(List<int> rrIntervals, int movingWindowSize)
        {
            double[] rrSumUnfilt = rrIntervals.Skip(6).Zip(rrIntervals.Skip(7), (a, b) => (a + b) * (Math.Sqrt(2) / 2)).ToArray();
            return 1000 * Statistics.StandardDeviation(rrSumUnfilt);
        }
    }
}
