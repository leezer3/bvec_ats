namespace Plugin {

	/// <summary>Represents a Train Protection and Warning System.</summary>
	internal partial class TPWS : Device {
		internal enum SafetyStates {
			/// <summary>The Train Protection and Warning System is in a state where no action need be taken. The numerical value of this constant is 0.</summary>
			None = 0,
			/// <summary>The Train Protection and Warning System Overspeed Sensor System (OSS) has been armed, and one or both OSS timers is active. The numerical value of this constant is 1.</summary>
			OssArmed = 1,
			/// <summary>The Train Protection and Warning System Overspeed Sensor System (OSS) has been armed with legacy behaviour. The numerical value of this constant is 2.</summary>
			LegacyOssArmed = 2,
			/// <summary>The Train Protection and Warning System Trainstop Sensor System (TSS) has been armed, and one or both TSS arming detection states is true. The numerical value of this constant is 3.</summary>
			TssArmed = 3,
			/// <summary>A Train Protection and Warning System Brake Demand has been issued. The numerical value of this constant is 4.</summary>
			TssBrakeDemand = 4,
			/// <summary>The Train Protection and Warning System Brake Demand indication has been acknowledged by the driver. The numerical value of this constant is 5.</summary>
			BrakeDemandAcknowledged = 5,
			/// <summary>The train has been brought to a stand due a Train Protection and Warning System Brake Demand, this has been acknowledged,
			/// and now the timeout period is expiring before the brakes can be released again. The numerical value of this constant is 6.</summary>
			BrakesAppliedCountingDown = 6,
			/// <summary>The Train Protection and Warning System Temporary Override is active. The numerical value of this constant is 7.</summary>
			TemporaryOverride = 7,
			/// <summary>The Train Protection and Warning System has been isolated (along with the Automatic Warning System, and Vigilance Device).
			/// The numerical value of this constant is 8.</summary>
			Isolated = 8,
			/// <summary>The Train Protection and Warning System is in self-test mode. The numerical value of this constant is 9.</summary>
			SelfTest = 9
		}
	}
}