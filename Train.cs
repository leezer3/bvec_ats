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
		
		/// <summary>The current state of the doors.</summary>
		internal DoorStates Doors;

        /// <summary>Stores whether any safety systems have been tripped.</summary>
        internal bool overspeedtripped;
        internal bool drastate;
        //internal bool deadmanstripped;
        /// <summary>Stores whether the AWS is isolated.</summary>
        internal static bool AWSIsolated = false;
        /// <summary>Stores whether the startup self-test has been performed.</summary>
        internal static bool selftest = false;

        /// <summary>Stores whether we can fuel the train.</summary>
        internal bool canfuel = false;

		// --- acceleration ---

        /// <summary>The speed.</summary>
        internal int trainspeed;

        /// <summary>The current location of the train.</summary>
        internal double trainlocation;
        internal double previouslocation;
        internal int direction;

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
        internal tractionmanager tractionmanager;

		/// <summary>Steam Traction or Null Reference if not installed</summary>
		internal steam steam;

        /// <summary>Electric Traction or Null Reference if not installed</summary>
        internal electric electric;

        /// <summary>Diesel Traction or Null Reference if not installed</summary>
        internal diesel diesel;

        /// <summary>Vigilance Devices or Null Reference if not installed</summary>
        /// Overspeed, deadman's handle & DRA
        internal vigilance vigilance;

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

	    internal LEDLights LedLights;

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
		/// <param name="file">The configuration file.</param>
		internal void LoadConfigurationFile(string file) {
            

            //Initialise all safety devices first
            //Set Enabled state if they are installed
            
            //Only initialise traction types if required
            this.tractionmanager = new tractionmanager(this);
            
            
            
            
            //this.Calling = new Calling(this, playSound);
            this.Driver = new AI_Driver(this);
            this.DebugLogger = new DebugLogger();
            this.SCMT = new SCMT(this);
            this.SCMT_Traction = new SCMT_Traction(this);
            this.Windscreen = new Windscreen(this);
            this.Animations = new Animations(this);
			string[] lines = File.ReadAllLines(file, Encoding.UTF8);
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
			                    this.steam = new steam(this);
                                DebugLogger.LogMessage("Steam traction enabled");
			                    break;
			                case "diesel":
			                    this.diesel = new diesel(this);
                                DebugLogger.LogMessage("Diesel traction enabled");
			                    break;
			                case "electric":
			                    this.electric = new electric(this);
                                DebugLogger.LogMessage("Electric traction enabled");
			                    break;
			                case "vigilance":
			                    this.vigilance = new vigilance(this);
                                DebugLogger.LogMessage("Vigilance system(s) enabled");
			                    break;
			                case "aws":
                                LoadAWSTPWS();
			                    this.AWS.enabled = true;
                                DebugLogger.LogMessage("AWS enabled");
			                    break;
			                case "tpws":
                                LoadAWSTPWS();
			                    this.TPWS.enabled = true;
                                DebugLogger.LogMessage("TPWS enabled");
			                    break;
                            case "scmt":
                                this.SCMT.enabled = true;
			                    this.SCMT_Traction.enabled = true;
                                DebugLogger.LogMessage("SCMT enabled");
                                break;
                            case "caws":
                                this.CAWS = new CAWS(this);
                                this.CAWS.enabled = true;
                                DebugLogger.LogMessage("CAWS enabled");
                                break;
                            case "pzb":
			                    this.PZB = new PZB(this);
			                    this.PZB.enabled = true;
                                DebugLogger.LogMessage("PZB enabled");
			                    break;
			                case "interlocks":
			                    //Twiddle
			                    break;
			                case "keyassignments":
			                    //Twiddle
			                    break;
			                case "animations":
			                    //Twiddle
			                    break;
                            case "settings":
                                //Twiddle
                                break;
                            case "ledlights":
                                this.LedLights = new LEDLights(this);
			                    break;
                            case "debug":
                                //Twiddle
                                //Although we've already parsed this, it needs to be marked as a supported section
                                // TODO: Can this check be removed? Seems to be a hangup from the original plugin template??
                                // Can't see a reason off the top of my head to throw an exception either.....
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
			                    throw new InvalidDataException("The section " + line[0] + " is not supported.");
			            }
			        } else {
			            int equals = line.IndexOf('=');
			            if (@equals >= 0) {
			                string key = line.Substring(0, @equals).Trim().ToLowerInvariant();
			                string value = line.Substring(@equals + 1).Trim();
			                switch (section) {
			                    case "steam":
			                        switch (key) {
			                            case "automatic":
			                                InternalFunctions.ParseBool(value, ref steam.automatic, key);
			                                break;
			                            case "heatingpart":
			                                InternalFunctions.ValidateSetting(value, ref steam.heatingpart, key);
			                                break;
			                            case "heatingrate":
			                                this.steam.heatingrate = value;
			                                break;
			                            case "overheatwarn":
			                                InternalFunctions.ParseNumber(value, ref steam.overheatwarn, key);
			                                break;
			                            case "overheat":
			                                InternalFunctions.ParseNumber(value, ref steam.overheat, key);
			                                break;
			                            case "overheatresult":
			                                InternalFunctions.ValidateSetting(value, ref steam.overheatresult, key);
			                                break;
			                            case "thermometer":
			                                InternalFunctions.ValidateIndex(value, ref steam.thermometer, key);
			                                break;
			                            case "overheatindicator":
			                                InternalFunctions.ValidateIndex(value, ref steam.overheatindicator, key);
			                                break;
			                            case "overheatalarm":
			                                InternalFunctions.ValidateIndex(value, ref steam.OverheatAlarm.LoopSound, key);
			                                break;
			                            case "cutoffmax":
			                                InternalFunctions.ParseNumber(value, ref steam.cutoffmax, key);
			                                break;
			                            case "cutoffineffective":
			                                InternalFunctions.ParseNumber(value, ref steam.cutoffineffective, key);
			                                break;
			                            case "cutoffratio":
			                                InternalFunctions.ParseNumber(value, ref steam.cutoffratio, key);
			                                break;
			                            case "cutoffratiobase":
			                                InternalFunctions.ParseNumber(value, ref steam.cutoffratiobase, key);
			                                break;
			                            case "cutoffmin":
			                                InternalFunctions.ParseNumber(value, ref steam.cutoffmin, key);
			                                break;
			                            case "cutoffdeviation":
			                                InternalFunctions.ParseNumber(value, ref steam.cutoffdeviation, key);
			                                break;
			                            case "cutoffindicator":
			                                InternalFunctions.ValidateIndex(value, ref steam.cutoffindicator, key);
			                                break;
			                            case "boilermaxpressure":
			                                InternalFunctions.ParseNumber(value, ref steam.boilermaxpressure, key);
			                                break;
			                            case "boilerminpressure":
			                                InternalFunctions.ParseNumber(value, ref steam.boilerminpressure, key);
			                                break;
			                            case "boilerstartwaterlevel":
			                                InternalFunctions.ParseNumber(value, ref steam.boilerstartwaterlevel, key);
			                                break;
			                            case "boilermaxwaterlevel":
			                                InternalFunctions.ParseNumber(value, ref steam.boilermaxwaterlevel, key);
			                                break;
			                            case "boilerpressureindicator":
			                                InternalFunctions.ValidateIndex(value, ref steam.boilerpressureindicator, key);
			                                break;
			                            case "boilerwaterlevelindicator":
			                                InternalFunctions.ValidateIndex(value, ref steam.boilerwaterlevelindicator, key);
			                                break;
			                            case "boilerwatertosteamrate":
			                                InternalFunctions.ParseNumber(value, ref steam.boilerwatertosteamrate, key);
			                                break;
			                            case "fuelindicator":
			                                InternalFunctions.ValidateIndex(value, ref steam.fuelindicator, key);
			                                break;
			                            case "fuelstartamount":
			                                InternalFunctions.ParseNumber(value, ref steam.fuelstartamount, key);
			                                break;
			                            case "fuelcapacity":
			                                InternalFunctions.ParseNumber(value, ref steam.fuelcapacity, key);
			                                break;
			                            case "fuelfillspeed":
			                                InternalFunctions.ParseNumber(value, ref steam.fuelfillspeed, key);
			                                break;
			                            case "fuelfillindicator":
			                                InternalFunctions.ValidateIndex(value, ref steam.fuelfillindicator, key);
			                                break;
			                            case "injectorrate":
			                                InternalFunctions.ParseNumber(value, ref steam.injectorrate, key);
			                                break;
			                            case "injectorindicator":
			                                InternalFunctions.ValidateIndex(value, ref steam.Injector.PanelIndex, key);
			                                break;
			                            case "automaticindicator":
			                                InternalFunctions.ValidateIndex(value, ref steam.automaticindicator, key);
			                                break;
			                            case "injectorsound":
			                                    string[] injectorsplit = value.Split(',');
			                                    for (int k = 0; k < injectorsplit.Length; k++)
			                                    {
			                                        if (k == 0)
			                                        {
			                                            InternalFunctions.ValidateIndex(injectorsplit[0], ref steam.Injector.LoopSound, key);
			                                        }
			                                        else if(k == 1)
			                                        {
                                                        InternalFunctions.ValidateIndex(injectorsplit[1], ref steam.Injector.PlayOnceSound, key);
			                                        }
			                                        else
			                                        {
			                                            InternalFunctions.LogError("Unexpected extra paramaters were found in injectorsound. These have been ignored.",6);
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
                                                    InternalFunctions.ValidateIndex(cylindersplit[0], ref steam.CylinderCocks.LoopSound, key);
                                                }
                                                else if(k == 1)
                                                {
                                                    InternalFunctions.ValidateIndex(cylindersplit[1], ref steam.CylinderCocks.PlayOnceSound, key);
                                                }
                                                else
                                                {
                                                    InternalFunctions.LogError("Unexpected extra paramaters were found in cylindercocksound. These have been ignored.",6);
                                                    break;
                                                }
                                            }
                                            break;
                                        case "cylindercocksindicator":
                                            InternalFunctions.ValidateIndex(value, ref steam.CylinderCocks.PanelIndex, key);
                                            break;
			                            case "blowoffsound":
			                                InternalFunctions.ValidateIndex(value, ref steam.Blowoff.SoundIndex, key);
			                                break;
                                        case "blowoffindicator":
                                            InternalFunctions.ValidateIndex(value, ref steam.Blowoff.PanelIndex, key);
                                            break;
                                        case "blowofftime":
                                            InternalFunctions.ParseNumber(value, ref steam.Blowoff.BlowoffTime, key);
			                                break;
			                            case "klaxonpressureuse":
			                                InternalFunctions.ParseNumber(value, ref steam.klaxonpressureuse, key);
			                                break;
                                        case "blowers_pressurefactor":
                                            InternalFunctions.ParseNumber(value, ref steam.blowers_pressurefactor, key);
                                            break;
                                        case "blowers_firefactor":
                                            InternalFunctions.ParseNumber(value, ref steam.blowers_firefactor, key);
                                            break;
                                        case "blowersound":
                                            InternalFunctions.ValidateIndex(value, ref steam.Blowers.LoopSound, key);
                                            break;
                                        case "blowersindicator":
                                            InternalFunctions.ValidateIndex(value, ref steam.Blowers.PanelIndex, key);
                                            break;
                                        case "steamheatindicator":
                                            InternalFunctions.ValidateIndex(value, ref steam.steamheatindicator, key);
                                            break;
                                        case "steamheatpressureuse":
                                            InternalFunctions.ParseNumber(value, ref steam.steamheatpressureuse, key);
                                            break;
                                        case "boilerblowoffpressure":
                                            InternalFunctions.ParseNumber(value, ref steam.Blowoff.TriggerPressure, key);
                                            break;
                                        case "regulatorpressureuse":
                                            InternalFunctions.ParseNumber(value, ref steam.regulatorpressureuse, key);
                                            break;
                                        case "cylindercocks_pressureuse":
                                            string[] cylindercocksplit = value.Split(',');
                                            for (int k = 0; k < cylindercocksplit.Length; k++)
                                            {
                                                if (k == 0)
                                                {
                                                    InternalFunctions.ParseNumber(cylindercocksplit[0], ref steam.cylindercocks_basepressureuse, key);
                                                }
                                                else
                                                {
                                                    InternalFunctions.ParseNumber(cylindercocksplit[1], ref steam.cylindercocks_notchpressureuse, key);
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
			                                InternalFunctions.ValidateSetting(value, ref electric.heatingpart, key);
			                                break;
			                            case "heatingrate":
			                                this.electric.heatingrate = value;
			                                break;
			                            case "overheatwarn":
			                                InternalFunctions.ParseNumber(value, ref electric.overheatwarn, key);
			                                break;
			                            case "overheat":
			                                InternalFunctions.ParseNumber(value, ref electric.overheat, key);
			                                break;
			                            case "overheatresult":
			                                InternalFunctions.ValidateSetting(value, ref electric.overheatresult, key);
			                                break;
			                            case "thermometer":
			                                InternalFunctions.ValidateIndex(value, ref electric.thermometer, key);
			                                break;
			                            case "overheatindicator":
			                                InternalFunctions.ValidateIndex(value, ref electric.overheatindicator, key);
			                                break;
			                            case "overheatalarm":
			                                InternalFunctions.ValidateIndex(value, ref electric.overheatalarm, key);
			                                break;
			                            case "ammeter":
			                                InternalFunctions.ValidateIndex(value, ref electric.ammeter, key);
			                                break;
			                            case "ammetervalues":
			                                this.electric.ammetervalues = value;
			                                break;
			                            case "powerpickuppoints":
			                                this.electric.pickuppoints = value;
			                                break;
			                            case "powergapbehaviour":
			                                InternalFunctions.ValidateSetting(value, ref electric.powergapbehaviour, key);
			                                break;
			                            case "powerindicator":
			                                InternalFunctions.ValidateIndex(value, ref electric.powerindicator, key);
			                                break;
			                            case "breakersound":
			                                InternalFunctions.ValidateIndex(value, ref electric.breakersound, key);
			                                break;
			                            case "breakerindicator":
			                                InternalFunctions.ValidateIndex(value, ref electric.breakerindicator, key);
			                                break;
			                            case "pantographindicator_f":
			                                InternalFunctions.ValidateIndex(value, ref electric.pantographindicator_f, key);
			                                break;
			                            case "pantographindicator_r":
			                                InternalFunctions.ValidateIndex(value, ref electric.pantographindicator_r, key);
			                                break;
			                            case "pantographraisedsound":
			                                InternalFunctions.ValidateIndex(value, ref electric.pantographraisedsound, key);
			                                break;
			                            case "pantographloweredsound":
			                                InternalFunctions.ValidateIndex(value, ref electric.pantographloweredsound, key);
			                                break;
			                            case "pantographalarmsound":
			                                InternalFunctions.ValidateIndex(value, ref electric.pantographalarmsound, key);
			                                break;
			                            case "pantographretryinterval":
			                                InternalFunctions.ParseNumber(value, ref electric.pantographretryinterval, key);
			                                break;
			                            case "pantographalarmbehaviour":
			                                InternalFunctions.ValidateSetting(value, ref electric.pantographalarmbehaviour, key);
			                                break;
			                            case "powerloopsound":
			                                try
			                                {
			                                    string[] powerloopsplit = value.Split(',');
			                                    for (int k = 0; k < powerloopsplit.Length; k++)
			                                    {
			                                        if (k == 0)
			                                        {
			                                            this.electric.powerloopsound = Convert.ToInt32(powerloopsplit[0]);
			                                        }
			                                        else
			                                        {
			                                            this.electric.powerlooptime = Convert.ToInt32(powerloopsplit[1]);
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
			                                            this.electric.breakerloopsound = Convert.ToInt32(breakerloopsplit[0]);
			                                        }
			                                        else
			                                        {
			                                            this.electric.breakerlooptime = Convert.ToInt32(breakerloopsplit[1]);
			                                        }
			                                    }
			                                }
			                                catch
			                                {
			                                    InternalFunctions.LogError("breakerloopsound",0);
			                                }
			                                break;
			                            default:
			                                throw new InvalidDataException("The parameter " + key + " is not supported.");

			                        }
			                        break;
			                    case "diesel":
			                        switch (key)
			                        {
			                            case "automatic":
			                                InternalFunctions.ParseBool(value, ref diesel.automatic, key);
			                                break;
			                            case "heatingpart":
			                                InternalFunctions.ValidateSetting(value, ref diesel.heatingpart, key);
			                                break;
			                            case "heatingrate":
			                                this.diesel.heatingrate = value;
			                                break;
			                            case "overheatwarn":
			                                InternalFunctions.ParseNumber(value, ref diesel.overheatwarn, key);
			                                break;
			                            case "overheat":
			                                InternalFunctions.ParseNumber(value, ref diesel.overheat, key);
			                                break;
			                            case "overheatresult":
			                                InternalFunctions.ValidateSetting(value, ref diesel.overheatresult, key);
			                                break;
			                            case "thermometer":
			                                InternalFunctions.ValidateIndex(value, ref diesel.thermometer, key);
			                                break;
			                            case "overheatindicator":
			                                InternalFunctions.ValidateIndex(value, ref diesel.overheatindicator, key);
			                                break;
			                            case "overheatalarm":
			                                InternalFunctions.ValidateIndex(value, ref diesel.overheatalarm, key);
			                                break;
			                            case "fuelstartamount":
			                                InternalFunctions.ParseNumber(value, ref diesel.fuelstartamount, key);
			                                break;
			                            case "fuelindicator":
			                                InternalFunctions.ValidateIndex(value, ref diesel.fuelindicator, key);
			                                break;
			                            case "automaticindicator":
			                                InternalFunctions.ValidateIndex(value, ref diesel.automaticindicator, key);
			                                break;  
			                            case "gearratios":
			                                this.diesel.gearratios = value;
			                                break;
			                            case "gearfadeinrange":
			                                this.diesel.gearfadeinrange = value;
			                                break;
			                            case "gearfadeoutrange":
			                                this.diesel.gearfadeoutrange = value;
			                                break;
			                            case "gearindicator":
			                                InternalFunctions.ValidateIndex(value, ref diesel.gearindicator, key);
			                                break;
			                            case "gearchangesound":
			                                InternalFunctions.ValidateIndex(value, ref diesel.gearchangesound, key);
			                                break;
			                            case "tachometer":
			                                InternalFunctions.ValidateIndex(value, ref diesel.tachometer, key);
			                                break;
			                            case "allowneutralrevs":
			                                InternalFunctions.ValidateSetting(value, ref diesel.allowneutralrevs, key);
			                                break;
			                            case "revsupsound":
			                                InternalFunctions.ValidateIndex(value, ref diesel.revsupsound, key);
			                                break;
			                            case "revsdownsound":
			                                InternalFunctions.ValidateIndex(value, ref diesel.revsdownsound, key);
			                                break;
			                            case "motorsound":
			                                InternalFunctions.ValidateIndex(value, ref diesel.motorsound, key);
			                                break;
			                            case "fuelconsumption":
			                                this.diesel.fuelconsumption = value;
			                                break;
			                            case "fuelcapacity":
			                                InternalFunctions.ParseNumber(value, ref diesel.fuelcapacity, key);
			                                break;
			                            case "fuelfillspeed":
			                                InternalFunctions.ParseNumber(value, ref diesel.fuelfillspeed, key);
			                                break;
			                            case "fuelfillindicator":
			                                InternalFunctions.ValidateIndex(value, ref diesel.fuelfillindicator, key);
			                                break;
			                            case "gearloopsound":
			                                try
			                                {
			                                    string[] gearloopsplit = value.Split(',');
			                                    for (int k = 0; k < gearloopsplit.Length; k++)
			                                    {
			                                        if (k == 0)
			                                        {
			                                            this.diesel.gearloopsound = Convert.ToInt32(gearloopsplit[0]);
			                                        }
			                                        else
			                                        {
			                                            this.diesel.gearlooptime = Convert.ToInt32(gearloopsplit[1]);
			                                        }
			                                    }
			                                }
			                                catch
			                                {
			                                    InternalFunctions.LogError("gearloopsound",0);
			                                }
			                                break;
                                        case "reversercontrol":
                                            InternalFunctions.ValidateSetting(value, ref diesel.reversercontrol, key);
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
			                                InternalFunctions.ValidateSetting(value, ref vigilance.overspeedcontrol, key);
			                                break;
			                            case "warningspeed":
			                                InternalFunctions.ParseNumber(value, ref vigilance.warningspeed, key);
			                                break;
			                            case "overspeed":
			                                InternalFunctions.ParseNumber(value, ref vigilance.overspeed, key);
			                                break;
			                            case "safespeed":
			                                InternalFunctions.ParseNumber(value, ref vigilance.safespeed, key);
			                                break;
			                            case "overspeedindicator":
			                                InternalFunctions.ValidateIndex(value, ref vigilance.overspeedindicator, key);
			                                break;
			                            case "overspeedalarm":
			                                InternalFunctions.ValidateIndex(value, ref vigilance.overspeedalarm, key);
			                                break;
			                            case "overspeedtime":
			                                InternalFunctions.ParseNumber(value, ref vigilance.overspeedtime, key);
			                                break;
			                            case "vigilanceinterval":
			                                this.vigilance.vigilancetimes = value;
			                                break;
			                            case "vigilancelamp":
			                                InternalFunctions.ValidateIndex(value, ref vigilance.vigilancelamp, key);
			                                break;
			                            case "deadmanshandle":
			                                InternalFunctions.ValidateSetting(value, ref vigilance.deadmanshandle, key);
			                                break;
			                            case "vigilanceautorelease":
			                                InternalFunctions.ValidateSetting(value, ref vigilance.vigilanceautorelease, key);
			                                break;
			                            case "vigilancecancellable":
			                                InternalFunctions.ValidateSetting(value, ref vigilance.vigilancecancellable, key);
			                                break;
			                            case "independentvigilance":
			                                InternalFunctions.ValidateSetting(value, ref vigilance.independantvigilance, key);
			                                break;
			                            case "draenabled":
			                                InternalFunctions.ValidateSetting(value, ref vigilance.draenabled, key);
			                                break;
			                            case "drastartstate":
			                                InternalFunctions.ValidateSetting(value, ref vigilance.drastartstate, key);
			                                break;
			                            case "draindicator":
			                                InternalFunctions.ValidateIndex(value, ref vigilance.draindicator, key);
			                                break;
			                            case "vigilancealarm":
			                                InternalFunctions.ValidateIndex(value, ref vigilance.vigilancealarm, key);
			                                break;
			                            case "vigilancedelay1":
			                                InternalFunctions.ParseNumber(value, ref vigilance.vigilancedelay1, key);
			                                break;
			                            case "vigilancedelay2":
			                                InternalFunctions.ParseNumber(value, ref vigilance.vigilancedelay2, key);
			                                break;
			                            case "vigilanceinactivespeed":
			                                InternalFunctions.ParseNumber(value, ref vigilance.vigilanceinactivespeed, key);
			                                break;
                                        case "vigilante":
                                            InternalFunctions.ParseBool(value, ref vigilance.vigilante, key);
                                            break;
			                            default:
			                                throw new InvalidDataException("The parameter " + key + " is not supported.");
                                            
			                        }
			                        break;
			                    case "interlocks":
			                        switch (key)
			                        {
			                            case "doorpowerlock":
			                                InternalFunctions.ValidateSetting(value, ref tractionmanager.doorpowerlock, key);
			                                break;
			                            case "doorapplybrake":
			                                InternalFunctions.ValidateSetting(value, ref tractionmanager.doorapplybrake, key);
			                                break;
			                            case "neutralrvrbrake":
			                                InternalFunctions.ValidateSetting(value, ref tractionmanager.neutralrvrbrake, key);
			                                break;
			                            case "neutralrvrbrakereset":
			                                InternalFunctions.ValidateSetting(value, ref tractionmanager.neutralrvrbrakereset, key);
			                                break;
			                            case "directionindicator":
			                                InternalFunctions.ValidateIndex(value, ref tractionmanager.directionindicator, key);
			                                break;
			                            case "reverserindex":
			                                InternalFunctions.ValidateIndex(value, ref tractionmanager.reverserindex, key);
			                                break;
			                            case "travelmeter100":
			                                InternalFunctions.ValidateIndex(value, ref tractionmanager.travelmeter100, key);
			                                break;
			                            case "travelmeter10":
			                                InternalFunctions.ValidateIndex(value, ref tractionmanager.travelmeter10, key);
			                                break;
			                            case "travelmeter1":
			                                InternalFunctions.ValidateIndex(value, ref tractionmanager.travelmeter1, key);
			                                break;
			                            case "travelmeter01":
			                                InternalFunctions.ValidateIndex(value, ref tractionmanager.travelmeter01, key);
			                                break;
			                            case "travelmeter001":
			                                InternalFunctions.ValidateIndex(value, ref tractionmanager.travelmeter001, key);
			                                break;
			                            case "travelmetermode":
			                                InternalFunctions.ValidateSetting(value, ref tractionmanager.travelmetermode, key);
			                                break;
			                            case "klaxonindicator":
			                                this.tractionmanager.klaxonindicator = value;
			                                break;
			                            case "customindicators":
			                                this.tractionmanager.customindicators = value;
			                                break;
                                        case "customindicatorsounds":
                                            this.tractionmanager.customindicatorsounds = value;
			                                break;
										case "customindicatorbehaviour":
					                        this.tractionmanager.customindicatorbehaviour = value;
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
			                                InternalFunctions.ValidateIndex(value, ref AWS.awswarningsound, key);
			                                break;
			                            case "awsclearsound":
			                                InternalFunctions.ValidateIndex(value, ref AWS.awsclearsound, key);
			                                break;
			                            case "awsdelay":
			                                InternalFunctions.ParseNumber(value, ref AWS.canceltimeout, key);
			                                break;
			                            case "tpwswarningsound":
			                                InternalFunctions.ValidateIndex(value, ref AWS.tpwswarningsound, key);
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
                                            InternalFunctions.ParseBool(value, ref tractionmanager.independantreset, key);
			                                break;
			                        }
			                        break;

			                    case "keyassignments":
			                        switch (key)
			                        {
			                                //No validation is necessary for key assignments
			                                //Errors here simply mean they won't be matched
			                            case "safetykey":
			                                this.tractionmanager.safetykey = value;
			                                break;
			                            case "automatickey":
			                                this.tractionmanager.automatickey = value;
			                                break;
			                            case "injectorkey":
			                                this.tractionmanager.injectorkey = value;
			                                break;
                                        case "blowerskey":
                                            this.tractionmanager.blowerskey = value;
                                            break;
			                            case "cutoffdownkey":
			                                this.tractionmanager.cutoffdownkey = value;
			                                break;
			                            case "cutoffupkey":
			                                this.tractionmanager.cutoffupkey = value;
			                                break;
			                            case "fuelkey":
			                                this.tractionmanager.fuelkey = value;
			                                break;
                                        case "enginestartkey":
			                                this.tractionmanager.EngineStartKey = value;
			                                break;
                                        case "enginestopkey":
                                            this.tractionmanager.EngineStopKey = value;
                                            break;
			                            case "wiperspeedup":
			                                this.tractionmanager.wiperspeedup = value;
			                                break;
			                            case "wiperspeeddown":
			                                this.tractionmanager.wiperspeeddown = value;
			                                break;
			                            case "isolatesafetykey":
			                                if (WesternDiesel == null)
			                                {
			                                    this.tractionmanager.isolatesafetykey = value;
			                                }
			                                else
			                                {
			                                    this.tractionmanager.WesternAWSIsolationKey = value;
			                                }
			                                break;
			                            case "gearupkey":
			                                this.tractionmanager.gearupkey = value;
			                                break;
			                            case "geardownkey":
			                                this.tractionmanager.geardownkey = value;
			                                break;
			                            case "drakey":
			                                this.tractionmanager.DRAkey = value;
			                                break;
			                            case "customindicatorkey1":
			                                this.tractionmanager.customindicatorkey1 = value;
			                                break;
			                            case "customindicatorkey2":
			                                this.tractionmanager.customindicatorkey2 = value;
			                                break;
			                            case "customindicatorkey3":
			                                this.tractionmanager.customindicatorkey3 = value;
			                                break;
			                            case "customindicatorkey4":
			                                this.tractionmanager.customindicatorkey4 = value;
			                                break;
			                            case "customindicatorkey5":
			                                this.tractionmanager.customindicatorkey5 = value;
			                                break;
			                            case "customindicatorkey6":
			                                this.tractionmanager.customindicatorkey6 = value;
			                                break;
			                            case "customindicatorkey7":
			                                this.tractionmanager.customindicatorkey7 = value;
			                                break;
			                            case "customindicatorkey8":
			                                this.tractionmanager.customindicatorkey8 = value;
			                                break;
			                            case "customindicatorkey9":
			                                this.tractionmanager.customindicatorkey9 = value;
			                                break;
			                            case "customindicatorkey10":
			                                this.tractionmanager.customindicatorkey10 = value;
			                                break;
			                            case "frontpantographkey":
			                                this.tractionmanager.frontpantographkey = value;
			                                break;
			                            case "rearpantographkey":
			                                this.tractionmanager.rearpantographkey = value;
			                                break;
			                            case "advancedrivingkey":
			                                this.tractionmanager.advancedrivingkey = value;
			                                break;
                                        case "steamheatincreasekey":
                                            this.tractionmanager.steamheatincreasekey = value;
			                                break;
                                        case "steamheatdecreasekey":
                                            this.tractionmanager.steamheatdecreasekey = value;
                                            break;
                                        case "cylindercockskey":
                                            this.tractionmanager.cylindercockskey = value;
                                            break;
                                        case "headcodekey":
                                            this.tractionmanager.headcodekey = value;
                                            break;
                                        case "cawskey":
                                            this.tractionmanager.CAWSKey = value;
                                            break;
                                        case "impvelsukey":
                                            this.tractionmanager.SCMTincreasespeed = value;
                                            break;
                                        case "impvelgiukey":
                                            this.tractionmanager.SCMTdecreasespeed = value;
                                            break;
                                        case "scmtkey":
                                            this.tractionmanager.TestSCMTKey = value;
                                            break;
                                        case "lcmupkey":
                                            this.tractionmanager.LCMupKey = value;
                                            break;
                                        case "lcmdownkey":
                                            this.tractionmanager.LCMdownkey = value;
                                            break;
                                        case "abbancokey":
                                            this.tractionmanager.AbilitaBancoKey = value;
                                            break;
                                        case "consavvkey":
                                            this.tractionmanager.ConsensoAvviamentoKey = value;
                                            break;
                                        case "avvkey":
                                            this.tractionmanager.AvviamentoKey = value;
                                            break;
                                        case "spegnkey":
                                            this.tractionmanager.SpegnimentoKey = value;
                                            break;
                                        case "vigilantekey":
                                            this.tractionmanager.vigilantekey = value;
                                            break;
                                        case "vigilanteresetkey":
                                            this.tractionmanager.vigilanteresetkey = value;
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
                                            this.tractionmanager.PZBKey = value;
			                                break;
                                        case "pzbreleasekey":
			                                this.tractionmanager.PZBReleaseKey = value;
			                                break;
                                        case "pzbstopoverridekey":
			                                this.tractionmanager.PZBStopOverrideKey = value;
			                                break;
                                        //Keys added for BR Class 52 'Western' Diesel Locomotive
                                        case "westernbatteryswitch":
                                            this.tractionmanager.WesternBatterySwitch = value;
                                            break;
                                        case "westernfuelpumpswitch":
                                            this.tractionmanager.WesternFuelPumpSwitch = value;
                                            break;
                                        case "westernmasterkey":
			                                this.tractionmanager.WesternMasterKey = value;
                                            break;
                                        case "westerntransmissionresetkey":
			                                this.tractionmanager.WesternTransmissionResetButton = value;
                                            break;
                                        case "westernengineswitchkey":
			                                this.tractionmanager.WesternEngineSwitchKey = value;
			                                break;
                                        case "westernfirebellkey":
                                            this.tractionmanager.WesternFireBellKey = value;
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
            if (this.tractionmanager != null)
            {
                devices.Add(this.tractionmanager);
            }

            if (this.StartupSelfTestManager != null)
            {
                devices.Add(this.StartupSelfTestManager);
            }
            
            if (this.steam != null) {
				devices.Add(this.steam);
			}

            if (this.diesel != null)
            {
                devices.Add(this.diesel);
            }

            if (this.electric != null)
            {
                devices.Add(this.electric);
            }

            if (this.vigilance != null)
            {
                devices.Add(this.vigilance);
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
		    if (this.LedLights != null)
		    {
		        devices.Add(this.LedLights);
		    }
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
            //Set the state of the ATC/ATS switch indicator
            signal.Kirikae = (flag ? Atc.KirikaeStates.ToAts : Atc.KirikaeStates.ToAtc);
            //??Set whether this signal is a distant??
            signal.ZenpouYokoku = flag1;
            //Set whether this signal has overrun protection
            signal.OverrunProtector = flag2;
            //Finally return the signal
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
                trainspeed = (int)data.Vehicle.Speed.KilometersPerHour;
                this.previouslocation = this.trainlocation;
                this.trainlocation = data.Vehicle.Location;
                
                //Calculate direction
                if (this.trainlocation > this.previouslocation)
                {
                    this.direction = 1;
                }
                else if (this.trainlocation == this.previouslocation)
                {
                    this.direction = 0;
                }
                else
                {
                    this.direction = 2;
                }
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
		internal void SetSignal(SignalData[] signal) {

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
                    if (this.electric != null)
                    {
                        /* APC magnets and neutral section */
                        if (beacon.Optional > 0)
                        {
                            /* Handle legacy APC magnet behaviour with only one beacon */
                            int secondMagnetDistance;
                            int.TryParse(beacon.Optional.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out secondMagnetDistance);
                            electric.legacypowercutoff((int)trainlocation, secondMagnetDistance);
                        }
                        else if (beacon.Optional == -1)
                        {
                            //Signals the start and end of the neutral section
                            electric.newpowercutoff((int)trainlocation);
                        }
                        else
                        {
                            //Opens/ Closes ACB/VCB
                            //Line volts indicator should be illuminated, and should be resetable
                            electric.breakertrip();
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
                            if (this.steam != null)
                            {
                                this.steam.fuelling = false;
                            }
                            if (this.diesel != null)
                            {
                                this.diesel.fuelling = false;
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
                            AWS.Suppress(trainlocation);
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
                                TPWS.ArmTss(beacon.Optional, trainlocation);
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
            if (this.SCMT.enabled == true)
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
