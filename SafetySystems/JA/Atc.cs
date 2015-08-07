/*
 * Largely public domain code by Odakyufan
 * Modified to work with BVEC_ATS traction manager
 * 
 */
#pragma warning disable 0660, 0661

using System;
using System.Collections.Generic;
using OpenBveApi.Runtime;

namespace Plugin {
	/// <summary>Represents ATC.</summary>
	internal class Atc : Device {
		
		
		// --- enumerations and structures ---
		
		/// <summary>Represents different states of ATC.</summary>
		internal enum States {
			/// <summary>The system is disabled.</summary>
			Disabled = 0,
			/// <summary>The system is enabled, but currently suppressed. This will change to States.Ats once the emergency brakes are released.</summary>
			Suppressed = 1,
			/// <summary>The system has been set to ATS mode.</summary>
			Ats = 2,
			/// <summary>The system is operating normally.</summary>
			Normal = 3,
			/// <summary>The system is applying the service brakes.</summary>
			Service = 4,
			/// <summary>The system is applying the emergency brakes.</summary>
			Emergency = 5
		}
		
		/// <summary>Represents different states of the compatibility ATC track.</summary>
		private enum CompatibilityStates {
			/// <summary>ATC is not available.</summary>
			Ats = 0,
			/// <summary>ATC is available. The ToAtc reminder plays when the train has come to a stop.</summary>
			ToAtc = 1,
			/// <summary>ATC is available.</summary>
			Atc = 2,
			/// <summary>ATC is available. The ToAts reminder plays when the train has come to a stop.</summary>
			ToAts = 3
		}
		
		/// <summary>Represents different states of the the signal indicator inside the cab.</summary>
		internal enum SignalIndicators {
			/// <summary>The signal shows nothing.</summary>
			None = 0,
			/// <summary>The signal is green.</summary>
			Green = 1,
			/// <summary>The signal is green and the advance warning lamp is lit.</summary>
			Yellow = 2,
			/// <summary>The signal is red.</summary>
			Red = 3,
			/// <summary>The signal is red and the P lamp is lit.</summary>
			P = 4,
			/// <summary>The signal is red, the P lamp is lit, and the train should switch to ATS.</summary>
			Kirikae = 5,
			/// <summary>The signal is red and the X lamp is lit.</summary>
			X = 6
		}
		
		/// <summary>Represents a signal that was received from the track.</summary>
		internal class Signal {
			// --- members ---
			/// <summary>The aspect underlying this signal, or -1 if not relevant.</summary>
			internal int Aspect;
			/// <summary>What the signal indicator should show for this signal.</summary>
			internal SignalIndicators Indicator;
			/// <summary>The initial speed limit at the beginning of the block, or System.Double.MaxValue to carry over the current speed limit.</summary>
			internal double InitialSpeed;
			/// <summary>The final speed limit at the end of the block, or a negative number for an emergency brake application.</summary>
			internal double FinalSpeed;
			/// <summary>The distance to the end of the block, or a non-positive number to indicate that the final speed should apply immediately, or System.Double.MaxValue if the distance to the end of the block is not known.</summary>
			internal double Distance;
			// --- constructors ---
			/// <summary>Creates a new signal.</summary>
			/// <param name="indicator">What the signal indicator should show for this signal.</param>
			/// <param name="speed">The immediate speed limit, or a negative number for an emergency brake application.</param>
			internal Signal(SignalIndicators indicator, double speed) {
				this.Aspect = -1;
				this.Indicator = indicator;
				this.InitialSpeed = speed;
				this.FinalSpeed = speed;
				this.Distance = double.MaxValue;
			}
			/// <summary>Creates a new signal.</summary>
			/// <param name="aspect">The aspect underlying this signal, or -1 if not relevant.</param>
			/// <param name="indicator">What the signal indicator should show for this signal.</param>
			/// <param name="speed">The immediate speed limit, or a negative number for an emergency brake application.</param>
			internal Signal(int aspect, SignalIndicators indicator, double speed) {
				this.Aspect = aspect;
				this.Indicator = indicator;
				this.InitialSpeed = speed;
				this.FinalSpeed = speed;
				this.Distance = double.MaxValue;
			}
			/// <summary>Creates a new signal.</summary>
			/// <param name="aspect">The aspect underlying this signal, or -1 if not relevant.</param>
			/// <param name="indicator">What the signal indicator should show for this signal.</param>
			/// <param name="initialSpeed">The initial speed limit at the beginning of the block, or System.Double.MaxValue to carry over the current speed limit.</param>
			/// <param name="finalSpeed">The final speed limit at the end of the block, or a negative number for an emergency brake application.</param>
			/// <param name="distance">The distance to the end of the block, or a non-positive number to indicate that the final speed should apply immediately, or System.Double.MaxValue if the distance to the end of the block is not known.</param>
			internal Signal(int aspect, SignalIndicators indicator, double initialSpeed, double finalSpeed, double distance) {
				this.Aspect = aspect;
				this.Indicator = indicator;
				this.InitialSpeed = initialSpeed;
				this.FinalSpeed = finalSpeed;
				this.Distance = distance;
			}
		}
		
