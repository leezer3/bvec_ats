using System;
using OpenBveApi.Runtime;
using System.Drawing;
using Microsoft.Win32;

namespace Plugin
{
    //The traction manager operates panel variables common to all traction types, and handles power & brake requests
    internal class tractionmanager : Device
    {
        // --- members ---
        internal static bool powercutoffdemanded;
        private static bool brakedemanded;
        private static bool neutralrvrtripped;
        internal static bool overheated;
        internal bool canisolate;
        internal bool safetyisolated;
        internal double travelled;
        internal int travel100;
        internal int travel10;
        internal int travel1;
        internal int travel01;
        internal int travel001;
        internal bool primaryklaxonplaying;
        internal bool secondaryklaxonplaying;
        internal bool musicklaxonplaying;
        internal double klaxonindicatortimer;
        internal bool doorlock;

        //Debug/ Advanced Driving Window Functions
        //
        //Is it currently visible?
        internal static bool debugwindowshowing;
        /** <summary><para>A string array containing the information to be passed to the debug window as follows:</para>
         * <para>1. Plugin debug message</para>
         * <para>2. Steam locomotive boiler pressure</para>
         * <para>3. Steam locomotive pressure generation rate</para>
         * <para>4. Steam locomotice pressure usage rate</para>
         * <para>5. Steam locomotive current cutoff</para>
         * <para>6. Steam locomotive optimal cutoff</para>
         * <para>7. Steam locomotive fire mass</para>
         * <para>8. Steam locomotive fire temperature</para>
         * <para>9. Steam locomotive injectors state</para>
         * <para>10. Steam locomotive blowers state</para>
         * <para>11. Steam locomotive boiler water levels</para>
         * <para>12. Steam locomotive tanks water levels</para>
         * <para>13. Steam locomotive automatic cutoff state</para>
         * <para>14. Train speed</para></summary> */
        //These will probably be renumbered at some stage....

        public static string[] debuginformation = new string[20];
        public static int tractiontype;

        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        //Default Variables
        internal int doorpowerlock = 0;
        internal int doorapplybrake = 0;
        internal int neutralrvrbrake = 0;
        internal int neutralrvrbrakereset = 0;
        internal int directionindicator = -1;
        internal int reverserindex = -1;
        internal int travelmeter100 = -1;
        internal int travelmeter10 = -1;
        internal int travelmeter1 = -1;
        internal int travelmeter01 = -1;
        internal int travelmeter001 = -1;
        internal int travelmetermode = 0;
        internal string klaxonindicator = "-1";
        internal string customindicators = "-1";

        //Default Key Assignments
        //Keys Down
        internal string safetykey = "A1";
        internal string automatickey = "A2";
        internal string injectorkey = "B2";
        internal string cutoffdownkey = "C1";
        internal string cutoffupkey = "C2";
        internal string fuelkey = "I";
        internal string wiperspeedup = "J";
        internal string wiperspeeddown = "K";
        internal string isolatesafetykey = "L";

        //Keys Up
        internal string gearupkey = "B1";
        internal string geardownkey = "B2";
        internal string DRAkey = "S";

        //Custom Indicators
        internal string customindicatorkey1 = "D";
        internal string customindicatorkey2 = "E";
        internal string customindicatorkey3 = "F";
        internal string customindicatorkey4 = "G";
        internal string customindicatorkey5 = "H";
        internal string customindicatorkey6 = "";
        internal string customindicatorkey7 = "";
        internal string customindicatorkey8 = "";
        internal string customindicatorkey9 = "";
        internal string customindicatorkey10 = "";
        internal string frontpantographkey;
        internal string rearpantographkey;
        internal string shovellingkey;
        internal string blowerskey;
        internal string advancedrivingkey;
        internal string steamheatincreasekey;
        internal string steamheatdecreasekey;


        //Arrays
        int[] klaxonarray;
        int[,] customindicatorsarray;

        internal tractionmanager(Train train) {
			this.Train = train;
                       
		}
		
