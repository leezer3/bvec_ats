namespace Plugin
{

    /// <summary>Represents a flashing doors light.</summary>
    internal partial class Animations : Device
    {
        /// <summary>Possible states of the doors light.</summary>
        internal enum DoorLightStates
        {
            /// <summary>The doors are closed and the train is in motion.</summary>
            InMotion = 0,
            /// <summary>The doors light has been primed.</summary>
            Primed = 1,
            /// <summary>The doors are open.</summary>
            DoorsOpen = 2,
            /// <summary>The train has stopped with the doors open & the countdown is active.</summary>
            Countdown = 3,
            /// <summary>The doors closing light is flashing.</summary>
            DoorsClosing = 4,
            /// <summary>The doors have closed.</summary>
            DoorsClosed = 5,
        }

        /// <summary>Possible states of the cylinder cocks steam puff</summary>
        internal enum CylinderPuffStates
        {
            /// <summary>The cylinder cock is closed and emitting no steam.</summary>
            CockClosed = 0,
            /// <summary>The cylinder cock is open, and the train is stationary with no regulator applied.</summary>
            OpenStationary = 1,
            /// <summary>The cylinder cock is open, and the train is stationary with no regulator applied.</summary>
            OpenStationaryPowered = 2,
            /// <summary>The cylinder cock is open, and the piston is moving on the outward stroke- No puff.</summary>
            OpenNoPuff = 3,
            /// <summary>The cylinder cock is open, and the piston is moving on the inbound stroke with power applied- Large puff</summary>
            OpenPuffingPowered = 4,
            /// <summary>The cylinder cock is open, and the piston is moving on the inbound stroke with no power applied- Small puff.</summary>
            OpenPuffingUnpowered = 5,
        }
    }
}