using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using OpenBveApi.Runtime;
using Plugin.AI;

namespace Plugin {
	/// <summary>Represents a train that is simulated by this plugin.</summary>
	internal class Train {
		
		// --- classes and enumerations ---
		
		/// <summary>Represents handles that can only be read from.</summary>
		internal class ReadOnlyHandles {
			// --- members ---
			/// <summary>The reverser position.</summary>
			private int MyReverser;
			/// <summary>The power notch.</summary>
			private int MyPowerNotch;
			/// <summary>The brake notch.</summary>
			private int MyBrakeNotch;
			/// <summary>Whether the const speed system is enabled.</summary>
			private bool MyConstSpeed;
			// --- properties ---
			/// <summary>Gets or sets the reverser position.</summary>
			internal int Reverser {
				get {
					return this.MyReverser;
				}
			}
			/// <summary>Gets or sets the power notch.</summary>
			internal int PowerNotch {
				get {
					return this.MyPowerNotch;
				}
			}
			/// <summary>Gets or sets the brake notch.</summary>
			internal int BrakeNotch {
				get {
					return this.MyBrakeNotch;
				}
			}
			/// <summary>Gets or sets whether the const speed system is enabled.</summary>
			internal bool ConstSpeed {
				get {
					return this.MyConstSpeed;
				}
			}
			// --- constructors ---
			/// <summary>Creates a new instance of this class.</summary>
			/// <param name="handles">The handles</param>
			internal ReadOnlyHandles(Handles handles) {
				this.MyReverser = handles.Reverser;
				this.MyPowerNotch = handles.PowerNotch;
				this.MyBrakeNotch = handles.BrakeNotch;
				this.MyConstSpeed = handles.ConstSpeed;
			}
		}
		

		
		
		// --- plugin ---
		
		/// <summary>Whether the plugin is currently initializing. This happens in-between Initialize and Elapse calls, for example when jumping to a station from the menu.</summary>
		internal bool PluginInitializing;
		
		
		// --- train ---

		/// <summary>The train specifications.</summary>
		internal VehicleSpecs Specs;
		
		/// <summary>The current state of the train.</summary>
		internal VehicleState State;
		
		/// <summary>The driver handles at the last Elapse call.</summary>
		internal ReadOnlyHandles Handles;

		internal KeyConfiguration CurrentKeyConfiguration = new KeyConfiguration(false);
		
		/// <summary>The current state of the doors.</summary>
		internal DoorStates Doors;

		/// <summary>Stores whether any safety systems have been tripped.</summary>
		internal bool overspeedtripped;
		internal bool drastate;
		//internal bool deadmanstripped;
		/// <summary>Stores whether the startup self-test has been performed.</summary>
		internal static bool selftest = false;
		/// <summary>Stores whether the master switch is on (Affects in-cab lights etc)</summary>
		internal bool MasterSwitch = false;

		/// <summary>Stores whether we can fuel the train.</summary>
		internal bool canfuel;

		// --- acceleration ---

		/// <summary>The speed.</summary>
		internal int CurrentSpeed;

		/// <summary>The current location of the train.</summary>
		internal double TrainLocation;
		internal double PreviousLocation;

		/// <summary>The next signal</summary>
		internal SignalData NextSignal;

		/// <summary>The in-game time of day, in seconds.</summary>
		internal static double SecondsSinceMidnight;
		
		/// <summary>The current value of the accelerometer.</summary>
		internal double Acceleration;
		
		/// <summary>The speed of the train at the beginning of the accelerometer timer.</summary>
		private double AccelerometerSpeed;
		
		/// <summary>The time elapsed since the last reset of the accelerometer timer.</summary>
		private double AccelerometerTimer;
		
		/// <summary>The maximum value for the accelerometer timer.</summary>
		private const double AccelerometerMaximumTimer = 0.25;


		// --- panel and sound ---
		
		/// <summary>The panel variables.</summary>
		internal int[] Panel;
		
		/// <summary>Whether illumination in the panel is enabled.</summary>
		internal bool PanelIllumination;
		
		/// <summary>Remembers which of the virtual keys are currently pressed down.</summary>
		private bool[] KeysPressed = new bool[16];
		
		// --- devices ---
		/// <summary>Traction Manager- Manages brake and power applications</summary>
		internal TractionManager TractionManager;

		/// <summary>Steam Traction or Null Reference if not installed</summary>
		internal steam SteamEngine;

		/// <summary>Electric Traction or Null Reference if not installed</summary>
		internal Electric ElectricEngine;

		/// <summary>Diesel Traction or Null Reference if not installed</summary>
		internal Diesel DieselEngine;

		/// <summary>Vigilance Devices or Null Reference if not installed</summary>
		/// Overspeed, deadman's handle & DRA
		internal Vigilance Vigilance;

		/// <summary>AWS System</summary>
		internal AWS AWS;

		/// <summary>TPWS System</summary>
		internal TPWS TPWS;

		/// <summary>SCMT System</summary>
		internal SCMT SCMT;

		/// <summary>SCMT Traction Modelling System</summary>
		internal SCMT_Traction SCMT_Traction;

		/// <summary>CAWS System</summary>
		internal CAWS CAWS;

		/// <summary>PZB System</summary>
		internal PZB PZB;

		/// <summary>Startup Self-Test Manager</summary>
		internal StartupSelfTestManager StartupSelfTestManager;

		/// <summary>Windscreen & Wipers</summary>
		internal Windscreen Windscreen;

		/// <summary>Advanced animation handlers</summary>
		internal Animations Animations;

		/// <summary>The AI Driver</summary>
		internal AI.AI_Driver Driver;

		internal DebugLogger DebugLogger;

		/*
		 * DEVICES ADDED FROM ODAKYUFANATS
		 * 
		 */
		/// <summary>The AI component that calls out signal aspects and speed restrictions.</summary>
		internal Calling Calling;

		// --- devices ---

		/// <summary>The ATS-Sx device, or a null reference if not installed.</summary>
		internal AtsSx AtsSx;

		/// <summary>The ATS-Ps device, or a null reference if not installed.</summary>
		internal AtsPs AtsPs;

		/// <summary>The ATS-P device, or a null reference if not installed.</summary>
		internal AtsP AtsP;

		/// <summary>The ATC device, or a null reference if not installed.</summary>
		internal Atc Atc;

		/// <summary>The EB device, or a null reference if not installed.</summary>
		internal Eb Eb;

		/// <summary>The TASC device, or a null reference if not installed.</summary>
		internal Tasc Tasc;

		/// <summary>The ATO device, or a null reference if not installed.</summary>
		internal Ato Ato;

		/// <summary>The F92 device, or a null reference if not installed.</summary>
		internal F92 F92;

#if !DebugNBS
		internal LEDLights LedLights;
#endif
		internal WesternDiesel WesternDiesel;


		/// <summary>A list of all the devices installed on this train</summary>
		internal Device[] Devices;


		// --- constructors ---

		/// <summary>Creates a new train without any devices installed.</summary>
		/// <param name="panel">The array of panel variables.</param>
		internal Train(int[] panel) {
			this.PluginInitializing = false;
			this.Specs = new VehicleSpecs(0, BrakeTypes.ElectromagneticStraightAirBrake, 0, false, 0);
			this.State = new VehicleState(0.0, new Speed(0.0), 0.0, 0.0, 0.0, 0.0, 0.0);
			this.Handles = new ReadOnlyHandles(new Handles(0, 0, 0, false));
			this.Doors = DoorStates.None;
			this.Panel = panel;
			
		}

		internal void LoadAWSTPWS()
		{
			if (this.AWS != null)
			{
				return;
			}
			//This function loads AWS and TPWS into memory
			this.TPWS = new TPWS(this);
			this.AWS = new AWS(this);
			this.StartupSelfTestManager = new StartupSelfTestManager(this);
		}
		
		// --- functions ---
		
