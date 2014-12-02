namespace Plugin
{
    /// <summary>Represents the traction modelling of the Italian SCMT system.</summary>
    internal partial class SCMT_Traction : Device
    {
        /// <summary>Represents a timer.</summary>
        internal struct Timer
        {
            public double TimeElapsed;
            public bool TimerActive;
        }

        /// <summary>Represents a flashing indicator.</summary>
        internal struct Indicator
        {

            public bool Flashing;
            public bool FlashOnce;
            public bool Solid;
            public double FlashInterval;
            public double TimeElapsed;
            public int IndicatorState;
        }
    }
}
