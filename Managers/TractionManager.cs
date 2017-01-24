using System;
using OpenBveApi.Runtime;
using Microsoft.Win32;
using Point = System.Drawing.Point;

namespace Plugin
{
	//The traction manager operates panel variables common to all traction types, and handles power & brake requests
	internal partial class TractionManager
	{
		// --- members ---
		/// <summary>Power cutoff has been demaned</summary>
		internal bool PowerCutoffDemanded;
		/// <summary>A safety system has triggered a brake intervention</summary>
		internal bool BrakeInterventionDemanded;
		/// <summary>The current safety system brake notch demanded</summary>
		internal int CurrentInterventionBrakeNotch;
		/// <summary>The current direction of travel</summary>
		internal DirectionOfTravel CurrentDirectionOfTravel;

		internal ReverserManager CurrentReverserManager;

		

		/// <summary>The engine has overheated</summary>
		internal bool EngineOverheated;
		/// <summary>Whether the safety systems are currently isolated by the driver</summary>
		internal bool SafetySystemsIsolated;

		internal TravelMeter CurrentTravelMeter;
		/// <summary>Stores whether the primary klaxon is playing</summary>
		internal bool primaryklaxonplaying;
		/// <summary>Stores whether the secondary klaxon is playing</summary>
		internal bool secondaryklaxonplaying;
		/// <summary>Stores whether the music klaxon is playing</summary>
		internal bool musicklaxonplaying;
		internal double klaxonindicatortimer;
		/// <summary>Stores whether the door state is triggering power cutoff or a brake intervention</summary>
		internal bool doorlock;

		internal bool independantreset;
		/// <summary>Whether advanced functions are automatically handled (e.g. gears & cutoff)</summary>
		internal bool AutomaticAdvancedFunctions;
		//Debug/ Advanced Driving Window Functions
		//
		//Is it currently visible?
		internal static bool debugwindowshowing;
		/** <summary><para>A string array containing the information to be passed to the debug window as follows:</para>
		 * <para>1. Plugin debug message</para>
		 * <para>2. Steam locomotive boiler pressure</para>
		 * <para>3. Steam locomotive pressure generation rate</para>
		 * <para>4. Steam locomotice pressure usage rate</para>
		 * <para>5. Steam locomotive current cutoff</para>
		 * <para>6. Steam locomotive optimal cutoff</para>
		 * <para>7. Steam locomotive fire mass</para>
		 * <para>8. Steam locomotive fire temperature</para>
		 * <para>9. Steam locomotive injectors state</para>
		 * <para>10. Steam locomotive blowers state</para>
		 * <para>11. Steam locomotive boiler water levels</para>
		 * <para>12. Steam locomotive tanks water levels</para>
		 * <para>13. Steam locomotive automatic cutoff state</para>
		 * <para>14. Train speed</para>
		 * <para>15. Front pantograph state</para>
		 * <para>16. Rear pantograph state</para>
		 * <para>17. ACB/VCB state</para>
		 * <para>18. Line Volts</para></summary> */
		//These will probably be renumbered at some stage....

		internal AdvancedDrivingData DebugWindowData = new AdvancedDrivingData();

		public static string[] debuginformation = new string[30];

		/// <summary>Stores the type of the current locomotive</summary>
		public static TractionType CurrentLocomotiveType;

		/// <summary>The underlying train.</summary>
		private readonly Train Train;

		//Default Variables
		/// <summary>The panel index lit when the door state is cutting off the power</summary>
		internal int doorpowerlock = 0;
		/// <summary>The panel index lit when the door state is applying a brake intervention</summary>
		internal int doorapplybrake = 0;

		/// <summary>The panel index of the direction indicator</summary>
		internal int directionindicator = -1;
		/// <summary>The panel index of the reverser handle</summary>
		/// Whilst OpenBVE provides a native implementation for this, the actual reverser handle may be altered
		/// for example by the plugin's cutoff state
		internal int reverserindex = -1;
		internal int travelmeter100 = -1;
		internal int travelmeter10 = -1;
		internal int travelmeter1 = -1;
		internal int travelmeter01 = -1;
		internal int travelmeter001 = -1;
		internal int travelmetermode = 0;
		internal string klaxonindicator = "-1";
		internal string customindicators = "-1";
		internal string customindicatorsounds = "-1";
		internal string customindicatorbehaviour = "-1";

		//Default Key Assignments
		//Keys Down


		/// <summary>The maximum power notch allowed by the engine</summary>
		internal int MaximumPowerNotch;
		/// <summary>The maximum power notch allowed by the safety system</summary>
		internal int SafetySystemMaximumPowerNotch;

		//Arrays
		int[] klaxonarray;
		//Custom Indicators
		internal CustomIndicator[] CustomIndicatorsArray = new CustomIndicator[10];
		
		internal TractionManager(Train train) {
			this.Train = train;
			this.CurrentTravelMeter = new TravelMeter();
			this.CurrentReverserManager = new ReverserManager(train);
		}
		
