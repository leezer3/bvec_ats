namespace Plugin
{
    public class Component
    {
		/// <summary>A reference to the base train</summary>
		internal Train Train;
        public int PanelIndex = -1;
        /// <summary>A sound index played to be looped</summary>
        public int LoopSound = -1;
        /// <summary>A sound index to be played once (e.g. Toggled on / off)</summary>
        public int PlayOnceSound = -1;
        /// <summary>Stores whether the toggle / play once sound has played</summary>
        public bool TogglePlayed = false;
        /// <summary>Stores the timer for this component</summary>
        public double Timer = 0.0;
		/// <summary>The failure chance, per frame</summary>
		internal double FailureChance = 0;

	    public bool Active;
	}
    
}
