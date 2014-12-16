/* This file contains code originally derived from that developed by Stefano Zilocchi & licenced under the GPL.
 * Relicenced under BSD 2-Clause with permission
 */

using System;
using OpenBveApi.Runtime;
using System.Globalization;

namespace Plugin
{
    /// <summary>Represents the traction modelling of the Italian SCMT system.</summary>
    internal partial class SCMT_Traction : Device
    {

        // --- members ---

        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        internal bool enabled;

        //Internal Variables
        internal double heatingtimer;
        internal double currentheat;
        internal bool nogears;
        /// <summary>The current gear</summary>
        internal static int gear = 0;
        internal int totalgears = 0;
        internal int currentrevs;
        internal static bool gearsblocked = false;
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

        internal int sunoavv = -1;
        internal static int sunoconsavv = -1;
        internal static int sunosottofondo = -1;
        internal static int sunoarr = -1;
        internal static int sunoimpvel = -1;
        internal static int sunoscmton = -1;
        internal static int sunoconfdati = -1;

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

        //PROVIDES A STATIC COPY OF THE REVERSER POSITION
        internal static int reverserposition;


        //STORES PARAMATERS FOR OS_SZ_ATS TRACTION
        internal static bool lcm;
        internal int lcm_state;
        internal bool lca;
        internal int flag;
        internal static bool Avv;
        internal static bool ConsAvv;
        internal static bool ChiaveBanco;
        internal static int BatteryVoltage;
        internal static int indlcm;
        internal static int indattesa;
        internal static bool attessa;
        /// <summary>Waiting timer</summary>
        internal static Timer AttessaTimer;
        internal static bool flagavv;
        internal bool flagfe;
        
        //Indicators
        internal static Indicator Abbanco;
        internal static Indicator ConsAvviam;
        internal static Indicator AvariaGen;
        internal static Indicator Avviam;
        internal static Indicator Arresto;

        //Three timers triggered by the SCMT self-test sequence
        //Self-test timer??
        internal static Timer SCMTtesttimer;
        //Buttons timer
        internal static Timer timertestpulsanti;
        //Static, waiting timer??
        internal static Timer timerScariche;
        //SCMT Self-test lights timers
        internal static Timer timerRitSpegscmt;
        //Battery Timer
        internal static Timer BatteryTimer;
        //Starter Timer
        internal static Timer StarterTimer;

        internal static bool flagspiascmt;
        //Exhaust sequence??
        internal static int seqScarico;
        //Test buttons
        internal int testpulsanti;
        /// <summary>Stores the revs counter value [REFACTOR TO SEPARATE STORE WHEN PANEL IMPLEMENTED]</summary>
        internal int indcontgiri;
        /// <summary>Tachometer of some description?? [REFACTOR TO SEPARATE STORE WHEN PANEL IMPLEMENTED]</summary>
        internal int indgas;
        internal bool flagcarr;
        /// <summary>Stores whether diagnostic mode is active [Under 4km/h]</summary>
        internal bool flagmonitor;
        /// <summary>Stores the timer value for the trolley brakes</summary>
        internal double trolleybraketimer;
        //Used by the dynometer
        internal double v1;
        internal double v2;
        /// <summary>Stores the current dynometer value</summary>
        internal double dynometer;
        /// <summary>Stores the timer value for the dynometer</summary>
        internal static double dynometertimer;
        /// <summary>Stores whether the battery timer is currently active</summary>
        
        /// <summary>Stores whether the set speed system is currently active</summary>
        internal bool setspeed_active;
        /// <summary>The current set speed</summary>
        internal static int setpointspeed;
        /// <summary>The maximum possible set speed</summary>
        internal static int maxsetpointspeed;
        /// <summary>The numeric state of the setpoint speed</summary>
        internal static int setpointspeedstate;
        /// <summary>The sound played when the setpoint speed is changed</summary>
        internal static int setpointspeed_sound = -1;

        internal static bool setspeedincrease_pressed;
        internal static bool setspeeddecrease_pressed;

        // --- constructors ---

        /// <summary>Creates a new instance of this system.</summary>
        /// <param name="train">The train.</param>
        internal SCMT_Traction(Train train)
        {
            this.Train = train;
        }

