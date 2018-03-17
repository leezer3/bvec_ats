using System;
using OpenBveApi.Runtime;

namespace Plugin
{
	/// <summary>Represents an electric locomotive.</summary>
	internal partial class Electric : Device
	{

		// --- members ---

		/// <summary>The underlying train.</summary>
		private readonly Train Train;

		//Internal Variables
		internal double heatingtimer;
		internal double currentheat;
		internal double temperature;
		/// <summary>Stores whether we are currently in a power gap</summary>
		internal bool PowerGap;
		/// <summary>Stores the current state of the ACB/VCB</summary>
		internal bool BreakerTripped;
		/// <summary>Stores whether the power was cutoff by a legacy OS_ATS standard beacon</summary>
		internal bool LegacyPowerCut;
		internal int nextmagnet;
		internal int firstmagnet;
		internal int lastmagnet;


		internal double linevoltstimer;

		internal double powerlooptimer;
		internal bool powerloop;
		internal double breakerlooptimer;

		internal bool electricPowerCutoff;

		/// <summary>The current state of the front pantograph</summary>
		internal Pantograph FrontPantograph;
		/// <summary>The current state of the rear pantograph</summary>
		internal Pantograph RearPantograph;
		/// <summary>The ammeter</summary>
		internal Ammeter Ammeter;
		/// <summary>The behaviour of the train in a powergap</summary>
		internal PowerGapBehaviour PowerGapBehaviour = PowerGapBehaviour.NoEffect;
		/// <summary>The retry interval after an unsucessful attempt to raise the pantograph, or it has been lowered with the ACB/VCB closed</summary>
		internal double pantographretryinterval = 5000;
		/// <summary>The behaviour when a pantograph is lowered with the ACB/VCB closed</summary>
		internal int pantographalarmbehaviour = 0;
		/// <summary>The time before the power notch loop sound is started afer each notch change in ms</summary>
		internal double powerlooptime = 0;
		/// <summary>The time before the breaker loop sound is started after the ACB/VCB is closed with a pantograph available</summary>
		internal double breakerlooptime = 0;
		/// <summary>Do we heave a part that heats up?</summary>
		internal int heatingpart = 0;
		/// <summary>The overheat warning temperature</summary>
		internal double overheatwarn = 0;
		/// <summary>The temperature at which the engine overheats</summary>
		internal double overheat = 0;
		/// <summary>What happens when we overheat?</summary>
		internal int overheatresult = 0;
		/// <summary>The panel index of the line volts indicator</summary>
		internal int powerindicator = -1;
		/// <summary>The panel index of the ACB/VCB</summary>
		internal int breakerindicator = -1;
		/// <summary>The panel indicator for the thermometer</summary>
		internal int thermometer = -1;
		/// <summary>The panel indicator for the overheat indicator</summary>
		internal int overheatindicator = -1;

		//Sound Indicies

		/// <summary>The sound index played when the ACB/VCB is closed</summary>
		internal int breakersound = -1;

		/// <summary>The power notch loop sound index</summary>
		internal int powerloopsound = -1;
		/// <summary>The breaker loop sound index</summary>
		internal int breakerloopsound = -1;
		/// <summary>Sound index for overheat alarm</summary>
		internal int overheatalarm = -1;

		//Arrays
		/// <summary>An array storing the location of all available pickup points</summary>
		internal int[] PickupLocations = { 0 };
		/// <summary>A list of the heating rates (in heat units per second) for each power notch</summary>
		internal int[] HeatingRates = { 0 };

		/// <summary>The speed at which the pantograph will be automatically lowered</summary>
		internal double AutomaticPantographLowerSpeed = Double.MaxValue;
		/// <summary>The mode by which the pantograph is automatically lowered</summary>
		internal AutomaticPantographLoweringModes PantographLoweringMode = AutomaticPantographLoweringModes.NoAction;

		// --- constructors ---

		/// <summary>Creates a new instance of this system.</summary>
		/// <param name="train">The train.</param>
		internal Electric(Train train)
		{
			this.Train = train;
			FrontPantograph = new Pantograph(train);
			RearPantograph = new Pantograph(train);
			Ammeter = new Ammeter(train);
		}

