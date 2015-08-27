namespace Plugin
{

    /// <summary>Manages the startup and self-test sequence.</summary>
    internal partial class WesternStartupManager
    {
        /// <summary>Possible train startup self test sequence initialisation states.</summary>
        internal enum SequenceStates
        {
            /// <summary>The startup and self-test procedure has not been initiated yet. The numerical value of this constant is 0.</summary>
            Pending = 0,
            /// <summary>The battery has been energized, providing power to the electrical systems. The numerical value of this constant is 1.</summary>
            BatteryEnergized = 1,
            /// <summary>The transmission reset button has been pressed. The numerical value of this constant is 2.</summary>
            TransmissionResetPressed = 2,
            /// <summary>The master key has been inserted. The numerical value of this constant is 3.</summary>
            MasterKeyInserted = 3,
            /// <summary>A direction has been selected. The numerical value of this constant is 4.</summary>
            DirectionSelected = 4,
            /// <summary>The DSD buzzer has been acknowledged. The numerical value of this constant is 5.</summary>
            DSDAcknowledged = 5,
            /// <summary>Neutral reverser has been selected. The numerical value of this constant is 6.</summary>
            NeutralSelected = 6,
            /// <summary>The locomotive is now ready to start. The numerical value of this constant is 6.</summary>
            ReadyToStart = 7,
            /// <summary>The locomotive's AWS system has been energized.</summary>
            AWSOnline = 8
        }
    }
}