		/// <summary>Sets up the devices from the specified configuration file.</summary>
		/// <param name="lines">A string array containing the lines read from the configuration file.</param>
		internal void LoadConfigurationFile(string[] lines) {
			

			//Initialise all safety devices first
			//Set Enabled state if they are installed
			
			//Only initialise traction types if required
			this.TractionManager = new TractionManager(this);
			
			
			
			
			//this.Calling = new Calling(this, playSound);
			this.Driver = new AI_Driver(this);
			this.DebugLogger = new DebugLogger();
			this.SCMT = new SCMT(this);
			this.SCMT_Traction = new SCMT_Traction(this);
			this.Windscreen = new Windscreen(this);
			this.Animations = new Animations(this);
			string section = string.Empty;
			foreach (string line in lines)
			{
				//First, check to see if we're running in debug mode
				//This needs to be done first, followed by a reparse of the configuration file
				//to actually load traction types etc.
				if (line.Length != 0 && line[0] == '[' & line[line.Length - 1] == ']')
				{
					section = line.Substring(1, line.Length - 2).ToLowerInvariant();
					if (section == "debug")
					{
						DebugLogger.DebugDate = DateTime.Now.ToString("dd-MM-yyyy");
						DebugLogger.DebugLogEnabled = true;
						string version = Assembly.GetEntryAssembly().GetName().Version.ToString();
						DebugLogger.LogMessage("BVEC_ATS " + version + " loaded");
					}
					if (section == "legacykeyassignments")
					{
						DebugLogger.LogMessage("Using legacy upgraded key assignments");
						CurrentKeyConfiguration = new KeyConfiguration(true);
					}
				}
			}
			foreach (string t in lines)
			{
				string line = t;
				int semicolon = line.IndexOf(';');
				if (semicolon >= 0) {
					line = line.Substring(0, semicolon).Trim();
				} else {
					line = line.Trim();
				}
				if (line.Length != 0) {
					if (line[0] == '[' & line[line.Length - 1] == ']') {
						section = line.Substring(1, line.Length - 2).ToLowerInvariant();
						switch (section) {
							case "steam":
								this.SteamEngine = new steam(this);
								DebugLogger.LogMessage("Steam traction enabled");
								break;
							case "diesel":
								this.DieselEngine = new Diesel(this);
								DebugLogger.LogMessage("Diesel traction enabled");
								break;
							case "electric":
								this.ElectricEngine = new Electric(this);
								DebugLogger.LogMessage("Electric traction enabled");
								break;
							case "vigilance":
								this.Vigilance = new Vigilance(this);
								DebugLogger.LogMessage("Vigilance system(s) enabled");
								break;
							case "aws":
								LoadAWSTPWS();
								this.AWS.Enabled = true;
								DebugLogger.LogMessage("AWS enabled");
								break;
							case "tpws":
								LoadAWSTPWS();
								this.TPWS.Enabled = true;
								DebugLogger.LogMessage("TPWS enabled");
								break;
							case "scmt":
								this.SCMT.Enabled = true;
								this.SCMT_Traction.Enabled = true;
								DebugLogger.LogMessage("SCMT enabled");
								break;
							case "caws":
								this.CAWS = new CAWS(this);
								this.CAWS.Enabled = true;
								DebugLogger.LogMessage("CAWS enabled");
								break;
							case "pzb":
								this.PZB = new PZB(this);
								this.PZB.Enabled = true;
								DebugLogger.LogMessage("PZB enabled");
								break;
							case "animations":
							case "debug":
							case "interlocks":
							case "keyassignments":
							case "legacykeyassignments":
							case "settings":
								//These don't necessarily need their own parser settings
								break;
#if !DebugNBS
							case "ledlights":
								this.LedLights = new LEDLights(this);
								break;
#endif

								break;
							case "windscreen":
								this.Windscreen.enabled = true;
								DebugLogger.LogMessage("Windscreen enabled");
								break;
							case "ats-sx":
								this.AtsSx = new AtsSx(this);
								break;
							case "ats-ps":
								this.AtsPs = new AtsPs(this);
								break;
							case "ats-p":
								this.AtsP = new AtsP(this);
								break;
							case "atc":
								this.Atc = new Atc(this);
								break;
							case "eb":
								this.Eb = new Eb(this);
								break;
							case "tasc":
								this.Tasc = new Tasc(this);
								break;
							case "ato":
								this.Ato = new Ato(this);
								break;
							case "westerndiesel":
								this.WesternDiesel = new WesternDiesel(this);
								break;
							default:
								throw new InvalidDataException("The section " + section + " is not supported.");
						}
					} else {
						int equals = line.IndexOf('=');
						if (@equals >= 0) {
							string key = line.Substring(0, @equals).Trim().ToLowerInvariant();
							string value = line.Substring(@equals + 1).Trim();
							string[] splitValue = value.Split(',');
							switch (section) {
								case "steam":
									switch (key) {
										case "automatic":
											InternalFunctions.ParseBool(value, ref TractionManager.AutomaticAdvancedFunctions, key);
											break;
										case "heatingpart":
											InternalFunctions.ValidateSetting(value, ref SteamEngine.heatingpart, key);
											break;
										case "heatingrate":
											this.SteamEngine.heatingrate = value;
											break;
										case "overheatwarn":
											InternalFunctions.ParseNumber(value, ref SteamEngine.overheatwarn, key);
											break;
										case "overheat":
											InternalFunctions.ParseNumber(value, ref SteamEngine.overheat, key);
											break;
										case "overheatresult":
											InternalFunctions.ValidateSetting(value, ref SteamEngine.overheatresult, key);
											break;
										case "thermometer":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.thermometer, key);
											break;
										case "overheatindicator":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.overheatindicator, key);
											break;
										case "overheatalarm":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.OverheatAlarm.LoopSound, key);
											break;
										case "cutoffmax":
											InternalFunctions.ParseNumber(value, ref SteamEngine.cutoffmax, key);
											break;
										case "cutoffineffective":
											InternalFunctions.ParseNumber(value, ref SteamEngine.cutoffineffective, key);
											break;
										case "cutoffratio":
											InternalFunctions.ParseNumber(value, ref SteamEngine.cutoffratio, key);
											break;
										case "cutoffratiobase":
											InternalFunctions.ParseNumber(value, ref SteamEngine.cutoffratiobase, key);
											break;
										case "cutoffmin":
											InternalFunctions.ParseNumber(value, ref SteamEngine.cutoffmin, key);
											break;
										case "cutoffdeviation":
											InternalFunctions.ParseNumber(value, ref SteamEngine.cutoffdeviation, key);
											break;
										case "cutoffindicator":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.cutoffindicator, key);
											break;
										case "boilermaxpressure":
											InternalFunctions.ParseNumber(value, ref SteamEngine.boilermaxpressure, key);
											break;
										case "boilerminpressure":
											InternalFunctions.ParseNumber(value, ref SteamEngine.boilerminpressure, key);
											break;
										case "boilerstartwaterlevel":
											InternalFunctions.ParseNumber(value, ref SteamEngine.boilerstartwaterlevel, key);
											break;
										case "boilermaxwaterlevel":
											InternalFunctions.ParseNumber(value, ref SteamEngine.boilermaxwaterlevel, key);
											break;
										case "boilerpressureindicator":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.boilerpressureindicator, key);
											break;
										case "boilerwaterlevelindicator":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.boilerwaterlevelindicator, key);
											break;
										case "boilerwatertosteamrate":
											InternalFunctions.ParseNumber(value, ref SteamEngine.boilerwatertosteamrate, key);
											break;
										case "fuelindicator":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.fuelindicator, key);
											break;
										case "fuelstartamount":
											InternalFunctions.ParseNumber(value, ref SteamEngine.fuelstartamount, key);
											break;
										case "fuelcapacity":
											InternalFunctions.ParseNumber(value, ref SteamEngine.fuelcapacity, key);
											break;
										case "fuelfillspeed":
											InternalFunctions.ParseNumber(value, ref SteamEngine.fuelfillspeed, key);
											break;
										case "fuelfillindicator":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.fuelfillindicator, key);
											break;
										case "injectorrate":
										case "livesteaminjectorrate":
											//By default, OS_ATS uses a single live-steam injector, which shares a common water and steam rate
											for (int k = 0; k < splitValue.Length; k++)
											{
												switch (k)
												{
													case 0:
														InternalFunctions.ParseNumber(splitValue[k], ref SteamEngine.LiveSteamInjector.WaterRate, key);
														if (splitValue.Length == 1)
														{
															InternalFunctions.ParseNumber(splitValue[k], ref SteamEngine.LiveSteamInjector.SteamRate, key);
														}
														break;
													case 1:
														InternalFunctions.ParseNumber(splitValue[k], ref SteamEngine.LiveSteamInjector.SteamRate, key);
														break;
													default:
														InternalFunctions.LogError("Unexpected extra paramaters were found in " + key + ". These have been ignored.", 6);
														break;
												}
											}
											InternalFunctions.ParseNumber(value, ref SteamEngine.LiveSteamInjector.WaterRate, key);
											break;
										case "exhauststeaminjectorrate":
											for (int k = 0; k < splitValue.Length; k++)
											{
												switch (k)
												{
													case 0:
														InternalFunctions.ParseNumber(splitValue[k], ref SteamEngine.LiveSteamInjector.WaterRate, key);
														break;
													case 1:
														InternalFunctions.ParseNumber(splitValue[k], ref SteamEngine.LiveSteamInjector.MinimumPowerNotch, key);
														break;
													default:
														InternalFunctions.LogError("Unexpected extra paramaters were found in " + key + ". These have been ignored.", 6);
														break;
												}
											}
											break;
										case "injectorindicator":
										case "livesteaminjectorindicator":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.LiveSteamInjector.PanelIndex, key);
											break;
										case "exhauststeaminjectorindicator":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.ExhaustSteamInjector.PanelIndex, key);
											break;
										case "automaticindicator":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.automaticindicator, key);
											break;
										case "injectorsound":
										case "livesteaminjectorsound":
												splitValue = value.Split(',');
												for (int k = 0; k < splitValue.Length; k++)
												{
													switch (k)
													{
														case 0:
															InternalFunctions.ValidateIndex(splitValue[k], ref SteamEngine.LiveSteamInjector.LoopSound, key);
															break;
														case 1:
															InternalFunctions.ValidateIndex(splitValue[k], ref SteamEngine.LiveSteamInjector.PlayOnceSound, key);
															break;
														default:
															InternalFunctions.LogError("Unexpected extra paramaters were found in " + key + ". These have been ignored.", 6);
															break;
													}
												}
											break;
										case "exhauststeaminjectorsound":
											splitValue = value.Split(',');
											for (int k = 0; k < splitValue.Length; k++)
											{
												switch (k)
												{
													case 0:
														InternalFunctions.ValidateIndex(splitValue[k], ref SteamEngine.ExhaustSteamInjector.LoopSound, key);
														break;
													case 1:
														InternalFunctions.ValidateIndex(splitValue[k], ref SteamEngine.ExhaustSteamInjector.PlayOnceSound, key);
														break;
													default:
														InternalFunctions.LogError("Unexpected extra paramaters were found in " + key + ". These have been ignored.", 6);
														break;
												}
											}
											break;
										case "cylindercocksound":
											string[] cylindersplit = value.Split(',');
											for (int k = 0; k < cylindersplit.Length; k++)
											{
												if (k == 0)
												{
													InternalFunctions.ValidateIndex(cylindersplit[0], ref SteamEngine.CylinderCocks.LoopSound, key);
												}
												else if(k == 1)
												{
													InternalFunctions.ValidateIndex(cylindersplit[1], ref SteamEngine.CylinderCocks.PlayOnceSound, key);
												}
												else
												{
													InternalFunctions.LogError("Unexpected extra paramaters were found in cylindercocksound. These have been ignored.",6);
													break;
												}
											}
											break;
										case "cylindercocksindicator":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.CylinderCocks.PanelIndex, key);
											break;
										case "blowoffsound":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.Blowoff.SoundIndex, key);
											break;
										case "blowoffindicator":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.Blowoff.PanelIndex, key);
											break;
										case "blowofftime":
											InternalFunctions.ParseNumber(value, ref SteamEngine.Blowoff.BlowoffTime, key);
											break;
										case "klaxonpressureuse":
											InternalFunctions.ParseNumber(value, ref SteamEngine.klaxonpressureuse, key);
											break;
										case "blowers_pressurefactor":
											InternalFunctions.ParseNumber(value, ref SteamEngine.Blowers.PressureIncreaseFactor, key);
											break;
										case "blowers_firefactor":
											InternalFunctions.ParseNumber(value, ref SteamEngine.Blowers.PressureIncreaseFactor, key);
											break;
										case "blowersound":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.Blowers.LoopSound, key);
											break;
										case "blowersindicator":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.Blowers.PanelIndex, key);
											break;
										case "steamheatindicator":
											InternalFunctions.ValidateIndex(value, ref SteamEngine.steamheatindicator, key);
											break;
										case "steamheatpressureuse":
											InternalFunctions.ParseNumber(value, ref SteamEngine.steamheatpressureuse, key);
											break;
										case "boilerblowoffpressure":
											InternalFunctions.ParseNumber(value, ref SteamEngine.Blowoff.TriggerPressure, key);
											break;
										case "regulatorpressureuse":
											InternalFunctions.ParseNumber(value, ref SteamEngine.regulatorpressureuse, key);
											break;
										case "cylindercocks_pressureuse":
											string[] cylindercocksplit = value.Split(',');
											for (int k = 0; k < cylindercocksplit.Length; k++)
											{
												if (k == 0)
												{
													InternalFunctions.ParseNumber(cylindercocksplit[0], ref SteamEngine.cylindercocks_basepressureuse, key);
												}
												else
												{
													InternalFunctions.ParseNumber(cylindercocksplit[1], ref SteamEngine.cylindercocks_notchpressureuse, key);
												}
											}
											break;
										default:
											throw new InvalidDataException("The parameter " + key + " is not supported.");
									}

									break;
								case "electric":
									switch (key)
									{
										case "heatingpart":
											InternalFunctions.ValidateSetting(value, ref ElectricEngine.heatingpart, key);
											break;
										case "heatingrate":
											InternalFunctions.ParseStringToIntArray(value, ref ElectricEngine.HeatingRates, "heatingrate");
											break;
										case "overheatwarn":
											InternalFunctions.ParseNumber(value, ref ElectricEngine.overheatwarn, key);
											break;
										case "overheat":
											InternalFunctions.ParseNumber(value, ref ElectricEngine.overheat, key);
											break;
										case "overheatresult":
											InternalFunctions.ValidateSetting(value, ref ElectricEngine.overheatresult, key);
											break;
										case "thermometer":
											InternalFunctions.ValidateIndex(value, ref ElectricEngine.thermometer, key);
											break;
										case "overheatindicator":
											InternalFunctions.ValidateIndex(value, ref ElectricEngine.overheatindicator, key);
											break;
										case "overheatalarm":
											InternalFunctions.ValidateIndex(value, ref ElectricEngine.overheatalarm, key);
											break;
										case "ammeter":
											InternalFunctions.ValidateIndex(value, ref ElectricEngine.Ammeter.PanelIndex, key);
											break;
										case "ammetervalues":
											this.ElectricEngine.Ammeter.Initialize(value);
											break;
										case "powerpickuppoints":
											InternalFunctions.ParseStringToIntArray(value, ref ElectricEngine.PickupLocations, "pickuppoints");
											break;
										case "powergapbehaviour":
											int pgb = 0;
											InternalFunctions.ValidateSetting(value, ref pgb, key);
											if (pgb < 0 || pgb > 3)
											{
												InternalFunctions.LogError("powergapbehaviour",0);
												break;
											}
											ElectricEngine.PowerGapBehaviour = (PowerGapBehaviour) pgb;
											break;
										case "powerindicator":
											InternalFunctions.ValidateIndex(value, ref ElectricEngine.powerindicator, key);
											break;
										case "breakersound":
											InternalFunctions.ValidateIndex(value, ref ElectricEngine.breakersound, key);
											break;
										case "breakerindicator":
											InternalFunctions.ValidateIndex(value, ref ElectricEngine.breakerindicator, key);
											break;
										case "pantographindicator_f":
											InternalFunctions.ValidateIndex(value, ref ElectricEngine.FrontPantograph.PanelIndex, key);
											break;
										case "pantographindicator_r":
											InternalFunctions.ValidateIndex(value, ref ElectricEngine.RearPantograph.PanelIndex, key);
											break;
										case "pantographraisedsound":
											InternalFunctions.ValidateIndex(value, ref ElectricEngine.FrontPantograph.RaisedSound, key);
											InternalFunctions.ValidateIndex(value, ref ElectricEngine.RearPantograph.RaisedSound, key);
											break;
										case "pantographloweredsound":
											InternalFunctions.ValidateIndex(value, ref ElectricEngine.FrontPantograph.LoweredSound, key);
											InternalFunctions.ValidateIndex(value, ref ElectricEngine.RearPantograph.LoweredSound, key);
											break;
										case "pantographalarmsound":
											InternalFunctions.ValidateIndex(value, ref ElectricEngine.FrontPantograph.AlarmSound, key);
											InternalFunctions.ValidateIndex(value, ref ElectricEngine.RearPantograph.AlarmSound, key);
											break;
										case "pantographretryinterval":
											InternalFunctions.ParseNumber(value, ref ElectricEngine.pantographretryinterval, key);
											break;
										case "pantographalarmbehaviour":
											InternalFunctions.ValidateSetting(value, ref ElectricEngine.pantographalarmbehaviour, key);
											break;
										case "pantographfitted":
											bool b = true;
											InternalFunctions.ParseBool(value, ref b, key);
											if (!b)
											{
												CurrentKeyConfiguration.FrontPantograph = null;
												CurrentKeyConfiguration.RearPantograph = null;
											}
											break;
										case "powerloopsound":
											try
											{
												string[] powerloopsplit = value.Split(',');
												for (int k = 0; k < powerloopsplit.Length; k++)
												{
													if (k == 0)
													{
														this.ElectricEngine.powerloopsound = Convert.ToInt32(powerloopsplit[0]);
													}
													else
													{
														this.ElectricEngine.powerlooptime = Convert.ToInt32(powerloopsplit[1]);
													}
												}
											}
											catch
											{
												InternalFunctions.LogError("powerloopsound",0);
											}
											break;
										case "breakerloopsound":
											try
											{
												string[] breakerloopsplit = value.Split(',');
												for (int k = 0; k < breakerloopsplit.Length; k++)
												{
													if (k == 0)
													{
														this.ElectricEngine.breakerloopsound = Convert.ToInt32(breakerloopsplit[0]);
													}
													else
													{
														this.ElectricEngine.breakerlooptime = Convert.ToInt32(breakerloopsplit[1]);
													}
												}
											}
											catch
											{
												InternalFunctions.LogError("breakerloopsound",0);
											}
											break;
										case "automaticpantographlowerbehaviour":
											int s = -1;
											InternalFunctions.ValidateSetting(value, ref s, key);
											if (s < 0 || s > 5)
											{
												InternalFunctions.LogError("AutomaticPantographLowerBehaviour was not within the range of valid values.",6);
												break;
											}
											ElectricEngine.PantographLoweringMode = (Electric.AutomaticPantographLoweringModes)s;
											break;
										case "automaticpantographlowerspeed":
											InternalFunctions.ParseNumber(value, ref ElectricEngine.AutomaticPantographLowerSpeed, key);
											break;
										default:
											throw new InvalidDataException("The parameter " + key + " is not supported.");

									}
									break;
								case "diesel":
									switch (key)
									{
										case "ammeter":
											InternalFunctions.ValidateIndex(value, ref DieselEngine.Ammeter.PanelIndex, key);
											break;
										case "ammetervalues":
											this.DieselEngine.Ammeter.Initialize(value);
											break;
										case "automatic":
											InternalFunctions.ParseBool(value, ref TractionManager.AutomaticAdvancedFunctions, key);
											break;
										case "heatingpart":
											InternalFunctions.ValidateSetting(value, ref DieselEngine.heatingpart, key);
											break;
										case "heatingrate":
											this.DieselEngine.heatingrate = value;
											break;
										case "overheatwarn":
											InternalFunctions.ParseNumber(value, ref DieselEngine.overheatwarn, key);
											break;
										case "overheat":
											InternalFunctions.ParseNumber(value, ref DieselEngine.overheat, key);
											break;
										case "overheatresult":
											InternalFunctions.ValidateSetting(value, ref DieselEngine.overheatresult, key);
											break;
										case "thermometer":
											InternalFunctions.ValidateIndex(value, ref DieselEngine.thermometer, key);
											break;
										case "overheatindicator":
											InternalFunctions.ValidateIndex(value, ref DieselEngine.overheatindicator, key);
											break;
										case "overheatalarm":
											InternalFunctions.ValidateIndex(value, ref DieselEngine.overheatalarm, key);
											break;
										case "fuelstartamount":
											InternalFunctions.ParseNumber(value, ref DieselEngine.fuelstartamount, key);
											break;
										case "fuelindicator":
											InternalFunctions.ValidateIndex(value, ref DieselEngine.fuelindicator, key);
											break;
										case "automaticindicator":
											InternalFunctions.ValidateIndex(value, ref DieselEngine.automaticindicator, key);
											break;  
										case "gearratios":
											this.DieselEngine.gearratios = value;
											break;
										case "gearfadeinrange":
											this.DieselEngine.gearfadeinrange = value;
											break;
										case "gearfadeoutrange":
											this.DieselEngine.gearfadeoutrange = value;
											break;
										case "gearindicator":
											InternalFunctions.ValidateIndex(value, ref DieselEngine.gearindicator, key);
											break;
										case "gearchangesound":
											InternalFunctions.ValidateIndex(value, ref DieselEngine.gearchangesound, key);
											break;
										case "tachometer":
											InternalFunctions.ValidateIndex(value, ref DieselEngine.tachometer, key);
											break;
										case "allowneutralrevs":
											InternalFunctions.ValidateSetting(value, ref DieselEngine.allowneutralrevs, key);
											break;
										case "revsupsound":
											InternalFunctions.ValidateIndex(value, ref DieselEngine.revsupsound, key);
											break;
										case "revsdownsound":
											InternalFunctions.ValidateIndex(value, ref DieselEngine.revsdownsound, key);
											break;
										case "motorsound":
											InternalFunctions.ValidateIndex(value, ref DieselEngine.motorsound, key);
											break;
										case "fuelconsumption":
											this.DieselEngine.fuelconsumption = value;
											break;
										case "fuelcapacity":
											InternalFunctions.ParseNumber(value, ref DieselEngine.fuelcapacity, key);
											break;
										case "fuelfillspeed":
											InternalFunctions.ParseNumber(value, ref DieselEngine.fuelfillspeed, key);
											break;
										case "fuelfillindicator":
											InternalFunctions.ValidateIndex(value, ref DieselEngine.fuelfillindicator, key);
											break;
										case "gearloopsound":
											try
											{
												string[] gearloopsplit = value.Split(',');
												for (int k = 0; k < gearloopsplit.Length; k++)
												{
													if (k == 0)
													{
														this.DieselEngine.gearloopsound = Convert.ToInt32(gearloopsplit[0]);
													}
													else
													{
														this.DieselEngine.gearlooptime = Convert.ToInt32(gearloopsplit[1]);
													}
												}
											}
											catch
											{
												InternalFunctions.LogError("gearloopsound",0);
											}
											break;
										case "reversercontrol":
											InternalFunctions.ValidateSetting(value, ref DieselEngine.reversercontrol, key);
											break;
										default:
											throw new InvalidDataException("The parameter " + key + " is not supported.");

									}
									break;
								case "westerndiesel":
									switch (key)
									{
										case "ilcluster1":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.ILCluster1, key);
											break;
										case "ilcluster2":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.ILCluster2, key);
											break;
										case "masterkey":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.MasterKeyIndex, key);
											break;
										case "startdelay":
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine1Starter.StartDelay, key);
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine2Starter.StartDelay, key);
											break;
										case "fireupdelay":
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine1Starter.FireUpDelay, key);
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine2Starter.FireUpDelay, key);
											break;
										case "rundowndelay":
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine1Starter.RunDownDelay, key);
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine2Starter.RunDownDelay, key);
											break;
										case "dsdsound":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.DSDBuzzer, key);
											break;
										case "neutralselectedsound":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.NeutralSelectedSound, key);
											break;
										case "engineloopsound":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.EngineLoopSound, key);
											break;
										case "turborunupsound":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.Turbocharger.TurbochargerRunUpSound, key);
											break;
										case "turboloopsound":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.Turbocharger.TurbochargerLoopSound, key);
											break;
										case "turborundownsound":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.Turbocharger.TurbochargerRunDownSound, key);
											break;
										case "enginefiresound":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.Engine1Starter.EngineFireSound, key);
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.Engine2Starter.EngineFireSound, key);
											break;
										case "enginestallsound":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.Engine1Starter.EngineStallSound, key);
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.Engine2Starter.EngineStallSound, key);
											break;
										case "enginestopsound":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.EngineStopSound, key);
											break;
										case "starterloopsound":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.Engine1Starter.StarterLoopSound, key);
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.Engine2Starter.StarterLoopSound, key);
											break;
										case "starterrunupsound":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.Engine1Starter.StarterRunUpSound, key);
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.Engine2Starter.StarterRunUpSound, key);
											break;
										case "switchsound":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.SwitchSound, key);
											break;
										case "masterkeysound":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.MasterKeySound, key);
											break;
										case "firebellsound":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.FireBellSound, key);
											break;
										case "firebellindex":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.FireBellIndex, key);
											break;
										case "voltsgauge":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.BatteryVoltsGauge, key);
											break;
										case "ampsgauge":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.BatteryChargeGauge, key);
											break;
										case "rpmgauge1":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.RPMGauge1, key);
											break;
										case "rpmgauge2":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.RPMGauge2, key);
											break;
										case "engine1button":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.Engine1Button, key);
											break;
										case "engine2button":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.Engine2Button, key);
											break;
										case "fuelpumpswitch":
											InternalFunctions.ValidateIndex(value, ref WesternDiesel.FuelPumpSwitchIndex, key);
											break;
										case "exhaust1":
											try
											{
												string[] exhaustsplit = value.Split(',');
												for (int k = 0; k < exhaustsplit.Length; k++)
												{
													if (k == 0)
													{
														InternalFunctions.ValidateIndex(exhaustsplit[k], ref WesternDiesel.Engine1Smoke, key);
													}
													else
													{
														InternalFunctions.ParseNumber(exhaustsplit[k], ref WesternDiesel.Engine1Sparks, key);
													}
												}
											}
											catch
											{
												InternalFunctions.LogError("exhaust1", 0);
											}
											break;
										case "exhaust2":
											try
											{
												string[] exhaustsplit2 = value.Split(',');
												for (int k = 0; k < exhaustsplit2.Length; k++)
												{
													if (k == 0)
													{
														InternalFunctions.ValidateIndex(exhaustsplit2[k], ref WesternDiesel.Engine2Smoke, key);
													}
													else
													{
														InternalFunctions.ParseNumber(exhaustsplit2[k], ref WesternDiesel.Engine2Sparks, key);
													}
												}
											}
											catch
											{
												InternalFunctions.LogError("exhaust2", 0);
											}
											break;
										case "rpmchangerate":
											InternalFunctions.ParseNumber(value, ref WesternDiesel.RPMChange, key);
											break;
										case "fireprobability":
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine1Starter.FireProbability, key);
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine2Starter.FireProbability, key);
											break;
										case "maximumfireprobability":
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine1Starter.MaximumFireProbability, key);
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine2Starter.MaximumFireProbability, key);
											break;
										case "stallprobability":
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine1Starter.StallProbability, key);
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine2Starter.StallProbability, key);
											break;
										case "maximumstallprobability":
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine1Starter.MaximumStallProbability, key);
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine2Starter.MaximumStallProbability, key);
											break;
										case "temperaturechangerate":
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine1Temperature.HeatingRate, key);
											InternalFunctions.ParseNumber(value, ref WesternDiesel.Engine2Temperature.HeatingRate, key);
											InternalFunctions.ParseNumber(value, ref WesternDiesel.TransmissionTemperature.HeatingRate, key);
											break;
										case "enginetemperature":
											try
											{
												string[] temperaturesplit = value.Split(',');
												if (temperaturesplit.Length != 4)
												{
													WesternDiesel.Engine1Temperature.MaximumTemperature = 1500;
													WesternDiesel.Engine1Temperature.OverheatTemperature = 1200;
													WesternDiesel.Engine1Temperature.FloorTemperature = 500;
													WesternDiesel.Engine1Temperature.ResetTemperature = 1000;
													WesternDiesel.Engine2Temperature.MaximumTemperature = 1500;
													WesternDiesel.Engine2Temperature.OverheatTemperature = 1200;
													WesternDiesel.Engine2Temperature.FloorTemperature = 500;
													WesternDiesel.Engine2Temperature.ResetTemperature = 1000;
													InternalFunctions.LogError("enginetemperature", 0);
												}
												for (int k = 0; k < temperaturesplit.Length; k++)
												{
													switch (k)
													{
														case 0:
															InternalFunctions.ParseNumber(temperaturesplit[k], ref WesternDiesel.Engine1Temperature.MaximumTemperature, key);
															InternalFunctions.ParseNumber(temperaturesplit[k], ref WesternDiesel.Engine2Temperature.MaximumTemperature, key);
															break;
														case 1:
															InternalFunctions.ParseNumber(temperaturesplit[k], ref WesternDiesel.Engine1Temperature.OverheatTemperature, key);
															InternalFunctions.ParseNumber(temperaturesplit[k], ref WesternDiesel.Engine2Temperature.OverheatTemperature, key);
															break;
														case 2:
															InternalFunctions.ParseNumber(temperaturesplit[k], ref WesternDiesel.Engine1Temperature.FloorTemperature, key);
															InternalFunctions.ParseNumber(temperaturesplit[k], ref WesternDiesel.Engine2Temperature.FloorTemperature, key);
															break;
														case 3:
															InternalFunctions.ParseNumber(temperaturesplit[k], ref WesternDiesel.Engine1Temperature.ResetTemperature, key);
															InternalFunctions.ParseNumber(temperaturesplit[k], ref WesternDiesel.Engine2Temperature.ResetTemperature, key);
															break;
													}
												}

											}
											catch
											{
												InternalFunctions.LogError("enginetemperature", 0);
											}
											break;
										case "transmissiontemperature":
											try
											{
												string[] temperaturesplit = value.Split(',');
												if (temperaturesplit.Length != 4)
												{
													WesternDiesel.TransmissionTemperature.MaximumTemperature = 1500;
													WesternDiesel.TransmissionTemperature.OverheatTemperature = 1200;
													WesternDiesel.TransmissionTemperature.FloorTemperature = 500;
													WesternDiesel.TransmissionTemperature.ResetTemperature = 1000;
													InternalFunctions.LogError("enginetemperature", 0);
												}
												for (int k = 0; k < temperaturesplit.Length; k++)
												{
													switch (k)
													{
														case 0:
															InternalFunctions.ParseNumber(temperaturesplit[k], ref WesternDiesel.TransmissionTemperature.MaximumTemperature, key);
															break;
														case 1:
															InternalFunctions.ParseNumber(temperaturesplit[k], ref WesternDiesel.TransmissionTemperature.OverheatTemperature, key);
															break;
														case 2:
															InternalFunctions.ParseNumber(temperaturesplit[k], ref WesternDiesel.TransmissionTemperature.FloorTemperature, key);
															break;
														case 3:
															InternalFunctions.ParseNumber(temperaturesplit[k], ref WesternDiesel.TransmissionTemperature.ResetTemperature, key);
															break;
													}
												}

											}
											catch
											{
												InternalFunctions.LogError("transmissiontemperature", 0);
											}
											break;
									}
									break;
								case "vigilance":
									switch (key)
									{
										case "overspeedcontrol":
											int osc = 0;
											InternalFunctions.ValidateSetting(value, ref osc, key);
											switch (osc)
											{
												case 0:
													Vigilance.OverspeedDevice.CurrentBehaviour = OverspeedMonitor.OverspeedBehaviour.None;
													break;
												case 1:
													Vigilance.OverspeedDevice.CurrentBehaviour = OverspeedMonitor.OverspeedBehaviour.ApplyServiceBrakes;
													break;
												case 2:
													Vigilance.OverspeedDevice.CurrentBehaviour = OverspeedMonitor.OverspeedBehaviour.ApplyEmergencyBrakes;
													break;
												case 3:
													Vigilance.OverspeedDevice.CurrentBehaviour = OverspeedMonitor.OverspeedBehaviour.CutoffPower;
													break;

											}
											break;
										case "warningspeed":
											InternalFunctions.ParseNumber(value, ref Vigilance.OverspeedDevice.WarningSpeed, key);
											break;
										case "overspeed":
											InternalFunctions.ParseNumber(value, ref Vigilance.OverspeedDevice.OverSpeed, key);
											break;
										case "safespeed":
											InternalFunctions.ParseNumber(value, ref Vigilance.OverspeedDevice.SafeSpeed, key);
											break;
										case "overspeedindicator":
											InternalFunctions.ValidateIndex(value, ref Vigilance.OverspeedDevice.PanelIndicator, key);
											break;
										case "overspeedalarm":
											InternalFunctions.ValidateIndex(value, ref Vigilance.OverspeedDevice.AlarmSound, key);
											break;
										case "overspeedtime":
											InternalFunctions.ParseNumber(value, ref Vigilance.OverspeedDevice.MaximumTimeOverspeed, key);
											break;
										case "vigilanceinterval":
											this.Vigilance.vigilancetimes = value;
											break;
										case "vigilancelamp":
											InternalFunctions.ValidateIndex(value, ref Vigilance.vigilancelamp, key);
											break;
										case "deadmanshandle":
											InternalFunctions.ValidateSetting(value, ref Vigilance.deadmanshandle, key);
											break;
										case "vigilanceautorelease":
											InternalFunctions.ParseBool(value, ref Vigilance.AutoRelease, key);
											break;
										case "vigilancecancellable":
											InternalFunctions.ValidateSetting(value, ref Vigilance.vigilancecancellable, key);
											break;
										case "independentvigilance":
											InternalFunctions.ValidateSetting(value, ref Vigilance.independantvigilance, key);
											break;
										case "draenabled":
											InternalFunctions.ParseBool(value, ref Vigilance.DRAEnabled, key);
											break;
										case "drastartstate":
											InternalFunctions.ValidateSetting(value, ref Vigilance.DRAStartState, key);
											break;
										case "draindicator":
											InternalFunctions.ValidateIndex(value, ref Vigilance.DRAIndicator, key);
											break;
										case "vigilancealarm":
											InternalFunctions.ValidateIndex(value, ref Vigilance.vigilancealarm, key);
											break;
										case "vigilancedelay1":
											InternalFunctions.ParseNumber(value, ref Vigilance.vigilancedelay1, key);
											break;
										case "vigilancedelay2":
											InternalFunctions.ParseNumber(value, ref Vigilance.vigilancedelay2, key);
											break;
										case "vigilanceinactivespeed":
											InternalFunctions.ParseNumber(value, ref Vigilance.vigilanceinactivespeed, key);
											break;
										case "vigilante":
											InternalFunctions.ParseBool(value, ref Vigilance.vigilante, key);
											break;
										default:
											throw new InvalidDataException("The parameter " + key + " is not supported.");
											
									}
									break;
								case "interlocks":
									switch (key)
									{
										case "doorpowerlock":
											InternalFunctions.ValidateSetting(value, ref TractionManager.doorpowerlock, key);
											break;
										case "doorapplybrake":
											InternalFunctions.ValidateSetting(value, ref TractionManager.doorapplybrake, key);
											break;
										case "neutralrvrbrake":
											int nrb = -1;
											InternalFunctions.ValidateSetting(value, ref nrb, key);
											switch (nrb)
											{
												case 0:
													TractionManager.CurrentReverserManager.CurrentBehaviour = ReverserManager.NeutralBehaviour.NoChange;
													break;
												case 1:
													TractionManager.CurrentReverserManager.CurrentBehaviour = ReverserManager.NeutralBehaviour.ServiceBrakes;
													break;
												case 2:
													TractionManager.CurrentReverserManager.CurrentBehaviour = ReverserManager.NeutralBehaviour.EmergencyBrakes;
													break;
											}
											break;
										case "neutralrvrbrakereset":
											int nrbr = -1;
											InternalFunctions.ValidateSetting(value, ref nrbr, key);
											switch (nrbr)
											{
												case 0:
													TractionManager.CurrentReverserManager.CurrentResetBehaviour = ReverserManager.ResetBehaviour.LeaveNeutral;
													break;
												case 1:
													TractionManager.CurrentReverserManager.CurrentResetBehaviour = ReverserManager.ResetBehaviour.FullStand;
													break;
												case 2:
													TractionManager.CurrentReverserManager.CurrentResetBehaviour = ReverserManager.ResetBehaviour.FullStandServiceBrakes;
													break;
												case 3:
													TractionManager.CurrentReverserManager.CurrentResetBehaviour = ReverserManager.ResetBehaviour.FullStandEmergencyBrakes;
													break;
											}
											break;
										case "neutralrvrbrakeindicator":
											InternalFunctions.ValidateIndex(value, ref TractionManager.CurrentReverserManager.AlarmPanelIndex, key);
											break;
										case "neutralrvrbrakesound":
											string[] splitnrb = value.Split(',');
											InternalFunctions.ValidateIndex(splitnrb[0], ref TractionManager.CurrentReverserManager.AlarmSound, key);
											if (splitnrb.Length == 1)
											{
												TractionManager.CurrentReverserManager.AlarmLooped = true;
												break;
											}
											InternalFunctions.ParseBool(splitnrb[1], ref TractionManager.CurrentReverserManager.AlarmLooped, key);
											break;
										case "directionindicator":
											InternalFunctions.ValidateIndex(value, ref TractionManager.directionindicator, key);
											break;
										case "reverserindex":
											InternalFunctions.ValidateIndex(value, ref TractionManager.reverserindex, key);
											break;
										case "travelmeter1000":
											InternalFunctions.ValidateIndex(value, ref TractionManager.travelmeter1000, key);
											break;
										case "travelmeter100":
											InternalFunctions.ValidateIndex(value, ref TractionManager.travelmeter100, key);
											break;
										case "travelmeter10":
											InternalFunctions.ValidateIndex(value, ref TractionManager.travelmeter10, key);
											break;
										case "travelmeter1":
											InternalFunctions.ValidateIndex(value, ref TractionManager.travelmeter1, key);
											break;
										case "travelmeter01":
											InternalFunctions.ValidateIndex(value, ref TractionManager.travelmeter01, key);
											break;
										case "travelmeter001":
											InternalFunctions.ValidateIndex(value, ref TractionManager.travelmeter001, key);
											break;
										case "travelmetermode":
											int mode = -1;
											InternalFunctions.ValidateSetting(value, ref mode, key);
											switch (mode)
											{
												case 0:
													TractionManager.CurrentTravelMeter.Units = TractionManager.TravelMeterUnits.Kilometers;
													break;
												case 1:
													TractionManager.CurrentTravelMeter.Units = TractionManager.TravelMeterUnits.Miles;
													break;
											}
											break;
										case "klaxonindicator":
											this.TractionManager.klaxonindicator = value;
											break;
										case "customindicators":
											this.TractionManager.customindicators = value;
											break;
										case "customindicatorsounds":
											this.TractionManager.customindicatorsounds = value;
											break;
										case "customindicatorbehaviour":
											this.TractionManager.customindicatorbehaviour = value;
											break;
										case "leftdoorindicator":
											InternalFunctions.ValidateIndex(value, ref TractionManager.LeftDoorIndicator, key);
											break;
										case "rightdoorindicator":
											InternalFunctions.ValidateIndex(value, ref TractionManager.RightDoorIndicator, key);
											break;
										case "ukdtmasterswitchindicator":
											InternalFunctions.ValidateIndex(value, ref TractionManager.UKDTMasterSwitchIndicator, key);
											break;
										case "masterswitchindicator":
											InternalFunctions.ValidateIndex(value, ref TractionManager.MasterSwitchIndicator, key);
											break;
										default:
											throw new InvalidDataException("The parameter " + key + " is not supported.");
									}
									break;
									//Handles SCMT & associated traction parameters
							   case "scmt":
									switch (key)
									{
										case "spiablu":
											InternalFunctions.ValidateIndex(value, ref SCMT.spiablue, key);
											break;
										case "spiarossi":
											InternalFunctions.ValidateIndex(value, ref SCMT.spiarossi, key);
											break;
										case "spiascmt":
											InternalFunctions.ValidateIndex(value, ref SCMT.spiaSCMT, key);
											break;
										case "indsr":
											InternalFunctions.ValidateIndex(value, ref SCMT.srIndicator.PanelIndex, key);
											break;
										case "traintrip":
											InternalFunctions.ValidateIndex(value, ref SCMT.TraintripIndicator.PanelIndex, key);
											break;
										case "testscmt":
											InternalFunctions.ValidateIndex(value, ref SCMT.testscmt_variable, key);
											break;
										case "testpulsanti":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.testpulsanti, key);
											break;
										case "suonoscmton":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.sunoscmton, key);
											break;
										case "suonoconfdati":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.sunoconfdati, key);
											break;
										case "suonoinsscmt":
											InternalFunctions.ValidateIndex(value, ref SCMT.sound_scmt, key);
											break;
										case "indlcm":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.indlcm_variable, key);
											break;
										case "indimpvelpressed":
											SCMT_Traction.indimpvelpressed = value;
											break;
										case "indimpvelpressedsu":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.ImpvelSu.PanelIndex, key);
											break;
										case "indimpvelpressedgiu":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.ImpvelGiu.PanelIndex, key);
											break;
										case "suonoimpvel":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.sunoimpvel, key);
											break;
										case "indabbanco":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.Abbanco.PanelIndex, key);
											break;
										case "suonoconsavv":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.sunoconsavv, key);
											break;
										case "indconsavv":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.ConsAvviam.PanelIndex, key);
											break;
										case "indavv":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.Avviam.PanelIndex, key);
											break;
										case "suonoavv":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.sunoavv, key);
											break;
										case "indarr":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.Arresto.PanelIndex, key);
											break;
										case "suonoarr":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.sunoarr, key);
											break;
										case "indattesa":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.indattesa_variable, key);
											break;
										case "indacarrfren":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.indcarrfren, key);
											break;
										case "accensionemot":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.AvariaGen.PanelIndex, key);
											break;
										case "indcontgiri":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.indcontgiri_variable, key);
											break;
										case "indgas":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.indgas_variable, key);
											break;
										case "inddinamometro":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.inddinamometro, key);
											break;
										case "indvoltbatt":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.indvoltbatt, key);
											break;
										case "indspegnmon":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.indspegnmon, key);
											break;
										case "suonosottofondo":
											InternalFunctions.ValidateIndex(value, ref SCMT_Traction.sunosottofondo, key);
											break;
										case "tpwsstopdelay":
											InternalFunctions.ParseNumber(value, ref SCMT.tpwsstopdelay, key);
											break;
									}
									break;
									//Handles CAWS
							   case "caws":
									switch (key)
									{
										case "aspectindicator":
											InternalFunctions.ValidateIndex(value, ref CAWS.AspectIndicator, key);
											break;
										case "ebindicator":
											InternalFunctions.ValidateIndex(value, ref CAWS.EBIndicator, key);
											break;
										case "acknowledgementindicator":
											InternalFunctions.ValidateIndex(value, ref CAWS.AcknowlegementIndicator, key);
											break;
										case "downgradesound":
											InternalFunctions.ValidateIndex(value, ref CAWS.DowngradeSound, key);
											break;
										case "upgradesound":
											InternalFunctions.ValidateIndex(value, ref CAWS.UpgradeSound, key);
											break;
									}
									break;
									//Handles PZB
								case "pzb":
									switch (key)
									{
										case "trainclass":
											InternalFunctions.ValidateSetting(value, ref PZB.trainclass, key);
											break;
										case "distantprogramlength":
											InternalFunctions.ParseNumber(value, ref PZB.DistantProgramLength, key);
											break;
										case "wachamswitch":
											InternalFunctions.ValidateIndex(value, ref PZB.WachamIndicator, key);
											break;
										case "freiswitch":
											InternalFunctions.ValidateIndex(value, ref PZB.FreiIndicator, key);
											break;
										case "Befehlswitch":
											InternalFunctions.ValidateIndex(value, ref PZB.BefehlIndicator, key);
											break;
										case "runninglights":
											InternalFunctions.ValidateIndex(value, ref PZB.RunningLightsStartIndicator, key);
											break;
										case "eblight":
											InternalFunctions.ValidateIndex(value, ref PZB.EBLight, key);
											break;
									}
									break;
								case "aws":
									switch (key)
									{
										case "awsindicator":
											InternalFunctions.ValidateIndex(value, ref AWS.awsindicator, key);
											break;
										case "awswarningsound":
											InternalFunctions.ValidateIndex(value, ref AWS.WarningSound, key);
											break;
										case "awsclearsound":
											InternalFunctions.ValidateIndex(value, ref AWS.ClearSound, key);
											break;
										case "awsdelay":
											InternalFunctions.ParseNumber(value, ref AWS.canceltimeout, key);
											break;
										case "tpwswarningsound":
											InternalFunctions.ValidateIndex(value, ref AWS.TPWSWarningSound, key);
											break;
										case "cancelbuttonindex":
											InternalFunctions.ValidateIndex(value, ref AWS.CancelButtonIndex, key);
											break;
										default:
											throw new InvalidDataException("The parameter " + key + " is not supported.");
									}
									break;
								case "tpws":
									switch (key)
									{
										case "tpwsoverridelifetime":
											InternalFunctions.ParseNumber(value, ref TPWS.overridetimeout, key);
											break;
										case "tpwsstopdelay":
											InternalFunctions.ParseNumber(value, ref TPWS.brakesappliedtimeout, key);
											break;
										case "tpwsindicator":
											try
											{
												string[] tpwssplit1 = value.Split(',');
												for (var j = 0; j < tpwssplit1.Length; j++)
												{
													if (j == 0)
													{
														this.TPWS.brakedemandindicator = Convert.ToInt32(tpwssplit1[0]);
													}
													else
													{
														this.TPWS.brakeindicatorblinkrate = Convert.ToInt32(tpwssplit1[1]);
													}
												}
											}
											catch
											{
												InternalFunctions.LogError("tpwsindicator",0);
											}
											break;
										case "tpwsindicator2":
											try
											{
												string[] tpwssplit2 = value.Split(',');
												for (int k = 0; k < tpwssplit2.Length; k++)
												{
													if (k == 0)
													{
														this.TPWS.twpsoverrideindicator = Convert.ToInt32(tpwssplit2[0]);
													}
													else
													{
														this.TPWS.tpwsoverrideblinkrate = Convert.ToInt32(tpwssplit2[1]);
													}
												}
											}
											catch
											{
												InternalFunctions.LogError("tpwsindicator2",0);
											}
											break;
										case "tpwsindicator4":
											InternalFunctions.ValidateIndex(value, ref TPWS.tpwsisolatedindicator, key);
											break;
										default:
											throw new InvalidDataException("The parameter " + key + " is not supported.");
									}
									break;
								case "windscreen":
									switch (key)
									{
										case "dropstartindex":
											InternalFunctions.ValidateIndex(value, ref Windscreen.dropstartindex, key);
											break;
										case "numberofdrops":
											InternalFunctions.ValidateIndex(value, ref Windscreen.numberofdrops, key);
											break;
										case "wiperindex":
											InternalFunctions.ValidateIndex(value, ref Windscreen.wiperindex, key);
											break;
										case "wiperholdposition":
											InternalFunctions.ParseNumber(value, ref Windscreen.wiperholdposition, key);
											break;
										case "wiperdelay":
											InternalFunctions.ParseNumber(value, ref Windscreen.wiperdelay, key);
											break;
										case "wiperrate":
											InternalFunctions.ParseNumber(value, ref Windscreen.wiperrate, key);
											break;
										case "wiperswitchindex":
											InternalFunctions.ValidateIndex(value, ref Windscreen.wiperswitchindex, key);
											break;
										case "wiperswitchsound":
											InternalFunctions.ValidateIndex(value, ref Windscreen.wiperswitchsound, key);
											break;
										case "dropsound":
											try
											{
												string[] dropsplit = value.Split(',');
												for (int k = 0; k < dropsplit.Length; k++)
												{
													if (k == 0)
													{
														this.Windscreen.dropsound1 = Convert.ToInt32(dropsplit[0]);
													}
													else
													{
														this.Windscreen.dropsound2 = Convert.ToInt32(dropsplit[1]);
													}
												}
											}
											catch
											{
												InternalFunctions.LogError("dropsound",0);
											}
											break;
										case "drywipesound":
											InternalFunctions.ValidateIndex(value, ref Windscreen.drywipesound, key);
											break;
										case "wetwipesound":
											InternalFunctions.ValidateIndex(value, ref Windscreen.wetwipesound, key);
											break;
										case "wipersoundbehaviour":
											InternalFunctions.ValidateSetting(value, ref Windscreen.wipersoundbehaviour, key);
											break;
										default:
											throw new InvalidDataException("The parameter " + key + " is not supported.");
									}
									break;

								case "animations":
									switch (key)
									{
										case "gear_yvariable_r":
											InternalFunctions.ValidateIndex(value, ref Animations.gear_Yvariable_R, key);
											break;
										case "gear_zvariable_r":
											InternalFunctions.ValidateIndex(value, ref Animations.gear_Zvariable_R, key);
											break;
										case "gear_yvariable_l":
											InternalFunctions.ValidateIndex(value, ref Animations.gear_Yvariable_L, key);
											break;
										case "gear_zvariable_l":
											InternalFunctions.ValidateIndex(value, ref Animations.gear_Zvariable_L, key);
											break;
										case "rodradius":
											InternalFunctions.ParseNumber(value, ref Animations.rodradius, key);
											break;
										case "crankradius":
											InternalFunctions.ParseNumber(value, ref Animations.crankradius, key);
											break;
										case "cranklength":
											InternalFunctions.ParseNumber(value, ref Animations.cranklength, key);
											break;
										case "crankvariable_l":
											InternalFunctions.ValidateIndex(value, ref Animations.crankvariable_L, key);
											break;
										case "crankvariable_r":
											InternalFunctions.ValidateIndex(value, ref Animations.crankvariable_R, key);
											break;
										case "crankrotation_l":
											InternalFunctions.ValidateIndex(value, ref Animations.crankrotation_L, key);
											break;
										case "crankrotation_r":
											InternalFunctions.ValidateIndex(value, ref Animations.crankrotation_R, key);
											break;
										case "wheelrotation_variable":
											InternalFunctions.ValidateIndex(value, ref Animations.wheelrotation_variable, key);
											break;
										case "flashingdoorlight":
											InternalFunctions.ValidateIndex(value, ref Animations.doorlight, key);
											break;
										case "cylinderpuff_l":
											InternalFunctions.ValidateIndex(value, ref Animations.cylinderpuff_L, key);
											break;
										case "cylinderpuff_r":
											InternalFunctions.ValidateIndex(value, ref Animations.cylinderpuff_R, key);
											break;
										case "headcodeindicator":
											try
											{
												string[] headcodesplit = value.Split(',');
												for (int k = 0; k < headcodesplit.Length; k++)
												{
													if (k == 0)
													{
														this.Animations.headcodeindex = Convert.ToInt32(headcodesplit[0]);
													}
													else
													{
														Animations.totalheadcodestates = Convert.ToInt32(headcodesplit[1]);
													}
												}
											}
											catch
											{
												InternalFunctions.LogError("headcodeindicator",0);
											}
											break;
										default:
											throw new InvalidDataException("The parameter " + key + " is not supported.");
									}
									break;

								case "ats-sx":
									switch (key)
									{
										case "durationofalarm":
											this.AtsSx.DurationOfAlarm = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										case "durationofinitialization":
											this.AtsSx.DurationOfInitialization = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										case "durationofspeedcheck":
											this.AtsSx.DurationOfSpeedCheck = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										default:
											throw new InvalidDataException("The parameter " + key + " is not supported.");
									}
									break;
								case "ats-ps":
									switch (key)
									{
										case "maximumspeed":
											this.AtsPs.TrainPermanentPattern.SetPersistentLimit((1.0 / 3.6) * double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));
											break;
										default:
											throw new InvalidDataException("The parameter " + key + " is not supported.");
									}
									break;
								case "ats-p":
									switch (key)
									{
										case "durationofinitialization":
											this.AtsP.DurationOfInitialization = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										case "durationofbrakerelease":
											this.AtsP.DurationOfBrakeRelease = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										case "designdeceleration":
											this.AtsP.DesignDeceleration = (1.0 / 3.6) * double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										case "brakepatterndelay":
											this.AtsP.BrakePatternDelay = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										case "brakepatternoffset":
										case "signaloffset":
											this.AtsP.BrakePatternOffset = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										case "brakepatterntolerance":
											this.AtsP.BrakePatternTolerance = (1.0 / 3.6) * double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										case "warningpatterndelay":
										case "reactiondelay":
											this.AtsP.WarningPatternDelay = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										case "warningpatternoffset":
											this.AtsP.WarningPatternOffset = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										case "warningpatterntolerance":
											this.AtsP.WarningPatternTolerance = (1.0 / 3.6) * double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										case "patternspeeddifference":
											this.AtsP.WarningPatternTolerance = (-1.0 / 3.6) * double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										case "releasespeed":
											this.AtsP.ReleaseSpeed = (1.0 / 3.6) * double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										case "maximumspeed":
											this.AtsP.TrainPermanentPattern.SetPersistentLimit((1.0 / 3.6) * double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));
											break;
										case "d-ats-p":
											this.AtsP.DAtsPSupported = value.Equals("true", StringComparison.OrdinalIgnoreCase);
											break;
										default:
											throw new InvalidDataException("The parameter " + key + " is not supported.");
									}
									break;
								case "atc":
									switch (key)
									{
										case "automaticswitch":
											this.Atc.AutomaticSwitch = value.Equals("true", StringComparison.OrdinalIgnoreCase);
											break;
										case "emergencyoperation":
											if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
											{
												this.Atc.EmergencyOperationSignal = null;
											}
											else
											{
												double limit = (1.0 / 3.6) * double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
												if (limit <= 0.0)
												{
													this.Atc.EmergencyOperationSignal = null;
												}
												else
												{
													this.Atc.EmergencyOperationSignal = Atc.Signal.CreateEmergencyOperationSignal(limit);
												}
											}
											break;
										case "kakuninprimedindicator":
											InternalFunctions.ValidateIndex(value, ref this.Atc.KakuninPrimedIndicator, key);
											break;
										case "kakunintimerindicator":
											InternalFunctions.ValidateIndex(value, ref this.Atc.KakuninTimerActiveIndicator, key);
											break;
										case "kakuninbrakeindicator":
											InternalFunctions.ValidateIndex(value, ref this.Atc.KakuninBrakeApplicationIndicator, key);
											break;
										case "kakunintimeout":
											InternalFunctions.ParseNumber(value, ref this.Atc.KakuninDelay, key);
											break;
										default:
											int aspect;
											if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out aspect))
											{
												if (aspect >= 10)
												{
													Atc.Signal signal = ParseAtcCode(aspect, value);
													if (signal != null)
													{
														this.Atc.Signals.Add(signal);
														break;
													}
													throw new InvalidDataException("The ATC code " + value + " is not supported.");

												}
											}
											throw new InvalidDataException("The parameter " + key + " is not supported.");
									}
									break;
								case "eb":
									switch (key)
									{
										case "timeuntilbell":
											this.Eb.TimeUntilBell = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										case "timeuntilbrake":
											this.Eb.TimeUntilBrake = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										case "speedthreshold":
											this.Eb.SpeedThreshold = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										default:
											throw new InvalidDataException("The parameter " + key + " is not supported.");
									}
									break;
								case "tasc":
									switch (key)
									{
										case "designdeceleration":
											this.Tasc.DesignDeceleration = (1.0 / 3.6) * double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
											break;
										default:
											throw new InvalidDataException("The parameter " + key + " is not supported.");
									}
									break;
									//Handles some global settings
							   case "settings":
									switch (key)
									{
										case "independantreset":
											InternalFunctions.ParseBool(value, ref TractionManager.independantreset, key);
											break;
									}
									break;

								case "keyassignments":
								case "legacykeyassignments":
									switch (key)
									{
										case "safetykey": //Deprecated, but must still parse
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.SafetyKey, key);
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.AWSKey, key);
											break;
										case "vigilancekey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.SafetyKey, key);
											break;
										case "awskey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.AWSKey, key);
											break;
										case "automatickey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.AutomaticGearsCutoff, key);
											break;
										case "injectorkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.LiveSteamInjector, key);
											break;
										case "blowerskey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.Blowers, key);
											break;
										case "cutoffdownkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.CutoffDecrease, key);
											break;
										case "cutoffupkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.CutoffIncrease, key);
											break;
										case "fuelkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.FillFuel, key);
											break;
										case "enginestartkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.EngineStartKey, key);
											break;
										case "enginestopkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.EngineStopKey, key);
											break;
										case "wiperspeedup":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.IncreaseWiperSpeed, key);
											break;
										case "wiperspeeddown":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.DecreaseWiperSpeed, key);
											break;
										case "isolatesafetykey":
											if (WesternDiesel == null)
											{
												InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.IsolateSafetySystems, key);
											}
											else
											{
												InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.WesternAWSIsolationKey, key);
											}
											break;
										case "gearupkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.GearUp, key);
											break;
										case "geardownkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.GearDown, key);
											break;
										case "drakey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.DRA, key);
											break;
										case "customindicatorkey1":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.CustomIndicatorKey1, key);
											break;
										case "customindicatorkey2":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.CustomIndicatorKey2, key);
											break;
										case "customindicatorkey3":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.CustomIndicatorKey3, key);
											break;
										case "customindicatorkey4":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.CustomIndicatorKey4, key);
											break;
										case "customindicatorkey5":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.CustomIndicatorKey5, key);
											break;
										case "customindicatorkey6":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.CustomIndicatorKey6, key);
											break;
										case "customindicatorkey7":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.CustomIndicatorKey7, key);
											break;
										case "customindicatorkey8":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.CustomIndicatorKey8, key);
											break;
										case "customindicatorkey9":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.CustomIndicatorKey9, key);
											break;
										case "customindicatorkey10":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.CustomIndicatorKey10, key);
											break;
										case "frontpantographkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.FrontPantograph, key);
											break;
										case "rearpantographkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.RearPantograph, key);
											break;
										case "advancedrivingkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.ShowAdvancedDrivingWindow, key);
											break;
										case "steamheatincreasekey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.IncreaseSteamHeat, key);
											break;
										case "steamheatdecreasekey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.DecreaseSteamHeat, key);
											break;
										case "cylindercockskey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.CylinderCocks, key);
											break;
										case "headcodekey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.HeadCode, key);
											break;
										case "cawskey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.CAWSKey, key);
											break;
										case "impvelsukey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.SCMTincreasespeed, key);
											break;
										case "impvelgiukey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.SCMTdecreasespeed, key);
											break;
										case "scmtkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.TestSCMTKey, key);
											break;
										case "lcmupkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.LCMupKey, key);
											break;
										case "lcmdownkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.LCMdownkey, key);
											break;
										case "abbancokey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.AbilitaBancoKey, key);
											break;
										case "consavvkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.ConsensoAvviamentoKey, key);
											break;
										case "avvkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.AvviamentoKey, key);
											break;
										case "spegnkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.SpegnimentoKey, key);
											break;
										case "vigilantekey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.vigilantekey, key);
											break;
										case "vigilanteresetkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.vigilanteresetkey, key);
											break;
											/* TODO: Add from SCMT these values:
											 * impvel++
											 * impvel--
											 * [INCREASE/ DECREASE SETPOINT SPEEDS]
											 * scmtkey
											 * [Key to turn SCMT system plate]
											 * lcmupkey
											 * lcmdownkey
											 * [Increase/ decrease the LCM]
											 * abbancokey
											 * [Turns the key counter, starter key??]
											 * consavvkey
											 * [Press to give permisson to start?]
											 * avvkey
											 * [Press to start the engines]
											 * spegnkey
											 * [Press to stop the engines]
											 * 
											 * Functions should already be implemented, requires adding these to traction manager
											 */
										case "pzbkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.PZBKey, key);
											break;
										case "pzbreleasekey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.PZBReleaseKey, key);
											break;
										case "pzbstopoverridekey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.PZBStopOverrideKey, key);
											break;
										//Keys added for BR Class 52 'Western' Diesel Locomotive
										case "westernbatteryswitch":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.WesternBatterySwitch, key);
											break;
										case "westernfuelpumpswitch":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.WesternFuelPumpSwitch, key);
											break;
										case "westernmasterkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.WesternMasterKey, key);
											break;
										case "westerntransmissionresetkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.WesternTransmissionResetButton, key);
											break;
										case "westernengineswitchkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.WesternEngineSwitchKey, key);
											break;
										case "westernfirebellkey":
											InternalFunctions.ParseKey(value, ref CurrentKeyConfiguration.WesternFireBellKey, key);
											break;
										default:
											throw new InvalidDataException("The parameter " + key + " is not supported.");
									}
									break;

							}
						}
					}
				}
			}
			//Check for null references and add all the devices
			var devices = new List<Device>();
			if (this.TractionManager != null)
			{
				devices.Add(this.TractionManager);
			}

			if (this.StartupSelfTestManager != null)
			{
				devices.Add(this.StartupSelfTestManager);
			}
			
			if (this.SteamEngine != null) {
				devices.Add(this.SteamEngine);
			}

			if (this.DieselEngine != null)
			{
				devices.Add(this.DieselEngine);
			}

			if (this.ElectricEngine != null)
			{
				devices.Add(this.ElectricEngine);
			}

			if (this.Vigilance != null)
			{
				devices.Add(this.Vigilance);
			}

			if (this.AWS != null)
			{
				devices.Add(this.AWS);
			}

			if (this.TPWS != null)
			{
				devices.Add(this.TPWS);
			}

			if (this.SCMT != null)
			{
				devices.Add(this.SCMT);
			}

			if (this.SCMT_Traction != null)
			{
				devices.Add(this.SCMT_Traction);
			}

			if (this.CAWS != null)
			{
				devices.Add(this.CAWS);
			}

			if (this.PZB != null)
			{
				devices.Add(this.PZB);
			}

			if (this.Windscreen != null)
			{
				devices.Add(this.Windscreen);
			}

			if (this.Animations != null)
			{
				devices.Add(this.Animations);
			}
			if (this.Ato != null)
			{
				devices.Add(this.Ato);
			}
			if (this.Tasc != null)
			{
				devices.Add(this.Tasc);
			}
			if (this.Eb != null)
			{
				devices.Add(this.Eb);
			}
			if (this.Atc != null)
			{
				devices.Add(this.Atc);
			}
			if (this.AtsP != null)
			{
				devices.Add(this.AtsP);
			}
			if (this.AtsPs != null)
			{
				devices.Add(this.AtsPs);
				if (this.AtsSx == null)
				{
					this.AtsSx = new AtsSx(this);
				}
			}
			if (this.AtsSx != null)
			{
				devices.Add(this.AtsSx);
			}
			if (this.WesternDiesel != null)
			{
				devices.Add(this.WesternDiesel);
			}
