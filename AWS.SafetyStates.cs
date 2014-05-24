namespace Plugin {

	/// <summary>Represents an Automatic Warning System.</summary>
	internal partial class AWS: Device {
		/// <summary>Possible non-visual warning states of the Automatic Warning System.</summary>
		internal enum SafetyStates {
			/// <summary>Set this state when no processing or action is to be taken. The numerical value of this constant is 0.</summary>
			None = 0,
			/// <summary>The Automatic Warning System has been primed by a magnetic south pole detection, and the delay period is active. The numerical value of this constant is 1.</summary>
			Primed = 1,
			/// <summary>The Automatic Warning System reports a clear signal. The numerical value of this constant is 2.</summary>
			Clear = 2,
			/// <summary>The Automatic Warning System issued a warning, the cancellation timer has been triggered, and is counting down.
			/// The numerical value of this constant is 3.</summary>
			CancelTimerActive = 3,
			/// <summary>The cancellation timer expired and the safety system is intervening. The numerical value of this constant is 4.</summary>
			CancelTimerExpired = 4,
			/// <summary>The Automatic Warning System warning horn was acknowledged in time. The numerical value of this constant is 5.</summary>
			WarningAcknowledged = 5,
			/// <summary>An AWS initiated TPWS Brake Demand has been issued. The numerical value of this constant is 6.</summary>
			TPWSAWSBrakeDemandIssued = 6,
			/// <summary>A TPWS TSS Brake Demand has been issued. The numerical value of this constant is 7.</summary>
			TPWSTssBrakeDemandIssued = 7,
			/// <summary>The Automatic Warning System is in self-test mode. The numerical value of this constant is 8.</summary>
			SelfTest = 8,
			/// <summary>The Automatic Warning System is in self-test mode, and has issued an AWS warning. The numerical value of this constant is 9.</summary>
			/// <remarks>Unlike the CancelTimerActive state, there is no time-limit associated with this state.</remarks>
			SelfTestWarning = 9,
			/// <summary>The Automatic Warning System has been isolated. The numerical value of this constant is 10.</summary>
			Isolated = 10
		}
	}
}