		/// <summary>Represents a pattern, containing the signal obtained from the route, as well as the current applicable speed limit.</summary>
		internal class SignalPattern {
			// --- members ---
			/// <summary>The current signal.</summary>
			internal Signal Signal;
			/// <summary>The distance remaining until the end of the block, or System.Double.MaxValue if the end of the block is not known.</summary>
			internal double Distance;
			/// <summary>The top speed limit (the current speed limit never exceeds this value).</summary>
			internal double TopSpeed;
			/// <summary>The current speed limit (above which the brakes are engaged).</summary>
			internal double CurrentSpeed;
			/// <summary>The release speed limit (below which the brakes are released).</summary>
			internal double ReleaseSpeed;
			// --- constructors ---
			/// <summary>Creates a new signal pattern, assuming the beginning of the block.</summary>
			internal SignalPattern(Signal signal) {
				this.Signal = signal;
				this.Distance = signal.Distance;
				this.TopSpeed = signal.InitialSpeed;
				this.Update(0.0);
			}
			// --- functions ---
			/// <summary>Updates the signal pattern.</summary>
			/// <param name="distanceTraveled">The distance traveled since the last call to this function.</param>
			internal void Update(double distanceTraveled) {
				if (this.Distance == double.MaxValue) {
					this.CurrentSpeed = this.Signal.InitialSpeed;
				} else {
					this.Distance -= distanceTraveled;
					if (this.Distance > 0.0) {
						const double deceleration = 1.945 / 3.6;
						const double delay = 0.5;
						double sqrtTerm = 2.0 * deceleration * this.Distance + deceleration * deceleration * delay * delay + this.Signal.FinalSpeed * this.Signal.FinalSpeed;
						if (sqrtTerm > 0.0) {
							this.CurrentSpeed = Math.Sqrt(sqrtTerm) - deceleration * delay;
							if (this.CurrentSpeed > this.Signal.InitialSpeed) {
								this.CurrentSpeed = this.Signal.InitialSpeed;
							} else if (this.CurrentSpeed < this.Signal.FinalSpeed) {
								this.CurrentSpeed = this.Signal.FinalSpeed;
							}
						} else {
							this.CurrentSpeed = this.Signal.FinalSpeed;
						}
					} else {
						this.CurrentSpeed = this.Signal.FinalSpeed;
					}
				}
				if (this.CurrentSpeed > this.TopSpeed) {
					this.CurrentSpeed = this.TopSpeed;
				}
				this.ReleaseSpeed = Math.Max(this.Signal.FinalSpeed - 1.0 / 3.6, this.CurrentSpeed - 10.0 / 3.6);
			}
			/// <summary>Checks if two pattern have the same appearance.</summary>
			/// <param name="a">The first pattern.</param>
			/// <param name="b">The second pattern.</param>
			/// <returns>Whether the patterns have the same apperance.</returns>
			internal static bool ApperanceEquals(SignalPattern a, SignalPattern b) {
				if (a.Signal.Indicator != b.Signal.Indicator) return false;
				{
					int an = (int)Math.Ceiling(0.72 * a.Signal.FinalSpeed - 0.001);
					int bn = (int)Math.Ceiling(0.72 * b.Signal.FinalSpeed - 0.001);
					if (an != bn) return false;
				}
				{
					int an = (int)Math.Floor(0.72 * a.CurrentSpeed + 0.001);
					int bn = (int)Math.Floor(0.72 * b.CurrentSpeed + 0.001);
					if (an != bn) return false;
				}
				return true;
			}
		}
		
