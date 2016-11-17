/*
 * Largely public domain code by Odakyufan
 * Modified to work with BVEC_ATS traction manager
 * 
 */
using OpenBveApi.Runtime;
using System;
using System.Collections.Generic;

namespace Plugin
{
	internal class Atc : Device
	{
		private const double CompatibilitySuppressDistance = 50;

		/// <summary>The underlying train.</summary>
		private readonly Train Train;

		/// <summary>The current state of the system.</summary>
		internal Atc.States State;

		/// <summary>The previous state of the system (Used for logging).</summary>
		internal Atc.States PreviousState;

		private bool SwitchToAtcOnce;

		/// <summary>Whether emergency operation is enabled. In emergency operation, the train is allowed to travel at 15 km/h (or a custom value) regardless of the actually permitted speed.</summary>
		internal bool EmergencyOperation;

		/// <summary>The current signal aspect.</summary>
		private int Aspect;

		private double BlockLocation;

		/// <summary>The current signal pattern.</summary>
		internal Atc.SignalPattern Pattern;

		/// <summary>The state of the compatibility ATC track.</summary>
		private Atc.CompatibilityStates CompatibilityState;

		/// <summary>A list of all ATC speed limits in the route.</summary>
		private List<Atc.CompatibilityLimit> CompatibilityLimits;

		/// <summary>The element in the CompatibilityLimits list that holds the last encountered speed limit.</summary>
		private int CompatibilityLimitPointer;

		private double CompatibilitySuppressLocation = double.MinValue;

		/// <summary>The state of the preceding train, or a null reference.</summary>
		private PrecedingVehicleState PrecedingTrain;

		private int RealTimeAdvanceWarningUpcomingSignalAspect = -1;

		private double RealTimeAdvanceWarningUpcomingSignalLocation = double.MinValue;

		private double RealTimeAdvanceWarningReferenceLocation = double.MinValue;

		private double ServiceBrakesTimer;

		private double KakuninCheckSpeed = -1;

		internal int KakuninDelay = 5000;

		private double KakuninTimer = 0;

		internal bool KakuninPrimed = false;

		internal bool KakuninActive = false;

		internal bool KakuninCheck = false;

		private bool KakuninCanReset = false;

		private bool KakuninNewSection = false;

		private bool KakuninATCRelease = false;

		internal int KakuninPrimedIndicator = -1;

		internal int KakuninTimerActiveIndicator = -1;

		internal int KakuninBrakeApplicationIndicator = -1;


		/// <summary>Represents the signal that indicates that ATC is not available.</summary>
		private readonly Atc.Signal NoSignal = Atc.Signal.CreateNoSignal(-1);

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

		private double MaximumDeceleration = 1.11111111111111;

		private double RegularDeceleration = 0.530555555555556;

		private double RegularDelay = 0.5;

		private double OrpDeceleration = 1.14444444444444;

		private double OrpDelay = 3.9;

		private double OrpReleaseSpeed = 2.77777777777778;

		private double Acceleration = 0.530555555555556;

		private double AccelerationDelay = 2;

		private double AccelerationTimeThreshold1 = 5;

		private double AccelerationTimeThreshold2 = 15;

		private double FinalApproachSpeed = 6.94444444444444;

		private double ServiceBrakesTimerMaximum = 2;

		private double ServiceBrakesSpeedDifference = 2.77777777777778;

		/// <summary>Whether to automatically switch between ATS and ATC.</summary>
		internal bool AutomaticSwitch;

		/// <summary>Represents the signal for the emergency operation mode, or a null reference if emergency operation is not available.</summary>
		internal Atc.Signal EmergencyOperationSignal = Atc.Signal.CreateEmergencyOperationSignal(15.0/ 3/6);

		internal List<Atc.Signal> Signals = new List<Atc.Signal>();

		internal Atc(Train train)
		{
			this.Train = train;
			this.State = Atc.States.Disabled;
			this.EmergencyOperation = false;
			this.Aspect = 0;
			this.Pattern = new Atc.SignalPattern(this.NoSignal, this);
			this.CompatibilityState = Atc.CompatibilityStates.Ats;
			this.CompatibilityLimits = new List<Atc.CompatibilityLimit>();
		}