		//<param name="mode">The initialization mode.</param>
		internal override void Initialize(InitializationModes mode) {
            try
            {
                //Split klaxon indicators and timings into an array
                string[] splitklaxonindicator = klaxonindicator.Split(',');
                klaxonarray = new int[5];
                for (int i = 0; i < 5; i++)
                {
                    if (i < splitklaxonindicator.Length)
                    {
                        //If we have a value parse it
                        klaxonarray[i] = Int32.Parse(splitklaxonindicator[i]);
                    }
                    else
                    {
                        //Otherwise return 500- Default klaxon timing in ms
                        //Stops a complicated null check being required later
                        klaxonarray[i] = 500;
                    }
                }
            }
            catch
            {
                InternalFunctions.LogError("klaxonindicator");
            }
            try
            {
                //Split custom indicators into an array
                string[] splitcustomindicators = customindicators.Split(',');
                customindicatorsarray = new int[10, 2];
                for (int i = 0; i < 9; i++)
                {
                    //Parse the panel value and set second array value to false
                    if (i < splitcustomindicators.Length)
                    {
                        customindicatorsarray[i, 0] = Int32.Parse(splitcustomindicators[i]);
                        customindicatorsarray[i, 1] = 0;
                    }
                    else
                    {
                        customindicatorsarray[i, 0] = -1;
                        customindicatorsarray[i, 1] = 0;
                    }
                }
            }
            catch
            {
                InternalFunctions.LogError("customindicators");
            }
            //Set traction type to pass to debug window
            if (Train.steam != null)
            {
                tractiontype = 0;
            }
            else if (Train.diesel != null)
            {
                tractiontype = 1;
            }
            else
            {
                tractiontype = 2;
            }

		}

        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            //Door interlocks; Fitted to all trains
            if (this.Train.Doors > 0)
            {
                if (doorpowerlock == 1 && tractionmanager.powercutoffdemanded == false)
                {
                    tractionmanager.demandpowercutoff();
                    data.DebugMessage = "Power cutoff demanded by open doors";
                    doorlock = true;
                }

                if (doorapplybrake == 1 && tractionmanager.brakedemanded == false)
                {
                    tractionmanager.demandbrakeapplication();
                    data.DebugMessage = "Brakes demanded by open doors";
                    doorlock = true;
                }
                
            }
            else
            {
                if ((tractionmanager.powercutoffdemanded == true || tractionmanager.brakedemanded == true) && doorlock == true)
                {
                    tractionmanager.resetpowercutoff();
                    tractionmanager.resetbrakeapplication();
                    doorlock = false;
                }

            }
            //Reverser change brake behaviour
            if (neutralrvrbrake != 0)
            {
                if (neutralrvrbrake == 1)
                {
                    if (Train.Handles.Reverser == 0)
                    {
                        tractionmanager.demandbrakeapplication();
                        tractionmanager.neutralrvrtripped = true;
                    }
                }
                else if (neutralrvrbrake == 2)
                {
                    if (Train.Handles.Reverser == 0)
                    {
                        tractionmanager.demandbrakeapplication();
                        tractionmanager.neutralrvrtripped = true;
                    }
                }

                if (neutralrvrtripped == true)
                {
                    //OS_ATS Default behaviour
                    if (neutralrvrbrakereset == 0 && Train.Handles.Reverser != 0)
                    {
                        tractionmanager.resetbrakeapplication();
                        tractionmanager.neutralrvrtripped = false;
                    }
                    //Behaviour 1- Train must come to a full stand before brakes are reset
                    if (neutralrvrbrakereset == 1 && Train.trainspeed == 0)
                    {
                        tractionmanager.resetbrakeapplication();
                        tractionmanager.neutralrvrtripped = false;
                    }
                    //Behaviour 2- Train must come to a full stand and driver applies full service brakes before reset
                    if (neutralrvrbrakereset == 2 && Train.Handles.BrakeNotch == this.Train.Specs.BrakeNotches && Train.trainspeed == 0)
                    {
                        tractionmanager.resetbrakeapplication();
                        tractionmanager.neutralrvrtripped = false;
                    }
                }
            }
            //Insufficient steam boiler pressure
            //Debug messages need to be called via the traction manager to be passed to the debug window correctly
            if (Train.steam != null && Train.steam.stm_power == 0 && Train.steam.stm_boilerpressure < Train.steam.boilerminpressure)
            {
                data.DebugMessage = "Power cutoff due to boiler pressure below minimum";
            }

