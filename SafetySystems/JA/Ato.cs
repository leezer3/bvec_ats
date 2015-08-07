using System;
using OpenBveApi.Runtime;

namespace Plugin {
	/// <summary>Represents ATO.</summary>
	internal class Ato : Device {
		
		// --- enumerations ---
		
		/// <summary>Represents different states of ATO.</summary>
		internal enum States {
			/// <summary>The system is disabled.</summary>
			Disabled = 0,
			/// <summary>The system is currently idle.</summary>
			Idle = 1,
			/// <summary>The system is currently applying power.</summary>
			Power = 2
		}
		
		
		// --- members ---

		/// <summary>The underlying train.</summary>
		private Train Train;
		
		/// <summary>The current state of ATO.</summary>
		private States State;
		
		/// <summary>The countdown before ATO raises the power notch.</summary>
		private double Countdown;
		
		/// <summary>The current power notch ATO applies.</summary>
		private int Notch;
		
		
		// --- parameters ---
		
		/// <summary>Whether the system automatically deactivates once the doors are open.</summary>
		private readonly bool AutomaticallyDeactivates = true;
		
		/// <summary>The time it takes ATO to go from power notch zero to the maximum power notch.</summary>
		private readonly double PowerApplicationTime = 1.0;
		
		
		// --- constructors ---
		
		/// <summary>Creates a new instance of this system.</summary>
		/// <param name="train">The train.</param>
		internal Ato(Train train) {
			this.Train = train;
			this.State = States.Disabled;
			this.Countdown = 0.0;
			this.Notch = 0;
		}
		
		
		// --- functions ---
		
		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data.</param>
		/// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
		internal override void Elapse(ElapseData data, ref bool blocking) {
			// --- behavior ---
			if (this.Train.Doors != DoorStates.None & this.AutomaticallyDeactivates) {
				this.State = States.Disabled;
			}
			if (this.State != States.Disabled) {
				if (this.Train.Atc != null && (this.Train.Atc.State == Atc.States.Normal | this.Train.Atc.State == Atc.States.Service | this.Train.Atc.State == Atc.States.Emergency)) {
					if (this.Train.Atc.State == Atc.States.Normal & data.Handles.Reverser == 1 & data.Handles.BrakeNotch == 0) {
						double speed = this.Train.State.Speed.MetersPerSecond;
						double limit = this.Train.Atc.Pattern.CurrentSpeed;
						if (this.Train.Tasc != null && this.Train.Tasc.State == Tasc.States.Pattern && !this.Train.Tasc.Override) {
							const double tascLimit = 25.0 / 3.6;
							if (tascLimit < limit) {
								limit = tascLimit;
							}
						}
						double minimumSpeed = limit - 15.0 / 3.6;
						double maximumSpeed = limit - 5.0 / 3.6;
						if (speed <= minimumSpeed) {
							this.State = States.Power;
						} else if (speed >= maximumSpeed) {
							this.State = States.Idle;
						}
						if (this.State == States.Power) {
							const double threshold = 5.0 / 3.6;
							int notch = (int)Math.Ceiling((maximumSpeed - speed) / threshold * (double)this.Train.Specs.PowerNotches);
							if (notch < 1) {
								notch = 1;
							} else if (notch > this.Train.Specs.PowerNotches) {
								notch = this.Train.Specs.PowerNotches;
							}
							if (this.Notch < notch) {
								if (this.Countdown <= 0.0) {
									this.Notch++;
									this.Countdown = this.PowerApplicationTime / (double)this.Train.Specs.PowerNotches;
								}
							} else {
								this.Notch = notch;
							}
							data.Handles.PowerNotch = this.Notch;
						} else {
							this.Notch = 0;
							data.Handles.PowerNotch = 0;
						}
					} else {
						this.Notch = 0;
						data.Handles.PowerNotch = 0;
					}
				} else {
					this.Notch = 0;
					data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches + 1;
				}
			} else {
				this.Notch = 0;
			}
			if (this.Countdown > 0.0) {
				this.Countdown -= data.ElapsedTime.Seconds;
			}
			// --- panel ---
			if (this.State != States.Disabled) {
				this.Train.Panel[91] = 1;
				if (this.Train.Atc == null || (this.Train.Atc.State != Atc.States.Normal & this.Train.Atc.State != Atc.States.Service & this.Train.Atc.State != Atc.States.Emergency)) {
					this.Train.Panel[92] = 1;
				}
			}
		}
		
		/// <summary>Is called when a key is pressed.</summary>
		/// <param name="key">The key.</param>
		internal override void KeyDown(VirtualKeys key) {
			if (key == VirtualKeys.J) {
				if (this.State == States.Disabled) {
					this.State = States.Idle;
				} else {
					this.State = States.Disabled;
				}
			}
		}
		
	}
}