		internal override void Elapse(ElapseData data, ref bool blocking)
		{
			int currentBrakeNotchRequest;
			if (this.Train.Handles.BrakeNotch != 0)
			{
				currentBrakeNotchRequest = this.Train.Handles.BrakeNotch;
			}
			else
			{
				currentBrakeNotchRequest = this.State == States.Ats ? 0 : this.Train.TractionManager.CurrentInterventionBrakeNotch;
				
			}
			double acceleration;
			object str;
			object obj;
			this.PrecedingTrain = data.PrecedingVehicle;
			foreach (Atc.Signal signal in this.Signals)
			{
				if (this.Aspect != signal.Aspect)
				{
					continue;
				}
				this.CompatibilitySuppressLocation = this.Train.State.Location;
				break;
			}
			if (this.SwitchToAtcOnce)
			{
				this.State = Atc.States.Normal;
				this.SwitchToAtcOnce = false;
			}
			if (this.State == Atc.States.Suppressed && this.Train.TractionManager.CurrentInterventionBrakeNotch <= this.Train.Specs.BrakeNotches)
			{
				this.State = Atc.States.Ats;
			}
			if (!blocking)
			{
				//Kankunin
				if(KakuninPrimed == true)
				{
					if (KakuninActive == true && KakuninCheck == false)
					{
						//Check that our speed is GREATER than the check speed, and that our timer has not yet expired
						if (data.Vehicle.Speed.KilometersPerHour > KakuninCheckSpeed && KakuninCheck == false && KakuninTimer < KakuninDelay)
						{
							KakuninTimer += data.ElapsedTime.Milliseconds;
						}
						else
						{
							//One of the conditions has failed, so set the ATC state to full service
							KakuninCheck = true;
							KakuninTimer = 0;
						}
					}
					//The Kakunin check has failed
					if (KakuninCheck == true)
					{
						if (Train.TractionManager.BrakeInterventionDemanded == false)
						{
							//Apply brakes via the TractionManager
							Train.TractionManager.DemandBrakeApplication(this.Train.Specs.BrakeNotches);
						}
						if (data.Vehicle.Speed.KilometersPerHour == 0)
						{
							//We have stopped, so elapse the timer
							KakuninTimer += data.ElapsedTime.Milliseconds;
							if (KakuninTimer > 5000)
							{
								//We're only worried about the driver's inputs, not the plugin input
								if (Train.Handles.Reverser == 0 && Train.Handles.BrakeNotch == Train.Specs.BrakeNotches + 1)
								{
									//Reverser is in N, brake is at EMG
									KakuninCanReset = true;
								}
								else
								{
									KakuninCanReset = false;
								}
							}
						}
						else
						{
							//We're moving, so reset the timer
							KakuninTimer = 0;
						}
					}

				}
				//Kankuin panel variables
				if (KakuninPrimedIndicator != -1)
				{
					this.Train.Panel[KakuninPrimedIndicator] = KakuninPrimed ? 1 : 0;
				}
				if (KakuninTimerActiveIndicator != -1)
				{
					this.Train.Panel[KakuninTimerActiveIndicator] = KakuninActive ? 1 : 0;
				}
				if (KakuninBrakeApplicationIndicator != -1)
				{
					this.Train.Panel[KakuninBrakeApplicationIndicator] = KakuninCheck ? 1 : 0;
				}
				Atc.Signal currentSignal = this.GetCurrentSignal();
				Atc.SignalPattern pattern = this.Pattern;
				Atc.SignalPattern signalPattern = new Atc.SignalPattern(currentSignal, this);
				if (Math.Abs(this.RealTimeAdvanceWarningUpcomingSignalLocation - this.RealTimeAdvanceWarningReferenceLocation) < 5)
				{
					if (!(currentSignal.FinalSpeed <= 0 | currentSignal.OverrunProtector))
					{
						Atc.Signal upcomingSignal = this.GetUpcomingSignal();
						currentSignal.ZenpouYokoku = upcomingSignal.FinalSpeed < currentSignal.FinalSpeed & currentSignal.FinalSpeed > 0;
					}
					else
					{
						currentSignal.ZenpouYokoku = false;
					}
				}
				signalPattern.Update(this);
				if (currentSignal.Distance > 0 & currentSignal.Distance < double.MaxValue)
				{
					double blockLocation = this.BlockLocation + currentSignal.Distance - this.Train.State.Location;
					double num = (currentSignal.OverrunProtector ? this.OrpDeceleration : this.RegularDeceleration);
					double num1 = (currentSignal.OverrunProtector ? this.OrpDelay : this.RegularDelay);
					double currentSpeed = (pattern.CurrentSpeed * pattern.CurrentSpeed - currentSignal.FinalSpeed * currentSignal.FinalSpeed) / (2 * num) + pattern.CurrentSpeed * num1;
					double num2 = (blockLocation - currentSpeed) / Math.Max(pattern.CurrentSpeed, 1.38888888888889);
					bool finalSpeed = currentSignal.FinalSpeed <= 0 & signalPattern.CurrentSpeed < this.FinalApproachSpeed | currentSignal.OverrunProtector;
					if (num2 > this.AccelerationTimeThreshold2 & !finalSpeed)
					{
						double acceleration1 = 4 * this.Acceleration * this.Acceleration * num * num * (this.AccelerationTimeThreshold2 + num1) * (this.AccelerationTimeThreshold2 + num1) + 4 * (this.Acceleration + num) * (num * this.Train.State.Speed.MetersPerSecond * this.Train.State.Speed.MetersPerSecond + this.Acceleration * (2 * num * (blockLocation - this.Train.State.Speed.MetersPerSecond * this.AccelerationDelay) + currentSignal.FinalSpeed * currentSignal.FinalSpeed));
						if (acceleration1 <= 0)
						{
							acceleration = Math.Max(currentSignal.FinalSpeed, pattern.CurrentSpeed);
						}
						else
						{
							acceleration = (-this.Acceleration * num * (this.AccelerationTimeThreshold2 + num1) + 0.5 * Math.Sqrt(acceleration1)) / (this.Acceleration + num);
							if (acceleration < currentSignal.FinalSpeed)
							{
								acceleration = currentSignal.FinalSpeed;
							}
						}
						acceleration = Math.Floor(0.72 * acceleration + 0.001) / 0.72;
						if (acceleration < pattern.CurrentSpeed)
						{
							acceleration = pattern.CurrentSpeed;
						}
						currentSignal = new Atc.Signal(currentSignal.Aspect, currentSignal.Indicator, acceleration, acceleration, -1, currentSignal.Kirikae, currentSignal.ZenpouYokoku, currentSignal.OverrunProtector);
						signalPattern = new Atc.SignalPattern(currentSignal, this);
						signalPattern.Update(this);
					}
					else if (!(num2 > this.AccelerationTimeThreshold1 & !finalSpeed))
					{
						if (finalSpeed & currentSignal.Indicator == Atc.SignalIndicators.Green)
						{
							currentSignal = new Atc.Signal(currentSignal.Aspect, Atc.SignalIndicators.Red, currentSignal.InitialSpeed, currentSignal.FinalSpeed, currentSignal.Distance, currentSignal.Kirikae, currentSignal.ZenpouYokoku, currentSignal.OverrunProtector);
							signalPattern = new Atc.SignalPattern(currentSignal, this);
						}
						if (!currentSignal.OverrunProtector)
						{
							signalPattern.TopSpeed = Math.Max(currentSignal.FinalSpeed, pattern.CurrentSpeed);
						}
						signalPattern.Update(this);
					}
					else if (!(pattern.Signal.Distance > 0 & pattern.Signal.Distance < double.MaxValue))
					{
						double currentSpeed1 = pattern.CurrentSpeed;
						currentSignal = new Atc.Signal(pattern.Signal.Aspect, pattern.Signal.Indicator, currentSpeed1, currentSpeed1, -1, pattern.Signal.Kirikae, pattern.Signal.ZenpouYokoku, pattern.Signal.OverrunProtector);
						signalPattern = new Atc.SignalPattern(currentSignal, this);
						signalPattern.Update(this);
					}
					else
					{
						signalPattern.TopSpeed = Math.Max(currentSignal.FinalSpeed, pattern.CurrentSpeed);
						signalPattern.Update(this);
					}
				}
				if (this.State == Atc.States.Normal | this.State == Atc.States.ServiceHalf | this.State == Atc.States.ServiceFull | this.State == Atc.States.Emergency && !Atc.SignalPattern.ApperanceEquals(pattern, signalPattern))
				{
					if (!SoundManager.IsPlaying(CommonSounds.ATCBell))
					{
						SoundManager.Play(CommonSounds.ATCBell, 1.0, 1.0, false);
					}
				}
				this.Pattern = signalPattern;
				if (!(this.State == Atc.States.Normal | this.State == Atc.States.ServiceHalf | this.State == Atc.States.ServiceFull | this.State == Atc.States.Emergency))
				{
					this.ServiceBrakesTimer = 0;
				}
				else
				{
					if (this.Pattern.CurrentSpeed < 0 || this.Pattern.Signal.OverrunProtector && (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= this.Pattern.CurrentSpeed || this.State == Atc.States.Emergency && (Math.Abs(data.Vehicle.Speed.MetersPerSecond) > 0 || this.ServiceBrakesTimer < this.ServiceBrakesTimerMaximum)))
					{
						if (this.State != Atc.States.ServiceFull & this.State != Atc.States.Emergency)
						{
							this.ServiceBrakesTimer = 0;
						}
						if (this.State != Atc.States.Emergency)
						{
							if (!SoundManager.IsPlaying(CommonSounds.ATCBell))
							{
								SoundManager.Play(CommonSounds.ATCBell, 1.0, 1.0, false);
							}
							this.State = Atc.States.Emergency;
						}
						if (this.Pattern.Signal.OverrunProtector & Math.Abs(data.Vehicle.Speed.MetersPerSecond) > 0)
						{
							this.ServiceBrakesTimer = 0;
						}
						else if (this.ServiceBrakesTimer < this.ServiceBrakesTimerMaximum)
						{
							Atc serviceBrakesTimer = this;
							serviceBrakesTimer.ServiceBrakesTimer = serviceBrakesTimer.ServiceBrakesTimer + data.ElapsedTime.Seconds;
						}
					}
					else if (this.Pattern.CurrentSpeed == 0)
					{
						if (this.State != Atc.States.ServiceFull & this.State != Atc.States.Emergency)
						{
							this.ServiceBrakesTimer = 0;
						}
						if (this.State == Atc.States.Emergency)
						{
							if (!SoundManager.IsPlaying(CommonSounds.ATCBell))
							{
								SoundManager.Play(CommonSounds.ATCBell, 1.0, 1.0, false);
							}
						}
						this.State = Atc.States.ServiceFull;
						if (this.ServiceBrakesTimer < this.ServiceBrakesTimerMaximum)
						{
							Atc atc = this;
							atc.ServiceBrakesTimer = atc.ServiceBrakesTimer + data.ElapsedTime.Seconds;
						}
					}
					else if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) < this.Pattern.ReleaseSpeed)
					{
						if (this.State == Atc.States.Emergency)
						{
							if (!SoundManager.IsPlaying(CommonSounds.ATCBell))
							{
								SoundManager.Play(CommonSounds.ATCBell, 1.0, 1.0, false);
							}
							this.State = Atc.States.Normal;
							this.ServiceBrakesTimer = 0;
						}
						if (this.ServiceBrakesTimer < this.ServiceBrakesTimerMaximum)
						{
							Atc serviceBrakesTimer1 = this;
							serviceBrakesTimer1.ServiceBrakesTimer = serviceBrakesTimer1.ServiceBrakesTimer + data.ElapsedTime.Seconds;
						}
						if (this.ServiceBrakesTimer >= this.ServiceBrakesTimerMaximum)
						{
							if (this.State == Atc.States.ServiceFull | this.State == Atc.States.Emergency)
							{
								this.State = Atc.States.ServiceHalf;
								this.ServiceBrakesTimer = 0;
							}
							else if (this.State == Atc.States.ServiceHalf)
							{
								this.State = Atc.States.Normal;
								this.ServiceBrakesTimer = 0;
							}
						}
					}
					else if (Math.Abs(data.Vehicle.Speed.MetersPerSecond) >= this.Pattern.CurrentSpeed + 0.277777777777778)
					{
						if (this.State == Atc.States.Emergency)
						{
							if (!SoundManager.IsPlaying(CommonSounds.ATCBell))
							{
								SoundManager.Play(CommonSounds.ATCBell, 1.0, 1.0, false);
							}
							this.State = Atc.States.ServiceFull;
							this.ServiceBrakesTimer = 0;
						}
						if (!(this.Pattern.CurrentSpeed > 0 & this.Train.State.Speed.MetersPerSecond < this.Pattern.CurrentSpeed + this.ServiceBrakesSpeedDifference))
						{
							if (this.ServiceBrakesTimer < this.ServiceBrakesTimerMaximum)
							{
								Atc atc1 = this;
								atc1.ServiceBrakesTimer = atc1.ServiceBrakesTimer + data.ElapsedTime.Seconds;
							}
							if (this.ServiceBrakesTimer >= this.ServiceBrakesTimerMaximum)
							{
								if (this.State == Atc.States.Normal)
								{
									this.State = Atc.States.ServiceHalf;
									this.ServiceBrakesTimer = 0;
								}
								else if (this.State == Atc.States.ServiceHalf)
								{
									this.State = Atc.States.ServiceFull;
									this.ServiceBrakesTimer = 0;
								}
							}
						}
						else
						{
							if (this.ServiceBrakesTimer < this.ServiceBrakesTimerMaximum)
							{
								Atc serviceBrakesTimer2 = this;
								serviceBrakesTimer2.ServiceBrakesTimer = serviceBrakesTimer2.ServiceBrakesTimer + data.ElapsedTime.Seconds;
							}
							if (this.ServiceBrakesTimer >= this.ServiceBrakesTimerMaximum && this.State != Atc.States.ServiceHalf)
							{
								this.State = Atc.States.ServiceHalf;
								this.ServiceBrakesTimer = 0;
							}
						}
					}
					if (this.State == Atc.States.ServiceHalf)
					{
						double num3 = (this.Pattern.Signal.OverrunProtector ? this.OrpDeceleration : this.RegularDeceleration);
						int atsNotch = (int)Math.Round((double)(this.Train.Specs.BrakeNotches - this.Train.Specs.AtsNotch + 1) * (num3 / this.MaximumDeceleration));
						atsNotch = atsNotch + (this.Train.Specs.AtsNotch - 1);
						if (atsNotch > this.Train.Specs.BrakeNotches)
						{
							atsNotch = this.Train.Specs.BrakeNotches;
						}
						if (this.Train.TractionManager.CurrentInterventionBrakeNotch < atsNotch)
						{

							currentBrakeNotchRequest = atsNotch;
						}
					}
					else if (this.State == Atc.States.ServiceFull)
					{
						//Demand full service brakes
						currentBrakeNotchRequest = this.Train.Specs.BrakeNotches;
					}
					else if (this.State == Atc.States.Emergency)
					{
						//Demand EB
						currentBrakeNotchRequest = this.Train.Specs.BrakeNotches + 1;
					}
					else
					{
						currentBrakeNotchRequest = 0;
					}
					blocking = true;
				}
			}
			else
			{
				if (this.State != Atc.States.Disabled & this.State != Atc.States.Suppressed)
				{
					this.State = Atc.States.Ats;
				}
				this.Pattern.Signal = this.NoSignal;
				this.Pattern.Update(this);
				this.ServiceBrakesTimer = 0;
			}
			Train.TractionManager.SetBrakeNotch(currentBrakeNotchRequest);
			
