using System;
using System.Collections;
using OpenBveApi.Runtime;



namespace Plugin
{
    /// <summary>Represents an electric locomotive.</summary>
    internal partial class electric : Device
    {

        // --- members ---

        /// <summary>The underlying train.</summary>
        private Train Train;

        //Internal Variables
        internal double heatingtimer;
        internal double currentheat;
        internal double temperature;
        internal static bool powergap;
        internal static bool breakertripped;
        internal int nextmagnet;
        internal int firstmagnet;
        internal int lastmagnet;
        internal bool pantographraised_f;
        internal bool pantographraised_r;
        internal double linevoltstimer;
        internal double pantographcooldowntimer_r;
        internal double pantographcooldowntimer_f;

        private PantographStates FrontPantographState;
        private PantographStates RearPantographState;

        //Default Variables
        internal double ammeter = -1;
        internal string ammetervalues = "0";
        internal string pickuppoints = "0";
        internal double powergapbehaviour = 0;
        internal double recieverlocation = 0;
        internal string heatingrate = "0";
        internal double pantographretryinterval = 5000;
        internal double pantographalarmbehaviour = 0;
        
        //Panel Indicies
        internal double powerindicator = -1;
        internal double breakerindicator = -1;
        internal double pantographindicator_f = -1;
        internal double pantographindicator_r = -1;

        //Sound Indicies
        internal static double breakersound = -1;
        internal double pantographsound = -1;
        internal double pantographalarmsound = -1;

        //Arrays
        int[] ammeterarray;
        int[] pickuparray;
        int[] heatingarray;



        // --- constants ---
        /// <summary>Is our transmission automatic</summary>
        internal double automatic = -1;

        /// <summary>Do we heave a part that heats up?</summary>
        internal double heatingpart = 0;

        /// <summary>Overheat warning temperature<tTsummary>
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

        //Panel Indicies
        internal double automaticindicator = -1;


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
            //Split ammeter values into an array
            string[] splitammetervalues = ammetervalues.Split(',');
            ammeterarray = new int[splitammetervalues.Length];
            for (int i = 0; i < ammeterarray.Length; i++)
            {
                ammeterarray[i] = Int32.Parse(splitammetervalues[i]);
            }

            //Split pickup location values into an array
            string[] splitpickups = pickuppoints.Split(',');
            pickuparray = new int[splitpickups.Length];
            for (int i = 0; i < pickuparray.Length; i++)
            {
                pickuparray[i] = Int32.Parse(splitpickups[i]);
            }