		//<param name="mode">The initialization mode.</param>
		internal override void Initialize(InitializationModes mode) {
			try
			{
				//Split klaxon indicators and timings into an array
				string[] splitklaxonindicator = klaxonindicator.Split(',');
				klaxonarray = new int[5];
				for (int i = 0; i < 5; i++)
				{
					if (i < splitklaxonindicator.Length)
					{
						//If we have a value parse it
						klaxonarray[i] = Int32.Parse(splitklaxonindicator[i]);
					}
					else
					{
						//Otherwise return 500- Default klaxon timing in ms
						//Stops a complicated null check being required later
						klaxonarray[i] = 500;
					}
				}
			}
			catch
			{
				InternalFunctions.LogError("klaxonindicator",0);
			}
			try
			{
				//Split custom indicators into an array
				string[] splitcustomindicators = customindicators.Split(',');
				for (int i = 0; i < CustomIndicatorsArray.Length; i++)
				{
					CustomIndicatorsArray[i] = new CustomIndicator();
					//Parse the panel value
					if (i < splitcustomindicators.Length)
					{
						CustomIndicatorsArray[i].PanelIndex = Int32.Parse(splitcustomindicators[i]);
					}
					//Set the key assignments
					if (CustomIndicatorsArray[i].Key == null)
					{
						switch (i)
						{
							case 0:
								CustomIndicatorsArray[i].Key = Train.CurrentKeyConfiguration.CustomIndicatorKey1;
							break;
							case 1:
								CustomIndicatorsArray[i].Key = Train.CurrentKeyConfiguration.CustomIndicatorKey2;
							break;
							case 2:
								CustomIndicatorsArray[i].Key = Train.CurrentKeyConfiguration.CustomIndicatorKey3;
							break;
							case 3:
								CustomIndicatorsArray[i].Key = Train.CurrentKeyConfiguration.CustomIndicatorKey4;
							break;
							case 4:
								CustomIndicatorsArray[i].Key = Train.CurrentKeyConfiguration.CustomIndicatorKey5;
							break;
							case 5:
								CustomIndicatorsArray[i].Key = Train.CurrentKeyConfiguration.CustomIndicatorKey6;
							break;
							case 6:
								CustomIndicatorsArray[i].Key = Train.CurrentKeyConfiguration.CustomIndicatorKey7;
							break;
							case 7:
								CustomIndicatorsArray[i].Key = Train.CurrentKeyConfiguration.CustomIndicatorKey8;
							break;
							case 8:
								CustomIndicatorsArray[i].Key = Train.CurrentKeyConfiguration.CustomIndicatorKey9;
							break;
							case 9:
								CustomIndicatorsArray[i].Key = Train.CurrentKeyConfiguration.CustomIndicatorKey10;
							break;
						}
					}
				}
				string[] splitcustomindicatorsounds = customindicatorsounds.Split(',');
				for (int i = 0; i < CustomIndicatorsArray.Length; i++)
				{
					//Parse the sound index value if the array value is not empty
					if (i < splitcustomindicatorsounds.Length && !String.IsNullOrEmpty(splitcustomindicatorsounds[i]))
					{
						CustomIndicatorsArray[i].SoundIndex = Int32.Parse(splitcustomindicatorsounds[i]);
					}
				}
				string[] SplitCustomIndicatorType = customindicatorbehaviour.Split(',');
				for (int i = 0; i < CustomIndicatorsArray.Length; i++)
				{
					//Parse the sound index value if the array value is not empty
					if (i < SplitCustomIndicatorType.Length && !String.IsNullOrEmpty(SplitCustomIndicatorType[i]))
					{
						 InternalFunctions.ParseBool(SplitCustomIndicatorType[i], ref CustomIndicatorsArray[i].PushToMake,"CustomIndicatorBehaviour" + i);
					}
				}

			}
			catch
			{
				InternalFunctions.LogError("customindicators",0);
			}
			//Set traction type to pass to debug window
			//TODO:
			//This should be able to be moved to when the traction type is loaded
			if (Train.SteamEngine != null)
			{
				CurrentLocomotiveType = TractionType.Steam;
			}
			else if (Train.DieselEngine != null)
			{
				CurrentLocomotiveType = TractionType.Diesel;
			}
			else if (Train.ElectricEngine != null)
			{
				CurrentLocomotiveType = TractionType.Electric;
			}
			else if (Train.WesternDiesel != null)
			{
				CurrentLocomotiveType = TractionType.WesternDiesel;
			}
			else
			{
				//Set traction type to 99 (Unknown)
				CurrentLocomotiveType = TractionType.Unknown;
			}
			
			MaximumPowerNotch = this.Train.Specs.PowerNotches;
			SafetySystemMaximumPowerNotch = this.Train.Specs.PowerNotches;
		}

		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data.</param>
		/// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
		internal override void Elapse(ElapseData data, ref bool blocking)
		{
			//Calculate direction
			if (Train.TrainLocation > Train.PreviousLocation)
			{
				CurrentDirectionOfTravel = DirectionOfTravel.Forwards;
			}
			else if (Train.TrainLocation == Train.PreviousLocation)
			{
				CurrentDirectionOfTravel = DirectionOfTravel.Stationary;
			}
			else
			{
				CurrentDirectionOfTravel = DirectionOfTravel.Reverse;
			}
			//Cuts the power if required
			if (MaximumPowerNotch < this.Train.Handles.PowerNotch)
			{
				data.Handles.PowerNotch = MaximumPowerNotch;
			}
			//Door interlocks; Fitted to all trains
			if (this.Train.Doors > 0)
			{
				if (doorpowerlock == 1 && Train.TractionManager.PowerCutoffDemanded == false)
				{
					Train.TractionManager.DemandPowerCutoff();
					data.DebugMessage = "Power cutoff demanded by open doors";
					doorlock = true;
				}

				if (doorapplybrake == 1 && BrakeInterventionDemanded == false)
				{
					Train.TractionManager.DemandBrakeApplication(this.Train.Specs.BrakeNotches);
					data.DebugMessage = "Brakes demanded by open doors";
					doorlock = true;
				}
				
			}
			else
			{
				if ((Train.TractionManager.PowerCutoffDemanded == true || BrakeInterventionDemanded == true) && doorlock == true)
				{
					Train.TractionManager.ResetPowerCutoff();
					Train.TractionManager.ResetBrakeApplication();
					doorlock = false;
				}

			}
			//Reverser change brake behaviour
			CurrentReverserManager.Update();
			//Insufficient steam boiler pressure
			//Debug messages need to be called via the traction manager to be passed to the debug window correctly
			if (Train.SteamEngine != null && Train.SteamEngine.stm_power == 0 && Train.SteamEngine.stm_boilerpressure < Train.SteamEngine.boilerminpressure)
			{
				data.DebugMessage = "Power cutoff due to boiler pressure below minimum";
			}

			if (Train.TractionManager.PowerCutoffDemanded == true)
			{
				if (Train.drastate == true)
				{
					data.DebugMessage = "Power cutoff demanded by DRA Appliance";
				}
				else if (Train.ElectricEngine != null && Train.ElectricEngine.powergap == true)
				{
					if (Train.ElectricEngine.FrontPantograph.State != PantographStates.OnService && Train.ElectricEngine.RearPantograph.State != PantographStates.OnService)
					{
						data.DebugMessage = "Power cutoff due to no available pantographs";
					}
					else
					{
						data.DebugMessage = "Power cutoff demanded by electric conductor power gap";
					}
				}
				else if (Train.ElectricEngine != null && Train.ElectricEngine.breakertripped == true)
				{
					if (Train.ElectricEngine.FrontPantograph.State != PantographStates.OnService && Train.ElectricEngine.RearPantograph.State != PantographStates.OnService)
					{
						data.DebugMessage = "Power cutoff demanded by ACB/VCB not turned on";
					}
					else
					{
						data.DebugMessage = "Power cutoff demanded by ACB/VCB tripping for neutral section";
					}
				}
				else if (doorlock == true)
				{
					data.DebugMessage = "Power cutoff demanded by open doors";
				}
				else if (Train.WesternDiesel != null && (Train.WesternDiesel.Engine1Temperature.Overheated == true || Train.WesternDiesel.Engine2Temperature.Overheated == true))
				{
					if (Train.WesternDiesel.Engine1Temperature.Overheated && Train.WesternDiesel.Engine2Temperature.Overheated)
					{
						data.DebugMessage = "Power cutoff demanded by the Western Diesel engines 1 and 2 overheating";
					}
					else if (Train.WesternDiesel.Engine1Temperature.Overheated)
					{
						data.DebugMessage = "Power cutoff demanded by the Western Diesel engine 1 overheating";
					}
					else
					{
						data.DebugMessage = "Power cutoff demanded by the Western Diesel engine 2 overheating";
					}
				}
				else
				{
					data.DebugMessage = "Power cutoff demanded by AWS";
				}
				data.Handles.PowerNotch = 0;
			}

			if (BrakeInterventionDemanded == true)
			{
				//Set brake notch demanded
				data.Handles.BrakeNotch = CurrentInterventionBrakeNotch;
				//Update debug messages
				if (Train.AWS != null && Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerExpired)
				{
					data.DebugMessage = "EB Brakes demanded by AWS System";
				}
				else if (Train.Vigilance != null)
				{
					if (Train.Vigilance.DeadmansHandleState == Vigilance.DeadmanStates.BrakesApplied)
					{
						data.DebugMessage = "EB Brakes demanded by deadman's handle";
					}
					else if (Train.Vigilance.VigilanteState == Vigilance.VigilanteStates.EbApplied)
					{
						data.DebugMessage = "EB Brakes demanded by Vigilante Device";
					}
					else if (Train.Vigilance.OverspeedDevice.Tripped == true && Train.Vigilance.OverspeedDevice.CurrentBehaviour != OverspeedMonitor.OverspeedBehaviour.CutoffPower)
					{
						data.DebugMessage = "Service Brakes demanded by overspeed device";
					}
				}
				else if (Train.TPWS != null && (Train.TPWS.SafetyState == TPWS.SafetyStates.TssBrakeDemand ||
												Train.TPWS.SafetyState == TPWS.SafetyStates.BrakeDemandAcknowledged ||
												Train.TPWS.SafetyState == TPWS.SafetyStates.BrakesAppliedCountingDown))
				{
					data.DebugMessage = "EB Brakes demanded by TPWS Device";
				}
				else if (doorlock == true)
				{
					data.DebugMessage = "Service Brakes demanded by open doors";
				}
				else if (Train.TractionManager.CurrentReverserManager.Tripped)
				{
					if (Train.TractionManager.CurrentReverserManager.CurrentBehaviour == ReverserManager.NeutralBehaviour.ServiceBrakes)
					{
						data.DebugMessage = "Service Brakes demanded by neutral reverser";
					}
					if (Train.TractionManager.CurrentReverserManager.CurrentBehaviour == ReverserManager.NeutralBehaviour.EmergencyBrakes)
					{
						data.DebugMessage = "EB Brakes demanded by neutral reverser";
					}
				}
				else if (Train.SCMT.enabled == true && SCMT.EBDemanded == true)
				{
					data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches + 1;
				}
				else if (Train.SCMT.enabled == true && SCMT_Traction.ConstantSpeedBrake == true)
				{
					data.DebugMessage = "Brake Notch demanaded by SCMT Constant Speed Device";
				}
				else if (Train.CAWS != null && Train.CAWS.enabled == true)
				{
					data.DebugMessage = "EB Brakes demanded by CAWS Safety System";
				}
			}
			else
			{
				//Workaround for brakes not released correctly if driver attempts to release brakes before
				//brake cutoff is released
				if (Train.Handles.BrakeNotch == 0)
				{
					data.Handles.BrakeNotch = 0;
				}
			}
			//Independant Panel Variables
			if (directionindicator != -1)
			{
				//Direction Indicator
				this.Train.Panel[directionindicator] = (int)CurrentDirectionOfTravel;
			}
			if (reverserindex != -1)
			{
				//Reverser Indicator
				if (Train.Handles.Reverser == 0)
				{
					this.Train.Panel[reverserindex] = 0;
				}
				else if (Train.Handles.Reverser == 1)
				{
					this.Train.Panel[reverserindex] = 1;
				}
				else
				{
					this.Train.Panel[reverserindex] = 2;
				}
			}
			{
				CurrentTravelMeter.Update(this.Train.TrainLocation - this.Train.PreviousLocation);
				//100km
				if (travelmeter100 != -1)
				{
					this.Train.Panel[travelmeter100] = CurrentTravelMeter.Digit100;

				}
				//10km
				if (travelmeter10 != -1)
				{
					this.Train.Panel[travelmeter10] = CurrentTravelMeter.Digit10;

				}
				//1km
				if (travelmeter1 != -1)
				{
					this.Train.Panel[travelmeter1] = CurrentTravelMeter.Digit1;

				}
				//100m
				if (travelmeter01 != -1)
				{
					this.Train.Panel[travelmeter01] = CurrentTravelMeter.Decimal10;

				}
				//1m
				if (travelmeter001 != -1)
				{
					this.Train.Panel[travelmeter001] = CurrentTravelMeter.Decimal1;

				}
			}
			{

				//Horn Indicators
				if (primaryklaxonplaying == true || secondaryklaxonplaying == true || musicklaxonplaying == true)
				{
					klaxonindicatortimer += data.ElapsedTime.Milliseconds;
					//Primary Horn
					if (primaryklaxonplaying == true && klaxonarray[0] != -1)
					{
						if (klaxonarray[0] != -1)
						{
							this.Train.Panel[klaxonarray[0]] = 1;
						}
						if (klaxonindicatortimer > klaxonarray[1])
						{
							primaryklaxonplaying = false;
							klaxonindicatortimer = 0.0;
						}
					}
					//Secondary Horn
					else if (secondaryklaxonplaying == true && klaxonarray[2] != 500)
					{
						if (klaxonarray[2] != 500)
						{
							this.Train.Panel[klaxonarray[2]] = 1;
						}
						if (klaxonindicatortimer > klaxonarray[3])
						{
							secondaryklaxonplaying = false;
							klaxonindicatortimer = 0.0;
						}
					}
					//Music Horn
					else if (musicklaxonplaying == true && klaxonarray[4] != 500)
					{
						if (klaxonarray[4] != 500)
						{
							this.Train.Panel[klaxonarray[4]] = 1;
						}
						if (klaxonindicatortimer > klaxonarray[5])
						{
							musicklaxonplaying = false;
							klaxonindicatortimer = 0.0;
						}
					}
				}
				else
				{
					if (klaxonarray[0] != -1)
					{
						this.Train.Panel[klaxonarray[0]] = 0;
					}
					if (klaxonarray[2] != 500)
					{
						this.Train.Panel[klaxonarray[2]] = 0;
					}
					if (klaxonarray[4] != 500)
					{
						this.Train.Panel[klaxonarray[4]] = 0;
					}
				}
			}
			{
				//Custom Indicators
				for (int i = 0; i < CustomIndicatorsArray.GetLength(0); i++)
				{
					if (CustomIndicatorsArray[i].PanelIndex != -1)
					{
						Train.Panel[CustomIndicatorsArray[i].PanelIndex] = Convert.ToInt32(CustomIndicatorsArray[i].Active);
					}
				}
			}

			//Handles the debug/ advanced driving window
			if (debugwindowshowing == true)
			{
				if (AdvancedDriving.CheckInst == null)
				{
					AdvancedDriving.CreateInst.Show(); // This creates and displays Form2

					using (var key = Registry.CurrentUser.OpenSubKey(@"Software\BVEC_ATS", true))
					{
						if (key != null)
						{
							AdvancedDriving.CreateInst.Left = (int) key.GetValue("Left");
							AdvancedDriving.CreateInst.Top = (int) key.GetValue("Top");
						}
						else
						{
							AdvancedDriving.CreateInst.Location = new Point(100, 100);
						}
					}
				}
				else
				{
					debuginformation[0] = data.DebugMessage;
					debuginformation[13] = Convert.ToString(Train.CurrentSpeed) + " km/h";
					AdvancedDriving.CreateInst.Elapse(debuginformation, (int)CurrentLocomotiveType, DebugWindowData);
				}


			}
			else
			{
				if (AdvancedDriving.CheckInst != null)
				{
					AdvancedDriving.CreateInst.Close();
				}

			}
			
		}