            if (tractionmanager.powercutoffdemanded == true)
            {
                if (Train.drastate == true)
                {
                    data.DebugMessage = "Power cutoff demanded by DRA Appliance";
                }
                else if (electric.powergap == true)
                {
                    if (Train.electric.FrontPantographState != electric.PantographStates.OnService && Train.electric.RearPantographState != electric.PantographStates.OnService)
                    {
                        data.DebugMessage = "Power cutoff due to no available pantographs";
                    }
                    else
                    {
                        data.DebugMessage = "Power cutoff demanded by electric conductor power gap";
                    }
                }
                else if (electric.breakertripped == true)
                {
                    if (Train.electric.FrontPantographState != electric.PantographStates.OnService && Train.electric.RearPantographState != electric.PantographStates.OnService)
                    {
                        data.DebugMessage = "Power cutoff demanded by ACB/VCB not turned on";
                    }
                    else
                    {
                        data.DebugMessage = "Power cutoff demanded by ACB/VCB tripping for neutral section";
                    }
                }
                else if (doorlock == true)
                {
                    data.DebugMessage = "Power cutoff demanded by open doors";
                }
                else
                {
                    data.DebugMessage = "Power cutoff demanded by AWS";
                }
                data.Handles.PowerNotch = 0;
            }

            if (tractionmanager.brakedemanded == true)
            {
                if (Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerExpired)
                {
                    data.DebugMessage = "EB Brakes demanded by AWS System";
                    data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches + 1;
                }
                else if (Train.overspeedtripped == true)
                {
                    data.DebugMessage = "Service Brakes demanded by overspeed device";
                    data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches;
                }
                else if (Train.vigilance.DeadmansHandleState == vigilance.DeadmanStates.BrakesApplied)
                {
                    data.DebugMessage = "EB Brakes demanded by deadman's handle";
                    data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches + 1;
                }
                else if (Train.TPWS.SafetyState == TPWS.SafetyStates.TssBrakeDemand || Train.TPWS.SafetyState == TPWS.SafetyStates.BrakeDemandAcknowledged || Train.TPWS.SafetyState == TPWS.SafetyStates.BrakesAppliedCountingDown)
                {
                    data.DebugMessage = "EB Brakes demanded by TPWS Device";
                    data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches + 1;
                }
                else if (doorlock == true)
                {
                    data.DebugMessage = "Service Brakes demanded by open doors";
                    data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches;
                }
                else if (tractionmanager.neutralrvrtripped)
                {
                    if (neutralrvrbrake == 1)
                    {
                        data.DebugMessage = "Service Brakes demanded by neutral reverser";
                        data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches;
                    }
                    else if (neutralrvrbrake == 2)
                    {
                        data.DebugMessage = "EB Brakes demanded by neutral reverser";
                        data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches + 1;
                    }
                }
                else
                {
                    data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches + 1;
                }
            }
            else
            {
                //Workaround for brakes not released correctly if driver attempts to release brakes before
                //brake cutoff is released
                if (Train.Handles.BrakeNotch == 0)
                {
                    data.Handles.BrakeNotch = 0;
                }
            }
            //Independant Panel Variables
            if (directionindicator != -1)
            {
                //Direction Indicator
                this.Train.Panel[directionindicator] = this.Train.direction;
            }
            if (reverserindex != -1)
            {
                //Reverser Indicator
                if (Train.Handles.Reverser == 0)
                {
                    this.Train.Panel[reverserindex] = 0;
                }
                else if (Train.Handles.Reverser == 1)
                {
                    this.Train.Panel[reverserindex] = 1;
                }
                else
                {
                    this.Train.Panel[reverserindex] = 2;
                }
            }
            {
                //Travel Meter
                if (travelmetermode == 0)
                {
                    travelled += (Train.trainlocation - Train.previouslocation);
                }
                else
                {
                    travelled += ((Train.trainlocation - Train.previouslocation) / 0.621);
                }
                if (travelled > 1)
                {
                    travel001++;
                    travelled = 0.0;
                }
                if (travel001 > 9)
                {
                    travel01++;
                    travel001 = 0;
                }
                if (travel01 > 9)
                {
                    travel1++;
                    travel01 = 0;
                }
                if (travel1 > 9)
                {
                    travel10++;
                    travel1 = 0;
                }
                if (travel10 > 9)
                {
                    travel100++;
                    travel10 = 0;
                }

                //100km
                if (travelmeter100 != -1)
                {
                    this.Train.Panel[travelmeter100] = travel100;

                }
                //10km
                if (travelmeter10 != -1)
                {
                    this.Train.Panel[travelmeter10] = travel10;

                }
                //1km
                if (travelmeter1 != -1)
                {
                    this.Train.Panel[travelmeter1] = travel1;

                }
                //100m
                if (travelmeter01 != -1)
                {
                    this.Train.Panel[travelmeter01] = travel01;

                }
                //1m
                if (travelmeter001 != -1)
                {
                    this.Train.Panel[travelmeter001] = travel001;

                }
            }
            {
                //Horn Indicators
                if (primaryklaxonplaying == true || secondaryklaxonplaying == true || musicklaxonplaying == true)
                {
                    klaxonindicatortimer += data.ElapsedTime.Milliseconds;
                    //Primary Horn
                    if (primaryklaxonplaying == true && klaxonarray[0] != -1)
                    {
                        this.Train.Panel[klaxonarray[0]] = 1;
                        if (klaxonindicatortimer > klaxonarray[1])
                        {
                            primaryklaxonplaying = false;
                            klaxonindicatortimer = 0.0;
                        }
                    }
                    //Secondary Horn
                    else if (secondaryklaxonplaying == true && klaxonarray[2] != 500)
                    {
                        this.Train.Panel[klaxonarray[2]] = 1;
                        if (klaxonindicatortimer > klaxonarray[3])
                        {
                            secondaryklaxonplaying = false;
                            klaxonindicatortimer = 0.0;
                        }
                    }
                    //Music Horn
                    else if (musicklaxonplaying == true && klaxonarray[4] != 500)
                    {
                        this.Train.Panel[klaxonarray[4]] = 1;
                        if (klaxonindicatortimer > klaxonarray[5])
                        {
                            musicklaxonplaying = false;
                            klaxonindicatortimer = 0.0;
                        }
                    }
                }
                else
                {
                    this.Train.Panel[klaxonarray[0]] = 0;
                    if (klaxonarray[2] != 500)
                    {
                        this.Train.Panel[klaxonarray[2]] = 0;
                    }
                    if (klaxonarray[4] != 500)
                    {
                        this.Train.Panel[klaxonarray[4]] = 0;
                    }
                }
            }
            {
                //Custom Indicators
                for (int i = 0; i < customindicatorsarray.GetLength(0); i++)
                {
                    if (customindicatorsarray[i, 0] != -1)
                    {
                        Train.Panel[customindicatorsarray[i, 0]] = customindicatorsarray[i, 1];
                    }
                }
            }

