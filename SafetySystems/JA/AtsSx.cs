/*
 * Largely public domain code by Odakyufan
 * Modified to work with BVEC_ATS traction manager
 * 
 */

using OpenBveApi.Runtime;
using System;

namespace Plugin
{
	internal class AtsSx : Device
	{
		private readonly Train Train;

		internal AtsSx.States State;

		private double AlarmCountdown;

		private double SpeedCheckCountdown;

		private double CompatibilityAccidentalDepartureCounter;

		private double CompatibilityDistanceAccumulator;

		internal double RedSignalLocation;

		internal double DurationOfAlarm = 5;

		internal double DurationOfInitialization = 3;

		internal double DurationOfSpeedCheck = 0.5;

		internal AtsSx(Train train)
		{
			this.Train = train;
			this.State = AtsSx.States.Disabled;
			this.AlarmCountdown = 0;
			this.SpeedCheckCountdown = 0;
			this.RedSignalLocation = 0;
		}

		internal override void Elapse(ElapseData data, ref bool blocking)
		{
			if (this.State == AtsSx.States.Suppressed && this.Train.TractionManager.CurrentInterventionBrakeNotch <= this.Train.Specs.BrakeNotches)
			{
				this.AlarmCountdown = this.DurationOfInitialization;
				this.State = AtsSx.States.Initializing;
			}
			if (this.State == AtsSx.States.Initializing)
			{
				if (!SoundManager.IsPlaying(CommonSounds.ATSBell))
				{
					SoundManager.Play(CommonSounds.ATSBell, 1.0, 1.0, false);
				}
				AtsSx alarmCountdown = this;
				alarmCountdown.AlarmCountdown = alarmCountdown.AlarmCountdown - data.ElapsedTime.Seconds;
				if (this.AlarmCountdown <= 0)
				{
					this.State = AtsSx.States.Chime;
				}
			}
			if (!blocking)
			{
				if (this.State == AtsSx.States.Blocked)
				{
					this.State = AtsSx.States.Chime;
				}
				if (this.State == AtsSx.States.Chime)
				{
					if (!SoundManager.IsPlaying(CommonSounds.ATSChime))
					{
						SoundManager.Play(CommonSounds.ATSChime, 1.0, 1.0, false);
					}
				}
				else if (this.State == AtsSx.States.Alarm)
				{
					if (!SoundManager.IsPlaying(CommonSounds.ATSBell))
					{
						SoundManager.Play(CommonSounds.ATSBell, 1.0, 1.0, false);
					}
					AtsSx atsSx = this;
					atsSx.AlarmCountdown = atsSx.AlarmCountdown - data.ElapsedTime.Seconds;
					if (this.AlarmCountdown <= 0)
					{
						this.State = AtsSx.States.Emergency;
					}
				}
				else if (this.State == AtsSx.States.Emergency)
				{
					if (!SoundManager.IsPlaying(CommonSounds.ATSBell))
					{
						SoundManager.Play(CommonSounds.ATSBell, 1.0, 1.0, false);
					}
					if (!Train.TractionManager.BrakeInterventionDemanded)
					{
						Train.TractionManager.DemandBrakeApplication(this.Train.Specs.BrakeNotches, "Brake application demanded by ATS-Sx");
					}
				}
				if (this.SpeedCheckCountdown > 0 & data.ElapsedTime.Seconds > 0)
				{
					AtsSx speedCheckCountdown = this;
					speedCheckCountdown.SpeedCheckCountdown = speedCheckCountdown.SpeedCheckCountdown - data.ElapsedTime.Seconds;
				}
				if (this.CompatibilityDistanceAccumulator != 0)
				{
					AtsSx compatibilityDistanceAccumulator = this;
					compatibilityDistanceAccumulator.CompatibilityDistanceAccumulator = compatibilityDistanceAccumulator.CompatibilityDistanceAccumulator + data.Vehicle.Speed.MetersPerSecond * data.ElapsedTime.Seconds;
					if (this.CompatibilityDistanceAccumulator > 27.7)
					{
						this.CompatibilityDistanceAccumulator = 0;
					}
				}
			}
			else if (this.State != AtsSx.States.Disabled & this.State != AtsSx.States.Suppressed)
			{
				this.State = AtsSx.States.Blocked;
			}
			AtsSx compatibilityAccidentalDepartureCounter = this;
			compatibilityAccidentalDepartureCounter.CompatibilityAccidentalDepartureCounter = compatibilityAccidentalDepartureCounter.CompatibilityAccidentalDepartureCounter + data.ElapsedTime.Seconds;
			if (this.State == AtsSx.States.Chime | this.State == AtsSx.States.Normal)
			{
				this.Train.Panel[0] = 1;
				this.Train.Panel[256] = 1;
			}
			if (this.State == AtsSx.States.Initializing | this.State == AtsSx.States.Alarm)
			{
				this.Train.Panel[1] = 1;
				this.Train.Panel[257] = 1;
				this.Train.Panel[258] = 1;
				return;
			}
			if (this.State == AtsSx.States.Emergency)
			{
				int num = ((int)data.TotalTime.Milliseconds % 1000 < 500 ? 1 : 0);
				this.Train.Panel[1] = num;
				this.Train.Panel[257] = 2;
				this.Train.Panel[258] = num;
			}
		}

