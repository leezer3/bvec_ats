﻿using System;
using OpenBveApi.Runtime;


namespace Plugin {
	/// <summary>Represents a steam locomotive.</summary>
	internal class steam : Device {
		
		// --- members ---
		
		/// <summary>The underlying train.</summary>
		private readonly Train Train;
		
		
		//Internal Variables
        internal double heatingtimer;
        internal double currentheat;
        internal double cutoff;
        internal double optimalcutoff;
        internal double speed;
        internal double temperature;
        internal int stm_reverser;
        internal int new_reverser;
        internal int new_power;
        internal int pressureup;
        internal int pressureuse;
	    internal static int steamheatlevel;
        /// <summary>Stores the current boiler pressure</summary>
        internal int stm_boilerpressure;
        /// <summary>Stores the current boiler water level</summary>
        internal int stm_boilerwater;

        /// <summary>Stores whether the injectors are currently active</summary>
        internal bool stm_injector;
        /// <summary>Stores the current water level in the tanks</summary>
        internal int fuel;
        /// <summary>Stores the maximum possible ATS power notch [INTERNAL]</summary>
        internal int stm_power;
        /// <summary>Stores the current state of the cutoff</summary>
        internal int cutoffstate;
        internal double cutofftimer;
        /// <summary>Stores whether we are currently taking on coal & water</summary>
        internal bool fuelling;

        /// <summary>The basic rate at which water turns to steam per second under ideal conditions</summary>
        /// Calculated once at initilisation from the initial variables which are per minute
        internal int calculatedsteamrate;
        /// <summary>The rate at which steam is generated</summary>
        internal int finalsteamrate;
        /// <summary>The current fire mass</summary>
        internal int firemass;
        /// <summary>The current fire temperature</summary>
        internal int firetemp;
        /// <summary>Whether we are currently shovelling coal</summary>
        internal bool shovelling;
        /// <summary>Stores whether the blowers are currently active</summary>
        internal bool blowers;

		// --- constants ---
	    /// <summary>Is our transmission automatic</summary>
	    internal bool automatic;

		/// <summary>Do we heave a part that heats up?</summary>
		internal int heatingpart = 0;

        /// <summary>Overheat warning temperature</summary>
        internal double overheatwarn = 0;

        /// <summary>Overheat temperature</summary>
        internal double overheat = 0;

        /// <summary>What happens when we overheat?</summary>
        internal int overheatresult = 0;

        /// <summary>Panel indicator for thermometer</summary>
        internal int thermometer = -1;

        /// <summary>Panel indicator for overheat indicator</summary>
        internal int overheatindicator = -1;

        /// <summary>Sound index for overheat alarm</summary>
        internal int overheatalarm = -1;