		//<param name="mode">The initialization mode.</param>
		internal override void Initialize(InitializationModes mode)
		{
			//Set starting pantograph states
			//If neither pantograph has a key assigned, set both to enabled
			if (Train.CurrentKeyConfiguration.FrontPantograph == null && Train.CurrentKeyConfiguration.RearPantograph == null)
			{
				BreakerTripped = false;
				FrontPantograph.Raised = true;
				FrontPantograph.State = PantographStates.OnService;
				RearPantograph.Raised = true;
				RearPantograph.State = PantographStates.OnService;
			}
			//On service- Set the enabled pantograph(s) to the OnService state
			//Set the ACB/ VCB to closed
			else if (mode == InitializationModes.OnService)
			{
				BreakerTripped = false;
				if (Train.CurrentKeyConfiguration.FrontPantograph == null && Train.CurrentKeyConfiguration.RearPantograph != null)
				{
					//Rear pantograph only is enabled
					FrontPantograph.Raised = false;
					FrontPantograph.State = PantographStates.Disabled;
					RearPantograph.Raised = true;
					RearPantograph.State = PantographStates.OnService;
				}
				else if(Train.CurrentKeyConfiguration.FrontPantograph != null && Train.CurrentKeyConfiguration.RearPantograph == null)
				{
					//Front pantograph only is enabled
					FrontPantograph.Raised = true;
					FrontPantograph.State = PantographStates.OnService;
					RearPantograph.Raised = false;
					RearPantograph.State = PantographStates.Disabled;
				}
				else
				{
					FrontPantograph.Raised = true;
					FrontPantograph.State = PantographStates.OnService;
					RearPantograph.Raised = true;
					RearPantograph.State = PantographStates.OnService;
				}
			}
			//Not on service- Set the enabled pantograph(s) to the lowered state
			//Set the ACB/ VCB to open
			else
			{
				BreakerTripped = true;
				if (Train.CurrentKeyConfiguration.FrontPantograph == null && Train.CurrentKeyConfiguration.RearPantograph != null)
				{
					//Rear pantograph only is enabled
					FrontPantograph.Raised = false;
					FrontPantograph.State = PantographStates.Disabled;
					RearPantograph.Raised = false;
					RearPantograph.State = PantographStates.Lowered;
				}
				else if(Train.CurrentKeyConfiguration.FrontPantograph != null && Train.CurrentKeyConfiguration.RearPantograph == null)
				{
					//Front pantograph only is enabled
					FrontPantograph.Raised = false;
					FrontPantograph.State = PantographStates.Lowered;
					RearPantograph.Raised = false;
					RearPantograph.State = PantographStates.Disabled;
				}
				else
				{
					FrontPantograph.Raised = false;
					FrontPantograph.State = PantographStates.Lowered;
					RearPantograph.Raised = false;
					RearPantograph.State = PantographStates.Lowered;
				}
			}

		}



		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data.</param>
		/// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
		internal override void Elapse(ElapseData data, ref bool blocking)
		{
			//Required to reset the max notch before each frame
			this.Train.TractionManager.SetMaxPowerNotch(this.Train.Specs.PowerNotches, false);
			//Check we've got a maximum temperature and a heating part
			if (overheat != 0 && heatingpart != 0)
			{
				this.heatingtimer += data.ElapsedTime.Milliseconds;
				if (heatingpart == 0 || overheat == 0)
				{
					//No heating part or overheat temperature not set
					this.temperature = 0.0;
					this.heatingtimer = 0.0;
				}
				else if (heatingpart == 1)
				{
					//Heats based upon power notch
					if (this.heatingtimer > 1000)
					{
						this.heatingtimer = 0.0;
						if (Train.Handles.PowerNotch == 0)
						{
							currentheat = HeatingRates[0];
						}
						else if (Train.Handles.PowerNotch < HeatingRates.Length)
						{
							currentheat = HeatingRates[Train.Handles.PowerNotch];
						}
						else
						{
							currentheat = HeatingRates[HeatingRates.Length - 1];
						}
						temperature += currentheat;
					}
				}
				else
				{
					//Heats based upon RPM- Not on an electric loco!
					this.temperature = 0.0;
					this.heatingtimer = 0.0;
				}

				//Keep temperature below max & above zero
				if (temperature > overheat)
				{
					temperature = overheat;
					if (overheatresult == 1 && Train.TractionManager.EngineOverheated == false)
					{
						DemandPowerCutoff("Power cutoff was demanded due to the electric engine overheating");
						Train.TractionManager.EngineOverheated = true;
					}
				}
				else if (temperature < overheat && temperature > 0)
				{
					if (BreakerTripped == false && ((FrontPantograph.State == PantographStates.Disabled && RearPantograph.State == PantographStates.OnService) 
						|| (RearPantograph.State == PantographStates.Disabled && FrontPantograph.State == PantographStates.OnService)))
					{
						ResetPowerCutoff("Power cutoff was released due to the electric engine temperature returning to safe levels");
					}
					Train.TractionManager.EngineOverheated = false;
				}
				else if (temperature < 0)
				{
					temperature = 0;
				}
			}

			{
				
				//If we're in a power gap, check whether we have a pickup available
				if (PowerGap)
				{
					int new_power;
					//First check to see whether the first pickup is in the neutral section
					if (Train.TrainLocation - PickupLocations[0] > firstmagnet && Train.TrainLocation - PickupLocations[0] < nextmagnet)
					{
						//Cycle through the other pickups
						int j = 0;
						foreach (int t in PickupLocations)
						{
							if (Train.TrainLocation - t < firstmagnet)
							{
								j++;
							}
						}

						switch (PowerGapBehaviour)
						{
							case PowerGapBehaviour.ProportionalReduction:
								//Reduce max throttle by percentage of how many pickups are in the gap
								double throttlemultiplier = (double)j / (double)PickupLocations.Length;
								new_power = (int)(this.Train.Specs.PowerNotches * throttlemultiplier);
								this.Train.TractionManager.SetMaxPowerNotch(new_power, false);
								//data.Handles.PowerNotch = new_power;
								break;
							case PowerGapBehaviour.InactiveAny:
								//Kill traction power if any pickup is on the gap
								if (j != 0 && Train.TractionManager.PowerCutoffDemanded == false)
								{
									DemandPowerCutoff("Power cutoff was demanded due to a neutral gap in the overhead line");
								}

								break;
							case PowerGapBehaviour.InactiveAll:
								if (j == PickupLocations.Length && Train.TractionManager.PowerCutoffDemanded == false)
								{
									DemandPowerCutoff("Power cutoff was demanded due to a neutral gap in the overhead line");
								}
								break;
						}
					}
					//Now, check to see if the last pickup is in the neutral section
					else if (Train.TrainLocation - PickupLocations[PickupLocations.Length - 1] > firstmagnet && Train.TrainLocation - PickupLocations[PickupLocations.Length - 1] < nextmagnet)
					{
						//Cycle through the other pickups
						int j = 0;
						foreach (int t in PickupLocations)
						{
							if (Train.TrainLocation - t < firstmagnet)
							{
								j++;
							}
						}
						switch (PowerGapBehaviour)
						{
							case PowerGapBehaviour.ProportionalReduction:
								//Reduce max throttle by percentage of how many pickups are in the gap
								double throttlemultiplier = (double)j / (double)PickupLocations.Length;
								new_power = (int)(this.Train.Specs.PowerNotches * throttlemultiplier);
								this.Train.TractionManager.SetMaxPowerNotch(new_power, false);
								//data.Handles.PowerNotch = new_power;
								break;
							case PowerGapBehaviour.InactiveAny:
								//Kill traction power if any pickup is on the gap
								if (j != 0 && Train.TractionManager.PowerCutoffDemanded == false)
								{
									DemandPowerCutoff("Power cutoff was demanded due to a neutral gap in the overhead line");
								}

								break;
							case PowerGapBehaviour.InactiveAll:
								//Kill traction power when all pickups are on the gap
								if (j == PickupLocations.Length && Train.TractionManager.PowerCutoffDemanded == false)
								{
									DemandPowerCutoff("Power cutoff was demanded due to a neutral gap in the overhead line");
								}
								break;
						}
					}
					//Neither the first or last pickups are in the power gap, reset the power
					//However also check that the breaker has not been tripped by a UKTrainSys beacon
					else if (nextmagnet != 0 && (Train.TrainLocation - PickupLocations[PickupLocations.Length - 1]) > nextmagnet && (Train.TrainLocation - PickupLocations[0]) > nextmagnet)
					{
						PowerGap = false;
						if (LegacyPowerCut == true)
						{
							//Reset legacy power cutoff state and retrip breaker
							Train.ElectricEngine.TripBreaker();
							LegacyPowerCut = false;
						}
						if (BreakerTripped == false && ((FrontPantograph.State == PantographStates.Disabled && RearPantograph.State == PantographStates.OnService) || (RearPantograph.State == PantographStates.Disabled && FrontPantograph.State == PantographStates.OnService)))
						{
							ResetPowerCutoff("Power cutoff was released due to leaving the neutral gap");
						}
					}
					//If the final pickup has passed the UKTrainSys standard power gap location
					else if (Train.TrainLocation - PickupLocations[PickupLocations.Length - 1] < lastmagnet)
					{
						PowerGap = false;
					}
				}
				//This section of code handles a UKTrainSys compatible ACB/VCB
				//
				//If the ACB/VCB has tripped, always demand power cutoff
				if (BreakerTripped == true && Train.TractionManager.PowerCutoffDemanded == false)
				{
					DemandPowerCutoff("Power cutoff was demanded due to the ACB/VCB state");
				}
				//If we're in a power gap, also always demand power cutoff
				else if (BreakerTripped == false && PowerGap == true && Train.TractionManager.PowerCutoffDemanded == false)
				{
					DemandPowerCutoff("Power cutoff was demanded due to a neutral gap in the overhead line");
				}
				//If the ACB/VCB has now been reset with a pantograph available & we're not in a powergap reset traction power
				if (BreakerTripped == false && PowerGap == false && (FrontPantograph.State == PantographStates.OnService || RearPantograph.State == PantographStates.OnService))
				{
					ResetPowerCutoff("Power cutoff was released due to the availability of overhead power");
				}
			}
			{
				//This section of code handles raising the pantographs and the alarm state
				//
				//If both pantographs are lowered or disabled, then there are no line volts
				if ((FrontPantograph.State == PantographStates.Lowered || FrontPantograph.State == PantographStates.Disabled) &&
				(RearPantograph.State == PantographStates.Lowered || RearPantograph.State == PantographStates.Disabled) && Train.TractionManager.PowerCutoffDemanded == false)
				{
					DemandPowerCutoff("Power cutoff was demanded due to no available pantographs");
					PowerGap = true;
				}
				//If the powergap behaviour cuts power when *any* pantograph is disabled / lowered
				//
				//Line volts is lit, but power is still cut off
				else if (FrontPantograph.State != PantographStates.Disabled && RearPantograph.State != PantographStates.Disabled
				&& (FrontPantograph.State != PantographStates.OnService || RearPantograph.State != PantographStates.OnService) && PowerGapBehaviour == PowerGapBehaviour.InactiveAny && Train.TractionManager.PowerCutoffDemanded == false)
				{
					DemandPowerCutoff("Power cutoff was demanded due to no available pantographs");
				}
				

				//Pantographs
				//One pantograph has been raised, and the line volts indicator has lit, but the timer is active
				//Power cutoff is still in force
				FrontPantograph.Update(data.ElapsedTime.Milliseconds);
				RearPantograph.Update(data.ElapsedTime.Milliseconds);
				if ((FrontPantograph.State == PantographStates.RaisedTimer && RearPantograph.State != PantographStates.OnService) || (RearPantograph.State == PantographStates.RaisedTimer && FrontPantograph.State != PantographStates.OnService))
				{
					PowerGap = false;
					DemandPowerCutoff(null);
				}

				if (Train.CurrentSpeed > AutomaticPantographLowerSpeed)
				{
					//Automatic pantograph lowering
					switch (PantographLoweringMode)
					{
						case AutomaticPantographLoweringModes.LowerAll:
							//In lower all mode, we don't need to check anything
							if (FrontPantograph.State == PantographStates.OnService || FrontPantograph.State == PantographStates.RaisedTimer || FrontPantograph.State == PantographStates.VCBReady)
							{
								FrontPantograph.Lower(true);
							}
							if (RearPantograph.State == PantographStates.OnService || RearPantograph.State == PantographStates.RaisedTimer || RearPantograph.State == PantographStates.VCBReady)
							{
								RearPantograph.Lower(true);
							}
							Train.DebugLogger.LogMessage("Automatically lowered all pantographs due to reaching the setspeed.");
							break;
						case AutomaticPantographLoweringModes.LowerFront:
							if (RearPantograph.State == PantographStates.OnService)
							{
								if (FrontPantograph.State == PantographStates.OnService || FrontPantograph.State == PantographStates.RaisedTimer || FrontPantograph.State == PantographStates.VCBReady)
								{
									FrontPantograph.Lower(true);
									Train.DebugLogger.LogMessage("Automatically lowered the front pantograph due to reaching the setspeed.");
								}
							}
							break;
						case AutomaticPantographLoweringModes.LowerRear:
							if (FrontPantograph.State == PantographStates.OnService)
							{
								if (RearPantograph.State == PantographStates.OnService || RearPantograph.State == PantographStates.RaisedTimer || RearPantograph.State == PantographStates.VCBReady)
								{
									RearPantograph.Lower(true);
									Train.DebugLogger.LogMessage("Automatically lowered the rear pantograph due to reaching the setspeed.");
								}
							}
							break;
						case AutomaticPantographLoweringModes.LowerFrontRegardless:
							if (FrontPantograph.State == PantographStates.OnService || FrontPantograph.State == PantographStates.RaisedTimer || FrontPantograph.State == PantographStates.VCBReady)
							{
								FrontPantograph.Lower(true);
								Train.DebugLogger.LogMessage("Automatically lowered the front pantograph due to reaching the setspeed.");
							}
							break;
						case AutomaticPantographLoweringModes.LowerRearRegardless:
							if (RearPantograph.State == PantographStates.OnService || RearPantograph.State == PantographStates.RaisedTimer || RearPantograph.State == PantographStates.VCBReady)
							{
								RearPantograph.Lower(true);
								Train.DebugLogger.LogMessage("Automatically lowered the rear pantograph due to reaching the setspeed.");
							}
							break;

					}
				}


			}

			//This section of code runs the power notch loop sound
			if (powerloopsound != -1 && data.Handles.PowerNotch != 0)
			{
				if (BreakerTripped == false)
				{
					//Start the timer
					powerlooptimer += data.ElapsedTime.Milliseconds;
					if (powerlooptimer > powerlooptime && powerloop == false)
					{
						//Start playback and reset our conditions
						powerloop = true;
						SoundManager.Play(powerloopsound, 1.0, 1.0, true);
					}
					else if (powerloop == false)
					{
						SoundManager.Stop(powerloopsound);
					}
				}
				else
				{
					SoundManager.Stop(powerloopsound);
				}
			}
			else if (powerloopsound != -1 && this.Train.Handles.PowerNotch == 0)
			{
				SoundManager.Stop(powerloopsound);
			}
			//This section of code runs the breaker loop sound
			if (breakerloopsound != -1 && BreakerTripped == false)
			{
				if (!PowerGap && SoundManager.IsPlaying(breakerloopsound) == false)
				{
					breakerlooptimer += data.ElapsedTime.Milliseconds;
					if (breakerlooptimer > breakerlooptime)
					{
						SoundManager.Play(breakerloopsound, 1.0, 1.0, true);
						breakerlooptimer = 0.0;
					}
				}
			}
			else if (breakerloopsound != -1 && BreakerTripped == true)
			{
				SoundManager.Stop(breakerloopsound);
				breakerlooptimer = 0.0;
			}

			//Panel Indicies
			{
				//Ammeter
				if (Ammeter.PanelIndex != -1)
				{
					if(PowerGap == true || BreakerTripped == true || Train.TractionManager.PowerCutoffDemanded == true)
					{
						this.Train.Panel[Ammeter.PanelIndex] = 0;
					}
					else
					{
						this.Train.Panel[Ammeter.PanelIndex] = Ammeter.GetCurrentValue();
					}
				}
				//Line Volts indicator
				if (powerindicator != -1)
				{
					if (!PowerGap)
					{
						this.Train.Panel[powerindicator] = 1;
					}
					else
					{
						this.Train.Panel[powerindicator] = 0;
					}
				}
				//ACB/VCB Breaker Indicator
				if (breakerindicator != -1)
				{
					if (!BreakerTripped)
					{
						this.Train.Panel[breakerindicator] = 1;
					}
					else
					{
						this.Train.Panel[breakerindicator] = 0;
					}
				}
				if (thermometer != -1)
				{
					this.Train.Panel[(thermometer)] = (int)temperature;
				}
				if (overheatindicator != -1)
				{
					if (temperature > overheatwarn)
					{
						this.Train.Panel[(overheatindicator)] = 1;
					}
					else
					{
						this.Train.Panel[(overheatindicator)] = 0;
					}
				}
				//Pantograph Indicators
				if (FrontPantograph.PanelIndex != -1)
				{
					if (FrontPantograph.Raised == true)
					{
						this.Train.Panel[FrontPantograph.PanelIndex] = 1;
					}
					else
					{
						this.Train.Panel[FrontPantograph.PanelIndex] = 0;
					}
				}
				if (RearPantograph.PanelIndex != -1)
				{
					if (RearPantograph.Raised == true)
					{
						this.Train.Panel[RearPantograph.PanelIndex] = 1;
					}
					else
					{
						this.Train.Panel[RearPantograph.PanelIndex] = 0;
					}
				}
			}
			//Sounds
			{
				if (overheatalarm != -1)
				{
					if (temperature > overheatalarm)
					{
						SoundManager.Play(overheatalarm, 1.0, 1.0, true);
					}
					else
					{
						SoundManager.Stop(overheatalarm);
					}
				}
			}
			//Pass information to the advanced driving window
			if (AdvancedDriving.CheckInst != null)
			{
				TractionManager.debuginformation[14] = Convert.ToString(FrontPantograph.State);
				TractionManager.debuginformation[15] = Convert.ToString(RearPantograph.State);
				TractionManager.debuginformation[16] = Convert.ToString(!BreakerTripped);
				TractionManager.debuginformation[17] = Convert.ToString(!PowerGap);
			}
			
		}

