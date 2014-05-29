using System;
using OpenBveApi.Runtime;


namespace Plugin {
	/// <summary>Represents a steam locomotive.</summary>
	internal class steam : Device {
		
		// --- members ---
		
		/// <summary>The underlying train.</summary>
		private Train Train;
		
		
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
        internal int stm_boilerpressure;
        internal int stm_boilerwater;
        internal double boilertimer;
        internal double draintimer;
        internal bool stm_injector;
        internal double injectortimer;
        internal int fuel;
        internal int stm_power;
        internal int cutoffstate;
        internal double cutofftimer;
        internal bool fuelling;
        internal double fuellingtimer;
        internal double klaxonpressuretimer;
        

		// --- constants ---
        /// <summary>Is our transmission automatic</summary>
        internal double automatic = -1;

		/// <summary>Do we heave a part that heats up?</summary>
		internal double heatingpart = 0;

        /// <summary>Overheat warning temperature</summary>
        internal double overheatwarn = 0;

        /// <summary>Overheat temperature</summary>
        internal double overheat = 0;

        /// <summary>What happens when we overheat?</summary>
        internal double overheatresult = 0;

        /// <summary>Panel indicator for thermometer</summary>
        internal double thermometer = -1;

        /// <summary>Panel indicator for overheat indicator</summary>
        internal double overheatindicator = -1;

        /// <summary>Sound index for overheat alarm</summary>
        internal double overheatalarm = -1;

        /// <summary>Default paramaters</summary>
        /// Used if no value is loaded from the config file
        internal double cutoffmax = 75;
        internal double cutoffmin = -55;
        internal double cutoffineffective = 15;
        internal double cutoffratiobase = 30;
        internal double cutoffratio = 10;
        internal double cutoffdeviation = 8;
        internal double boilermaxpressure = 20000;
        internal double boilerminpressure = 12000;
        internal double boilermaxwaterlevel = 1600;
        internal double boilerwatertosteamrate = 1500;
        internal double fuelstartamount = 20000;
        internal double injectorrate = 100;
        internal double regulatorpressureuse = 32;
        internal double cutoffchangespeed = 40;
        internal double cutoffchangetest = 1;
        internal double boilerstartwaterlevel = -1;
        internal double blowoffpressure = 21000;
        internal string heatingrate = "0";
        internal double fuelfillspeed = 50;
        internal double fuelcapacity = 20000;
        internal double fuelfillindicator = -1;
        internal double klaxonpressureuse = -1;

        //Panel Indicies
        internal double reverserindex = -1;
        internal double boilerpressureindicator = -1;
        internal double boilerwaterlevelindicator = -1;
        internal double cutoffindicator = -1;
        internal double fuelindicator = -1;
        internal double injectorindicator = -1;
        internal double automaticindicator = -1;

        //Sound Indicies
        internal double injectorsound = -1;
        internal double blowoffsound = -1;

        //Arrays
        int[] heatingarray;
		
		// --- constructors ---
		
