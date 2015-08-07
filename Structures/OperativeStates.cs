namespace Plugin {
	/// <summary>Possible system operational or failure states.</summary>
	internal enum OperativeStates {
		/// <summary>The system is operating normally. The numerical value of this constant is 0.</summary>
		Normal = 0,
		/// <summary>The system has malfunctioned and may be behaving erratically or sporadically. The numerical value of this constant is 1.</summary>
		Malfunction = 1,
		/// <summary>The system has completely failed, and cannot be recovered. The numerical value of this constant is 2.</summary>
		Failed = 2
	}
}