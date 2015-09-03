using System;

namespace Plugin
{

    /// <summary>Represents the gearbox of a BR Class 52 'Western'.</summary>
    internal class WesternGearBox
    {
        /// <summary>Gets the state of the torque convertor.</summary>
        internal TorqueConvertorStates TorqueConvertorState { get; set; }
        /// <summary>Stores the timer used for filling the torque convertor.</summary>
        internal double TorqueConvertorTimer = 0.0;
        internal enum TorqueConvertorStates
        {
            /// <summary>The torque converter is currently empty, and the torque convertor error light is lit. The numerical value of this constant is 0.</summary>
            Empty = 0,
            /// <summary>The torque converter is currently filling. The numerical value of this constant is 1.</summary>
            FillInProgress = 1,
            /// <summary>The torque converter is on service. The numerical value of this constant is 2.</summary>
            OnService = 2,
        }
        /// <summary>Returns the maximum power notch allowed.</summary>
        internal int PowerNotch(int CurrentRPM, int NumberOfEngines, bool TurboChargerActive)
        {
            //Our initial calculated power notch will always be zero
            int FinalPowerNotch = 0;
            //First calculate the RPM as a percent of the standard maximum RPM
            //1650 is the BR spec for the engines, 1400 is the derated preserved spec
            //Maybe requires a twidde to change this, but that then requires train.dat editing.....
            double RPMPercentage = (CurrentRPM - 600) / 1050.0;
            //Now calculate the maximum power notch using Math.Ceiling
            double CalculatedPowerNotch = Math.Ceiling(8 * RPMPercentage);
            switch (NumberOfEngines)
            {
                case 1:
                    //If running on one engine, our final power notch is the calculated notch divided by two and rounded up
                    FinalPowerNotch = (int)Math.Ceiling(CalculatedPowerNotch / 2);
                    //We should only activate the final two power notches if the turbocharger is active
                    if (TurboChargerActive == true)
                    {
                        FinalPowerNotch += 2;
                    }
                    break;
                case 2:
                    //If running on two engines, then use the calculated power notch rounded up
                    FinalPowerNotch = (int)Math.Ceiling(CalculatedPowerNotch);
                    //We should only activate the final two power notches if the turbocharger is active
                    if (TurboChargerActive == true)
                    {
                        FinalPowerNotch += 2;
                    }
                    break;
            }
            return FinalPowerNotch;
        }
        
    }
}