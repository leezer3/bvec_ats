namespace Plugin
{

    /// <summary>Represents a pantograph.</summary>
    internal partial class electric : Device
    {
        /// <summary>Possible states of the pantograph.</summary>
        internal enum PantographStates
        {
            /// <summary>The pantograph is lowered and providing no current.</summary>
            Lowered = 0,
            /// <summary>The pantograph has been raised and the line volts timer is active.</summary>
            RaisedTimer = 1,
            /// <summary>The pantograph has been raised and the line volts timer has expired. The ACB/ VCB may now be closed</summary>
            VCBReady = 2,
            /// <summary>The pantograph has been raised with the ACB/ VCB closed.</summary>
            RaisedVCBClosed = 3,
            /// <summary>The pantograph VCB reset timer is running.</summary>
            VCBResetTimer = 4,
            /// <summary>The pantograph is on service & providing traction current.</summary>
            OnService = 5,
            /// <summary>The pantograph has been lowered at speed.</summary>
            LoweredAtSpeed = 6,
            /// <summary>The pantograph has been lowered at speed & brakes have been applied.</summary>
            LoweredAtspeedBraking = 7,
            /// <summary>The pantograph is disabled or not fitted.</summary>
            Disabled = 8,
        }
    }
}