		internal override void Initialize(InitializationModes mode)
		{
			if (mode == InitializationModes.OffEmergency)
			{
				this.State = AtsSx.States.Suppressed;
				return;
			}
			this.State = AtsSx.States.Normal;
		}

		internal override void KeyDown(VirtualKeys key)
		{
			VirtualKeys virtualKey = key;
			switch (virtualKey)
			{
				case VirtualKeys.S:
					{
						if (!(this.State == AtsSx.States.Alarm & this.Train.Handles.PowerNotch == 0 & this.Train.Handles.BrakeNotch >= this.Train.Specs.AtsNotch))
						{
							return;
						}
						this.State = AtsSx.States.Chime;
						return;
					}
				case VirtualKeys.A1:
					{
						if (this.State != AtsSx.States.Chime)
						{
							return;
						}
						this.State = AtsSx.States.Normal;
						return;
					}
				case VirtualKeys.B1:
					{
						if (!(this.State == AtsSx.States.Emergency & this.Train.Handles.Reverser == 0 & this.Train.Handles.PowerNotch == 0 & this.Train.Handles.BrakeNotch == this.Train.Specs.BrakeNotches + 1))
						{
							return;
						}
						this.State = AtsSx.States.Chime;
						Train.TractionManager.ResetBrakeApplication();
						return;
					}
				default:
					{
						if (virtualKey != VirtualKeys.D)
						{
							return;
						}
						if (this.State == AtsSx.States.Disabled)
						{
							this.State = AtsSx.States.Suppressed;
							return;
						}
						this.State = AtsSx.States.Disabled;
						return;
					}
			}
		}

