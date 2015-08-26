using System.Text;

namespace Plugin
{

    /// <summary>Manages the startup and self-test sequence.</summary>
    internal class WesternGearBox
    {
        /// <summary>Gets the state of the startup self-test sequence.</summary>
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
    }
}