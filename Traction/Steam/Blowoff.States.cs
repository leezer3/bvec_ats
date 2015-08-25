namespace Plugin
{
    partial class Blowoff
    {
        internal enum BlowoffStates
        {
            /// <summary>Set this state when no processing or action is to be taken. The numerical value of this constant is 0.</summary>
            None = 0,
            /// <summary>The boiler is over maximum pressure, but the blowoff has not yet triggered. The numerical value of this constant is 1.</summary>
            OverMaxPressure = 1,
            /// <summary>The boiler is blowing off excess pressure. The numerical value of this constant is 1.</summary>
            Blowoff = 2,
        }
    }
}
