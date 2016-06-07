using System;
using OpenBveApi.Runtime;
using System.Drawing;
using System.Windows.Input;
using Microsoft.Win32;

namespace Plugin
{
    //The traction manager operates panel variables common to all traction types, and handles power & brake requests
    internal class tractionmanager : Device
    {
        // --- members ---
        /// <summary>Power cutoff has been demaned</summary>
        internal bool powercutoffdemanded;
        /// <summary>A safety system has triggered a brake intervention</summary>
        internal bool brakedemanded;
        /// <summary>The current safety system brake notch demanded</summary>
        internal int currentbrakenotch;
        internal bool neutralrvrtripped;
        /// <summary>The engine has overheated</summary>
        internal bool overheated;
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
        internal string customindicatorsounds = "-1";
	    internal string customindicatorbehaviour = "-1";

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
        //Diesel locomotive functions
        internal string EngineStartKey;
        internal string EngineStopKey;

        //These keys are for the Western locomotive
        //This has it's own manager type
        internal string WesternBatterySwitch;
        internal string WesternMasterKey;
        internal string WesternTransmissionResetButton;
        internal string WesternEngineSwitchKey;
        internal string WesternAWSIsolationKey;
        internal string WesternFireBellKey;
        internal string WesternEngineOnlyKey;
        internal string WesternFuelPumpSwitch;

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

        /// <summary>The maximum power notch allowed by the engine</summary>
        internal int MaximumPowerNotch;
        /// <summary>The maximum power notch allowed by the safety system</summary>
        internal int SafetySystemMaximumPowerNotch;

        //Arrays
        int[] klaxonarray;
        //Custom Indicators
        internal CustomIndicator[] CustomIndicatorsArray = new CustomIndicator[10];
        
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
                InternalFunctions.LogError("klaxonindicator",0);
            }
            try
            {
                //Split custom indicators into an array
                string[] splitcustomindicators = customindicators.Split(',');
                for (int i = 0; i < CustomIndicatorsArray.Length; i++)
                {
                    CustomIndicatorsArray[i] = new CustomIndicator();
                    //Parse the panel value
                    if (i < splitcustomindicators.Length)
                    {
                        CustomIndicatorsArray[i].PanelIndex = Int32.Parse(splitcustomindicators[i]);
                    }
                    //Set the key assignments
                    if (String.IsNullOrEmpty(CustomIndicatorsArray[i].Key))
                    {
                        switch (i)
                        {
                            case 0:
                                CustomIndicatorsArray[i].Key = customindicatorkey1;
                            break;
                            case 1:
                                CustomIndicatorsArray[i].Key = customindicatorkey2;
                            break;
                            case 2:
                                CustomIndicatorsArray[i].Key = customindicatorkey3;
                            break;
                            case 3:
                                CustomIndicatorsArray[i].Key = customindicatorkey4;
                            break;
                            case 4:
                                CustomIndicatorsArray[i].Key = customindicatorkey5;
                            break;
                            case 5:
                                CustomIndicatorsArray[i].Key = customindicatorkey6;
                            break;
                            case 6:
                                CustomIndicatorsArray[i].Key = customindicatorkey7;
                            break;
                            case 7:
                                CustomIndicatorsArray[i].Key = customindicatorkey8;
                            break;
                            case 8:
                                CustomIndicatorsArray[i].Key = customindicatorkey9;
                            break;
                            case 9:
                                CustomIndicatorsArray[i].Key = customindicatorkey10;
                            break;
                        }
                    }
                }
                string[] splitcustomindicatorsounds = customindicatorsounds.Split(',');
                for (int i = 0; i < CustomIndicatorsArray.Length; i++)
                {
                    //Parse the sound index value if the array value is not empty
                    if (i < splitcustomindicators.Length && !String.IsNullOrEmpty(splitcustomindicatorsounds[i]))
                    {
                        CustomIndicatorsArray[i].SoundIndex = Int32.Parse(splitcustomindicatorsounds[i]);
                    }
                }
				string[] SplitCustomIndicatorType = customindicatorbehaviour.Split(',');
				for (int i = 0; i < CustomIndicatorsArray.Length; i++)
				{
					//Parse the sound index value if the array value is not empty
					if (i < splitcustomindicators.Length && !String.IsNullOrEmpty(SplitCustomIndicatorType[i]))
					{
						 InternalFunctions.ParseBool(SplitCustomIndicatorType[i], ref CustomIndicatorsArray[i].PushToMake,"CustomIndicatorBehaviour" + i);
					}
				}

            }
            catch
            {
                InternalFunctions.LogError("customindicators",0);
            }
            //Set traction type to pass to debug window
            //TODO:
            //This should be able to be moved to when the traction type is loaded
            if (Train.steam != null)
            {
                tractiontype = 0;
            }
            else if (Train.diesel != null)
            {
                tractiontype = 1;
            }
            else if (Train.electric != null)
            {
                tractiontype = 2;
            }
            else if (Train.WesternDiesel != null)
            {
                tractiontype = 3;
            }
            else
            {
                //Set traction type to 99 (Unknown)
                tractiontype = 99;
            }
            
