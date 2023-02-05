namespace Plugin
{
	class CylinderCocks : Component
	{
		/// <summary>The base pressure use when the cylinder cocks are active</summary>
		internal double BasePressureUse;
		/// <summary>The pressure use per notch of power when the cylinder cocks are active</summary>
		internal double NotchPressureUse;
		
		internal void Update(double TimeElapsed, ref double BoilerPressure)
		{
			//Set panel index
			if (PanelIndex != -1)
			{
				this.Train.Panel[PanelIndex] = Active ? 1 : 0;
			}
			//Sounds
			if (Active)
			{
				if (TogglePlayed == false)
				{
					SoundManager.Play(PlayOnceSound, 2.0, 1.0, false);
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

			if (!Active)
			{
				return;
			}
			BoilerPressure -= TimeElapsed * (BasePressureUse + (NotchPressureUse * Train.Handles.PowerNotch));
		}

		internal CylinderCocks(Train train)
		{
			this.Train = train;
		}
	}
}
