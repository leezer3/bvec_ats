namespace Plugin
{
    partial class Blowoff
    {
        /// <summary>Gets the state of the blowoff.</summary>
        internal BlowoffStates BlowoffState { get; set; }
        /// <summary>The panel index of the blowoff</summary>
        public int PanelIndex = -1;
        /// <summary>The sound index played when the blowoff triggers</summary>
        public int SoundIndex = -1;
        /// <summary>Stores whether the toggle / play once sound has played</summary>
        public bool Played = false;
        /// <summary>Stores the timer for this component</summary>
        public double Timer = 0.0;
        /// <summary>The time in seconds taken for the blowoff to go from maximum pressure to minimum pressure</summary>
        internal double BlowoffTime = 0;
        /// <summary>The calculated pressure drop per second whilst the blowoff is active</summary>
        public double BlowoffRate = 0;
        /// <summary>The pressure at which the boiler blowoff will operate</summary>
        internal double TriggerPressure = 21000;
    }
}