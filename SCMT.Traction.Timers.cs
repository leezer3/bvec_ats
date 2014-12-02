namespace Plugin
{
    /// <summary>Represents the traction modelling of the Italian SCMT system.</summary>
    internal partial class SCMT_Traction : Device
    {
        internal struct Timer
        {
            public double TimeElapsed;
            public bool TimerActive;
        }
    }
}
