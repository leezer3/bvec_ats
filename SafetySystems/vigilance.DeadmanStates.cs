namespace Plugin
{

    /// <summary>Represents a deadman's handle.</summary>
    internal partial class Vigilance : Device
    {
        /// <summary>Possible non-visual states of the deadman's handle.</summary>
        internal enum DeadmanStates
        {
            /// <summary>The deadman's handle is inactive or disabled.</summary>
            None = 0,
            /// <summary>The deadman's handle timer is running.</summary>
            OnTimer = 1,
            /// <summary>The deadman's handle timer has expired and the warning light has lit.</summary>
            TimerExpired = 2,
            /// <summary>The deadman's handle timer has expired and the audible alarm is triggered.</summary>
            OnAlarm = 3,
            /// <summary>The deadman's handle alarm timer is running.</summary>
            AlarmTimer = 4,
            /// <summary>The deadman's handle has applied the brakes.</summary>
            BrakesApplied = 5,
        }

        internal enum VigilanteStates
        {
            /// <summary>The Vigilante device is inactive.</summary>
            None = 0,
            /// <summary>The train is moving and the alarm is sounding.</summary>
            AlarmSounding = 1,
            /// <summary>The train is moving and the alarm has been acknowledged</summary>
            OnService = 2,
            /// <summary>The alarm has not been cancelled in time and the EB has been applied.</summary>
            EbApplied = 3,
        }
    }
}