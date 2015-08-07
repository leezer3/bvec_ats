using System;
using OpenBveApi.Runtime;

namespace Plugin {
	/// <summary>Represents ATS-Sx.</summary>
	internal class AtsSx : Device {
		
		// --- enumerations ---
		
		/// <summary>Represents different states of ATS-Sx.</summary>
		internal enum States {
			/// <summary>The system is disabled.</summary>
			Disabled = 0,
			/// <summary>The system is enabled, but currently suppressed. This will change to States.Initializing once the emergency brakes are released.</summary>
			Suppressed = 1,
			/// <summary>The system is initializing. This will change to States.Chime once the initialization is complete.</summary>
			Initializing = 2,
			/// <summary>The system is enabled but blocked by another system.</summary>
			Blocked = 3,
			/// <summary>The chime is ringing.</summary>
			Chime = 4,
			/// <summary>The system is operating normally.</summary>
			Normal = 5,
			/// <summary>The alarm is ringing. This will change to States.Emergency once the countdown runs out.</summary>
			Alarm = 6,
			/// <summary>The system applies the emergency brakes.</summary>
			Emergency = 7
		}
		
		
		// --- members ---
		
		/// <summary>The underlying train.</summary>
		private Train Train;
		
		/// <summary>The current state of the system.</summary>
		internal States State;
		
		/// <summary>The current alarm countdown.</summary>
		/// <remarks>With States.Initializing, this counts down until the initialization is complete.</remarks>
		/// <remarks>With States.Alarm, this counts down until the emergency brakes are engaged.</remarks>
		private double AlarmCountdown;
		
		/// <summary>The current speed check countdown.</summary>
		private double SpeedCheckCountdown;
		
		/// <summary>The time that elapsed since the last SN/P-style beacon 60 was encountered.</summary>
		private double CompatibilityAccidentalDepartureCounter;
		
		/// <summary>The distance traveled since the last IIYAMA-style Ps2 beacon.</summary>
		private double CompatibilityDistanceAccumulator;
		
		/// <summary>The location of the farthest known red signal, or System.Double.MinValue.</summary>
		internal double RedSignalLocation;
		
		
		// --- parameters ---
		
		/// <summary>The duration of the alarm until the emergency brakes are applied.</summary>
		internal double DurationOfAlarm = 5.0;
		
		/// <summary>The duration of the initialization process.</summary>
		internal double DurationOfInitialization = 3.0;
		
		/// <summary>The duration of the Sx speed check.</summary>
		internal double DurationOfSpeedCheck = 0.5;
		
		
		// --- constructors ---
		
		/// <summary>Creates a new instance of this system with default parameters.</summary>
		/// <param name="train">The train.</param>
		internal AtsSx(Train train) {
			this.Train = train;
			this.State = States.Disabled;
			this.AlarmCountdown = 0.0;
			this.SpeedCheckCountdown = 0.0;
			this.RedSignalLocation = 0.0;
		}
		
		
		// --- inherited functions ---
		
		/// <summary>Is called when the system should initialize.</summary>
		/// <param name="mode">The initialization mode.</param>
		internal override void Initialize(InitializationModes mode) {
			if (mode == InitializationModes.OffEmergency) {
				this.State = States.Suppressed;
			} else {
				this.State = States.Normal;
			}
		}

		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data.</param>
		/// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
		internal override void Elapse(ElapseData data, ref bool blocking) {
			// --- behavior ---
			if (this.State == States.Suppressed) {
				if (data.Handles.BrakeNotch <= this.Train.Specs.BrakeNotches) {
					this.AlarmCountdown = DurationOfInitialization;
					this.State = States.Initializing;
				}
			}
			if (this.State == States.Initializing) {
				this.Train.Sounds.AtsBell.Play();
				this.AlarmCountdown -= data.ElapsedTime.Seconds;
				if (this.AlarmCountdown <= 0.0) {
					this.State = States.Chime;
				}
			}
			if (blocking) {
				if (this.State != States.Disabled & this.State != States.Suppressed) {
					this.State = States.Blocked;
				}
			} else {
				if (this.State == States.Blocked) {
					this.State = States.Chime;
				}
				if (this.State == States.Chime) {
					this.Train.Sounds.AtsChime.Play();
				} else if (this.State == States.Alarm) {
					this.Train.Sounds.AtsBell.Play();
					this.AlarmCountdown -= data.ElapsedTime.Seconds;
					if (this.AlarmCountdown <= 0.0) {
						this.State = States.Emergency;
					}
				} else if (this.State == States.Emergency) {
					this.Train.Sounds.AtsBell.Play();
					Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches+1);
				}
				if (this.SpeedCheckCountdown > 0.0 & data.ElapsedTime.Seconds > 0.0) {
					this.SpeedCheckCountdown -= data.ElapsedTime.Seconds;
				}
				if (this.CompatibilityDistanceAccumulator != 0.0) {
					this.CompatibilityDistanceAccumulator += data.Vehicle.Speed.MetersPerSecond * data.ElapsedTime.Seconds;
					if (this.CompatibilityDistanceAccumulator > 27.7) {
						this.CompatibilityDistanceAccumulator = 0.0;
					}
				}
				if (this.State != States.Disabled & this.Train.Doors != DoorStates.None) {
					Train.tractionmanager.demandpowercutoff();
				}
				else
				{
				    Train.tractionmanager.resetpowercutoff();
				}
			}
			this.CompatibilityAccidentalDepartureCounter += data.ElapsedTime.Seconds;
			// --- panel ---
			if (this.State == States.Chime | this.State == States.Normal) {
				this.Train.Panel[0] = 1;
				this.Train.Panel[256] = 1;
			}
			if (this.State == States.Initializing | this.State == States.Alarm) {
				this.Train.Panel[1] = 1;
				this.Train.Panel[257] = 1;
				this.Train.Panel[258] = 1;
			} else if (this.State == States.Emergency) {
				int value = (int)data.TotalTime.Milliseconds % 1000 < 500 ? 1 : 0;
				this.Train.Panel[1] = value;
				this.Train.Panel[257] = 2;
				this.Train.Panel[258] = value;
			}
		}

