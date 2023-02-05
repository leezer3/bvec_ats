using OpenBveApi.Runtime;

namespace Plugin
{
	/// <summary>Incomplete implementation of the F92 plugin</summary>
	internal class F92: Device
	{
		/// <summary>The underlying train.</summary>
		private readonly Train Train;

		internal F92(Train train)
		{
			this.Train = train;
		}

		internal override void Initialize(InitializationModes mode)
		{
		    CurrentSpeed = SelectorSpeeds.Neutral;
			CurrentDoorsSide = DoorsSide.None;
		}

		/// <summary>The currently selected sound index for the electronic loud-speaker</summary>
		internal int ELASoundIndex;

		internal bool CurveFlangeNoise;

		internal int CurveFlangeSound;

		internal bool TightCurveNoise;

		internal int TightCurveSound;

		internal int ThirdRailSound;

		internal int StraightSwitchSound;
		internal int DivergingSwitchSound;


		internal bool TripcockActive;

		internal bool SpeedTrapActivated;

		internal bool DeadmansDeviceActive;

		internal bool Overspeed;
		internal int OverspeedSound;

		internal int EmergencyBrakeCounter;

		internal bool PassedRedSignal;

		internal int NumberOfPassengers;

		internal DoorsSide CurrentDoorsSide;

		internal SelectorSpeeds CurrentSpeed;

		/// <summary>Defines the side on which the doors are to open</summary>
		internal enum DoorsSide
		{
			None = 0,
			Right = 1,
			Left = 2,
			
		}