			//Panel Indicators Start Here

			/*
			 * Reset Panel Indicators done in main function ATM
			 */
			
			if (this.State == Atc.States.Ats)
			{
				this.Train.Panel[21] = 1;
				this.Train.Panel[271] = 12;
			}
			else if (this.State == Atc.States.Normal | this.State == Atc.States.ServiceHalf | this.State == Atc.States.ServiceFull | this.State == Atc.States.Emergency)
			{
				this.Train.Panel[15] = 1;
				this.Train.Panel[265] = 1;
				if (this.Pattern.Signal.Indicator != Atc.SignalIndicators.X)
				{
					if (this.Pattern.Signal.Aspect == 100)
					{
						this.Train.Panel[24] = 1;
					}
					else if (this.Pattern.Signal.Aspect >= 101 & this.Pattern.Signal.Aspect <= 112)
					{
						this.Train.Panel[this.Pattern.Signal.Aspect - 79] = 1;
					}
					for (int i = 11; i >= 1; i--)
					{
						if (this.Pattern.Signal.FinalSpeed + 0.001 >= this.CompatibilitySpeeds[i])
						{
							this.Train.Panel[271] = i;
							break;
						}
					}
				}
				else
				{
					this.Train.Panel[22] = 1;
					this.Train.Panel[271] = 0;
				}
				switch (this.Pattern.Signal.Indicator)
				{
					case Atc.SignalIndicators.Green:
						{
							this.Train.Panel[111] = 1;
							goto case Atc.SignalIndicators.P;
						}
					case Atc.SignalIndicators.Red:
						{
							this.Train.Panel[110] = 1;
							goto case Atc.SignalIndicators.P;
						}
					case Atc.SignalIndicators.P:
						{
							if (this.Pattern.Signal.OverrunProtector)
							{
								this.Train.Panel[112] = 1;
							}
							if (this.Pattern.Signal.ZenpouYokoku)
							{
								this.Train.Panel[113] = 1;
							}
							this.Train.Panel[34] = (int)Math.Round(3600 * Math.Max(0, this.Pattern.CurrentSpeed));
							if (!this.Pattern.Signal.OverrunProtector)
							{
								if (this.Pattern.Signal.Indicator == Atc.SignalIndicators.X)
								{
									break;
								}
								int num5 = Math.Min(Math.Max(0, (int)Math.Floor(0.72 * this.Pattern.CurrentSpeed + 0.001)), 100);
								int num6 = Math.Min(Math.Max(0, (int)Math.Floor(0.72 * this.Pattern.Signal.FinalSpeed + 0.001)), 100);
								for (int i = num6; i <= num5; i++)
								{
									this.Train.Panel[120 + i] = 1;
								}
							}
							else
							{
								this.Train.Panel[114] = (int)Math.Round(3600 * Math.Max(0, this.Pattern.CurrentSpeed));
								break;
							}
							break;
						}
					case Atc.SignalIndicators.X:
						{
							this.Train.Panel[22] = 1;
							goto case Atc.SignalIndicators.P;
						}
					default:
						{
							goto case Atc.SignalIndicators.P;
						}
				}
			}
			if (this.State == Atc.States.ServiceHalf | this.State == Atc.States.ServiceFull)
			{
				this.Train.Panel[16] = 1;
				this.Train.Panel[267] = 1;
			}
			else if (this.State == Atc.States.Emergency)
			{
				this.Train.Panel[17] = 1;
				this.Train.Panel[268] = 1;
			}
			if (this.State != Atc.States.Disabled & this.State != Atc.States.Suppressed)
			{
				this.Train.Panel[18] = 1;
				this.Train.Panel[266] = 1;
			}
			if (this.EmergencyOperation)
			{
				this.Train.Panel[19] = 1;
				this.Train.Panel[52] = 1;
			}
			if (this.State == Atc.States.Disabled)
			{
				this.Train.Panel[20] = 1;
				this.Train.Panel[53] = 1;
			}
			if (this.ShouldSwitchToAts())
			{
				if (!(this.AutomaticSwitch & Math.Abs(data.Vehicle.Speed.MetersPerSecond) < 0.277777777777778))
				{
					if (!SoundManager.IsPlaying(CommonSounds.ToATSReminder))
					{
						SoundManager.Play(CommonSounds.ToATSReminder, 1.0, 1.0, false);
					}
				}
				else
				{
					this.KeyDown(VirtualKeys.C1);
				}
			}
			else if (this.ShouldSwitchToAtc())
			{
				if (!(this.AutomaticSwitch & Math.Abs(data.Vehicle.Speed.MetersPerSecond) < 0.277777777777778))
				{
					if (!SoundManager.IsPlaying(CommonSounds.ToATCReminder))
					{
						SoundManager.Play(CommonSounds.ToATCReminder, 1.0, 1.0, false);
					}
				}
				else
				{
					this.KeyDown(VirtualKeys.C2);
				}
			}
			if (this.State == Atc.States.Normal | this.State == Atc.States.ServiceHalf | this.State == Atc.States.ServiceFull | this.State == Atc.States.Emergency)
			{
				ElapseData elapseDatum = data;
				object[] objArray = new object[] { this.State.ToString(), " - A:", this.Pattern.Signal.Aspect, " I:", null, null, null, null, null, null, null, null, null };
				object[] objArray1 = objArray;
				if (this.Pattern.Signal.InitialSpeed < double.MaxValue)
				{
					double initialSpeed = 3.6 * this.Pattern.Signal.InitialSpeed;
					str = initialSpeed.ToString("0");
				}
				else
				{
					str = "∞";
				}
				objArray1[4] = str;
				objArray[5] = " F:";
				double finalSpeed1 = 3.6 * this.Pattern.Signal.FinalSpeed;
				objArray[6] = finalSpeed1.ToString("0");
				objArray[7] = " D=";
				object[] objArray2 = objArray;
				if (this.Pattern.Signal.Distance == double.MaxValue)
				{
					obj = "∞";
				}
				else
				{
					double distance = this.Pattern.Signal.Distance - (this.Train.State.Location - this.BlockLocation);
					obj = distance.ToString("0");
				}
				objArray2[8] = obj;
				objArray[9] = " T:";
				double topSpeed = 3.6 * this.Pattern.TopSpeed;
				objArray[10] = topSpeed.ToString("0");
				objArray[11] = " C:";
				double currentSpeed2 = 3.6 * this.Pattern.CurrentSpeed;
				objArray[12] = currentSpeed2.ToString("0");
				elapseDatum.DebugMessage = string.Concat(objArray);
			}
		}

		/// <summary>Gets the ATC speed from the current and upcoming speed limits.</summary>
		/// <returns>The ATC speed, or -1 if ATC is not available.</returns>
		private double GetAtcSpeedFromLimit()
		{
			if (this.CompatibilityState == Atc.CompatibilityStates.Ats)
			{
				return -1;
			}
			if (this.CompatibilityLimits.Count == 0)
			{
				return double.MaxValue;
			}
			if (this.CompatibilityLimits.Count == 1)
			{
				return this.CompatibilityLimits[0].Limit;
			}
			while (this.CompatibilityLimitPointer > 0)
			{
				if (this.CompatibilityLimits[this.CompatibilityLimitPointer].Location > this.Train.State.Location)
				{
					Atc compatibilityLimitPointer = this;
					compatibilityLimitPointer.CompatibilityLimitPointer = compatibilityLimitPointer.CompatibilityLimitPointer - 1;
				}
				else
				{
					break;
				}
			}
			while (this.CompatibilityLimitPointer < this.CompatibilityLimits.Count - 1 && this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Location <= this.Train.State.Location)
			{
				Atc atc = this;
				atc.CompatibilityLimitPointer = atc.CompatibilityLimitPointer + 1;
			}
			if (this.CompatibilityLimitPointer == this.CompatibilityLimits.Count - 1)
			{
				return this.CompatibilityLimits[this.CompatibilityLimitPointer].Limit;
			}
			if (this.CompatibilityLimits[this.CompatibilityLimitPointer].Limit <= this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Limit)
			{
				return this.CompatibilityLimits[this.CompatibilityLimitPointer].Limit;
			}
			double limit = this.CompatibilityLimits[this.CompatibilityLimitPointer].Limit;
			double num = this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Limit;
			if (this.Train.State.Location < this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Location - ((limit * limit - num * num) / (2 * this.RegularDeceleration) + this.RegularDelay * limit))
			{
				return this.CompatibilityLimits[this.CompatibilityLimitPointer].Limit;
			}
			return this.CompatibilityLimits[this.CompatibilityLimitPointer + 1].Limit;
		}

		/// <summary>Gets the ATC speed from the distance to the preceding train if operating in compatibility ATC mode.</summary>
		/// <returns>The ATC speed, or -1 if ATC is not available.</returns>
		private double GetAtcSpeedFromTrain()
		{
			if (this.CompatibilityState == Atc.CompatibilityStates.Ats)
			{
				return -1;
			}
			if (this.PrecedingTrain == null)
			{
				return this.CompatibilitySpeeds[11];
			}
			switch ((int)Math.Floor(this.PrecedingTrain.Location / 100) - (int)Math.Floor(this.Train.State.Location / 100))
			{
				case 0:
					{
						return this.CompatibilitySpeeds[0];
					}
				case 1:
					{
						return this.CompatibilitySpeeds[1];
					}
				case 2:
					{
						return this.CompatibilitySpeeds[3];
					}
				case 3:
					{
						return this.CompatibilitySpeeds[4];
					}
				case 4:
					{
						return this.CompatibilitySpeeds[5];
					}
				case 5:
					{
						return this.CompatibilitySpeeds[6];
					}
				case 6:
					{
						return this.CompatibilitySpeeds[7];
					}
				case 7:
					{
						return this.CompatibilitySpeeds[8];
					}
				case 8:
					{
						return this.CompatibilitySpeeds[9];
					}
				case 9:
					{
						return this.CompatibilitySpeeds[10];
					}
			}
			return this.CompatibilitySpeeds[11];
		}

		private Atc.Signal GetCurrentSignal()
		{
			Atc.Signal signal;
			if (this.EmergencyOperation)
			{
				return this.EmergencyOperationSignal;
			}
			List<Atc.Signal>.Enumerator enumerator = this.Signals.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					Atc.Signal current = enumerator.Current;
					if (current == null)
					{
						return this.NoSignal;
					}
					if (current.Aspect != this.Aspect)
					{
						continue;
					}
					signal = current;
					return signal;
				}
				if (Math.Abs(this.CompatibilitySuppressLocation - this.Train.State.Location) > 50 && this.CompatibilityState != Atc.CompatibilityStates.Ats)
				{
					double atcSpeedFromTrain = this.GetAtcSpeedFromTrain();
					double num = Math.Min(atcSpeedFromTrain, this.GetAtcSpeedFromLimit());
					if (num > 0)
					{
						if (this.CompatibilityState != Atc.CompatibilityStates.ToAts)
						{
							return new Atc.Signal(-1, Atc.SignalIndicators.Green, num);
						}
						return new Atc.Signal(-1, Atc.SignalIndicators.Red, num, 0, double.MaxValue, Atc.KirikaeStates.ToAts, false, false);
					}
					if (num == 0)
					{
						return new Atc.Signal(-1, Atc.SignalIndicators.Red, 0);
					}
				}
				return this.NoSignal;
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
		}

		private Atc.Signal GetUpcomingSignal()
		{
			Atc.Signal signal;
			if (this.EmergencyOperation)
			{
				return this.EmergencyOperationSignal;
			}
			List<Atc.Signal>.Enumerator enumerator = this.Signals.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					Atc.Signal current = enumerator.Current;
					if (current == null)
					{
						return this.NoSignal;
					}
					if (current.Aspect != this.RealTimeAdvanceWarningUpcomingSignalAspect)
					{
						continue;
					}
					signal = current;
					return signal;
				}
				return this.NoSignal;
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
		}

		internal override void Initialize(InitializationModes mode)
		{
			if (mode == InitializationModes.OffEmergency)
			{
				this.State = Atc.States.Suppressed;
				return;
			}
			this.State = Atc.States.Ats;
		}

		internal override void KeyDown(VirtualKeys key)
		{
			switch (key)
			{
				case VirtualKeys.C1:
					//Switch to ATS
					{
						if (!(this.State == Atc.States.Normal | this.State == Atc.States.ServiceHalf | this.State == Atc.States.ServiceFull | this.State == Atc.States.Emergency))
						{
							return;
						}
						this.State = Atc.States.Ats;
						if (!this.ShouldSwitchToAtc())
						{
							if (!SoundManager.IsPlaying(CommonSounds.ToATS))
							{
								SoundManager.Play(CommonSounds.ToATS, 1.0, 1.0, false);
							}
						}
						if (!SoundManager.IsPlaying(CommonSounds.ATCBell))
						{
							SoundManager.Play(CommonSounds.ATCBell, 1.0, 1.0, false);
						}
						return;
					}
				case VirtualKeys.C2:
					//Switch to ATC
					{
						if (this.State != Atc.States.Ats)
						{
							return;
						}
						this.State = Atc.States.Normal;
						if (!this.ShouldSwitchToAts())
						{
							if (!SoundManager.IsPlaying(CommonSounds.ToATC))
							{
								SoundManager.Play(CommonSounds.ToATC, 1.0, 1.0, false);
							}

						}
						if (!SoundManager.IsPlaying(CommonSounds.ATCBell))
						{
							SoundManager.Play(CommonSounds.ATCBell, 1.0, 1.0, false);
						}

						return;
					}
				case VirtualKeys.D:
					ResetKakunin();
					break;
				case VirtualKeys.E:
				case VirtualKeys.F:
					{
						return;
					}
				case VirtualKeys.G:
					//Activates or de-activates the system
					{

						if (this.State == Atc.States.Disabled)
						{
							this.State = Atc.States.Suppressed;
							return;
						}
						this.State = Atc.States.Disabled;
						return;
					}
				case VirtualKeys.H:
					//Enables or disables emergency operation mode
					{
						if (this.EmergencyOperationSignal == null)
						{
							return;
						}
						this.EmergencyOperation = !this.EmergencyOperation;
						return;
					}
				default:
					{
						return;
					}
			}
		}

		/// <summary>Attempts to perform a reset on the Kakunin system</summary>
		internal void ResetKakunin()
		{
			//Kakunin is active, but has not yet triggered a brake application
			if (KakuninActive == true && KakuninCheck == false)
			{
				KakuninActive = false;
				KakuninTimer = 0;
				return;
			}
			//Kakunin is applying brakes, but one of the conditions for reset is available
			if (KakuninCheck == true && (KakuninCanReset == true || KakuninNewSection == true))
			{
				KakuninCheck = false;
				KakuninCanReset = false;
				KakuninActive = false;
				KakuninTimer = 0;
				KakuninNewSection = false;
				Train.TractionManager.ResetBrakeApplication();
				this.State = Atc.States.Normal;
				return;
			}
		}

		internal override void SetBeacon(BeaconData beacon)
		{
			int type = beacon.Type;
			switch (type)
			{
				case -16777215:
					{
						if (!(beacon.Optional >= 0 & beacon.Optional <= 3))
						{
							break;
						}
						this.CompatibilityState = (Atc.CompatibilityStates)beacon.Optional;
						return;
					}
				case -16777214:
					{
						double optional = (double)(beacon.Optional & 4095) / 3.6;
						double num = (double)(beacon.Optional >> 12);
						Atc.CompatibilityLimit compatibilityLimit = new Atc.CompatibilityLimit(optional, num);
						if (this.CompatibilityLimits.Contains(compatibilityLimit))
						{
							break;
						}
						this.CompatibilityLimits.Add(compatibilityLimit);
						break;
					}
				case 5000:
					{
						//Enables Kakunin, using the speed set via Beacon.Optional
						KakuninCheckSpeed = beacon.Optional;
						KakuninPrimed = true;
						break;
					}
				case 5001:
					{
						//Disables Kakunin
						KakuninCheckSpeed = -1;
						KakuninPrimed = false;
						break;
					}
				default:
					{
						if (type != 31)
						{
							return;
						}
						if (!(beacon.Signal.Distance > 0 & beacon.Optional == 0))
						{
							break;
						}
						this.RealTimeAdvanceWarningReferenceLocation = this.Train.State.Location + beacon.Signal.Distance;
						return;
					}
			}
		}

		internal override void SetSignal(SignalData[] signal)
		{
			if (KakuninPrimed == true)
			{
				//If Kakunin is currently primed, check whether ATC brakes are already applying
				if (this.State > States.Ats)
				{
					//ATC brakes are active
					if (KakuninCheck == true)
					{
						//Kakunin brakes are currently applied, but can now be reset as we have left the section
						KakuninNewSection = true;
					}
				}
				else
				{
					//ATC brakes are not active
					if (Train.CurrentSpeed < KakuninCheckSpeed && KakuninCheckSpeed != -1)
					{
						//Speed is less than KakuninCheckSpeed, therefore initiate check
						//Otherwise do nothing...
						KakuninActive = true;
					}
					
				}
				KakuninActive = true;
			}
			this.BlockLocation = this.Train.State.Location + signal[0].Distance;
			this.Aspect = signal[0].Aspect;
			if ((int)signal.Length < 2)
			{
				this.RealTimeAdvanceWarningUpcomingSignalAspect = -1;
				this.RealTimeAdvanceWarningUpcomingSignalLocation = double.MaxValue;
				return;
			}
			this.RealTimeAdvanceWarningUpcomingSignalAspect = signal[1].Aspect;
			this.RealTimeAdvanceWarningUpcomingSignalLocation = this.Train.State.Location + signal[1].Distance;
		}

		/// <summary>Whether the driver should switch to ATC. This returns false if already operating in ATC.</summary>
		/// <returns>Whether the driver should switch to ATC.</returns>
		internal bool ShouldSwitchToAtc()
		{
			if (this.State == Atc.States.Ats && this.Pattern.Signal.Kirikae == Atc.KirikaeStates.ToAtc && Math.Abs(this.Train.State.Speed.MetersPerSecond) < 0.277777777777778)
			{
				return true;
			}
			return false;
		}

		/// <summary>Whether the driver should switch to ATS. This returns false if already operating in ATS.</summary>
		/// <returns>Whether the driver should switch to ATS.</returns>
		internal bool ShouldSwitchToAts()
		{
			if (this.State == Atc.States.Normal | this.State == Atc.States.ServiceHalf | this.State == Atc.States.ServiceFull | this.State == Atc.States.Emergency && this.Pattern.Signal.Kirikae == Atc.KirikaeStates.ToAts && Math.Abs(this.Train.State.Speed.MetersPerSecond) < 0.277777777777778)
			{
				return true;
			}
			return false;
		}

		/// <summary>Represents a speed limit at a specific track position.</summary>
		private struct CompatibilityLimit
		{
			/// <summary>The speed limit.</summary>
			internal double Limit;
			/// <summary>The track position.</summary>
			internal double Location;
			/// <summary>Creates a new compatibility limit.</summary>
			/// <param name="limit">The speed limit.</param>
			/// <param name="location">The track position.</param>
			internal CompatibilityLimit(double limit, double location)
			{
				this.Limit = limit;
				this.Location = location;
			}
		}

		private enum CompatibilityStates
		{
			Ats,
			ToAtc,
			Atc,
			ToAts
		}

		/// <summary>The possible states of the ATC/ ATS switch panel indicator.</summary>
		internal enum KirikaeStates
		{
			/// <summary>The ATC/ ATS state is unchanged.</summary>
			Unchanged,
			/// <summary>The train should switch to ATS.</summary>
			ToAts,
			/// <summary>The train should switch to ATC.</summary>
			ToAtc
		}

		/// <summary>Represents a signal that was received from the track.</summary>
		internal class Signal
		{
			/// <summary>The aspect underlying this signal, or -1 if not relevant.</summary>
			internal int Aspect;
			/// <summary>What the signal indicator should show for this signal.</summary>
			internal Atc.SignalIndicators Indicator;
			/// <summary>The initial speed limit at the beginning of the block, or System.Double.MaxValue to carry over the current speed limit.</summary>
			internal double InitialSpeed;
			/// <summary>The final speed limit at the end of the block, or a negative number for an emergency brake application.</summary>
			internal double FinalSpeed;
			/// <summary>The distance to the end of the block, or a non-positive number to indicate that the final speed should apply immediately, or System.Double.MaxValue if the distance to the end of the block is not known.</summary>
			internal double Distance;
			/// <summary>The state of the ATC Switch Indicator.</summary>
			internal Atc.KirikaeStates Kirikae;
			/// <summary>??? Whether this is a distant signal ???</summary>
			internal bool ZenpouYokoku;
			/// <summary>Whether overrun protection is active on this signal.</summary>
			internal bool OverrunProtector;

			/// <summary>Creates a new signal.</summary>
			/// <param name="aspect">The aspect underlying this signal, or -1 if not relevant.</param>
			/// <param name="indicator">What the signal indicator should show for this signal.</param>
			/// <param name="finalSpeed">The immediate speed limit, or a negative number for an emergency brake application.</param>
			internal Signal(int aspect, Atc.SignalIndicators indicator, double finalSpeed)
			{
				this.Aspect = aspect;
				this.Indicator = indicator;
				this.InitialSpeed = finalSpeed;
				this.FinalSpeed = finalSpeed;
				this.Distance = -1;
				this.Kirikae = Atc.KirikaeStates.ToAtc;
				this.ZenpouYokoku = false;
				this.OverrunProtector = false;
			}

			/// <summary>Creates a new signal.</summary>
			/// <param name="aspect">The aspect underlying this signal, or -1 if not relevant.</param>
			/// <param name="indicator">What the signal indicator should show for this signal.</param>
			/// <param name="initialSpeed">The initial speed limit at the beginning of the block, or System.Double.MaxValue to carry over the current speed limit.</param>
			/// <param name="finalSpeed">The final speed limit at the end of the block, or a negative number for an emergency brake application.</param>
			/// <param name="distance">The distance to the end of the block, or a non-positive number to indicate that the final speed should apply immediately, or System.Double.MaxValue if the distance to the end of the block is not known.</param>
			internal Signal(int aspect, Atc.SignalIndicators indicator, double initialSpeed, double finalSpeed, double distance)
			{
				this.Aspect = aspect;
				this.Indicator = indicator;
				this.InitialSpeed = initialSpeed;
				this.FinalSpeed = finalSpeed;
				this.Distance = distance;
				this.Kirikae = Atc.KirikaeStates.ToAtc;
				this.ZenpouYokoku = false;
				this.OverrunProtector = false;
			}

			/// <summary>Creates a new signal.</summary>
			/// <param name="aspect">The aspect underlying this signal, or -1 if not relevant.</param>
			/// <param name="indicator">What the signal indicator should show for this signal.</param>
			/// <param name="initialSpeed">The initial speed limit at the beginning of the block, or System.Double.MaxValue to carry over the current speed limit.</param>
			/// <param name="finalSpeed">The final speed limit at the end of the block, or a negative number for an emergency brake application.</param>
			/// <param name="distance">The distance to the end of the block, or a non-positive number to indicate that the final speed should apply immediately, or System.Double.MaxValue if the distance to the end of the block is not known.</param>
			/// <param name="kirikae">The state which the ATC switch indicator should show.</param>
			/// <param name="zenpouYokoku">??? Whether this is a distant signal ???</param>
			/// <param name="overrunProtector">Whether overrun protection is active on this signal</param>
			internal Signal(int aspect, Atc.SignalIndicators indicator, double initialSpeed, double finalSpeed, double distance, Atc.KirikaeStates kirikae, bool zenpouYokoku, bool overrunProtector)
			{
				this.Aspect = aspect;
				this.Indicator = indicator;
				this.InitialSpeed = initialSpeed;
				this.FinalSpeed = finalSpeed;
				this.Distance = distance;
				this.Kirikae = kirikae;
				this.ZenpouYokoku = zenpouYokoku;
				this.OverrunProtector = overrunProtector;
			}

			/// <summary>Creates a new signal.</summary>
			/// <param name="limit">The speed over which EB brakes will be applied.</param>
			internal static Atc.Signal CreateEmergencyOperationSignal(double limit)
			{
				return new Atc.Signal(-1, Atc.SignalIndicators.None, limit, limit, -1, Atc.KirikaeStates.Unchanged, false, false);
			}

			internal static Atc.Signal CreateNoSignal(int aspect)
			{
				return new Atc.Signal(aspect, Atc.SignalIndicators.X, -1, -1, -1, Atc.KirikaeStates.ToAts, false, false);
			}
		}

		/// <summary>Represents different states of the the signal indicator inside the cab.</summary>
		internal enum SignalIndicators
		{
			/// <summary>The signal shows nothing.</summary>
			None,
			/// <summary>The signal is green.</summary>
			Green,
			/// <summary>The signal is red.</summary>
			Red,
			/// <summary>The signal is red and the P lamp is lit.</summary>
			P,
			/// <summary>The signal is red and the X lamp is lit.</summary>
			X
		}

		internal class SignalPattern
		{
			/// <summary>The current signal.</summary>
			internal Atc.Signal Signal;
			/// <summary>The top speed limit (the current speed limit never exceeds this value).</summary>
			internal double TopSpeed;
			/// <summary>The current speed limit (above which the brakes are engaged).</summary>
			internal double CurrentSpeed;
			/// <summary>The release speed limit (below which the brakes are released).</summary>
			internal double ReleaseSpeed;
			/// <summary>Creates a new signal pattern, assuming the beginning of the block.</summary>
			internal SignalPattern(Atc.Signal signal, Atc atc)
			{
				this.Signal = signal;
				this.TopSpeed = signal.InitialSpeed;
				this.Update(atc);
			}

			/// <summary>Checks if two pattern have the same appearance.</summary>
			/// <param name="oldPattern">The first pattern.</param>
			/// <param name="newPattern">The second pattern.</param>
			/// <returns>Whether the patterns have the same apperance.</returns>
			internal static bool ApperanceEquals(Atc.SignalPattern oldPattern, Atc.SignalPattern newPattern)
			{
				if (oldPattern.Signal.Indicator != newPattern.Signal.Indicator)
				{
					return false;
				}
				if (!oldPattern.Signal.ZenpouYokoku & newPattern.Signal.ZenpouYokoku)
				{
					return false;
				}
				if (oldPattern.Signal.OverrunProtector != newPattern.Signal.OverrunProtector)
				{
					return false;
				}
				if (!newPattern.Signal.OverrunProtector)
				{
					int num = Math.Min(Math.Max(0, (int)Math.Floor(0.72 * oldPattern.CurrentSpeed + 0.001)), 59);
					int num1 = Math.Min(Math.Max(0, (int)Math.Floor(0.72 * newPattern.CurrentSpeed + 0.001)), 59);
					if (num < num1)
					{
						return false;
					}
					if (num > num1 + 1)
					{
						return false;
					}
				}
				else
				{
					if (newPattern.CurrentSpeed <= 0 & oldPattern.CurrentSpeed > 0)
					{
						return false;
					}
					if (Math.Abs(newPattern.CurrentSpeed - oldPattern.CurrentSpeed) > 1.66666666666667)
					{
						return false;
					}
				}
				if (Math.Min(Math.Max(0, (int)Math.Floor(0.72 * oldPattern.Signal.FinalSpeed + 0.001)), 59) != Math.Min(Math.Max(0, (int)Math.Floor(0.72 * newPattern.Signal.FinalSpeed + 0.001)), 59))
				{
					return false;
				}
				return true;
			}

			/// <summary>Updates the signal pattern.</summary>
			internal void Update(Atc atc)
			{
				double regularDeceleration;
				double regularDelay;
				if (this.Signal.Distance == double.MaxValue)
				{
					//If the distance to the signal is not known, set the ATC speed to the inital speed for this signal's code.
					this.CurrentSpeed = this.Signal.InitialSpeed;
				}
				else if (this.Signal.Distance <= 0)
				{
					//If we have passed the set distance, set the ATC speed to the final speed
					this.CurrentSpeed = this.Signal.FinalSpeed;
				}
				else
				{
					double blockLocation = atc.BlockLocation + this.Signal.Distance - atc.Train.State.Location;
					//Set the deceleration curve
					if (!this.Signal.OverrunProtector)
					{
						//If this signal not fitted with overrun protection, use the standard values
						regularDeceleration = atc.RegularDeceleration;
						regularDelay = atc.RegularDelay;
					}
					else
					{
						//Otherwise use the overrun protection values
						regularDeceleration = atc.OrpDeceleration;
						regularDelay = atc.OrpDelay;
					}
					//Calculate the speed required to declerate
					double finalSpeed = 2 * regularDeceleration * blockLocation + regularDeceleration * regularDeceleration * regularDelay * regularDelay + this.Signal.FinalSpeed * this.Signal.FinalSpeed;
					if (finalSpeed <= 0)
					{
						//If the deceleration required is less than or equal to zero, return the ATC speed, as we are now at the final speed
						this.CurrentSpeed = this.Signal.FinalSpeed;
					}
					else
					{
						this.CurrentSpeed = Math.Sqrt(finalSpeed) - regularDeceleration * regularDelay;
						if (this.CurrentSpeed > this.Signal.InitialSpeed)
						{
							//If our speed is greater than the signal's inital speed, set the ATC speed to the signal's inital speed
							this.CurrentSpeed = this.Signal.InitialSpeed;
						}
						else if (this.CurrentSpeed < this.Signal.FinalSpeed)
						{
							//If our speed is less than the signal's final speed, set the ATC speed to the signal's final speed
							this.CurrentSpeed = this.Signal.FinalSpeed;
						}
					}
					if (blockLocation > 0 & this.CurrentSpeed < atc.OrpReleaseSpeed)
					{
						//If we've decelerated to less than the overrun protection speed, but are still within the block, set the ATC speed to the overrun protection speed
						this.CurrentSpeed = atc.OrpReleaseSpeed;
					}
				}
				if (this.CurrentSpeed > this.TopSpeed)
				{
					//Restrict the ATC speed to max speed
					this.CurrentSpeed = this.TopSpeed;
				}
				//The ATC release speed is the maximum of the signal's final speed and our calculated ATC speed
				this.ReleaseSpeed = Math.Max(this.Signal.FinalSpeed - 1.0 / 3.6, this.CurrentSpeed - 1.0 / 3.6);
			}
		}

		internal enum States
		{
			Disabled,
			Suppressed,
			Ats,
			Normal,
			ServiceHalf,
			ServiceFull,
			Emergency,
			Kakunin
		}
	}
}