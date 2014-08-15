using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using OpenBveApi.Runtime;

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

        /// <summary>Startup Self-Test Manager</summary>
        internal StartupSelfTestManager StartupSelfTestManager;

        /// <summary>Windscreen & Wipers</summary>
        internal Windscreen Windscreen;

        /// <summary>Advanced animation handlers</summary>
        internal Animations Animations;


		/// <summary>A list of all the devices installed on this train</summary>
		internal Device[] Devices;


  		// --- constructors ---

		/// <summary>Creates a new train without any devices installed.</summary>
		/// <param name="panel">The array of panel variables.</param>
		/// <param name="playSound">The delegate to play sounds.</param>
		internal Train(int[] panel) {
			this.PluginInitializing = false;
			this.Specs = new VehicleSpecs(0, BrakeTypes.ElectromagneticStraightAirBrake, 0, false, 0);
			this.State = new VehicleState(0.0, new Speed(0.0), 0.0, 0.0, 0.0, 0.0, 0.0);
			this.Handles = new ReadOnlyHandles(new Handles(0, 0, 0, false));
			this.Doors = DoorStates.None;
			this.Panel = panel;
			
		}
		
		
		// --- functions ---
		
		/// <summary>Sets up the devices from the specified configuration file.</summary>
		/// <param name="file">The configuration file.</param>
		internal void LoadConfigurationFile(string file) {
            //Initialise all safety devices first
            //Set Enabled state if they are installed
            
            //Only initialise traction types if required
            this.tractionmanager = new tractionmanager(this);
            this.StartupSelfTestManager = new StartupSelfTestManager(this);
            this.AWS = new AWS(this);
            this.TPWS = new TPWS(this);
            this.Windscreen = new Windscreen(this);
            this.Animations = new Animations(this);
			string[] lines = File.ReadAllLines(file, Encoding.UTF8);
			string section = string.Empty;
			for (int i = 0; i < lines.Length; i++) {
				string line = lines[i];
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
                                break;
                            case "diesel":
                                this.diesel = new diesel(this);
                                break;
                            case "electric":
                                this.electric = new electric(this);
                                break;
                            case "vigilance":
                                this.vigilance = new vigilance(this);
                                break;
                            case "aws":
                                this.AWS.enabled = true;
                                break;
                            case "tpws":
                                this.TPWS.enabled = true;
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
                            case "windscreen":
                                this.Windscreen.enabled = true;
                                break;
							default:
								throw new InvalidDataException("The section " + line[0] + " is not supported.");
						}
					} else {
						int equals = line.IndexOf('=');
						if (equals >= 0) {
							string key = line.Substring(0, equals).Trim().ToLowerInvariant();
							string value = line.Substring(equals + 1).Trim();
							switch (section) {
								case "steam":
									switch (key) {
                                        case "automatic":
                                        this.steam.automatic = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "heatingpart":
                                        this.steam.heatingpart = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "heatingrate":
                                        this.steam.heatingrate = value;
                                        break;
                                        case "overheatwarn":
                                        this.steam.overheatwarn = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "overheat":
                                        this.steam.overheat = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "overheatresult":
                                        this.steam.overheatresult = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "thermometer":
                                        this.steam.thermometer = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "overheatindicator":
                                        this.steam.overheatindicator = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "overheatalarm":
                                        this.steam.overheatalarm = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "cutoffmax":
                                        this.steam.cutoffmax = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "cutoffineffective":
                                        this.steam.cutoffineffective = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "cutoffratio":
                                        this.steam.cutoffratio = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "cutoffratiobase":
                                        this.steam.cutoffratiobase = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "cutoffmin":
                                        this.steam.cutoffmin = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "cutoffdeviation":
                                        this.steam.cutoffdeviation = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "cutoffindicator":
                                        this.steam.cutoffindicator = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "boilermaxpressure":
                                        this.steam.boilermaxpressure = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "boilerminpressure":
                                        this.steam.boilerminpressure = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "boilerstartwaterlevel":
                                        this.steam.boilerstartwaterlevel = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "boilermaxwaterlevel":
                                        this.steam.boilermaxwaterlevel = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "boilerpressureindicator":
                                        this.steam.boilerpressureindicator = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "boilerwaterlevelindicator":
                                        this.steam.boilerwaterlevelindicator = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "boilerwatertosteamrate":
                                        this.steam.boilerwatertosteamrate = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "fuelindicator":
                                        this.steam.fuelindicator = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "fuelstartamount":
                                        this.steam.fuelstartamount = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "fuelcapacity":
                                        this.steam.fuelcapacity = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "fuelfillspeed":
                                        this.steam.fuelfillspeed = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "fuelfillindicator":
                                        this.steam.fuelfillindicator = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "injectorrate":
                                        this.steam.injectorrate = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "injectorindicator":
                                        this.steam.injectorindicator = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "automaticindicator":
                                        this.steam.automaticindicator = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "injectorsound":
                                        this.steam.injectorsound = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "blowoffsound":
                                        this.steam.blowoffsound = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                        break;
                                        case "klaxonpressureuse":
                                        this.steam.klaxonpressureuse = double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
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
                                            InternalFunctions.LogError("powerloopsound");
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
                                            InternalFunctions.LogError("breakerloopsound");
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
                                            InternalFunctions.ValidateSetting(value, ref diesel.automatic, key);
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
                                                InternalFunctions.LogError("gearloopsound");
                                            }
                                            break;
                                        default:
                                            throw new InvalidDataException("The parameter " + key + " is not supported.");

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
                                        default:
                                            throw new InvalidDataException("The parameter " + key + " is not supported.");
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
                                                for (int j = 0; j < tpwssplit1.Length; j++)
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
                                                InternalFunctions.LogError("tpwsindicator");
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
                                                InternalFunctions.LogError("tpwsindicator2");
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
                                                InternalFunctions.LogError("dropsound");
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
                                        case "gear_yvariable":
                                            InternalFunctions.ValidateIndex(value, ref Animations.gear_Yvariable, key);
                                            break;
                                        case "gear_zvariable":
                                            InternalFunctions.ValidateIndex(value, ref Animations.gear_Zvariable, key);
                                            break;
                                        case "wheelradius":
                                            InternalFunctions.ParseNumber(value, ref Animations.wheelradius, key);
                                            break;
                                            default:
                                            throw new InvalidDataException("The parameter " + key + " is not supported.");
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
                                        case "cutoffdownkey":
                                            this.tractionmanager.cutoffdownkey = value;
                                            break;
                                        case "cutoffupkey":
                                            this.tractionmanager.cutoffupkey = value;
                                            break;
                                        case "fuelkey":
                                            this.tractionmanager.fuelkey = value;
                                            break;

                                        case "wiperspeedup":
                                            this.tractionmanager.wiperspeedup = value;
                                            break;
                                        case "wiperspeeddown":
                                            this.tractionmanager.wiperspeeddown = value;
                                            break;
                                        case "isolatesafetykey":
                                            this.tractionmanager.isolatesafetykey = value;
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
			List<Device> devices = new List<Device>();
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

            if (this.Windscreen != null)
            {
                devices.Add(this.Windscreen);
            }

            if (this.Animations != null)
            {
                devices.Add(this.Animations);
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
		}
		
		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data.</param>
		internal void Elapse(ElapseData data) {
            
			this.PluginInitializing = false;
			if (data.ElapsedTime.Seconds > 0.0 & data.ElapsedTime.Seconds < 1.0) {
				// --- panel ---
				for (int i = 0; i < this.Panel.Length; i++) {
					this.Panel[i] = 0;
				}
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
				for (int i = (int)VirtualKeys.S; i <= (int)VirtualKeys.C2; i++) {
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
                this.trainspeed = (int)data.Vehicle.Speed.KilometersPerHour;
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
			int index = (int)key;
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
			int index = (int)key;
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
            }
            //AWS Beacons
            if (this.AWS.enabled == true)
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
                        }
                        else if (beacon.Optional == 270)
                        {
                            /* This is an AWS suppression electromagnet - new prototypical behaviour */
                            AWS.Suppress(trainlocation);
                        }
                        else if (beacon.Optional == 360)
                        {
                            /* The following if statement must remain nested in the containing if statement */
                            if (beacon.Signal.Aspect > 3)
                            {
                                /* This is the north pole of the AWS electromagnet - it is energised, so issue a clear indication */
                                AWS.IssueClear();
                            }
                        }
                        else if (beacon.Signal.Aspect <= 3)
                        {
                            /* Aspect is restrictive, so issue a warning - this is the legacy fallback behaviour */
                            AWS.Prime();
                        }
                        else if (beacon.Signal.Aspect > 3)
                        {
                            /* Aspect is clear, so issue a clear inducation - this is the legacy fallback behaviour */
                            AWS.IssueLegacyClear();
                        }
                        break;
                    case 44001:
                        /* Permanently installed Automatic Warning System magnet which triggers a warning whenever passed over.
                         * 
                         * Issue a warning regardless - this is the legacy fallback behaviour ONLY */
                        AWS.Prime();
                        break;
                }
                if (this.TPWS.enabled == true)
                {
                    switch (beacon.Type)
                    {
                        case 44002:
                            /* Train Protection and Warning System Overspeed Sensor induction loop - associated with signal */

                            if (beacon.Signal.Aspect == 0)
                            {
                                TPWS.ArmOss(beacon.Optional);
                            }
                            break;
                        case 44003:
                            /* Train Protection and Warning System Train Stop Sensor induction loop */
                            if (beacon.Signal.Aspect == 0)
                            {
                                TPWS.ArmTss(beacon.Optional, trainlocation);
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
                            break;
                    }
                }
            }
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