		/// <summary>Is called when a key is pressed.</summary>
		/// <param name="key">The key.</param>
		internal override void KeyDown(VirtualKeys key) {
			switch (key) {
				case VirtualKeys.S:
					// --- acknowledge the alarm ---
					if (this.State == States.Alarm & this.Train.Handles.PowerNotch == 0 & this.Train.Handles.BrakeNotch >= this.Train.Specs.AtsNotch) {
						this.State = States.Chime;
					}
					break;
				case VirtualKeys.A1:
					// --- stop the chime ---
					if (this.State == States.Chime) {
						this.State = States.Normal;
					}
					break;
				case VirtualKeys.B1:
					// --- reset the system ---
					if (this.State == States.Emergency & this.Train.Handles.Reverser == 0 & this.Train.Handles.PowerNotch == 0 & this.Train.Handles.BrakeNotch == this.Train.Specs.BrakeNotches + 1) {
						this.State = States.Chime;
					}
					break;
				case VirtualKeys.D:
					// --- activate or deactivate the system ---
					if (this.State == States.Disabled) {
						this.State = States.Suppressed;
					} else {
						this.State = States.Disabled;
					}
					break;
			}
		}
		
		/// <summary>Is called to inform about signals.</summary>
		/// <param name="signal">The signal data.</param>
		internal override void SetSignal(SignalData[] signal) {
			if (this.RedSignalLocation != double.MinValue) {
				for (int i = 0; i < signal.Length; i++) {
					const double visibility = 50.0;
					if (signal[i].Distance < visibility) {
						double location = this.Train.State.Location + signal[i].Distance;
						if (Math.Abs(location - this.RedSignalLocation) < 50.0) {
							if (signal[i].Aspect != 0) {
								this.RedSignalLocation = double.MinValue;
							}
						}
					}
				}
			}
		}
		
