/*
 * Largely public domain code by Odakyufan
 * Modified to work with BVEC_ATS traction manager
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;

using OpenBveApi.Runtime;

namespace Plugin {
	/// <summary>Represents ATS-P.</summary>
	internal class AtsP : Device {
		
		// --- enumerations ---
		
		/// <summary>Represents different states of ATS-P.</summary>
		internal enum States {
			/// <summary>The system is disabled.</summary>
			Disabled = 0,
			/// <summary>The system is enabled, but currently suppressed. This will change to States.Initializing once the emergency brakes are released.</summary>
			Suppressed = 1,
			/// <summary>The system is initializing. This will change to States.Standby once the initialization is complete.</summary>
			Initializing = 2,
			/// <summary>The system is available but no ATS-P signal has yet been picked up.</summary>
			Standby = 3,
			/// <summary>The system is operating normally.</summary>
			Normal = 4,
			/// <summary>The system is approaching a brake pattern.</summary>
			Pattern = 5,
			/// <summary>The system is braking due to speed excess.</summary>
			Brake = 6,
			/// <summary>The system applies the service brakes due to an immediate stop command.</summary>
			Service = 7,
			/// <summary>The system applies the emergency brakes due to an immediate stop command.</summary>
			Emergency = 8
		}

		
		// --- pattern ---
		
		/// <summary>Represents a pattern.</summary>
		internal class Pattern {
			// --- members ---
			/// <summary>The underlying ATS-P device.</summary>
			internal AtsP Device;
			/// <summary>The position of the point of danger, or System.Double.MinValue, or System.Double.MaxValue.</summary>
			internal double Position;
			/// <summary>The warning pattern, or System.Double.MaxValue.</summary>
			internal double WarningPattern;
			/// <summary>The brake pattern, or System.Double.MaxValue.</summary>
			internal double BrakePattern;
			/// <summary>The speed limit at the point of danger, or System.Double.MaxValue.</summary>
			internal double TargetSpeed;
			/// <summary>The current gradient.</summary>
			internal double Gradient;
			/// <summary>Whether the pattern is persistent, i.e. cannot be cleared.</summary>
			internal bool Persistent;
			// --- constructors ---
			/// <summary>Creates a new pattern.</summary>
			/// <param name="device">A reference to the underlying ATS-P device.</param>
			internal Pattern(AtsP device) {
				this.Device = device;
				this.Position = double.MaxValue;
				this.WarningPattern = double.MaxValue;
				this.BrakePattern = double.MaxValue;
				this.TargetSpeed = double.MaxValue;
				this.Gradient = 0.0;
				this.Persistent = false;
			}
			// --- functions ---
			/// <summary>Updates the pattern.</summary>
			/// <param name="system">The current ATS-P system.</param>
			/// <param name="data">The elapse data.</param>
			internal void Perform(AtsP system, ElapseData data) {
				if (this.Position == double.MaxValue | this.TargetSpeed == double.MaxValue) {
					this.WarningPattern = double.MaxValue;
					this.BrakePattern = double.MaxValue;
				} else if (this.Position == double.MinValue) {
					if (this.TargetSpeed > 1.0 / 3.6) {
						this.WarningPattern = this.TargetSpeed + this.Device.WarningPatternTolerance;
						this.BrakePattern = this.TargetSpeed + this.Device.BrakePatternTolerance;
					} else {
						this.WarningPattern = this.TargetSpeed;
						this.BrakePattern = this.TargetSpeed;
					}
					if (this.BrakePattern < this.Device.ReleaseSpeed) {
						this.BrakePattern = this.Device.ReleaseSpeed;
					}
				} else {
					const double earthGravity = 9.81;
					double deceleration = this.Device.DesignDeceleration + earthGravity * this.Gradient;
					double distance = this.Position - system.Position;
					// --- calculate the warning pattern ---
					{
						double sqrtTerm = 2.0 * deceleration * (distance - this.Device.WarningPatternOffset) + deceleration * deceleration * this.Device.WarningPatternDelay * this.Device.WarningPatternDelay + this.TargetSpeed * this.TargetSpeed;
						if (sqrtTerm <= 0.0) {
							this.WarningPattern = -deceleration * this.Device.WarningPatternDelay;
						} else {
							this.WarningPattern = Math.Sqrt(sqrtTerm) - deceleration * this.Device.WarningPatternDelay;
						}
						if (this.TargetSpeed > 1.0 / 3.6) {
							if (this.WarningPattern < this.TargetSpeed + this.Device.WarningPatternTolerance) {
								this.WarningPattern = this.TargetSpeed + this.Device.WarningPatternTolerance;
							}
						} else {
							if (this.WarningPattern < this.TargetSpeed) {
								this.WarningPattern = this.TargetSpeed;
							}
						}
					}
					// --- calculate the brake pattern ---
					{
						double sqrtTerm = 2.0 * deceleration * (distance - this.Device.BrakePatternOffset) + deceleration * deceleration * this.Device.BrakePatternDelay * this.Device.BrakePatternDelay + this.TargetSpeed * this.TargetSpeed;
						if (sqrtTerm <= 0.0) {
							this.BrakePattern = -deceleration * this.Device.BrakePatternDelay;
						} else {
							this.BrakePattern = Math.Sqrt(sqrtTerm) - deceleration * this.Device.BrakePatternDelay;
						}
						if (this.TargetSpeed > 1.0 / 3.6) {
							if (this.BrakePattern < this.TargetSpeed + this.Device.BrakePatternTolerance) {
								this.BrakePattern = this.TargetSpeed + this.Device.BrakePatternTolerance;
							}
						} else {
							if (this.BrakePattern < this.TargetSpeed) {
								this.BrakePattern = this.TargetSpeed;
							}
						}
						if (this.BrakePattern < this.Device.ReleaseSpeed) {
							this.BrakePattern = this.Device.ReleaseSpeed;
						}
					}
					
				}
			}
			/// <summary>Sets the position of the red signal.</summary>
			/// <param name="distance">The position.</param>
			internal void SetRedSignal(double position) {
				this.Position = position;
				this.TargetSpeed = 0.0;
			}
			/// <summary>Sets the position of the green signal.</summary>
			/// <param name="distance">The position.</param>
			internal void SetGreenSignal(double position) {
				this.Position = position;
				this.TargetSpeed = double.MaxValue;
			}
			/// <summary>Sets a speed limit and the position of the speed limit.</summary>
			/// <param name="speed">The speed.</param>
			/// <param name="distance">The position.</param>
			internal void SetLimit(double speed, double position) {
				this.Position = position;
				this.TargetSpeed = speed;
			}
			/// <summary>Sets the train-specific permanent speed limit.</summary>
			/// <param name="speed">The speed limit.</param>
			internal void SetPersistentLimit(double speed) {
				this.Position = double.MinValue;
				this.TargetSpeed = speed;
				this.Persistent = true;
			}
			/// <summary>Sets the gradient.</summary>
			/// <param name="gradient">The gradient.</param>
			internal void SetGradient(double gradient) {
				this.Gradient = gradient;
			}
			/// <summary>Clears the pattern.</summary>
			internal void Clear() {
				if (!this.Persistent) {
					this.Position = double.MaxValue;
					this.WarningPattern = double.MaxValue;
					this.BrakePattern = double.MaxValue;
					this.TargetSpeed = double.MaxValue;
					this.Gradient = 0.0;
				}
			}
			/// <summary>Adds a textual representation to the specified string builder if this pattern is not clear.</summary>
			/// <param name="prefix">The textual prefix.</param>
			/// <param name="builder">The string builder.</param>
			internal void AddToStringBuilder(string prefix, StringBuilder builder) {
				if (this.Position >= double.MaxValue | this.TargetSpeed >= double.MaxValue) {
					// do nothing
				} else if (this.Position <= double.MinValue) {
					string text = prefix + (3.6 * this.BrakePattern).ToString("0");
					if (builder.Length != 0) {
						builder.Append(", ");
					}
					builder.Append(text);
				} else {
					string text;
					double distance = this.Position - this.Device.Position;
					if (distance <= 0.0) {
						text = prefix + (3.6 * this.BrakePattern).ToString("0");
					} else {
						text = prefix + (3.6 * this.TargetSpeed).ToString("0") + "(" + (3.6 * this.BrakePattern).ToString("0") + ")@" + distance.ToString("0");
					}
					if (builder.Length != 0) {
						builder.Append(", ");
					}
					builder.Append(text);
				}
			}
		}
		
		// --- compatibility limit ---
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
		
		/// <summary>Whether simultaneous ATS-Sx/P mode is currently active.</summary>
		private bool AtsSxPMode;
		
		/// <summary>Whether the brake release is currently active.</summary>
		private bool BrakeRelease;
		
		/// <summary>The remaining time before the brake release is over.</summary>
		private double BrakeReleaseCountdown;
		
		/// <summary>The current initialization countdown.</summary>
		private double InitializationCountdown;
		
		/// <summary>The position of the train as obtained from odometry.</summary>
		private double Position;
		
		/// <summary>The position at which to switch to ATS-Sx, or System.Double.MaxValue.</summary>
		private double SwitchToAtsSxPosition;
		
		/// <summary>A list of all compatibility temporary speed limits in the route.</summary>
		private List<CompatibilityLimit> CompatibilityLimits;
		
		/// <summary>The element in the CompatibilityLimits list that holds the last speed limit.</summary>
		private int CompatibilityLimitPointer;
		
		
		// --- odakyu digital ats-p (experimental) ---
		
		/// <summary>Whether D-ATS-P is supported by the train.</summary>
		internal bool DAtsPSupported;
		
		/// <summary>Whether D-ATS-P has been activated through a beacon.</summary>
		private bool DAtsPActive;

		/// <summary>Whether D-ATS-P is transmitting continuously.</summary>
		private bool DAtsPContinuous;

		/// <summary>The current signal aspect.</summary>
		private int DAtsPAspect;
		
		/// <summary>The D-ATS-P pattern for the last encountered signal.</summary>
		private Pattern DAtsPZerothSignalPattern;

		/// <summary>The D-ATS-P pattern for the next signal.</summary>
		private Pattern DAtsPFirstSignalPattern;

		/// <summary>The D-ATS-P pattern for the signal after the next signal.</summary>
		private Pattern DAtsPSecondSignalPattern;
		
		
		// --- patterns ---
		
		/// <summary>The signal patterns.</summary>
		private Pattern[] SignalPatterns;
		
		/// <summary>The divergence pattern.</summary>
		private Pattern DivergencePattern;
		
		/// <summary>The downslope pattern.</summary>
		private Pattern DownslopePattern;

		/// <summary>The curve pattern.</summary>
		private Pattern CurvePattern;

		/// <summary>The temporary pattern.</summary>
		private Pattern TemporaryPattern;

		/// <summary>The route-specific permanent pattern.</summary>
		private Pattern RoutePermanentPattern;
		
		/// <summary>The train-specific permanent pattern.</summary>
		internal Pattern TrainPermanentPattern;

		/// <summary>The compatibility temporary pattern.</summary>
		private Pattern CompatibilityTemporaryPattern;

		/// <summary>The compatibility permanent pattern.</summary>
		private Pattern CompatibilityPermanentPattern;

		/// <summary>A list of all patterns.</summary>
		private Pattern[] Patterns;
		
		
		// --- parameters ---

		/// <summary>The duration of the initialization process.</summary>
		internal double DurationOfInitialization = 3.0;

		/// <summary>The duration of the brake release. If zero, brake release is not available.</summary>
		internal double DurationOfBrakeRelease = 60.0;
		
		/// <summary>The design deceleration.</summary>
		internal double DesignDeceleration = 2.445 / 3.6;

		/// <summary>The reaction delay for the brake pattern.</summary>
		internal double BrakePatternDelay = 0.5;

		/// <summary>The signal offset for the brake pattern.</summary>
		internal double BrakePatternOffset = 0.0;
		
		/// <summary>The speed tolerance for the brake pattern.</summary>
		internal double BrakePatternTolerance = 0.0 / 3.6;

		/// <summary>The reaction delay for the warning pattern.</summary>
		internal double WarningPatternDelay = 5.5;

		/// <summary>The signal offset for the warning pattern.</summary>
		internal double WarningPatternOffset = 50.0;

		/// <summary>The speed tolerance for the warning pattern.</summary>
		internal double WarningPatternTolerance = -5.0 / 3.6;

		/// <summary>The release speed.</summary>
		internal double ReleaseSpeed = 15.0 / 3.6;

	    internal int ATSPBell =  -1;
		
		
		// --- constructors ---
		
		/// <summary>Creates a new instance of this system.</summary>
		/// <param name="train">The train.</param>
		internal AtsP(Train train) {
			this.Train = train;
			this.State = States.Disabled;
			this.AtsSxPMode = false;
			this.InitializationCountdown = 0.0;
			this.SwitchToAtsSxPosition = double.MaxValue;
			this.CompatibilityLimits = new List<CompatibilityLimit>();
			this.CompatibilityLimitPointer = 0;
			this.DAtsPSupported = false;
			this.DAtsPActive = false;
			this.DAtsPContinuous = false;
			this.SignalPatterns = new Pattern[10];
			for (int i = 0; i < this.SignalPatterns.Length; i++) {
				this.SignalPatterns[i] = new Pattern(this);
			}
			this.DivergencePattern = new Pattern(this);
			this.DownslopePattern = new Pattern(this);
			this.CurvePattern = new Pattern(this);
			this.TemporaryPattern = new Pattern(this);
			this.RoutePermanentPattern = new Pattern(this);
			this.TrainPermanentPattern = new Pattern(this);
			this.CompatibilityTemporaryPattern = new Pattern(this);
			this.CompatibilityPermanentPattern = new Pattern(this);
			this.DAtsPZerothSignalPattern = new Pattern(this);
			this.DAtsPFirstSignalPattern = new Pattern(this);
			this.DAtsPSecondSignalPattern = new Pattern(this);
			List<Pattern> patterns = new List<Pattern>();
			patterns.AddRange(this.SignalPatterns);
			patterns.Add(this.DivergencePattern);
			patterns.Add(this.DownslopePattern);
			patterns.Add(this.CurvePattern);
			patterns.Add(this.TemporaryPattern);
			patterns.Add(this.RoutePermanentPattern);
			patterns.Add(this.TrainPermanentPattern);
			patterns.Add(this.CompatibilityTemporaryPattern);
			patterns.Add(this.CompatibilityPermanentPattern);
			patterns.Add(this.DAtsPZerothSignalPattern);
			patterns.Add(this.DAtsPFirstSignalPattern);
			patterns.Add(this.DAtsPSecondSignalPattern);
			this.Patterns = patterns.ToArray();
		}
		
		
		// --- functions ---
		
		/// <summary>Changes to standby mode and continues in ATS-Sx mode.</summary>
		private void SwitchToSx() {
			if (this.Train.AtsSx != null) {
				foreach (Pattern pattern in this.Patterns) {
					pattern.Clear();
				}
				this.State = States.Standby;
                SoundManager.Play(ATSPBell, 1.0, 1.0, false);
				this.Train.AtsSx.State = AtsSx.States.Chime;
			} else if (this.State != States.Emergency) {
				this.State = States.Emergency;
				if (this.State != States.Brake & this.State != States.Service) {
                    SoundManager.Play(ATSPBell, 1.0, 1.0, false);
				}
			}
			this.SwitchToAtsSxPosition = double.MaxValue;
			this.DAtsPActive = false;
		}
		
		/// <summary>Switches to ATS-P.</summary>
		/// <param name="state">The desired state.</param>
		private void SwitchToP(States state) {
			if (this.State == States.Standby) {
				if (this.Train.AtsSx == null || this.Train.AtsSx.State != AtsSx.States.Emergency) {
					this.State = state;
                    SoundManager.Play(ATSPBell, 1.0, 1.0, false);
				}
			} else if (state == States.Service | state == States.Emergency) {
				if (this.State != States.Brake & this.State != States.Service & this.State != States.Emergency) {
                    SoundManager.Play(ATSPBell, 1.0, 1.0, false);
				}
				this.State = state;
			}
		}
		
		/// <summary>Updates the compatibility temporary speed pattern from the list of known speed limits.</summary>
		private void UpdateCompatibilityTemporarySpeedPattern() {
			if (this.CompatibilityLimits.Count != 0) {
				if (this.CompatibilityTemporaryPattern.Position != double.MaxValue) {
					if (this.CompatibilityTemporaryPattern.BrakePattern < this.Train.State.Speed.MetersPerSecond) {
						return;
					}
					double delta = this.CompatibilityTemporaryPattern.Position - this.Train.State.Location;
					if (delta >= -50.0 & delta <= 0.0) {
						return;
					}
				}
				while (CompatibilityLimitPointer > 0 && this.CompatibilityLimits[CompatibilityLimitPointer].Location > this.Train.State.Location) {
					CompatibilityLimitPointer--;
				}
				while (CompatibilityLimitPointer < this.CompatibilityLimits.Count - 1 && this.CompatibilityLimits[CompatibilityLimitPointer + 1].Location <= this.Train.State.Location) {
					CompatibilityLimitPointer++;
				}
				if (this.CompatibilityLimitPointer == 0 && this.CompatibilityLimits[0].Location > this.Train.State.Location) {
					this.CompatibilityTemporaryPattern.SetLimit(this.CompatibilityLimits[0].Limit, this.CompatibilityLimits[0].Location);
				} else if (this.CompatibilityLimitPointer < this.CompatibilityLimits.Count - 1) {
					this.CompatibilityTemporaryPattern.SetLimit(this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Limit, this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Location);
				} else {
					this.CompatibilityTemporaryPattern.Clear();
				}
			}
		}
		
		
		// --- inherited functions ---
		
		/// <summary>Is called when the system should initialize.</summary>
		/// <param name="mode">The initialization mode.</param>
		internal override void Initialize(InitializationModes mode) {
			if (mode == InitializationModes.OffEmergency) {
				this.State = States.Suppressed;
			} else {
				this.State = States.Standby;
			}
			foreach (Pattern pattern in this.Patterns) {
				if (Math.Abs(this.Train.State.Speed.MetersPerSecond) >= pattern.WarningPattern) {
					pattern.Clear();
				}
			}
		}

		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data.</param>
		/// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
		internal override void Elapse(ElapseData data, ref bool blocking) {
			// --- behavior ---
			if (this.State == States.Suppressed) {
				if (data.Handles.BrakeNotch <= this.Train.Specs.BrakeNotches) {
					this.InitializationCountdown = DurationOfInitialization;
					this.State = States.Initializing;
				}
			}
			if (this.State == States.Initializing) {
				this.InitializationCountdown -= data.ElapsedTime.Seconds;
				if (this.InitializationCountdown <= 0.0) {
					this.State = States.Standby;
					this.BrakeRelease = false;
					this.SwitchToAtsSxPosition = double.MaxValue;
					foreach (Pattern pattern in this.Patterns) {
						if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= pattern.WarningPattern) {
							pattern.Clear();
						}
					}
                    SoundManager.Play(ATSPBell, 1.0, 1.0, false);
				}
			}
			if (BrakeRelease) {
				BrakeReleaseCountdown -= data.ElapsedTime.Seconds;
				if (BrakeReleaseCountdown <= 0.0) {
					BrakeRelease = false;
                    SoundManager.Play(ATSPBell, 1.0, 1.0, false);
				}
			}
			if (this.State != States.Disabled & this.State != States.Initializing) {
				this.Position += data.Vehicle.Speed.MetersPerSecond * data.ElapsedTime.Seconds;
			}
			if (blocking) {
				if (this.State != States.Disabled & this.State != States.Suppressed) {
					this.State = States.Standby;
				}
			} else {
				if (this.DAtsPSupported) {
					double distance = this.DAtsPFirstSignalPattern.Position - this.Train.State.Location;
					if (distance < 0.0) {
						this.DAtsPZerothSignalPattern.Position = this.DAtsPFirstSignalPattern.Position;
						this.DAtsPZerothSignalPattern.TargetSpeed = this.DAtsPFirstSignalPattern.TargetSpeed;
						this.DAtsPFirstSignalPattern.Position = this.DAtsPSecondSignalPattern.Position;
						this.DAtsPFirstSignalPattern.TargetSpeed = this.DAtsPSecondSignalPattern.TargetSpeed;
						this.DAtsPSecondSignalPattern.Position = double.MaxValue;
						this.DAtsPSecondSignalPattern.TargetSpeed = double.MaxValue;
					}
				}
				if (this.DAtsPActive & this.DAtsPContinuous) {
					switch (this.DAtsPAspect) {
							case 1: this.DAtsPFirstSignalPattern.TargetSpeed = 25.0 / 3.6; break;
							case 2: this.DAtsPFirstSignalPattern.TargetSpeed = 45.0 / 3.6; break;
							case 3: this.DAtsPFirstSignalPattern.TargetSpeed = 75.0 / 3.6; break;
							case 4: case 5: case 6: this.DAtsPFirstSignalPattern.TargetSpeed = double.MaxValue; break;
							default: this.DAtsPFirstSignalPattern.TargetSpeed = 0.0; break;
					}
					if (this.DAtsPZerothSignalPattern.TargetSpeed < this.DAtsPFirstSignalPattern.TargetSpeed) {
						this.DAtsPZerothSignalPattern.TargetSpeed = this.DAtsPFirstSignalPattern.TargetSpeed;
					}
				}
				if (this.State == States.Normal | this.State == States.Pattern | this.State == States.Brake) {
					bool brake = false;
					bool warning = false;
					bool normal = true;
					if (this.DivergencePattern.Position > double.MinValue & this.DivergencePattern.Position < double.MaxValue) {
						if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) < this.DivergencePattern.BrakePattern) {
							double distance = this.DivergencePattern.Position - this.Position;
							if (distance < -50.0) {
								this.DivergencePattern.Clear();
							}
						}
					}
					UpdateCompatibilityTemporarySpeedPattern();
					foreach (Pattern pattern in this.Patterns) {
						pattern.Perform(this, data);
						if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= pattern.WarningPattern - 1.0 / 3.6) {
							normal = false;
						}
						if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= pattern.WarningPattern) {
							warning = true;
						}
						if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= pattern.BrakePattern) {
							brake = true;
						}
					}
					if (BrakeRelease) {
						brake = false;
					}
					if (brake & this.State != States.Brake) {
						this.State = States.Brake;
                        SoundManager.Play(ATSPBell, 1.0, 1.0, false);
					} else if (warning & this.State == States.Normal) {
						this.State = States.Pattern;
                        SoundManager.Play(ATSPBell, 1.0, 1.0, false);
					} else if (!brake & !warning & normal & (this.State == States.Pattern | this.State == States.Brake)) {
						this.State = States.Normal;
                        SoundManager.Play(ATSPBell, 1.0, 1.0, false);
					}
					if (this.State == States.Brake) {
						if (data.Handles.BrakeNotch < this.Train.Specs.BrakeNotches) {
							Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches);
						}
					}
					if (this.Position > this.SwitchToAtsSxPosition) {
						SwitchToSx();
					}
				} else if (this.State == States.Service) {
					if (data.Handles.BrakeNotch < this.Train.Specs.BrakeNotches) {
                        Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches);
					}
				} else if (this.State == States.Emergency) {
                    Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);
				}
				if (!this.AtsSxPMode & (this.State == States.Normal | this.State == States.Pattern | this.State == States.Brake | this.State == States.Service | this.State == States.Emergency)) {
					blocking = true;
				}
				if (this.State != States.Disabled & this.Train.Doors != DoorStates.None) {
					Train.tractionmanager.demandpowercutoff();
				}
			}
			// --- panel ---
			if (this.State != States.Disabled & this.State != States.Suppressed) {
				this.Train.Panel[2] = 1;
				this.Train.Panel[259] = 1;
			}
			if (this.State == States.Pattern | this.State == States.Brake | this.State == States.Service | this.State == States.Emergency) {
				this.Train.Panel[3] = 1;
				this.Train.Panel[260] = 1;
			}
			if (this.State == States.Brake | this.State == States.Service | this.State == States.Emergency) {
				this.Train.Panel[5] = 1;
				this.Train.Panel[262] = 1;
			}
			if (this.State != States.Disabled & this.State != States.Suppressed & this.State != States.Standby) {
				this.Train.Panel[6] = 1;
				this.Train.Panel[263] = 1;
			}
			if (this.State == States.Initializing) {
				this.Train.Panel[7] = 1;
				this.Train.Panel[264] = 1;
			}
			if (this.State == States.Disabled) {
				this.Train.Panel[50] = 1;
			}
			if (this.State != States.Disabled & this.State != States.Suppressed & this.State != States.Standby & this.BrakeRelease) {
				this.Train.Panel[4] = 1;
				this.Train.Panel[261] = 1;
			}
			// --- debug ---
			if (this.State == States.Normal | this.State == States.Pattern | this.State == States.Brake | this.State == States.Service | this.State == States.Emergency) {
				StringBuilder builder = new StringBuilder();
				for (int i = 0; i < this.SignalPatterns.Length; i++) {
					this.SignalPatterns[i].AddToStringBuilder(i.ToString() + ":", builder);
				}
				this.DivergencePattern.AddToStringBuilder("分岐/D:", builder);
				this.TemporaryPattern.AddToStringBuilder("臨時/T:", builder);
				this.CurvePattern.AddToStringBuilder("曲線/C:", builder);
				this.DownslopePattern.AddToStringBuilder("勾配/S:", builder);
				this.RoutePermanentPattern.AddToStringBuilder("P:", builder);
				this.TrainPermanentPattern.AddToStringBuilder("M:", builder);
				if (builder.Length == 0) {
					data.DebugMessage = this.State.ToString();
				} else {
					data.DebugMessage = this.State.ToString() + " - " + builder.ToString();
				}
			}
		}
		
		/// <summary>Is called when a key is pressed.</summary>
		/// <param name="key">The key.</param>
		internal override void KeyDown(VirtualKeys key) {
			switch (key) {
				case VirtualKeys.B1:
					// --- reset the system ---
					if ((this.State == States.Brake | this.State == States.Service | this.State == States.Emergency) & this.Train.Handles.Reverser == 0 & this.Train.Handles.PowerNotch == 0 & this.Train.Handles.BrakeNotch >= this.Train.Specs.BrakeNotches) {
						foreach (Pattern pattern in this.Patterns) {
							if (Math.Abs(this.Train.State.Speed.MetersPerSecond) >= pattern.WarningPattern) {
								pattern.Clear();
							}
						}
						this.State = States.Normal;
						this.SwitchToAtsSxPosition = double.MaxValue;
                        SoundManager.Play(ATSPBell, 1.0, 1.0, false);
					}
					break;
				case VirtualKeys.B2:
					// --- brake release ---
					if ((this.State == States.Normal | this.State == States.Pattern) & !BrakeRelease & DurationOfBrakeRelease > 0.0) {
						BrakeRelease = true;
						BrakeReleaseCountdown = DurationOfBrakeRelease;
                        SoundManager.Play(ATSPBell, 1.0, 1.0, false);
					}
					break;
				case VirtualKeys.E:
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
			if (signal.Length >= 2) {
				this.DAtsPAspect = signal[1].Aspect;
			} else {
				this.DAtsPAspect = 5;
			}
		}
		
		/// <summary>Is called when a beacon is passed.</summary>
		/// <param name="beacon">The beacon data.</param>
		internal override void SetBeacon(BeaconData beacon) {
			if (this.State != States.Disabled & this.State != States.Suppressed & this.State != States.Initializing) {
				switch (beacon.Type) {
					case 3:
					case 4:
					case 5:
						// --- P signal pattern / P immediate stop ---
						this.Position = this.Train.State.Location;
						if (this.State != States.Service & this.State != States.Emergency) {
							if (this.State == States.Standby & beacon.Optional != -1) {
								SwitchToP(States.Normal);
							}
							if (this.State != States.Standby) {
								if (beacon.Type == 3 & beacon.Optional >= 10 & beacon.Optional <= 19) {
									int pattern = beacon.Optional - 10;
									this.SignalPatterns[pattern].Clear();
								} else {
									int pattern;
									if (beacon.Type == 3 & beacon.Optional >= 1 & beacon.Optional <= 9) {
										pattern = beacon.Optional;
									} else {
										pattern = 0;
									}
									double position = this.Position + beacon.Signal.Distance;
									bool update = false;
									if (pattern != 0) {
										update = true;
									} else if (this.SignalPatterns[pattern].Position == double.MaxValue) {
										update = true;
									} else if (position > this.SignalPatterns[pattern].Position - 30.0) {
										update = true;
									}
									if (update) {
										if (beacon.Signal.Aspect == 0 | beacon.Signal.Aspect > 100) {
											this.SignalPatterns[pattern].SetRedSignal(position);
											if (beacon.Type != 3 & beacon.Signal.Distance < 50.0 & !BrakeRelease) {
												if (beacon.Type == 4) {
													SwitchToP(States.Emergency);
												} else {
													SwitchToP(States.Service);
												}
											}
										} else {
											this.SignalPatterns[pattern].SetGreenSignal(position);
										}
									}
								}
							}
						}
						break;
					case 6:
						// --- P divergence speed limit ---
						{
							int distance = beacon.Optional / 1000;
							if (distance > 0) {
								if (this.State == States.Standby) {
									SwitchToP(States.Normal);
								}
								this.Position = this.Train.State.Location;
								int speed = beacon.Optional % 1000;
								this.DivergencePattern.SetLimit((double)speed / 3.6, this.Position + distance);
							}
						}
						break;
					case 7:
						// --- P permanent speed limit ---
						this.Position = this.Train.State.Location;
						if (beacon.Optional > 0) {
							if (this.State == States.Standby) {
								SwitchToP(States.Normal);
							}
							this.RoutePermanentPattern.SetLimit((double)beacon.Optional / 3.6, double.MinValue);
						} else {
							SwitchToP(States.Emergency);
						}
						break;
					case 8:
						// --- P downslope speed limit ---
						{
							int distance = beacon.Optional / 1000;
							if (distance > 0) {
								if (this.State == States.Standby) {
									SwitchToP(States.Normal);
								}
								this.Position = this.Train.State.Location;
								int speed = beacon.Optional % 1000;
								this.DownslopePattern.SetLimit((double)speed / 3.6, this.Position + distance);
							}
						}
						break;
					case 9:
						// --- P curve speed limit ---
						{
							int distance = beacon.Optional / 1000;
							if (distance > 0) {
								if (this.State == States.Standby) {
									SwitchToP(States.Normal);
								}
								this.Position = this.Train.State.Location;
								int speed = beacon.Optional % 1000;
								this.CurvePattern.SetLimit((double)speed / 3.6, this.Position + distance);
							}
						}
						break;
					case 10:
						// --- P temporary speed limit / P->S (IIYAMA style) ---
						{
							int left = beacon.Optional / 1000;
							int right = beacon.Optional % 1000;
							if (left != 0) {
								if (this.State == States.Standby) {
									SwitchToP(States.Normal);
								}
								this.Position = this.Train.State.Location;
								this.TemporaryPattern.SetLimit((double)right / 3.6, this.Position + left);
							} else if (left == 0 & right != 0) {
								this.Position = this.Train.State.Location;
								this.SwitchToAtsSxPosition = this.Position + right;
							}
						}
						break;
					case 16:
						// --- P divergence limit released ---
						if (beacon.Optional == 0) {
							this.Position = this.Train.State.Location;
							this.DivergencePattern.Clear();
						}
						break;
					case 18:
						// --- P downslope limit released ---
						if (beacon.Optional == 0) {
							this.Position = this.Train.State.Location;
							this.DownslopePattern.Clear();
						}
						break;
					case 19:
						// --- P curve limit released ---
						if (beacon.Optional == 0) {
							this.Position = this.Train.State.Location;
							this.CurvePattern.Clear();
						}
						break;
					case 20:
						// --- P temporary limit released ---
						if (beacon.Optional == 0) {
							this.Position = this.Train.State.Location;
							this.TemporaryPattern.Clear();
						}
						break;
					case 22:
						// --- D-ATS-P signal pattern ---
						if (beacon.Optional == 0) {
							if (this.DAtsPSupported) {
								this.Position = this.Train.State.Location;
								if (this.DAtsPFirstSignalPattern.Position == double.MaxValue) {
									this.DAtsPFirstSignalPattern.Position = this.Position;
									this.DAtsPFirstSignalPattern.TargetSpeed = double.MaxValue;
								}
								this.DAtsPSecondSignalPattern.Position = this.Position + beacon.Signal.Distance;
								if (this.State == States.Standby) {
									SwitchToP(States.Normal);
								}
								if (!this.DAtsPActive) {
									this.DAtsPActive = true;
                                    SoundManager.Play(ATSPBell, 1.0, 1.0, false);
								}
							}
						}
						break;
					case 25:
						// --- P/S system switch ---
						if (beacon.Optional == 0) {
							// --- Sx only ---
							this.Position = this.Train.State.Location;
							if (this.State == States.Normal | this.State == States.Pattern | this.State == States.Brake) {
								SwitchToSx();
							}
						} else if (beacon.Optional == 1) {
							// --- P only ---
							this.Position = this.Train.State.Location;
							if (this.State == States.Standby) {
								SwitchToP(States.Normal);
							}
							if (this.AtsSxPMode) {
								this.AtsSxPMode = false;
                                SoundManager.Play(ATSPBell, 1.0, 1.0, false);
							}
						} else if (beacon.Optional == 2) {
							// --- simultaneous Sx/P ---
							this.Position = this.Train.State.Location;
							if (this.State == States.Standby) {
								SwitchToP(States.Normal);
							}
							if (!this.AtsSxPMode) {
								this.AtsSxPMode = true;
                                SoundManager.Play(ATSPBell, 1.0, 1.0, false);
							}
						}
						break;
					case 42:
						// --- D-ATS-P continuous transmission ---
						if (beacon.Optional == 0) {
							this.DAtsPContinuous = false;
						} else if (beacon.Optional == 1) {
							this.DAtsPContinuous = this.DAtsPSupported;
						}
						break;
				}
			}
			switch (beacon.Type) {
				case -16777213:
					// --- compatibility temporary pattern ---
					{
						double limit = (double)(beacon.Optional & 4095) / 3.6;
						double position = (beacon.Optional >> 12);
						CompatibilityLimit item = new CompatibilityLimit(limit, position);
						if (!this.CompatibilityLimits.Contains(item)) {
							this.CompatibilityLimits.Add(item);
						}
					}
					break;
				case -16777212:
					// --- compatibility permanent pattern ---
					if (beacon.Optional == 0) {
						this.CompatibilityPermanentPattern.Clear();
					} else {
						double limit = (double)beacon.Optional / 3.6;
						this.CompatibilityPermanentPattern.SetLimit(limit, double.MinValue);
					}
					break;
			}
		}

	}
}