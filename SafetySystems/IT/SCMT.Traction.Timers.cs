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
            public int PanelIndex;
            public double FlashInterval;
            public double TimeElapsed;
            public bool Lit;
            public IndicatorStates IndicatorState;

        }

        internal enum IndicatorStates
        {
            Off = 0,
            Flashing = 1,
            Solid = 2,
        }
    }
}