		/// <summary>Represents a speed limit at a specific track position.</summary>
		private struct CompatibilityLimit {
			// --- members ---
			/// <summary>The speed limit.</summary>
			internal double Limit;
			/// <summary>The track position.</summary>
			internal double Location;
			// --- constructors ---
			/// <summary>Creates a new compatibility limit.</summary>
			/// <param name="limit">The speed limit.</param>
			/// <param name="position">The track position.</param>
			internal CompatibilityLimit(double limit, double location) {
				this.Limit = limit;
				this.Location = location;
			}
		}
		
		
		// --- members ---
		
		/// <summary>The underlying train.</summary>
		private Train Train;
		
		/// <summary>The current state of the system.</summary>
		internal States State;
		
		/// <summary>Whether to switch to ATC in the next Elapse call. This is set by the Initialize call if the train should start in ATC mode. It is necessary to switch in the Elapse call because at the time of the Initialize call, the ATC track status is not yet known.</summary>
		private bool StateSwitch;

		/// <summary>Whether emergency operation is enabled. In emergency operation, the train is allowed to travel at 15 km/h (or a custom value) regardless of the actually permitted speed.</summary>
		internal bool EmergencyOperation;
		
		/// <summary>The current signal aspect.</summary>
		private int Aspect;
		
		/// <summary>The current signal pattern.</summary>
		internal SignalPattern Pattern;
		
		/// <summary>The state of the compatibility ATC track.</summary>
		private CompatibilityStates CompatibilityState;
		
		/// <summary>A list of all ATC speed limits in the route.</summary>
		private List<CompatibilityLimit> CompatibilityLimits;
		
		/// <summary>The element in the CompatibilityLimits list that holds the last encountered speed limit.</summary>
		private int CompatibilityLimitPointer;
		
		/// <summary>The state of the preceding train, or a null reference.</summary>
		private PrecedingVehicleState PrecedingTrain;
		
		
		// --- parameters ---
		
		/// <summary>Whether to automatically switch between ATS and ATC.</summary>
		internal bool AutomaticSwitch = false;
		
		/// <summary>Represents the signal that indicates that ATC is not available.</summary>
		private readonly Signal NoSignal = new Signal(SignalIndicators.X, -1.0);
		
		/// <summary>Represents the signal for the emergency operation mode, or a null reference if emergency operation is not available.</summary>
		internal Signal EmergencyOperationSignal = new Signal(SignalIndicators.None, 15.0 / 3.6);
		
		/// <summary>The signals recognized by this ATC implementation. The Source parameters must not be null references.</summary>
		internal List<Signal> Signals = new List<Signal>();
		
		/// <summary>The compatibility speeds. A value of -1 indicates that ATC is not available.</summary>
		private readonly double[] CompatibilitySpeeds = new double[] {
			-1.0,
			0.0,
			15.0 / 3.6,
			25.0 / 3.6,
			45.0 / 3.6,
			55.0 / 3.6,
			65.0 / 3.6,
			75.0 / 3.6,
			90.0 / 3.6,
			100.0 / 3.6,
			110.0 / 3.6,
			120.0 / 3.6
		};

        /// <summary>The sound index for the ATC Bell.</summary>
	    internal int ATCBell = -1;
        /// <summary>The sound index for the switch to ATS reminder sound.</summary>
        internal int SwitchToATSReminder = -1;
        /// <summary>The sound index for the switch to ATC reminder sound.</summary>
        internal int SwitchToATCReminder = -1;
        /// <summary>The sound index for the switch to ATS sound.</summary>
        internal int SwitchToATS = -1;
        /// <summary>The sound index for the switch to ATC sound.</summary>
        internal int SwitchToATC = -1;
		
		// --- constructors ---
		
		/// <summary>Creates a new instance of this system.</summary>
		/// <param name="train">The train.</param>
		internal Atc(Train train) {
			this.Train = train;
			this.State = States.Disabled;
			this.EmergencyOperation = false;
			this.Aspect = 0;
			this.Pattern = new SignalPattern(this.NoSignal);
			this.CompatibilityState = CompatibilityStates.Ats;
			this.CompatibilityLimits = new List<CompatibilityLimit>();
		}
		
		
		// --- functions ---
		
