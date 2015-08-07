using System;
using OpenBveApi.Runtime;

namespace Plugin {
	/// <summary>Represents the TASC device.</summary>
	internal class Tasc : Device {
		
		// --- enumerations ---
		
		internal enum States {
			/// <summary>The system is operating normally.</summary>
			Normal = 1,
			/// <summary>The system has received a pattern.</summary>
			Pattern = 2,
			/// <summary>The system is released after having stopped at a previous stop point.</summary>
			Released = 3
		}
		
		
		// --- members ---
		
		/// <summary>The underlying train.</summary>
		private Train Train;
		
		/// <summary>The current state of the system.</summary>
		internal States State;
		
		/// <summary>Whether to override the system and prevent it from braking the train at stations.</summary>
		internal bool Override;
		
		/// <summary>The distance to the next stop point.</summary>
		private double Distance;
		
		/// <summary>The currently selected brake notch.</summary>
		private int BrakeNotch;

		/// <summary>Whether the station has home doors.</summary>
		private bool HomeDoors;
		
		
		// --- parameters --
		
		/// <summary>The design deceleration.</summary>
		internal double DesignDeceleration = 2.445 / 3.6;

		
		// --- constructors ---
		
		/// <summary>Creates a new instance of this system.</summary>
		/// <param name="train">The train.</param>
		internal Tasc(Train train) {
			this.Train = train;
			this.State = States.Released;
			this.Distance = 0.0;
			this.BrakeNotch = 0;
			this.HomeDoors = false;
		}
		
		
		// --- inherited functions ---
		