		//This function handles legacy power magnets- Set the electric powergap to true & set the end magnet location
		internal void LegacyPowerCutoff(int trainLocation,int magnetDistance)
		{
			if (!Train.ElectricEngine.PowerGap)
			{
				Train.ElectricEngine.PowerGap = true;
				firstmagnet = trainLocation;
				nextmagnet = trainLocation + magnetDistance;
				LegacyPowerCut = true;
				Train.ElectricEngine.TripBreaker();
			}
		}

		//This function handles UKTrainSYS compatible dual magnets
		//Set the next magnet to false
		internal void newpowercutoff(int trainlocation)
		{
			if (!Train.ElectricEngine.PowerGap)
			{
				Train.ElectricEngine.PowerGap = true;
				firstmagnet = trainlocation;
				nextmagnet = 0;
			}
			else
			{
				Train.ElectricEngine.PowerGap = false;
				lastmagnet = trainlocation;
			}
		}

		//Toggle the ACB/VCB based upon it's state
		internal void TripBreaker()
		{
			if (!BreakerTripped)
			{
				
				BreakerTripped = true;
				powerloop = false;
				if (breakersound != -1)
				{
					SoundManager.Play(breakersound, 1.0, 1.0, false);
				}
				Train.TractionManager.DemandPowerCutoff();
				Train.DebugLogger.LogMessage("The ACB/VCB was opened");
				Train.DebugLogger.LogMessage("Power cutoff was demanded due to an open ACB/VCB");
			}
			else
			{
				
				BreakerTripped = false;
				if (breakersound != -1)
				{
					SoundManager.Play(breakersound, 1.0, 1.0, false);
				}
				Train.DebugLogger.LogMessage("The ACB/VCB was closed");
				Train.TractionManager.ResetPowerCutoff();
			}
		}

		//Raises & lowers the pantographs
		internal void PantographToggle(int pantograph)
		{
			if (pantograph == 0)
			{
				FrontPantograph.ToggleState();
			}
			else
			{
				RearPantograph.ToggleState();
			}
		}

		/// <summary>Attempts to reset electric traction power</summary>
		/// <param name="Message">The message to log</param>
		internal void ResetPowerCutoff(string Message)
		{
			if (electricPowerCutoff == true)
			{
				Train.DebugLogger.LogMessage(Message);
				Train.TractionManager.ResetPowerCutoff();
				electricPowerCutoff = false;
			}
		}

		/// <summary>Attempts to reset electric traction power</summary>
		/// <param name="Message">The message to log</param>
		internal void DemandPowerCutoff(string Message)
		{
			if (electricPowerCutoff == false)
			{
				if (Message != null)
				{
					Train.DebugLogger.LogMessage(Message);
				}
				Train.TractionManager.DemandPowerCutoff();
				electricPowerCutoff = true;
			}
		}
	}
}