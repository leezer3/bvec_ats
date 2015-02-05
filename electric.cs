using System;
using OpenBveApi.Runtime;



namespace Plugin
{
    /// <summary>Represents an electric locomotive.</summary>
    internal partial class electric : Device
    {

        // --- members ---

        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        //Internal Variables
        internal double heatingtimer;
        internal double currentheat;
        internal double temperature;
        /// <summary>Stores whether we are currently in a power gap</summary>
        internal bool powergap;
        /// <summary>Stores the current state of the ACB/VCB</summary>
        internal bool breakertripped;
        /// <summary>Stores whether the power was cutoff by a legacy OS_ATS standard beacon</summary>
        internal bool legacypowercut;
        internal int nextmagnet;
        internal int firstmagnet;
        internal int lastmagnet;
        internal bool pantographraised_f;
        internal bool pantographraised_r;
        internal double linevoltstimer;
        internal double pantographcooldowntimer_r;
        internal double pantographcooldowntimer_f;
        internal double powerlooptimer;
        internal bool powerloop;
        internal double breakerlooptimer;

        /// <summary>The current state of the front pantograph</summary>
        internal PantographStates FrontPantographState;
        /// <summary>The current state of the rear pantograph</summary>
        internal PantographStates RearPantographState;

        //Default Variables

        /// <summary>The ammeter value for each power notch</summary>
        internal string ammetervalues = "0";
        /// <summary>A list of the available power pickup points</summary>
        internal string pickuppoints = "0";
        /// <summary>The behaviour of the train in a powergap</summary>
        internal int powergapbehaviour = 0;
        /// <summary>A list of the heating rates (in heat units per second) for each power notch</summary>
        internal string heatingrate = "0";
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

        //Panel Indicies

        /// <summary>The panel index of the ammeter</summary>
        internal int ammeter = -1;
        /// <summary>The panel index of the line volts indicator</summary>
        internal int powerindicator = -1;
        /// <summary>The panel index of the ACB/VCB</summary>
        internal int breakerindicator = -1;
        /// <summary>The panel index of front pantograph</summary>
        internal int pantographindicator_f = -1;
        /// <summary>The panel index of the rear pantograph</summary>
        internal int pantographindicator_r = -1;
        /// <summary>The panel indicator for the thermometer</summary>
        internal int thermometer = -1;
        /// <summary>The panel indicator for the overheat indicator</summary>
        internal int overheatindicator = -1;

        //Sound Indicies

        /// <summary>The sound index played when the ACB/VCB is closed</summary>
        internal int breakersound = -1;
        /// <summary>The sound index played when a pantograph is raised</summary>
        internal int pantographraisedsound = -1;
        /// <summary>The sound index played when a pantograph is lowered</summary>
        internal int pantographloweredsound = -1;
        /// <summary>The alarm sound index played when a pantograph is lowered with the ACB/VCB closed</summary>
        internal int pantographalarmsound = -1;
        /// <summary>The power notch loop sound index</summary>
        internal int powerloopsound = -1;
        /// <summary>The breaker loop sound index</summary>
        internal int breakerloopsound = -1;
        /// <summary>Sound index for overheat alarm</summary>
        internal int overheatalarm = -1;

        //Arrays
        /// <summary>An array storing the ammeter values for each power notch</summary>
        int[] ammeterarray;
        /// <summary>An array storing the location of all available pickup points</summary>
        int[] pickuparray;
        /// <summary>An array storing the heating rate for each power notch</summary>
        int[] heatingarray;


        /// <summary>Gets the current state of a pantograph.</summary>
        internal PantographStates PantographState
        {
            get { return this.FrontPantographState; }
        }

        // --- constructors ---

        /// <summary>Creates a new instance of this system.</summary>
        /// <param name="train">The train.</param>
        internal electric(Train train)
        {
            this.Train = train;
        }

