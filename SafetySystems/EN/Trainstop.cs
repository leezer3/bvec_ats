using OpenBveApi.Runtime;

namespace Plugin
{
	/// <summary>Represents a LU Trainstop device</summary>
	internal class Trainstop : Device
	{
		/// <summary>The underlying train.</summary>
		private readonly Train Train;

		// control booleans
		internal bool awsIndicatorLit;
		internal bool brakeDemandIndicatorLit;
		internal bool stopOverrideIndicatorLit;
		internal bool tpwsIndicator3Lit;
		// panel indicies
		internal int awsIndicator;
		internal int awsKeyIndicator = -1;
		internal int brakeDemandIndicator;
		internal int stopOverrideIndicator;
		internal int tpwsIndicator3;
		// keypress tracking
		internal bool awsKeyPressed = false;
		// sound indicies
		internal int awsClearSound = -1;
		internal int awsWarningSound = -1;
		internal int tpwsWarningSound;
		internal int tpwsStartupTest;
		// control values
		internal int awsBrakeCancel;
		internal double awsDelay;
		internal int tpwsBrakeCancel;
		internal double tpwsStopDelay;
		internal double tpwsOverrideLifetime;
		internal int startupTest;
		// state booleans
		private bool awsStop;
		private bool awsRelease;
		private bool tpwsTrainstop;
		private bool tpwsRelease;
		//timers
		private Timer awsTimer;
		private Timer overrideTimer;
		private Timer stopTimer;

		internal Trainstop(Train train)
		{
			Train = train;
		}

		internal override void Elapse(ElapseData data, ref bool blocking)
		{
			if(awsTimer.TimerActive && awsTimer.TimeElapsed != -2)
			{
				awsTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
				if(awsTimer.TimeElapsed > awsDelay && awsStop == false)
				{
					awsTimer.TimeElapsed = -2;
					awsStop = true;
					Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches + 1, "Brake application demanded by the LU Trainstop system.");
					awsRelease = false;
				}
			}

			brakeDemandIndicatorLit = true;
			stopOverrideIndicatorLit = true;
	
			if(data.Vehicle.Speed.KilometersPerHour == 0)
			{
				if (stopTimer.TimerActive == false)
				{
					stopTimer.TimerActive = true;
					stopTimer.TimeElapsed = 0;
				}
					
					
				if(awsRelease)
				{
					Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches + 1, "Brake application demanded by the LU Trainstop system.");
					awsIndicatorLit = true;
					awsTimer.TimerActive = false;
					awsStop = false;
					awsRelease = false;
				}

				stopTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
				if(tpwsRelease && stopTimer.TimeElapsed > tpwsStopDelay * 1000)
				{
					Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches + 1, "Brake application demanded by the LU Trainstop system.");
					brakeDemandIndicatorLit = true;
					tpwsTrainstop = false;
					tpwsRelease = false;
				}
			}

			if (overrideTimer.TimerActive)
			{
				overrideTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
				if(overrideTimer.TimeElapsed > tpwsOverrideLifetime * 1000)
				{
					overrideTimer.TimerActive = false;
					overrideTimer.TimeElapsed = 0;
					stopOverrideIndicatorLit = false;
				}
			}

			//Set panel
			if (awsIndicator != -1)
			{
				Train.Panel[awsIndicator] = awsIndicatorLit ? 1 : 0;
			}
			if (brakeDemandIndicator != -1)
			{
				Train.Panel[brakeDemandIndicator] = brakeDemandIndicatorLit ? 1 : 0;
			}
			if (stopOverrideIndicator != -1)
			{
				Train.Panel[stopOverrideIndicator] = stopOverrideIndicatorLit ? 1 : 0;
			}

			if (awsKeyIndicator != -1)
			{
				Train.Panel[awsKeyIndicator] = awsKeyPressed ? 1 : 0;
			}
		}

		internal override void SetBeacon(BeaconData beacon)
		{
			if(beacon.Type == 44003 && tpwsTrainstop == false) //trainstop
			{
				if(overrideTimer.TimerActive)
				{
					overrideTimer.TimerActive = false;
					overrideTimer.TimeElapsed = 0;
					stopOverrideIndicatorLit = false;
				}
				else if(beacon.Signal.Aspect <= 0)
				{
					//BlinkIndicator(brakeDemand, 1);
					SoundManager.Play(tpwsWarningSound, 1.0, 1.0, true);
					Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches + 1, "Brake application demanded by the LU Trainstop system.");
					tpwsTrainstop = true;
					stopTimer.TimerActive = false;
					stopTimer.TimeElapsed = 0;
					tpwsRelease = false;
				}
			}
		}

		internal override void KeyDown(VirtualKeys key)
		{
			if (key == Train.CurrentKeyConfiguration.AWSKey)
			{
				awsKeyPressed = true;
		
				if(tpwsStartupTest == 2)
				{
					awsIndicatorLit = true;
					SoundManager.Stop(awsWarningSound);
				
					if(startupTest == 2)
					{
						SoundManager.Stop(awsClearSound);
						brakeDemandIndicatorLit = false;
						stopOverrideIndicatorLit = false;
						tpwsIndicator3Lit = false;
					}
					tpwsStartupTest = 0;
				}
				else if(awsTimer.TimerActive && awsTimer.TimeElapsed != -2)
				{
					awsIndicatorLit = true;
					SoundManager.Stop(awsWarningSound);
					awsTimer.TimerActive = false;
				}
				else if(awsTimer.TimeElapsed != -2 && (Train.CurrentSpeed == 0 || awsBrakeCancel != 0) && awsStop)
				{
					if (awsBrakeCancel == 1)
					{
						awsRelease = true;
					}
					else
					{
						Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches + 1, "Brake application demanded by the LU Trainstop system self-test.");
						awsIndicatorLit = false;
						SoundManager.Stop(awsWarningSound);
						awsTimer.TimerActive = false;
						awsStop = false;
					}
				}

				if(key == Train.CurrentKeyConfiguration.SafetyKey && tpwsTrainstop)
				{
					brakeDemandIndicatorLit = true;
					if(tpwsBrakeCancel != 0 || (Train.CurrentSpeed == 0 && stopTimer.TimeElapsed > tpwsStopDelay* 1000))
					{
						if (tpwsBrakeCancel == 1)
						{
							tpwsRelease = true;
						}
						else
						{
							Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches + 1, "EB Brake application demanded by the LU Trainstop system.");
							brakeDemandIndicatorLit = false;
							tpwsTrainstop = false;
						}

						SoundManager.Stop(tpwsWarningSound);
					}
				}
	
				if(key == Train.CurrentKeyConfiguration.TPWSOverride && tpwsTrainstop == false && tpwsStartupTest == 0)
				{
					if(overrideTimer.TimerActive == false)
					{
						overrideTimer.TimeElapsed = 0;
						//BlinkIndicator(stopOverride, 1);
					}
					else
					{
						overrideTimer.TimerActive = false;
						stopOverrideIndicatorLit = false;
					}
				}
			}
		}

		internal override void KeyUp(VirtualKeys key)
		{
			if (key == Train.CurrentKeyConfiguration.AWSKey)
			{
				awsKeyPressed = false;
			}
		}

		internal override void SetReverser(int reverser)
		{
			if (reverser == 0)
			{
				return;
			}
			SoundManager.Play(awsWarningSound, 1.0, 1.0, true);
			if(startupTest == 2)
			{
				brakeDemandIndicatorLit = true;
				stopOverrideIndicatorLit = true;
				tpwsIndicator3Lit = true;
			}
			tpwsStartupTest = 2;
		}
	}
}