		/// <summary>Is called when a beacon is passed.</summary>
		/// <param name="beacon">The beacon data.</param>
		internal override void SetBeacon(BeaconData beacon) {
			if (this.State != States.Disabled & this.State != States.Initializing) {
				switch (beacon.Type) {
					case 0:
						// --- Sx long ---
						if (beacon.Signal.Aspect == 0 | beacon.Signal.Aspect > 100) {
							if (this.State == States.Chime | this.State == States.Normal) {
								this.AlarmCountdown = DurationOfAlarm;
								this.State = States.Alarm;
								UpdateRedSignalLocation(beacon);
							}
						}
						break;
					case 1:
						// --- Sx immediate stop ---
						if (beacon.Signal.Aspect == 0 | beacon.Signal.Aspect > 100) {
							if (this.State == States.Chime | this.State == States.Normal | this.State == States.Alarm) {
								this.State = States.Emergency;
							}
						}
						break;
					case 2:
						// --- accidental departure ---
						if ((beacon.Signal.Aspect == 0 | beacon.Signal.Aspect > 100) & (beacon.Optional == 0 | beacon.Optional >= this.Train.Specs.Cars)) {
							if (this.State == States.Chime | this.State == States.Normal | this.State == States.Alarm) {
								this.State = States.Emergency;
							}
						}
						break;
					case 9:
						// --- P upcoming curve limit / Sx speed check ---
						int distance = beacon.Optional / 1000;
						int speed = beacon.Optional % 1000;
						if (distance == 0) {
							// --- Sx speed check ---
							if (this.State == States.Chime | this.State == States.Normal | this.State == States.Alarm) {
								if (Math.Abs(this.Train.State.Speed.KilometersPerHour) > speed) {
									this.State = States.Emergency;
								}
							}
						}
						break;
					case 12:
						// --- Ps second pattern / Sx speed check ---
						if (this.CompatibilityDistanceAccumulator == 0.0) {
							this.CompatibilityDistanceAccumulator = double.Epsilon;
						} else {
							if (beacon.Signal.Aspect == 0 | beacon.Signal.Aspect > 100) {
								if (this.State == States.Chime | this.State == States.Normal | this.State == States.Alarm) {
									if (Math.Abs(this.Train.State.Speed.KilometersPerHour) > beacon.Optional) {
										this.State = States.Emergency;
									}
								}
							}
							this.CompatibilityDistanceAccumulator = 0.0;
						}
						break;
					case 50:
						// --- Sx speed check, immediate stop type, 45 km/h if signal is red ---
						if (beacon.Signal.Aspect == 0 | beacon.Signal.Aspect > 100) {
							if (this.State == States.Chime | this.State == States.Normal | this.State == States.Alarm) {
								if (Math.Abs(this.Train.State.Speed.KilometersPerHour) > 45) {
									this.State = States.Emergency;
								}
							}
						}
						break;
					case 55:
						// --- Sx speed check, immediate stop type ---
						if (this.State == States.Chime | this.State == States.Normal | this.State == States.Alarm) {
							if (Math.Abs(this.Train.State.Speed.KilometersPerHour) > beacon.Optional) {
								this.State = States.Emergency;
							}
						}
						break;
					case 56:
						// --- Sx speed check, alarm ---
						if (this.State == States.Chime | this.State == States.Normal) {
							if (Math.Abs(this.Train.State.Speed.KilometersPerHour) > beacon.Optional) {
								this.AlarmCountdown = DurationOfAlarm;
								this.State = States.Alarm;
							}
						}
						break;
					case 60:
						// --- accidental departure counter reset ---
						this.CompatibilityAccidentalDepartureCounter = 0.0;
						break;
					case 61:
						// --- accidental departure trigger ---
						if (beacon.Signal.Aspect == 0 | beacon.Signal.Aspect > 100) {
							if (this.CompatibilityAccidentalDepartureCounter > 0.001 * (double)beacon.Optional) {
								if (this.State == States.Chime | this.State == States.Normal | this.State == States.Alarm) {
									this.State = States.Emergency;
								}
							}
						}
						break;
					default:
						// --- frequency-based beacons ---
						int frequency = Train.GetFrequencyFromBeacon(beacon);
						switch (frequency) {
							case 108:
								// --- Sx speed check (108.5 KHz) ---
								if (this.State == States.Chime | this.State == States.Normal | this.State == States.Alarm) {
									if (this.SpeedCheckCountdown <= 0.0) {
										this.SpeedCheckCountdown = DurationOfSpeedCheck;
									} else {
										this.SpeedCheckCountdown = 0.0;
										this.State = States.Emergency;
									}
								}
								break;
							case 123:
								// --- SN immediate stop (123 KHz) ---
								if (this.State == States.Chime | this.State == States.Normal | this.State == States.Alarm) {
									this.State = States.Emergency;
								}
								break;
							case 129:
							case 130:
								// --- S long (129.3 KHz / 130 KHz) ---
								if (this.State == States.Chime | this.State == States.Normal) {
									this.AlarmCountdown = DurationOfAlarm;
									this.State = States.Alarm;
									UpdateRedSignalLocation(beacon);
								}
								break;
						}
						break;
				}
			}
		}
		
		
		// --- private functions ---
		
		/// <summary>Updates the location of the farthest known red signal from the specified beacon.</summary>
		/// <param name="beacon">The beacon that holds the distance to a known red signal.</param>
		private void UpdateRedSignalLocation(BeaconData beacon) {
			if (beacon.Signal.Distance < 1200.0) {
				double signalLocation = this.Train.State.Location + beacon.Signal.Distance;
				if (signalLocation > this.RedSignalLocation) {
					this.RedSignalLocation = signalLocation;
				}
			}
		}
		
	}
}