		/// <summary>Creates a new instance of this system.</summary>
		/// <param name="train">The train.</param>
		internal steam(Train train) {
			this.Train = train;
            this.boilertimer = 0.0;
            this.draintimer = 0.0;
            this.cutofftimer = 0.0;

            
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
            this.boilertimer = 0.0;
            this.draintimer = 0.0;

            string[] splitheatingrate = heatingrate.Split(',');
            heatingarray = new int[splitheatingrate.Length];
            for (int i = 0; i < heatingarray.Length; i++)
            {
                heatingarray[i] = Int32.Parse(splitheatingrate[i]);
            }

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
                if (heatingpart == 0 | overheat == 0)
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

            //Automatic Cutoff
            if (automatic != -1)
            {


                if (Train.Handles.Reverser == 1 && cutoff >= cutoffineffective)
                {

                    cutoff = cutoffmax;
                }
                else if (Train.Handles.Reverser == -1 && cutoff <= -cutoffineffective)
                {

                    cutoff = cutoffmin;
                }
                else
                {

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
                        if (automatic != -1)
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
                        if (automatic != -1)
                        {
                            cutoff = (int)Math.Min(optimalcutoff, -cutoffineffective - 1);
                        }
                        else
                        {
                            new_power = Math.Max((int)(this.Train.Specs.PowerNotches - ((cutoffmin - cutoff) < 0 ? -((int)cutoffmin - cutoff) : ((int)cutoffmin - cutoff)) / cutoffdeviation), 0);
                        }
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
                int pwr_limit = Math.Min(new_power, (int)(bp / (float)(bp_range) + (1.0 / this.Train.Specs.PowerNotches - 0.01)) * this.Train.Specs.PowerNotches);

                

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
                this.boilertimer += data.ElapsedTime.Seconds;
                if (this.boilertimer > 1)
                {
                    int pressureup = ((int)boilerwatertosteamrate / 60) * (int)boilertimer;
                    stm_boilerpressure = stm_boilerpressure + pressureup;
                    stm_boilerwater = stm_boilerwater - pressureup;
                    //Blowoff
                    if (stm_boilerpressure > blowoffpressure)
                    {
                        stm_boilerpressure = (int)boilermaxpressure;
                        if (blowoffsound != -1)
                        {
                            SoundManager.Play((int)blowoffsound, 1.0, 1.0, false);
                        }
                        
                    }
                    else if (stm_boilerpressure > boilermaxpressure)
                    {
                        stm_boilerpressure = stm_boilerpressure - 4;
                    }
                    
                    
                    boilertimer = 0.0;
                }
                
            }

            if (automatic != -1)
            {
                //This section of code operates the automatic injectors
                if (stm_boilerwater > boilermaxwaterlevel / 2 && stm_boilerpressure > boilermaxpressure / 4)
                {
                    stm_injector = false;
                }
                else
                {
                    stm_injector = true;
                }
            }

            //This section of code operates the injectors
            if (stm_injector == true)
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
                
                this.draintimer += data.ElapsedTime.Seconds;
                int regpruse = (int)Train.Handles.PowerNotch / this.Train.Specs.PowerNotches * (int)regulatorpressureuse;
                //32
                float cutprboost = Math.Abs((float)cutoff) / Math.Abs((float)cutoffmax);
                //1
                float spdpruse = 1 + Math.Abs((float)data.Vehicle.Speed.KilometersPerHour) / 25;

                if (draintimer > 1)
                {
                    stm_boilerpressure = stm_boilerpressure - ((int)(regpruse * cutprboost * spdpruse));
                    draintimer = 0.0;
                }
            }
            //This section of code governs the pressure used by the horn
            if (klaxonpressureuse != -1 && (Train.tractionmanager.primaryklaxonplaying || Train.tractionmanager.secondaryklaxonplaying || Train.tractionmanager.musicklaxonplaying))
            {
                this.klaxonpressuretimer += data.ElapsedTime.Seconds;
                if (klaxonpressuretimer > 0.5)
                {
                    klaxonpressuretimer = 0.0;
                    stm_boilerpressure = stm_boilerpressure - (int)(klaxonpressureuse / 2);
                }
            }
            //This section of code fills our tanks from a water tower
            if (fuelling == true)
            {
                fuellingtimer += data.ElapsedTime.Milliseconds;
                if (fuellingtimer > 1000)
                {
                    fuellingtimer = 0.0;
                    fuel += (int)fuelfillspeed;
                }
                if (fuel > fuelcapacity)
                {
                    fuel = (int)fuelcapacity;
                }
            }
            {
                //Set Panel Indicators
                if (cutoffindicator != -1)
                {
                    this.Train.Panel[(int)(cutoffindicator)] = (int)cutoff;
                }
                if (boilerpressureindicator != -1)
                {
                    this.Train.Panel[(int)(boilerpressureindicator)] = (int)stm_boilerpressure;
                }
                if (boilerwaterlevelindicator != -1)
                {
                    this.Train.Panel[(int)(boilerwaterlevelindicator)] = (int)stm_boilerwater;
                }
                if (fuelindicator != -1)
                {
                    this.Train.Panel[(int)(fuelindicator)] = (int)fuel;
                }
                if (injectorindicator != -1)
                {
                    if (stm_injector == true)
                    {
                        this.Train.Panel[(int)(injectorindicator)] = 1;

                    }
                    else
                    {
                        this.Train.Panel[(int)(injectorindicator)] = 0;

                    }
                }
                if (automaticindicator != -1)
                {
                    if (automatic == -1)
                    {
                        this.Train.Panel[(int)(automaticindicator)] = 0;
                    }
                    else
                    {
                        this.Train.Panel[(int)(automaticindicator)] = 1;
                    }
                }
                if (thermometer != -1)
                {
                    this.Train.Panel[(int)(thermometer)] = (int)temperature;
                }
                if (overheatindicator != -1)
                {
                    if (temperature > overheatwarn)
                    {
                        this.Train.Panel[(int)(overheatindicator)] = 1;
                    }
                    else
                    {
                        this.Train.Panel[(int)(overheatindicator)] = 0;
                    }
                }
                if (fuelfillindicator != -1)
                {
                    if (fuelling == true)
                    {
                        this.Train.Panel[(int)(fuelfillindicator)] = 1;
                    }
                }
            }
        {
            //Sounds
            if (this.injectorsound != -1)
            {
                if (stm_injector == true)
                {
                    SoundManager.Play((int)injectorsound, 1.0, 1.0, true);
                }
                else
                {
                    SoundManager.Stop((int)injectorsound);
                }
            }
            if (overheatalarm != -1)
            {
                if (temperature > overheatalarm)
                {
                    SoundManager.Play((int)overheatalarm, 1.0, 1.0, true);
                }
                else
                {
                    SoundManager.Stop((int)overheatalarm);
                }
            }
        }

        }
	}
}