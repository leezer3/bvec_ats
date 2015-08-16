/*
 * Largely public domain code by Odakyufan
 * Modified to work with BVEC_ATS traction manager
 * 
 */

using System;
using System.Collections.Generic;
using OpenBveApi.Runtime;

namespace Plugin {
	/// <summary>Represents the AI component that calls out signal aspects and speed restrictions.</summary>
	internal class Calling {
		
		
		// --- structures and enumerations ---

		/// <summary>Represents various signal types.</summary>
		internal enum SignalTypes {
			/// <summary>No signal type was defined.</summary>
			None = 0,
			/// <summary>The signal is a home signal (場内).</summary>
			HomeSignal = 1,
			/// <summary>The signal is a block signal (閉塞).</summary>
			BlockSignal = 2,
			/// <summary>The signal is a departure signal (出発).</summary>
			DepartureSignal = 3,
			/// <summary>The signal is a repeating signal (中継) with white bars.</summary>
			RepeatingSignalSurface = 4,
			/// <summary>The signal is a repeating signal (中継) with color lights.</summary>
			RepeatingSignalTunnel = 41,
			/// <summary>The signal is a distant signal (遠方).</summary>
			DistantSignal = 5,
			/// <summary>The signal is a passing signal (通過).</summary>
			PassingSignal = 6
		}
		
		
		// --- members ---
		
		/// <summary>The underlying train.</summary>
		private Train Train = null;
		
		/// <summary>The delegate to the function to play sounds.</summary>
		private PlaySoundDelegate PlaySound = null;
		
		/// <summary>The type of signal to call.</summary>
		internal SignalTypes SignalType = SignalTypes.None;
		
		/// <summary>The aspects a distant signal shows in relation to the main signal.</summary>
		private int SignalAspects = 0;
		
		/// <summary>The location of the signal to call.</summary>
		internal double SignalLocation = double.MaxValue;

		/// <summary>The aspect of the signal. This does not necessarily correspond to the aspect of the section.</summary>
		internal int SignalAspect = -1;
		
		/// <summary>Whether the signal was called.</summary>
		private bool SignalCalled = true;

		/// <summary>The queue of sounds to call out.</summary>
		private Queue<int> CallQueue = new Queue<int>();
		
		/// <summary>The handle of the currently called-out sound.</summary>
		private SoundHandle CallHandle = null;
		
		/// <summary>The time counter. With a handle, this counts up to CallMaximumDuration after which the sound is forcibly stopped. Without a handle, this counts up to CallMinimumSpacing after which the next sound in the queue is played.</summary>
		private double CallCounter = 0.0;

		/// <summary>The maximum duration of a call.</summary>
		private const double CallMaximumDuration = 10.0;

		/// <summary>The minimum spacing between successive calls.</summary>
		private const double CallMinimumSpacing = 1.0;
		

		// --- constructors ---
		
		/// <summary>Creates a new instance of this class.</summary>
		/// <param name="train">The underlying train.</param>
		/// <param name="playSound">The delegate to the function to play sounds.</param>
		internal Calling(Train train, PlaySoundDelegate playSound) {
			this.Train = train;
			this.PlaySound = playSound;
		}

		
		// --- functions ---
		
		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data.</param>
		internal void Elapse(ElapseData data) {
			// --- signal ---
			if (this.SignalType != SignalTypes.None & !this.SignalCalled) {
				double speed = data.Vehicle.Speed.MetersPerSecond;
				if (speed > 1.0 / 3.6 | this.Train.Doors == DoorStates.None) {
					bool call = false;
					if (this.SignalAspect == 0) {
						call = true;
					} else if (this.SignalType == SignalTypes.DepartureSignal) {
						double distance = this.SignalLocation - data.Vehicle.Location;
						if (distance <= 0.0) {
							call = true;
						} else {
							double deceleration = data.Vehicle.Speed.MetersPerSecond * data.Vehicle.Speed.MetersPerSecond / (2.0 * distance);
							if (deceleration > 2.445 / 3.6) {
								call = true;
							}
						}
					} else {
						double distance = this.SignalLocation - data.Vehicle.Location - 50.0;
						if (distance <= 0.0) {
							call = true;
						} else if (speed > 0.0) {
							double time = distance / speed;
							if (time < 20.0) {
								call = true;
							}
						}
					}
					if (call) {
						CallOutSignalAspect(true);
					}
				}
			}
			// --- queue ---
			if (this.CallHandle != null) {
				this.CallCounter += data.ElapsedTime.Seconds;
				if (this.CallHandle.Stopped | this.CallCounter > CallMaximumDuration) {
					this.CallHandle = null;
					this.CallCounter = 0.0;
				}
			}
			if (this.CallHandle == null) {
				this.CallCounter += data.ElapsedTime.Seconds;
				if (this.CallCounter > CallMinimumSpacing) {
					if (this.CallQueue.Count != 0) {
						int sound = this.CallQueue.Dequeue();
						this.CallHandle = this.PlaySound(sound, 1.0, 1.0, false);
						this.CallCounter = 0.0;
					}
				}
			}
		}
		
		/// <summary>Is called when the state of the doors changes.</summary>
		/// <param name="oldState">The old state of the doors.</param>
		/// <param name="newState">The new state of the doors.</param>
		public void DoorChange(DoorStates oldState, DoorStates newState) {
			if (oldState != DoorStates.None & newState == DoorStates.None) {
				if (this.SignalType != SignalTypes.None & !this.SignalCalled) {
					CallOutSignalAspect(false);
				}
			}
		}

