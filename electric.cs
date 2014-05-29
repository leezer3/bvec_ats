using System;
using System.Collections;
using OpenBveApi.Runtime;


namespace Plugin
{
    /// <summary>Represents an electric locomotive.</summary>
    internal class electric : Device
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

        //Default Variables
        internal double ammeter = -1;
        internal string ammetervalues = "0";
        internal string pickuppoints = "0";
        internal double powergapbehaviour = 0;
        internal double recieverlocation = 0;
        internal string heatingrate = "0";

        //Panel Indicies
        internal double powerindicator = -1;
        internal double breakerindicator = -1;

        //Sound Indicies
        internal static double breakersound = -1;

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

        //Sound Indicies


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
                    tractionmanager.resetpowercutoff();
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
                        if (breakertripped == false)
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
                //If the ACB/VCB has now been reset & we're not in a powergap reset traction power
                if (breakertripped == false && powergap == false)
                {
                    tractionmanager.resetpowercutoff();
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

                    else if (Train.Handles.PowerNotch !=0 && Train.Handles.PowerNotch <= ammeterlength)
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
    }
}