        //<param name="mode">The initialization mode.</param>
        internal override void Initialize(InitializationModes mode)
        {
            //Use try/ catch to handle exceptions in our array parsing
            try
            {
                //Split ammeter values into an array
                string[] splitammetervalues = ammetervalues.Split(',');
                ammeterarray = new int[splitammetervalues.Length];
                for (int i = 0; i < ammeterarray.Length; i++)
                {
                    ammeterarray[i] = Int32.Parse(splitammetervalues[i]);
                }
            }
            catch
            {
                InternalFunctions.LogError("ammetervalues");
            }
            try
            {
                //Split pickup location values into an array
                string[] splitpickups = pickuppoints.Split(',');
                pickuparray = new int[splitpickups.Length];
                for (int i = 0; i < pickuparray.Length; i++)
                {
                    pickuparray[i] = Int32.Parse(splitpickups[i]);
                }
            }
            catch
            {
                InternalFunctions.LogError("pickupoints");
            }
            try
            {
                //Split Heating Values into an array
                string[] splitheatingrate = heatingrate.Split(',');
                heatingarray = new int[splitheatingrate.Length];
                for (int i = 0; i < heatingarray.Length; i++)
                {
                    heatingarray[i] = Int32.Parse(splitheatingrate[i]);
                }
            }
            catch
            {
                InternalFunctions.LogError("heatingrate");
            }
            //Set starting pantograph states
            //If neither pantograph has a key assigned, set both to enabled
            if (String.IsNullOrEmpty(Train.tractionmanager.frontpantographkey) && String.IsNullOrEmpty(Train.tractionmanager.rearpantographkey))
                {
                    breakertripped = false;
                    pantographraised_f = true;
                    FrontPantographState = PantographStates.OnService;
                    pantographraised_r = true;
                    RearPantographState = PantographStates.OnService;
                }
            //On service- Set the enabled pantograph(s) to the OnService state
            //Set the ACB/ VCB to closed
            else if (mode == InitializationModes.OnService)
            {
                breakertripped = false;
                if (String.IsNullOrEmpty(Train.tractionmanager.frontpantographkey) && !String.IsNullOrEmpty(Train.tractionmanager.rearpantographkey))
                {
                    //Rear pantograph only is enabled
                    pantographraised_f = false;
                    FrontPantographState = PantographStates.Disabled;
                    pantographraised_r = true;
                    RearPantographState = PantographStates.OnService;
                }
                else
                {
                    //Front pantograph only is enabled
                    pantographraised_f = true;
                    FrontPantographState = PantographStates.OnService;
                    pantographraised_r = false;
                    RearPantographState = PantographStates.Disabled;
                }
            }
            //Not on service- Set the enabled pantograph(s) to the lowered state
            //Set the ACB/ VCB to open
            else
            {
                breakertripped = true;
                if (String.IsNullOrEmpty(Train.tractionmanager.frontpantographkey) && !String.IsNullOrEmpty(Train.tractionmanager.rearpantographkey))
                {
                    //Rear pantograph only is enabled
                    pantographraised_f = false;
                    FrontPantographState = PantographStates.Disabled;
                    pantographraised_r = false;
                    RearPantographState = PantographStates.Lowered;
                }
                else
                {
                    //Front pantograph only is enabled
                    pantographraised_f = false;
                    FrontPantographState = PantographStates.Lowered;
                    pantographraised_r = false;
                    RearPantographState = PantographStates.Disabled;
                }
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
                    //Heats based upon RPM- Not on an electric loco!
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
                    if (breakertripped == false && ((FrontPantographState == PantographStates.Disabled && RearPantographState == PantographStates.OnService) || (RearPantographState == PantographStates.Disabled && FrontPantographState == PantographStates.OnService)))
                    {
                        tractionmanager.resetpowercutoff();
                    }
                    tractionmanager.overheated = false;
                }
                else if (temperature < 0)
                {
                    temperature = 0;
                }
            }