		/// <summary>Is called to inform about signals.</summary>
		/// <param name="signal">The signal data.</param>
		internal void SetSignal(SignalData[] signal) {
			if (this.SignalType != SignalTypes.None & signal.Length >= 2) {
				int aspect = GetSignalAspect(signal[1].Aspect);
				if (this.SignalAspect != aspect) {
					this.SignalAspect = aspect;
					this.SignalCalled = false;
				}
			}
		}
		
		/// <summary>Is called when a beacon is passed.</summary>
		/// <param name="beacon">The beacon data.</param>
		internal void SetBeacon(BeaconData beacon) {
			if (beacon.Type == 40) {
				int type = beacon.Optional % 100;
				int data = beacon.Optional / 100;
				if (type >= 1 & type <= 6) {
					if (type == 4 & data == 1) {
						this.SignalType = SignalTypes.RepeatingSignalTunnel;
					} else if (type == 5 | data == 0) {
						this.SignalType = (SignalTypes)type;
					} else {
						this.SignalType = SignalTypes.None;
					}
					if (this.SignalType != SignalTypes.None) {
						if (type == 4 | type == 5) {
							this.SignalLocation = this.Train.State.Location + Math.Min(200.0, beacon.Signal.Distance);
						} else {
							this.SignalLocation = this.Train.State.Location + beacon.Signal.Distance;
						}
						if (type == 5) {
							this.SignalAspects = data;
						} else {
							this.SignalAspects = 0;
						}
						this.SignalAspect = GetSignalAspect(beacon.Signal.Aspect);
						this.SignalCalled = false;
					}
				} else if (type == 9) {
					if (this.SignalType != SignalTypes.None) {
						int aspect = GetSignalAspect(beacon.Signal.Aspect);
						if (this.SignalAspect != aspect | !this.SignalCalled) {
							this.SignalAspect = aspect;
							CallOutSignalAspect(false);
						}
					}
					this.SignalType = SignalTypes.None;
					this.SignalLocation = double.MaxValue;
					this.SignalAspect = -1;
					this.SignalAspects = 0;
					this.SignalCalled = true;
				} else if (type == 10) {
					CallOutSpeedRestriction(data, false);
				} else if (type == 11) {
					CallOutSpeedRestriction(data, true);
				}
			}
		}
		
		/// <summary>Gets the aspect the signal shows.</summary>
		/// <param name="aspect">The aspect of the underlying section.</param>
		/// <returns>The aspect the signal shows.</returns>
		private int GetSignalAspect(int aspect) {
			if (this.SignalType == SignalTypes.RepeatingSignalSurface) {
				if (aspect == 0 | aspect >= 10) {
					return 0;
				} else if (aspect >= 5) {
					return aspect;
				} else {
					return 4;
				}
			} else if (this.SignalType == SignalTypes.DistantSignal) {
				if (aspect >= 0 | aspect <= 6) {
					int value = this.SignalAspects;
					for (int i = 0; i < 6 - aspect; i++) {
						value /= 10;
					}
					return value % 10;
				} else {
					return 0;
				}
			} else {
				if (aspect < 10) {
					return aspect;
				} else {
					return 0;
				}
			}
		}
		
		/// <summary>Calls out the signal aspect.</summary>
		/// <param name="pause">Whether to include a short pause before calling out the signal aspect.</param>
		private void CallOutSignalAspect(bool pause) {
			if (!this.Train.PluginInitializing) {
				int sound = -1;
				switch (this.SignalType) {
					case SignalTypes.HomeSignal:
						if (this.SignalAspect >= 0 & this.SignalAspect <= 6) {
							sound = 100 + this.SignalAspect;
						}
						break;
					case SignalTypes.BlockSignal:
						if (this.SignalAspect >= 0 & this.SignalAspect <= 6) {
							sound = 107 + this.SignalAspect;
						}
						break;
					case SignalTypes.DepartureSignal:
						if (this.SignalAspect >= 0 & this.SignalAspect <= 6) {
							sound = 114 + this.SignalAspect;
						}
						break;
					case SignalTypes.RepeatingSignalSurface:
						if (this.SignalAspect == 0 | this.SignalAspect == 5 | this.SignalAspect == 6) {
							sound = 121 + this.SignalAspect;
						} else if (this.SignalAspect >= 1 & this.SignalAspect <= 4) {
							sound = 128;
						}
						break;
					case SignalTypes.RepeatingSignalTunnel:
						if (this.SignalAspect >= 0 & this.SignalAspect <= 6) {
							sound = 121 + this.SignalAspect;
						}
						break;
					case SignalTypes.DistantSignal:
						if (this.SignalAspect >= 1 & this.SignalAspect <= 6) {
							sound = 128 + this.SignalAspect;
						}
						break;
					case SignalTypes.PassingSignal:
						if (this.SignalAspect == 0) {
							sound = 100;
						} else if (this.SignalAspect == 1 | this.SignalAspect == 2) {
							sound = 135;
						} else if (this.SignalAspect >= 3 & this.SignalAspect <= 6) {
							sound = 136;
						}
						break;
				}
				if (sound >= 0) {
					if (pause) {
						if (this.CallHandle == null) {
							this.CallCounter = 0.0;
						}
					}
					this.CallQueue.Enqueue(sound);
				}
			}
			this.SignalCalled = true;
		}
		
		/// <summary>Calls out the specified speed restriction.</summary>
		/// <param name="kmph">The speed limit in km/h.</param>
		/// <param name="advance">Whether the speed limit is an advance warning.</param>
		private void CallOutSpeedRestriction(int kmph, bool advance) {
			if (!this.Train.PluginInitializing) {
				if (kmph >= 0 & kmph < 500) {
					int sound;
					if (advance) {
						sound = 300 + kmph / 5;
					} else {
						sound = 200 + kmph / 5;
					}
					this.CallQueue.Enqueue(sound);
				}
			}
		}
		
	}
}