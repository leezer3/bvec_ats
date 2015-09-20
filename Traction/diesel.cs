﻿using System;
using OpenBveApi.Runtime;
using System.Globalization;

namespace Plugin
{
    /// <summary>Represents an electric locomotive.</summary>
    internal class diesel : Device
    {

        // --- members ---

        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        //Internal Variables
        internal double heatingtimer;
        internal double currentheat;
        internal bool nogears;
        /// <summary>The current gear</summary>
        internal int gear = 0;
        /// <summary>The total number of gears fitted to this train</summary>
        internal int totalgears = 0;
        /// <summary>The current RPM of the engine</summary>
        internal int currentrevs;
        internal bool gearsblocked = false;
        internal bool gearplayed = true;
        internal int previousrevs;
        /// <summary>The current engine temperature</summary>
        internal double temperature;
        /// <summary>Stores whether we are currently fuelling</summary>
        internal bool fuelling;
        internal double fuellingtimer;
        /// <summary>The current amount of fuel/ water in the tanks</summary>
        internal int fuel;
        internal double fuelusetimer;
        internal bool gearloop;
        internal double gearlooptimer;
        //Stores ratios for the current gears
        /// <summary>The current gear ratio</summary>
        internal int gearratio;
        /// <summary>The current gear's fade in ratio</summary>
        internal int fadeinratio;
        /// <summary>The current gear's fade out ratio</summary>
        internal int fadeoutratio;

        
        
        //Default Variables
        /// <summary>A comma separated list of the train's gear ratios</summary>
        internal string gearratios = "0";
        /// <summary>A comma separated list of the train's gear fade in ratios</summary>
        internal string gearfadeinrange = "0";
        /// <summary>A comma separated list of the train's gear fade out ratios</summary>
        internal string gearfadeoutrange = "0";
        /// <summary>The starting amount of fuel in the tanks</summary>
        internal double fuelstartamount = 20000;
        /// <summary>The total capacity of the fuel tanks</summary>
        internal double fuelcapacity = 20000;
        /// <summary>The behaviour when the reverser is placed into neutral with the train in motion</summary>
        internal int reversercontrol = 0;
        /// <summary>A comma separated list of the heating rates for each throttle notch</summary>
        internal string heatingrate = "0";
        /// <summary>The total number of fuel units filled per second whilst fuelling is active</summary>
        internal double fuelfillspeed = 50;
        /// <summary>A comma separated list of the fuel consumption for each gear/ power notch</summary>
        internal string fuelconsumption = "0";
        /// <summary>The panel index of the fuel filling indicator</summary>
        internal int fuelfillindicator = -1;
        /// <summary>The time before the gear loop sound is started after changing gear</summary>
        internal double gearlooptime = 0;
        /// <summary>Defines whether we are allowed to rev the engine in neutral</summary>
        internal int allowneutralrevs = 0;

        /// <summary>Is our transmission automatic</summary>
        internal bool automatic;
        /// <summary>Do we heave a part that heats up?</summary>
        internal int heatingpart = 0;
        /// <summary>The overheat warning temperature</summary>
        internal double overheatwarn = 0;
        /// <summary>The overheat temperature</summary>
        internal double overheat = 0;
        /// <summary>What happens when we overheat?</summary>
        internal int overheatresult = 0;

        //Panel Indicies
        /// <summary>The panel indicator for the gear indicator</summary>
        internal int gearindicator = -1;
        /// <summary>The panel indicator for the tachometer</summary>
        internal int tachometer = -1;
        /// <summary>The panel indicator for the fuel gauge</summary>
        internal int fuelindicator = -1;
        /// <summary>The panel indicator for thermometer</summary>
        internal int thermometer = -1;
        /// <summary>The panel indicator for overheat indicator</summary>
        internal int overheatindicator = -1;
        /// <summary>The panel indicator for automatic gears</summary>
        internal int automaticindicator = -1;

