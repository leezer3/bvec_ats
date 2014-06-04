using System;
using System.Collections;
using OpenBveApi.Runtime;
using System.Globalization;

namespace Plugin
{
    /// <summary>Represents an electric locomotive.</summary>
    internal class diesel : Device
    {

        // --- members ---

        /// <summary>The underlying train.</summary>
        private Train Train;

        //Internal Variables
        internal double heatingtimer;
        internal double currentheat;
        internal bool nogears;
        internal int gear = 0;
        internal int totalgears = 0;
        internal int currentrevs;
        internal static bool overheated;
        internal static bool gearsblocked = false;
        internal bool gearplayed = true;
        internal int previousrevs;
        internal double temperature;
        internal bool fuelling;
        internal double fuellingtimer;
        internal int fuel;
        internal double fuelusetimer;
        internal bool gearloop;
        internal double gearlooptimer;
        //Stores ratios for the current gears
        internal int gearratio;
        internal int fadeinratio;
        internal int fadeoutratio;

        //New features
        internal double allowneutralrevs = 0;
        internal double revsupsound = -1;
        internal double revsdownsound = -1;
        internal double motorsound = -1;
        //Default Variables
        internal string gearratios = "0";
        internal string gearfadeinrange = "0";
        internal string gearfadeoutrange = "0";
        internal double fuelstartamount = 20000;
        internal double fuelcapacity = 20000;
        internal double reversercontrol = 0;
        internal string heatingrate = "0";
        internal double fuelfillspeed = 50;
        internal string fuelconsumption = "0";
        internal double fuelfillindicator = -1;
        internal double gearlooptime = 0;
        
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
        //Panel Indicies
        internal double gearindicator = -1;
        internal double tachometer = -1;
        internal double fuelindicator = -1;

        //Sound Indicies
        internal double gearchangesound = -1;
        internal double gearloopsound = -1;
        

        //Arrays
        int[] geararray;
        int[] gearfadeinarray;
        int[] gearfadeoutarray;
        int[] heatingarray;
        int[] fuelarray;


        // --- constants ---



        /// <summary>Default paramaters</summary>
        /// Used if no value is loaded from the config file

        //Panel Indicies
        internal double automaticindicator = -1;

        //Sound Indicies


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
            //Split gear ratios into an array
            string[] splitgearratios = gearratios.Split(',');
            geararray = new int[splitgearratios.Length];
            for (int i = 0; i < geararray.Length; i++)
            {
                geararray[i] = (int)(double.Parse(splitgearratios[i], CultureInfo.InvariantCulture));
            }

            //Split gear fade in range into an array
            string[] splitgearfade = gearfadeinrange.Split(',');
            gearfadeinarray = new int[splitgearfade.Length];
            for (int i = 0; i < gearfadeinarray.Length; i++)
            {
                gearfadeinarray[i] = (int)double.Parse(splitgearfade[i], NumberStyles.Integer, CultureInfo.InvariantCulture);
            }

            //Split gear fade out range into an array
            string[] splitgearfade1 = gearfadeoutrange.Split(',');
            gearfadeoutarray = new int[splitgearfade1.Length];
            for (int i = 0; i < gearfadeoutarray.Length; i++)
            {
                gearfadeoutarray[i] = (int)double.Parse(splitgearfade1[i], NumberStyles.Integer, CultureInfo.InvariantCulture);
            }

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

            string[] splitheatingrate = heatingrate.Split(',');
            heatingarray = new int[splitheatingrate.Length];
            for (int i = 0; i < heatingarray.Length; i++)
            {
                heatingarray[i] = (int)double.Parse(splitheatingrate[i], NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
            //Set temperature to zero
            this.temperature = 0;
            //Split fuel consumption into an array
            string[] splitfuelconsumption = fuelconsumption.Split(',');
            fuelarray = new int[splitfuelconsumption.Length];
            for (int i = 0; i < fuelarray.Length; i++)
            {
                fuelarray[i] = (int)double.Parse(splitfuelconsumption[i], NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
            fuel = (int)fuelstartamount;
        }



        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {



            //If reverser is put into neutral when moving, block the gears
            if (reversercontrol != 0 && Train.trainspeed > 0 && Train.Handles.Reverser == 0)
            {
                diesel.gearsblocked = true;
            }

            int power_limit;
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
                currentrevs = Math.Max(0,Math.Min(1000, (int)Train.trainspeed * gearratio));

                //Now calculate the maximumum power notch
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
                if (automatic != -1)
                {
                    if (diesel.gearsblocked == true)
                    {
                        power_limit = 0;
                        //Stop, drop to N with no power applied and the gears will unblock
                        if (Train.trainspeed == 0 && Train.Handles.Reverser == 0 && Train.Handles.PowerNotch == 0)
                        {
                            diesel.gearsblocked = false;
                        }
                    }

                    //Test if all handles are in a position for a gear to be activated
                    if (Train.Handles.Reverser != 0 && Train.Handles.PowerNotch != 0 && Train.Handles.BrakeNotch == 0)
                    {
                        gearplayed = false;
                        //If we aren't in gear & gears aren't blocked
                        if (gear == 0 && diesel.gearsblocked == false)
                        {
                            gear = 1;
                            gearchange();
                        }

                        if (currentrevs > Math.Min((2000 - fadeoutratio) / 2, 800) && gear < totalgears - 1)
                        {
                            gear++;
                            gearchange();
                        }
                        //Change down
                        else if (currentrevs < Math.Max(fadeinratio / 2, 200) && gear > 1)
                        {
                            gear--;
                            gearchange();
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
                    data.Handles.PowerNotch = (int)Math.Min(power_limit, this.Train.Handles.PowerNotch);
                }
                else
                {
                    data.Handles.PowerNotch = 0;
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
                            SoundManager.Play((int)motorsound, 0.8, pitch, true);
                            SoundManager.Play((int)revsupsound, 1.0, 1.0, false);
                            SoundManager.Stop((int)revsdownsound);
                            
                        }
                        else if (currentrevs >= 0 && currentrevs < previousrevs)
                        {
                            SoundManager.Play((int)motorsound, 0.8, pitch, true);
                            SoundManager.Play((int)revsdownsound, 1.0, 1.0, false);
                            SoundManager.Stop((int)revsupsound);
                            
                        }
                        else if (currentrevs == 0)
                        {
                            SoundManager.Stop((int)revsupsound);
                            SoundManager.Stop((int)revsdownsound);
                            SoundManager.Stop((int)motorsound);
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
                                currentheat = heatingarray[(int)revspercentage];
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
                    gearlooptimer = 0.0;
                    SoundManager.Play((int)gearloopsound, 1.0, 1.0, true);
                }
                else
                {
                    SoundManager.Stop((int)gearloopsound);
                }
            }
            else if (gearloopsound != -1 && gear == 0)
            {
                SoundManager.Stop((int)gearloopsound);
            }
            
            {
                //Panel Variables
                if (!nogears)
                {
                    if (gearindicator != -1)
                    {
                        this.Train.Panel[(int)(gearindicator)] = gear;
                    }
                    if (tachometer != -1)
                    {
                        this.Train.Panel[(int)(tachometer)] = currentrevs;
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
                if (fuelindicator != -1)
                {
                    this.Train.Panel[(int)(fuelindicator)] = (int)fuel;
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

        internal void gearchange()
        {
            if (gearchangesound != -1)
            {
                SoundManager.Play((int)gearchangesound, 1.0, 1.0, false);
            }
        }

    }
}