        //<param name="mode">The initialization mode.</param>
        internal override void Initialize(InitializationModes mode)
        {
            try
            {
                //Split gear ratios into an array
                string[] splitgearratios = gearratios.Split(',');
                geararray = new int[splitgearratios.Length];
                for (int i = 0; i < geararray.Length; i++)
                {
                    geararray[i] = (int)(double.Parse(splitgearratios[i], CultureInfo.InvariantCulture));
                }
            }
            catch
            {
                InternalFunctions.LogError("gearratios");
            }
            try
            {
                //Split gear fade in range into an array
                string[] splitgearfade = gearfadeinrange.Split(',');
                gearfadeinarray = new int[splitgearfade.Length];
                for (int i = 0; i < gearfadeinarray.Length; i++)
                {
                    gearfadeinarray[i] = (int)double.Parse(splitgearfade[i], NumberStyles.Integer, CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                InternalFunctions.LogError("gearfadeinrange");
            }
            try
            {
                //Split gear fade out range into an array
                string[] splitgearfade1 = gearfadeoutrange.Split(',');
                gearfadeoutarray = new int[splitgearfade1.Length];
                for (int i = 0; i < gearfadeoutarray.Length; i++)
                {
                    gearfadeoutarray[i] = (int)double.Parse(splitgearfade1[i], NumberStyles.Integer, CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                InternalFunctions.LogError("gearfadeoutrange");
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
            try
            {
                string[] splitheatingrate = heatingrate.Split(',');
                heatingarray = new int[splitheatingrate.Length];
                for (int i = 0; i < heatingarray.Length; i++)
                {
                    heatingarray[i] = (int)double.Parse(splitheatingrate[i], NumberStyles.Integer, CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                InternalFunctions.LogError("heatingrate");
            }
            //Set temperature to zero
            this.temperature = 0;
            //Split fuel consumption into an array
            try
            {
                string[] splitfuelconsumption = fuelconsumption.Split(',');
                fuelarray = new int[splitfuelconsumption.Length];
                for (int i = 0; i < fuelarray.Length; i++)
                {
                    fuelarray[i] = (int)double.Parse(splitfuelconsumption[i], NumberStyles.Integer, CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                InternalFunctions.LogError("fuelconsumption");
            }

            fuel = (int)fuelstartamount;
            reverserposition = Train.Handles.Reverser;
        }



        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            if (enabled == true)
            {
                reverserposition = Train.Handles.Reverser;
                //If reverser is put into neutral when moving, block the gears
                if (reversercontrol != 0 && Train.trainspeed > 0 && Train.Handles.Reverser == 0)
                {
                    diesel.gearsblocked = true;
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
                        gearratio = geararray[geararray.Length - 1];
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
                        fadeinratio = gearfadeinarray[gearfadeinarray.Length - 1];
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
                        fadeoutratio = gearfadeoutarray[gearfadeoutarray.Length - 1];
                    }

                    //If the fade in and fade out ratios would make this gear not work, set them both to zero
                    if (fadeinratio + fadeoutratio >= 1000)
                    {
                        fadeinratio = 0;
                        fadeoutratio = 0;
                    }

                    //Set current revolutions per minute
                    currentrevs = Math.Max(0, Math.Min(1000, Train.trainspeed*gearratio));

                    //Now calculate the maximumum power notch
                    int power_limit;
                    if (currentrevs < fadeinratio)
                    {
                        power_limit = (int) ((float) currentrevs/fadeinratio*this.Train.Specs.PowerNotches);
                    }
                    else if (currentrevs > 1000 - fadeoutratio)
                    {
                        power_limit =
                            (int)
                                (this.Train.Specs.PowerNotches -
                                 (float) (currentrevs - (1000 - fadeoutratio))/fadeoutratio*
                                 this.Train.Specs.PowerNotches);
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
                        if (SCMT_Traction.gearsblocked == true)
                        {
                            power_limit = 0;
                            //Stop, drop to N with no power applied and the gears will unblock
                            if (Train.trainspeed == 0 && Train.Handles.Reverser == 0 && Train.Handles.PowerNotch == 0)
                            {
                                SCMT_Traction.gearsblocked = false;
                            }
                        }

                        //Test if all handles are in a position for a gear to be activated
                        if (Train.Handles.Reverser != 0 && Train.Handles.PowerNotch != 0 &&
                            Train.Handles.BrakeNotch == 0)
                        {
                            gearplayed = false;
                            //If we aren't in gear & gears aren't blocked
                            if (gear == 0 && SCMT_Traction.gearsblocked == false)
                            {
                                gear = 1;
                                gearchange();
                                Train.diesel.gearloop = false;
                                Train.diesel.gearlooptimer = 0.0;
                            }

                            if (currentrevs > Math.Min((2000 - fadeoutratio)/2, 800) && gear < totalgears - 1)
                            {
                                gear++;
                                gearchange();
                                Train.diesel.gearloop = false;
                                Train.diesel.gearlooptimer = 0.0;
                            }
                                //Change down
                            else if (currentrevs < Math.Max(fadeinratio/2, 200) && gear > 1)
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
                        data.Handles.PowerNotch = Math.Min(power_limit, this.Train.Handles.PowerNotch);
                    }
                    else
                    {
                        data.Handles.PowerNotch = 0;
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
                            int revspercentage = currentrevs/100;
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
                        fuel += (int) fuelfillspeed;
                    }
                    if (fuel > fuelcapacity)
                    {
                        fuel = (int) fuelcapacity;
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

                //Start SZ_ATS handling
                if (lcm == false && Train.Handles.PowerNotch > 0)
                {
                    lca = true;
                }
                if (lcm == true && lcm_state == 0)
                {
                    lcm = false;
                }
                if (lcm_state > 0 && gear != 0)
                {
                    setspeed_active = false;
                    setpointspeed = 0;
                    lca = false;
                    lcm = true;
                }
                else if (gear == 0)
                {
                    lcm = false;
                    data.Handles.PowerNotch = 0;
                }

                if (maxsetpointspeed != -1)
                {
                    //This handles the setpoint speed function
                    if (setpointspeed == 0 && lca == true)
                    {
                        data.Handles.PowerNotch = 0;
                    }
                    if (Train.trainspeed > setpointspeed && flag == 0 && setpointspeed > 0 && lca == true &&
                        data.Handles.PowerNotch > 0)
                    {
                        //If we've exceeded the set point speed cut power
                        flag = 1;
                        tractionmanager.demandpowercutoff();
                    }
                    if (Train.trainspeed > setpointspeed + 1 && flag == 1 && lca == true && data.Handles.PowerNotch > 0)
                    {
                        //If we're continuing to accelerate, demand a brake application
                        tractionmanager.demandbrakeapplication();
                        flag = 2;
                    }
                    if ((Train.trainspeed < setpointspeed && flag == 2 && lca == true) ||
                        (lca == true && flag == 2 && data.Handles.PowerNotch == 0) || (lcm == true && flag == 2))
                    {
                        tractionmanager.resetbrakeapplication();
                        flag = 1;
                    }
                    if ((Train.trainspeed < setpointspeed - 2 && flag == 1 && lca == true) ||
                        (lca == true && flag == 2 && data.Handles.PowerNotch == 0) || (lcm == true && flag == 1))
                    {
                        tractionmanager.resetpowercutoff();
                        flag = 0;
                    }
                }
                //Handles the starter motor
                if (flagavv == true)
                {
                    if (StarterTimer.TimerActive == true)
                    {
                        StarterTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
                        if (StarterTimer.TimeElapsed > 2000)
                        {
                            Avv = true;
                            //Play SUNOAVV
                            if (sunoavv != -1)
                            {
                                SoundManager.Play(sunoavv, 1.0, 1.0, false);
                            }
                            //Play SUONOSOTTOFONDO looping
                            if (sunosottofondo != -1)
                            {
                                SoundManager.Play(sunosottofondo, 1.0, 1.0, true);
                            }

                            BatteryTimer.TimerActive = true;
                            BatteryTimer.TimeElapsed = 0.0;
                            AvariaGen.IndicatorState = IndicatorStates.Flashing;
                            AvariaGen.Lit = false;
                        }

                        if (Avv == true && (SCMT.testscmt == 4 || SCMT.testscmt == 0))
                        {
                            if (gear == 0)
                            {
                                lcm = true;
                                gear = 1;
                            }
                        }

                        if (BatteryTimer.TimerActive == true)
                        {
                            BatteryTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
                            if (BatteryTimer.TimeElapsed > 3000)
                            {
                                BatteryVoltage = 29;
                                BatteryTimer.TimerActive = false;
                            }
                        }
                    }
                }
                //Handles the dynameter
                {
                    dynometertimer += data.ElapsedTime.Milliseconds;
                    if (dynometertimer > 200)
                    {
                        v1 = v2;
                        v2 = Train.trainspeed;
                        if ((v1 != 0 && v2 != 0 && Train.Handles.BrakeNotch < 1 &&
                             (Train.Handles.PowerNotch > 0 || Train.Handles.PowerNotch > 0)) ||
                            (v1 != 0 && v2 != 0 && Train.Handles.BrakeNotch >= 1) && Train.Handles.Reverser != 0 &&
                            Train.trainspeed > 10)
                        {
                            dynometer = (((v2 - v1)/3.6))/0.2*100;
                            if (dynometer < -100)
                            {
                                dynometer = -100;
                            }
                        }
                        else
                        {
                            dynometer = 0;
                        }
                        dynometertimer = 0.0;
                    }
                }
                //Handles the trolley brakes
                {
                    if (flagcarr == false && Train.Handles.BrakeNotch >= 1 && data.Vehicle.BcPressure > 5000)
                    {
                        trolleybraketimer += data.ElapsedTime.Milliseconds;
                        if (trolleybraketimer > 1000)
                        {
                            flagcarr = true;
                            //Set INDCARRFREN to 1
                            trolleybraketimer = 0;
                        }
                    }
                    else if (flagcarr == true && Train.Handles.BrakeNotch < 1 && data.Vehicle.BcPressure < 5000)
                    {
                        trolleybraketimer += data.ElapsedTime.Milliseconds;
                        if (trolleybraketimer > 2500)
                        {
                            //Set INDCARRFREN to 0
                            flagcarr = false;
                            trolleybraketimer = 0;
                        }
                    }
                }
                //Handles the diagnostic display at under 4km/h
                {
                    if (Train.trainspeed > 4 && flagmonitor == false)
                    {
                        //Set INDSPEGNMON to 1
                        flagmonitor = true;
                    }
                    else if (Train.trainspeed == 0 && flagmonitor == true)
                    {
                        //Set INDSPEGNMON to 0
                        flagmonitor = false;
                    }
                }

                //Handles the revs counter
                {
                    if (Avv == true)
                    {
                        if (data.Handles.BrakeNotch == 0)
                        {
                            if (Train.trainspeed < 40)
                            {
                                indcontgiri = (int) dynometer + 10;
                                indgas = (int) dynometer + 110;
                            }
                            else
                            {
                                indcontgiri = (int) dynometer;
                                indgas = (int) dynometer + 50;
                            }
                        }
                        else if (data.Handles.BrakeNotch > 0)
                        {
                            if ((int) Math.Abs(dynometer) < 130)
                            {
                                indcontgiri = (int) Math.Abs(dynometer) + 40;
                                indgas = (int) Math.Abs(dynometer) + 120;
                            }
                            else
                            {
                                indcontgiri = 150;
                                indgas = 230;
                            }
                        }
                    }
                }
                //Literal translation appears to be faith/ belief?
                //Stops brake notch #1 intervening at certain speeds
                {
                    if (Train.Handles.BrakeNotch == 1 && Train.trainspeed < 40 && flagfe == false)
                    {
                        data.Handles.BrakeNotch = 0;
                    }
                    else if (Train.Handles.BrakeNotch == 1 && Train.trainspeed >= 40)
                    {
                        data.Handles.BrakeNotch = 1;
                        flagfe = true;
                    }
                    else if (Train.Handles.BrakeNotch == 1 && Train.trainspeed <= 35 && flagfe == true)
                    {
                        data.Handles.BrakeNotch = 0;
                        flagfe = false;
                    }
                }
                //Waiting function
                {
                    if (indattesa == 1)
                    {
                        if (AttessaTimer.TimerActive == true)
                        {
                            AttessaTimer.TimeElapsed += data.ElapsedTime.Milliseconds;
                            if (AttessaTimer.TimeElapsed > 7000)
                            {
                                indattesa = 0;
                                AttessaTimer.TimerActive = false;
                            }
                        }
                    }
                }
                //Self-test function
                {
                    if (SCMTtesttimer.TimerActive == true)
                    {
                        if (SCMT.testscmt == 1)
                        {
                            SCMTtesttimer.TimeElapsed += data.ElapsedTime.Milliseconds;
                            if (SCMTtesttimer.TimeElapsed > 35000)
                            {
                                SCMTtesttimer.TimeElapsed = 0;
                                SCMT.testscmt = 2;
                                SCMTtesttimer.TimerActive = false;
                            }
                        }
                    }
                    if (timerScariche.TimerActive == true)
                    {
                        timerScariche.TimeElapsed += data.ElapsedTime.Milliseconds;
                        if (seqScarico == 0)
                        {
                            if (timerScariche.TimeElapsed > 100)
                            {
                                timerScariche.TimeElapsed = 0;
                                data.Handles.BrakeNotch = 7;
                                seqScarico = 1;
                            }
                        }
                        if (seqScarico == 1)
                        {
                            if (timerScariche.TimeElapsed > 10000)
                            {
                                timerScariche.TimeElapsed = 0;
                                data.Handles.BrakeNotch = Train.Handles.BrakeNotch;
                                if (Train.Handles.BrakeNotch > 2)
                                {
                                    SCMT.testscmt = 5;
                                    //Stop sunoscmton from playing
                                    SCMTtesttimer.TimerActive = false;
                                    timertestpulsanti.TimerActive = false;
                                    timerScariche.TimerActive = false;
                                }
                                seqScarico = 2;
                            }
                        }
                        if (seqScarico == 2)
                        {
                            if (timerScariche.TimeElapsed > 4000)
                            {
                                timerScariche.TimeElapsed = 0;
                                data.Handles.BrakeNotch = 5;
                                seqScarico = 3;
                            }
                        }
                        if (seqScarico == 3)
                        {
                            if (timerScariche.TimeElapsed > 1000)
                            {
                                timerScariche.TimeElapsed = 0;
                                data.Handles.BrakeNotch = Train.Handles.BrakeNotch;
                                seqScarico = 4;
                            }
                        }
                        if (seqScarico == 4)
                        {
                            if (timerScariche.TimeElapsed > 500)
                            {
                                timerScariche.TimeElapsed = 0;
                                data.Handles.BrakeNotch = 5;
                                seqScarico = 5;
                            }
                        }
                        if (seqScarico == 5)
                        {
                            if (timerScariche.TimeElapsed > 1000)
                            {
                                timerScariche.TimeElapsed = 0;
                                data.Handles.BrakeNotch = Train.Handles.BrakeNotch;
                                seqScarico = 6;
                            }
                        }
                        if (seqScarico == 6)
                        {
                            if (timerScariche.TimeElapsed > 10500)
                            {
                                timerScariche.TimeElapsed = 0;
                                data.Handles.BrakeNotch = 5;
                                seqScarico = 7;
                            }
                        }
                        if (seqScarico == 7)
                        {
                            if (timerScariche.TimeElapsed > 500)
                            {
                                timerScariche.TimeElapsed = 0;
                                data.Handles.BrakeNotch = Train.Handles.BrakeNotch;
                                seqScarico = 8;
                            }
                        }
                        if (seqScarico == 8)
                        {
                            if (timerScariche.TimeElapsed > 500)
                            {
                                timerScariche.TimeElapsed = 0;
                                data.Handles.BrakeNotch = 5;
                                seqScarico = 9;
                            }
                        }
                        if (seqScarico == 9)
                        {
                            if (timerScariche.TimeElapsed > 300)
                            {
                                timerScariche.TimeElapsed = 0;
                                data.Handles.BrakeNotch = Train.Handles.BrakeNotch;
                                seqScarico = 10;
                                timerScariche.TimerActive = false;
                            }
                        }
                    }
                    if (timerRitSpegscmt.TimerActive == true)
                    {
                        timerRitSpegscmt.TimeElapsed += data.ElapsedTime.Milliseconds;
                        if (timerRitSpegscmt.TimeElapsed > 6000)
                        {
                            timerRitSpegscmt.TimerActive = false;
                        }

                    }
                }
                //Runs the buttons during self-test
                {
                    if (timertestpulsanti.TimerActive == true)
                    {
                        timertestpulsanti.TimeElapsed += data.ElapsedTime.Milliseconds;
                        if (testpulsanti == 8)
                        {
                            if (timertestpulsanti.TimeElapsed > 23000)
                            {
                                timertestpulsanti.TimeElapsed = 0;
                                testpulsanti = 0;
                            }
                        }
                        if (testpulsanti == 0)
                        {
                            if (timertestpulsanti.TimeElapsed > 150)
                            {
                                timertestpulsanti.TimeElapsed = 0;
                                testpulsanti = 1;
                            }
                        }
                        if (testpulsanti == 1)
                        {
                            if (timertestpulsanti.TimeElapsed > 150)
                            {
                                timertestpulsanti.TimeElapsed = 0;
                                testpulsanti = 2;
                            }
                        }
                        if (testpulsanti == 2)
                        {
                            if (timertestpulsanti.TimeElapsed > 150)
                            {
                                timertestpulsanti.TimeElapsed = 0;
                                testpulsanti = 3;
                            }
                        }
                        if (testpulsanti == 3)
                        {
                            if (timertestpulsanti.TimeElapsed > 150)
                            {
                                timertestpulsanti.TimeElapsed = 0;
                                testpulsanti = 4;
                            }
                        }
                        if (testpulsanti == 4)
                        {
                            if (timertestpulsanti.TimeElapsed > 150)
                            {
                                timertestpulsanti.TimeElapsed = 0;
                                testpulsanti = 5;
                            }
                        }
                        if (testpulsanti == 5)
                        {
                            if (timertestpulsanti.TimeElapsed > 150)
                            {
                                timertestpulsanti.TimeElapsed = 0;
                                testpulsanti = 6;
                            }
                        }
                        if (testpulsanti == 6)
                        {
                            if (timertestpulsanti.TimeElapsed > 150)
                            {
                                timertestpulsanti.TimeElapsed = 0;
                                testpulsanti = 7;
                            }
                        }
                        if (testpulsanti == 7)
                        {
                            if (timertestpulsanti.TimeElapsed > 150)
                            {
                                timertestpulsanti.TimerActive = false;
                                testpulsanti = 8;
                            }
                        }
                    }
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
                        this.Train.Panel[(thermometer)] = (int) temperature;
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
                {
                    //Indicators
                    if (Abbanco.PanelIndex != -1)
                    {
                        if (Abbanco.IndicatorState == IndicatorStates.Solid)
                        {
                            Abbanco.Lit = true;
                        }
                        else if (Abbanco.IndicatorState == IndicatorStates.Flashing)
                        {
                            Abbanco.TimeElapsed += data.ElapsedTime.Milliseconds;
                            if (Abbanco.TimeElapsed > 1000)
                            {
                                if (Abbanco.Lit == true)
                                {
                                    Abbanco.Lit = false;
                                }
                                else
                                {
                                    Abbanco.Lit = true;
                                }
                                Abbanco.TimeElapsed = 0.0;
                            }
                        }

                    }
                    if (ConsAvviam.PanelIndex != -1)
                    {
                        if (ConsAvviam.IndicatorState == IndicatorStates.Solid)
                        {
                            ConsAvviam.Lit = true;
                        }
                        else if (ConsAvviam.IndicatorState == IndicatorStates.Flashing)
                        {
                            ConsAvviam.TimeElapsed += data.ElapsedTime.Milliseconds;
                            if (ConsAvviam.TimeElapsed > 1000)
                            {
                                if (ConsAvviam.Lit == true)
                                {
                                    ConsAvviam.Lit = false;
                                }
                                else
                                {
                                    ConsAvviam.Lit = true;
                                }
                                Abbanco.TimeElapsed = 0.0;
                            }
                        }

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

        //Runs the speed control function

        /// <summary>Call from the traction manager to increase the set constant speed</summary>
        internal static void increasesetspeed()
        {
            if (SCMT_Traction.setpointspeed < SCMT_Traction.maxsetpointspeed)
            {
                SCMT_Traction.setpointspeed += 5;
                setpointspeedstate += 1;
                if (setpointspeed_sound != -1)
                {
                    SoundManager.Play(setpointspeed_sound, 1.0, 1.0, false);
                }
                SCMT_Traction.setspeedincrease_pressed = true;
                //Need to sort out a blinker for this
                //Run the blinker in the main thread
            }
        }

        /// <summary>Call from the traction manager to decrease the set constant speed</summary>
        internal static void decreasesetspeed()
        {
            if (SCMT_Traction.setpointspeed > 0)
            {
                SCMT_Traction.setpointspeed -= 5;
                setpointspeed -= 1;
                if (setpointspeed_sound != -1)
                {
                    SoundManager.Play(setpointspeed_sound, 1.0, 1.0, false);
                }
                SCMT_Traction.setspeeddecrease_pressed = true;
                //Need to sort out a blinker for this
                //Run the blinker in the main thread
            }
        }

        /// <summary>Call from the traction manager when a speed up/ down key is released</summary>
        internal static void releasekey()
        {
            if (setpointspeed_sound != -1)
            {
                SoundManager.Play(setpointspeed_sound, 1.0, 1.0, false);
            }
            SCMT_Traction.setspeedincrease_pressed = false;
            SCMT_Traction.setspeeddecrease_pressed = false;
        }

        /// <summary>Call from the traction manager when the key counter key is pressed</summary>
        /// Something to do with turning the key to start permission?
        internal static void AbilitaBanco()
        {
            if (ConsAvv == false && SCMT.testscmt == 0)
            {
                if (ChiaveBanco == false)
                {
                    Abbanco.IndicatorState = IndicatorStates.Flashing;
                    //Show waiting indicator
                    AttessaTimer.TimeElapsed = 0;
                }
                else
                {
                    SCMT.testscmt = 0;
                }

                ChiaveBanco = false;
                //Play key turned to bench and start permission sound
                if (sunoconsavv != -1)
                {
                    SoundManager.Play(sunoconsavv, 1.0, 1.0, false);
                }
            }
        }

        /// <summary>Call from the traction manager when the consent to start key is pressed</summary>
        /// Check if engine can be started
        internal static void ConsensoAvviamento()
        {
            if (ConsAvv == false && ChiaveBanco == true)
            {
                ConsAvv = true;
                //Play key turned to bench and start permission sound
                if (sunoconsavv != -1)
                {
                    SoundManager.Play(sunoconsavv, 1.0, 1.0, false);
                }
            }
            else if (ConsAvv == true && ChiaveBanco == true)
            {
                ConsAvv = false;
                //Play key turned to bench and start permission sound
                if (sunoconsavv != -1)
                {
                    SoundManager.Play(sunoconsavv, 1.0, 1.0, false);
                }
            }

            if (ConsAvv == false && Avv == true)
            {
                Avv = false;
                SCMT_Traction.gear = 0;
                if (sunosottofondo != -1)
                {
                    SoundManager.Play(sunosottofondo, 1.0, 1.0, false);
                }
                if (sunoarr != -1)
                {
                    SoundManager.Play(sunoarr, 1.0, 1.0, false);
                }
                BatteryVoltage = 23;
                AvariaGen.IndicatorState = IndicatorStates.Flashing;
                AvariaGen.Lit = true;
                
                //Set indcontgiri [Tachometer]
                //indgas [Digital fuel gauge?]
                // to -50
                indattesa = -1;
                AttessaTimer.TimeElapsed = 0;
                AttessaTimer.TimerActive = true;

            }
        }

        /// <summary>Call from the traction manager when the engine start key is pressed</summary>
        internal static void Avviamento()
        {
            //Blink indicator Avviam
            //Then show
            if (ConsAvv == true && Avv == false && reverserposition == 0 && indlcm == 0 && indattesa == 0)
            {
                flagavv = true;
                if (StarterTimer.TimerActive == false)
                {
                    StarterTimer.TimerActive = true;
                    StarterTimer.TimeElapsed = 0.0;
                }
            }
        }

        /// <summary>Call from the traction manager when the engine start key is released</summary>
        internal static void AvviamentoReleased()
        {
            Avviam.IndicatorState = IndicatorStates.Flashing;
            Avviam.Lit = false;
            flagavv = false;
            StarterTimer.TimerActive = false;
        }

        /// <summary>Call from the traction manager when the engine shutdown key is pressed</summary>
        internal static void Spegnimento()
        {
            //Blink indicator Arresto
            Arresto.IndicatorState = IndicatorStates.Flashing;
            Arresto.Lit = true;
            if (Avv == true)
            {
                //Stop SUONOSOTTOFONDO
                //Play SUONOARR
                Avv = false;
                lcm = false;
                SCMT_Traction.gear = 0;
                BatteryVoltage = 23;
                //Set indcontgiri [Tachometer]
                //indgas [Digital fuel gauge?]
                // to -50
                AvariaGen.IndicatorState = IndicatorStates.Flashing;
                AvariaGen.Lit = true;
                indattesa = 1;
                AttessaTimer.TimerActive = true;
                AttessaTimer.TimeElapsed = 0;
            }
        }

        /// <summary>Call from the traction manager when the engine shutdown key is released</summary>
        internal static void SpegnimentoReleased()
        {
            Arresto.IndicatorState = IndicatorStates.Flashing;
            Arresto.Lit = false;
        }

        /// <summary>Call from the traction manager when the LCM Up key is pressed</summary>
        internal static void LCMup()
        {
            if (indlcm < 8)
            {
                indlcm += 1;
                if (sunoimpvel != -1)
                {
                    SoundManager.Play(sunoimpvel, 1.0, 1.0, false);
                }
                lcm = true;
            }

            if (lcm == true)
            {
                //Set power handle to the indlcm value
            }
        }

        /// <summary>Call from the traction manager when the LCM Down key is pressed</summary>
        internal static void LCMdown()
        {
            if (indlcm > 0)
            {
                indlcm -= 1;
                if (sunoimpvel != -1)
                {
                    SoundManager.Play(sunoimpvel, 1.0, 1.0, false);
                }
                //Set power handle to the indlcm value
            }

            if (lcm == true)
            {
                //set power handle to indlcm value
                if (indlcm == 0)
                {
                    lcm = false;
                }
            }
        }

        /// <summary>Call from the traction manager when the SCMT Test key is pressed</summary>
        internal static void TestSCMT()
        {
            if (SCMT.testscmt == 0 && ChiaveBanco == true && Train.trainspeed == 0)
            {
                SCMT.testscmt = 1;
                SCMTtesttimer.TimeElapsed = 0;
                timertestpulsanti.TimeElapsed = 0;
                timerScariche.TimeElapsed = 0;
                if (sunoconsavv != -1)
                {
                    SoundManager.Play(sunoconsavv, 1.0, 1.0, false);
                }
                if (sunoscmton != -1)
                {
                    SoundManager.Play(sunoscmton, 1.0, 1.0, false);
                }
            }
            else if (SCMT.testscmt == 2 && Train.trainspeed == 0)
            {
                SCMT.testscmt = 3;
                if (sunoconfdati != -1)
                {
                    SoundManager.Play(sunoconfdati, 1.0, 1.0, false);
                }
            }
            else if (SCMT.testscmt == 3 && Train.trainspeed == 0)
            {
                SCMT.testscmt = 4;
                if (sunoconfdati != -1)
                {
                    SoundManager.Play(sunoconfdati, 1.0, 1.0, false);
                }
                timerRitSpegscmt.TimeElapsed = 0;
            }
            else if (SCMT.testscmt == 4 && Train.trainspeed == 0)
            {
                if (timerRitSpegscmt.TimeElapsed > 5000 || timerRitSpegscmt.TimerActive == false)
                {
                    SCMT.testscmt = 0;
                    flagspiascmt = false;
                    SCMT.SCMT_Alert = true;
                    if (sunoconsavv != -1)
                    {
                        SoundManager.Play(sunoconsavv, 1.0, 1.0, false);
                    }
                    seqScarico = 0;
                    timerRitSpegscmt.TimerActive = false;
                }
            }
            else if (SCMT.testscmt == 5)
            {
                SCMT.testscmt = 0;
                flagspiascmt = false;
                SCMT.SCMT_Alert = true;
                if (sunoconsavv != -1)
                {
                    SoundManager.Play(sunoconsavv, 1.0, 1.0, false);
                }
                seqScarico = 0;
            }
        }
    }
}