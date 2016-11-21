namespace Plugin
{
	class Blowers : Component
	{
		internal double PressureIncreaseFactor = 1;
		internal double FireTempIncreaseFactor = 1;

		internal void Update(double TimeElapsed)
		{
			//Panel index
			if (PanelIndex != -1)
			{
				this.Train.Panel[PanelIndex] = Active == true ? 1 : 0;
			}
			//Sounds
			if (LoopSound != -1)
			{
				if (Active == true)
				{
					if (TogglePlayed == false)
					{
						if (PlayOnceSound != -1)
						{
							SoundManager.Play(PlayOnceSound, 2.0, 1.0, false);
						}
						TogglePlayed = true;
					}
					else
					{
						if ((!SoundManager.IsPlaying(PlayOnceSound) || PlayOnceSound == -1) && Train.Handles.PowerNotch > 0)
						{
							SoundManager.Play(LoopSound, 2.0, 1.0, true);
						}
					}

				}
				else
				{
					TogglePlayed = false;
					if (SoundManager.IsPlaying(LoopSound))
					{
						SoundManager.Stop(LoopSound);
						if (PlayOnceSound != -1)
						{
							SoundManager.Play(PlayOnceSound, 2.0, 1.0, false);
						}
					}
				}
			}
			
		}

		internal Blowers(Train train)
		{
			this.Train = train;
		}
	}
}