		internal override void SetBeacon(BeaconData beacon)
		{
			if (this.State != AtsSx.States.Disabled & this.State != AtsSx.States.Initializing)
			{
				int type = beacon.Type;
				if (type > 9)
				{
					if (type == 12)
					{
						if (this.CompatibilityDistanceAccumulator == 0)
						{
							this.CompatibilityDistanceAccumulator = 4.94065645841247E-324;
							return;
						}
						if (beacon.Signal.Aspect == 0 | beacon.Signal.Aspect >= 10 && this.State == AtsSx.States.Chime | this.State == AtsSx.States.Normal | this.State == AtsSx.States.Alarm && Math.Abs(this.Train.State.Speed.KilometersPerHour) > (double)beacon.Optional)
						{
							this.State = AtsSx.States.Emergency;
						}
						this.CompatibilityDistanceAccumulator = 0;
						return;
					}
					if (type != 50)
					{
						switch (type)
						{
							case 55:
								{
									if (!(this.State == AtsSx.States.Chime | this.State == AtsSx.States.Normal | this.State == AtsSx.States.Alarm) || Math.Abs(this.Train.State.Speed.KilometersPerHour) <= (double)beacon.Optional)
									{
										return;
									}
									this.State = AtsSx.States.Emergency;
									return;
								}
							case 56:
								{
									if (!(this.State == AtsSx.States.Chime | this.State == AtsSx.States.Normal) || Math.Abs(this.Train.State.Speed.KilometersPerHour) <= (double)beacon.Optional)
									{
										return;
									}
									this.AlarmCountdown = this.DurationOfAlarm;
									this.State = AtsSx.States.Alarm;
									return;
								}
							case 60:
								{
									this.CompatibilityAccidentalDepartureCounter = 0;
									return;
								}
							case 61:
								{
									if (!(beacon.Signal.Aspect == 0 | beacon.Signal.Aspect >= 10) || this.CompatibilityAccidentalDepartureCounter <= 0.001 * (double)beacon.Optional || !(this.State == AtsSx.States.Chime | this.State == AtsSx.States.Normal | this.State == AtsSx.States.Alarm))
									{
										return;
									}
									this.State = AtsSx.States.Emergency;
									return;
								}
						}
					}
					else if (beacon.Signal.Aspect == 0 | beacon.Signal.Aspect >= 10 && this.State == AtsSx.States.Chime | this.State == AtsSx.States.Normal | this.State == AtsSx.States.Alarm && Math.Abs(this.Train.State.Speed.KilometersPerHour) > 45)
					{
						this.State = AtsSx.States.Emergency;
						return;
					}
					else
					{
						return;
					}
				}
				else
				{
					switch (type)
					{
						case 0:
							{
								if (!(beacon.Signal.Aspect == 0 | beacon.Signal.Aspect >= 10) || !(this.State == AtsSx.States.Chime | this.State == AtsSx.States.Normal))
								{
									return;
								}
								this.AlarmCountdown = this.DurationOfAlarm;
								this.State = AtsSx.States.Alarm;
								this.UpdateRedSignalLocation(beacon);
								return;
							}
						case 1:
							{
								if (!(beacon.Signal.Aspect == 0 | beacon.Signal.Aspect >= 10) || !(this.State == AtsSx.States.Chime | this.State == AtsSx.States.Normal | this.State == AtsSx.States.Alarm))
								{
									return;
								}
								this.State = AtsSx.States.Emergency;
								return;
							}
						case 2:
							{
								if (!((beacon.Signal.Aspect == 0 | beacon.Signal.Aspect >= 10) & (beacon.Optional == 0 | beacon.Optional >= this.Train.Specs.Cars)) || !(this.State == AtsSx.States.Chime | this.State == AtsSx.States.Normal | this.State == AtsSx.States.Alarm))
								{
									return;
								}
								this.State = AtsSx.States.Emergency;
								return;
							}
						default:
							{
								if (type == 9)
								{
									int optional = beacon.Optional / 1000;
									int num = beacon.Optional % 1000;
									if (optional != 0 || !(this.State == AtsSx.States.Chime | this.State == AtsSx.States.Normal | this.State == AtsSx.States.Alarm) || Math.Abs(this.Train.State.Speed.KilometersPerHour) <= (double)num)
									{
										return;
									}
									this.State = AtsSx.States.Emergency;
									return;
								}
								else
								{
									break;
								}
							}
					}
				}
				int frequencyFromBeacon = Train.GetFrequencyFromBeacon(beacon);
				if (frequencyFromBeacon == 108)
				{
					if (this.State == AtsSx.States.Chime | this.State == AtsSx.States.Normal | this.State == AtsSx.States.Alarm)
					{
						if (this.SpeedCheckCountdown <= 0)
						{
							this.SpeedCheckCountdown = this.DurationOfSpeedCheck;
							return;
						}
						this.SpeedCheckCountdown = 0;
						this.State = AtsSx.States.Emergency;
						return;
					}
				}
				else if (frequencyFromBeacon != 123)
				{
					switch (frequencyFromBeacon)
					{
						case 129:
						case 130:
							{
								if (!(this.State == AtsSx.States.Chime | this.State == AtsSx.States.Normal))
								{
									break;
								}
								this.AlarmCountdown = this.DurationOfAlarm;
								this.State = AtsSx.States.Alarm;
								this.UpdateRedSignalLocation(beacon);
								break;
							}
						default:
							{
								return;
							}
					}
				}
				else if (this.State == AtsSx.States.Chime | this.State == AtsSx.States.Normal | this.State == AtsSx.States.Alarm)
				{
					this.State = AtsSx.States.Emergency;
					return;
				}
			}
		}

		internal override void SetSignal(SignalData[] signal)
		{
			if (this.RedSignalLocation != double.MinValue)
			{
				for (int i = 0; i < (int)signal.Length; i++)
				{
					if (signal[i].Distance < 50 && Math.Abs(this.Train.State.Location + signal[i].Distance - this.RedSignalLocation) < 50 && signal[i].Aspect != 0)
					{
						this.RedSignalLocation = double.MinValue;
					}
				}
			}
		}

		private void UpdateRedSignalLocation(BeaconData beacon)
		{
			if (beacon.Signal.Distance < 1200)
			{
				double location = this.Train.State.Location + beacon.Signal.Distance;
				if (location > this.RedSignalLocation)
				{
					this.RedSignalLocation = location;
				}
			}
		}

		internal enum States
		{
			Disabled,
			Suppressed,
			Initializing,
			Blocked,
			Chime,
			Normal,
			Alarm,
			Emergency
		}
	}
}