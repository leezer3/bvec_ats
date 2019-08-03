namespace Plugin
{
	class OverspeedMonitor
	{
		private readonly Train Train;
		/// <summary>The speed at which an overspeed warning will be triggered in km/h</summary>
		internal double WarningSpeed = double.MaxValue;
		/// <summary>The speed at which an overspeed intervention will be triggered in km/h</summary>
		internal double OverSpeed = 1000;
		/// <summary>The speed at which an overspeed intervention will automatically be cancelled in km/h</summary>
		internal double SafeSpeed = 0;
		/// <summary>The time for which you may be overspeed before an intervention is triggered</summary>
		internal double MaximumTimeOverspeed = 0;
		/// <summary>The panel index of the overspeed indicator</summary>
		internal int PanelIndicator = -1;
		/// <summary>The sound index of the audible overspeed alarm</summary>
		internal int AlarmSound;
		/// <summary>Whether the overspeed device has tripped</summary>
		internal bool Tripped;

		internal OverspeedBehaviour CurrentBehaviour;

		private double OverspeedTimer;
		internal void Update(double ElapsedTime)
		{
			if (Train.CurrentSpeed > OverSpeed && Tripped == false)
			{
				//Elapse the timer
				OverspeedTimer += ElapsedTime;
				if (OverspeedTimer > MaximumTimeOverspeed)
				{
					Tripped = true;
				}
				
			}

			if (Tripped && Train.TractionManager.BrakeInterventionDemanded == false)
			{
				switch (CurrentBehaviour)
				{
					case OverspeedBehaviour.None:
						Tripped = false;
						break;
					case OverspeedBehaviour.ApplyServiceBrakes:
						Train.TractionManager.DemandBrakeApplication(this.Train.Specs.BrakeNotches, "Brake application demanded by the overspeed device");
						break;
					case OverspeedBehaviour.ApplyEmergencyBrakes:
						Train.TractionManager.DemandBrakeApplication(this.Train.Specs.BrakeNotches + 1, "Brake application demanded by the overspeed device");
						break;
					case OverspeedBehaviour.CutoffPower:
						Train.TractionManager.DemandPowerCutoff("Power cutoff was demanded by an overspeed intervention");
						break;
				}
				return;
			}

			if (Train.CurrentSpeed <= SafeSpeed && Tripped)
			{
				if (Train.Vigilance.AutoRelease == true)
				{
					Tripped = false;
					Train.TractionManager.ResetBrakeApplication();
				}
			}

			if (PanelIndicator != -1)
			{
				Train.Panel[PanelIndicator] = Tripped == true || Train.CurrentSpeed > WarningSpeed ? 1 : 0;
			}
			if (AlarmSound != -1)
			{
				if (Tripped || Train.CurrentSpeed > WarningSpeed)
				{
					if (!SoundManager.IsPlaying(AlarmSound))
					{
						SoundManager.Play(AlarmSound,1.0,1.0,true);
					}
				}
				else
				{
					SoundManager.Stop(AlarmSound);
				}
			}

		}

		internal OverspeedMonitor(Train train)
		{
			this.Train = train;
		}

		internal enum OverspeedBehaviour
		{
			None = 0,
			ApplyServiceBrakes = 1,
			ApplyEmergencyBrakes = 2,
			CutoffPower = 3
		}

	}
}