		    MaximumPowerNotch = this.Train.Specs.PowerNotches;
		    SafetySystemMaximumPowerNotch = this.Train.Specs.PowerNotches;
		}

        /// <summary>Is called every frame.</summary>
        /// <param name="data">The data.</param>
        /// <param name="blocking">Whether the device is blocked or will block subsequent devices.</param>
        internal override void Elapse(ElapseData data, ref bool blocking)
        {
            //Cuts the power if required
            if (MaximumPowerNotch < this.Train.Handles.PowerNotch)
            {
                data.Handles.PowerNotch = MaximumPowerNotch;
            }
            //Door interlocks; Fitted to all trains
            if (this.Train.Doors > 0)
            {
                if (doorpowerlock == 1 && Train.tractionmanager.powercutoffdemanded == false)
                {
                    Train.tractionmanager.demandpowercutoff();
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
                if ((Train.tractionmanager.powercutoffdemanded == true || brakedemanded == true) && doorlock == true)
                {
                    Train.tractionmanager.resetpowercutoff();
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
                        Train.tractionmanager.neutralrvrtripped = true;
                    }
                }
                else if (neutralrvrbrake == 2)
                {
                    if (Train.Handles.Reverser == 0)
                    {
                        Train.tractionmanager.demandbrakeapplication(this.Train.Specs.BrakeNotches);
                        Train.tractionmanager.neutralrvrtripped = true;
                    }
                }

                if (neutralrvrtripped == true)
                {
                    //OS_ATS Default behaviour
                    if (neutralrvrbrakereset == 0 && Train.Handles.Reverser != 0)
                    {
                        Train.tractionmanager.resetbrakeapplication();
                        Train.tractionmanager.neutralrvrtripped = false;
                    }
                    //Behaviour 1- Train must come to a full stand before brakes are reset
                    if (neutralrvrbrakereset == 1 && Train.trainspeed == 0)
                    {
                        Train.tractionmanager.resetbrakeapplication();
                        Train.tractionmanager.neutralrvrtripped = false;
                    }
                    //Behaviour 2- Train must come to a full stand and driver applies full service brakes before reset
                    if (neutralrvrbrakereset == 2 && Train.Handles.BrakeNotch == this.Train.Specs.BrakeNotches && Train.trainspeed == 0)
                    {
                        Train.tractionmanager.resetbrakeapplication();
                        Train.tractionmanager.neutralrvrtripped = false;
                    }
                }
            }
            //Insufficient steam boiler pressure
            //Debug messages need to be called via the traction manager to be passed to the debug window correctly
            if (Train.steam != null && Train.steam.stm_power == 0 && Train.steam.stm_boilerpressure < Train.steam.boilerminpressure)
            {
                data.DebugMessage = "Power cutoff due to boiler pressure below minimum";
            }

            if (Train.tractionmanager.powercutoffdemanded == true)
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
				else if (Train.WesternDiesel != null && (Train.WesternDiesel.Engine1Temperature.Overheated == true || Train.WesternDiesel.Engine2Temperature.Overheated == true))
				{
					if (Train.WesternDiesel.Engine1Temperature.Overheated && Train.WesternDiesel.Engine2Temperature.Overheated)
					{
						data.DebugMessage = "Power cutoff demanded by the Western Diesel engines 1 and 2 overheating";
					}
					else if (Train.WesternDiesel.Engine1Temperature.Overheated)
					{
						data.DebugMessage = "Power cutoff demanded by the Western Diesel engine 1 overheating";
					}
					else
					{
						data.DebugMessage = "Power cutoff demanded by the Western Diesel engine 2 overheating";
					}
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
                if (Train.AWS != null && Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerExpired)
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
                else if (Train.tractionmanager.neutralrvrtripped)
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
                else if (Train.CAWS != null && Train.CAWS.enabled == true)
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
                for (int i = 0; i < CustomIndicatorsArray.GetLength(0); i++)
                {
                    if (CustomIndicatorsArray[i].PanelIndex != -1)
                    {
                        Train.Panel[CustomIndicatorsArray[i].PanelIndex] = Convert.ToInt32(CustomIndicatorsArray[i].Active);
                    }
                }
            }

            //Handles the debug/ advanced driving window
            if (debugwindowshowing == true)
            {
                if (AdvancedDriving.CheckInst == null)
                {
                    AdvancedDriving.CreateInst.Show(); // This creates and displays Form2

                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\BVEC_ATS", true))
                    {
                        if (key != null)
                        {
                            AdvancedDriving.CreateInst.Left = (int) key.GetValue("Left");
                            AdvancedDriving.CreateInst.Top = (int) key.GetValue("Top");
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

        //Call this function to set the maximum permissable power notch
        /// <summary>Sets the maximum permissiable power notch.</summary>
        /// <param name="NotchRequested">The notch requested.</param>
        /// <param name="SafetySystem">Whether this is being requested by a safety system, or the traction model.</param>
        /// Lower notches will always succeed.
        /// A notch increase must always be first requested by a safety system if applicable
        internal void SetMaxPowerNotch(int NotchRequested, bool SafetySystem)
        {
            if (NotchRequested < MaximumPowerNotch)
            {
                MaximumPowerNotch = NotchRequested;
                if (SafetySystem == true)
                {
                    //Set the maximum power notch allowed by the safety system
                    SafetySystemMaximumPowerNotch = NotchRequested;
                }
            }
            else
            {
                //Return the minimum of the notch requested and the safety system's maximum power notch
                MaximumPowerNotch = Math.Min(SafetySystemMaximumPowerNotch, NotchRequested);
            }
        }

        //Call this function from a safety system to demand power cutoff
        /// <summary>Deamnds traction power cutoff.</summary>
        /// <remarks>This call will always succeed.</remarks>
        internal void demandpowercutoff()
        {
            Train.tractionmanager.powercutoffdemanded = true;
        }

        //Call this function to attempt to reset the power cutoff
        /// <summary>Attempts to reset the power cutoff state.</summary>
        /// <remarks>The default OS_ATS behaviour is to reset all cutoffs at once.</remarks>
        internal void resetpowercutoff()
        {
            //Do not reset power cutoff if still overheated
            if (overheated == true)
            {
                Train.DebugLogger.LogMessage("Traction power was not restored due to an overheated engine");
                return;
            }
            //Do not reset power cutoff if AWS brake demands are active
            if (Train.AWS != null &&
                (Train.AWS.SafetyState == AWS.SafetyStates.TPWSAWSBrakeDemandIssued ||
                 Train.AWS.SafetyState == AWS.SafetyStates.TPWSTssBrakeDemandIssued))
            {
                Train.DebugLogger.LogMessage("Traction power was not restored due to an AWS/ TPWS intervention");
                return;
            }
            //Do not reset power cutoff if DRA is active
            if (Train.drastate == true)
            {
                Train.DebugLogger.LogMessage("Traction power was not restored due to the DRA being active");
                return;
            }
            //Do not reset power cutoff if doors are still open
            if (doorlock == true)
            {
                Train.DebugLogger.LogMessage("Traction power was not restored due to the doors power lock being active");
                return;
            }
            //Do not reset power cutoff if ATC is still demanding power cutoff via open doors
            if (Train.Atc != null && (Train.Atc.State != Atc.States.Disabled && this.Train.Doors != DoorStates.None))
            {
                Train.DebugLogger.LogMessage("Traction power was not restored due to the ATC door power lock being active");
                return;
            }
            //Do not reset power cutoff if ATS-P is still demanding power cutoff via open doors
            if (Train.AtsP != null && (Train.AtsP.State != AtsP.States.Disabled && this.Train.Doors != DoorStates.None))
            {
                Train.DebugLogger.LogMessage("Traction power was not restored due to the ATS-P power lock being active");
                return;
            }
            //Do not reset power cutoff if ATS-SX is still demanding power cutoff via open doors
            if (Train.AtsSx != null && (Train.AtsSx.State != AtsSx.States.Disabled && this.Train.Doors != DoorStates.None))
            {
                Train.DebugLogger.LogMessage("Traction power was not restored due to the ATS-Sx power lock being active");
                return;
            }
            if (Train.WesternDiesel != null && Train.WesternDiesel.GearBox.TorqueConvertorState != WesternGearBox.TorqueConvertorStates.OnService)
            {
                Train.DebugLogger.LogMessage("Traction power was not restored due to the Western Diesel torque convertor being out of service");
                return;
            }
            if (Train.WesternDiesel != null && Train.WesternDiesel.TransmissionTemperature.Overheated)
            {
                Train.DebugLogger.LogMessage("Traction power was not restored due to the Western Diesel transmission being overheated");
                return;
            }
            Train.tractionmanager.powercutoffdemanded = false;
            Train.DebugLogger.LogMessage("Traction power restored");

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
                if (Train.AWS != null && Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerExpired)
                {
                    Train.DebugLogger.LogMessage("The current brake application was not reset due to a AWS/ TPWS intervention.");
                    return;
                }
                if (Train.overspeedtripped == true)
                {
                    Train.DebugLogger.LogMessage("The current brake application was not reset due to the overspeed device being active.");
                    return;
                }
                if (Train.vigilance.DeadmansHandleState == vigilance.DeadmanStates.BrakesApplied)
                {
                    Train.DebugLogger.LogMessage("The current brake application was not reset due to the deadman's handle being active.");
                    return;
                }
                if (Train.TPWS != null && (Train.TPWS.SafetyState == TPWS.SafetyStates.TssBrakeDemand ||
                                                Train.TPWS.SafetyState == TPWS.SafetyStates.BrakeDemandAcknowledged ||
                                                Train.TPWS.SafetyState == TPWS.SafetyStates.BrakesAppliedCountingDown))
                {
                    Train.DebugLogger.LogMessage("The current brake application was not reset due to a AWS/ TPWS intervention.");
                    return;
                }
                if (doorlock == true)
                {
                    Train.DebugLogger.LogMessage("The current brake application was not reset due to the doors power lock being active.");
                    return;
                }
                if (Train.tractionmanager.neutralrvrtripped && neutralrvrbrake == 2)
                {
                    Train.DebugLogger.LogMessage("The current brake application was not reset due to the neutral reverser safety device.");
                    return;
                }
                if (Train.SCMT.enabled == true && SCMT.EBDemanded == true)
                {
                    Train.DebugLogger.LogMessage("The current brake application was not reset due to a SCMT intervention.");
                    return;
                }
                if (Train.vigilance.VigilanteState == vigilance.VigilanteStates.EbApplied)
                {
                    Train.DebugLogger.LogMessage("The current brake application was not reset due to a SCMT Vigilante intervention.");
                    return;
                }
                if (Train.CAWS.enabled == true && Train.CAWS.EmergencyBrakeCountdown < 0.0)
                {
                    Train.DebugLogger.LogMessage("The current brake application was not reset due to a CAWS intervention.");
                    return;
                }
                //These conditions set a different brake notch to EB
                //
                //Set service brakes as reverser is in neutral
                if (Train.tractionmanager.neutralrvrtripped && neutralrvrbrake == 1)
                {
                    currentbrakenotch = Train.Specs.BrakeNotches;
                    return;
                }
                //Set brake notch 1 for SCMT constant speed device
                if (Train.SCMT.enabled == true && SCMT_Traction.ConstantSpeedBrake == true)
                {
                    Train.DebugLogger.LogMessage("The currently demanded brake notch was changed to 1 due to the SCMT constant-speed brake.");
                    currentbrakenotch = 1;
                    return;
                }
                //Do not reset brake application if ATC is currently demanding one
                if (Train.Atc != null && (Train.Atc.State == Atc.States.ServiceHalf || Train.Atc.State == Atc.States.ServiceFull || Train.Atc.State == Atc.States.Emergency))
                {
                    Train.DebugLogger.LogMessage("The current brake application was not reset due to a ATC intervention.");
                    return;
                }
                //Do not reset brake application if ATS-P is currently demanding one
                if (Train.AtsP != null && (Train.AtsP.State == AtsP.States.Brake || Train.AtsP.State == AtsP.States.Service || Train.AtsP.State == AtsP.States.Emergency))
                {
                    Train.DebugLogger.LogMessage("The current brake application was not reset due to a ATS-P intervention.");
                    return;
                }
                //Do not reset brake application if ATS-PS is currently demanding one
                if (Train.AtsPs != null && (Train.AtsPs.State == AtsPs.States.Emergency))
                {
                    Train.DebugLogger.LogMessage("The current brake application was not reset due to a ATS-PS intervention.");
                    return;
                }
                //Do not reset brake application if ATS-SX is currently demanding one
                if (Train.AtsSx != null && (Train.AtsSx.State == AtsSx.States.Emergency))
                {
                    Train.DebugLogger.LogMessage("The current brake application was not reset due to a ATS-S intervention.");
                    return;
                }
				//Do not reset brake application if F92 is currently overspeed
				if (Train.F92 != null && (Train.trainspeed > 70))
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to the F92's overspeed device.");
					return;
				}
				//Do not reset brake application if F92 has passed a red signal
				if (Train.F92 != null && Train.F92.PassedRedSignal == true)
				{
					Train.DebugLogger.LogMessage("The current brake application was not reset due to the F92 having passed a red signal.");
					return;
				}
            }
            Train.DebugLogger.LogMessage("The current brake application was reset.");
            currentbrakenotch = 0;
            brakedemanded = false;
        }

        //Call this function to attempt to isolate or re-enable the TPWS & AWS Systems
        /// <summary>Attempts to disable or re-enable the TPWS & AWS safety systems.</summary>
        internal void isolatetpwsaws()
        {
            if (Train.AWS == null)
            {
                return;
            }
            bool CanIsolate = false;
            if (safetyisolated == false)
            {
                //First check if TPWS is enabled in this train [AWS must therefore be enabled]
                if (Train.TPWS.enabled == true)
                {
                    if (Train.TPWS.SafetyState == TPWS.SafetyStates.None && (Train.AWS.SafetyState == AWS.SafetyStates.Clear || Train.AWS.SafetyState == AWS.SafetyStates.None))
                    {
                        CanIsolate = true;
                    }
                }
                else if (Train.TPWS.enabled == false && Train.AWS.enabled == true)
                {
                    if (Train.AWS.SafetyState == AWS.SafetyStates.Clear || Train.AWS.SafetyState == AWS.SafetyStates.None)
                    {
                        CanIsolate = true;
                    }
                }

                if (CanIsolate == true)
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
            if (Train.AWS == null)
            {
                return;
            }
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
            if (key == VirtualKeys.LiveSteamInjector || key == VirtualKeys.ExhaustSteamInjector)
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

                if (Train.AWS != null)
                {
                    //Acknowledge AWS warning
                    if (Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerActive)
                    {
                        Train.AWS.Acknowlege();
                    }
                    //Reset AWS
                    else if (Train.AWS.SafetyState == AWS.SafetyStates.CancelTimerExpired && Train.trainspeed == 0 &&
                        Train.Handles.Reverser == 0)
                    {
                        if (SoundManager.IsPlaying(Train.AWS.awswarningsound))
                        {
                            SoundManager.Stop(Train.AWS.awswarningsound);
                        }
                        Train.AWS.Reset();
                        resetpowercutoff();
                    }
	                this.Train.AWS.CancelButtonPressed = true;
                }
                if (Train.TPWS != null)
                {
                    //Acknowledge TPWS Brake Demand
                    if (Train.TPWS.SafetyState == TPWS.SafetyStates.TssBrakeDemand)
                    {
                        Train.TPWS.AcknowledgeBrakeDemand();
                    }
                }
                //Acknowledge Self-Test warning
                if (Train.StartupSelfTestManager != null && Train.StartupSelfTestManager.SequenceState == StartupSelfTestManager.SequenceStates.AwaitingDriverInteraction)
                {
                    Train.StartupSelfTestManager.driveracknowledge();
                }
            }
            if (key == VirtualKeys.FillFuel)
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
            if (key == VirtualKeys.WiperSpeedDown)
            {
                //Wipers Speed Down
                if (Train.Windscreen.enabled == true)
                {
                    Train.Windscreen.windscreenwipers(1);
                }
            }
            if (key == VirtualKeys.WiperSpeedUp)
            {
                //Wipers Speed Up
                if (Train.Windscreen.enabled == true)
                {
                    Train.Windscreen.windscreenwipers(0);
                }
            }
            if (keypressed == isolatesafetykey)
            {
                if (Train.WesternDiesel != null)
                {
                    Train.WesternDiesel.ToggleAWS();
                }
                else
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

            foreach (CustomIndicator Indicator in CustomIndicatorsArray)
            {
                if (keypressed == Indicator.Key)
                {
                    Indicator.Active = !Indicator.Active;
                    //Play the toggle sound if this has been set
                    if (Indicator.SoundIndex != -1)
                    {
                        SoundManager.Play(Indicator.SoundIndex, 1.0, 1.0, false);
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
                if (key == VirtualKeys.IncreaseCutoff)
                {
                    //Cutoff Up
                    if (Train.steam != null)
                    {
                        Train.steam.cutoffstate = 1;
                    }
                }
                if (key == VirtualKeys.DecreaseCutoff)
                {
                    //Cutoff Down
                    if (Train.steam != null)
                    {
                        Train.steam.cutoffstate = -1;
                    }
                }
                //Blowers
                if (key == VirtualKeys.Blowers)
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
            if (Train.CAWS != null)
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
            //Western Diesel Locomotive
            if (Train.WesternDiesel != null)
            {
                if (keypressed == safetykey)
                {
                    if (Train.WesternDiesel.StartupManager.StartupState == WesternStartupManager.SequenceStates.DirectionSelected)
                    {
                        //Acknowledge the DSD Buzzer
                        Train.WesternDiesel.StartupManager.StartupState = WesternStartupManager.SequenceStates.DSDAcknowledged;
                    }
                }
                if (key == VirtualKeys.EngineStart)
                {
                    Train.WesternDiesel.StarterKeyPressed = true;
                }
                if (keypressed == WesternBatterySwitch)
                {
                    Train.WesternDiesel.BatterySwitch();
                }
                if (keypressed == WesternMasterKey)
                {
                    Train.WesternDiesel.MasterKey();
                }
                
                if (keypressed == WesternAWSIsolationKey)
                {
                    Train.WesternDiesel.ToggleAWS();
                }
                if (keypressed == WesternFireBellKey)
                {
                    Train.WesternDiesel.FireBellTest();
                }
                if (keypressed == WesternEngineOnlyKey)
                {
                    Train.WesternDiesel.EngineOnly = !Train.WesternDiesel.EngineOnly;
                }
                if (keypressed == WesternEngineSwitchKey)
                {
                    if (Train.WesternDiesel.EngineSelector == 1)
                    {
                        Train.WesternDiesel.EngineSelector = 2;
                    }
                    else
                    {
                        Train.WesternDiesel.EngineSelector = 1;
                    }
                }
                if (key == VirtualKeys.EngineStop)
                {
                    if (Train.WesternDiesel.EngineSelector == 1)
                    {
                        Train.WesternDiesel.EngineStop(1);
                    }
                    else
                    {
                        Train.WesternDiesel.EngineStop(2);
                    }
                }
                if (keypressed == WesternFuelPumpSwitch)
                {
                    Train.WesternDiesel.FuelPumpSwitch();
                }
            }
        }

        internal override void KeyUp(VirtualKeys key)
        {
            //Convert keypress to string for comparison
            string keypressed = Convert.ToString(key);
            if (key == VirtualKeys.GearUp)
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
            if (key == VirtualKeys.GearDown)
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
            if (key == VirtualKeys.IncreaseCutoff)
            {
                //Cutoff Up
                if (Train.steam != null)
                {
                    Train.steam.cutoffstate = 0;
                }
            }
            if (key == VirtualKeys.DecreaseCutoff)
            {
                //Cutoff Down
                if (Train.steam != null)
                {
                    Train.steam.cutoffstate = 0;
                }
            }
            if (key == VirtualKeys.FillFuel)
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
			foreach (CustomIndicator Indicator in CustomIndicatorsArray)
			{
				//Reset any push-to-make indicators
				if (keypressed == Indicator.Key && Indicator.PushToMake == true)
				{
					Indicator.Active = !Indicator.Active;
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
            //Western Diesel Locomotive
            if (Train.WesternDiesel != null)
            {
                if (key == VirtualKeys.EngineStart)
                {
                    Train.WesternDiesel.StarterKeyPressed = false;
                }
            }
	        if (Train.AWS != null)
	        {
		        if (keypressed == safetykey)
		        {
			        this.Train.AWS.CancelButtonPressed = false;
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
