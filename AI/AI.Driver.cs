﻿using OpenBveApi.Runtime;

namespace Plugin.AI
{
	public class AI_Driver
	{
		private readonly Train Train;

		/// <summary>Creates a new instance of this system.</summary>
		/// <param name="train">The train.</param>
		internal AI_Driver(Train train)
		{
			this.Train = train;
		}

		internal bool WesternEnginesRunning;
		internal bool PantographRising;
		internal bool SelfTestPerformed;
		internal int SelfTestSequence = 0;
		internal bool AWSWarningRecieved;
		private bool DRAResponse = false;

		internal void TrainDriver(AIData data)
		{
			//Check if we need to perform the startup self-test
			if (SelfTestPerformed == false)
			{
				if (Train.StartupSelfTestManager == null ||Train.StartupSelfTestManager.SequenceState == StartupSelfTestManager.SequenceStates.Initialised)
				{
					SelfTestPerformed = true;
				}
				else
				{
					switch (SelfTestSequence)
					{
						case 0:
							data.Handles.Reverser = 1;
							data.Response = AIResponse.Long;
							SelfTestSequence++;
							break;
						case 1:
							data.Response = AIResponse.Long;
							SelfTestSequence++;
							break;
						case 2:
							data.Handles.Reverser = 0;
							data.Response = AIResponse.Long;
							SelfTestSequence++;
							break;
						case 3:
							data.Response = AIResponse.Long;
							SelfTestSequence++;
							break;
						case 4:
							data.Response = AIResponse.Long;
							SelfTestSequence++;
							break;
						case 5:
							SelfTestSequence++;
							if (Train.StartupSelfTestManager.SequenceState == StartupSelfTestManager.SequenceStates.AwaitingDriverInteraction)
							{
								Train.StartupSelfTestManager.DriverAcknowledge();
								data.Response = AIResponse.Long;
							}
							SelfTestPerformed = true;
							SelfTestSequence = 0;
							break;
					}
				}
				return;
			}
			//Run the traction specific AI first
			switch (TractionManager.CurrentLocomotiveType)
			{
				case TractionManager.TractionType.Steam:
					SteamLocomotive(ref data);
					break;
				case TractionManager.TractionType.Diesel:
					DieselLocomotive(ref data);
					break;
				case TractionManager.TractionType.Electric:
					ElectricLocomotive(ref data);
					break;
				case TractionManager.TractionType.WesternDiesel:
					WesternDiesel(ref data);
					break;
			}

			//Vigilance Devices
			if (Train.Vigilance != null)
			{
				if (Train.Vigilance.deadmanshandle != 0)
				{
					if (Train.Vigilance.deadmanstimer > (Train.Vigilance.vigilancetime * 0.7))
					{
						Train.Vigilance.deadmanstimer = 0.0;
						Train.Vigilance.DeadmansHandleState = Vigilance.DeadmanStates.OnTimer;
						data.Response = AIResponse.Medium;
					}
				}

				if (Train.Vigilance.DRAEnabled)
				{
					if (Train.drastate == false && Train.CurrentSpeed == 0)
					{
						//When stopped check if we meet one (or more) of the conditions to apply the DRA
						if (Train.NextSignal.Aspect == 0)
						{
							Train.drastate = true;
							Train.TractionManager.DemandPowerCutoff(null);
							data.Response = AIResponse.Short;
						}
						if (Train.Doors != DoorStates.None)
						{
							Train.drastate = true;
							Train.TractionManager.DemandPowerCutoff(null);
							data.Response = AIResponse.Short;
						}
					}
					else if (Train.drastate)
					{
						if (Train.NextSignal.Aspect != 0 && Train.Doors == DoorStates.None)
						{
							if (DRAResponse)
							{
								Train.drastate = false;
								Train.TractionManager.ResetPowerCutoff();
								data.Response = AIResponse.Short;
								DRAResponse = false;
							}
							else
							{
								//Simulate delay from the conditions being met and the driver releasing the DRA
								data.Response = AIResponse.Medium;
								DRAResponse = true;
							}

							
						}
					}
					

				}
			}
			//AWS Handling
			if (Train.AWS != null && Train.AWS.Enabled)
			{
				if (Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerActive)
				{
					if (AWSWarningRecieved == false)
					{
						//This gives a realistic delay between the AWS warning being recieved and the driver acknowledging it
						AWSWarningRecieved = true;
						data.Response = AIResponse.Long;
					}
					else
					{
						Train.AWS.Acknowlege();
						data.Response = AIResponse.Medium;
					}
				}
			}

			if (Train.CAWS != null)
			{
				if (Train.CAWS.AcknowledgementPending)
				{
					if (AWSWarningRecieved == false)
					{
						//This gives a realistic delay between the CAWS warning being recieved and the driver acknowledging it
						AWSWarningRecieved = true;
						data.Response = AIResponse.Long;
					}
					else
					{
						Train.CAWS.AcknowledgementCountdown = 0.0;
						Train.CAWS.AcknowledgementPending = false;
						data.Response = AIResponse.Medium;
					}
				}
			}
		}

		internal void AWSSystem(ref AIData data)
		{
			
		}

