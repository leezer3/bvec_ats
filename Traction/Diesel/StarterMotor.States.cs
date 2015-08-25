namespace Plugin
{
    partial class StarterMotor
    {
        internal enum StarterMotorStates
        {
            /// <summary>Set this state when no processing or action is to be taken. The numerical value of this constant is 0.</summary>
            None = 0,
            /// <summary>The starter motor is currently running up. The numerical value of this constant is 1.</summary>
            RunUp = 1,
            /// <summary>The starter motor is currently active. The numerical value of this constant is 2.</summary>
            Active = 2,
            /// <summary>The engine has fired. The numerical value of this constant is 3.</summary>
            EngineFire = 3,
            /// <summary>The engine has stalled. The numerical value of this constant is 4.</summary>
            EngineStall = 4,
            /// <summary>The starter motor is currently running down. The numerical value of this constant is 5.</summary>
            RunDown = 5,
            /// <summary>The starter motor sequence can now be restarted. The numerical value of this constant is 6.</summary>
            CanRestart = 6
        }
    }
}