		/// <summary>Gets the currently applicable signal.</summary>
		/// <param name="oldSignal">The signal previously received.</param>
		/// <returns>The signal.</returns>
		private Signal GetCurrentSignal(Signal oldSignal) {
			if (this.EmergencyOperation) {
				return this.EmergencyOperationSignal;
			} else {
				foreach (Signal signal in this.Signals) {
					if (signal.Aspect == this.Aspect) {
						return signal;
					}
				}
				if (this.CompatibilityState != CompatibilityStates.Ats) {
					double a = GetAtcSpeedFromTrain();
					double b = GetAtcSpeedFromLimit();
					double limit = Math.Min(a, b);
					if (limit > 0.0) {
						if (this.CompatibilityState == CompatibilityStates.ToAts) {
							return new Signal(SignalIndicators.Kirikae, limit);
						} else {
							return new Signal(SignalIndicators.Green, limit);
						}
					} else if (limit == 0.0) {
						return new Signal(SignalIndicators.Red, 0.0);
					}
				}
				return this.NoSignal;
			}
		}
		
		/// <summary>Gets the ATC speed from the distance to the preceding train if operating in compatibility ATC mode.</summary>
		/// <returns>The ATC speed, or -1 if ATC is not available.</returns>
		private double GetAtcSpeedFromTrain() {
			if (this.CompatibilityState != CompatibilityStates.Ats) {
				if (this.PrecedingTrain == null) {
					return this.CompatibilitySpeeds[11];
				} else {
					const double blockLength = 100.0;
					int a = (int)Math.Floor(this.PrecedingTrain.Location / blockLength);
					int b = (int)Math.Floor(this.Train.State.Location / blockLength);
					int blocks = a - b;
					switch (blocks) {
						case 0:
							return this.CompatibilitySpeeds[0];
						case 1:
							return this.CompatibilitySpeeds[1];
						case 2:
							return this.CompatibilitySpeeds[3];
						case 3:
							return this.CompatibilitySpeeds[4];
						case 4:
							return this.CompatibilitySpeeds[5];
						case 5:
							return this.CompatibilitySpeeds[6];
						case 6:
							return this.CompatibilitySpeeds[7];
						case 7:
							return this.CompatibilitySpeeds[8];
						case 8:
							return this.CompatibilitySpeeds[9];
						case 9:
							return this.CompatibilitySpeeds[10];
						default:
							return this.CompatibilitySpeeds[11];
					}
				}
			} else {
				return -1.0;
			}
		}
		
		/// <summary>Gets the ATC speed from the current and upcoming speed limits.</summary>
		/// <returns>The ATC speed, or -1 if ATC is not available.</returns>
		private double GetAtcSpeedFromLimit() {
			if (this.CompatibilityState != CompatibilityStates.Ats) {
				if (this.CompatibilityLimits.Count == 0) {
					return double.MaxValue;
				} else if (this.CompatibilityLimits.Count == 1) {
					return this.CompatibilityLimits[0].Limit;
				} else {
					while (CompatibilityLimitPointer > 0 && this.CompatibilityLimits[CompatibilityLimitPointer].Location > this.Train.State.Location) {
						CompatibilityLimitPointer--;
					}
					while (CompatibilityLimitPointer < this.CompatibilityLimits.Count - 1 && this.CompatibilityLimits[CompatibilityLimitPointer + 1].Location <= this.Train.State.Location) {
						CompatibilityLimitPointer++;
					}
					if (this.CompatibilityLimitPointer == this.CompatibilityLimits.Count - 1) {
						return this.CompatibilityLimits[this.CompatibilityLimitPointer].Limit;
					} else if (this.CompatibilityLimits[this.CompatibilityLimitPointer].Limit <= this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Limit) {
						return this.CompatibilityLimits[this.CompatibilityLimitPointer].Limit;
					} else {
						const double deceleration = 1.910 / 3.6;
						double currentLimit = this.CompatibilityLimits[this.CompatibilityLimitPointer].Limit;
						double upcomingLimit = this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Limit;
						double distance = (currentLimit * currentLimit - upcomingLimit * upcomingLimit) / (2.0 * deceleration);
						if (this.Train.State.Location < this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Location - distance) {
							return this.CompatibilityLimits[this.CompatibilityLimitPointer].Limit;
						} else {
							return this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Limit;
						}
					}
				}
			} else {
				return -1.0;
			}
		}
		