		//Call this function to set the maximum permissable power notch
		/// <summary>Sets the maximum permissiable power notch.</summary>
		/// <param name="NotchRequested">The notch requested.</param>
		/// <param name="SafetySystem">Whether this is being requested by a safety system, or the traction model.</param>
		/// Lower notches will always succeed.
		/// A notch increase must always be first requested by a safety system if applicable
		internal void SetMaxPowerNotch(int NotchRequested, bool SafetySystem)
		{
			if (NotchRequested < MaximumPowerNotch)
			{
				MaximumPowerNotch = NotchRequested;
				if (SafetySystem == true)
				{
					//Set the maximum power notch allowed by the safety system
					SafetySystemMaximumPowerNotch = NotchRequested;
				}
			}
			else
			{
				//Return the minimum of the notch requested and the safety system's maximum power notch
				MaximumPowerNotch = Math.Min(SafetySystemMaximumPowerNotch, NotchRequested);
			}
		}

		//Call this function from a safety system to demand power cutoff
		/// <summary>Deamnds traction power cutoff.</summary>
		/// <remarks>This call will always succeed.</remarks>
		internal void DemandPowerCutoff()
		{
			Train.TractionManager.PowerCutoffDemanded = true;
		}

		//Call this function to attempt to reset the power cutoff
		/// <summary>Attempts to reset the power cutoff state.</summary>
		/// <remarks>The default OS_ATS behaviour is to reset all cutoffs at once.</remarks>
		internal void ResetPowerCutoff()
		{
			//Do not reset power cutoff if still overheated
			if (EngineOverheated == true)
			{
				Train.DebugLogger.LogMessage("Traction power was not restored due to an overheated engine");
				return;
			}
			//Do not reset power cutoff if AWS brake demands are active
			if (Train.AWS != null &&
				(Train.AWS.SafetyState == AWS.SafetyStates.TPWSAWSBrakeDemandIssued ||
				 Train.AWS.SafetyState == AWS.SafetyStates.TPWSTssBrakeDemandIssued))
			{
				Train.DebugLogger.LogMessage("Traction power was not restored due to an AWS/ TPWS intervention");
				return;
			}
			//Do not reset power cutoff if DRA is active
			if (Train.drastate == true)
			{
				Train.DebugLogger.LogMessage("Traction power was not restored due to the DRA being active");
				return;
			}
			//Do not reset power cutoff if doors are still open
			if (doorlock == true)
			{
				Train.DebugLogger.LogMessage("Traction power was not restored due to the doors power lock being active");
				return;
			}
			//Do not reset power cutoff if ATC is still demanding power cutoff via open doors
			if (Train.Atc != null && (Train.Atc.State != Atc.States.Disabled && this.Train.Doors != DoorStates.None))
			{
				Train.DebugLogger.LogMessage("Traction power was not restored due to the ATC door power lock being active");
				return;
			}
			//Do not reset power cutoff if ATS-P is still demanding power cutoff via open doors
			if (Train.AtsP != null && (Train.AtsP.State != AtsP.States.Disabled && this.Train.Doors != DoorStates.None))
			{
				Train.DebugLogger.LogMessage("Traction power was not restored due to the ATS-P power lock being active");
				return;
			}
			//Do not reset power cutoff if ATS-SX is still demanding power cutoff via open doors
			if (Train.AtsSx != null && (Train.AtsSx.State != AtsSx.States.Disabled && this.Train.Doors != DoorStates.None))
			{
				Train.DebugLogger.LogMessage("Traction power was not restored due to the ATS-Sx power lock being active");
				return;
			}
			if (Train.WesternDiesel != null && Train.WesternDiesel.GearBox.TorqueConvertorState != WesternGearBox.TorqueConvertorStates.OnService)
			{
				Train.DebugLogger.LogMessage("Traction power was not restored due to the Western Diesel torque convertor being out of service");
				return;
			}
			if (Train.WesternDiesel != null && Train.WesternDiesel.TransmissionTemperature.Overheated)
			{
				Train.DebugLogger.LogMessage("Traction power was not restored due to the Western Diesel transmission being overheated");
				return;
			}
			Train.TractionManager.PowerCutoffDemanded = false;
			Train.DebugLogger.LogMessage("Traction power restored");

		}

