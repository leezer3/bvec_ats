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
        /// <summary>Power cutoff has been demaned</summary>
        internal static bool powercutoffdemanded;
        /// <summary>A safety system has triggered a brake intervention</summary>
        internal bool brakedemanded;
        /// <summary>The current safety system brake notch demanded</summary>
        internal static int currentbrakenotch;
        private static bool neutralrvrtripped;
        /// <summary>The engine has overheated</summary>
        internal static bool overheated;
        internal bool canisolate;
        internal bool safetyisolated;
        /// <summary>The total distance travelled</summary>
        internal double travelled;
        /// <summary>The 100km digit of the travel meter</summary>
        internal int travel100;
        /// <summary>The 10km digit of the travel meter</summary>
        internal int travel10;
        /// <summary>The 100m digit of the travel meter</summary>
        internal int travel1;
        /// <summary>The 10m digit of the travel meter</summary>
        internal int travel01;
        /// <summary>The 1m digit of the travel meter</summary>
        internal int travel001;
        /// <summary>Stores whether the primary klaxon is playing</summary>
        internal bool primaryklaxonplaying;
        /// <summary>Stores whether the secondary klaxon is playing</summary>
        internal bool secondaryklaxonplaying;
        /// <summary>Stores whether the music klaxon is playing</summary>
        internal bool musicklaxonplaying;
        internal double klaxonindicatortimer;
        /// <summary>Stores whether the door state is triggering power cutoff or a brake intervention</summary>
        internal bool doorlock;

        internal bool independantreset;
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
         * <para>14. Train speed</para>
         * <para>15. Front pantograph state</para>
         * <para>16. Rear pantograph state</para>
         * <para>17. ACB/VCB state</para>
         * <para>18. Line Volts</para></summary> */
        //These will probably be renumbered at some stage....

        internal AdvancedDrivingData DebugWindowData = new AdvancedDrivingData();
        

        public static string[] debuginformation = new string[30];
        public static int tractiontype;

        /// <summary>The underlying train.</summary>
        private readonly Train Train;

        //Default Variables
        /// <summary>The panel index lit when the door state is cutting off the power</summary>
        internal int doorpowerlock = 0;
        /// <summary>The panel index lit when the door state is applying a brake intervention</summary>
        internal int doorapplybrake = 0;
        internal int neutralrvrbrake = 0;
        internal int neutralrvrbrakereset = 0;
        /// <summary>The panel index of the direction indicator</summary>
        internal int directionindicator = -1;
        /// <summary>The panel index of the reverser handle</summary>
        /// Whilst OpenBVE provides a native implementation for this, the actual reverser handle may be altered
        /// for example by the plugin's cutoff state
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
        internal string headcodekey = "";
        //NEW KEYS ADDED FOR BVEC_ATS

        //Front/ rear pantographs
        internal string frontpantographkey;
        internal string rearpantographkey;
        //Steam locomotive functions
        internal string shovellingkey;
        internal string blowerskey;
        internal string advancedrivingkey;
        internal string steamheatincreasekey;
        internal string steamheatdecreasekey;
        internal string cylindercockskey;

        //KEYS ADDED BY OS_SZ_ATS

        internal string SCMTincreasespeed;
        internal string SCMTdecreasespeed;
        internal string AbilitaBancoKey;
        internal string ConsensoAvviamentoKey;
        internal string AvviamentoKey;
        internal string SpegnimentoKey;
        internal string LCMupKey;
        internal string LCMdownkey;
        internal string TestSCMTKey;
        //These used to use safetykey && tpwsresetkey
        //tpwsreset doesn't exist, so move to their own key assignments
        internal string vigilantekey;
        internal string vigilanteresetkey;

        //CAWS Key
        internal string CAWSKey = "S";

        //PZB Keys
        internal string PZBKey;
        internal string PZBReleaseKey;
        internal string PZBStopOverrideKey;


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

                if (doorapplybrake == 1 && brakedemanded == false)
                {
                    Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches);
                    data.DebugMessage = "Brakes demanded by open doors";
                    doorlock = true;
                }
                
            }
            else
            {
                if ((tractionmanager.powercutoffdemanded == true || brakedemanded == true) && doorlock == true)
                {
                    tractionmanager.resetpowercutoff();
                    Train.tractionmanager.resetbrakeapplication();
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
                        Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches);
                        tractionmanager.neutralrvrtripped = true;
                    }
                }
                else if (neutralrvrbrake == 2)
                {
                    if (Train.Handles.Reverser == 0)
                    {
                        Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches);
                        tractionmanager.neutralrvrtripped = true;
                    }
                }

                if (neutralrvrtripped == true)
                {
                    //OS_ATS Default behaviour
                    if (neutralrvrbrakereset == 0 && Train.Handles.Reverser != 0)
                    {
                        Train.tractionmanager.resetbrakeapplication();
                        tractionmanager.neutralrvrtripped = false;
                    }
                    //Behaviour 1- Train must come to a full stand before brakes are reset
                    if (neutralrvrbrakereset == 1 && Train.trainspeed == 0)
                    {
                        Train.tractionmanager.resetbrakeapplication();
                        tractionmanager.neutralrvrtripped = false;
                    }
                    //Behaviour 2- Train must come to a full stand and driver applies full service brakes before reset
                    if (neutralrvrbrakereset == 2 && Train.Handles.BrakeNotch == this.Train.Specs.BrakeNotches && Train.trainspeed == 0)
                    {
                        Train.tractionmanager.resetbrakeapplication();
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
                else if (Train.electric != null && Train.electric.powergap == true)
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
                else if (Train.electric != null && Train.electric.breakertripped == true)
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

            if (brakedemanded == true)
            {
                //Set brake notch demanded
                data.Handles.BrakeNotch = currentbrakenotch;
                //Update debug messages
                if (Train.AWS.enabled == true && Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerExpired)
                {
                    data.DebugMessage = "EB Brakes demanded by AWS System";
                }
                else if (Train.overspeedtripped == true)
                {
                    data.DebugMessage = "Service Brakes demanded by overspeed device";
                }
                else if (Train.vigilance != null)
                {
                    if (Train.vigilance.DeadmansHandleState == vigilance.DeadmanStates.BrakesApplied)
                    {
                        data.DebugMessage = "EB Brakes demanded by deadman's handle";
                    }
                    else if (Train.vigilance.VigilanteState == vigilance.VigilanteStates.EbApplied)
                    {
                        data.DebugMessage = "EB Brakes demanded by Vigilante Device";
                    }
                }
                else if (Train.TPWS != null && (Train.TPWS.SafetyState == TPWS.SafetyStates.TssBrakeDemand ||
                                                Train.TPWS.SafetyState == TPWS.SafetyStates.BrakeDemandAcknowledged ||
                                                Train.TPWS.SafetyState == TPWS.SafetyStates.BrakesAppliedCountingDown))
                {
                    data.DebugMessage = "EB Brakes demanded by TPWS Device";
                }
                else if (doorlock == true)
                {
                    data.DebugMessage = "Service Brakes demanded by open doors";
                }
                else if (tractionmanager.neutralrvrtripped)
                {
                    if (neutralrvrbrake == 1)
                    {
                        data.DebugMessage = "Service Brakes demanded by neutral reverser";
                    }
                    else if (neutralrvrbrake == 2)
                    {
                        data.DebugMessage = "EB Brakes demanded by neutral reverser";
                    }
                }
                else if (Train.SCMT.enabled == true && SCMT.EBDemanded == true)
                {
                    data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches + 1;
                }
                else if (Train.SCMT.enabled == true && SCMT_Traction.ConstantSpeedBrake == true)
                {
                    data.DebugMessage = "Brake Notch demanaded by SCMT Constant Speed Device";
                }
                else if (Train.CAWS.enabled == true)
                {
                    data.DebugMessage = "EB Brakes demanded by CAWS Safety System";
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
                        if (klaxonarray[0] != -1)
                        {
                            this.Train.Panel[klaxonarray[0]] = 1;
                        }
                        if (klaxonindicatortimer > klaxonarray[1])
                        {
                            primaryklaxonplaying = false;
                            klaxonindicatortimer = 0.0;
                        }
                    }
                    //Secondary Horn
                    else if (secondaryklaxonplaying == true && klaxonarray[2] != 500)
                    {
                        if (klaxonarray[2] != 500)
                        {
                            this.Train.Panel[klaxonarray[2]] = 1;
                        }
                        if (klaxonindicatortimer > klaxonarray[3])
                        {
                            secondaryklaxonplaying = false;
                            klaxonindicatortimer = 0.0;
                        }
                    }
                    //Music Horn
                    else if (musicklaxonplaying == true && klaxonarray[4] != 500)
                    {
                        if (klaxonarray[4] != 500)
                        {
                            this.Train.Panel[klaxonarray[4]] = 1;
                        }
                        if (klaxonindicatortimer > klaxonarray[5])
                        {
                            musicklaxonplaying = false;
                            klaxonindicatortimer = 0.0;
                        }
                    }
                }
                else
                {
                    if (klaxonarray[0] != -1)
                    {
                        this.Train.Panel[klaxonarray[0]] = 0;
                    }
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
                    AdvancedDriving.CreateInst.Elapse(debuginformation, tractiontype, DebugWindowData);
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
        /// <summary>Demands a brake application from the traction manager.</summary>
        /// <remarks>Takes the notch demanded as the paramater. If this notch is less than the current notch, do not change notch.</remarks>
        internal void demandbrakeapplication(int notchdemanded)
        {
            brakedemanded = true;
            if (notchdemanded > currentbrakenotch)
            {
                currentbrakenotch = notchdemanded;
            }
        }

        /// <summary>Attempts to reset a brake application from the traction manager.</summary>
        /// <remarks>If independantreset is enabled, then the conditions for reseting all safety systems must be met to release
        /// a brake application.
        /// The default OS_ATS behaviour is to reset all applications at once.</remarks>
        internal void resetbrakeapplication()
        {
            if (independantreset == true)
            {
                if (Train.AWS.enabled == true && Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerExpired)
                {
                    return;
                }
                if (Train.overspeedtripped == true)
                {
                    return;
                }
                if (Train.vigilance.DeadmansHandleState == vigilance.DeadmanStates.BrakesApplied)
                {
                    return;
                }
                if (Train.TPWS != null && (Train.TPWS.SafetyState == TPWS.SafetyStates.TssBrakeDemand ||
                                                Train.TPWS.SafetyState == TPWS.SafetyStates.BrakeDemandAcknowledged ||
                                                Train.TPWS.SafetyState == TPWS.SafetyStates.BrakesAppliedCountingDown))
                {
                    return;
                }
                if (doorlock == true)
                {
                    return;
                }
                if (tractionmanager.neutralrvrtripped && neutralrvrbrake == 2)
                {
                    return;
                }
                if (Train.SCMT.enabled == true && SCMT.EBDemanded == true)
                {
                    return;
                }
                if (Train.vigilance.VigilanteState == vigilance.VigilanteStates.EbApplied)
                {
                    return;
                }
                if (Train.CAWS.enabled == true && Train.CAWS.EmergencyBrakeCountdown < 0.0)
                {
                    return;
                }
                //These conditions set a different brake notch to EB
                //
                //Set service brakes as reverser is in neutral
                if (tractionmanager.neutralrvrtripped && neutralrvrbrake == 1)
                {
                    currentbrakenotch = Train.Specs.BrakeNotches;
                    return;
                }
                //Set brake notch 1 for SCMT constant speed device
                if (Train.SCMT.enabled == true && SCMT_Traction.ConstantSpeedBrake == true)
                {
                    currentbrakenotch = 1;
                    return;
                }
            }
            currentbrakenotch = 0;
            brakedemanded = false;
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
                if (Train.vigilance != null)
                {
                    if (Train.vigilance.DeadmansHandleState != vigilance.DeadmanStates.BrakesApplied &&
                        Train.vigilance.independantvigilance == 0)
                    {
                        //Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
                        Train.vigilance.deadmanstimer = 0.0;
                        SoundManager.Stop(Train.vigilance.vigilancealarm);
                        Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
                    }
                }
            }

            if (keypressed == automatickey)
            {
                //Toggle Automatic Cutoff/ Gears
                if (Train.steam != null)
                {
                    if (Train.steam.automatic == true)
                    {
                        Train.steam.automatic = false;
                    }
                    else
                    {
                        Train.steam.automatic = true;
                    }
                }
                else if (Train.diesel != null)
                {
                    if (Train.diesel.automatic == true)
                    {
                        Train.diesel.automatic = false;
                    }
                    else
                    {
                        Train.diesel.automatic = false;
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

            if (keypressed == safetykey)
            {
                if (Train.vigilance != null)
                {
                    //Reset Overspeed Trip
                    if ((Train.trainspeed == 0 || Train.vigilance.vigilancecancellable != 0) &&
                        Train.overspeedtripped == true)
                    {
                        Train.overspeedtripped = false;
                        resetbrakeapplication();
                    }


                    //Reset deadman's handle if independant vigilance is selected & brakes have not been applied
                    if (Train.vigilance.independantvigilance != 0 &&
                        Train.vigilance.DeadmansHandleState != vigilance.DeadmanStates.BrakesApplied)
                    {
                        Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
                        Train.vigilance.deadmanstimer = 0.0;
                        SoundManager.Stop(Train.vigilance.vigilancealarm);
                        resetbrakeapplication();
                    }
                        //Reset brakes if allowed
                    else if (Train.vigilance.vigilancecancellable != 0 &&
                             Train.vigilance.DeadmansHandleState == vigilance.DeadmanStates.BrakesApplied)
                    {
                        Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
                        Train.vigilance.deadmanstimer = 0.0;
                        SoundManager.Stop(Train.vigilance.vigilancealarm);
                    }
                        //If brakes cannot be cancelled and we've stopped
                    else if (Train.vigilance.vigilancecancellable == 0 && Train.trainspeed == 0 &&
                             Train.vigilance.DeadmansHandleState == vigilance.DeadmanStates.BrakesApplied)
                    {
                        Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
                        Train.vigilance.deadmanstimer = 0.0;
                        SoundManager.Stop(Train.vigilance.vigilancealarm);
                        resetbrakeapplication();
                    }
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
                    Train.electric.breakertrip();
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
            if (keypressed == headcodekey)
            {
                Animations.headcodetoggle();
            }
            //Advanced steam locomotive functions
            if (Train.steam != null)
            {
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
                if (keypressed == steamheatincreasekey)
                {
                    if (Train.steam.steamheatlevel < 5)
                    {
                        Train.steam.steamheatlevel++;
                    }
                }
                if (keypressed == steamheatdecreasekey)
                {
                    if (Train.steam.steamheatlevel > 0)
                    {
                        Train.steam.steamheatlevel--;
                    }
                }
                if (keypressed == cylindercockskey)
                {
                    if (Train.steam.cylindercocks == false)
                    {
                        Train.steam.cylindercocks = true;
                    }
                    else
                    {
                        Train.steam.cylindercocks = false;
                    }
                }
            }
            if (Train.SCMT_Traction.enabled == true)
            {
                if (keypressed == SCMTincreasespeed)
                {
                    SCMT_Traction.increasesetspeed();
                }
                if (keypressed == SCMTdecreasespeed)
                {
                    SCMT_Traction.decreasesetspeed();
                }
                if (keypressed == AbilitaBancoKey)
                {
                    SCMT_Traction.AbilitaBanco();
                }
                if (keypressed == ConsensoAvviamentoKey)
                {
                    SCMT_Traction.ConsensoAvviamento();
                }
                if (keypressed == AvviamentoKey)
                {
                    SCMT_Traction.Avviamento();
                }
                if (keypressed == SpegnimentoKey)
                {
                    SCMT_Traction.Spegnimento();
                }
                if (keypressed == LCMupKey)
                {
                    SCMT_Traction.LCMup();
                }
                if (keypressed == LCMdownkey)
                {
                    SCMT_Traction.LCMdown();
                }
                if (keypressed == TestSCMTKey)
                {
                    Train.SCMT_Traction.TestSCMT();
                }
            }
            if (Train.CAWS.enabled == true)
            {
                if (keypressed == CAWSKey)
                {
                    if (CAWS.AcknowledgementCountdown > 0.0)
                    {
                        CAWS.AcknowledgementCountdown = 0.0;
                        CAWS.AcknowledgementPending = false;
                    }
                }
            }
            //Italian SCMT vigilante system
            if (Train.vigilance != null && Train.vigilance.vigilante == true)
            {
                if (keypressed == vigilantekey)
                {
                    if (Train.vigilance.VigilanteState == vigilance.VigilanteStates.AlarmSounding)
                    {
                        Train.vigilance.VigilanteState = vigilance.VigilanteStates.OnService;
                    }
                }
                else if (keypressed == vigilanteresetkey)
                {
                    Train.vigilance.VigilanteReset();
                }
            }
            if (Train.PZB != null)
            {
                if (keypressed == PZBKey)
                {
                    Train.PZB.Acknowledge();
                    Train.PZB.WachamPressed = true;
                }
                if (keypressed == PZBReleaseKey)
                {
                    Train.PZB.Release();
                    Train.PZB.FreiPressed = true;
                }
                if (keypressed == PZBStopOverrideKey)
                {
                    if (Train.PZB.BefehlState == PZB.PZBBefehlStates.None)
                    {
                        Train.PZB.PZBBefehlState = PZB.PZBBefehlStates.Applied;
                    }
                    Train.PZB.StopOverrideKeyPressed = true;
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
            if (Train.vigilance != null)
            {
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
            if (Train.SCMT_Traction != null)
            {
                if (keypressed == SCMTincreasespeed || keypressed == SCMTdecreasespeed)
                {
                    SCMT_Traction.releasekey();
                }
                if (keypressed == AvviamentoKey)
                {
                    SCMT_Traction.AvviamentoReleased();
                }
                if (keypressed == SpegnimentoKey)
                {
                    SCMT_Traction.SpegnimentoReleased();
                }
            }
            if (Train.PZB != null)
            {
                if (keypressed == PZBKey)
                {
                    Train.PZB.WachamPressed = false;
                }
                if (keypressed == PZBReleaseKey)
                {
                    Train.PZB.Release();
                    Train.PZB.FreiPressed = false;
                }

                if (keypressed == PZBStopOverrideKey)
                {
                    Train.PZB.StopOverrideKeyPressed = false;
                    if (Train.PZB.PZBBefehlState != PZB.PZBBefehlStates.EBApplication)
                    {
                        Train.PZB.PZBBefehlState = PZB.PZBBefehlStates.None;
                    }
                }
            }
        }

        /// <summary>Is called when the driver changes the reverser.</summary>
        /// <param name="reverser">The new reverser position.</param>
        internal override void SetReverser(int reverser)
        {
            if (Train.vigilance != null)
            {
                if (Train.vigilance.DeadmansHandleState != vigilance.DeadmanStates.BrakesApplied &&
                    Train.vigilance.independantvigilance == 0)
                {
                    //Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
                    Train.vigilance.deadmanstimer = 0.0;
                    SoundManager.Stop(Train.vigilance.vigilancealarm);
                    Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
                }
            }

        }

        /// <summary>Is called when the driver changes the power notch.</summary>
        /// <param name="powerNotch">The new power notch.</param>
        internal override void SetPower(int powerNotch)
        {
            if (Train.vigilance != null)
            {
                if (Train.vigilance.DeadmansHandleState != vigilance.DeadmanStates.BrakesApplied &&
                    Train.vigilance.independantvigilance == 0)
                {
                    //Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
                    Train.vigilance.deadmanstimer = 0.0;
                    SoundManager.Stop(Train.vigilance.vigilancealarm);
                    Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
                }
            }
            //Trigger electric powerloop sound timer
            if (Train.electric != null)
            {
                Train.electric.powerloop = false;
                Train.electric.powerlooptimer = 0.0;
            }
        }

        /// <summary>Is called when the driver changes the brake notch.</summary>
        /// <param name="brakeNotch">The new brake notch.</param>
        internal override void SetBrake(int brakeNotch)
        {
            if (Train.vigilance != null)
            {
                if (Train.vigilance.DeadmansHandleState != vigilance.DeadmanStates.BrakesApplied &&
                    Train.vigilance.independantvigilance == 0)
                {
                    //Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
                    Train.vigilance.deadmanstimer = 0.0;
                    SoundManager.Stop(Train.vigilance.vigilancealarm);
                    Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
                }
            }
        }

        internal override void HornBlow(HornTypes type)
        {
            if (Train.vigilance != null)
            {
                if (Train.vigilance.DeadmansHandleState != vigilance.DeadmanStates.BrakesApplied &&
                    Train.vigilance.independantvigilance == 0)
                {
                    //Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
                    Train.vigilance.deadmanstimer = 0.0;
                    SoundManager.Stop(Train.vigilance.vigilancealarm);
                    Train.vigilance.DeadmansHandleState = vigilance.DeadmanStates.OnTimer;
                }
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