#if !DebugNBS
			if (this.LedLights != null)
			{
				devices.Add(this.LedLights);
			}
#endif
			this.Devices = devices.ToArray();
		}

		/// <summary>Parses an ATC code and returns the corresponding signal.</summary>
		/// <param name="aspect">The aspect.</param>
		/// <param name="code">The code.</param>
		/// <returns>The signal corresponding to the code, or a null reference if the code is invalid.</returns>
		private Atc.Signal ParseAtcCode(int aspect, string code)
		{
			Atc.SignalIndicators signalIndicator;
			double num;
			double num1;
			double num2;
			double num3;
			double num4;
			double num5;
			double num6;
			int num7;
			//Codes representing a red signal aspect with an ATC speed limit of zero
			if (code == "S01" || code == "01")
			{
				return new Atc.Signal(aspect, Atc.SignalIndicators.Red, 0);
			}
			//Codes representing a red signal aspect for which ATC applies EB
			if (code == "S02E" || code == "02E" || code == "03")
			{
				return new Atc.Signal(aspect, Atc.SignalIndicators.Red, -1);
			}
			//Code representing any signal aspect for which ATC is to apply EB
			if (code == "02")
			{
				return Atc.Signal.CreateNoSignal(aspect);
			}
			//Code representing a beacon which changes the ATC signal indicator to red, and tells the train it should switch to ATS
			if (code == "ATS")
			{
				return new Atc.Signal(aspect, Atc.SignalIndicators.Red, double.MaxValue, 0, double.MaxValue, Atc.KirikaeStates.ToAts, false, false);
			}
			bool flag = false;
			bool flag1 = false;
			bool flag2 = false;
			if (code.StartsWith("ATS"))
			{
				signalIndicator = Atc.SignalIndicators.Red;
				flag = true;
				code = code.Substring(3);
			}
			else if (code.StartsWith("K"))
			{
				signalIndicator = Atc.SignalIndicators.Red;
				flag = true;
				code = code.Substring(1);
			}
			else if (code.StartsWith("P"))
			{
				signalIndicator = Atc.SignalIndicators.P;
				code = code.Substring(1);
			}
			else if (code.StartsWith("R"))
			{
				signalIndicator = Atc.SignalIndicators.Red;
				code = code.Substring(1);
			}
			else if (code.StartsWith("SY"))
			{
				signalIndicator = Atc.SignalIndicators.Red;
				flag1 = true;
				code = code.Substring(2);
			}
			else if (code.StartsWith("Y"))
			{
				signalIndicator = Atc.SignalIndicators.Green;
				flag1 = true;
				code = code.Substring(1);
			}
			else if (code.StartsWith("S"))
			{
				signalIndicator = Atc.SignalIndicators.Red;
				code = code.Substring(1);
			}
			else if (!code.StartsWith("G"))
			{
				signalIndicator = Atc.SignalIndicators.Green;
			}
			else
			{
				signalIndicator = Atc.SignalIndicators.Green;
				code = code.Substring(1);
			}
			if (code.EndsWith("ORP"))
			{
				code = code.Substring(0, code.Length - 3);
				flag2 = true;
			}
			Atc.Signal signal = null;
			if (code.Contains("/"))
			{
				int num8 = code.IndexOf('/');
				string str = code.Substring(0, num8);
				string str1 = code.Substring(num8 + 1);
				if (str1.Contains("@"))
				{
					/* Code made up as follows:
					 * 
					 * INITIAL SPEED / FINAL SPEED @ DISTANCE TO FINAL SPEED
					 * 
					 */
					num8 = str1.IndexOf('@');
					string str2 = str1.Substring(num8 + 1);
					str1 = str1.Substring(0, num8);
					if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out num) && double.TryParse(str1, NumberStyles.Float, CultureInfo.InvariantCulture, out num1) && double.TryParse(str2, NumberStyles.Float, CultureInfo.InvariantCulture, out num2))
					{
						if (num < 0)
						{
							//If initial speed is less than zero, this is invalid
							return null;
						}
						if (num1 < 0)
						{
							//If final speed is less than zero, this is invalid
							return null;
						}
						if (num2 < 0)
						{
							//If distance is less than zero, this is invalid
							return null;
						}
						if (num < num1)
						{
							//If final speed is greater than inital speed, this is invalid
							return null;
						}
						signal = new Atc.Signal(aspect, signalIndicator, (double)num / 3.6, (double)num1 / 3.6, num2);
					}
				}
				else if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out num3) && double.TryParse(str1, NumberStyles.Float, CultureInfo.InvariantCulture, out num4))
				{
					/* Code made up as follows:
					 * 
					 * INITIAL SPEED / FINAL SPEED
					 * 
					 */
					if (num3 < 0)
					{
						//If intial speed is less than zero, this is invalid
						return null;
					}
					if (num4 < 0)
					{
						//If final speed is less than zero, this is invalid
						return null;
					}
					if (num3 < num4)
					{
						//If final speed is greater than inital speed, this is invalid
						return null;
					}
					signal = new Atc.Signal(aspect, signalIndicator, (double)num3 / 3.6, (double)num4 / 3.6, double.MaxValue);
				}
			}
			else if (code.Contains("@"))
			{
				/* Code made up as follows:
				 * 
				 * FINAL SPEED @ DISTANCE TO FINAL SPEED
				 * 
				 */
				int num9 = code.IndexOf('@');
				string str3 = code.Substring(0, num9);
				string str4 = code.Substring(num9 + 1);
				if (double.TryParse(str3, NumberStyles.Float, CultureInfo.InvariantCulture, out num5) && double.TryParse(str4, NumberStyles.Float, CultureInfo.InvariantCulture, out num6))
				{
					if (num5 < 0)
					{
						//If final speed is less than zero, this is invalid
						return null;
					}
					if (num6 < 0)
					{
						//If distance to final speed is less than zero, this is invalid
						return null;
					}
					signal = new Atc.Signal(aspect, signalIndicator, double.MaxValue, (double)num5 / 3.6, num6);
				}
			}
			else if (int.TryParse(code, NumberStyles.Float, CultureInfo.InvariantCulture, out num7))
			{
				/* Code made up as follows:
				 * 
				 * FINAL SPEED
				 * 
				 */
				if ((double)num7 < 0)
				{
					//If final speed is less than zero, this is invalid
					return null;
				}
				signal = new Atc.Signal(aspect, signalIndicator, (double)num7 / 3.6);
			}
			if (signal != null)
			{
				//Set the state of the ATC/ATS switch indicator
				signal.Kirikae = (flag ? Atc.KirikaeStates.ToAts : Atc.KirikaeStates.ToAtc);
				//??Set whether this signal is a distant??
				signal.ZenpouYokoku = flag1;
				//Set whether this signal has overrun protection
				signal.OverrunProtector = flag2;
				//Finally return the signal
			}
			return signal;
		}

		/// <summary>Sets up the devices from the specified train.dat file.</summary>
		/// <param name="file">The train.dat file.</param>
		internal void LoadTrainDatFile(string file)
		{
			string[] lines = File.ReadAllLines(file, Encoding.UTF8);
			for (int i = 0; i < lines.Length; i++)
			{
				int semicolon = lines[i].IndexOf(';');
				if (semicolon >= 0)
				{
					lines[i] = lines[i].Substring(0, semicolon).Trim();
				}
				else
				{
					lines[i] = lines[i].Trim();
				}
			}
			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].Equals("#DEVICE", StringComparison.OrdinalIgnoreCase))
				{
					if (i < lines.Length - 1)
					{
						int value = int.Parse(lines[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture);
						if (value == 0)
						{
							this.AtsSx = new AtsSx(this);
						}
						else if (value == 1)
						{
							this.AtsSx = new AtsSx(this);
							this.AtsP = new AtsP(this);
						}
					}
					if (i < lines.Length - 2)
					{
						int value = int.Parse(lines[i + 2], NumberStyles.Integer, CultureInfo.InvariantCulture);
						if (value == 1)
						{
							this.Atc = new Atc(this);
						}
						else if (value == 2)
						{
							this.Atc = new Atc(this);
							this.Atc.AutomaticSwitch = true;
						}
					}
					if (i < lines.Length - 3)
					{
						int value = int.Parse(lines[i + 3], NumberStyles.Integer, CultureInfo.InvariantCulture);
						if (value == 1)
						{
							this.Eb = new Eb(this);
						}
					}
					break;
				}
			}
			// --- devices ---
			List<Device> devices = new List<Device>();
			if (this.Eb != null)
			{
				devices.Add(this.Eb);
			}
			if (this.Atc != null)
			{
				devices.Add(this.Atc);
			}
			if (this.AtsP != null)
			{
				devices.Add(this.AtsP);
			}
			if (this.AtsSx != null)
			{
				devices.Add(this.AtsSx);
			}
			this.Devices = devices.ToArray();
		}

		/// <summary>Is called when the system should initialize.</summary>
		/// <param name="mode">The initialization mode.</param>
		internal void Initialize(InitializationModes mode) {
			this.PluginInitializing = true;
			for (int i = this.Devices.Length - 1; i >= 0; i--) {
				this.Devices[i].Initialize(mode);
			}
			// --- panel ---
			for (int i = 0; i < this.Panel.Length; i++)
			{
				this.Panel[i] = 0;
			}
		}

		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data.</param>
		internal void Elapse(ElapseData data)
		{
			this.PluginInitializing = false;
			if (data.ElapsedTime.Seconds > 0.0 & data.ElapsedTime.Seconds < 1.0) {
				//Odakyufan's code requires clearing the array each time
				//This doesn't hurt anything, but may cause a small drop in performance??
				Array.Clear(this.Panel, 0, this.Panel.Length);
				// --- devices ---
				this.State = data.Vehicle;
				this.Handles = new ReadOnlyHandles(data.Handles);
				bool blocking = false;
				foreach (Device device in this.Devices) {
					device.Elapse(data, ref blocking);
				}
				
				if (data.Handles.BrakeNotch != 0) {
					data.Handles.PowerNotch = 0;
				}
				// --- panel ---

				if (data.Handles.Reverser != 0 & (this.Handles.PowerNotch > 0 & this.Handles.BrakeNotch == 0 | this.Handles.PowerNotch == 0 & this.Handles.BrakeNotch == 1 & this.Specs.HasHoldBrake)) {
					this.Panel[100] = 1;
				}
				if (data.Handles.BrakeNotch >= this.Specs.AtsNotch & data.Handles.BrakeNotch <= this.Specs.BrakeNotches | data.Handles.Reverser != 0 & data.Handles.BrakeNotch == 1 & this.Specs.HasHoldBrake) {
					this.Panel[101] = 1;
				}
				for (var i = (int)VirtualKeys.S; i <= (int)VirtualKeys.C2; i++) {
					if (KeysPressed[i]) {
						this.Panel[93 + i] = 1;
					}
				}
				if (PanelIllumination) {
					this.Panel[161] = 1;
				}
				// --- accelerometer ---
				this.AccelerometerTimer += data.ElapsedTime.Seconds;
				if (this.AccelerometerTimer > AccelerometerMaximumTimer) {
					this.Acceleration = (data.Vehicle.Speed.MetersPerSecond - AccelerometerSpeed) / this.AccelerometerTimer;
					this.AccelerometerSpeed = data.Vehicle.Speed.MetersPerSecond;
					this.AccelerometerTimer = 0.0;
				}
				if (this.Acceleration < 0.0) {
					double value = -3.6 * this.Acceleration;
					if (value >= 10.0) {
						this.Panel[74] = 9;
						this.Panel[75] = 9;
					} else {
						this.Panel[74] = (int)Math.Floor(value) % 10;
						this.Panel[75] = (int)Math.Floor(10.0 * value) % 10;
					}
				}
				
				//Set the global speed, direction and starting location variables
				CurrentSpeed = (int)data.Vehicle.Speed.KilometersPerHour;
				this.PreviousLocation = this.TrainLocation;
				this.TrainLocation = data.Vehicle.Location;
				
				
				//Calculate in game time in easily accessible format
				SecondsSinceMidnight = data.TotalTime.Seconds;
			}
		}
		
		/// <summary>Is called when the driver changes the reverser.</summary>
		/// <param name="reverser">The new reverser position.</param>
		internal void SetReverser(int reverser) {
			foreach (Device device in this.Devices) {
				device.SetReverser(reverser);
			}
		}
		
		/// <summary>Is called when the driver changes the power notch.</summary>
		/// <param name="powerNotch">The new power notch.</param>
		internal void SetPower(int powerNotch) {
			foreach (Device device in this.Devices) {
				device.SetPower(powerNotch);
			}
		}
		
		/// <summary>Is called when the driver changes the brake notch.</summary>
		/// <param name="brakeNotch">The new brake notch.</param>
		internal void SetBrake(int brakeNotch) {
			foreach (Device device in this.Devices) {
				device.SetBrake(brakeNotch);
			}
		}
		
		/// <summary>Is called when a key is pressed.</summary>
		/// <param name="key">The key.</param>
		internal void KeyDown(VirtualKeys key) {
			var index = (int)key;
			if (index >= 0 & index < KeysPressed.Length) {
				KeysPressed[index] = true;
			}
			foreach (Device device in this.Devices) {
				device.KeyDown(key);
			}
			if (key == VirtualKeys.L) {
				this.PanelIllumination = !this.PanelIllumination;
			}
		}
		
		/// <summary>Is called when a key is released.</summary>
		/// <param name="key">The key.</param>
		internal void KeyUp(VirtualKeys key) {
			var index = (int)key;
			if (index >= 0 & index < KeysPressed.Length) {
				KeysPressed[index] = false;
			}
			foreach (Device device in this.Devices) {
				device.KeyUp(key);
			}
		}
		
		/// <summary>Is called when a horn is played or when the music horn is stopped.</summary>
		/// <param name="type">The type of horn.</param>
		internal void HornBlow(HornTypes type) {
			foreach (Device device in this.Devices) {
				device.HornBlow(type);
			}
		}
		
		/// <summary>Is called when the state of the doors changes.</summary>
		/// <param name="oldState">The old state of the doors.</param>
		/// <param name="newState">The new state of the doors.</param>
		public void DoorChange(DoorStates oldState, DoorStates newState) {
			this.Doors = newState;
			foreach (Device device in this.Devices) {
				device.DoorChange(oldState, newState);
			}

		}

		/// <summary>Is called to inform about signals.</summary>
		/// <param name="signal">The signal data.</param>
		internal void SetSignal(SignalData[] signal)
		{
			NextSignal = signal[1];

			foreach (Device device in this.Devices) {
				device.SetSignal(signal);
			}

		}

		/// <summary>Is called when the train passes a beacon.</summary>
		/// <param name="beacon">The beacon data.</param>
		public void SetBeacon(BeaconData beacon)
		{
			//Standard Beacons- Process All
			switch (beacon.Type)
			{
				case 20:
					if (this.ElectricEngine != null)
					{
						/* APC magnets and neutral section */
						if (beacon.Optional > 0)
						{
							/* Handle legacy APC magnet behaviour with only one beacon */
							int secondMagnetDistance;
							int.TryParse(beacon.Optional.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out secondMagnetDistance);
							ElectricEngine.LegacyPowerCutoff((int)TrainLocation, secondMagnetDistance);
						}
						else if (beacon.Optional == -1)
						{
							//Signals the start and end of the neutral section
							ElectricEngine.newpowercutoff((int)TrainLocation);
						}
						else
						{
							//Opens/ Closes ACB/VCB
							//Line volts indicator should be illuminated, and should be resetable
							ElectricEngine.TripBreaker();
						}
					}
					break;

				case 21:
					if (this.Windscreen != null)
					{
						/* Rain */

						Windscreen.rainstart(beacon.Optional);

					}
					break;
				case 22:
					{
						//Handle fuelling section
						if (beacon.Optional == 1)
						{
							canfuel = true;
						}
						else
						{
							canfuel = false;
							if (this.SteamEngine != null)
							{
								this.SteamEngine.fuelling = false;
							}
							if (this.DieselEngine != null)
							{
								this.DieselEngine.fuelling = false;
							}
						}
					}
					break;

				//Additional BVEC_ATS beacons, process all
				case 30:
					{
						//Door light trigger beacon
						//Set the timer for this station to the optional flag on the beacon
						//Then hit the trigger function
						Animations.stoptime = beacon.Optional;
						Animations.doorlighttrigger();
					}
					break;
				case 31:
					{
						//Door light timer
						Animations.departuretime = TimeSpan.Parse(Convert.ToString(beacon.Optional)).TotalSeconds;
					}
					break;
			}
			//AWS Beacons
			if (this.AWS != null)
			{
				switch (beacon.Type)
				{
					//Anthony Bowden's UKTrainSYS beacon codes
					case 44000:
						/* Automatic Warning System magnet for signals */
						if (beacon.Optional == 180)
						{
							/* This is the south pole of an AWS permanent magnet, so prime the AWS - new prototypical behaviour */
							AWS.Prime();
							DebugLogger.LogMessage("Beacon recived: UKTrainsys AWS Prime");
						}
						else if (beacon.Optional == 270)
						{
							/* This is an AWS suppression electromagnet - new prototypical behaviour */
							AWS.Suppress(TrainLocation);
							DebugLogger.LogMessage("Beacon received: UKTrainsys AWS Suppression");
						}
						else if (beacon.Optional == 360)
						{
							/* The following if statement must remain nested in the containing if statement */
							if (beacon.Signal.Aspect > 3)
							{
								/* This is the north pole of the AWS electromagnet - it is energised, so issue a clear indication */
								AWS.IssueClear();
								DebugLogger.LogMessage("Beacon received: UKTrainsys AWS Clear");
							}
						}
						else if (beacon.Signal.Aspect <= 3)
						{
							/* Aspect is restrictive, so issue a warning - this is the legacy fallback behaviour */
							AWS.Prime();
							DebugLogger.LogMessage("Beacon received: Legacy AWS Signal Aspect");
						}
						else if (beacon.Signal.Aspect > 3)
						{
							/* Aspect is clear, so issue a clear inducation - this is the legacy fallback behaviour */
							AWS.IssueLegacyClear();
							DebugLogger.LogMessage("Beacon received: Legacy AWS Clear");
						}
						break;
					case 44001:
						/* Permanently installed Automatic Warning System magnet which triggers a warning whenever passed over.
						 * 
						 * Issue a warning regardless - this is the legacy fallback behaviour ONLY */
						AWS.Prime();
						DebugLogger.LogMessage("Beacon received: Legacy AWS Permenant");
						break;
				}
				if (this.TPWS != null)
				{
					switch (beacon.Type)
					{
						case 44002:
							/* Train Protection and Warning System Overspeed Sensor induction loop - associated with signal */

							if (beacon.Signal.Aspect == 0)
							{
								TPWS.ArmOss(beacon.Optional);
								DebugLogger.LogMessage("Beacon received: TPWS Signal Overspeed Loop");
							}
							break;
						case 44003:
							/* Train Protection and Warning System Train Stop Sensor induction loop */
							if (beacon.Signal.Aspect == 0)
							{
								TPWS.ArmTss(beacon.Optional, TrainLocation);
								DebugLogger.LogMessage("Beacon received: TPWS Trainstop Loop");
							}
							else
							{
								if (TPWS.SafetyState == TPWS.SafetyStates.None)
								{
									TPWS.Reset();
								}
							}
							break;
						case 44004:
							/* Train Protection and Warning System Overspeed Sensor induction loop - permanent speed restriction */
							TPWS.ArmOss(beacon.Optional);
							DebugLogger.LogMessage("Beacon received: TPWS Permenant Overspeed Loop");
							break;
					}
					
				}
				
			}
			//SCMT Safety System Beacons
			if (this.SCMT.Enabled == true)
			{
				if (SCMT.testscmt == 0)
				{
					return;
				}
				if (beacon.Type == 44002 || beacon.Type == 44003 || beacon.Type == 44004)
				{
					//Trigger a SCMT alert
					SCMT.trigger();
					//Set the beacon type and signal data for the recieved beacon
					SCMT.beacon_type = beacon.Type;
					SCMT.beacon_signal = beacon.Signal;
					//Now parse the beacon type and react appropriately
					switch (beacon.Type)
					{
						case 44002:
							//Trigger if the signal for the last beacon 44005 recieved was at a danger aspect
							if (SCMT.beacon_44005 == 4)
							{
								//Set the SCMT last beacon type recieved to 44004
								SCMT.beacon_type = 44004;
								//Reset the blue light timer for SCMT
								SCMT.SpiabluTimer.TimerActive = true;
								SCMT.SpiabluTimer.TimeElapsed = 0;
							}
							break;
						case 44003:
							SCMT.beacon_speed = SCMT.speed;
							SCMT.beacon_type = 44004;
							SCMT.SpiabluTimer.TimerActive = false;
							SCMT.spiablue_act = false;
							break;
						case 44004:
							SCMT.speed = beacon.Optional;
							SCMT.beacon_speed = beacon.Optional;
							break;
					}
					//Set the maximum permissable and alert speeds
					SCMT.maxspeed = SCMT.beacon_speed;
					SCMT.alertspeed = SCMT.alertspeed + 2;
				}
				if (beacon.Type == 44005)
				{
					if (SCMT.beacon_44005 == 3 && SCMT.curveflag == false)
					{
						SCMT.curveflag = true;
					}
					else
					{
						SCMT.curveflag = false;
					}
					//Set the aspect data and distance to this signal
					SCMT.beacon_44005 = beacon.Signal.Aspect;
					SCMT.beacon_distance = beacon.Signal.Distance;
				}
			}
			if (this.PZB != null)
			{

				switch (beacon.Type)
				{
					case 500:
						DebugLogger.LogMessage("Beacon received: PZB 500hz");
						PZB.BeaconAspect = beacon.Signal.Aspect;
						PZB.Trigger(beacon.Type, beacon.Optional);                        
						break;
					case 1000:
						DebugLogger.LogMessage("Beacon received: PZB 1000hz");
						PZB.BeaconAspect = beacon.Signal.Aspect;
						PZB.Trigger(beacon.Type, beacon.Optional);
						break;
					case 2000:
						//Home signal standard inductors
						DebugLogger.LogMessage("Beacon received: PZB 2000hz");
						PZB.BeaconAspect = beacon.Signal.Aspect;
						PZB.Trigger(beacon.Type, beacon.Optional);
						break;
					case 2001:
						//Home signal speed restrictive inductors
						DebugLogger.LogMessage("Beacon received: PZB 2000hz");
						PZB.BeaconAspect = beacon.Signal.Aspect;
						PZB.Trigger(beacon.Type, beacon.Optional);
						break;
				}
			}
			if (this.F92 != null)
			{
				F92.Trigger(beacon.Type, beacon.Optional, beacon.Signal.Aspect);
			}
			//Japanese Safety System Beacons
			if (this.Atc != null | this.Ato != null | this.AtsP != null | this.AtsPs != null | this.AtsSx != null |
				this.Calling != null | this.Tasc != null)
			{
				if (beacon.Type != 44)
				{
					Device[] devices = this.Devices;
					for (int i = 0; i < (int) devices.Length; i++)
					{
						devices[i].SetBeacon(beacon);
					}
					//this.Calling.SetBeacon(beacon);
				}
				/*
				 * This should be irrelevant
				 * I hope
				 * 
				 * 
				else
				{
					int optional = beacon.Optional%100;
					if (optional == 0)
					{
						this.ContinuousAnalogTransmissionAvailable = false;
						this.ContinuousAnalogTransmissionLastFrequency = 0;
						return;
					}
					if (optional == 1)
					{
						int num = beacon.Optional/100%1000;
						if (num >= 73)
						{
							int optional1 = beacon.Optional/100000%1000;
							int num1 = beacon.Optional/100000000;
							if (num1 >= 10)
							{
								num1 = 0;
							}
							if (!this.ContinuousAnalogTransmissionAvailable)
							{
								int num2 = num;
								int num3 = 1000*num1 + optional1;
								this.SetBeacon(new BeaconData(num2, num3, beacon.Signal));
							}
							this.ContinuousAnalogTransmissionAvailable = true;
							this.ContinuousAnalogTransmissionSignalPosition = this.State.Location +
																			  beacon.Signal.Distance;
							this.ContinuousAnalogTransmissionSignalAspect = num1;
							this.ContinuousAnalogTransmissionActiveFrequency = num;
							this.ContinuousAnalogTransmissionIdleFrequency = optional1;
							return;
						}
					}
				}
				*/
			}
		}

		/// <summary>Is called when the plugin should perform the AI.</summary>
		/// <param name="data">The AI data.</param>
		public void PerformAI(AIData data)
		{
			if (Driver == null)
			{
				return;
			}
			Driver.TrainDriver(data);
		}
		
		

		// --- static functions ---
		
		/// <summary>Gets the frequency a beacon is transmitting at, or 0 if not recognized.</summary>
		/// <param name="beacon">The beacon.</param>
		/// <returns>The frequency the beacon is transmitting at, or 0 if not recognized.</returns>
		internal static int GetFrequencyFromBeacon(BeaconData beacon) {
			/*
			 * Frequency-based beacons encode the frequency as the
			 * beacon type in KHz and have the following optional data:
			 * 
			 * siii
			 * |\_/
			 * | \- idle frequency (KHz)
			 * \--- signal aspect
			 * 
			 * or
			 * 
			 * -1
			 * always active
			 * 
			 * If the aspect of the signal the beacon is attached to
			 * matches the aspect encoded in the optional data, the
			 * beacon transmits at its active frequency. Otherwise,
			 * the beacon transmits at its idle frequency.
			 * 
			 * If the optional data is -1, the beacon always transmits
			 * at its active frequency.
			 * */
			if (beacon.Type >= 73) {
				if (beacon.Optional == -1) {
					return beacon.Type;
				} else {
					int beaconAspect = beacon.Optional / 1000;
					if (beaconAspect >= 10) beaconAspect = 0;
					int signalAspect = beacon.Signal.Aspect;
					if (signalAspect >= 10) signalAspect = 0;
					if (beaconAspect == signalAspect) {
						return beacon.Type;
					} else {
						int idle = beacon.Optional % 1000;
						return idle;
					}
				}
			} else {
				return 0;
			}
		}
		
	}
}