            //Handles the debug/ advanced driving window
            if (debugwindowshowing == true)
            {
                if (AdvancedDriving.CheckInst == null)
                {
                    AdvancedDriving.CreateInst.Show();  // This creates and displays Form2
                    
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\BVEC_ATS", true))
                    {
                        if (key != null)
                        {
                            AdvancedDriving.CreateInst.Left = (int)key.GetValue("Left");
                            AdvancedDriving.CreateInst.Top = (int)key.GetValue("Top");
                        }
                        else
                        {
                            AdvancedDriving.CreateInst.Location = new Point(100, 100);
                        }
                    }
                }
                else
                {
                    debuginformation[0] = data.DebugMessage;
                    debuginformation[13] = Convert.ToString(Train.trainspeed) + " km/h";
                    AdvancedDriving.CreateInst.Elapse(debuginformation, tractiontype);
                }
            }
            else
            {
                if (AdvancedDriving.CheckInst != null)
                {
                    AdvancedDriving.CreateInst.Close();
                }
            }
            
        }

        //Call this function from a safety system to demand power cutoff
        
        internal static void demandpowercutoff()
        {
            tractionmanager.powercutoffdemanded = true;
        }

        //Call this function to reset the power cutoff
        internal static void resetpowercutoff()
        {
            tractionmanager.powercutoffdemanded = false;
        }

        //Call this function from a safety system to demand a brake application
        /// <summary>Unconditionally demands a brake application from the traction manager.</summary>
        internal static void demandbrakeapplication()
        {
            tractionmanager.brakedemanded = true;
        }

