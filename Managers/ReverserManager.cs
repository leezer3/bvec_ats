namespace Plugin
{
	internal class ReverserManager
	{
		private readonly Train Train;

		internal ReverserManager(Train train)
		{
			this.Train = train;
			this.CurrentBehaviour = NeutralBehaviour.NoChange;
			this.CurrentResetBehaviour = ResetBehaviour.LeaveNeutral;
		}

		internal NeutralBehaviour CurrentBehaviour;
		internal ResetBehaviour CurrentResetBehaviour;
		internal bool Tripped;
		/// <summary>The alarm sound to play when the neutral reverser system trips</summary>
		internal int AlarmSound = -1;
		/// <summary>Whether the alarm sound is to be looped</summary>
		internal bool AlarmLooped;
		/// <summary>Whether the alarm sound has been played (if not looped)</summary>
		internal bool AlarmSoundPlayed;
		/// <summary>The panel index of the reverser alarm</summary>
		internal int AlarmPanelIndex = -1;

		internal void Update()
		{
			if (!Tripped)
			{
				if (SoundManager.IsPlaying(AlarmSound))
				{
					SoundManager.Stop(AlarmSound);
				}
				if (Train.Handles.Reverser != 0 || Train.CurrentSpeed == 0)
				{
					return;
				}
				switch (CurrentBehaviour)
				{
					case NeutralBehaviour.NoChange:
						return;
					case NeutralBehaviour.ServiceBrakes:
						if (Train.CurrentSpeed > 0)
						{
							Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches, "Brake application was demanded by the Neutral Reverser behaviour setting");
						}
						break;
					case NeutralBehaviour.EmergencyBrakes:
						if (Train.CurrentSpeed > 0)
						{
							Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches + 1, "Brake application was demanded by the Neutral Reverser behaviour setting");
						}
						break;
				}

			}
			if (AlarmSound != -1)
			{
				if (AlarmLooped && !SoundManager.IsPlaying(AlarmSound))
				{
					SoundManager.Play(AlarmSound, 1.0,1.0, true);
				}
				else if(!AlarmLooped && !AlarmSoundPlayed)
				{
					SoundManager.Play(AlarmSound, 1.0,1.0, false);
					AlarmSoundPlayed = true;
				}
			}
			switch (CurrentResetBehaviour)
			{
				case ResetBehaviour.LeaveNeutral:
					if (Train.Handles.Reverser != 0)
					{
						Train.TractionManager.ResetBrakeApplication();
					}
					break;
				case ResetBehaviour.FullStand:
					if (Train.CurrentSpeed == 0)
					{
						Train.TractionManager.ResetBrakeApplication();
					}
					break;
				case ResetBehaviour.FullStandServiceBrakes:
					if (Train.CurrentSpeed == 0 && Train.Handles.BrakeNotch == Train.Specs.BrakeNotches)
					{
						Train.TractionManager.ResetBrakeApplication();
					}
					break;
				case ResetBehaviour.FullStandEmergencyBrakes:
					if (Train.CurrentSpeed == 0 && Train.Handles.BrakeNotch == Train.Specs.BrakeNotches + 1)
					{
						Train.TractionManager.ResetBrakeApplication();
					}
					break;
			}

			if (AlarmPanelIndex != -1)
			{
				Train.Panel[AlarmPanelIndex] = Tripped ? 1 : 0;
			}

		}

		internal enum NeutralBehaviour
		{
			/// <summary>No change when the reverser is placed into neutral</summary>
			NoChange = 0,
			/// <summary>The service brakes are applied when the reverser is placed into neutral</summary>
			ServiceBrakes = 1,
			/// <summary>The emergency brakes are applied when the reverser is placed into neutral</summary>
			EmergencyBrakes = 2
		}

		internal enum ResetBehaviour
		{
			/// <summary>The brake application is reset when the driver returns the reverser from neutral</summary>
			LeaveNeutral = 1,
			/// <summary>The brake application is reset when the train reaches a full stand</summary>
			FullStand = 2,
			/// <summary>The brake application is reset when the train reaches a full stand & the driver applies service brakes</summary>
			FullStandServiceBrakes = 3,
			/// <summary>The brake application is reset when the train reaches a full stand & the driver applies EB brakes</summary>
			FullStandEmergencyBrakes = 4
		}
	}
}