        //Sound Indicies
        /// <summary>The sound index played when the gear is changed</summary>
        internal int gearchangesound = -1;
        /// <summary>The sound played looped whilst we are in gear</summary>
        internal int gearloopsound = -1;
        /// <summary>The sound index for the overheat alarm</summary>
        internal int overheatalarm = -1;
        /// <summary>The sound index played when the revs increase in neutral</summary>
        internal int revsupsound = -1;
        /// <summary>The sound index played when the revs decrease in neutral</summary>
        internal int revsdownsound = -1;
        /// <summary>The sound index played whilst the motor is revving in netural</summary>
        internal int motorsound = -1;

        //Arrays
        /// <summary>An array storing the ratio for all the train's gears</summary>
        int[] geararray;
        /// <summary>An array storing the fade in ratio for all the train's gears</summary>
        int[] gearfadeinarray;
        /// <summary>An array storing the fade out ratio for all the train's gears</summary>
        int[] gearfadeoutarray;
        /// <summary>An array storing the heating rate for all gears/ power notches</summary>
        int[] heatingarray;
        /// <summary>An array storing the fuel usage for all the gears/ power notches</summary>
        int[] fuelarray;

        // --- constructors ---

        /// <summary>Creates a new instance of this system.</summary>
        /// <param name="train">The train.</param>
        internal diesel(Train train)
        {
            this.Train = train;
        }