            {
                
                //If we're in a power gap, check whether we have a pickup available
                if (powergap)
                {
                    int new_power;
                    //First check to see whether the first pickup is in the neutral section
                    if (Train.trainlocation - pickuparray[0] > firstmagnet && Train.trainlocation - pickuparray[0] < nextmagnet)
                    {
                        //Cycle through the other pickups
                        int j = 0;
                        foreach (int t in pickuparray)
                        {
                            if (Train.trainlocation - t < firstmagnet)
                            {
                                j++;
                            }
                        }
                        if (powergapbehaviour == 0)
                        {
                            //Power gaps have no effect, twiddle
                        }
                        else if (powergapbehaviour == 1)
                        {
                            //Reduce max throttle by percentage of how many pickups are in the gap
                            double throttlemultiplier = (double)j / (double)pickuparray.Length;
                            new_power = (int)(this.Train.Specs.PowerNotches * throttlemultiplier);
                            data.Handles.PowerNotch = new_power;
                        }
                        else if (powergapbehaviour == 2)
                        {
                            //Kill traction power if any pickup is on the gap
                            if (j != 0)
                            {
                                tractionmanager.demandpowercutoff();
                            }
                        }
                        else
                        {
                            //Kill traction power when all pickups are on the gap
                            if (j == pickuparray.Length)
                            {
                                tractionmanager.demandpowercutoff();
                            }
                        }
                    }
                    //Now, check to see if the last pickup is in the neutral section
                    else if (Train.trainlocation - pickuparray[pickuparray.Length - 1] > firstmagnet && Train.trainlocation - pickuparray[pickuparray.Length - 1] < nextmagnet)
                    {
                        //Cycle through the other pickups
                        int j = 0;
                        foreach (int t in pickuparray)
                        {
                            if (Train.trainlocation - t < firstmagnet)
                            {
                                j++;
                            }
                        }
                        if (powergapbehaviour == 0)
                        {
                            //Power gaps have no effect, twiddle
                        }
                        else if (powergapbehaviour == 1)
                        {
                            //Reduce max throttle by percentage of how many pickups are in the gap
                            double throttlemultiplier = (double)j / (double)pickuparray.Length;
                            new_power = (int)(this.Train.Specs.PowerNotches * throttlemultiplier);
                            data.Handles.PowerNotch = new_power;
                        }
                        else if (powergapbehaviour == 2)
                        {
                            //Kill traction power if any pickup is on the gap
                            if (j != 0)
                            {
                                tractionmanager.demandpowercutoff();
                            }
                        }
                        else
                        {
                            //Kill traction power when all pickups are on the gap
                            if (j == pickuparray.Length)
                            {
                                tractionmanager.demandpowercutoff();
                            }
                        }
                    }
                    //Neither the first or last pickups are in the power gap, reset the power
                    //However also check that the breaker has not been tripped by a UKTrainSys beacon
                    else if (nextmagnet != 0 && (Train.trainlocation - pickuparray[pickuparray.Length - 1]) > nextmagnet && (Train.trainlocation - pickuparray[0]) > nextmagnet)
                    {
                        powergap = false;
                        if (legacypowercut == true)
                        {
                            //Reset legacy power cutoff state and retrip breaker
                            Train.electric.breakertrip();
                            legacypowercut = false;
                        }
                        if (breakertripped == false && ((FrontPantographState == PantographStates.Disabled && RearPantographState == PantographStates.OnService) || (RearPantographState == PantographStates.Disabled && FrontPantographState == PantographStates.OnService)))
                        {
                            tractionmanager.resetpowercutoff();
                        }
                    }
                    //If the final pickup has passed the UKTrainSys standard power gap location
                    else if (Train.trainlocation - pickuparray[pickuparray.Length - 1] < lastmagnet)
                    {
                        powergap = false;
                    }
                }
                //This section of code handles a UKTrainSys compatible ACB/VCB
                //
                //If the ACB/VCB has tripped, always demand power cutoff
                if (breakertripped == true)
                {
                    tractionmanager.demandpowercutoff();
                }
                //If we're in a power gap, also always demand power cutoff
                else if (breakertripped == false && powergap == true)
                {
                    tractionmanager.demandpowercutoff();
                }
                //If the ACB/VCB has now been reset with a pantograph available & we're not in a powergap reset traction power
                if (breakertripped == false && powergap == false && ((FrontPantographState == PantographStates.OnService || RearPantographState == PantographStates.OnService)))
                {
                    tractionmanager.resetpowercutoff();
                }
            }
            {
                //This section of code handles raising the pantographs and the alarm state
                //
                //If both pantographs are lowered or disabled, then there are no line volts
                if ((FrontPantographState == PantographStates.Lowered || FrontPantographState == PantographStates.Disabled) && 
                (RearPantographState == PantographStates.Lowered || RearPantographState == PantographStates.Disabled))
                {
                    powergap = true;
                    tractionmanager.demandpowercutoff();
                }
                //If the powergap behaviour cuts power when *any* pantograph is disabled / lowered
                //
                //Line volts is lit, but power is still cut off
                else if (FrontPantographState != PantographStates.Disabled && RearPantographState != PantographStates.Disabled
                && (FrontPantographState != PantographStates.OnService || RearPantographState != PantographStates.OnService) && powergapbehaviour == 2)
                {
                    tractionmanager.demandpowercutoff();
                }
                

                //Now handle each of our pantographs
                //The front pantograph has been raised, and the line volts indicator has lit, but the timer is active
                //Power cutoff is still in force
                if (FrontPantographState == PantographStates.RaisedTimer && RearPantographState != PantographStates.OnService)
                {
                    pantographraised_f = true;
                    powergap = false;
                    tractionmanager.demandpowercutoff();
                    linevoltstimer += data.ElapsedTime.Milliseconds;
                    if (linevoltstimer > 1000)
                    {
                        FrontPantographState = PantographStates.VCBReady;
                        linevoltstimer = 0.0;
                    }
                }
                //The timer has now expired and when the ACB/ VCB is toggled, the pantograph is on service
                else if (FrontPantographState == PantographStates.VCBReady)
                {
                    if (breakertripped == false)
                    {
                        FrontPantographState = PantographStates.OnService;
                    }
                }
                //The front pantograph has been raised with the ACB/ VCB closed
                else if (FrontPantographState == PantographStates.RaisedVCBClosed)
                {
                    breakertrip();
                    FrontPantographState = PantographStates.VCBResetTimer;
                }
                //The pantograph VCB reset timer is running
                else if (FrontPantographState == PantographStates.VCBResetTimer)
                {
                    pantographcooldowntimer_f += data.ElapsedTime.Milliseconds;
                    if (pantographcooldowntimer_f > pantographretryinterval)
                    {
                        FrontPantographState = PantographStates.Lowered;
                        pantographcooldowntimer_f = 0.0;
                    }
                }
                //An attempt has been made to raise or lower a pantograph with the train in motion
                else if (FrontPantographState == PantographStates.LoweredAtSpeed)
                {
                    pantographraised_f = false;
                    if (pantographalarmbehaviour == 0)
                    {
                        //Just lower the pantograph
                        RearPantographState = PantographStates.Lowered;
                    }
                    else if (pantographalarmbehaviour == 1)
                    {
                        //Trip the ACB/VCB and lower the pantograph
                        breakertripped = true;
                        FrontPantographState = PantographStates.Lowered;
                    }
                    else if (pantographalarmbehaviour == 2)
                    {
                        //Apply brakes and trip the ACB/VCB
                        breakertrip();
                        Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);
                        if (pantographalarmsound != -1)
                        {
                            SoundManager.Play(pantographalarmsound, 1.0, 1.0, true);
                        }
                        FrontPantographState = PantographStates.LoweredAtspeedBraking;
                    }
                }
                else if (FrontPantographState == PantographStates.LoweredAtspeedBraking)
                {
                    if (Train.trainspeed == 0)
                    {
                        pantographcooldowntimer_f += data.ElapsedTime.Milliseconds;
                        if (pantographcooldowntimer_f > pantographretryinterval)
                        {
                            RearPantographState = PantographStates.Lowered;
                            pantographcooldowntimer_f = 0.0;
                            if (pantographalarmsound != -1)
                            {
                                SoundManager.Stop(pantographalarmsound);
                            }
                        }
                    }
                }