        //Call this function from a safety system to reset a brake application
        /// <summary>Unconditionally resets a brake application from the traction manager.</summary>
        internal static void resetbrakeapplication()
        {

            tractionmanager.brakedemanded = false;
        }

        //Call this function to attempt to isolate or re-enable the TPWS & AWS Systems
        /// <summary>Attempts to disable or re-enable the TPWS & AWS safety systems.</summary>
        internal void isolatetpwsaws()
        {
            if (safetyisolated == false)
            {
                //First check if TPWS is enabled in this train [AWS must therefore be enabled]
                if (Train.TPWS.enabled == true)
                {
                    if (Train.TPWS.SafetyState == TPWS.SafetyStates.None && (Train.AWS.SafetyState == AWS.SafetyStates.Clear || Train.AWS.SafetyState == AWS.SafetyStates.None))
                    {
                        canisolate = true;
                    }
                }
                else if (Train.TPWS.enabled == false && Train.AWS.enabled == true)
                {
                    if (Train.AWS.SafetyState == AWS.SafetyStates.Clear || Train.AWS.SafetyState == AWS.SafetyStates.None)
                    {
                        canisolate = true;
                    }
                }

                if (canisolate == true)
                {
                    if (Train.TPWS.enabled == true)
                    {
                        Train.TPWS.Isolate();
                    }
                    if (Train.AWS.enabled == true)
                    {
                        Train.AWS.Isolate();
                    }
                    safetyisolated = true;
                }
            }
        }

        /// <summary>Enables the AWS & TPWS systems if they are fitted.</summary>
        internal void reenabletpwsaws()
        {
            if (safetyisolated == true)
            {
                if (Train.AWS.enabled == true)
                {
                    Train.AWS.Reset();
                    safetyisolated = false;
                }
                if (Train.TPWS.enabled == true)
                {
                    Train.TPWS.Reset();
                    safetyisolated = false;
                }
            }
        }

