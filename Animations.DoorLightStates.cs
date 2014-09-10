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
    }
}