		/// <summary>Represents the driver class for a steam locomotive</summary>
		internal void SteamLocomotive(ref AIData data)
		{
			Train.TractionManager.AutomaticAdvancedFunctions = true;
		}

		/// <summary>Represents the driver class for a diesel locomotive</summary>
		internal void DieselLocomotive(ref AIData data)
		{
			Train.TractionManager.AutomaticAdvancedFunctions = true;
		}

		/// <summary>Represents the driver class for a BR Class 52 'Western' Diesel</summary>
		internal void WesternDiesel(ref AIData data)
		{
			if (WesternEnginesRunning == false)
			{
				switch (Train.WesternDiesel.StartupManager.StartupState)
				{
					//First, check if the battery is currently isolated
					case WesternStartupManager.SequenceStates.Pending:
						Train.WesternDiesel.BatteryIsolated = false;
						data.Response = AIResponse.Short;
						return;
					//The battery is energised, so insert the master key
					case WesternStartupManager.SequenceStates.BatteryEnergized:
						Train.WesternDiesel.StartupManager.StartupState =
							WesternStartupManager.SequenceStates.MasterKeyInserted;
						data.Response = AIResponse.Short;
						return;
					//Shift the reverser forwards
					case WesternStartupManager.SequenceStates.MasterKeyInserted:
						if (data.Handles.Reverser == 0)
						{
							data.Handles.Reverser = 1;
							data.Response = AIResponse.Long;
							return;
						}
						data.Handles.Reverser = 0;
						data.Response = AIResponse.Long;
						return;
					//Acknowledge DSD
					case WesternStartupManager.SequenceStates.DirectionSelected:
						Train.WesternDiesel.StartupManager.StartupState =
							WesternStartupManager.SequenceStates.DSDAcknowledged;
						data.Response = AIResponse.Long;
						return;
					//Return reverser to neutral
					case WesternStartupManager.SequenceStates.DSDAcknowledged:
						data.Handles.Reverser = 0;
						data.Response = AIResponse.Long;
						return;
					//Turn on starter motor
					case WesternStartupManager.SequenceStates.ReadyToStart:
						Train.TractionManager.EngineStartKeyPressed = true;
						if (Train.WesternDiesel.Engine2Running)
						{
							Train.WesternDiesel.EngineSelector = 1;
							data.Response = AIResponse.Long;
						}
						if (Train.WesternDiesel.Engine1Running)
						{
							Train.TractionManager.EngineStartKeyPressed = false;
							WesternEnginesRunning = true;
							data.Response = AIResponse.Long;
						}
						return;


				}
			}
			else
			{
				//Our engines are running
				//This will require implementation of handling for the Tooth on Tooth button
			}

		}

		/// <summary>Represents the driver class for an electric locomotive</summary>
		internal void ElectricLocomotive(ref AIData data)
		{
			if (PantographRising)
			{
				//We need a delay if the pantograph is currently rising, as the default Long response isn't quite long enough....
				PantographRising = false;
				data.Response = AIResponse.Long;
				return;
			}

			//The first thing we need to do is to check the pantographs
			//NOTE: Don't attempt to raise a pantograph whilst on the move
			if (Train.CurrentSpeed == 0 && (Train.ElectricEngine.FrontPantograph.State != PantographStates.VCBReady || Train.ElectricEngine.RearPantograph.State != PantographStates.VCBReady))
			{
				//First check whether we have any pantographs
				if (Train.CurrentKeyConfiguration.FrontPantograph != null || Train.CurrentKeyConfiguration.RearPantograph != null)
				{
					//Test the front pantograph first
					if (Train.CurrentKeyConfiguration.FrontPantograph != null &&
						Train.ElectricEngine.FrontPantograph.State == PantographStates.Lowered)
					{
						Train.DebugLogger.LogMessage("AI Driver: Raised the front pantograph.");
						Train.ElectricEngine.FrontPantograph.ToggleState();
						data.Response = AIResponse.Long;
						return;
					}

					//Then test the rear pantograph
					if (Train.CurrentKeyConfiguration.RearPantograph != null &&
						Train.ElectricEngine.RearPantograph.State == PantographStates.Lowered)
					{
						Train.DebugLogger.LogMessage("AI Driver: Raised the rear pantograph.");
						Train.ElectricEngine.RearPantograph.ToggleState();
						data.Response = AIResponse.Long;
						return;
					}
				}
			}
			else if (Train.ElectricEngine.FrontPantograph.State == PantographStates.VCBReady || Train.ElectricEngine.RearPantograph.State == PantographStates.VCBReady)
			{

				//We have a pantograph that's ready for usage, so turn on the ACB/VCB
				Train.DebugLogger.LogMessage("AI Driver: Toggled the VCB.");
				Train.ElectricEngine.TripBreaker();
				data.Response = AIResponse.Short;
				return;
			}

			if (Train.ElectricEngine.PowerGap)
			{
				if (data.Handles.PowerNotch > 0)
				{
					//We're in a power gap, so the driver should shut off power manually
					data.Handles.PowerNotch -= 1;
					data.Response = AIResponse.Short;
				}
				else
				{
					data.Handles.PowerNotch = 0;
					data.Response = AIResponse.Short;
				}

			}
			
		}
	}
}