        /// <summary>Is called when a key is pressed.</summary>
        /// <param name="key">The key.</param>
        internal override void KeyDown(VirtualKeys key)
        {
            //Convert keypress to string for comparison
            string keypressed = Convert.ToString(key);
            {
                if (Train.vigilance.DeadmansHandleState != vigilance.DeadmanStates.BrakesApplied && Train.vigilance.independantvigilance == 0)
                {
                    //Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
                    Train.vigilance.deadmanstimer = 0.0;
                    SoundManager.Stop(Train.vigilance.vigilancealarm);
                    Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
                }
            }

            if (keypressed == automatickey)
            {
                //Toggle Automatic Cutoff/ Gears
                if (Train.steam != null)
                {
                    if (Train.steam.automatic != -1)
                    {
                        Train.steam.automatic = -1;
                    }
                    else
                    {
                        Train.steam.automatic = 0;
                    }
                }
                else if (Train.diesel != null)
                {
                    if (Train.diesel.automatic != -1)
                    {
                        Train.diesel.automatic = -1;
                    }
                    else
                    {
                        Train.diesel.automatic = 0;
                    }
                }
            }
            if (keypressed == advancedrivingkey)
            {
                if (debugwindowshowing == false)
                {
                    debugwindowshowing = true;
                }
                else
                {
                    debugwindowshowing = false;
                }
            }
            if (keypressed == injectorkey)
            {
                //Injectors
                if (Train.steam != null)
                {
                    if (Train.steam.stm_injector == true)
                    {
                        Train.steam.stm_injector = false;
                    }
                    else
                    {
                        Train.steam.stm_injector = true;
                    }
                }
            }
            if (keypressed == cutoffdownkey)
            {
                //Cutoff Up
                if (Train.steam != null)
                {
                    Train.steam.cutoffstate = 1;
                }
            }
            if (keypressed == cutoffupkey)
            {
                //Cutoff Down
                if (Train.steam != null)
                {
                    Train.steam.cutoffstate = -1;
                }
            }
            if (keypressed == safetykey)
            {
                //Reset Overspeed Trip
                if ((Train.trainspeed == 0 || Train.vigilance.vigilancecancellable != 0) && Train.overspeedtripped == true)
                {
                    Train.overspeedtripped = false;
                    resetbrakeapplication();
                }

                //Reset deadman's handle if independant vigilance is selected & brakes have not been applied
                if (Train.vigilance.independantvigilance != 0 && Train.vigilance.DeadmansHandleState != vigilance.DeadmanStates.BrakesApplied)
                {
                    Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
                    Train.vigilance.deadmanstimer = 0.0;
                    SoundManager.Stop(Train.vigilance.vigilancealarm);
                    resetbrakeapplication();
                }
                //Reset brakes if allowed
                else if (Train.vigilance.vigilancecancellable != 0 && Train.vigilance.DeadmansHandleState == vigilance.DeadmanStates.BrakesApplied)
                {
                    Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
                    Train.vigilance.deadmanstimer = 0.0;
                    SoundManager.Stop(Train.vigilance.vigilancealarm);
                }
                //If brakes cannot be cancelled and we've stopped
                else if (Train.vigilance.vigilancecancellable == 0 && Train.trainspeed == 0 && Train.vigilance.DeadmansHandleState == vigilance.DeadmanStates.BrakesApplied)
                {
                    Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
                    Train.vigilance.deadmanstimer = 0.0;
                    SoundManager.Stop(Train.vigilance.vigilancealarm);
                    resetbrakeapplication();
                }
                
                
                //Acknowledge AWS warning
                if (Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerActive)
                {
                    Train.AWS.Acknowlege();
                }

                //Reset AWS
                if (Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerExpired && Train.trainspeed == 0 && Train.Handles.Reverser == 0)
                {
                    if (SoundManager.IsPlaying(Train.AWS.awswarningsound))
                    {
                        SoundManager.Stop(Train.AWS.awswarningsound);
                    }
                    Train.AWS.Reset();
                    resetpowercutoff();
                }

                //Acknowledge TPWS Brake Demand
                if (Train.TPWS.SafetyState == TPWS.SafetyStates.TssBrakeDemand)
                {
                    Train.TPWS.AcknowledgeBrakeDemand();
                }

                //Acknowledge Self-Test warning
                if (Train.StartupSelfTestManager.SequenceState == StartupSelfTestManager.SequenceStates.AwaitingDriverInteraction)
                {
                    Train.StartupSelfTestManager.driveracknowledge();
                }
            }
            if (keypressed == fuelkey)
            {
                //Toggle Fuel fill
                if (Train.canfuel == true && Train.trainspeed == 0)
                {
                    if (Train.steam != null)
                    {
                        Train.steam.fuelling = true;
                    }
                    if (Train.diesel != null)
                    {
                        Train.diesel.fuelling = true;
                    }
                }
                //ACB/ VCB toggle
                if (Train.electric != null)
                {
                    electric.breakertrip();
                }
            }
            if (keypressed == wiperspeeddown)
            {
                //Wipers Speed Down
                if (Train.Windscreen.enabled == true)
                {
                    Train.Windscreen.windscreenwipers(1);
                }
            }
            if (keypressed == wiperspeedup)
            {
                //Wipers Speed Up
                if (Train.Windscreen.enabled == true)
                {
                    Train.Windscreen.windscreenwipers(0);
                }
            }
            if (keypressed == isolatesafetykey)
            {
                //Isolate Safety Systems
                if (safetyisolated == false)
                {
                    isolatetpwsaws();
                }
                else
                {
                    reenabletpwsaws();
                }
            }
            //Toggle Pantographs
            if (keypressed == frontpantographkey)
            {
                Train.electric.pantographtoggle(0);
            }
            if (keypressed == rearpantographkey)
            {
                Train.electric.pantographtoggle(1);
            }
            //Advanced steam locomotive functions
            //Blowers
            if (keypressed == blowerskey)
            {
                if (Train.steam.blowers == false)
                {
                    Train.steam.blowers = true;
                }
                else
                {
                    Train.steam.blowers = false;
                }
            }
            //Shovel Coal
            if (keypressed == shovellingkey)
            {
                if (Train.steam.shovelling == false)
                {
                    Train.steam.shovelling = true;
                }
                else
                {
                    Train.steam.shovelling = false;
                }
            }
            if (keypressed == customindicatorkey1)
            {
                //Toggle Custom Indicator 1
                if (customindicatorsarray[0, 0] != -1)
                {
                    if (customindicatorsarray[0, 1] == 0)
                    {
                        customindicatorsarray[0, 1] = 1;
                    }
                    else
                    {
                        customindicatorsarray[0, 1] = 0;
                    }
                }
            }
            if (keypressed == customindicatorkey2)
            {
                //Toggle Custom Indicator 1
                if (customindicatorsarray[1, 0] != -1)
                {
                    if (customindicatorsarray[1, 1] == 0)
                    {
                        customindicatorsarray[1, 1] = 1;
                    }
                    else
                    {
                        customindicatorsarray[1, 1] = 0;
                    }
                }
            }
            if (keypressed == customindicatorkey3)
            {
                //Toggle Custom Indicator 1
                if (customindicatorsarray[2, 0] != -1)
                {
                    if (customindicatorsarray[2, 1] == 0)
                    {
                        customindicatorsarray[2, 1] = 1;
                    }
                    else
                    {
                        customindicatorsarray[2, 1] = 0;
                    }
                }
            }
            if (keypressed == customindicatorkey4)
            {
                //Toggle Custom Indicator 1
                if (customindicatorsarray[3, 0] != -1)
                {
                    if (customindicatorsarray[3, 1] == 0)
                    {
                        customindicatorsarray[3, 1] = 1;
                    }
                    else
                    {
                        customindicatorsarray[3, 1] = 0;
                    }
                }
            }
            if (keypressed == customindicatorkey5)
            {
                //Toggle Custom Indicator 1
                if (customindicatorsarray[4, 0] != -1)
                {
                    if (customindicatorsarray[4, 1] == 0)
                    {
                        customindicatorsarray[4, 1] = 1;
                    }
                    else
                    {
                        customindicatorsarray[4, 1] = 0;
                    }
                }
            }
            if (keypressed == customindicatorkey6)
            {
                //Toggle Custom Indicator 1
                if (customindicatorsarray[5, 0] != -1)
                {
                    if (customindicatorsarray[5, 1] == 0)
                    {
                        customindicatorsarray[5, 1] = 1;
                    }
                    else
                    {
                        customindicatorsarray[5, 1] = 0;
                    }
                }
            }
            if (keypressed == customindicatorkey7)
            {
                //Toggle Custom Indicator 1
                if (customindicatorsarray[6, 0] != -1)
                {
                    if (customindicatorsarray[6, 1] == 0)
                    {
                        customindicatorsarray[6, 1] = 1;
                    }
                    else
                    {
                        customindicatorsarray[6, 1] = 0;
                    }
                }
            }
            if (keypressed == customindicatorkey8)
            {
                //Toggle Custom Indicator 1
                if (customindicatorsarray[7, 0] != -1)
                {
                    if (customindicatorsarray[7, 1] == 0)
                    {
                        customindicatorsarray[7, 1] = 1;
                    }
                    else
                    {
                        customindicatorsarray[7, 1] = 0;
                    }
                }
            }
            if (keypressed == customindicatorkey9)
            {
                //Toggle Custom Indicator 1
                if (customindicatorsarray[8, 0] != -1)
                {
                    if (customindicatorsarray[8, 1] == 0)
                    {
                        customindicatorsarray[8, 1] = 1;
                    }
                    else
                    {
                        customindicatorsarray[8, 1] = 0;
                    }
                }
            }
            if (keypressed == customindicatorkey10)
            {
                //Toggle Custom Indicator 1
                if (customindicatorsarray[9, 0] != -1)
                {
                    if (customindicatorsarray[9, 1] == 0)
                    {
                        customindicatorsarray[9, 1] = 1;
                    }
                    else
                    {
                        customindicatorsarray[9, 1] = 0;
                    }
                }
            }
            if (keypressed == steamheatincreasekey)
            {
                if (steam.steamheatlevel < 5)
                {
                    steam.steamheatlevel++;
                }
            }
            if (keypressed == steamheatdecreasekey)
            {
                if (steam.steamheatlevel > 0)
                {
                    steam.steamheatlevel--;
                }
            }
        }

