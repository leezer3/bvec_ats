namespace Plugin {
	
	/// <summary>Manages the startup and self-test sequence.</summary>
	internal partial class StartupSelfTestManager : Device {
		/// <summary>Possible train startup self test sequence initialisation states.</summary>
		internal enum SequenceStates {
			/// <summary>The startup and self-test procedure has not been initiated yet. The numerical value of this constant is 0.</summary>
			Pending = 0,
			/// <summary>The initial keystrole requried to begin the startup and self-test procedure has been made. The numerical value of this constant is 1.</summary>
			WaitingToStart = 1,
			/// <summary>The train systems are currently undergoing the startup self-test procedure. The numerical value of this constant is 2.</summary>
			Initialising = 2,
			/// <summary>The startup self-test procedure is awaiting the driver's Automatic Warning System acknowledgement. The numerical value of this constant is 3.</summary>
			AwaitingDriverInteraction = 3,
			/// <summary>The startup and self-test procedure is indicating a successful completion of the tests to the driver, before finishing. The numerical value of this constant is 4.</summary>
			Finalising = 4,
			/// <summary>The train systems have successfully completed the startup and self-test procedure and are ready for service. The numerical value of this constant is 5.</summary>
			Initialised = 5,
			/// <summary>The train systems have failed the startup self-test procedure, and the train is not in service. The numerical value of this constant is 6.</summary>
			Failed = 6

		}
	}
}
