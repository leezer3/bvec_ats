using System;
using OpenBveApi.Runtime;

namespace Plugin
{
    internal class tractionmanager : Device
    {
        // --- members ---
        private static bool powercutoffdemanded;
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
        
        /// <summary>The underlying train.</summary>
        private Train Train;

        //Default Variables
        internal double doorpowerlock = 0;
        internal double doorapplybrake = 0;
        internal double neutralrvrbrake = 0;
        internal double neutralrvrbrakereset = 0;
        internal double directionindicator = -1;
        internal double reverserindex = -1;
        internal double travelmeter100 = -1;
        internal double travelmeter10 = -1;
        internal double travelmeter1 = -1;
        internal double travelmeter01 = -1;
        internal double travelmeter001 = -1;
        internal double travelmetermode = 0;
        internal string klaxonindicator = "-1";
        internal string customindicators = "-1";

        //Arrays
        int[] klaxonarray;
        int[,] customindicatorsarray;

        internal tractionmanager(Train train) {
			this.Train = train;
                       
		}
		
		//<param name="mode">The initialization mode.</param>
		internal override void Initialize(InitializationModes mode) {
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
            //Split klaxon indicators and timings into an array
            string[] splitcustomindicators = customindicators.Split(',');
            customindicatorsarray = new int[splitcustomindicators.Length,2];
            for (int i = 0; i < splitcustomindicators.Length; i++)
            {
                //Parse the panel value and set second array value to false
                customindicatorsarray[i, 0] = Int32.Parse(splitcustomindicators[i]);
                customindicatorsarray[i, 1] = 0;
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
                }

                if (doorapplybrake == 1 && tractionmanager.brakedemanded == false)
                {
                    tractionmanager.demandbrakeapplication();
                    data.DebugMessage = "Brakes demanded by open doors";
                }
            }
            else
            {
                if (tractionmanager.powercutoffdemanded == true)
                {
                    tractionmanager.resetpowercutoff();
                }

                if (tractionmanager.brakedemanded == true)
                {
                    tractionmanager.resetbrakeapplication();
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

            if (tractionmanager.powercutoffdemanded == true)
            {
                if (Train.drastate == true)
                {
                    data.DebugMessage = "Power cutoff demanded by DRA Appliance";
                }
                else if (electric.powergap == true)
                {
                    data.DebugMessage = "Power cutoff demanded by electric conductor power gap";
                }
                else if (electric.breakertripped == true)
                {
                    data.DebugMessage = "Power cutoff demanded by ACB/VCB tripping";
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
                else if (Train.deadmanstripped == true)
                {
                    data.DebugMessage = "EB Brakes demanded by deadman's handle";
                    data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches + 1;
                }
                else if (Train.TPWS.SafetyState == TPWS.SafetyStates.TssBrakeDemand || Train.TPWS.SafetyState == TPWS.SafetyStates.BrakeDemandAcknowledged || Train.TPWS.SafetyState == TPWS.SafetyStates.BrakesAppliedCountingDown)
                {
                    data.DebugMessage = "EB Brakes demanded by TPWS Device";
                    data.Handles.BrakeNotch = this.Train.Specs.BrakeNotches + 1;
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
            //Independant Panel Variables
            if (directionindicator != -1)
            {
                //Direction Indicator
                this.Train.Panel[(int)directionindicator] = this.Train.direction;
            }
            if (reverserindex != -1)
            {
                //Reverser Indicator
                if (Train.Handles.Reverser == 0)
                {
                    this.Train.Panel[(int)reverserindex] = 0;
                }
                else if (Train.Handles.Reverser == 1)
                {
                    this.Train.Panel[(int)reverserindex] = 1;
                }
                else
                {
                    this.Train.Panel[(int)reverserindex] = 2;
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
                    this.Train.Panel[(int)travelmeter100] = travel100;

                }
                //10km
                if (travelmeter10 != -1)
                {
                    this.Train.Panel[(int)travelmeter10] = travel10;

                }
                //1km
                if (travelmeter1 != -1)
                {
                    this.Train.Panel[(int)travelmeter1] = travel1;

                }
                //100m
                if (travelmeter01 != -1)
                {
                    this.Train.Panel[(int)travelmeter01] = travel01;

                }
                //1m
                if (travelmeter001 != -1)
                {
                    this.Train.Panel[(int)travelmeter001] = travel001;

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
                        }
                    }
                    //Secondary Horn
                    else if (secondaryklaxonplaying == true && klaxonarray[2] != 500)
                    {
                        this.Train.Panel[klaxonarray[2]] = 1;
                        if (klaxonindicatortimer > klaxonarray[3])
                        {
                            secondaryklaxonplaying = false;
                        }
                    }
                    //Music Horn
                    else if (musicklaxonplaying == true && klaxonarray[4] != 500)
                    {
                        this.Train.Panel[klaxonarray[4]] = 1;
                        if (klaxonindicatortimer > klaxonarray[5])
                        {
                            musicklaxonplaying = false;
                        }
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
        internal static void demandbrakeapplication()
        {
            tractionmanager.brakedemanded = true;
        }

        //Call this function from a safety system to reset a brake application
        internal static void resetbrakeapplication()
        {

            tractionmanager.brakedemanded = false;
        }

        //Call this function to attempt to isolate or re-enable the TPWS & AWS Systems
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
            {
                if (Train.deadmanstripped == false && Train.vigilance.independantvigilance != 0)
                {
                    //Only reset deadman's timer automatically for any key if it's not already tripped and independant vigilance is not set
                    Train.vigilance.deadmanstimer = 0.0;
                }
            }

            {
                switch (key)
                {
                    //Toggle Automatic Cutoff & gears
                    case VirtualKeys.A2:
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
                        break;
                    case VirtualKeys.B2:
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
                        break;

                    case VirtualKeys.C1:
                        //Cutoff Up
                        if (Train.steam != null)
                        {
                            Train.steam.cutoffstate = 1;
                        }
                        break;
                    //Cutoff Down
                    case VirtualKeys.C2:
                        if (Train.steam != null)
                        {
                            Train.steam.cutoffstate = -1;
                        }
                        break;
                    
                    //Use INS to reset safety devices
                    case VirtualKeys.A1:
                        //Reset vigilance timer if independant vigilance is selected & the deadman's timer has not tripped
                        if (Train.vigilance.independantvigilance != 0 && Train.deadmanstripped == false)
                        {
                            Train.vigilance.deadmanstimer = 0.0;
                        }
                        //Reset Overspeed Trip
                        if (Train.trainspeed == 0 && Train.overspeedtripped == true)
                        {
                            Train.overspeedtripped = false;
                            resetbrakeapplication();
                        }

                        //Reset Deadman's Trip
                        if (Train.vigilance.vigilancecancellable != 0 && Train.deadmanstripped == true || Train.trainspeed == 0 && Train.deadmanstripped == true)
                        {
                            Train.deadmanstripped = false;
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
                            if (SoundManager.IsPlaying((int)Train.AWS.awswarningsound))
                            {
                                SoundManager.Stop((int)Train.AWS.awswarningsound);
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
                        break;



                    case VirtualKeys.D:
                        //Toggle Custom Indicator 1
                        if (customindicatorsarray[0,0] != -1)
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
                        break;

                    case VirtualKeys.E:
                        //Toggle Custom Indicator 2
                        if (customindicatorsarray.Length >= 1)
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
                        break;

                    case VirtualKeys.F:
                        //Toggle Custom Indicator 3
                        if (customindicatorsarray.Length >= 2)
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
                        break;

                    case VirtualKeys.G:
                        //Toggle Custom Indicator 4
                        if (customindicatorsarray.Length >= 3)
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
                        break;

                    case VirtualKeys.H:
                        //Toggle Custom Indicator 5
                        if (customindicatorsarray.Length >=4)
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
                        break;

                    case VirtualKeys.I:
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
                        break;

                    case VirtualKeys.J:
                        //Wipers Speed Down
                        if (Train.Windscreen.enabled == true)
                        {
                            Train.Windscreen.windscreenwipers(0);
                        }
                        break;
                    case VirtualKeys.K:
                        //Wipers Speed Up
                        if (Train.Windscreen.enabled == true)
                        {
                            Train.Windscreen.windscreenwipers(1);
                        }
                        break;
                    case VirtualKeys.L:
                        //Isolate Safety Systems
                        if (safetyisolated == false)
                        {
                            isolatetpwsaws();
                        }
                        else
                        {
                            reenabletpwsaws();
                        }
                        break;
                }
            }
        }

        internal override void KeyUp(VirtualKeys key)
        {
            switch (key)
            {

                case VirtualKeys.B1:
                    //Gear Up
                    if (Train.diesel != null)
                    {
                        if (Train.diesel.gear >= 0 && Train.diesel.gear < Train.diesel.totalgears - 1 && Train.Handles.PowerNotch == 0)
                        {
                            Train.diesel.gear++;
                            Train.diesel.gearchange();
                        }
                    }
                    break;
                //Gear Down
                case VirtualKeys.B2:
                    if (Train.diesel != null)
                    {
                        if (Train.diesel.gear <= Train.diesel.totalgears && Train.diesel.gear > 0 && Train.Handles.PowerNotch == 0)
                        {
                            Train.diesel.gear--;
                            Train.diesel.gearchange();
                        }
                    }
                    break;
                case VirtualKeys.C1:
                    //Cutoff Up
                    if (Train.steam != null)
                    {
                        Train.steam.cutoffstate = 0;
                    }
                    break;

                case VirtualKeys.C2:
                    if (Train.steam != null)
                    {
                        Train.steam.cutoffstate = 0;
                    }
                    break;
                case VirtualKeys.I:
                    //Toggle Fuel fill
                        if (Train.steam != null)
                        {
                            Train.steam.fuelling = false;
                        }
                        if (Train.diesel != null)
                        {
                            Train.diesel.fuelling = false;
                        }
                    break;
                case VirtualKeys.S:
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
                    break;
            }
        }

        /// <summary>Is called when the driver changes the reverser.</summary>
        /// <param name="reverser">The new reverser position.</param>
        internal override void SetReverser(int reverser)
        {
            if (Train.deadmanstripped == false && Train.vigilance.independantvigilance != 0)
            {
                Train.vigilance.deadmanstimer = 0.0;
            }

        }

        /// <summary>Is called when the driver changes the power notch.</summary>
        /// <param name="powerNotch">The new power notch.</param>
        internal override void SetPower(int powerNotch)
        {
            //Reset vigilance if independant vigilance is not selected
            if (Train.deadmanstripped == false && Train.vigilance.independantvigilance != 0)
            {
                Train.vigilance.deadmanstimer = 0.0;
            }
        }

        /// <summary>Is called when the driver changes the brake notch.</summary>
        /// <param name="brakeNotch">The new brake notch.</param>
        internal override void SetBrake(int brakeNotch)
        {
            //Reset vigilance if independant vigilance is not selected
            if (Train.deadmanstripped == false && Train.vigilance.independantvigilance != 0)
            {
                Train.vigilance.deadmanstimer = 0.0;
            }
        }

        internal override void HornBlow(HornTypes type)
        {
            //Reset vigilance if independant vigilance is not selected
            if (Train.deadmanstripped == false && Train.vigilance.independantvigilance != 0)
            {
                Train.vigilance.deadmanstimer = 0.0;
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