        //<param name="mode">The initialization mode.</param>
        internal override void Initialize(InitializationModes mode)
        {
            //Split gear ratios & fade in/ out ranges into an array
            InternalFunctions.ParseStringToIntArray(gearratios, ref geararray, "gearratios");
            InternalFunctions.ParseStringToIntArray(gearfadeinrange, ref gearfadeinarray, "gearfadeinrange");
            InternalFunctions.ParseStringToIntArray(gearfadeoutrange, ref gearfadeoutarray, "gearfadeoutrange");

            //Test if we have any gears
            if (geararray.Length == 1 && geararray[0] == 0)
            {
                nogears = true;
                totalgears = 0;
            }
            //If we have gears ensure that we're set in gear 0
            //Also set value for total number of gears for easy access
            else
            {
                gear = 0;
                totalgears = geararray.Length + 1;
            }
            //Set previous revs to zero
            previousrevs = 0;
            //Parse heating rates into an array
            InternalFunctions.ParseStringToIntArray(heatingrate, ref heatingarray, "heatingrate");
            //Set temperature to zero
            this.temperature = 0;
            //Parse fuel consumption into an array
            InternalFunctions.ParseStringToIntArray(fuelconsumption, ref fuelarray, "fuelconsumption");
            fuel = (int)fuelstartamount;
        }



        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {

            //Required to reset the max notch before each frame
            this.Train.tractionmanager.SetMaxPowerNotch(this.Train.Specs.PowerNotches, false);

            //If reverser is put into neutral when moving, block the gears
            if (reversercontrol != 0 && Train.trainspeed > 0 && Train.Handles.Reverser == 0)
            {
                Train.diesel.gearsblocked = true;
            }

            if (!nogears)
            {

                //Set gear ratio for current gear
                if (gear == 0)
                {
                    gearratio = 0;
                }
                else if (gear <= geararray.Length)
                {
                    gearratio = geararray[gear - 1];
                }
                else
                {
                    gearratio = geararray[geararray.Length -1];
                }

                //Set fade in ratio for current gear
                if (gear == 0)
                {
                    fadeinratio = 0;
                }
                else if (gear <= gearfadeinarray.Length)
                {
                    fadeinratio = gearfadeinarray[gear - 1];
                }
                else
                {
                    fadeinratio = gearfadeinarray[gearfadeinarray.Length -1];
                }


                //Set fade out ratio for current gear
                if (gear == 0)
                {
                    fadeoutratio = 0;
                }
                else if (gear >= gearfadeoutarray.Length)
                {
                    fadeoutratio = gearfadeoutarray[gear - 1];
                }
                else
                {
                    fadeoutratio = gearfadeoutarray[gearfadeoutarray.Length -1];
                }

                //If the fade in and fade out ratios would make this gear not work, set them both to zero
                if (fadeinratio + fadeoutratio >= 1000)
                {
                    fadeinratio = 0;
                    fadeoutratio = 0;
                }

                //Set current revolutions per minute
                currentrevs = Math.Max(0,Math.Min(1000, Train.trainspeed * gearratio));

                //Now calculate the maximumum power notch
                int power_limit;
                if (currentrevs < fadeinratio)
                {
                    power_limit = (int)((float)currentrevs / fadeinratio * this.Train.Specs.PowerNotches);
                }
                else if (currentrevs > 1000 - fadeoutratio)
                {
                    power_limit = (int)(this.Train.Specs.PowerNotches - (float)(currentrevs - (1000 - fadeoutratio)) / fadeoutratio * this.Train.Specs.PowerNotches);
                }
                else
                {
                    power_limit = this.Train.Specs.PowerNotches;
                }


                //Next we need to set the gears
                //Manual gears are handled in the KeyUp function
                //Automatic gears are handled here
                if (automatic == true)
                {
                    if (Train.diesel.gearsblocked == true)
                    {
                        power_limit = 0;
                        //Stop, drop to N with no power applied and the gears will unblock
                        if (Train.trainspeed == 0 && Train.Handles.Reverser == 0 && Train.Handles.PowerNotch == 0)
                        {
                            Train.diesel.gearsblocked = false;
                        }
                    }

                    //Test if all handles are in a position for a gear to be activated
                    if (Train.Handles.Reverser != 0 && Train.Handles.PowerNotch != 0 && Train.Handles.BrakeNotch == 0)
                    {
                        gearplayed = false;
                        //If we aren't in gear & gears aren't blocked
                        if (gear == 0 && Train.diesel.gearsblocked == false)
                        {
                            gear = 1;
                            gearchange();
                            Train.diesel.gearloop = false;
                            Train.diesel.gearlooptimer = 0.0;
                        }

                        if (currentrevs > Math.Min((2000 - fadeoutratio) / 2, 800) && gear < totalgears - 1)
                        {
                            gear++;
                            gearchange();
                            Train.diesel.gearloop = false;
                            Train.diesel.gearlooptimer = 0.0;
                        }
                        //Change down
                        else if (currentrevs < Math.Max(fadeinratio / 2, 200) && gear > 1)
                        {
                            gear--;
                            gearchange();
                            Train.diesel.gearloop = false;
                            Train.diesel.gearlooptimer = 0.0;
                        }


                    }
                    //If we're stopped with the power off, drop out of gear
                    else if (Train.Handles.Reverser == 0 && Train.Handles.PowerNotch == 0)
                    {
                        gear = 0;
                        if (gearplayed == false)
                        {
                            gearchange();
                            gearplayed = true;
                        }

                    }
                }

                //Finally set the power notch
                if (gear != 0)
                {
                    this.Train.tractionmanager.SetMaxPowerNotch(Math.Min(power_limit, this.Train.Handles.PowerNotch), false);
                    //data.Handles.PowerNotch = Math.Min(power_limit, this.Train.Handles.PowerNotch);
                }
                else
                {
                    this.Train.tractionmanager.SetMaxPowerNotch(0, false);
                }

                //If revving the engine is allowed in neutral
                if (allowneutralrevs == 1 && (gear == 0 || Train.Handles.Reverser == 0))
                {
                    currentrevs = (int)Math.Abs((1000 / Train.Specs.PowerNotches) * Train.Handles.PowerNotch * 0.9);
                    
                    //Play sounds based upon revs state
                    if (revsupsound != -1 && revsdownsound != -1 && motorsound != -1)
                    {
                        double pitch = (double)Train.Handles.PowerNotch / (double)Train.Specs.PowerNotches;
                        if (currentrevs > 0 && currentrevs > previousrevs)
                        {
                            SoundManager.Play(motorsound, 0.8, pitch, true);
                            SoundManager.Play(revsupsound, 1.0, 1.0, false);
                            SoundManager.Stop(revsdownsound);
                            
                        }
                        else if (currentrevs >= 0 && currentrevs < previousrevs)
                        {
                            SoundManager.Play(motorsound, 0.8, pitch, true);
                            SoundManager.Play(revsdownsound, 1.0, 1.0, false);
                            SoundManager.Stop(revsupsound);
                            
                        }
                        else if (currentrevs == 0)
                        {
                            SoundManager.Stop(revsupsound);
                            SoundManager.Stop(revsdownsound);
                            SoundManager.Stop(motorsound);
                        }
                        previousrevs = currentrevs;
                    }
                }

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
                        //Heats based upon RPM
                        int revspercentage = currentrevs / 100;
                        if (this.heatingtimer > 1000)
                        {
                            this.heatingtimer = 0.0;
                            if (revspercentage == 0)
                            {
                                currentheat = heatingarray[0];
                            }
                            else if (revspercentage < heatingarray.Length)
                            {
                                currentheat = heatingarray[revspercentage];
                            }
                            else
                            {
                                currentheat = heatingarray[heatingarray.Length - 1];
                            }
                            temperature += currentheat;
                        }
                    }

                    //Keep temperature below max & above zero
                    if (temperature > overheat)
                    {
                        temperature = overheat;
                        if (overheatresult == 1 && Train.tractionmanager.overheated == false)
                        {
                            Train.DebugLogger.LogMessage("Power cutoff was demanded due to the diesel engine overheating");
                            Train.tractionmanager.demandpowercutoff();
                            Train.tractionmanager.overheated = true;
                        }
                    }
                    else if (temperature < overheat && temperature > 0)
                    {
                        Train.tractionmanager.resetpowercutoff();
                        Train.tractionmanager.overheated = false;
                    }
                    else if (temperature < 0)
                    {
                        temperature = 0;
                    }
                }

            }
            //This section of code uses fuel
            fuelusetimer += data.ElapsedTime.Milliseconds;
            if (fuelusetimer > 1000)
            {
                fuelusetimer = 0.0;
                if (data.Handles.PowerNotch < fuelarray.Length)
                {
                    fuel -= fuelarray[data.Handles.PowerNotch];
                }
                else
                {
                    fuel -= fuelarray[fuelarray.Length - 1];
                }

                if (fuel <= 0)
                {
                    fuel = 0;
                }
            }
            //This section of code fills our fuel tanks
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
            //This section of code runs the gear loop sound
            if (gearloopsound != -1 && gear != 0)
            {
                //Start the timer
                gearlooptimer += data.ElapsedTime.Milliseconds;
                if (gearlooptimer > gearlooptime && gearloop == false)
                {
                    //Start playback and reset our conditions
                    gearloop = true;
                    SoundManager.Play(gearloopsound, 1.0, 1.0, true);
                }
                else if (gearloop == false)
                {
                    SoundManager.Stop(gearloopsound);
                }
            }
            else if (gearloopsound != -1 && gear == 0)
            {
                SoundManager.Stop(gearloopsound);
            }
            
            {
                //Panel Variables
                if (!nogears)
                {
                    if (gearindicator != -1)
                    {
                        this.Train.Panel[(gearindicator)] = gear;
                    }
                    if (tachometer != -1)
                    {
                        this.Train.Panel[(tachometer)] = currentrevs;
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
                if (fuelindicator != -1)
                {
                    this.Train.Panel[(fuelindicator)] = fuel;
                }
                if (fuelfillindicator != -1)
                {
                    if (fuelling == true)
                    {
                        this.Train.Panel[(fuelfillindicator)] = 1;
                    }
                }
            }
            {
                //Sounds
                if (overheatalarm != -1)
                {
                    if (temperature > overheatwarn)
                    {
                        SoundManager.Play(overheatalarm, 1.0, 1.0, true);
                    }
                    else
                    {
                        SoundManager.Stop(overheatalarm);
                    }
                }
            }
        }

        /// <summary>Triggers the gear change sound</summary>
        internal void gearchange()
        {
            if (gearchangesound != -1)
            {
                SoundManager.Play(gearchangesound, 1.0, 1.0, false);
            }
        }

    }
}