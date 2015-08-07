using OpenBveApi.Runtime;

namespace Plugin {
	/// <summary>Represents an abstract device.</summary>
	internal abstract class Device {
		
		/// <summary>Is called when the device should initialize.</summary>
		/// <param name="mode">The initialization mode.</param>
		internal virtual void Initialize(InitializationModes mode) { }

		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data.</param>
		/// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
		internal virtual void Elapse(ElapseData data, ref bool blocking) { }


        /// <summary>The current operative state of the system.</summary>
        /// <remarks>Use this member for determining failure modes.</remarks>
        protected OperativeStates MyOperativeState;


        /// <summary>Whether or not the system has had its functionality enabled.</summary>
        /// <remarks>Set this to true if a train is to be equipped with a particular system.</remarks>
        protected bool MyEnabled;


        
        // properties

        /// <summary>Gets the current operative state of the system.</summary>
        internal OperativeStates OperativeState
        {
            get { return this.MyOperativeState; }
        }

        


		/// <summary>Is called when the driver changes the reverser.</summary>
		/// <param name="reverser">The new reverser position.</param>
		internal virtual void SetReverser(int reverser) { }
		
		/// <summary>Is called when the driver changes the power notch.</summary>
		/// <param name="powerNotch">The new power notch.</param>
		internal virtual void SetPower(int powerNotch) { }
		
		/// <summary>Is called when the driver changes the brake notch.</summary>
		/// <param name="brakeNotch">The new brake notch.</param>
		internal virtual void SetBrake(int brakeNotch) { }
		
		/// <summary>Is called when a key is pressed.</summary>
		/// <param name="key">The key.</param>
		internal virtual void KeyDown(VirtualKeys key) { }
		
		/// <summary>Is called when a key is released.</summary>
		/// <param name="key">The key.</param>
		internal virtual void KeyUp(VirtualKeys key) { }
		
		/// <summary>Is called when the state of the doors changes.</summary>
		/// <param name="oldState">The old state of the doors.</param>
		/// <param name="newState">The new state of the doors.</param>
		internal virtual void DoorChange(DoorStates oldState, DoorStates newState) { }
		
		/// <summary>Is called when a horn is played or when the music horn is stopped.</summary>
		/// <param name="type">The type of horn.</param>
		internal virtual void HornBlow(HornTypes type) { }
		
		/// <summary>Is called to inform about signals.</summary>
		/// <param name="signal">The signal data.</param>
		internal virtual void SetSignal(SignalData[] signal) { }
		
		/// <summary>Is called when a beacon is passed.</summary>
		/// <param name="beacon">The beacon data.</param>
		internal virtual void SetBeacon(BeaconData beacon) { }

	}
}