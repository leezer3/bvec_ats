using System;

namespace Plugin
{
    partial class InternalFunctions
    {
        /// <summary>Parses a comma separated string to an array of integers</summary>
        /// <param name="InputString">The input string.</param>
        /// <param name="OutputArray">The array to output to.</param>
        /// <param name="FailingValue">The error string to pass to the debug logger if this fails.</param>
        public static void ParseStringToIntArray(string InputString, ref int[] OutputArray, string FailingValue)
        {
            try
            {
                string[] splitheatingrate = InputString.Split(',');
                OutputArray = new int[splitheatingrate.Length];
                for (int i = 0; i < OutputArray.Length; i++)
                {
                    OutputArray[i] = Int32.Parse(splitheatingrate[i]);
                }
            }
            catch
            {
                InternalFunctions.LogError("An error occured whilst attempting to parse " + FailingValue,6);
            }
        }
        /// <summary>Parses a comma separated string to an array of doubles</summary>
        /// <param name="InputString">The input string.</param>
        /// <param name="OutputArray">The array to output to.</param>
        /// <param name="FailingValue">The error string to pass to the debug logger if this fails.</param>
        public static void ParseStringToDoubleArray(string InputString, ref double[] OutputArray, string FailingValue)
        {
            try
            {
                string[] splitheatingrate = InputString.Split(',');
                OutputArray = new double[splitheatingrate.Length];
                for (int i = 0; i < OutputArray.Length; i++)
                {
                    OutputArray[i] = Double.Parse(splitheatingrate[i]);
                }
            }
            catch
            {
                InternalFunctions.LogError("An error occured whilst attempting to parse " + FailingValue, 6);
            }
        }
    }
}