		/// <summary>Defines the available speeds for the speed selector device</summary>
		internal enum SelectorSpeeds
		{
			/// <summary>25km/h backwards</summary>
			B25,
			/// <summary>15km/h backwards</summary>
			B15,
			/// <summary>Neutral</summary>
			Neutral,
			/// <summary>15km/h forwards</summary>
			F15,
			/// <summary>25km/h forwards</summary>
			F25,
			/// <summary>50km/h forwards</summary>
			F50,
			/// <summary>60km/h forwards</summary>
			F60,
			/// <summary>70km/h forwards</summary>
			F70

		}

		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data.</param>
		/// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
		internal override void Elapse(ElapseData data, ref bool blocking)
		{
			{
				if (TripcockActive)
				{
					if (DeadmansDeviceActive)
					{
						//We can accelerate!
						//If the throttle button is held down, then the train will accelerate towards the selected speed
						switch (CurrentSpeed)
						{
							case SelectorSpeeds.B25:
								data.Handles.Reverser = -1;
								if (Train.CurrentSpeed < 25 && Train.TractionManager.PowerCutoffDemanded == false)
								{
									data.Handles.PowerNotch = Train.Specs.PowerNotches;
								}
								else
								{
									data.Handles.PowerNotch = 0;
								}
								break;
							case SelectorSpeeds.B15:
								data.Handles.Reverser = -1;
								if (Train.CurrentSpeed < 15 && Train.TractionManager.PowerCutoffDemanded == false)
								{
									data.Handles.PowerNotch = Train.Specs.PowerNotches;
								}
								else
								{
									data.Handles.PowerNotch = 0;
								}
								break;
							case SelectorSpeeds.Neutral:
								data.Handles.Reverser = 0;
								data.Handles.PowerNotch = 0;
								break;
							case SelectorSpeeds.F15:
								data.Handles.Reverser = 1;
								if (Train.CurrentSpeed < 15 && Train.TractionManager.PowerCutoffDemanded == false)
								{
									data.Handles.PowerNotch = Train.Specs.PowerNotches;
								}
								else
								{
									data.Handles.PowerNotch = 0;
								}
								break;
							case SelectorSpeeds.F25:
								data.Handles.Reverser = 1;
								if (Train.CurrentSpeed < 25 && Train.TractionManager.PowerCutoffDemanded == false)
								{
									data.Handles.PowerNotch = Train.Specs.PowerNotches;
								}
								else
								{
									data.Handles.PowerNotch = 0;
								}
								break;
							case SelectorSpeeds.F50:
								data.Handles.Reverser = 1;
								if (Train.CurrentSpeed < 50 && Train.TractionManager.PowerCutoffDemanded == false)
								{
									data.Handles.PowerNotch = Train.Specs.PowerNotches;
								}
								else
								{
									data.Handles.PowerNotch = 0;
								}
								break;
							case SelectorSpeeds.F60:
								data.Handles.Reverser = 1;
								if (Train.CurrentSpeed < 60 && Train.TractionManager.PowerCutoffDemanded == false)
								{
									data.Handles.PowerNotch = Train.Specs.PowerNotches;
								}
								else
								{
									data.Handles.PowerNotch = 0;
								}
								break;
							case SelectorSpeeds.F70:
								data.Handles.Reverser = 1;
								if (Train.CurrentSpeed < 70 && Train.TractionManager.PowerCutoffDemanded == false)
								{
									data.Handles.PowerNotch = Train.Specs.PowerNotches;
								}
								else
								{
									data.Handles.PowerNotch = 0;
								}
								break;

						}

					}
					else
					{
						//The F92 has a counter for unwanted EB applications
						if (Train.TractionManager.BrakeInterventionDemanded == false)
						{
							EmergencyBrakeCounter++;
						}
						//If deadman's device is inactive, apply EB
						Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches + 1, "Brake application demanded by the F92");
						
					}
				}
				else
				{
					//If our tripcock is not currently turned on, then power must be cut off
					Train.TractionManager.DemandPowerCutoff("Power cutoff demanded by the F92 tripcock");
				}
				//Max speed device
				if (Train.CurrentSpeed > 70 && Overspeed == false)
				{
					//I'm presuming that the overspeed device will also up the EB counter, need to check this....
					EmergencyBrakeCounter++;
					Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches, "Brake application demanded by the F92 overspeed device");
					SoundManager.Play(OverspeedSound, 1.0, 1.0, true);
					Overspeed = true;
				}
				else
				{
					if (Overspeed)
					{
						//Only attempt to reset the brake application once, else this causes log spam
						Train.TractionManager.ResetBrakeApplication();
						SoundManager.Stop(OverspeedSound);
						Overspeed = false;
					}
				}
			}
			//Sounds
			{
				//If our trainspeed is greater than 50km/h then we should play the curve flange sound
				if (Train.CurrentSpeed > 50 && CurveFlangeNoise)
				{
					//Requires fade-in sound
					SoundManager.Play(CurveFlangeSound, 1.0, 1.0, true);
					
				}
				else
				{
					SoundManager.Stop(CurveFlangeSound);
					//Requires fade-out sound
				}
				//This sound is played whilst in tight curves (Rumbling noise) whilst the speed is above 25km/h
				if (Train.CurrentSpeed > 25 && TightCurveNoise)
				{
					//Requires fade-in sound
					SoundManager.Play(TightCurveSound, 1.0, 1.0, true);
				}
				else
				{
					//Requires fade-out sound
					SoundManager.Stop(CurveFlangeSound);
				}
			}
		}

		/// <summary>Call this function to attempt to activate the tripcock</summary>
		internal void Tripcock()
		{
			//Tripcock can only be activated below 7 km/h
			if (Train.CurrentSpeed < 7 && TripcockActive == false)
			{
				TripcockActive = true;
			}
		}

		internal void Trigger(int frequency, int data, int SignalAspect)
		{
			switch (frequency)
			{
					//Beacon 24 sets the side on which the doors are opened at the start of the route (If applicable)
				case 24:
					//Beacon 23 sets the side on which the doors are to open for the upcoming station (Ignores the BVE doors implementation entirely)
				case 23:
					CurrentDoorsSide = (DoorsSide)data;
					break;
					//Beacon 25 sets the number of passengers who are getting on at this station
					//I presume this is reflected in the stop time somewhere
				case 25:
					NumberOfPassengers = data;
					break;
					//Beacon 26 plays a speed dependant rail joint sound
				case 26:
					//Need to see how many rail joint sounds we have and what they do.
					//Assume that rail joint sounds have only been provided upto the 70km/h max of the train
					break;
					//Beacon 27 switches the ELA sound index
				case 27:
					ELASoundIndex = data;
					break;
					//Beacon 28 turns on or off the playing of curve flange sounds
				case 28:
					CurveFlangeNoise = !CurveFlangeNoise;
					break;
					//Beacon 29 turns on or off the playing of tight curve sounds
				case 29:
					TightCurveNoise = !TightCurveNoise;
					break;
					//Beacon 30 plays a sound when the third rail begins
				case 30:
					SoundManager.Play(ThirdRailSound, 1.0, 1.0, false);
					break;
					//Beacon 31 plays specific sound when passing over a point
				case 31:
					switch (data)
					{
						case 0:
							//Straight switch? NEEDS TESTING
							SoundManager.Play(StraightSwitchSound, 1.0, 1.0, false);
							break;
						case 1:
							//Curved switch? NEEDS TESTING
							SoundManager.Play(DivergingSwitchSound, 1.0, 1.0, false);
							break;
					}
					break;
					//44003 would appear to be a rehash of an AWS magnet
				case 44003:
					//If the signal is on ASPECT 1 (YELLOW EQUIV) or 2 (DOUBLE YELLOW EQUIV) & data is not zero, then data is the speed to check
					//Otherwise would appear to stop the train if the signal is on red
					if (data == 0)
					{
						if (SignalAspect == 0)
						{
							PassedRedSignal = true;
							Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches + 1, "Brake application demanded by the F92 passing a red signal");
						}
					}
					else
					{
						if (Train.CurrentSpeed > data)
						{
							SpeedTrapActivated = true;
							Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches + 1, "Braka application demanded by the F92 signal speed trap");
						}
					}
					break;
				case 44004:
					//Speed monitor only beacon, data parameter defines the speed in km/h
					if (Train.CurrentSpeed > data)
					{
						SpeedTrapActivated = true;
						Train.TractionManager.DemandBrakeApplication(Train.Specs.BrakeNotches + 1, "Brake application demanded by the F92 speed trap");
					}
					break;
			}
		}
	}
}