        /// <summary>Default paramaters</summary>
        /// 
        /// Used if no value is loaded from the config file
        /// <summary>The maximum cutoff value</summary>
        internal double cutoffmax = 75;
        /// <summary>The minimum cutoff value</summary>
        internal double cutoffmin = -55;
        /// <summary>The range on either side of zero at which cutoff is ineffective</summary>
        internal double cutoffineffective = 15;
        /// <summary>The the maximum speed in kph where maximum cutoff is effective</summary>
        internal double cutoffratiobase = 30;
        /// <summary>The ratio at which the ideal cutoff drops in relation to the speed of the train</summary>
        internal double cutoffratio = 10;
        /// <summary>The effective range around the ideal cutoff</summary>
        internal double cutoffdeviation = 8;
        /// <summary>The maximum boiler pressure</summary>
        internal double boilermaxpressure = 20000;
        /// <summary>The minimum boiler pressure</summary>
        internal double boilerminpressure = 12000;
        /// <summary>The maximum water level in the boiler</summary>
        internal double boilermaxwaterlevel = 1600;
        /// <summary>The number of water units converted to pressure units per minute at the maximum fire intensity</summary>
        internal double boilerwatertosteamrate = 1500;
        /// <summary>The starting amount of water in the water tanks</summary>
        internal double fuelstartamount = 20000;
        /// <summary>The number of units moved from the water tanks to the boiler per second</summary>
        internal double injectorrate = 100;
        /// <summary>The number of pressure units used per second at maximum regulator</summary>
        internal double regulatorpressureuse = 32;
        internal double cutoffchangespeed = 40;
        internal double cutoffchangetest = 1;
        internal double boilerstartwaterlevel = -1;
        /// <summary>The pressure at which the boiler blowoff will operate</summary>
        internal double blowoffpressure = 21000;
        internal string heatingrate = "0";
        internal double fuelfillspeed = 50;
        /// <summary>The capacity of the fuel tanks</summary>
        internal double fuelcapacity = 20000;
        /// <summary>The panel index of the fuelling indicator</summary>
        internal int fuelfillindicator = -1;
        /// <summary>The number of pressure units used per second by the whistle</summary>
        internal double klaxonpressureuse = -1;
        /// <summary>The blowers factor for pressure increase</summary>
        internal double blowers_pressurefactor = 1;
        /// <summary>The blowers factor for the fire</summary>
        internal double blowers_firefactor = 1;
        /// <summary>The sound index to be played whilst the blowers are active</summary>
	    internal int blowersound = -1;
        /// <summary>The number of pressure units used by each steam heating 'notch'</summary>
	    internal double steamheatpressureuse = -1;
        /// <summary>The starting mass of the fire</summary>
        internal double firestartmass = -1;
        /// <summary>The maximum fire mass</summary>
        internal double maximumfiremass = 1000;
        /// <summary>The starting temperature of the fire</summary>
        internal double firestartemp = 500;
        /// <summary>The number of units added per second whilst coal is being shovelled</summary>
        internal double shovellingrate = 10;
        /// <summary>Stores whether the cylinder cocks are currently open</summary>
	    internal static bool cylindercocks;

        /// <summary>The base pressure use per second when the cylinder cocks are open</summary>
	    internal double cylindercocks_basepressureuse = 0;
        /// <summary>The pressure use of each throttle notch whilst the cylinder cocks are open</summary>
	    internal double cylindercocks_notchpressureuse = 0;
        /// <summary>Stores whether advanced firing is enabled</summary>
        internal bool advancedfiring;

        //Panel Indicies
        /// <summary>The panel index of the reverser indicator</summary>
        internal int reverserindex = -1;
        /// <summary>The panel index of the boiler pressure gauge</summary>
        internal int boilerpressureindicator = -1;
        /// <summary>The panel index of the boiler water level gauge</summary>
        internal int boilerwaterlevelindicator = -1;
        /// <summary>The panel index of the cutoff indicator</summary>
        internal int cutoffindicator = -1;
        /// <summary>The panel index of the tanks water level indicator</summary>
        internal int fuelindicator = -1;
        /// <summary>The panel index of the injectors indicator</summary>
        internal int injectorindicator = -1;
        /// <summary>The panel index of the automatic cutoff & reverser indicator</summary>
        internal int automaticindicator = -1;
        /// <summary>The panel index of the steam heat level indicator</summary>
        internal int steamheatindicator = -1;

        //Sound Indicies
        /// <summary>The sound index played looped whilst the injectors are active</summary>
        internal int injectorloopsound = -1;
        /// <summary>The sound index played when the injectors are activated/ deactivated</summary>
        internal int injectorclanksound = -1;
        /// <summary>The sound index played when the boiler blows off excess pressure</summary>
        internal int blowoffsound = -1;
        /// <summary>The panel index triggered played when the boiler blows off excess pressure</summary>
        internal int blowoffindicator = -1;

	    internal bool injectorclank;

        //Used to run the blowoff timer
	    internal bool blowofftriggered;
	    internal double blowofftimer;

	    internal double maintimer;
        //Arrays
        int[] heatingarray;
		
		// --- constructors ---
		
