using System;
using System.IO;

namespace Plugin
{
    partial class InternalFunctions
    {
        //Call this function to validate a number input to a panel or sound index
        //It will attempt to parse the input, discarding all decimals
        //and checking whether the number is in the range -1 to 255
        /// <summary>Validate whether a panel/ sound index is potentially usable</summary>
        internal static void ValidateIndex(string input, ref int output, string failingvalue)
        {
            try
            {
                int finishedindex = Convert.ToInt32(Math.Floor(Convert.ToDouble(input)));
                if (finishedindex >= -1 && finishedindex <= 255)
                {
                    output = finishedindex;
                }
                else
                {
                    throw new InvalidDataException();
                }
            }
            catch
            {
                LogError(failingvalue, 1);

            }

        }
        //Call this function to validate a number input to a base setting
        //It will attempt to parse the input, discarding all decimals
        //and checking whether the number is in the range -2 to 3
        /// <summary>Validate whether a setting is potentially usable (Will not cause crashes)</summary>
        internal static void ValidateSetting(string input, ref int output, string failingvalue)
        {
            try
            {
                int finishedsetting = Convert.ToInt32(Math.Floor(Convert.ToDouble(input)));
                if (finishedsetting >= -2 && finishedsetting <= 3)
                {
                    output = finishedsetting;
                }
                else
                {
                    throw new InvalidDataException();
                }
            }
            catch
            {
                LogError(failingvalue, 2);

            }

        }
        //Call this function to parse a large number string input
        //It will simply attempt to parse the number into a double
        /// <summary>Check whether the input is a valid number</summary>
        internal static void ParseNumber(string input, ref double output, string failingvalue)
        {
            try
            {
                double finishednumber = double.Parse(input);
                output = finishednumber;
            }
            catch
            {
                LogError(failingvalue, 3);

            }

        }

        //Call this function to parse a large number string input
        //It will simply attempt to parse the number into an integer
        /// <summary>Check whether the input is a valid number</summary>
        internal static void ParseNumber(string input, ref int output, string failingvalue)
        {
            try
            {
                int finishednumber = Int32.Parse(input);
                output = finishednumber;
            }
            catch
            {
                LogError(failingvalue, 3);

            }

        }

        //Call this function to parse a number input into a bool
        /// <summary>Parses a bool type setting from a string input</summary>
        internal static void ParseBool(string input, ref bool output, string failingvalue)
        {
            try
            {
                if (input == "-1" || input == "false")
                {
                    output = false;
                }
                else
                {
                    output = true;
                }
            }
            catch
            {
                LogError(failingvalue, 4);
            }

        }
        internal static void UpgradeKey(string input, ref string output, string failingvalue)
        {
            try
            {
                switch (input)
                {
                    case "0":
                        output = "S";
                        break;
                    case "1":
                        output = "A1";
                        break;
                    case "2":
                        output = "A2";
                        break;
                    case "3":
                        output = "B1";
                        break;
                    case "4":
                        output = "B2";
                        break;
                    case "5":
                        output = "C1";
                        break;
                    case "6":
                        output = "C2";
                        break;
                    case "7":
                        output = "D";
                        break;
                    case "8":
                        output = "E";
                        break;
                    case "9":
                        output = "F";
                        break;
                    case "10":
                        output = "G";
                        break;
                    case "11":
                        output = "H";
                        break;
                    case "12":
                        output = "I";
                        break;
                    case "13":
                        output = "J";
                        break;
                    case "14":
                        output = "K";
                        break;
                    case "15":
                        output = "L";
                        break;
                }
            }
            catch
            {
                LogError(failingvalue, 5);

            }

        }
    }
}