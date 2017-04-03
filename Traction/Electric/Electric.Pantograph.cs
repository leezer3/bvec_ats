namespace Plugin
{
	/// <summary>Represents a pantograph.</summary>
	internal class Pantograph
	{
		private readonly Train Train;
		/// <summary>The current state of this pantograph</summary>
		internal PantographStates State;
		/// <summary>Whether this pantograph is currently raised</summary>
		internal bool Raised;
		/// <summary>Whether line volts are available from this pantograph</summary>
		internal bool LineVoltsAvailable;

		private double Timer;
		/// <summary>The time interval in seconds before raising the pantograph can be attempted again</summary>
		internal double RetryInterval;
		/// <summary>The alarm sound played when this pantograph is lowered at speed</summary>
		internal int AlarmSound = -1;
		/// <summary>The sound index played when this pantograph is raised</summary>
		internal int RaisedSound = -1;
		/// <summary>The sound index played when this pantograph is lowered</summary>
		internal int LoweredSound = -1;

		internal AlarmBehaviour Behaviour;


		internal void Update(double TimeElapsed)
		{
			switch (State)
			{
				case PantographStates.RaisedTimer:
					Raised = true;
					Timer += TimeElapsed;
					if (Timer > 1000)
					{
						State = PantographStates.VCBReady;
					}
					break;
				case PantographStates.VCBResetTimer:
					Timer += TimeElapsed;
					if (Timer > RetryInterval)
					{
						State = PantographStates.Lowered;
						Timer = 0.0;
					}
					break;
				case PantographStates.VCBReady:
					if (Train.ElectricEngine.breakertripped == false)
					{
						State = PantographStates.OnService;
					}
					break;
				case PantographStates.RaisedVCBClosed:
					Train.ElectricEngine.TripBreaker();
					State = PantographStates.Lowered;
					break;
				case PantographStates.LoweredAtSpeed:
					Raised = false;
					switch (Behaviour)
					{
						case AlarmBehaviour.None:
							State = PantographStates.Lowered;
							break;
						case AlarmBehaviour.TripVCB:
							if (!Train.ElectricEngine.breakertripped)
							{
								Train.ElectricEngine.TripBreaker();
							}
							break;
						case AlarmBehaviour.ApplyBrakesTripVCB:
							if (AlarmSound != -1)
							{
								SoundManager.Play(AlarmSound, 1.0, 1.0, true);
							}
							if (!Train.ElectricEngine.breakertripped)
							{
								Train.ElectricEngine.TripBreaker();
							}
							Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches + 1);
							break;
					}
					break;
				case PantographStates.LoweredAtspeedBraking:
					if (Train.CurrentSpeed == 0)
					{
						Timer += TimeElapsed;
						if (Timer > RetryInterval)
						{
							State = PantographStates.Lowered;
							Timer = 0.0;
							if (AlarmSound != -1)
							{
								SoundManager.Stop(AlarmSound);
							}
						}
					}
					break;
			}
		}

		/// <summary>Raises this pantograph</summary>
		private void Raise()
		{
			if (Train.ElectricEngine.breakertripped == true)
			{
				if (RaisedSound != -1)
				{
					SoundManager.Play(RaisedSound, 1.0, 1.0, false);
				}
				//We can raise the pantograph, so start the line volts timer
				State = PantographStates.RaisedTimer;
				Train.DebugLogger.LogMessage("A pantograph was raised sucessfully");
			}
			else
			{
				State = PantographStates.RaisedVCBClosed;
				Train.DebugLogger.LogMessage("An attempt was made to raise a pantograph with the ACB/VCB closed");
			}
		}

		/// <summary>Lowers this pantograph</summary>
		private void Lower()
		{
			if (LoweredSound != -1)
			{
				SoundManager.Play(LoweredSound, 1.0, 1.0, false);
			}
			//Lower the pantograph
			if (Train.CurrentSpeed == 0)
			{
				State = PantographStates.Lowered;
				Raised = false;
				Train.DebugLogger.LogMessage("A pantograph was lowered sucessfully");
			}
			else
			{
				State = PantographStates.LoweredAtSpeed;
				Train.DebugLogger.LogMessage("A pantograph was lowered whilst the train was in motion");
			}
		}

		/// <summary>Toggles the state of this pantograph</summary>
		internal void ToggleState()
		{
			if (Raised)
			{
				Lower();
			}
			else
			{
				Raise();
			}
		}

		internal Pantograph(Train train)
		{
			this.Train = train;
		}

		internal enum AlarmBehaviour
		{
			None = 0,
			TripVCB = 1,
			ApplyBrakesTripVCB = 2
		}
	}


	/// <summary>Possible states of the pantograph.</summary>
	internal enum PantographStates
	{
		/// <summary>The pantograph is lowered and providing no current.</summary>
		Lowered = 0,
		/// <summary>The pantograph has been raised and the line volts timer is active.</summary>
		RaisedTimer = 1,
		/// <summary>The pantograph has been raised and the line volts timer has expired. The ACB/ VCB may now be closed</summary>
		VCBReady = 2,
		/// <summary>The pantograph has been raised with the ACB/ VCB closed.</summary>
		RaisedVCBClosed = 3,
		/// <summary>The pantograph VCB reset timer is running.</summary>
		VCBResetTimer = 4,
		/// <summary>The pantograph is on service & providing traction current.</summary>
		OnService = 5,
		/// <summary>The pantograph has been lowered at speed.</summary>
		LoweredAtSpeed = 6,
		/// <summary>The pantograph has been lowered at speed & brakes have been applied.</summary>
		LoweredAtspeedBraking = 7,
		/// <summary>The pantograph is disabled or not fitted.</summary>
		Disabled = 8,
	}
}