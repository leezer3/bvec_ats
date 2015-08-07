namespace Plugin
{
    internal partial class PZB: Device
    {
        internal class Program
        {
            internal double InductorLocation;
            internal double InductorDistance;
            internal int Type;
            internal double MaxSpeed;
            internal double AcknowledgementTimer;
            internal bool BrakeCurveSwitchMode;
            internal double BrakeCurveTimer;
            internal double SwitchTimer;
            internal double BrakeReleaseTimer;

            /// <summary>Gets the current warning state of the PZB System.</summary>
            internal PZBProgramStates ProgramState { get; set; }
        }
    }
}