		/// <summary>Whether the driver should switch to ATS. This returns false if already operating in ATS.</summary>
		/// <returns>Whether the driver should switch to ATS.</returns>
		internal bool ShouldSwitchToAts() {
			if (this.State == States.Normal | this.State == States.Service | this.State == States.Emergency) {
				if (this.Pattern.Signal == null || this.Pattern.Signal.Indicator == SignalIndicators.Kirikae) {
					if (Math.Abs(this.Train.State.Speed.MetersPerSecond) < 1.0 / 3.6) {
						return true;
					}
				}
			}
			return false;
		}
		
		/// <summary>Whether the driver should switch to ATC. This returns false if already operating in ATC.</summary>
		/// <returns>Whether the driver should switch to ATC.</returns>
		internal bool ShouldSwitchToAtc() {
			if (this.State == States.Ats) {
				if (this.Pattern.Signal != null && this.Pattern.Signal.Indicator != SignalIndicators.X && this.Pattern.Signal.Indicator != SignalIndicators.Kirikae) {
					if (Math.Abs(this.Train.State.Speed.MetersPerSecond) < 1.0 / 3.6) {
						return true;
					}
				}
			}
			return false;
		}
		
		
		// --- inherited functions ---
		
		/// <summary>Is called when the system should initialize.</summary>
		/// <param name="mode">The initialization mode.</param>
		internal override void Initialize(InitializationModes mode) {
			if (mode == InitializationModes.OffEmergency) {
				this.State = States.Suppressed;
			} else {
				this.State = States.Ats;
			}
		}

		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data.</param>
		/// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
		internal override void Elapse(ElapseData data, ref bool blocking) {
			// --- behavior ---
			this.PrecedingTrain = data.PrecedingVehicle;
			if (this.StateSwitch) {
				this.State = States.Normal;
				this.StateSwitch = false;
			}
			if (this.State == States.Suppressed) {
				if (data.Handles.BrakeNotch <= this.Train.Specs.BrakeNotches) {
					this.State = States.Ats;
				}
			}
			double distanceTraveled = data.Vehicle.Speed.MetersPerSecond * data.ElapsedTime.Seconds;
			if (blocking) {
				if (this.State != States.Disabled & this.State != States.Suppressed) {
					this.State = States.Ats;
				}
				this.Pattern.Signal = this.NoSignal;
				this.Pattern.Update(distanceTraveled);
			} else {
				// --- update pattern ---
				this.Pattern.Update(distanceTraveled);
				Signal newSignal = GetCurrentSignal(this.Pattern.Signal);
				if (newSignal.Aspect != this.Pattern.Signal.Aspect | newSignal.FinalSpeed != this.Pattern.Signal.FinalSpeed) {
					SignalPattern oldPattern = this.Pattern;
					SignalPattern newPattern = new SignalPattern(newSignal);
					if (this.Pattern.CurrentSpeed <= newPattern.Signal.FinalSpeed & newPattern.Signal.Distance != double.MaxValue) {
						newPattern.TopSpeed = Math.Max(15.0 / 3.6, newPattern.Signal.FinalSpeed);
						newPattern.Update(0.0);
					} else if (newPattern.Signal.InitialSpeed == double.MaxValue) {
						newPattern.TopSpeed = Math.Max(15.0 / 3.6, this.Pattern.CurrentSpeed);
						newPattern.Update(0.0);
					}
					this.Pattern = newPattern;
					if (this.State == States.Normal | this.State == States.Service | this.State == States.Emergency) {
						if (!SignalPattern.ApperanceEquals(this.Pattern, oldPattern)) {
                            SoundManager.Play(ATCBell, 1.0, 1.0, false);
						}
					}
				}
				// --- switch states and apply brakes ---
				if (this.State == States.Normal | this.State == States.Service | this.State == States.Emergency) {
					if (this.Pattern.CurrentSpeed < 0.0) {
						if (this.State != States.Emergency) {
							this.State = States.Emergency;
                            SoundManager.Play(ATCBell, 1.0, 1.0, false);
						}
					} else if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) < this.Pattern.ReleaseSpeed) {
						if (this.State != States.Normal) {
							this.State = States.Normal;
                            SoundManager.Play(ATCBell, 1.0, 1.0, false);
						}
					} else if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= this.Pattern.CurrentSpeed) {
						if (this.State != States.Service) {
							this.State = States.Service;
                            SoundManager.Play(ATCBell, 1.0, 1.0, false);
						}
					}
					if (this.State == States.Service) {
						if (data.Handles.BrakeNotch < this.Train.Specs.BrakeNotches) {
							Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches);
						}
					} else if (this.State == States.Emergency) {
                        Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);
					}
					else
					{
					    Train.tractionmanager.resetbrakeapplication();
					}
				}
				if (this.State != States.Disabled & this.Train.Doors != DoorStates.None) {
                    //Demand power cutoff
                    Train.tractionmanager.demandpowercutoff();
				}
				else
				{
				    Train.tractionmanager.resetpowercutoff();
				}
			}
			// --- panel ---
			if (this.State == States.Ats) {
				this.Train.Panel[21] = 1;
				this.Train.Panel[271] = 12;
			} else if (this.State == States.Normal | this.State == States.Service | this.State == States.Emergency) {
				this.Train.Panel[15] = 1;
				this.Train.Panel[265] = 1;
				if (this.Pattern.Signal.Indicator == SignalIndicators.X) {
					this.Train.Panel[22] = 1;
					this.Train.Panel[271] = 0;
				} else {
					if (this.Pattern.Signal.Aspect == 100) {
						this.Train.Panel[24] = 1; // TODO
					} else if (this.Pattern.Signal.Aspect >= 101 & this.Pattern.Signal.Aspect <= 112) {
						this.Train.Panel[this.Pattern.Signal.Aspect - 79] = 1;
					}
					for (int i = 11; i >= 1; i--) {
						if (this.Pattern.Signal.FinalSpeed + 0.001 >= this.CompatibilitySpeeds[i]) {
							this.Train.Panel[271] = i;
							break;
						}
					}
				}
				switch (this.Pattern.Signal.Indicator) {
					case SignalIndicators.Green:
						this.Train.Panel[111] = 1;
						break;
					case SignalIndicators.Yellow:
						this.Train.Panel[111] = 1;
						this.Train.Panel[113] = 1;
						break;
					case SignalIndicators.Red:
						this.Train.Panel[110] = 1;
						break;
					case SignalIndicators.P:
					case SignalIndicators.Kirikae:
						this.Train.Panel[110] = 1;
						this.Train.Panel[112] = 1;
						break;
					case SignalIndicators.X:
						this.Train.Panel[22] = 1;
						break;
				}
				this.Train.Panel[34] = (int)Math.Round(3600.0 * Math.Max(0.0, this.Pattern.CurrentSpeed));
				if (this.Pattern.Signal.Indicator != SignalIndicators.X) {
					int currentIndex = Math.Min(Math.Max(0, (int)Math.Floor(0.72 * this.Pattern.CurrentSpeed + 0.001)), 59);
					int targetIndex = Math.Min(Math.Max(0, (int)Math.Ceiling(0.72 * this.Pattern.Signal.FinalSpeed - 0.001)), 59);
					for (int i = targetIndex; i <= currentIndex; i++) {
						this.Train.Panel[120 + i] = 1;
					}
				}
			}
			if (this.State == States.Service) {
				this.Train.Panel[16] = 1;
				this.Train.Panel[267] = 1;
			} else if (this.State == States.Emergency) {
				this.Train.Panel[17] = 1;
				this.Train.Panel[268] = 1;
			}
			if (this.State != States.Disabled & this.State != States.Suppressed) {
				this.Train.Panel[18] = 1;
				this.Train.Panel[266] = 1;
			}
			if (this.EmergencyOperation) {
				this.Train.Panel[19] = 1;
				this.Train.Panel[52] = 1;
			}
			if (this.State == States.Disabled) {
				this.Train.Panel[20] = 1;
				this.Train.Panel[53] = 1;
			}
			// --- manual or automatic switch ---
			if (ShouldSwitchToAts()) {
				if (this.AutomaticSwitch & Math.Abs(data.Vehicle.Speed.MetersPerSecond) < 1.0 / 3.6) {
					KeyDown(VirtualKeys.C1);
				} else {
                    SoundManager.Play(SwitchToATSReminder, 1.0, 1.0, false);
				}
			} else if (ShouldSwitchToAtc()) {
				if (this.AutomaticSwitch & Math.Abs(data.Vehicle.Speed.MetersPerSecond) < 1.0 / 3.6) {
					KeyDown(VirtualKeys.C2);
				} else {
                    SoundManager.Play(SwitchToATCReminder, 1.0, 1.0, false);
				}
			}
			// --- debug ---
			if (this.State == States.Normal | this.State == States.Service | this.State == States.Emergency) {
				data.DebugMessage = this.State.ToString() + " - A:" + this.Pattern.Signal.Aspect + " F:" + (3.6 * this.Pattern.Signal.FinalSpeed).ToString("0") + " D:" + (this.Pattern.Distance == double.MaxValue ? "inf" : this.Pattern.Distance.ToString("0")) + " T:" + (3.6 * this.Pattern.TopSpeed).ToString("0") + " C:" + (3.6 * this.Pattern.CurrentSpeed).ToString("0");
			}
		}
		
		/// <summary>Is called when the driver changes the reverser.</summary>
		/// <param name="reverser">The new reverser position.</param>
		internal override void SetReverser(int reverser) {
		}
		
		/// <summary>Is called when the driver changes the power notch.</summary>
		/// <param name="powerNotch">The new power notch.</param>
		internal override void SetPower(int powerNotch) {
		}
		
		/// <summary>Is called when the driver changes the brake notch.</summary>
		/// <param name="brakeNotch">The new brake notch.</param>
		internal override void SetBrake(int brakeNotch) {
		}
		
		/// <summary>Is called when a key is pressed.</summary>
		/// <param name="key">The key.</param>
		internal override void KeyDown(VirtualKeys key) {
			switch (key) {
				case VirtualKeys.C1:
					// --- switch to ats ---
					if (this.State == States.Normal | this.State == States.Service | this.State == States.Emergency) {
						this.State = States.Ats;
                        SoundManager.Play(SwitchToATS, 1.0, 1.0, false);
					}
					break;
				case VirtualKeys.C2:
					// --- switch to atc ---
					if (this.State == States.Ats) {
						this.State = States.Normal;
                        SoundManager.Play(SwitchToATC, 1.0, 1.0, false);
					}
					break;
				case VirtualKeys.G:
					// --- activate or deactivate the system ---
					if (this.State == States.Disabled) {
						this.State = States.Suppressed;
					} else {
						this.State = States.Disabled;
					}
					break;
				case VirtualKeys.H:
					// --- enable or disable emergency operation mode ---
					if (this.EmergencyOperationSignal != null) {
						this.EmergencyOperation = !this.EmergencyOperation;
						if (this.State == States.Normal | this.State == States.Service | this.State == States.Emergency) {
                            SoundManager.Play(ATCBell, 1.0, 1.0, false);
						}
					}
					break;
			}
		}
		
		/// <summary>Is called when a key is released.</summary>
		/// <param name="key">The key.</param>
		internal override void KeyUp(VirtualKeys key) {
		}
		
		/// <summary>Is called when a horn is played or when the music horn is stopped.</summary>
		/// <param name="type">The type of horn.</param>
		internal override void HornBlow(HornTypes type) {
		}
		
		/// <summary>Is called to inform about signals.</summary>
		/// <param name="signal">The signal data.</param>
		internal override void SetSignal(SignalData[] signal) {
			this.Aspect = signal[0].Aspect;
		}
		
		/// <summary>Is called when a beacon is passed.</summary>
		/// <param name="beacon">The beacon data.</param>
		internal override void SetBeacon(BeaconData beacon) {
			switch (beacon.Type) {
				case -16777215:
					if (beacon.Optional >= 0 & beacon.Optional <= 3) {
						this.CompatibilityState = (CompatibilityStates)beacon.Optional;
					}
					break;
				case -16777214:
					{
						double limit = (double)(beacon.Optional & 4095) / 3.6;
						double position = (beacon.Optional >> 12);
						CompatibilityLimit item = new CompatibilityLimit(limit, position);
						if (!this.CompatibilityLimits.Contains(item)) {
							this.CompatibilityLimits.Add(item);
						}
					}
					break;
			}
		}

	}
}