		/// <summary>Is called when the system should initialize.</summary>
		/// <param name="mode">The initialization mode.</param>
		internal override void Initialize(InitializationModes mode) {
			this.State = States.Released;
			this.Distance = 0.0;
			this.BrakeNotch = 0;
			this.HomeDoors = false;
		}

		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data.</param>
		/// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
		internal override void Elapse(ElapseData data, ref bool blocking) {
			// --- behavior ---
			if (this.State == States.Pattern | this.State == States.Released) {
				this.Distance -= data.Vehicle.Speed.MetersPerSecond * data.ElapsedTime.Seconds;
			}
			if (this.State == States.Pattern) {
				
//					double pattern = Math.Sqrt(2.0 * this.DesignDeceleration * this.Distance);
//					double overspeed = data.Vehicle.Speed.MetersPerSecond - pattern;
//					int mediumNotch = this.Train.Specs.B67Notch;
//					int notch;
//					if (overspeed < 0.0) {
//						const double factor = 20.0;
//						notch = mediumNotch + (int)Math.Floor(factor * overspeed);
//					} else {
//						const double factor = 20.0;
//						notch = mediumNotch + (int)Math.Floor(factor * overspeed);
//					}
//					if (notch < this.Train.Specs.AtsNotch) {
//						notch = 0;
//					} else if (notch > this.Train.Specs.BrakeNotches) {
//						notch = this.Train.Specs.BrakeNotches;
//					}
//					if (data.Handles.BrakeNotch < notch) {
//						data.Handles.BrakeNotch = notch;
//					}
//					this.BrakeNotch = notch;
				
				if (this.Train.Doors == DoorStates.None) {
					// --- doors closed ---
					const double maximumDeceleration = 4.2 / 3.6;
					const double delay = 0.5;
					const double exagerationFactor = 3.0;
					double offset = data.Vehicle.Speed.MetersPerSecond > 15.0 / 3.6 ? 3.0 : 0.0;
					int maximumBrakeNotch = this.Train.Specs.BrakeNotches - this.Train.Specs.AtsNotch + 1;
					int designBrakeNotch = (int)Math.Round(this.DesignDeceleration / maximumDeceleration * (double)maximumBrakeNotch);
					double denominator = 2.0 * (this.Distance - offset - delay * data.Vehicle.Speed.MetersPerSecond);
					if (denominator > 0.0) {
						// --- before the stop point ---
						double deceleration = data.Vehicle.Speed.MetersPerSecond * data.Vehicle.Speed.MetersPerSecond / denominator;
						double requiredBrakeNotch = deceleration / maximumDeceleration * (double)maximumBrakeNotch;
						requiredBrakeNotch = designBrakeNotch + (requiredBrakeNotch - designBrakeNotch) * exagerationFactor;
						int minimum;
						int maximum;
						if (requiredBrakeNotch < designBrakeNotch) {
							minimum = (int)Math.Floor(requiredBrakeNotch - 0.5);
							maximum = (int)Math.Floor(requiredBrakeNotch + 0.5);
						} else {
							minimum = (int)Math.Floor(requiredBrakeNotch + 0.5);
							maximum = (int)Math.Floor(requiredBrakeNotch + 1.5);
						}
						if (data.Vehicle.Speed.MetersPerSecond < 1.0 / 3.6 & this.Distance < 1.0) {
							this.BrakeNotch = maximumBrakeNotch;
						} else if (data.Vehicle.Speed.MetersPerSecond < 13.0 / 3.6 | data.Vehicle.Speed.MetersPerSecond > 15.0 / 3.6) {
							if (this.BrakeNotch < minimum) {
								this.BrakeNotch = minimum;
							} else if (this.BrakeNotch > maximum) {
								this.BrakeNotch = maximum;
							}
						} else {
							this.BrakeNotch = designBrakeNotch;
						}
						if (!this.Override) {
							if (this.BrakeNotch > maximumBrakeNotch) {
								if (data.Handles.BrakeNotch < this.Train.Specs.BrakeNotches) {
									Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches);
								}
							} else if (this.BrakeNotch > 0) {
								if (data.Handles.BrakeNotch < this.BrakeNotch + this.Train.Specs.AtsNotch - 1) {
									Train.tractionmanager.demandbrakeapplication(this.BrakeNotch + this.Train.Specs.AtsNotch - 1);
								}
							} else if (requiredBrakeNotch > 0.0) {
								Train.tractionmanager.demandpowercutoff();
							}
						}
						data.DebugMessage = "TASC -- " + requiredBrakeNotch.ToString("0.0") + " @ " + this.Distance.ToString("0.00");
					} else if (!this.Override) {
						// --- after the stop point ---
						if (data.Handles.BrakeNotch < this.Train.Specs.BrakeNotches) {
                            Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches);
						}
						data.DebugMessage = "TASC -- OVERRUN";
					}
				} else if (!this.Override) {
					// --- doors opened ---
					if (data.Handles.BrakeNotch < this.Train.Specs.BrakeNotches) {
                        Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches);
					}
					data.DebugMessage = "TASC -- DOORBLOCK";
				}
				
				
			} else if (this.State == States.Released) {
				if (Math.Abs(this.Distance) > 1.0) {
					this.State = States.Normal;
					this.BrakeNotch = 0;
					this.Distance = 0.0;
					this.HomeDoors = false;
				}
				data.DebugMessage = "TASC -- RELEASED";
			}
			// --- panel ---
			if (this.Override) {
				this.Train.Panel[83] = 1;
			}
			this.Train.Panel[56] = 1;
			this.Train.Panel[80] = 1;
			if (this.State == States.Pattern) {
				this.Train.Panel[81] = 1;
			}
			if (this.BrakeNotch > 0) {
				this.Train.Panel[82] = 1;
			}
			if (this.Distance >= -0.35 & this.Distance <= 0.35) {
				this.Train.Panel[85] = 1;
			}
			if (this.Train.Doors == DoorStates.None) {
				this.Train.Panel[86] = 1;
			}
			if (!this.HomeDoors | this.Distance < -0.35 | this.Distance > 0.35 | this.Train.Doors == DoorStates.None) {
				this.Train.Panel[87] = 1;
			}
			this.Train.Panel[90] = this.BrakeNotch + this.Train.Specs.AtsNotch - 1;
		}
		
		/// <summary>Is called when a key is pressed.</summary>
		/// <param name="key">The key.</param>
		internal override void KeyDown(VirtualKeys key) {
			switch (key) {
				case VirtualKeys.I:
					this.Override = !this.Override;
					break;
			}
		}
		
		/// <summary>Is called when the state of the doors changes.</summary>
		/// <param name="oldState">The old state of the doors.</param>
		/// <param name="newState">The new state of the doors.</param>
		internal override void DoorChange(DoorStates oldState, DoorStates newState) {
			if (oldState != DoorStates.None & newState == DoorStates.None) {
				if (this.State == States.Pattern) {
					this.State = States.Released;
				}
			}
		}
		
		/// <summary>Is called when a beacon is passed.</summary>
		/// <param name="beacon">The beacon data.</param>
		internal override void SetBeacon(BeaconData beacon) {
			switch (beacon.Type) {
				case 30:
					// --- TASC pattern ---
					int distance = beacon.Optional / 1000;
					this.Distance = (double)distance;
					if (this.State == States.Normal) {
						this.State = States.Pattern;
					}
					break;
				case 31:
					// --- TASC home doors ---
					this.HomeDoors = true;
					break;
				case 32:
					// --- TASC pattern ---
					int minimumCars = beacon.Optional / 1000000;
					int maximumCars = (beacon.Optional / 10000) % 100;
					if (this.Train.Specs.Cars >= minimumCars & this.Train.Specs.Cars <= maximumCars | minimumCars == 0 & maximumCars == 0) {
						this.Distance = beacon.Optional % 10000;
						if (this.State == States.Normal) {
							this.State = States.Pattern;
						}
					}
					break;
			}
		}
		
	}
}