        internal override void KeyUp(VirtualKeys key)
        {
            //Convert keypress to string for comparison
            string keypressed = Convert.ToString(key);
            if (keypressed == gearupkey)
            {
                //Gear Up
                if (Train.diesel != null)
                {
                    if (Train.diesel.gear >= 0 && Train.diesel.gear < Train.diesel.totalgears - 1 && Train.Handles.PowerNotch == 0)
                    {
                        Train.diesel.gear++;
                        Train.diesel.gearloop = false;
                        Train.diesel.gearlooptimer = 0.0;
                        Train.diesel.gearchange();
                    }
                }
            }
            if (keypressed == geardownkey)
            {
                //Gear Down
                if (Train.diesel != null)
                {
                    if (Train.diesel.gear <= Train.diesel.totalgears && Train.diesel.gear > 0 && Train.Handles.PowerNotch == 0)
                    {
                        Train.diesel.gear--;
                        Train.diesel.gearloop = false;
                        Train.diesel.gearlooptimer = 0.0;
                        Train.diesel.gearchange();
                    }
                }
            }
            if (keypressed == cutoffupkey)
            {
                //Cutoff Up
                if (Train.steam != null)
                {
                    Train.steam.cutoffstate = 0;
                }
            }
            if (keypressed == cutoffdownkey)
            {
                //Cutoff Down
                if (Train.steam != null)
                {
                    Train.steam.cutoffstate = 0;
                }
            }
            if (keypressed == fuelkey)
            {
                //Toggle Fuel fill
                if (Train.steam != null)
                {
                    Train.steam.fuelling = false;
                }
                if (Train.diesel != null)
                {
                    Train.diesel.fuelling = false;
                }
            }
            if (keypressed == DRAkey)
            {
                if (Train.vigilance.draenabled != -1)
                {
                    //Operate DRA
                    if (Train.drastate == false)
                    {
                        Train.drastate = true;
                        demandpowercutoff();
                    }
                    else
                    {
                        Train.drastate = false;
                        resetpowercutoff();
                    }
                }
            }
        }