		/// <summary>Creates a new instance of this system.</summary>
		/// <param name="train">The train.</param>
		internal steam(Train train) {
			this.Train = train;
            this.cutofftimer = 0.0;
		    this.maintimer = 0.0;


		}
		
		//<param name="mode">The initialization mode.</param>
		internal override void Initialize(InitializationModes mode) {
            stm_reverser = 0;
            stm_power = 0;
            cutoff = 30;
            cutoffstate = 0;
            stm_boilerpressure = (int)boilermaxpressure;
            if (boilerstartwaterlevel == -1)
            {
                stm_boilerwater = (int)boilermaxwaterlevel;
            }
            else
            {
                stm_boilerwater = (int)boilerstartwaterlevel;
            }
            fuel = (int)fuelstartamount;
            this.cutofftimer = 0.0;
            this.maintimer = 0.0;

            string[] splitheatingrate = heatingrate.Split(',');
            heatingarray = new int[splitheatingrate.Length];
            for (int i = 0; i < heatingarray.Length; i++)
            {
                heatingarray[i] = Int32.Parse(splitheatingrate[i]);
            }
            //Calculate the water to steam rate and store it here, don't call every frame
            calculatedsteamrate = (int)boilerwatertosteamrate / 60;
            //Set the starting fire mass
            if (firestartmass == -1)
            {
                firemass = (int)maximumfiremass;
            }
            else
            {
                firemass = (int)firestartmass;
            }
            firetemp = (int)firestartemp;

		}

        

		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data.</param>
		/// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
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
                            currentheat = heatingarray[0];
                        }
                        else if (Train.Handles.PowerNotch < heatingarray.Length)
                        {
                            currentheat = heatingarray[Train.Handles.PowerNotch];
                        }
                        else
                        {
                            currentheat = heatingarray[heatingarray.Length - 1];
                        }
                        temperature += currentheat;
                    }
                }
                else
                {
                    //Heats based upon RPM- Not on a steam loco!
                    this.temperature = 0.0;
                    this.heatingtimer = 0.0;
                }

                //Keep temperature below max & above zero
                if (temperature > overheat)
                {
                    temperature = overheat;
                    if (overheatresult == 1)
                    {
                        tractionmanager.demandpowercutoff();
                        tractionmanager.overheated = true;
                    }
                }
                else if (temperature < overheat && temperature > 0)
                {
                    tractionmanager.resetpowercutoff();
                    tractionmanager.overheated = false;
                }
                else if (temperature < 0)
                {
                    temperature = 0;
                }
            }

            //First try to set automatic cutoff without calculating
            if (automatic == true && Train.trainspeed == 0)
            {

                if (Train.Handles.Reverser == 0)
                {
                    //If reverser is in neutral, reset cutoff to 30
                    cutoff = 30;
                }
                else if (Train.Handles.Reverser == 1 && cutoff >= cutoffineffective)
                {
                    cutoff = cutoffmax;
                }
                else if (Train.Handles.Reverser == -1 && cutoff >= cutoffineffective)
                {

                    cutoff = cutoffmin;
                } 
            }
            else
            {
                double setcutoff = cutoff;
                if (cutoffstate == 1)
                {
                    //Set Cutoff Up       
                    if (cutoff < cutoffmax)
                    {
                        this.cutofftimer += data.ElapsedTime.Milliseconds;
                        if (this.cutofftimer > 40)
                        {
                            setcutoff = setcutoff + 1;
                            cutofftimer = 0.0;

                        }

                    }



                }

                else if (cutoffstate == -1)
                {
                    //Set Cutoff Down
                    if (cutoff > cutoffmin)
                    {
                        this.cutofftimer += data.ElapsedTime.Milliseconds;
                        if (this.cutofftimer > 40)
                        {
                            setcutoff = setcutoff - 1;
                            cutofftimer = 0.0;


                        }

                    }
                }
                else
                {

                    //twiddle our thumbs
                }
                cutoff = setcutoff;
            }

            //Manual

            {

                //This section of code operates the reverser & cutoff
                if (cutoff > cutoffineffective && (cutoffdeviation != 0))
                {

                    new_reverser = 1;
                    //Called to workout the optimum cutoff if we're over the optimum max cutoff speed FW
                    if (data.Vehicle.Speed.KilometersPerHour > cutoffratiobase)
                    {

                        speed = data.Vehicle.Speed.KilometersPerHour - cutoffratiobase;
                        optimalcutoff = cutoffmax - speed * cutoffratio / 10;
                        new_power = Math.Max((int)(this.Train.Specs.PowerNotches - ((optimalcutoff - cutoff) < 0 ? -(optimalcutoff - cutoff) : (optimalcutoff - cutoff)) / (int)cutoffdeviation), 0);
                        //Automagically set cutofff
                        if (automatic == true)
                        {

                            cutoff = (int)Math.Max(optimalcutoff, cutoffineffective + 1);
                        }
                    }
                    else
                    {
                        new_power = Math.Max((int)(this.Train.Specs.PowerNotches - (((int)cutoffmax - cutoff) < 0 ? -((int)cutoffmax - cutoff) : ((int)cutoffmax - cutoff)) / cutoffdeviation), 0);

                    }
                }
                else if (cutoff < cutoffineffective && cutoffdeviation != 0)
                {

                    new_reverser = -1;
                    //Called to workout the optimum cutoff if we're over the optimum max cutoff speed RV
                    if (Math.Abs(data.Vehicle.Speed.KilometersPerHour) > cutoffratiobase)
                    {
                        speed = Math.Abs(data.Vehicle.Speed.KilometersPerHour) - cutoffratiobase;
                        optimalcutoff = cutoffmin + speed * cutoffratio / 10;
                        new_power = Math.Max((int)(this.Train.Specs.PowerNotches - ((optimalcutoff - cutoff) < 0 ? -(optimalcutoff - cutoff) : (optimalcutoff - cutoff)) / (int)cutoffdeviation), 0);
                        //Automagically set cutoff
                        if (automatic == true)
                        {
                            cutoff = (int)Math.Min(optimalcutoff, -cutoffineffective - 1);
                        }
                    }
                    else
                    {
                        new_power = Math.Max((int)(this.Train.Specs.PowerNotches - ((Math.Abs(cutoffmin) - Math.Abs(cutoff)) < 0 ? -((int)Math.Abs(cutoffmin)
                            - Math.Abs(cutoff)) : ((int)Math.Abs(cutoffmin) - Math.Abs(cutoff))) / cutoffdeviation), 0);
                    }
                    
                }
                else
                {
                    new_reverser = 0;
                    new_power = 0;
                }



            }

            //CALL NEW FUNCTION TO SET THE REVERSER STATE
            if (new_reverser != stm_reverser)
            {
                stm_reverser = new_reverser;
                data.Handles.Reverser = stm_reverser;
            }
            else
            {
                data.Handles.Reverser = stm_reverser;
            }

            {
                //This section of code operates the pressure power drop
                int bp = Math.Max(stm_boilerpressure - (int)boilerminpressure, 0);
                int bp_range = (int)boilermaxpressure - (int)boilerminpressure;
                int pwr_limit = Math.Min(new_power, (int)((bp / (float)(bp_range) + (1.0 / this.Train.Specs.PowerNotches - 0.01)) * this.Train.Specs.PowerNotches));

                

                new_power = Math.Max(data.Handles.PowerNotch - this.Train.Specs.PowerNotches + pwr_limit, 0);

                //CALL NEW FUNCTION TO CHANGE POWER
            }


            if (Train.drastate != true)
            {
                if (new_power != stm_power)
                {
                    stm_power = new_power;
                    data.Handles.PowerNotch = stm_power;
                }
                else
                {
                    data.Handles.PowerNotch = stm_power;
                }
            }

            {
                //This section of code generates pressure and operates the blowoff

                //First elapse the main timer function
                this.maintimer += data.ElapsedTime.Seconds;

                //This section of code handles the fire simulator
                if (advancedfiring == false)
                {
                    //Advanced firing is not enabled, use the standard boiler water to steam rate
                    finalsteamrate = calculatedsteamrate;
                }
                else
                {
                    //Advanced firing
                    if (this.maintimer > 1)
                    {
                        //Check whether we can shovel coal
                        //Firemass must be below maximum and shovelling true [Non automatic]
                        //If automatic firing is on, only shovel coal if we are below 50% of max fire mass- Change???
                        //Use automatic behaviour if no shovelling key is set as obviously we can't shovel coal manually with no key
                        if (shovelling == true && firemass < maximumfiremass || automatic == true && firemass < (firemass / 2) && firemass < maximumfiremass
                            || String.IsNullOrEmpty(Train.tractionmanager.shovellingkey) && firemass < (firemass / 2) && firemass < maximumfiremass)
                        {
                            //Add the amount of coal shovelled per second to the fire mass & decrease it from the fire temperature
                            firemass += (int)shovellingrate;
                            firetemp -= (int)shovellingrate;
                        }
                        int fire_tempchange;
                        if (firemass != 0)
                        {
                            if (blowers == true)
                            {
                                fire_tempchange = (int)Math.Ceiling((double) (((firemass*0.5) - 10)/(firemass*0.05))*blowers_firefactor);
                            }
                            else
                            {
                                fire_tempchange = (int)Math.Ceiling((double)(((firemass * 0.5) - 10) / (firemass * 0.05)));
                            }
                        }
                        else
                        {
                            //Temperature change must be zero if our fire mass is zero
                            //Otherwise causes a division by zero error....
                            fire_tempchange = 0;
                        }
                        firemass = (int)((double)firemass * 0.9875);
                        if (firetemp < 1000)
                        {
                            //Add calculated temperature increase to the fire temperature
                            firetemp += fire_tempchange;
                        }
                        else
                        {
                            //Otherwise set to max
                            firetemp = 1000;
                        }
                        if (blowers == true)
                        {
                            finalsteamrate = (int) ((((double) calculatedsteamrate/1000)*firetemp)*blowers_pressurefactor);
                        }
                        else
                        {
                            finalsteamrate = (int)(((double)calculatedsteamrate / 1000) * firetemp);
                        }

                    }
                }
                if (this.maintimer > 1)
                {
                    if (blowers == true)
                    {
                        pressureup = (int) (((boilerwatertosteamrate/60)*maintimer)*blowers_pressurefactor);
                    }
                    else
                    {
                        pressureup = (int)((boilerwatertosteamrate / 60) * maintimer);
                    }
                    stm_boilerpressure = stm_boilerpressure + pressureup;
                    stm_boilerwater = stm_boilerwater - pressureup;
                    //Blowoff
                    if (stm_boilerpressure > blowoffpressure)
                    {
                        stm_boilerpressure = (int)boilermaxpressure;
                        if (blowoffsound != -1)
                        {
                            SoundManager.Play(blowoffsound, 2.0, 1.0, false);
                        }
                        blowofftriggered = true;

                    }
                    else if (stm_boilerpressure > boilermaxpressure)
                    {
                        stm_boilerpressure = stm_boilerpressure - 4;
                    }
                    
                }
                
            }

            if (automatic == true)
            {
                //This section of code operates the automatic injectors
                if (stm_boilerwater > boilermaxwaterlevel / 2 && stm_boilerpressure > boilermaxpressure / 4)
                {
                    stm_injector = false;
                    if (pressureuse > pressureup * 1.5)
                    {
                        //Turn on the blowers if we're using 50% more pressure than we're generating
                        blowers = true;
                    }
                    else
                    {
                        blowers = false;
                    }
                }
                else
                {
                    //Blowers shouldn't be on at the same time as the injectors
                    stm_injector = true;
                    blowers = false;
                }
            }

            //This section of code operates the injectors
            if (stm_injector == true && this.maintimer > 1)
            {
                if (stm_boilerpressure > 0 && stm_boilerwater < boilermaxwaterlevel)
                {
                    fuel = fuel - (int)injectorrate;
                    if (fuel <= 0)
                    {
                        fuel = 0;
                    }
                    stm_boilerwater = stm_boilerwater + (int)injectorrate;
                    stm_boilerpressure = stm_boilerpressure - ((int)injectorrate / 4);
                }
            }
		    
            //This section of code governs pressure usage
            if (stm_reverser != 0)
            {
                double regpruse = ((double)Train.Handles.PowerNotch / (double)this.Train.Specs.PowerNotches) * regulatorpressureuse;
                //32
                float cutprboost = Math.Abs((float)cutoff) / Math.Abs((float)cutoffmax);
                //1
                float spdpruse = 1 + Math.Abs((float)data.Vehicle.Speed.KilometersPerHour) / 25;
                if (maintimer > 1)
                {
                    pressureuse = (int)(regpruse * cutprboost * spdpruse);
                    stm_boilerpressure = stm_boilerpressure - pressureuse;
                }
            }
            //This section of code governs the pressure used by the horn
            if (klaxonpressureuse != -1 && (Train.tractionmanager.primaryklaxonplaying || Train.tractionmanager.secondaryklaxonplaying || Train.tractionmanager.musicklaxonplaying))
            {
                if (this.maintimer > 1)
                {
                    stm_boilerpressure = stm_boilerpressure - (int)klaxonpressureuse;
                }
            }
            //This section of code defines the pressure used by the train's steam heating system
		    if (steamheatpressureuse != -1 && steamheatlevel != 0)
		    {
		        if (maintimer > 1)
		        {
		            stm_boilerpressure -= (int)(steamheatlevel*steamheatpressureuse);
		        }
		    }
            //This section of code defines the pressure used by the cylinder cocks when open
		    if (cylindercocks == true && this.maintimer > 1)
		    {
		        stm_boilerpressure -= (int)(cylindercocks_basepressureuse + (cylindercocks_notchpressureuse*data.Handles.PowerNotch));
		    }
		    //This section of code fills our tanks from a water tower
            if (fuelling == true)
            {
                if (maintimer > 1)
                {
                    fuel += (int)fuelfillspeed;
                }
                if (fuel > fuelcapacity)
                {
                    fuel = (int)fuelcapacity;
                }
            }
            //Pass data to the debug window
            if (AdvancedDriving.CheckInst != null)
            {
                //Calculate total pressure usage figure
                int debugpressureuse = pressureuse;
                if (Train.tractionmanager.primaryklaxonplaying || Train.tractionmanager.secondaryklaxonplaying || Train.tractionmanager.musicklaxonplaying)
                {
                    debugpressureuse += (int)klaxonpressureuse;
                }
                if (steamheatpressureuse != -1 && steamheatlevel != 0)
                {
                    debugpressureuse += (int)(steamheatlevel*steamheatpressureuse);
                }
                if (cylindercocks == true)
                {
                    debugpressureuse += (int)(cylindercocks_basepressureuse + (cylindercocks_notchpressureuse * data.Handles.PowerNotch));
                }
                tractionmanager.debuginformation[1] = Convert.ToString(stm_boilerpressure);
                tractionmanager.debuginformation[2] = Convert.ToString(pressureup);
                tractionmanager.debuginformation[3] = Convert.ToString(debugpressureuse);
                tractionmanager.debuginformation[4] = Convert.ToString(cutoff);
                tractionmanager.debuginformation[5] = Convert.ToString(optimalcutoff);
                tractionmanager.debuginformation[6] = Convert.ToString(firemass);
                tractionmanager.debuginformation[7] = Convert.ToString(firetemp);
                tractionmanager.debuginformation[8] = Convert.ToString(stm_injector);
                tractionmanager.debuginformation[9] = Convert.ToString(blowers);
                tractionmanager.debuginformation[10] = Convert.ToString(stm_boilerwater) + " of " + Convert.ToString(boilermaxwaterlevel);
                tractionmanager.debuginformation[11] = Convert.ToString(fuel) + " of " + Convert.ToString(fuelcapacity);
                tractionmanager.debuginformation[12] = Convert.ToString(automatic);
                if (cylindercocks == true)
                {
                    tractionmanager.debuginformation[14] = "Open";    
                }
                else
                {
                    tractionmanager.debuginformation[14] = "Closed";    
                }

            }
            {
                //Set Panel Indicators
                if (cutoffindicator != -1)
                {
                    this.Train.Panel[(cutoffindicator)] = (int)cutoff;
                }
                if (boilerpressureindicator != -1)
                {
                    this.Train.Panel[(boilerpressureindicator)] = (int)stm_boilerpressure;
                }
                if (boilerwaterlevelindicator != -1)
                {
                    this.Train.Panel[(boilerwaterlevelindicator)] = (int)stm_boilerwater;
                }
                if (fuelindicator != -1)
                {
                    this.Train.Panel[(fuelindicator)] = (int)fuel;
                }
                if (injectorindicator != -1)
                {
                    if (stm_injector == true)
                    {
                        this.Train.Panel[(injectorindicator)] = 1;

                    }
                    else
                    {
                        this.Train.Panel[(injectorindicator)] = 0;

                    }
                }
                if (automaticindicator != -1)
                {
                    if (automatic == false)
                    {
                        this.Train.Panel[(automaticindicator)] = 0;
                    }
                    else
                    {
                        this.Train.Panel[(automaticindicator)] = 1;
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
                if (fuelfillindicator != -1)
                {
                    if (fuelling == true)
                    {
                        this.Train.Panel[(fuelfillindicator)] = 1;
                    }
                }
                if (blowoffindicator != -1)
                {
                    if (blowofftriggered == true)
                    {
                        this.Train.Panel[(blowoffindicator)] = 1;
                        //Run a 10 second timer for the blowoff index to display
                        this.blowofftimer += data.ElapsedTime.Seconds;
                        if (this.blowofftimer > 10)
                        {
                            this.blowofftriggered = false;
                            this.blowofftimer = 0;
                        }
                    }
                    else
                    {
                        this.Train.Panel[(blowoffindicator)] = 0;

                    }
                    if (steamheatindicator != -1)
                    {
                        this.Train.Panel[(steamheatindicator)] = steamheatlevel;
                    }
                }
            }
        {
            //Sounds
            if (this.injectorloopsound != -1)
            {
                if (stm_injector == true)
                {
                    if (injectorclank == false)
                    {
                        if (injectorclanksound != -1)
                        {
                            SoundManager.Play(injectorclanksound, 2.0, 1.0, false);
                        }
                        injectorclank = true;
                    }
                    else
                    {
                        if (!SoundManager.IsPlaying(injectorclanksound) || injectorclanksound == -1)
                        {
                            SoundManager.Play(injectorloopsound, 2.0, 1.0, true);
                        }
                    }
                    
                }
                else
                {
                    injectorclank = false;
                    if (SoundManager.IsPlaying(injectorloopsound))
                    {
                        SoundManager.Stop(injectorloopsound);
                        if (injectorclanksound != -1)
                        {
                            SoundManager.Play(injectorclanksound, 2.0, 1.0, false);
                        }
                    }
                }
            }
            if (blowersound != -1)
            {
                if (blowers == true && !SoundManager.IsPlaying(blowersound))
                {
                    SoundManager.Play(blowersound, 2.0, 1.0, true);
                }
                else if (blowers == false)
                {
                    SoundManager.Stop(blowersound);
                }
            }
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
            //Reset the main timer if it's over 1 second
		    if (this.maintimer > 1)
		    {
		        this.maintimer = 0.0;
		    }
        }
	}
}