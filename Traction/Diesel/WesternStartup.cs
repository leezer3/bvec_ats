namespace Plugin
{

    /// <summary>Manages the startup and self-test sequence.</summary>
    internal partial class WesternStartupManager
    {
        /// <summary>Gets the state of the startup self-test sequence.</summary>
        internal SequenceStates StartupState { get; set; }
    }
}