		//Call this function from a safety system to demand a brake application
		/// <summary>Demands a brake application from the traction manager.</summary>
		/// <remarks>Takes the notch demanded as the paramater. If this notch is less than the current notch, do not change notch.</remarks>
		internal void DemandBrakeApplication(int notchdemanded)
		{
			BrakeInterventionDemanded = true;
			if (notchdemanded > CurrentInterventionBrakeNotch)
			{
				CurrentInterventionBrakeNotch = notchdemanded;
			}
		}

		/// <summary>Attempts to reset a brake application from the traction manager.</summary>
		/// <remarks>If independantreset is enabled, then the conditions for reseting all safety systems must be met to release
		/// a brake application.
		/// The default OS_ATS behaviour is to reset all applications at once.</remarks>
		internal void ResetBrakeApplication(bool Silent = false)
		{
			if (independantreset == true)
			{
				if (Train.AWS != null && Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerExpired)
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to a AWS/ TPWS intervention.");
					return;
				}
				if (Train.Vigilance.OverspeedDevice.Tripped == true && Train.Vigilance.OverspeedDevice.CurrentBehaviour != OverspeedMonitor.OverspeedBehaviour.CutoffPower)
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to the overspeed device being active.");
					return;
				}
				if (Train.Vigilance.DeadmansHandleState == Vigilance.DeadmanStates.BrakesApplied)
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to the deadman's handle being active.");
					return;
				}
				if (Train.TPWS != null && (Train.TPWS.SafetyState == TPWS.SafetyStates.TssBrakeDemand ||
												Train.TPWS.SafetyState == TPWS.SafetyStates.BrakeDemandAcknowledged ||
												Train.TPWS.SafetyState == TPWS.SafetyStates.BrakesAppliedCountingDown))
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to a AWS/ TPWS intervention.");
					return;
				}
				if (doorlock == true)
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to the doors power lock being active.");
					return;
				}
				if (Train.TractionManager.CurrentReverserManager.Tripped)
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to the neutral reverser safety device.");
					if (Train.TractionManager.CurrentReverserManager.CurrentBehaviour == ReverserManager.NeutralBehaviour.ServiceBrakes)
					{
						CurrentInterventionBrakeNotch = Train.Specs.BrakeNotches;
					}
					return;
				}
				if (Train.SCMT.enabled == true && SCMT.EBDemanded == true)
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to a SCMT intervention.");
					return;
				}
				if (Train.Vigilance.VigilanteState == Vigilance.VigilanteStates.EbApplied)
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to a SCMT Vigilante intervention.");
					return;
				}
				if (Train.CAWS != null && Train.CAWS.enabled == true && Train.CAWS.EmergencyBrakeCountdown < 0.0)
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to a CAWS intervention.");
					return;
				}
				//These conditions set a different brake notch to EB

				//Set brake notch 1 for SCMT constant speed device
				if (Train.SCMT.enabled == true && SCMT_Traction.ConstantSpeedBrake == true)
				{
					Train.DebugLogger.LogMessage("The currently demanded brake notch was changed to 1 due to the SCMT constant-speed brake.");
					CurrentInterventionBrakeNotch = 1;
					return;
				}
				//Handle ATC brake reset
				if (Train.Atc != null)
				{
					if(Train.Atc.State == Atc.States.ServiceHalf || Train.Atc.State == Atc.States.ServiceFull || Train.Atc.State == Atc.States.Emergency)
					{
						Train.DebugLogger.LogMessage("The current brake application was not reset due to a ATC intervention.");
						return;
					}
					if (Train.Atc.KakuninCheck == true)
					{
						Train.DebugLogger.LogMessage("The current brake application was not reset due to a ATC (Kakunin) intervention.");
						return;
					}
				}
				//Do not reset brake application if ATS-P is currently demanding one
				if (Train.AtsP != null && (Train.AtsP.State == AtsP.States.Brake || Train.AtsP.State == AtsP.States.Service || Train.AtsP.State == AtsP.States.Emergency))
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to a ATS-P intervention.");
					return;
				}
				//Do not reset brake application if ATS-PS is currently demanding one
				if (Train.AtsPs != null && (Train.AtsPs.State == AtsPs.States.Emergency))
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to a ATS-PS intervention.");
					return;
				}
				//ATS-S
				if (Train.AtsSx != null && Train.AtsSx.State == AtsSx.States.Emergency)
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to a ATS-S intervention.");
					return;
				}
				//Do not reset brake application if F92 is currently overspeed
				if (Train.F92 != null && (Train.CurrentSpeed > 70))
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to the F92's overspeed device.");
					return;
				}
				//Do not reset brake application if F92 has passed a red signal
				if (Train.F92 != null && Train.F92.PassedRedSignal == true)
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to the F92 having passed a red signal.");
					return;
				}
			}
			if (Silent == false)
			{
				Train.DebugLogger.LogMessage("The current brake application was reset.");
			}
			CurrentInterventionBrakeNotch = 0;
			BrakeInterventionDemanded = false;
		}

		/// <summary>Attempts to set a new brake notch</summary>
		/// <remarks>This request may be blocked by safety systems demanding a higher brake notch.</remarks>
		/// <param name="Notch">The notch to set</param>
		internal void SetBrakeNotch(int Notch)
		{
			if (Train.AWS != null && Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerExpired)
			{
				return;
			}
			if (Train.Vigilance.OverspeedDevice.Tripped == true && Train.Vigilance.OverspeedDevice.CurrentBehaviour != OverspeedMonitor.OverspeedBehaviour.CutoffPower)
			{
				return;
			}
			if (Train.Vigilance != null)
			{
				if (Train.Vigilance.DeadmansHandleState == Vigilance.DeadmanStates.BrakesApplied)
				{
					return;
				}
				if (Train.Vigilance.VigilanteState == Vigilance.VigilanteStates.EbApplied)
				{
					return;
				}
			}
			if (Train.TPWS != null && (Train.TPWS.SafetyState == TPWS.SafetyStates.TssBrakeDemand ||
											Train.TPWS.SafetyState == TPWS.SafetyStates.BrakeDemandAcknowledged ||
											Train.TPWS.SafetyState == TPWS.SafetyStates.BrakesAppliedCountingDown))
			{
				return;
			}
			if (doorlock == true)
			{
				return;
			}
			if (Train.TractionManager.CurrentReverserManager.Tripped)
			{
				if (Train.TractionManager.CurrentReverserManager.CurrentBehaviour == ReverserManager.NeutralBehaviour.ServiceBrakes)
				{
					CurrentInterventionBrakeNotch = Train.Specs.BrakeNotches;
				}
				return;
			}
			if (Train.SCMT.enabled == true && SCMT.EBDemanded == true)
			{
				return;
			}
			
			if (Train.CAWS != null && Train.CAWS.enabled == true && Train.CAWS.EmergencyBrakeCountdown < 0.0)
			{
				return;
			}

			if (Train.SCMT.enabled == true && SCMT_Traction.ConstantSpeedBrake == true)
			{
				CurrentInterventionBrakeNotch = 1;
				return;
			}
			
			
			if (Train.F92 != null && (Train.CurrentSpeed > 70))
			{
				return;
			}
			if (Train.F92 != null && Train.F92.PassedRedSignal == true)
			{
				return;
			}
			if (Train.Atc != null && Train.Atc.KakuninCheck == true)
			{
				return;
			}
			CurrentInterventionBrakeNotch = Notch;
			BrakeInterventionDemanded = CurrentInterventionBrakeNotch != 0;
		}

		//Call this function to attempt to isolate or re-enable the TPWS & AWS Systems
		/// <summary>Attempts to disable or re-enable the TPWS & AWS safety systems.</summary>
		internal void isolatetpwsaws()
		{
			if (Train.AWS == null)
			{
				return;
			}
			bool CanIsolate = false;
			if (SafetySystemsIsolated == false)
			{
				//First check if TPWS is enabled in this train [AWS must therefore be enabled]
				if (Train.TPWS.enabled == true)
				{
					if (Train.TPWS.SafetyState == TPWS.SafetyStates.None && (Train.AWS.SafetyState == AWS.SafetyStates.Clear || Train.AWS.SafetyState == AWS.SafetyStates.None))
					{
						CanIsolate = true;
					}
				}
				else if (Train.TPWS.enabled == false && Train.AWS.enabled == true)
				{
					if (Train.AWS.SafetyState == AWS.SafetyStates.Clear || Train.AWS.SafetyState == AWS.SafetyStates.None)
					{
						CanIsolate = true;
					}
				}

				if (CanIsolate == true)
				{
					if (Train.TPWS.enabled == true)
					{
						Train.TPWS.Isolate();
					}
					if (Train.AWS.enabled == true)
					{
						Train.AWS.Isolate();
					}
					SafetySystemsIsolated = true;
				}
			}
		}

		/// <summary>Enables the AWS & TPWS systems if they are fitted.</summary>
		internal void reenabletpwsaws()
		{
			if (Train.AWS == null)
			{
				return;
			}
			if (SafetySystemsIsolated == true)
			{
				if (Train.AWS.enabled == true)
				{
					Train.AWS.Reset();
					SafetySystemsIsolated = false;
				}
				if (Train.TPWS.enabled == true)
				{
					Train.TPWS.Reset();
					SafetySystemsIsolated = false;
				}
			}
		}

		/// <summary>Is called when a key is pressed.</summary>
		/// <param name="key">The key.</param>
		internal override void KeyDown(VirtualKeys key)
		{


			if (Train.Vigilance != null)
			{
				if (Train.Vigilance.DeadmansHandleState != Vigilance.DeadmanStates.BrakesApplied &&
				    Train.Vigilance.independantvigilance == 0)
				{
					//Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
					Train.Vigilance.deadmanstimer = 0.0;
					SoundManager.Stop(Train.Vigilance.vigilancealarm);
					Train.Vigilance.DeadmansHandleState = Vigilance.DeadmanStates.OnTimer;
				}
			}


			if (key == Train.CurrentKeyConfiguration.AutomaticGearsCutoff)
			{
				//Toggle Automatic Cutoff/ Gears
				AutomaticAdvancedFunctions = !AutomaticAdvancedFunctions;
			}
			if (key == Train.CurrentKeyConfiguration.ShowAdvancedDrivingWindow)
			{
				if (debugwindowshowing == false)
				{
					debugwindowshowing = true;
				}
				else
				{
					debugwindowshowing = false;
				}
			}
			if (Train.SteamEngine != null)
			{
				//Injectors
				if (key == VirtualKeys.LiveSteamInjector)
				{
					Train.SteamEngine.LiveSteamInjector.Active = !Train.SteamEngine.LiveSteamInjector.Active;
				}
				if (key == VirtualKeys.ExhaustSteamInjector)
				{
					Train.SteamEngine.ExhaustSteamInjector.Active = !Train.SteamEngine.LiveSteamInjector.Active;
				}
			}

			if (key == Train.CurrentKeyConfiguration.SafetyKey)
			{
				if (Train.Vigilance != null)
				{
					//Reset Overspeed Trip
					if ((Train.CurrentSpeed == 0 || Train.Vigilance.vigilancecancellable != 0) &&
						Train.Vigilance.OverspeedDevice.Tripped == true)
					{
						Train.Vigilance.OverspeedDevice.Tripped = false;
						if (Train.Vigilance.OverspeedDevice.CurrentBehaviour == OverspeedMonitor.OverspeedBehaviour.CutoffPower)
						{
							ResetPowerCutoff();
						}
						else
						{
							ResetBrakeApplication();
						}

					}


					//Reset deadman's handle if independant vigilance is selected & brakes have not been applied
					if (Train.Vigilance.independantvigilance != 0 &&
						Train.Vigilance.DeadmansHandleState != Vigilance.DeadmanStates.BrakesApplied)
					{
						Train.Vigilance.DeadmansHandleState = Vigilance.DeadmanStates.OnTimer;
						Train.Vigilance.deadmanstimer = 0.0;
						SoundManager.Stop(Train.Vigilance.vigilancealarm);
						ResetBrakeApplication();
					}
						//Reset brakes if allowed
					else if (Train.Vigilance.vigilancecancellable != 0 &&
							 Train.Vigilance.DeadmansHandleState == Vigilance.DeadmanStates.BrakesApplied)
					{
						Train.Vigilance.DeadmansHandleState = Vigilance.DeadmanStates.OnTimer;
						Train.Vigilance.deadmanstimer = 0.0;
						SoundManager.Stop(Train.Vigilance.vigilancealarm);
					}
						//If brakes cannot be cancelled and we've stopped
					else if (Train.Vigilance.vigilancecancellable == 0 && Train.CurrentSpeed == 0 &&
							 Train.Vigilance.DeadmansHandleState == Vigilance.DeadmanStates.BrakesApplied)
					{
						Train.Vigilance.DeadmansHandleState = Vigilance.DeadmanStates.OnTimer;
						Train.Vigilance.deadmanstimer = 0.0;
						SoundManager.Stop(Train.Vigilance.vigilancealarm);
						ResetBrakeApplication();
					}
				}

				if (Train.AWS != null)
				{
					//Acknowledge AWS warning
					if (Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerActive)
					{
						Train.AWS.Acknowlege();
					}
					//Reset AWS
					else if (Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerExpired && Train.CurrentSpeed == 0 &&
						Train.Handles.Reverser == 0)
					{
						if (SoundManager.IsPlaying(Train.AWS.awswarningsound))
						{
							SoundManager.Stop(Train.AWS.awswarningsound);
						}
						Train.AWS.Reset();
						ResetPowerCutoff();
					}
					this.Train.AWS.CancelButtonPressed = true;
				}
				if (Train.TPWS != null)
				{
					//Acknowledge TPWS Brake Demand
					if (Train.TPWS.SafetyState == TPWS.SafetyStates.TssBrakeDemand)
					{
						Train.TPWS.AcknowledgeBrakeDemand();
					}
				}
				//Acknowledge Self-Test warning
				if (Train.StartupSelfTestManager != null && Train.StartupSelfTestManager.SequenceState == StartupSelfTestManager.SequenceStates.AwaitingDriverInteraction)
				{
					Train.StartupSelfTestManager.driveracknowledge();
				}
			}
			if (key == VirtualKeys.FillFuel)
			{
				//Toggle Fuel fill
				if (Train.canfuel == true && Train.CurrentSpeed == 0)
				{
					if (Train.SteamEngine != null)
					{
						Train.SteamEngine.fuelling = true;
					}
					if (Train.DieselEngine != null)
					{
						Train.DieselEngine.fuelling = true;
					}
				}
				//ACB/ VCB toggle
				if (Train.ElectricEngine != null)
				{
					Train.ElectricEngine.breakertrip();
				}
			}
			if (key == VirtualKeys.MainBreaker)
			{
				Train.ElectricEngine.breakertrip();
			}
			if (key == VirtualKeys.WiperSpeedDown)
			{
				//Wipers Speed Down
				if (Train.Windscreen.enabled == true)
				{
					Train.Windscreen.windscreenwipers(1);
				}
			}
			if (key == VirtualKeys.WiperSpeedUp)
			{
				//Wipers Speed Up
				if (Train.Windscreen.enabled == true)
				{
					Train.Windscreen.windscreenwipers(0);
				}
			}
			if (key == Train.CurrentKeyConfiguration.IsolateSafetySystems)
			{
				if (Train.WesternDiesel != null)
				{
					Train.WesternDiesel.ToggleAWS();
				}
				else
				{
					//Isolate Safety Systems
					if (SafetySystemsIsolated == false)
					{
						isolatetpwsaws();
					}
					else
					{
						reenabletpwsaws();
					}
				}
			}
			//Toggle Pantographs
			if (key == Train.CurrentKeyConfiguration.FrontPantograph)
			{
				Train.ElectricEngine.pantographtoggle(0);
			}
			if (key == Train.CurrentKeyConfiguration.RearPantograph)
			{
				Train.ElectricEngine.pantographtoggle(1);
			}

			foreach (CustomIndicator Indicator in CustomIndicatorsArray)
			{
				if (key == Indicator.Key)
				{
					Indicator.Active = !Indicator.Active;
					//Play the toggle sound if this has been set
					if (Indicator.SoundIndex != -1)
					{
						SoundManager.Play(Indicator.SoundIndex, 1.0, 1.0, false);
					}
				}
			}

			if (key == Train.CurrentKeyConfiguration.HeadCode)
			{
				Animations.headcodetoggle();
			}
			//Advanced steam locomotive functions
			if (Train.SteamEngine != null)
			{
				if (key == VirtualKeys.IncreaseCutoff)
				{
					//Cutoff Up
					if (Train.SteamEngine != null)
					{
						Train.SteamEngine.cutoffstate = 1;
					}
				}
				if (key == VirtualKeys.DecreaseCutoff)
				{
					//Cutoff Down
					if (Train.SteamEngine != null)
					{
						Train.SteamEngine.cutoffstate = -1;
					}
				}
				//Blowers
				if (key == VirtualKeys.Blowers)
				{
					//Train.SteamEngine.blowers = !Train.SteamEngine.blowers;
				}
				//Shovel Coal
				if (key == Train.CurrentKeyConfiguration.ShovelFuel)
				{
					Train.SteamEngine.shovelling = !Train.SteamEngine.shovelling;
					
				}
				if (key == Train.CurrentKeyConfiguration.IncreaseSteamHeat)
				{
					if (Train.SteamEngine.steamheatlevel < 5)
					{
						Train.SteamEngine.steamheatlevel++;
					}
				}
				if (key == Train.CurrentKeyConfiguration.DecreaseSteamHeat)
				{
					if (Train.SteamEngine.steamheatlevel > 0)
					{
						Train.SteamEngine.steamheatlevel--;
					}
				}
				if (key == Train.CurrentKeyConfiguration.CylinderCocks)
				{
					if (Train.SteamEngine.cylindercocks == false)
					{
						Train.SteamEngine.cylindercocks = true;
					}
					else
					{
						Train.SteamEngine.cylindercocks = false;
					}
				}
			}
			if (Train.SCMT_Traction.enabled == true)
			{
				if (key == Train.CurrentKeyConfiguration.SCMTincreasespeed)
				{
					SCMT_Traction.increasesetspeed();
				}
				if (key == Train.CurrentKeyConfiguration.SCMTdecreasespeed)
				{
					SCMT_Traction.decreasesetspeed();
				}
				if (key == Train.CurrentKeyConfiguration.AbilitaBancoKey)
				{
					SCMT_Traction.AbilitaBanco();
				}
				if (key == Train.CurrentKeyConfiguration.ConsensoAvviamentoKey)
				{
					SCMT_Traction.ConsensoAvviamento();
				}
				if (key == Train.CurrentKeyConfiguration.AvviamentoKey)
				{
					SCMT_Traction.Avviamento();
				}
				if (key == Train.CurrentKeyConfiguration.SpegnimentoKey)
				{
					SCMT_Traction.Spegnimento();
				}
				if (key == Train.CurrentKeyConfiguration.LCMupKey)
				{
					SCMT_Traction.LCMup();
				}
				if (key == Train.CurrentKeyConfiguration.LCMdownkey)
				{
					SCMT_Traction.LCMdown();
				}
				if (key == Train.CurrentKeyConfiguration.TestSCMTKey)
				{
					Train.SCMT_Traction.TestSCMT();
				}
			}
			if (Train.CAWS != null)
			{
				if (key == Train.CurrentKeyConfiguration.CAWSKey)
				{
					if (CAWS.AcknowledgementCountdown > 0.0)
					{
						CAWS.AcknowledgementCountdown = 0.0;
						CAWS.AcknowledgementPending = false;
					}
				}
			}
			//Italian SCMT vigilante system
			if (Train.Vigilance != null && Train.Vigilance.vigilante == true)
			{
				if (key == Train.CurrentKeyConfiguration.vigilantekey)
				{
					if (Train.Vigilance.VigilanteState == Vigilance.VigilanteStates.AlarmSounding)
					{
						Train.Vigilance.VigilanteState = Vigilance.VigilanteStates.OnService;
					}
				}
				else if (key == Train.CurrentKeyConfiguration.vigilanteresetkey)
				{
					Train.Vigilance.VigilanteReset();
				}
			}
			if (Train.PZB != null)
			{
				if (key == Train.CurrentKeyConfiguration.PZBKey)
				{
					Train.PZB.Acknowledge();
					Train.PZB.WachamPressed = true;
				}
				if (key == Train.CurrentKeyConfiguration.PZBReleaseKey)
				{
					Train.PZB.Release();
					Train.PZB.FreiPressed = true;
				}
				if (key == Train.CurrentKeyConfiguration.PZBStopOverrideKey)
				{
					if (Train.PZB.BefehlState == PZB.PZBBefehlStates.None)
					{
						Train.PZB.PZBBefehlState = PZB.PZBBefehlStates.Applied;
					}
					Train.PZB.StopOverrideKeyPressed = true;
				}
			}
			//Western Diesel Locomotive
			if (Train.WesternDiesel != null)
			{
				if (key == Train.CurrentKeyConfiguration.SafetyKey)
				{
					if (Train.WesternDiesel.StartupManager.StartupState == WesternStartupManager.SequenceStates.DirectionSelected)
					{
						//Acknowledge the DSD Buzzer
						Train.WesternDiesel.StartupManager.StartupState = WesternStartupManager.SequenceStates.DSDAcknowledged;
					}
				}
				if (key == Train.CurrentKeyConfiguration.EngineStartKey)
				{
					Train.WesternDiesel.StarterKeyPressed = true;
				}
				if (key == Train.CurrentKeyConfiguration.WesternBatterySwitch)
				{
					Train.WesternDiesel.BatterySwitch();
				}
				if (key == Train.CurrentKeyConfiguration.WesternMasterKey)
				{
					Train.WesternDiesel.MasterKey();
				}

				if (key == Train.CurrentKeyConfiguration.WesternAWSIsolationKey)
				{
					Train.WesternDiesel.ToggleAWS();
				}
				if (key == Train.CurrentKeyConfiguration.WesternFireBellKey)
				{
					Train.WesternDiesel.FireBellTest();
				}
				if (key == Train.CurrentKeyConfiguration.WesternEngineOnlyKey)
				{
					Train.WesternDiesel.EngineOnly = !Train.WesternDiesel.EngineOnly;
				}
				if (key == Train.CurrentKeyConfiguration.WesternEngineSwitchKey)
				{
					if (Train.WesternDiesel.EngineSelector == 1)
					{
						Train.WesternDiesel.EngineSelector = 2;
					}
					else
					{
						Train.WesternDiesel.EngineSelector = 1;
					}
				}
				if (key == VirtualKeys.EngineStop)
				{
					if (Train.WesternDiesel.EngineSelector == 1)
					{
						Train.WesternDiesel.EngineStop(1);
					}
					else
					{
						Train.WesternDiesel.EngineStop(2);
					}
				}
				if (key == Train.CurrentKeyConfiguration.WesternFuelPumpSwitch)
				{
					Train.WesternDiesel.FuelPumpSwitch();
				}
			}
		}

		internal override void KeyUp(VirtualKeys key)
		{
			if (key == VirtualKeys.GearUp)
			{
				//Gear Up
				if (Train.DieselEngine != null)
				{
					if (Train.DieselEngine.gear >= 0 && Train.DieselEngine.gear < Train.DieselEngine.totalgears - 1 && Train.Handles.PowerNotch == 0)
					{
						Train.DieselEngine.gear++;
						Train.DieselEngine.gearloop = false;
						Train.DieselEngine.gearlooptimer = 0.0;
						Train.DieselEngine.gearchange();
					}
				}
			}
			if (key == VirtualKeys.GearDown)
			{
				//Gear Down
				if (Train.DieselEngine != null)
				{
					if (Train.DieselEngine.gear <= Train.DieselEngine.totalgears && Train.DieselEngine.gear > 0 && Train.Handles.PowerNotch == 0)
					{
						Train.DieselEngine.gear--;
						Train.DieselEngine.gearloop = false;
						Train.DieselEngine.gearlooptimer = 0.0;
						Train.DieselEngine.gearchange();
					}
				}
			}
			if (key == VirtualKeys.IncreaseCutoff)
			{
				//Cutoff Up
				if (Train.SteamEngine != null)
				{
					Train.SteamEngine.cutoffstate = 0;
				}
			}
			if (key == VirtualKeys.DecreaseCutoff)
			{
				//Cutoff Down
				if (Train.SteamEngine != null)
				{
					Train.SteamEngine.cutoffstate = 0;
				}
			}
			if (key == VirtualKeys.FillFuel)
			{
				//Toggle Fuel fill
				if (Train.SteamEngine != null)
				{
					Train.SteamEngine.fuelling = false;
				}
				if (Train.DieselEngine != null)
				{
					Train.DieselEngine.fuelling = false;
				}
			}
			if (Train.Vigilance != null)
			{
				if (key == Train.CurrentKeyConfiguration.DRA)
				{
					if (Train.Vigilance.draenabled != -1)
					{
						//Operate DRA
						if (Train.drastate == false)
						{
							Train.drastate = true;
							DemandPowerCutoff();
						}
						else
						{
							Train.drastate = false;
							ResetPowerCutoff();
						}
					}
				}
			}
			foreach (CustomIndicator Indicator in CustomIndicatorsArray)
			{
				//Reset any push-to-make indicators
				if (key == Indicator.Key && Indicator.PushToMake == true)
				{
					Indicator.Active = !Indicator.Active;
				}
			}
			if (Train.SCMT_Traction != null)
			{
				if (key == Train.CurrentKeyConfiguration.SCMTincreasespeed || key == Train.CurrentKeyConfiguration.SCMTdecreasespeed)
				{
					SCMT_Traction.releasekey();
				}
				if (key == Train.CurrentKeyConfiguration.AvviamentoKey)
				{
					SCMT_Traction.AvviamentoReleased();
				}
				if (key == Train.CurrentKeyConfiguration.SpegnimentoKey)
				{
					SCMT_Traction.SpegnimentoReleased();
				}
			}
			if (Train.PZB != null)
			{
				if (key == Train.CurrentKeyConfiguration.PZBKey)
				{
					Train.PZB.WachamPressed = false;
				}
				if (key == Train.CurrentKeyConfiguration.PZBReleaseKey)
				{
					Train.PZB.Release();
					Train.PZB.FreiPressed = false;
				}

				if (key == Train.CurrentKeyConfiguration.PZBStopOverrideKey)
				{
					Train.PZB.StopOverrideKeyPressed = false;
					if (Train.PZB.PZBBefehlState != PZB.PZBBefehlStates.EBApplication)
					{
						Train.PZB.PZBBefehlState = PZB.PZBBefehlStates.None;
					}
				}
			}
			//Western Diesel Locomotive
			if (Train.WesternDiesel != null)
			{
				if (key == VirtualKeys.EngineStart)
				{
					Train.WesternDiesel.StarterKeyPressed = false;
				}
			}
			if (Train.AWS != null)
			{
				if (key == Train.CurrentKeyConfiguration.SafetyKey)
				{
					this.Train.AWS.CancelButtonPressed = false;
				}
			}
		}

		/// <summary>Is called when the driver changes the reverser.</summary>
		/// <param name="reverser">The new reverser position.</param>
		internal override void SetReverser(int reverser)
		{
			if (Train.Vigilance != null)
			{
				if (Train.Vigilance.DeadmansHandleState != Vigilance.DeadmanStates.BrakesApplied &&
					Train.Vigilance.independantvigilance == 0)
				{
					//Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
					Train.Vigilance.deadmanstimer = 0.0;
					SoundManager.Stop(Train.Vigilance.vigilancealarm);
					Train.Vigilance.DeadmansHandleState = Vigilance.DeadmanStates.OnTimer;
				}
			}

		}

		/// <summary>Is called when the driver changes the power notch.</summary>
		/// <param name="powerNotch">The new power notch.</param>
		internal override void SetPower(int powerNotch)
		{
			if (Train.Vigilance != null)
			{
				if (Train.Vigilance.DeadmansHandleState != Vigilance.DeadmanStates.BrakesApplied &&
					Train.Vigilance.independantvigilance == 0)
				{
					//Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
					Train.Vigilance.deadmanstimer = 0.0;
					SoundManager.Stop(Train.Vigilance.vigilancealarm);
					Train.Vigilance.DeadmansHandleState = Vigilance.DeadmanStates.OnTimer;
				}
			}
			//Trigger electric powerloop sound timer
			if (Train.ElectricEngine != null)
			{
				Train.ElectricEngine.powerloop = false;
				Train.ElectricEngine.powerlooptimer = 0.0;
			}
		}

		/// <summary>Is called when the driver changes the brake notch.</summary>
		/// <param name="brakeNotch">The new brake notch.</param>
		internal override void SetBrake(int brakeNotch)
		{
			if (Train.Vigilance != null)
			{
				if (Train.Vigilance.DeadmansHandleState != Vigilance.DeadmanStates.BrakesApplied &&
					Train.Vigilance.independantvigilance == 0)
				{
					//Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
					Train.Vigilance.deadmanstimer = 0.0;
					SoundManager.Stop(Train.Vigilance.vigilancealarm);
					Train.Vigilance.DeadmansHandleState = Vigilance.DeadmanStates.OnTimer;
				}
			}
		}

		internal override void HornBlow(HornTypes type)
		{
			if (Train.Vigilance != null)
			{
				if (Train.Vigilance.DeadmansHandleState != Vigilance.DeadmanStates.BrakesApplied &&
					Train.Vigilance.independantvigilance == 0)
				{
					//Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
					Train.Vigilance.deadmanstimer = 0.0;
					SoundManager.Stop(Train.Vigilance.vigilancealarm);
					Train.Vigilance.DeadmansHandleState = Vigilance.DeadmanStates.OnTimer;
				}
			}
			//Set the horn type that is playing
			if (type == HornTypes.Primary)
			{
				this.primaryklaxonplaying = true;
			}
			else if (type == HornTypes.Secondary)
			{
				this.secondaryklaxonplaying = true;
			}
			else
			{
				this.musicklaxonplaying = true;
			}
			
		}
	}
}
