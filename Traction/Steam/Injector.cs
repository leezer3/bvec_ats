namespace Plugin
{
	class Injector : Component
	{
		/// <summary>The type of injector</summary>
		internal InjectorType Type;
		/// <summary>The number of water units added per second</summary>
		internal double WaterRate;
		/// <summary>The number of steam units added per second</summary>
		internal double SteamRate;
		/// <summary>The minimum pressure required for the injector to function</summary>
		internal double MinimumPressure;
		/// <summary>The minimum power notch (If Exhaust Steam Injector)</summary>
		internal int MinimumPowerNotch;
		

		internal void Update(double TimeElapsed, ref double BoilerWaterLevel, ref double BoilerPressure, ref double TanksWaterLevel)
		{
			//Set panel index
			if (PanelIndex != -1)
			{
				Train.Panel[PanelIndex] = Active ? 1 : 0;
			}

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
						if (!SoundManager.IsPlaying(PlayOnceSound) || PlayOnceSound == -1)
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
			
			if (Active == false)
			{
				return;
			}
			var waterRate = WaterRate / TimeElapsed;
			var steamRate = SteamRate / TimeElapsed;
			bool fail = false;
			if (TanksWaterLevel - waterRate <= 0)
			{
				waterRate = TanksWaterLevel;
				fail = true;
			}
			else
			{
				
			}
			switch (Type)
			{
				case InjectorType.ExhaustSteam:
					if (Train.CurrentSpeed > 10 || Train.Handles.PowerNotch > MinimumPowerNotch)
					{
						BoilerWaterLevel += waterRate;
						TanksWaterLevel -= waterRate;
					}
					break;
				case InjectorType.LiveSteam:
					if (BoilerPressure > MinimumPressure)
					{
						BoilerWaterLevel += waterRate;
						BoilerPressure -= steamRate;
						TanksWaterLevel -= waterRate;
					}
					break;
				case InjectorType.Failed:
					//Failed injectors still use steam, but give no water
					//TODO: Multiple failure types (No water or steam, proportionate)
					BoilerPressure -= steamRate;
					break;
			}
			if (fail)
			{
				Type = InjectorType.Failed;
			}
		}

		/// <summary>Creates a new instance of this class</summary>
		/// <param name="train">The root train</param>
		/// <param name="type">The type of injector</param>
		internal Injector(Train train, InjectorType type)
		{
			this.Type = type;
			this.Train = train;
			this.WaterRate = 100;
			this.SteamRate = 100;
		}
	}

	internal enum InjectorType
	{
		ExhaustSteam = 0,
		LiveSteam = 1,
		Failed = 2
	}
}
