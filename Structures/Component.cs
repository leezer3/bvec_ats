namespace Plugin
{
    public class Component
    {
        public int PanelIndex = -1;
        /// <summary>A sound index played to be looped</summary>
        public int LoopSound = -1;
        /// <summary>A sound index to be played once (e.g. Toggled on / off)</summary>
        public int PlayOnceSound = -1;
        /// <summary>Stores whether the toggle / play once sound has played</summary>
        public bool TogglePlayed = false; 
    }
    
}