        /// <summary>Is called when the driver changes the reverser.</summary>
        /// <param name="reverser">The new reverser position.</param>
        internal override void SetReverser(int reverser)
        {
            if (Train.vigilance.DeadmansHandleState != vigilance.DeadmanStates.BrakesApplied && Train.vigilance.independantvigilance == 0)
            {
                //Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
                Train.vigilance.deadmanstimer = 0.0;
                SoundManager.Stop(Train.vigilance.vigilancealarm);
                Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
            }

        }

        /// <summary>Is called when the driver changes the power notch.</summary>
        /// <param name="powerNotch">The new power notch.</param>
        internal override void SetPower(int powerNotch)
        {
            if (Train.vigilance.DeadmansHandleState != vigilance.DeadmanStates.BrakesApplied && Train.vigilance.independantvigilance == 0)
            {
                //Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
                Train.vigilance.deadmanstimer = 0.0;
                SoundManager.Stop(Train.vigilance.vigilancealarm);
                Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
            }
            //Trigger electric powerloop sound timer
            if (Train.electric != null)
            {
                electric.powerloop = false;
                Train.electric.powerlooptimer = 0.0;
            }
        }

        /// <summary>Is called when the driver changes the brake notch.</summary>
        /// <param name="brakeNotch">The new brake notch.</param>
        internal override void SetBrake(int brakeNotch)
        {
            if (Train.vigilance.DeadmansHandleState != vigilance.DeadmanStates.BrakesApplied && Train.vigilance.independantvigilance == 0)
            {
                //Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
                Train.vigilance.deadmanstimer = 0.0;
                SoundManager.Stop(Train.vigilance.vigilancealarm);
                Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
            }
        }

        internal override void HornBlow(HornTypes type)
        {
            if (Train.vigilance.DeadmansHandleState != vigilance.DeadmanStates.BrakesApplied && Train.vigilance.independantvigilance == 0)
            {
                //Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
                Train.vigilance.deadmanstimer = 0.0;
                SoundManager.Stop(Train.vigilance.vigilancealarm);
                Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
            }
            //Set the horn type that is playing
            if (type == HornTypes.Primary)
            {
                this.primaryklaxonplaying = true;
            }
            else if (type == HornTypes.Secondary)
            {
                this.secondaryklaxonplaying = true;
            }
            else
            {
                this.musicklaxonplaying = true;
            }
            
        }
    }
}
