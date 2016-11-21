namespace Plugin
{
	class OverheatAlarm : Component
	{
		internal double OverheatTemperature;
		internal double WarningTemperature;
		internal int WarningIndicator = -1;
		internal int WarningSound;
		internal OverheatResetBehaviour ResetBehaviour;
		internal OverheatState State;
		internal bool Warning;
		private bool CanReset;
		internal double ResetTime = 0;
		internal void Update(double TimeElapsed, double Temperature)
		{
			switch (State)
			{
				case OverheatState.NotOverheated:
					if (SoundManager.IsPlaying(LoopSound))
					{
						SoundManager.Stop(LoopSound);
					}
					if (Temperature > WarningTemperature)
					{
						State = OverheatState.OverheatWarning;
					}
					else if(Temperature > OverheatTemperature)
					{
						State = OverheatState.Overheated;
					}
					break;
				case OverheatState.OverheatWarning:
					if (SoundManager.IsPlaying(LoopSound))
					{
						SoundManager.Stop(LoopSound);
					}
					if (WarningSound != -1)
					{
						SoundManager.Play(WarningSound, 1.0, 1.0, true);
					}
					if (Temperature > OverheatTemperature)
					{
						State = OverheatState.Overheated;
					}
					break;
				case OverheatState.Overheated:
					if (!Train.TractionManager.PowerCutoffDemanded)
					{
						Train.TractionManager.DemandPowerCutoff();
					}
					if (SoundManager.IsPlaying(WarningSound))
					{
						SoundManager.Stop(WarningSound);
					}
					SoundManager.Play(LoopSound, 1.0, 1.0, true);
					if (Temperature < OverheatTemperature)
					{
						switch (ResetBehaviour)
						{
							case OverheatResetBehaviour.UnderTemperature:
								State = Temperature > WarningTemperature ? OverheatState.NotOverheated : OverheatState.OverheatWarning;
								Train.TractionManager.ResetPowerCutoff();
								break;
							case OverheatResetBehaviour.Timer:
							case OverheatResetBehaviour.TimerKeyPress:
								State = OverheatState.OverheatCooldown;
								break;
						}
					}
					break;
				case OverheatState.OverheatCooldown:
					Timer += TimeElapsed;
					if (Timer > ResetTime)
					{
						State = OverheatState.OverheatCooldownExpired;
					}
					break;
				case OverheatState.OverheatCooldownExpired:
					if (ResetBehaviour != OverheatResetBehaviour.TimerKeyPress || CanReset)
					{
						State = Temperature > WarningTemperature ? OverheatState.NotOverheated : OverheatState.OverheatWarning;
						CanReset = false;
						Train.TractionManager.ResetPowerCutoff();
					}
					break;
			}
			if (Active)
			{
				
			}
			//Panel indicies
			
			if (WarningIndicator != -1)
			{
				this.Train.Panel[WarningIndicator] = Temperature > WarningTemperature ? 1 : 0;
			}
			if (PanelIndex != -1)
			{
				this.Train.Panel[PanelIndex] = Temperature > WarningTemperature ? 1 : 0;
			}
			
			
		}

		internal void AttemptReset()
		{
			if (State == OverheatState.OverheatCooldownExpired)
			{
				CanReset = true;
			}
		}

		internal OverheatAlarm(Train train)
		{
			this.Train = train;
		}

		internal enum OverheatResetBehaviour
		{
			/// <summary>The overheat cutoff automatically resets when dropping below the overheat temperature</summary>
			UnderTemperature = 0,
			/// <summary>The overheat cutoff automatically resets N seconds after dropping below the overheat temperature</summary>
			Timer = 1,
			/// <summary>The overheat cutoff is reset when under temperature and the specified key is pressed</summary>
			KeyPress = 2,
			/// <summary>The overheat cutoff is reset when under temperature, N seconds have elapsed and the specified key is pressed</summary>
			TimerKeyPress = 3
		}

		internal enum OverheatState
		{
			NotOverheated = 0,
			OverheatWarning = 1,
			Overheated = 2,
			OverheatCooldown = 3,
			OverheatCooldownExpired = 4
		}
	}
}