            //Split Heating Values into an array
            string[] splitheatingrate = heatingrate.Split(',');
            heatingarray = new int[splitheatingrate.Length];
            for (int i = 0; i < heatingarray.Length; i++)
            {
                heatingarray[i] = Int32.Parse(splitheatingrate[i]);
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

            int new_power = 0;
            {
                //If we're in a power gap, check whether we have a pickup available
                if (powergap)
                {

                    //First check to see whether the first pickup is in the neutral section
                    if (Train.trainlocation - pickuparray[0] > firstmagnet && Train.trainlocation - pickuparray[0] < nextmagnet)
                    {
                        //Cycle through the other pickups
                        int j = 0;
                        for (int i = 0; i < pickuparray.Length; i++)
                        {
                            if (Train.trainlocation - pickuparray[i] < firstmagnet)
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
                        for (int i = 0; i < pickuparray.Length; i++)
                        {
                            if (Train.trainlocation - pickuparray[i] < firstmagnet)
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
                        tractionmanager.demandbrakeapplication();
                        if (pantographalarmsound != -1)
                        {
                            SoundManager.Play((int)pantographalarmsound, 1.0, 1.0, true);
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
                                SoundManager.Stop((int)pantographalarmsound);
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
                        tractionmanager.demandbrakeapplication();
                        if (pantographalarmsound != -1)
                        {
                            SoundManager.Play((int)pantographalarmsound, 1.0, 1.0, true);
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
                                SoundManager.Stop((int)pantographalarmsound);
                            }
                        }
                    }
                }
            }


            //Panel Indicies
            {
                //Ammeter
                if (ammeter != -1)
                {
                    int ammeterlength = ammeterarray.Length;
                    if (Train.Handles.Reverser == 0 || data.Handles.BrakeNotch != 0 || Train.Handles.PowerNotch == 0)
                    {
                        this.Train.Panel[(int)ammeter] = 0;
                    }

                    else if (Train.Handles.PowerNotch != 0 && Train.Handles.PowerNotch <= ammeterlength)
                    {
                        this.Train.Panel[(int)ammeter] = ammeterarray[(Train.Handles.PowerNotch - 1)];
                    }
                    else
                    {
                        this.Train.Panel[(int)ammeter] = ammeterarray[(ammeterlength - 1)];
                    }
                }
                //Line Volts indicator
                if (powerindicator != -1)
                {
                    if (!powergap)
                    {
                        this.Train.Panel[(int)powerindicator] = 1;
                    }
                    else
                    {
                        this.Train.Panel[(int)powerindicator] = 0;
                    }
                }
                //ACB/VCB Breaker Indicator
                if (breakerindicator != -1)
                {
                    if (!breakertripped)
                    {
                        this.Train.Panel[(int)breakerindicator] = 1;
                    }
                    else
                    {
                        this.Train.Panel[(int)breakerindicator] = 0;
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
                //Pantograph Indicators
                if (pantographindicator_f != -1)
                {
                    if (pantographraised_f == true)
                    {
                        this.Train.Panel[(int)(pantographindicator_f)] = 1;
                    }
                    else
                    {
                        this.Train.Panel[(int)(pantographindicator_f)] = 0;
                    }
                }
                if (pantographindicator_r != -1)
                {
                    if (pantographraised_r == true)
                    {
                        this.Train.Panel[(int)(pantographindicator_r)] = 1;
                    }
                    else
                    {
                        this.Train.Panel[(int)(pantographindicator_r)] = 0;
                    }
                }
            }
            //Sounds
            {
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
            data.DebugMessage = Convert.ToString(FrontPantographState);
        }

        //This function handles legacy power magnets- Set the electric powergap to true & set the end magnet location
        internal void legacypowercutoff(int trainlocation,int magnetdistance)
        {
            if (!electric.powergap)
            {
                electric.powergap = true;
                firstmagnet = trainlocation;
                nextmagnet = trainlocation + magnetdistance;
            }
        }

        //This function handles UKTrainSYS compatible dual magnets
        //Set the next magnet to false
        internal void newpowercutoff(int trainlocation)
        {
            if (!electric.powergap)
            {
                electric.powergap = true;
                firstmagnet = trainlocation;
                nextmagnet = 0;
            }
            else
            {
                electric.powergap = false;
                lastmagnet = trainlocation;
            }
        }

        //Toggle the ACB/VCB based upon it's state
        internal static void breakertrip()
        {
            if (!electric.breakertripped)
            {
                
                electric.breakertripped = true;
                electric.breakerplay();
                tractionmanager.demandpowercutoff();
            }
            else
            {
                
                electric.breakertripped = false;
                electric.breakerplay();
            }
        }
        internal static void breakerplay()
        {
            if (breakersound != -1)
            {
                SoundManager.Play((int)breakersound, 1.0, 1.0, false);
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
                    if (pantographsound != -1)
                    {
                        SoundManager.Play((int)pantographsound, 1.0, 1.0, false);
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
                    if (pantographsound != -1)
                    {
                        SoundManager.Play((int)pantographsound, 1.0, 1.0, false);
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
                    if (pantographsound != -1)
                    {
                        SoundManager.Play((int)pantographsound, 1.0, 1.0, false);
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
                    if (pantographsound != -1)
                    {
                        SoundManager.Play((int)pantographsound, 1.0, 1.0, false);
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