                //The rear pantograph has been raised, and the line volts indicator has lit, but the timer is active
                //Power cutoff is still in force
                if (RearPantographState == PantographStates.RaisedTimer && FrontPantographState != PantographStates.OnService)
                {
                    pantographraised_r = true;
                    powergap = false;
                    tractionmanager.demandpowercutoff();
                    linevoltstimer += data.ElapsedTime.Milliseconds;
                    if (linevoltstimer > 1000)
                    {
                        RearPantographState = PantographStates.VCBReady;
                        linevoltstimer = 0.0;
                    }
                }
                //The timer has now expired and when the ACB/ VCB is toggled, the pantograph is on service
                else if (RearPantographState == PantographStates.VCBReady)
                {
                    if (breakertripped == false)
                    {
                        RearPantographState = PantographStates.OnService;
                    }
                }
                //The rear pantograph has been raised with the ACB/ VCB closed
                else if (RearPantographState == PantographStates.RaisedVCBClosed)
                {
                    breakertrip();
                    RearPantographState = PantographStates.VCBResetTimer;
                }
                //The pantograph VCB reset timer is running
                else if (RearPantographState == PantographStates.VCBResetTimer)
                {
                    pantographcooldowntimer_r += data.ElapsedTime.Milliseconds;
                    if (pantographcooldowntimer_r > pantographretryinterval)
                    {
                        RearPantographState = PantographStates.Lowered;
                        pantographcooldowntimer_r = 0.0;
                    }
                }
                //An attempt has been made to raise or lower a pantograph with the train in motion
                else if (RearPantographState == PantographStates.LoweredAtSpeed)
                {
                    pantographraised_r = false;
                    if (pantographalarmbehaviour == 0)
                    {
                        //Just lower the pantograph
                        RearPantographState = PantographStates.Lowered;
                    }
                    else if (pantographalarmbehaviour == 1)
                    {
                        //Trip the ACB/VCB and lower the pantograph
                        breakertripped = true;
                        RearPantographState = PantographStates.Lowered;
                    }
                    else if (pantographalarmbehaviour == 2)
                    {
                        //Apply brakes and trip the ACB/VCB
                        breakertrip();
                        Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches + 1);
                        if (pantographalarmsound != -1)
                        {
                            SoundManager.Play(pantographalarmsound, 1.0, 1.0, true);
                        }
                        RearPantographState = PantographStates.LoweredAtspeedBraking;
                    }
                }
                else if (RearPantographState == PantographStates.LoweredAtspeedBraking)
                {
                    if (Train.trainspeed == 0)
                    {
                        pantographcooldowntimer_r += data.ElapsedTime.Milliseconds;
                        if (pantographcooldowntimer_r > pantographretryinterval)
                        {
                            RearPantographState = PantographStates.Lowered;
                            pantographcooldowntimer_r = 0.0;
                            if (pantographalarmsound != -1)
                            {
                                SoundManager.Stop(pantographalarmsound);
                            }
                        }
                    }
                }
            }
            //This section of code runs the power notch loop sound
            if (powerloopsound != -1 && data.Handles.PowerNotch != 0)
            {
                if (breakertripped == false)
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
            else if (powerloopsound != -1 && data.Handles.PowerNotch == 0)
            {
                SoundManager.Stop(powerloopsound);
            }
            //This section of code runs the breaker loop sound
            if (breakerloopsound != -1 && breakertripped == false)
            {
                if (!powergap && SoundManager.IsPlaying(breakerloopsound) == false)
                {
                    breakerlooptimer += data.ElapsedTime.Milliseconds;
                    if (breakerlooptimer > breakerlooptime)
                    {
                        SoundManager.Play(breakerloopsound, 1.0, 1.0, true);
                        breakerlooptimer = 0.0;
                    }
                }
            }
            else if (breakerloopsound != -1 && breakertripped == true)
            {
                SoundManager.Stop(breakerloopsound);
                breakerlooptimer = 0.0;
            }

            //Panel Indicies
            {
                //Ammeter
                if (ammeter != -1)
                {
                    int ammeterlength = ammeterarray.Length;
                    if (Train.Handles.Reverser == 0 || data.Handles.BrakeNotch != 0 || Train.Handles.PowerNotch == 0)
                    {
                        this.Train.Panel[ammeter] = 0;
                    }
                    else if (Train.Handles.PowerNotch != 0 && (powergap == true || breakertripped == true || tractionmanager.powercutoffdemanded == true))
                    {
                        this.Train.Panel[ammeter] = 0;
                    }
                    else if (Train.Handles.PowerNotch != 0 && Train.Handles.PowerNotch <= ammeterlength)
                    {

                        this.Train.Panel[ammeter] = ammeterarray[(Train.Handles.PowerNotch - 1)];
                    }
                    else
                    {
                        this.Train.Panel[ammeter] = ammeterarray[(ammeterlength - 1)];
                    }
                }
                //Line Volts indicator
                if (powerindicator != -1)
                {
                    if (!powergap)
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
                    if (!breakertripped)
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
                if (pantographindicator_f != -1)
                {
                    if (pantographraised_f == true)
                    {
                        this.Train.Panel[(pantographindicator_f)] = 1;
                    }
                    else
                    {
                        this.Train.Panel[(pantographindicator_f)] = 0;
                    }
                }
                if (pantographindicator_r != -1)
                {
                    if (pantographraised_r == true)
                    {
                        this.Train.Panel[(pantographindicator_r)] = 1;
                    }
                    else
                    {
                        this.Train.Panel[(pantographindicator_r)] = 0;
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
                tractionmanager.debuginformation[14] = Convert.ToString(FrontPantographState);
                tractionmanager.debuginformation[15] = Convert.ToString(RearPantographState);
                tractionmanager.debuginformation[16] = Convert.ToString(!breakertripped);
                tractionmanager.debuginformation[17] = Convert.ToString(!powergap);
            }
            
        }

        //This function handles legacy power magnets- Set the electric powergap to true & set the end magnet location
        internal void legacypowercutoff(int trainlocation,int magnetdistance)
        {
            if (!Train.electric.powergap)
            {
                Train.electric.powergap = true;
                firstmagnet = trainlocation;
                nextmagnet = trainlocation + magnetdistance;
                legacypowercut = true;
                Train.electric.breakertrip();
            }
        }

        //This function handles UKTrainSYS compatible dual magnets
        //Set the next magnet to false
        internal void newpowercutoff(int trainlocation)
        {
            if (!Train.electric.powergap)
            {
                Train.electric.powergap = true;
                firstmagnet = trainlocation;
                nextmagnet = 0;
            }
            else
            {
                Train.electric.powergap = false;
                lastmagnet = trainlocation;
            }
        }

        //Toggle the ACB/VCB based upon it's state
        internal void breakertrip()
        {
            if (!breakertripped)
            {
                
                breakertripped = true;
                powerloop = false;
                breakerplay();
                tractionmanager.demandpowercutoff();
            }
            else
            {
                
                breakertripped = false;
                breakerplay();
            }
        }
        internal void breakerplay()
        {
            if (breakersound != -1)
            {
                SoundManager.Play(breakersound, 1.0, 1.0, false);
            }
        }
        //Raises & lowers the pantographs
        internal void pantographtoggle(int pantograph)
        {
            if (pantograph == 0)
            {
                //Front pantograph
                if (pantographraised_f == false && breakertripped == true)
                {
                    if (pantographraisedsound != -1)
                    {
                        SoundManager.Play(pantographraisedsound, 1.0, 1.0, false);
                    }
                    //We can raise the pantograph, so start the line volts timer
                    this.FrontPantographState = PantographStates.RaisedTimer;
                }
                else if (pantographraised_f == false && breakertripped == false)
                {
                    //We can't raise the pantograph as the ACB/ VCB is closed, so start the cooldown timer
                    this.FrontPantographState = PantographStates.RaisedVCBClosed;
                }
                else if (pantographraised_f == true)
                {
                    if (pantographloweredsound != -1)
                    {
                        SoundManager.Play(pantographloweredsound, 1.0, 1.0, false);
                    }
                    //Lower the pantograph
                    if (Train.trainspeed == 0)
                    {
                        this.FrontPantographState = PantographStates.Lowered;
                        pantographraised_f = false;
                    }
                    else
                    {
                        this.FrontPantographState = PantographStates.LoweredAtSpeed;
                    }
                }
            }
            else
            {
                //Rear pantograph
                if (pantographraised_r == false && breakertripped == true)
                {
                    if (pantographraisedsound != -1)
                    {
                        SoundManager.Play(pantographraisedsound, 1.0, 1.0, false);
                    }
                    //We can raise the pantograph, so start the line volts timer
                    this.RearPantographState = PantographStates.RaisedTimer;
                }
                else if (pantographraised_r == false && breakertripped == false)
                {
                    //We can't raise the pantograph as the ACB/ VCB is closed, so start the cooldown timer
                    this.RearPantographState = PantographStates.RaisedVCBClosed;
                }
                else if (pantographraised_r == true)
                {
                    if (pantographloweredsound != -1)
                    {
                        SoundManager.Play(pantographloweredsound, 1.0, 1.0, false);
                    }
                    //Lower the pantograph
                    if (Train.trainspeed == 0)
                    {
                        this.RearPantographState = PantographStates.Lowered;
                        pantographraised_r = false;
                    }
                    else
                    {
                        this.RearPantographState = PantographStates.LoweredAtSpeed;
                    }
                }
            }
        }
    }
}