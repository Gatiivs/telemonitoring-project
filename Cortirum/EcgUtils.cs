using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CortriumBLE.SignalProcessing
{
    /// <summary>
    /// These are utilities to calculate useful information - such as finding the 
    /// s and calculaing the heart rate
    /// </summary>
    public class EcgUtils
    {
        /// <summary>
        /// This is Stefans home made Peak detector
        /// It should be replaced by something better - but actually works
        /// fairly nice - when used on data with a max regression slope of 20%
        /// WARNING: THIS IS NOT VALIDATE FOR CLINICAL USE 
        /// </summary>
        /// <param name="signal">the data as an array of integers</param>
        /// <param name="samplingRate">the sample rate we use</param>
        /// <returns></returns>
        public static List<int> DetectPeaks(int[] signal, double samplingRate)
        {
            List<int> rPeaks = new List<int>();

            // Find the minimum value in the signal
            int lowest = signal.Min();

            // Shift the signal if there are negative values
            if (lowest < 0)
            {
                for (int i = 0; i < signal.Length; i++)
                {
                    signal[i] += Math.Abs(lowest);
                }
            }

            double threshold = signal.Max() * 0.50;  // This is the adaptive threshold
                                                     // Consider increasing to 0.75
                                                     // results are BEST with 0.5 !!!
                                                     // if I use slope of 20% max 
                                                     // this means - it will only take
                                                     // measurements when certain

            int lastPeak = 0; // Save the last peak

            for (int i = 1; i < signal.Length - 1; i++)
            {
                if (signal[i] > threshold && signal[i] > signal[i - 1] && signal[i] > signal[i + 1])
                {
                    //Check that we at least wait 2% of the total signal time
                    if (lastPeak == 0 || (i - lastPeak) > signal.Length*0.02)
                    { 
                        rPeaks.Add(i);
                        lastPeak = i;
                    }
                }
            }
            return rPeaks;
        }


        /// <summary>
        /// This is Stefans slope calucluate
        /// It will calculate the difference in the peaks 
        /// Maybe consider looking at the entire signal instead 
        /// </summary>
        /// <param name="peaks"></param>
        /// <returns></returns>
        public static double CalculateSlope(List<int> peaks)
        {
            int n = peaks.Count;
            if (n < 2) return -100;  //throw new InvalidOperationException("At least two points are required to calculate a slope.");

            // Calculate means of x (indices) and y (peaks values)
            double meanX = (n - 1) / 2.0;  // Index mean for 0, 1, ..., n-1 is (n-1) / 2
            double meanY = peaks.Average();

            // Calculate the slope using the regression formula
            double numerator = 0;
            double denominator = 0;

            for (int i = 0; i < n; i++)
            {
                double xDiff = i - meanX;
                double yDiff = peaks[i] - meanY;

                numerator += xDiff * yDiff;
                denominator += xDiff * xDiff;
            }

            double slope = numerator / denominator;
            return slope;
        }

    }

    public class EcgLowPassFilter
        {
        private static int[] FILTER_TAPS_ECG_250_SPS = {
            12,
            131,
            21,
            -16,
            -47,
            -28,25,
            58,
            33,
            -32,
            -71,
            -38,
            41,
            86,
            44,
            -51,
            -103,
            -50,
            62,
            121,
            57,
            -76,
            -143,
            -64,
            92,
            166,
            71,
            -111,
            -193,
            -78,
            132,
            223,
            85,
            -157,
            -257,
            -93,
            186,
            294,
            100,
            -220,
            -337,
            -107,
            259,
            386,
            114,
            -306,
            -442,
            -121,
            361,
            506,
            127,
            -426,
            -583,
            -133,
            507,
            675,
            139,
            -607,
            -788,
            -144,
            735,
            933,
            149,
            -905,
            -1127,
            -153,
            1145,
            1403,
            156,
            -1510,
            -1836,
            -159,
            2139,
            2626,
            160,
            -3508,
            -4577,
            -162,
            8942,
            18142,
            22007,
            18142,
            8942,
            -162,
            -4577,
            -3508,
            160,
            2626,
            2139,
            -159,
            -1836,
            -1510,
            156,
            1403,
            1145,
            -153,
            -1127,
            -905,
            149,
            933,
            735,
            -144,
            -788,
            -607,
            139,
            675,
            507,
            -133,
            -583,
            -426,
            127,
            506,
            361,
            -121,
            -442,
            -306,
            114,
            386,
            259,
            -107,
            -337,
            -220,
            100,
            294,
            186,
            -93,
            -257,
            -157,
            85,
            223,
            132,
            -78,
            -193,
            -111,
            71,
            166,
            92,
            -64,
            -143,
            -76,
            57,
            121,
            62,
            -50,
            -103,
            -51,
            44,
            86,
            41,
            -38,
            -71,
            -32,
            33,
            58,
            25,
            -28,
            -47,
            -16,
            21,
            131,
            12
    };

        private double NRCOEFF = 0.987488;
        private static int FILTER_ORDER = 161;
        private int SAMPLE_FILTER_ERROR = -32766;

        private int bufferStart;
        private int bufferCurrent;
        private int[] workingBuffer = new int[2 * FILTER_ORDER];
        private int[] coefficient = (int[])FILTER_TAPS_ECG_250_SPS.Clone();

        public EcgLowPassFilter()
            {
     

            }

            public int FilterInput(int input)
            {
                int sample = input;

                // Store the value in working buffer
                workingBuffer[bufferCurrent] = sample;
                int  result = FilterProcess(workingBuffer, bufferCurrent, coefficient);

                // Store the DC removed value in working buffer
                workingBuffer[bufferStart] = sample;

                bufferStart++;
                bufferCurrent++;

                if (bufferStart == FILTER_ORDER - 1)
                {
                    bufferStart = 0;
                    bufferCurrent = FILTER_ORDER - 1;
                }

                return result;
            }

            private int FilterProcess(int[] workingBuffer, int offset, int[] coefficientBuffer)
            {
                int result = 0;

                for (int index = 0; index < FILTER_ORDER;)
                {
                    if (offset < 0)
                        offset = FILTER_ORDER - 1;

                    result += workingBuffer[offset--] * coefficientBuffer[index++];
                }

                return (int)(result / 65536);
            }
        }


    public class EcgHighPassFilter
        {
            private const int MAX_INT_24 = 8388607;
            private const int MIN_INT_24 = -8388608;
            private const int FILTER_ERROR = -32766;

            private int iir_X_Prev;
            private int iir_Y_Prev;

            public EcgHighPassFilter()
            {
                ResetFilter();
            }

            public void ResetFilter()
            {
                iir_X_Prev = 0;
                iir_Y_Prev = 0;
            }

            public int FilterInput(int input)
            {
                int sample = input / 4;

                int temp = iir_Y_Prev / 128;
                temp = iir_Y_Prev - temp;
                iir_Y_Prev = (sample - iir_X_Prev) + temp;
                iir_X_Prev = sample;

                if (iir_Y_Prev > MAX_INT_24)
                {
                    iir_Y_Prev = MAX_INT_24;
                    sample = FILTER_ERROR;
                }
                else if (iir_Y_Prev < MIN_INT_24)
                {
                    iir_Y_Prev = MIN_INT_24;
                    sample = FILTER_ERROR;
                }
                else
                {
                    sample = iir_Y_Prev / 1;
                }

                return (int)sample;
            }

    }



public class HeartRateCalculator
    {
        private List<double> hrValues = new List<double>(); // List to store last 20 HR values

        public double CalculateHeartRate(List<int> peaks, double time)
        {

            
            // Calculate current HR based on peak count
            double hr = peaks.Count * 60 / time;

            double  average = 0;

            if (hrValues.Count > 0)
                average = hrValues.Average();

            if (hr > 50 &&  hr <150)
                // Add the new HR to the list
                hrValues.Add(hr);

            // Keep only the last 100 HR values
            if (hrValues.Count > 200)
            {
                hrValues.RemoveAt(0); // Remove the oldest HR value
            }

            var median = GetMedian(hrValues);
            if (median - average > 20)
                if (median < 40 || median > 100)
                    return average;
                if (median > 39 &&  median < 101)
                    return (average*2+median)/3;
            // Return the median HR from the list
            return median;
        }

        private double GetMedian(List<double> values)
        {
            var sortedValues = values.OrderBy(v => v).ToList();
            int count = sortedValues.Count;

            if (count % 2 == 1)
            {
                // Odd count, return the middle element
                return sortedValues[count / 2];
            }
            else
            {
                // Even count, return the average of the two middle elements
                return (sortedValues[(count / 2) - 1] + sortedValues[count / 2]) / 2.0;
            }
        }
    }


}

